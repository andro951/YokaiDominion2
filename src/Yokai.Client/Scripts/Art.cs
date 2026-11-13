using Godot;
using Yokai.Core;

namespace Yokai.Client;

/// <summary>Original source-generated development art. Replaceable independently from simulation.</summary>
public static class Art
{
    static readonly Dictionary<string, StandardMaterial3D> materials = new();
    public static readonly Color Gold = new("d5b875"), Teal = new("66d9cf"), Red = new("ed766b");
    public static Vector3 World(V2 p, float height = 0) => new((float)p.X, height, (float)p.Y);
    public static StandardMaterial3D Mat(string hex, float metal = 0, float emission = 0)
    {
        var key = $"{hex}:{metal}:{emission}";
        if (materials.TryGetValue(key, out var result)) return result;
        var c = new Color(hex);
        result = new StandardMaterial3D { AlbedoColor = c, Metallic = metal, Roughness = .75f,
            EmissionEnabled = emission > 0, Emission = c, EmissionEnergyMultiplier = emission };
        materials[key] = result; return result;
    }
    public static MeshInstance3D Mesh(Node3D parent, Mesh mesh, Vector3 position, Material material, Vector3? scale = null)
    {
        var node = new MeshInstance3D { Mesh = mesh, Position = position, MaterialOverride = material };
        if (scale.HasValue) node.Scale = scale.Value;
        parent.AddChild(node); return node;
    }
    public static MeshInstance3D Box(Node3D p, Vector3 at, Vector3 size, Material m) => Mesh(p, new BoxMesh { Size = size }, at, m);
    public static MeshInstance3D Sphere(Node3D p, Vector3 at, Vector3 size, Material m, int segments = 16) =>
        Mesh(p, new SphereMesh { Radius = .5f, Height = 1, RadialSegments = segments, Rings = 8 }, at, m, size);
    public static MeshInstance3D Cylinder(Node3D p, Vector3 at, float bottom, float top, float height, Material m, int segments = 16) =>
        Mesh(p, new CylinderMesh { BottomRadius = bottom, TopRadius = top, Height = height, RadialSegments = segments }, at, m);
    public static MeshInstance3D Ring(Node3D p, Vector3 at, float radius, float width, Color color)
    {
        var st = new SurfaceTool(); st.Begin(Godot.Mesh.PrimitiveType.Triangles);
        for (var i = 0; i < 64; i++)
        {
            var a = i * Mathf.Tau / 64; var b = (i + 1) * Mathf.Tau / 64;
            var ai = new Vector3(Mathf.Cos(a) * (radius - width), 0, Mathf.Sin(a) * (radius - width));
            var ao = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            var bi = new Vector3(Mathf.Cos(b) * (radius - width), 0, Mathf.Sin(b) * (radius - width));
            var bo = new Vector3(Mathf.Cos(b) * radius, 0, Mathf.Sin(b) * radius);
            foreach (var vertex in new[] { ai, bo, ao, ai, bi, bo }) { st.SetNormal(Vector3.Up); st.AddVertex(vertex); }
        }
        var material = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = .7f };
        return Mesh(p, st.Commit(), at, material);
    }
    public static void Beam(Node3D parent, Vector3 from, Vector3 to, float radius, Material material)
    {
        var delta = to - from;
        if (delta.LengthSquared() < .0001f) return;
        var n = Cylinder(parent, (from + to) * .5f, radius, radius * .8f, delta.Length(), material);
        n.Quaternion = new Quaternion(Vector3.Up, delta.Normalized());
    }
}

public partial class Arena : Node3D
{
    readonly Random random = new(7331);
    float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);
    public void Build(GameWorld world)
    {
        Name = "ShrineOfTheVeil";
        var ground = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/ground.gdshader") };
        Art.Mesh(this, new PlaneMesh { Size = new(58, 36), SubdivideWidth = 12, SubdivideDepth = 12 }, new(0, -.04f, 0), ground);
        Art.Box(this, new(0, -.8f, 0), new(59, 1.5f, 37), Art.Mat("1b262b"));
        var water = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/water.gdshader") };
        Art.Mesh(this, new PlaneMesh { Size = new(3.9f, 35), SubdivideWidth = 8, SubdivideDepth = 48 }, new(0, .015f, 0), water);
        // Three stone lanes with individually varied paving and narrow brass shrine lines.
        foreach (var lane in new[] { -9f, 0, 9f })
        {
            Art.Box(this, new(0, .015f, lane), new(51, .07f, 2.9f), Art.Mat("596455"));
            for (var x = -25; x < 26; x++) for (var z = -1; z <= 1; z++)
            {
                var tint = (x + z) % 3 == 0 ? "7a8070" : (x + z) % 2 == 0 ? "697465" : "737969";
                Art.Box(this, new(x + Next(-.07f, .07f), .065f, lane + z * .87f), new(.92f, .08f, .79f), Art.Mat(tint));
            }
            Bridge(lane);
            for (var x = -21; x <= 21; x += 7)
                foreach (var side in new[] { -1, 1 }) Lantern(new(x, 0, lane + side * 2.25f));
        }
        foreach (var sign in new[] { -1, 1 })
        {
            Art.Cylinder(this, new(sign * 23, .03f, 0), 4.2f, 4.2f, .15f, Art.Mat("687269"), 48);
            Art.Ring(this, new(sign * 23, .13f, 0), 3.9f, .045f, sign < 0 ? Art.Teal : Art.Red);
            Torii(new(sign * 20, 0, 0), Mathf.Pi / 2);
            for (var i = -1; i <= 1; i++)
                Art.Box(this, new(sign * 21, .025f, i * 4), new(4, .06f, 4), Art.Mat("5b675c"));
        }
        foreach (var rock in world.Terrain)
        {
            var pos = Art.World(rock.Center); var r = (float)rock.Radius;
            Art.Sphere(this, pos + new Vector3(0, .3f, 0), new(r * 1.8f, .8f, r * 1.5f), Art.Mat("3d5150"), 10);
            for (var i = 0; i < 3; i++)
                Art.Sphere(this, pos + new Vector3(Next(-r * .4f, r * .4f), Next(.4f, .8f), Next(-r * .4f, r * .4f)),
                    new(r, Next(.7f, 1.3f), r * .85f), Art.Mat(i % 2 == 0 ? "52635c" : "42564e"), 8);
        }
        for (var x = -26; x <= 26; x += 5)
        {
            Tree(new(x + Next(-1, 1), 0, -15 + Next(-.6f, .6f)), x % 2 == 0);
            Tree(new(x + Next(-1, 1), 0, 15 + Next(-.6f, .6f)), x % 3 == 0);
        }
        for (var i = 0; i < 18; i++)
        {
            var p = new Vector3(Next(-26, 26), 0, Next(-15, 15));
            if (Math.Abs(p.X) < 4 || new[] { -9, 0, 9 }.Any(l => Math.Abs(p.Z - l) < 3.4)) continue;
            Bamboo(p);
        }
        Grass();
        var env = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new("14252e"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new("93b4bd"), AmbientLightEnergy = .72f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic, ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            FogEnabled = true, FogLightColor = new("718c8c"), FogDensity = .003f,
            GlowEnabled = true, GlowIntensity = .55f, SsaoEnabled = true, SsaoRadius = 1.4f, SsaoIntensity = 1.2f
        };
        AddChild(new WorldEnvironment { Environment = env });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-58, -32, 0), LightColor = new("ffe2b1"),
            LightEnergy = 1.4f, ShadowEnabled = true, DirectionalShadowMaxDistance = 85 });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-28, 145, 0), LightColor = new("80c6dc"), LightEnergy = .45f });
    }
    void Bridge(float lane)
    {
        var wood = Art.Mat("514e43"); var trim = Art.Mat("a88655", .25f);
        for (var x = -3; x <= 3; x++) Art.Box(this, new(x * .48f, .14f, lane), new(.43f, .2f, 3.3f), wood);
        foreach (var side in new[] { -1, 1 })
        {
            Art.Box(this, new(0, .63f, lane + side * 1.85f), new(4.4f, .13f, .12f), trim);
            for (var i = -2; i <= 2; i++) Art.Box(this, new(i, .38f, lane + side * 1.85f), new(.12f, .75f, .12f), wood);
        }
    }
    void Lantern(Vector3 p)
    {
        var stone = Art.Mat("4d5a55");
        Art.Cylinder(this, p + new Vector3(0, .12f, 0), .32f, .24f, .24f, stone, 8);
        Art.Cylinder(this, p + new Vector3(0, .55f, 0), .12f, .1f, .8f, stone, 8);
        Art.Box(this, p + new Vector3(0, 1.04f, 0), new(.34f, .36f, .34f), Art.Mat("ffd892", 0, 1.6f));
        Art.Cylinder(this, p + new Vector3(0, 1.3f, 0), .4f, .06f, .22f, Art.Mat("313b3a"), 4).RotationDegrees = new(0, 45, 0);
    }
    void Torii(Vector3 p, float rotation)
    {
        var root = new Node3D { Position = p, Rotation = new(0, rotation, 0) }; AddChild(root);
        var red = Art.Mat("9e453e"); var dark = Art.Mat("293e42");
        foreach (var x in new[] { -1.9f, 1.9f })
        {
            Art.Cylinder(root, new(x, 1.75f, 0), .2f, .16f, 3.5f, red);
            Art.Cylinder(root, new(x, .3f, 0), .23f, .23f, .6f, dark);
        }
        Art.Box(root, new(0, 2.8f, 0), new(4.8f, .23f, .28f), red);
        Art.Box(root, new(0, 3.65f, 0), new(5.3f, .22f, .45f), dark);
        foreach (var sign in new[] { -1, 1 })
            Art.Box(root, new(sign * 2.5f, 3.76f, 0), new(1.05f, .2f, .45f), dark).RotationDegrees = new(0, 0, sign * 12);
        Art.Box(root, new(0, 3.23f, 0), new(.4f, .65f, .18f), Art.Mat("c7a662", .3f));
    }
    void Tree(Vector3 p, bool pink)
    {
        var root = new Node3D { Position = p }; AddChild(root);
        var bark = Art.Mat("54483f");
        Art.Cylinder(root, new(0, 1.35f, 0), .24f, .13f, 2.7f, bark, 10);
        for (var i = 0; i < 5; i++)
        {
            var angle = i * Mathf.Tau / 5;
            var end = new Vector3(Mathf.Cos(angle) * 1.05f, Next(2.5f, 3.4f), Mathf.Sin(angle) * 1.05f);
            Art.Beam(root, new(0, 1.6f, 0), end, .10f, bark);
            var color = pink ? (i % 2 == 0 ? "d990a8" : "b66f8d") : (i % 2 == 0 ? "527b63" : "365d50");
            Art.Sphere(root, end + new Vector3(0, .1f, 0), new(2.2f, 1.15f, 1.9f), Art.Mat(color), 10);
        }
    }
    void Bamboo(Vector3 p)
    {
        for (var i = 0; i < 5; i++)
        {
            var offset = new Vector3(Next(-.4f, .4f), 0, Next(-.4f, .4f)); var h = Next(1.8f, 3.4f);
            Art.Cylinder(this, p + offset + new Vector3(0, h * .5f, 0), .055f, .045f, h, Art.Mat("4c7455"), 6);
            for (var j = 0; j < 2; j++)
                Art.Sphere(this, p + offset + new Vector3(Next(-.3f, .3f), h - j * .5f, 0), new(.8f, .12f, .28f), Art.Mat("5f8862"), 8);
        }
    }
    void Grass()
    {
        var transforms = new List<Transform3D>();
        for (var i = 0; i < 2200; i++)
        {
            var x = Next(-27, 27); var z = Next(-16, 16);
            if (Math.Abs(x) < 3 || new[] { -9, 0, 9 }.Any(l => Math.Abs(z - l) < 1.8) || Math.Abs(x) > 19 && Math.Abs(z) < 4.6) continue;
            var basis = Basis.Identity.Rotated(Vector3.Up, Next(0, Mathf.Tau)).Scaled(new(.08f, Next(.14f, .38f), .07f));
            transforms.Add(new(basis, new(x, .09f, z)));
        }
        var mesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new PrismMesh { Size = Vector3.One }, InstanceCount = transforms.Count };
        for (var i = 0; i < transforms.Count; i++) mesh.SetInstanceTransform(i, transforms[i]);
        AddChild(new MultiMeshInstance3D { Multimesh = mesh, MaterialOverride = Art.Mat("608263"),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }
}
