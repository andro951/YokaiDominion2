using Godot;
using Yokai.Core;
namespace Yokai.Client;

public partial class Effects : Node3D
{
    sealed class Effect
    {
        public Node3D Node = null!;
        public float Remaining, Total;
        public Vector3 Velocity;
        public bool Expand;
    }
    readonly List<Effect> active = new();
    readonly Random random = new(442);
    public void Clear() { foreach (var effect in active) effect.Node.QueueFree(); active.Clear(); }
    void Add(Node3D node, float seconds, Vector3 velocity = default, bool expand = false) =>
        active.Add(new() { Node = node, Remaining = seconds, Total = seconds, Velocity = velocity, Expand = expand });
    public void MoveMarker(V2 at, bool attack)
    { Add(Art.Ring(this, Art.World(at, .16f), .7f, .06f, attack ? Art.Red : Art.Teal), .65f, expand: true); }
    public void Show(GameEvent e, GameWorld world)
    {
        var p = Art.World(e.Position, .15f);
        var color = world.Find(e.SourceId)?.Team == Team.Dawn ? Art.Teal : Art.Red;
        switch (e.Kind)
        {
            case EventKind.Damage:
                if (e.Amount >= 3)
                {
                    var label = new Label3D { Text = Math.Ceiling(e.Amount).ToString("0"), Position = p + Vector3.Up * 2,
                        FontSize = 52, PixelSize = .007f, OutlineSize = 10,
                        Modulate = e.Damage == DamageKind.True ? new Color("f4d884") : new Color("fff1d1"),
                        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = true };
                    AddChild(label); Add(label, .8f, Vector3.Up * 1.1f);
                }
                break;
            case EventKind.Punch:
                if (world.Find(e.SourceId)?.IsRanged == true)
                {
                    var root = new Node3D(); AddChild(root);
                    Art.Beam(root, Art.World(e.Origin, 1.2f), p + Vector3.Up, .035f, Art.Mat("f3d891", 0, 2)); Add(root, .13f);
                }
                else Burst(p + Vector3.Up, color, 5, .7f);
                break;
            case EventKind.Kick:
                Burst(p + Vector3.Up, Art.Gold, 9, 1.2f); break;
            case EventKind.Sweep:
                Add(Art.Ring(this, p, 2.7f, .22f, color), .35f, expand: true); break;
            case EventKind.Slam:
                Add(Art.Ring(this, p, 3.5f, .3f, Art.Gold), .7f, expand: true);
                Burst(p + Vector3.Up * .3f, Art.Gold, 22, 3); break;
            case EventKind.Death:
                Burst(p + Vector3.Up, color, 16, 2); break;
            case EventKind.LevelUp:
                Add(Art.Ring(this, p, 1, .1f, Art.Gold), 1.1f, Vector3.Up * 2, true); break;
        }
    }
    void Burst(Vector3 at, Color color, int count, float speed)
    {
        var material = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 1 };
        for (var i = 0; i < count; i++)
        {
            var node = Art.Sphere(this, at, Vector3.One * .085f, material, 6);
            var direction = new Vector3((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() + .3f, (float)random.NextDouble() * 2 - 1).Normalized();
            Add(node, .3f + (float)random.NextDouble() * .3f, direction * speed);
        }
    }
    public void Advance(float dt)
    {
        for (var i = active.Count - 1; i >= 0; i--)
        {
            var effect = active[i]; effect.Remaining -= dt;
            if (effect.Remaining <= 0) { effect.Node.QueueFree(); active.RemoveAt(i); continue; }
            effect.Node.Position += effect.Velocity * dt;
            if (effect.Expand) effect.Node.Scale = Vector3.One * (1 + (1 - effect.Remaining / effect.Total) * .7f);
            if (effect.Node is Label3D text) text.Modulate = new(text.Modulate, Mathf.Min(1, effect.Remaining * 4));
            if (effect.Node is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat && mat.Transparency != BaseMaterial3D.TransparencyEnum.Disabled)
                mat.AlbedoColor = new(mat.AlbedoColor, effect.Remaining / effect.Total);
        }
    }
}

public partial class SoundBank : Node
{
    readonly Dictionary<EventKind, AudioStreamWav> sounds = new();
    readonly List<AudioStreamPlayer> voices = new();
    int next;
    public override void _Ready()
    {
        for (var i = 0; i < 10; i++) { var voice = new AudioStreamPlayer { VolumeDb = -21 }; AddChild(voice); voices.Add(voice); }
        sounds[EventKind.Punch] = Tone(130, .11, true);
        sounds[EventKind.Kick] = Tone(95, .18, true);
        sounds[EventKind.Sweep] = Tone(260, .22, true);
        sounds[EventKind.Slam] = Tone(65, .45, true);
        sounds[EventKind.Purchase] = Tone(660, .16, false);
        sounds[EventKind.LevelUp] = Tone(880, .4, false);
    }
    public void Play(EventKind kind)
    {
        if (!sounds.TryGetValue(kind, out var sample) || voices.Count == 0) return;
        var voice = voices[next++ % voices.Count]; voice.Stream = sample; voice.Play();
    }
    static AudioStreamWav Tone(double frequency, double duration, bool noise)
    {
        const int rate = 22050; var count = (int)(rate * duration); var bytes = new byte[count * 2]; var random = new Random(79);
        for (var i = 0; i < count; i++)
        {
            var t = (double)i / rate; var envelope = Math.Min(1, t * 500) * Math.Pow(1 - t / duration, 2);
            var value = Math.Sin(Math.Tau * frequency * t * (noise ? 1 - .4 * t / duration : 1));
            if (noise) value = value * .55 + (random.NextDouble() * 2 - 1) * .45;
            var sample = (short)(value * envelope * 18000);
            bytes[i * 2] = (byte)(sample & 255); bytes[i * 2 + 1] = (byte)((sample >> 8) & 255);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate, Stereo = false, Data = bytes };
    }
}
