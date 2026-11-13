using Godot;
using Yokai.Core;
namespace Yokai.Client;

public partial class Hud : Control
{
    public Game Game { get; set; } = null!;
    readonly List<(Rect2 Rect, Action Action, bool Enabled, string Tip)> buttons = new();
    readonly Color ink = new("101b25"), panel = new("152530"), muted = new("93a6ad"), white = new("f2ecdc"), gold = Art.Gold;
    Font font = null!;
    Rect2 minimap;
    int shopPage;
    bool offHand;
    float W => GetViewportRect().Size.X;
    float H => GetViewportRect().Size.Y;
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        font = ThemeDB.FallbackFont;
    }
    void Text(string text, float x, float y, int size = 16, Color? color = null) =>
        DrawString(font, new(x, y), text, HorizontalAlignment.Left, -1, size, color ?? white);
    void Center(string text, float x, float y, int size = 16, Color? color = null) =>
        Text(text, x - font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X / 2, y, size, color);
    void Panel(Rect2 r, Color? fill = null)
    {
        DrawRect(r, fill ?? panel); DrawRect(r, new Color(gold, .38f), false, 1);
        DrawLine(r.Position + new Vector2(1, 0), r.Position + new Vector2(r.Size.X - 1, 0), gold, 2);
    }
    void Bar(Rect2 r, double fraction, Color color)
    {
        DrawRect(r, new Color("0a141b"));
        DrawRect(new(r.Position, new(r.Size.X * (float)Math.Clamp(fraction, 0, 1), r.Size.Y)), color);
    }
    void Button(Rect2 rect, string label, Action action, bool enabled = true, string tip = "")
    {
        var hover = rect.HasPoint(GetViewport().GetMousePosition());
        Panel(rect, enabled ? hover ? new Color("33433f") : new Color("22353b") : new Color("19252b"));
        Center(label, rect.GetCenter().X, rect.GetCenter().Y + 6, 16, enabled ? white : muted);
        buttons.Add((rect, action, enabled, tip));
    }
    public bool HandleClick(Vector2 p, MouseButton button)
    {
        if (button == MouseButton.Left)
            for (var i = buttons.Count - 1; i >= 0; i--)
                if (buttons[i].Rect.HasPoint(p)) { if (buttons[i].Enabled) buttons[i].Action(); return true; }
        if (Game.Started && !Game.ShopOpen && !Game.Paused && !Game.HelpOpen && minimap.HasPoint(p))
        {
            var map = (p - minimap.Position) / minimap.Size;
            var at = new V2(map.X * 56 - 28, map.Y * 34 - 17);
            if (button == MouseButton.Right) Game.World.OrderMove(Game.World.Player, at); else Game.PanTo(at);
            return true;
        }
        return Game.ShopOpen || Game.Paused || Game.HelpOpen || !Game.Started || p.Y > H - 158;
    }
    public override void _Draw()
    {
        if (font is null) return;
        buttons.Clear();
        if (!Game.Started) { MainMenu(); return; }
        var world = Game.World; var p = world.Player;
        HealthBars();
        Panel(new(0, 0, W, 54), new Color("101d27"));
        Text("YOKAI DOMINION", 24, 34, 21, gold);
        Center("DAWN  /  SHRINE OF THE VEIL  /  DUSK", W / 2, 23, 14, muted);
        Center($"{(int)world.Time / 60:00}:{(int)world.Time % 60:00}    •    WAVE {world.Wave}", W / 2, 43, 15);
        Text($"{p.Kills} / {p.Deaths}     {p.LastHits} LAST HITS     {(int)p.Gold} GOLD", W - 365, 33, 16, gold);
        minimap = new(W - 244, 74, 220, 137); DrawMap();
        Text("Destroy the opposing shrine", 24, 83, 17);
        Text("Right-click move / attack   •   F1 controls", 24, 108, 14, muted);
        if (Game.AttackMoveArmed) Text("ATTACK-MOVE: click a destination", 24, 135, 16, gold);
        if (world.Time < world.NoticeUntil) Center(world.Notice, W / 2, H - 190, 17, gold);
        if (!p.Alive) Center($"RESPAWNING IN {Math.Max(0, Math.Ceiling(p.RespawnAt - world.Time)):0}", W / 2, H / 2, 30, gold);
        BottomBar();
        if (Game.ShopOpen) Shop();
        if (Game.HelpOpen) Help();
        if (Game.Paused) PauseMenu();
        if (world.Winner.HasValue) EndMenu();
        var hovered = buttons.LastOrDefault(b => b.Rect.HasPoint(GetViewport().GetMousePosition()) && b.Tip.Length > 0);
        if (hovered.Tip is not null && hovered.Tip.Length > 0)
        {
            Panel(new(W / 2 - 350, H - 244, 700, 70)); Paragraph(hovered.Tip, W / 2 - 332, H - 220, 664, 15, muted);
        }
    }
    void HealthBars()
    {
        foreach (var u in Game.World.Units.Where(u => u.Alive))
        {
            var point = Art.World(u.Position, u.IsStructure ? 4.3f : u.Kind == UnitKind.Champion ? 2.65f : 1.65f);
            if (Game.Camera.IsPositionBehind(point)) continue;
            var p = Game.Camera.UnprojectPosition(point); var width = u.Kind == UnitKind.Minion ? 36 : 78;
            var color = u.Team == Team.Dawn ? Art.Teal : Art.Red;
            Bar(new(p.X - width / 2f, p.Y, width, 5), u.Health / u.MaxHealth, color);
            if (u.Kind == UnitKind.Champion) { Center(u.Name, p.X, p.Y - 8, 12); Bar(new(p.X - width / 2f, p.Y + 7, width, 3), u.Mana / u.MaxMana, new("719bd0")); }
            if (u.Bleeds.Count > 0) Center($"BLEED {u.Bleeds.Count}", p.X, p.Y + 23, 11, Art.Red);
        }
    }
    void DrawMap()
    {
        Panel(minimap, new Color("213b3d"));
        Vector2 Map(V2 p) => minimap.Position + new Vector2((float)(p.X + 28) / 56, (float)(p.Y + 17) / 34) * minimap.Size;
        foreach (var lane in new[] { -9, 0, 9 }) DrawLine(Map(new(-25, lane)), Map(new(25, lane)), new("657465"), 4);
        DrawLine(Map(new(0, -17)), Map(new(0, 17)), new("548d91"), 5);
        foreach (var u in Game.World.Units.Where(u => u.Alive))
        {
            var color = u.IsPlayer ? gold : u.Team == Team.Dawn ? Art.Teal : Art.Red;
            DrawCircle(Map(u.Position), u.IsStructure ? 4 : u.Kind == UnitKind.Champion ? 3.5f : 1.8f, color);
        }
        Text("No fog of war • prototype", minimap.Position.X + 4, minimap.End.Y + 18, 11, muted);
    }
    void BottomBar()
    {
        var p = Game.World.Player; var y = H - 154;
        Panel(new(0, y, W, 154), new Color("101c27"));
        DrawCircle(new(60, y + 61), 36, new("693c49")); DrawArc(new(60, y + 61), 37, 0, Mathf.Tau, 48, gold, 2, true);
        Center("V", 60, y + 72, 34, gold); Center($"LV {p.Level}", 60, y + 117, 14);
        Text("VAELIRA", 110, y + 30, 20, gold);
        Text(p.BloodScentActive ? "BLOOD SCENT  +10%" : "ONI MARTIAL ARTIST", 110, y + 52, 12, p.BloodScentActive ? Art.Red : muted);
        Bar(new(110, y + 66, 230, 15), p.Health / p.MaxHealth, Art.Teal);
        Center($"{Math.Ceiling(p.Health):0} / {p.MaxHealth:0}", 225, y + 78, 12, ink);
        Bar(new(110, y + 87, 230, 10), p.Mana / p.MaxMana, new("7096cd"));
        Text($"STR {p.Stats.Strength:0}    DEX {p.Stats.Dexterity:0}    INT {p.Stats.Intelligence:0}", 110, y + 119, 13);
        Text($"ATK {p.AttackDamage:0}    ARM {p.Armor:0}    RES {p.Resistance:0}", 110, y + 139, 12, muted);
        var start = W / 2 - 176;
        var names = new[] { "Twin Fang", "Reprise", "Leg Reaper", "Heavenfall" };
        for (var i = 0; i < 4; i++)
        {
            var index = i; var rect = new Rect2(start + i * 90, y + 22, 78, 76);
            var remaining = Math.Max(0, p.ReadyAt[i] - Game.World.Time);
            Panel(rect, remaining > 0 ? new Color("1a2935") : new Color("344845"));
            Center(new[] { "Q", "W", "E", "R" }[i], rect.GetCenter().X, rect.Position.Y + 32, 25, remaining > 0 ? muted : gold);
            Center(remaining > 0 ? remaining.ToString("0.0") : names[i], rect.GetCenter().X, rect.Position.Y + 59, remaining > 0 ? 20 : 11);
            for (var r = 0; r < p.Ranks[i]; r++) DrawRect(new(rect.Position.X + 9 + r * 12, rect.End.Y + 5, 8, 3), gold);
            buttons.Add((rect, () => Game.World.TryCast(p, (Ability)index, Game.Aim), true,
                names[i] + " • Hold key to repeat. Ctrl+key spends an ability point. " + new[] { "Fists: time Q before a hit, then recast for a free kick.", "Starts a new attack windup and refreshes Q.", "Trips nearby enemies. Enables R's bonus damage.", "Leap to cursor and slam. Bonus damage against knocked-down enemies." }[i]));
        }
        var phase = p.CurrentQWindow(Game.World.Time);
        var phaseName = p.FreeKickUntil > Game.World.Time ? "FREE KICK — Q NOW" : p.Loadout.FistCompatible ? phase switch { QWindow.Kick => "Q: KICK WINDOW", QWindow.Prepare => "Q: QUEUE DOUBLE HIT", _ => "Q: NEUTRAL PUNCH" } : "Q: MELEE KICK";
        Center(phaseName, W / 2, y + 132, 13, p.FreeKickUntil > Game.World.Time ? Art.Gold : muted);
        var sx = W - 380;
        Text($"{p.AttributePoints} ATTRIBUTE POINTS  /  {p.AbilityPoints} ABILITY POINTS", sx, y + 28, 12, gold);
        for (var i = 0; i < 3; i++) { var index = i; Button(new(sx + i * 106, y + 41, 96, 28), new[] { "1  +STR", "2  +DEX", "3  +INT" }[i], () => Game.World.AllocateAttribute(index), p.AttributePoints > 0); }
        Button(new(sx, y + 82, 152, 40), "B  SHRINE ARMORY", () => Game.ShopOpen = !Game.ShopOpen);
        Button(new(sx + 163, y + 82, 152, 40), "F1  FIELD GUIDE", () => Game.HelpOpen = !Game.HelpOpen);
        Bar(new(0, H - 4, W, 4), p.Experience / p.NextLevelExperience, gold);
    }
    void MainMenu()
    {
        DrawRect(new(0, 0, W, H), new Color(ink, .47f));
        Text("A C# MOBA IN DEVELOPMENT", 80, H * .24f, 15, gold);
        Text("YOKAI", 76, H * .24f + 88, 82);
        Text("DOMINION", 76, H * .24f + 172, 82);
        Text("SHRINE OF THE VEIL", 82, H * .24f + 216, 22, gold);
        Paragraph("Master the rhythm. Choose your weapons. Break the opposing shrine.", 82, H * .24f + 253, 530, 19, white);
        Button(new(82, H * .24f + 318, 300, 54), "ENTER THE BATTLEFIELD", Game.StartMatch);
        Text("ENTER to play  •  F11 fullscreen  •  M sound", 82, H * .24f + 404, 14, muted);
        Panel(new(W - 374, H - 214, 330, 166), new Color(panel, .95f));
        Text("FIRST PLAYABLE MILESTONE", W - 352, H - 182, 16, gold);
        Paragraph("Local three-lane match against a bot. C# simulation, original procedural arena, and articulated character proxies. Not final character art or an online release.", W - 352, H - 150, 288, 15, muted);
        Text("GODOT .NET  /  ORIGINAL DEVELOPMENT ART", 80, H - 32, 12, muted);
    }
    void Shop()
    {
        var p = Game.World.Player; var x = W / 2 - 450; var y = Math.Max(65, H / 2 - 330);
        Panel(new(x, y, 900, 650), new Color("142630"));
        Text("SHRINE ARMORY", x + 24, y + 38, 26, gold);
        Text($"{(int)p.Gold} GOLD   •   {(Game.World.AtBase(p) ? "IN SHRINE RANGE" : "RETURN TO SHRINE TO BUY")}", x + 24, y + 65, 14, muted);
        Button(new(x + 730, y + 18, 145, 34), "CLOSE [B]", () => Game.ShopOpen = false);
        Button(new(x + 24, y + 83, 180, 34), "WEAPONS & RELICS", () => shopPage = 0);
        Button(new(x + 214, y + 83, 180, 34), "ARMOR SETS", () => shopPage = 1);
        Button(new(x + 404, y + 83, 180, 34), $"STORAGE ({p.Storage.Count})", () => shopPage = 2);
        Button(new(x + 594, y + 83, 280, 34), offHand ? "HAND TARGET: OFF HAND" : "HAND TARGET: MAIN HAND", () => offHand = !offHand);
        var items = shopPage == 2 ? p.Storage.ToArray() : Items.Catalog.Where(i => shopPage == 1 ? Items.ArmorSlots.Contains(i.Slot) : !Items.ArmorSlots.Contains(i.Slot)).ToArray();
        for (var i = 0; i < Math.Min(items.Length, 10); i++)
        {
            var item = items[i]; var index = i; var bx = x + 24 + (i % 2) * 430; var by = y + 137 + (i / 2) * 79;
            Panel(new(bx, by, 420, 70), new Color("20333a"));
            Text(item.Name, bx + 12, by + 23, 16);
            Text($"{item.Slot}   {(shopPage == 2 ? "STORED" : item.Cost + " GOLD")}", bx + 12, by + 49, 12, muted);
            Button(new(bx + 304, by + 14, 104, 40), shopPage == 2 ? "EQUIP" : "BUY", () => { if (shopPage == 2) Game.World.EquipStored(index, offHand); else Game.World.Buy(item, offHand); },
                Game.World.AtBase(p) && p.Alive && (shopPage == 2 || p.Gold >= item.Cost), item.Description);
        }
        if (shopPage == 2 && items.Length == 0) Text("Replaced equipment is kept here, not destroyed.", x + 24, y + 171, 17, muted);
        Text("EQUIPPED", x + 24, y + 558, 13, gold);
        var slotIndex = 0;
        foreach (var slot in Enum.GetValues<EquipSlot>())
        {
            var col = slotIndex % 4; var row = slotIndex / 4;
            var name = p.Loadout[slot]?.Name ?? "—";
            if (name.Length > 18) name = name[..17] + "…";
            Text($"{slot}: {name}", x + 24 + col * 215, y + 583 + row * 20, 11, muted); slotIndex++;
        }
    }
    void Help()
    {
        DrawRect(new(0, 0, W, H), new Color(ink, .6f)); var x = W / 2 - 360; var y = H / 2 - 260;
        Panel(new(x, y, 720, 520)); Text("FIELD GUIDE", x + 30, y + 45, 28, gold);
        var lines = new[] {
            "RIGHT-CLICK  Move to ground or attack an enemy. A then click: attack-move. S: stop.",
            "Q  Fists: early kick / middle punch / late double-hit setup. After an empowered hit, Q recasts a free kick.",
            "W  Start a new attack windup and refresh Q. Hold ability keys to repeat on readiness.",
            "E  Trip nearby enemies. R  Leap to the cursor and slam; knocked-down enemies take extra damage.",
            "B  Shrine armory. Buy or swap at your base. Weapons change basic attacks; non-fist Q stays melee.",
            "1 / 2 / 3  Allocate Strength / Dexterity / Intelligence. CTRL+Q/W/E/R upgrades an ability.",
            "ARROWS  Pan camera. SPACE  Follow your hero. WHEEL  Zoom. Minimap: left pan, right move.",
            "ESC  Pause. M  Mute. F10  Quality toggle. F11  Fullscreen. Destroy the enemy shrine to win."
        };
        var yy = y + 83;
        foreach (var line in lines) { Paragraph(line, x + 30, yy, 660, 16, white); yy += 48; }
        Button(new(x + 230, y + 459, 260, 40), "RETURN TO BATTLE [F1]", () => Game.HelpOpen = false);
    }
    void PauseMenu()
    {
        DrawRect(new(0, 0, W, H), new Color(ink, .7f)); var x = W / 2 - 190; var y = H / 2 - 165;
        Panel(new(x, y, 380, 330)); Center("BATTLE PAUSED", W / 2, y + 48, 28, gold);
        Button(new(x + 35, y + 78, 310, 44), "RESUME", Game.TogglePause);
        Button(new(x + 35, y + 134, 310, 44), Game.Muted ? "SOUND: MUTED" : "SOUND: ON", Game.ToggleMute);
        Button(new(x + 35, y + 190, 310, 44), Game.HighQuality ? "QUALITY: HIGH" : "QUALITY: PERFORMANCE", Game.ToggleQuality);
        Button(new(x + 35, y + 246, 310, 44), "RESTART MATCH", Game.StartMatch);
    }
    void EndMenu()
    {
        DrawRect(new(0, 0, W, H), new Color(ink, .7f)); var x = W / 2 - 240; var y = H / 2 - 140;
        Panel(new(x, y, 480, 280));
        Center(Game.World.Winner == Team.Dawn ? "VICTORY" : "DEFEAT", W / 2, y + 70, 42, gold);
        Center("The shrine's dominion has changed.", W / 2, y + 113, 18);
        Center($"{Game.World.Player.Kills} kills  •  {Game.World.Player.LastHits} last hits  •  Level {Game.World.Player.Level}", W / 2, y + 153, 16, muted);
        Button(new(x + 80, y + 196, 320, 48), "NEW MATCH [ENTER]", Game.StartMatch);
    }
    void Paragraph(string text, float x, float y, float width, int size, Color color)
    {
        var line = "";
        foreach (var word in text.Split(' '))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && font.GetStringSize(candidate, HorizontalAlignment.Left, -1, size).X > width)
            { Text(line, x, y, size, color); y += size + 6; line = word; }
            else line = candidate;
        }
        if (line.Length > 0) Text(line, x, y, size, color);
    }
}
