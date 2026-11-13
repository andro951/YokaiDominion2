using Godot;
using Yokai.Core;

namespace Yokai.Client;

public partial class Game : Node3D
{
    public GameWorld World { get; private set; } = new();
    public Camera3D Camera { get; private set; } = null!;
    public bool Started { get; private set; }
    public bool Paused { get; private set; }
    public bool ShopOpen { get; set; }
    public bool HelpOpen { get; set; }
    public bool Muted { get; private set; }
    public bool HighQuality { get; private set; } = true;
    public bool CameraLocked { get; private set; } = true;
    public int HoveredId { get; private set; }
    public bool AttackMoveArmed { get; private set; }
    readonly Dictionary<int, UnitView> views = new();
    Node3D actors = null!;
    Arena arena = null!;
    Effects effects = null!;
    SoundBank sound = null!;
    Hud hud = null!;
    Vector3 focus;
    float zoom = 28;
    bool smoke, captured;
    int frame;
    string capturePath = "";
    static readonly Key[] AbilityKeys = [Key.Q, Key.W, Key.E, Key.R];
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var args = OS.GetCmdlineUserArgs(); smoke = args.Contains("--smoke");
        capturePath = args.FirstOrDefault(s => s.StartsWith("--capture=", StringComparison.Ordinal))?[10..] ?? "";
        Engine.MaxFps = 120;
        LoadSettings();
        arena = new Arena(); AddChild(arena); arena.Build(World);
        actors = new Node3D { Name = "Actors" }; AddChild(actors);
        effects = new Effects(); AddChild(effects);
        sound = new SoundBank(); AddChild(sound);
        Camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = zoom, Near = .1f, Far = 150, Current = true };
        AddChild(Camera);
        var layer = new CanvasLayer(); AddChild(layer);
        hud = new Hud { Game = this }; layer.AddChild(hud);
        ApplyQuality(); UpdateCamera(1); SyncViews();
        GD.Print("YOKAI_BOOT_OK Godot C# client initialized.");
    }
    public void StartMatch()
    {
        if (Started)
        {
            World = new GameWorld();
            foreach (var view in views.Values) view.QueueFree(); views.Clear(); effects.Clear();
        }
        Started = true; Paused = false; ShopOpen = false; HelpOpen = false; CameraLocked = true;
        focus = Art.World(World.Player.Position); SyncViews();
    }
    public void TogglePause() { if (Started) Paused = !Paused; }
    public void ToggleMute() { Muted = !Muted; SaveSettings(); }
    public void ToggleQuality() { HighQuality = !HighQuality; ApplyQuality(); SaveSettings(); }
    public void PanTo(V2 at) { CameraLocked = false; focus = Art.World(at); }
    public V2 Aim => Ground(GetViewport().GetMousePosition());
    public V2 Ground(Vector2 screen)
    {
        var hit = new Plane(Vector3.Up, 0).IntersectsRay(Camera.ProjectRayOrigin(screen), Camera.ProjectRayNormal(screen));
        return hit.HasValue ? new(hit.Value.X, hit.Value.Z) : World.Player.Position;
    }
    public override void _Process(double delta)
    {
        frame++;
        if (smoke && frame == 25)
        {
            StartMatch(); World.OrderMove(World.Player, new(-4, 0));
        }
        if (Started && !Paused && !HelpOpen)
        {
            if (!ShopOpen && !Input.IsKeyPressed(Key.Ctrl))
                for (var i = 0; i < 4; i++) if (Input.IsPhysicalKeyPressed(AbilityKeys[i])) World.TryCast(World.Player, (Ability)i, Aim);
            World.Advance(delta);
        }
        UpdateCamera(delta); HoveredId = PickUnit(GetViewport().GetMousePosition());
        SyncViews();
        foreach (var e in World.DrainEvents())
        {
            if (views.TryGetValue(e.SourceId, out var source)) source.Event(e, World.Time);
            if (e.TargetId != e.SourceId && views.TryGetValue(e.TargetId, out var target)) target.Event(e, World.Time);
            effects.Show(e, World);
            if (!Muted && V2.Distance(World.Player.Position, e.Position) < 15) sound.Play(e.Kind);
        }
        effects.Advance((float)delta);
        hud.QueueRedraw();
        if (smoke && frame == 18 && capturePath.Length > 0 && DisplayServer.GetName() != "headless")
            Capture(capturePath.Replace(".png", "-menu.png", StringComparison.Ordinal));
        if (smoke && frame >= 360 && !captured)
        {
            captured = true;
            if (capturePath.Length > 0 && DisplayServer.GetName() != "headless") Capture(capturePath);
            GD.Print($"SMOKE_OK units={World.Units.Count} simulation_seconds={World.Time:F3} player_alive={World.Player.Alive}");
            GetTree().Quit();
        }
    }
    void Capture(string path)
    {
        var image = GetViewport().GetTexture().GetImage();
        var error = image.SavePng(path);
        if (error != Error.Ok) GD.PushError("Screenshot failed: " + error); else GD.Print("CAPTURE_OK " + path);
    }
    void SyncViews()
    {
        var ids = World.Units.Select(u => u.Id).ToHashSet();
        foreach (var id in views.Keys.Where(id => !ids.Contains(id)).ToArray()) { views[id].QueueFree(); views.Remove(id); }
        foreach (var u in World.Units)
        {
            if (!views.TryGetValue(u.Id, out var view)) { view = new UnitView { Model = u }; actors.AddChild(view); views[u.Id] = view; }
            view.Animate(World.Time, World.RenderAlpha, u.Id == World.Player.TargetId || u.Id == HoveredId);
        }
    }
    void UpdateCamera(double delta)
    {
        var pan = Vector3.Zero;
        if (Started && !ShopOpen && !Paused)
        {
            if (Input.IsKeyPressed(Key.Left)) pan.X--;
            if (Input.IsKeyPressed(Key.Right)) pan.X++;
            if (Input.IsKeyPressed(Key.Up)) pan.Z--;
            if (Input.IsKeyPressed(Key.Down)) pan.Z++;
            if (pan.LengthSquared() > 0) { CameraLocked = false; focus += pan.Normalized() * (float)delta * 18; }
            if (Input.IsKeyPressed(Key.Space)) CameraLocked = true;
        }
        var desired = Started && CameraLocked ? Art.World(World.Player.Position) : focus;
        desired.X = Mathf.Clamp(desired.X, -13, 13); desired.Z = Mathf.Clamp(desired.Z, -7, 7);
        focus = focus.Lerp(desired, 1 - Mathf.Exp(-(float)delta * 7));
        Camera.Size = Mathf.Lerp(Camera.Size, zoom, 1 - Mathf.Exp(-(float)delta * 10));
        Camera.Position = focus + new Vector3(0, 27, 20);
        Camera.LookAt(focus, Vector3.Up);
    }
    int PickUnit(Vector2 screen)
    {
        var closest = 36f; var id = 0;
        foreach (var u in World.Units.Where(u => u.Alive && u.Team != World.Player.Team))
        {
            var center = Art.World(u.Position, u.IsStructure ? 1.5f : .9f);
            if (Camera.IsPositionBehind(center)) continue;
            var distance = Camera.UnprojectPosition(center).DistanceTo(screen);
            if (distance < closest) { closest = distance; id = u.Id; }
        }
        return id;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (hud.HandleClick(mouse.Position, mouse.ButtonIndex)) return;
            if (!Started || Paused || ShopOpen || HelpOpen || World.Winner.HasValue) return;
            if (mouse.ButtonIndex == MouseButton.WheelUp) zoom = Mathf.Clamp(zoom - 2, 18, 42);
            if (mouse.ButtonIndex == MouseButton.WheelDown) zoom = Mathf.Clamp(zoom + 2, 18, 42);
            if (mouse.ButtonIndex == MouseButton.Right || mouse.ButtonIndex == MouseButton.Left && AttackMoveArmed)
            {
                var target = PickUnit(mouse.Position);
                if (target != 0) World.OrderAttack(World.Player, target);
                else { var destination = Ground(mouse.Position); World.OrderMove(World.Player, destination, AttackMoveArmed); effects.MoveMarker(destination, AttackMoveArmed); }
                AttackMoveArmed = false;
            }
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.Enter && (!Started || World.Winner.HasValue)) { StartMatch(); return; }
        if (key.Keycode == Key.Escape)
        {
            if (ShopOpen) ShopOpen = false; else if (HelpOpen) HelpOpen = false; else TogglePause(); return;
        }
        if (key.Keycode == Key.F1) HelpOpen = !HelpOpen;
        if (key.Keycode == Key.F10) ToggleQuality();
        if (key.Keycode == Key.M) ToggleMute();
        if (key.Keycode == Key.F11)
            DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
        if (!Started || Paused) return;
        if (key.Keycode == Key.B) ShopOpen = !ShopOpen;
        if (key.Keycode == Key.A) AttackMoveArmed = true;
        if (key.Keycode == Key.S && !ShopOpen) World.Stop(World.Player);
        if (key.Keycode is >= Key.Key1 and <= Key.Key3) World.AllocateAttribute((int)(key.Keycode - Key.Key1));
        if (key.CtrlPressed)
            for (var i = 0; i < AbilityKeys.Length; i++) if (key.Keycode == AbilityKeys[i]) World.RankUp((Ability)i);
    }
    void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") != Error.Ok) return;
        Muted = config.GetValue("audio", "muted", false).AsBool();
        HighQuality = config.GetValue("video", "high_quality", true).AsBool();
    }
    void SaveSettings()
    {
        var config = new ConfigFile(); config.SetValue("audio", "muted", Muted); config.SetValue("video", "high_quality", HighQuality);
        config.Save("user://settings.cfg");
    }
    void ApplyQuality()
    {
        GetViewport().Msaa3D = (Viewport.Msaa)(HighQuality ? 2 : 0);
        foreach (var worldEnvironment in arena.GetChildren().OfType<WorldEnvironment>())
        {
            worldEnvironment.Environment.SsaoEnabled = HighQuality;
            worldEnvironment.Environment.GlowEnabled = HighQuality;
        }
    }
}
