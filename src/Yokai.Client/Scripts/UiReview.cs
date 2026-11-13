using Godot;
using Yokai.Core;
namespace Yokai.Client;

/// <summary>Exercises real drawn HUD hit regions, not only the shop's simulation methods.</summary>
public partial class UiReview : Node
{
    Game game = null!;
    Hud hud = null!;
    EquipmentOverlay equipment = null!;
    string prefix = "";
    int frame, assertions;
    public override void _Ready()
    {
        game = GetParent<Game>();
        prefix = OS.GetCmdlineUserArgs().FirstOrDefault(s => s.StartsWith("--capture=", StringComparison.Ordinal))?[10..] ?? "";
    }
    void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("UI REVIEW: " + label);
        assertions++; GD.Print("UI_CHECK_OK " + label);
    }
    void Click(float dx, float dy, MouseButton button = MouseButton.Left)
    {
        var size = hud.GetViewportRect().Size;
        var p = new Vector2(size.X / 2 - 450 + dx, Math.Max(65, size.Y / 2 - 330) + dy);
        Check(button == MouseButton.Right ? equipment.TryHandleEquipClick(p, button) : hud.HandleClick(p, button), "HUD handled " + button);
    }
    void Capture(string suffix)
    {
        if (prefix.Length == 0 || DisplayServer.GetName() == "headless") return;
        var result = GetViewport().GetTexture().GetImage().SavePng(prefix + "-" + suffix + ".png");
        Check(result == Error.Ok, "captured " + suffix);
    }
    public override void _Process(double delta)
    {
        frame++;
        switch (frame)
        {
            case 2:
                hud = game.GetChildren().OfType<CanvasLayer>().SelectMany(layer => layer.GetChildren()).OfType<Hud>().Single();
                equipment = game.GetNode<EquipmentOverlay>("EquipmentLayer/EquipmentOverlay");
                game.StartMatch(); game.ShopOpen = true;
                break;
            case 10: Click(380, 171); Check(game.World.Player.Loadout[EquipSlot.MainHand]?.Id == "brass", "buy primary glove"); break;
            case 12: Click(734, 100); break;
            case 14: Click(810, 171); Check(game.World.Player.Loadout[EquipSlot.OffHand]?.Id == "spiked", "buy off-hand glove"); break;
            case 20: Capture("shop"); break;
            case 22: Click(554, 598, MouseButton.Right); Check(game.World.Player.Loadout[EquipSlot.OffHand] is null, "store off-hand glove"); break;
            case 24: Click(124, 598, MouseButton.Right); Check(game.World.Player.Loadout[EquipSlot.MainHand] is null && game.World.Player.Storage.Count == 2, "store primary without losing either item"); break;
            case 26:
                game.World.Player.Gold += 300; Click(734, 100); Click(810, 250);
                Check(game.World.Player.Loadout[EquipSlot.MainHand]?.Id == "crossbow" && game.World.Player.IsRanged, "change from dual fists to ranged weapon through shop");
                break;
            case 28:
                var test = new GameWorld(true); test.Player.Gold = 1000;
                Check(test.Buy(Items.Neck) && test.Buy(Items.Katana) && test.Buy(Items.Shield), "prepare strength-dependent loadout");
                Check(!test.TryUnequip(EquipSlot.Neck, out _) && test.Player.Loadout[EquipSlot.Neck] is not null && test.Player.Storage.Count == 0, "failed removal restores Strength item atomically");
                test.Player.Position = new(0, 0);
                Check(!test.TryUnequip(EquipSlot.OffHand, out _) && test.Player.Loadout[EquipSlot.OffHand] is not null, "cannot store equipment outside shrine");
                break;
            case 30: game.ShopOpen = false; game.HelpOpen = true; break;
            case 34: Capture("help"); break;
            case 40: game.HelpOpen = false; game.TogglePause(); break;
            case 44: Capture("pause"); break;
            case 48: GD.Print($"UI_REVIEW_OK assertions={assertions}"); GetTree().Quit(); break;
        }
    }
}
