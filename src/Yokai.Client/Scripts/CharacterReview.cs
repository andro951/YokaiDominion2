using Godot;
using Yokai.Core;
namespace Yokai.Client;

/// <summary>Actual runtime skin/pose review, separate from the game's distant tactical camera.</summary>
public partial class CharacterReview : Node3D
{
    readonly List<CharacterAvatar> avatars = new();
    readonly Unit unit = new() { Kind = UnitKind.Champion, Team = Team.Dawn };
    int frames;
    string output = "";
    public override void _Ready()
    {
        output = OS.GetCmdlineUserArgs().FirstOrDefault(s => s.StartsWith("--capture=", StringComparison.Ordinal))?[10..] ?? "";
        Art.Box(this, new(0, -.08f, 0), new(8, .12f, 5), Art.Mat("25343d"));
        for (var i = 0; i < 3; i++)
        {
            var avatar = CharacterAvatar.Create() ?? throw new InvalidOperationException("Rigged asset required by character review.");
            avatar.Position = new((i - 1) * 1.8f, 0, 0); avatar.Rotation = new(0, -.25f, 0);
            AddChild(avatar); avatars.Add(avatar);
            var label = new Label3D { Text = new[] { "IDLE", "WALK", "KICK" }[i], Position = new((i - 1) * 1.8f, 2.5f, 0),
                FontSize = 40, PixelSize = .006f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Modulate = Art.Gold };
            AddChild(label);
        }
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 6.1f, Current = true, Position = new(1.7f, 2.25f, 7) };
        AddChild(camera); camera.LookAt(new(0, 1.15f, 0));
        AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new("14232d"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new("d6e6f0"), AmbientLightEnergy = .8f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled } });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-40, -30, 0), LightEnergy = 1.3f, LightColor = new("ffecd9"), ShadowEnabled = true });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-25, 140, 0), LightEnergy = .7f, LightColor = new("9cbcd5") });
    }
    public override void _Process(double delta)
    {
        frames++;
        // Fixed representative poses make image comparisons meaningful across revisions.
        avatars[0].Animate(unit, .14, false, 0, 0);
        avatars[1].Animate(unit, .14, true, 0, 0);
        avatars[2].Animate(unit, .14, false, 0, .31);
        if (frames != 20) return;
        if (output.Length > 0 && DisplayServer.GetName() != "headless")
        {
            var result = GetViewport().GetTexture().GetImage().SavePng(output);
            if (result != Error.Ok) throw new InvalidOperationException("Pose capture failed: " + result);
        }
        GD.Print("CHARACTER_REVIEW_OK bones=" + avatars[0].BoneCount); GetTree().Quit();
    }
}
