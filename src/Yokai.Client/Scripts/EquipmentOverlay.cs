using Godot;
using Yokai.Core;
namespace Yokai.Client;

/// <summary>Right-click interaction for the armory's existing equipment rows.</summary>
public partial class EquipmentOverlay : Control
{
    Game game = null!;
    string message = "";
    double messageUntil;
    bool Active => game is not null && game.ShopOpen && !game.HelpOpen && !game.Paused && !game.World.Winner.HasValue;
    public override void _Ready()
    {
        game = GetParent().GetParent<Game>();
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }
    public override void _Process(double delta) => QueueRedraw();
    public bool TryHandleEquipClick(Vector2 point, MouseButton button)
    {
        if (!Active || button != MouseButton.Right) return false;
        var size = GetViewportRect().Size;
        var x = size.X / 2 - 450; var y = Math.Max(65, size.Y / 2 - 330);
        var index = 0;
        foreach (var slot in Enum.GetValues<EquipSlot>())
        {
            // Same equipment-row layout rendered by Hud.Shop; UiReview guards this hit region.
            var rect = new Rect2(x + 24 + index % 4 * 215, y + 569 + index / 4 * 20, 210, 20);
            index++;
            if (!rect.HasPoint(point)) continue;
            game.World.TryUnequip(slot, out message); messageUntil = game.World.Time + 4;
            return true;
        }
        return false;
    }
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.Pressed && TryHandleEquipClick(mouse.Position, mouse.ButtonIndex))
            GetViewport().SetInputAsHandled();
    }
    public override void _Draw()
    {
        if (!Active) return;
        var size = GetViewportRect().Size; var x = size.X / 2 - 450; var y = Math.Max(65, size.Y / 2 - 330);
        DrawString(ThemeDB.FallbackFont, new(x + 150, y + 558), "Right-click an equipped item to store it.",
            HorizontalAlignment.Left, -1, 12, Art.Gold);
        if (game.World.Time < messageUntil)
            DrawString(ThemeDB.FallbackFont, new(x + 24, y + 646), message, HorizontalAlignment.Left, -1, 12, Art.Gold);
    }
}
