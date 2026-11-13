using Godot;
using Yokai.Core;

namespace Yokai.Client;

/// <summary>Articulated proxy, not the final Vaelira model or final character proportions.</summary>
public partial class UnitView : Node3D
{
    public Unit Model { get; set; } = null!;
    Node3D body = null!, leftArm = null!, rightArm = null!, leftLeg = null!, rightLeg = null!;
    MeshInstance3D? selection;
    Node3D? crystal;
    double punchUntil, kickUntil, hitUntil;
    string gearKey = "";
    Node3D? equipment;
    public override void _Ready()
    {
        Name = "Unit_" + Model.Id;
        body = new Node3D(); AddChild(body);
        if (Model.IsStructure) Structure(); else Character();
        selection = Art.Ring(this, new(0, .10f, 0), Model.IsStructure ? (float)Model.Radius + .25f : .75f, .035f,
            Model.Team == Team.Dawn ? Art.Teal : Art.Red);
        selection.Visible = Model.IsPlayer;
    }
    void Character()
    {
        var hero = Model.Kind == UnitKind.Champion;
        var cloth = Art.Mat(Model.Team == Team.Dawn ? "752e48" : "354f75");
        var dark = Art.Mat("202c39"); var skin = Art.Mat(hero && Model.IsPlayer ? "bc837e" : "697d82");
        var gold = Art.Mat("c3a265", .45f);
        var s = hero ? 1f : .62f; body.Scale = Vector3.One * s;
        // Deliberately neutral proxy geometry; replace the complete visual root with licensed skinned art later.
        Art.Sphere(body, new(0, 1.34f, 0), new(.59f, .72f, .35f), cloth);
        Art.Cylinder(body, new(0, .91f, 0), .39f, .28f, .48f, cloth, 12);
        Art.Cylinder(body, new(0, 1.12f, 0), .31f, .31f, .12f, gold, 12);
        Art.Sphere(body, new(0, 1.91f, 0), new(.40f, .48f, .36f), skin);
        Art.Sphere(body, new(0, 2.03f, .04f), new(.47f, .35f, .43f), dark);
        if (hero)
        {
            Art.Sphere(body, new(0, 1.66f, .22f), new(.42f, .80f, .22f), dark);
            foreach (var sign in new[] { -1, 1 })
            {
                var horn = Art.Cylinder(body, new(sign * .19f, 2.28f, -.015f), .08f, .005f, .42f, Art.Mat("ead7b4"), 10);
                horn.RotationDegrees = new(-10, 0, sign * -18);
                Art.Sphere(body, new(sign * .105f, 1.94f, -.175f), new(.075f, .035f, .035f), Art.Mat("ffd28f", 0, 1.7f));
            }
            Art.Box(body, new(.31f, .78f, .16f), new(.16f, .64f, .06f), gold).RotationDegrees = new(0, 0, -10);
        }
        else
        {
            Art.Box(body, new(0, 1.94f, -.17f), new(.31f, .31f, .07f), Art.Mat("d1c9a8"));
            Art.Cylinder(body, new(0, 2.18f, 0), .38f, .03f, .16f, dark, 12);
        }
        leftArm = Limb(body, new(-.37f, 1.61f, 0), skin, gold, true);
        rightArm = Limb(body, new(.37f, 1.61f, 0), skin, gold, true);
        leftLeg = Limb(body, new(-.16f, .86f, 0), dark, gold, false);
        rightLeg = Limb(body, new(.16f, .86f, 0), dark, gold, false);
    }
    static Node3D Limb(Node3D root, Vector3 pivot, Material main, Material trim, bool arm)
    {
        var limb = new Node3D { Position = pivot }; root.AddChild(limb);
        Art.Cylinder(limb, new(0, -.27f, 0), arm ? .085f : .105f, arm ? .10f : .13f, .54f, main, 10);
        Art.Cylinder(limb, new(0, -.46f, 0), .105f, .105f, .19f, trim, 10);
        Art.Sphere(limb, new(0, -.66f, arm ? -.025f : -.08f), arm ? new(.20f, .23f, .20f) : new(.24f, .23f, .36f), main);
        return limb;
    }
    void Structure()
    {
        var color = Model.Team == Team.Dawn ? "6bdace" : "e98177";
        var stone = Art.Mat("465957"); var trim = Art.Mat("baa16b", .5f);
        var core = Model.Kind == UnitKind.Core;
        Art.Cylinder(body, new(0, .18f, 0), core ? 1.65f : 1.05f, core ? 1.5f : .9f, .35f, stone, 8);
        Art.Cylinder(body, new(0, .85f, 0), core ? .9f : .5f, core ? .8f : .43f, 1.35f, stone, 8);
        foreach (var y in new[] { .42f, 1.35f }) Art.Cylinder(body, new(0, y, 0), core ? 1.05f : .65f, core ? 1.05f : .65f, .12f, trim, 8);
        crystal = new Node3D { Position = new(0, 2.15f, 0) }; body.AddChild(crystal);
        Art.Cylinder(crystal, new(0, .28f, 0), core ? .6f : .36f, 0, .95f, Art.Mat(color, .25f, 1.5f), 6);
        Art.Cylinder(crystal, new(0, -.3f, 0), 0, core ? .6f : .36f, .34f, Art.Mat(color, .25f, 1.5f), 6);
        foreach (var sign in new[] { -1, 1 })
        {
            Art.Cylinder(body, new(sign * (core ? 1.2f : .7f), 1.6f, .4f), .12f, .1f, 3.1f, Art.Mat("87483e"), 10);
        }
        var roof = Art.Cylinder(body, new(0, 3.35f, .2f), core ? 1.8f : 1.2f, .25f, .7f, Art.Mat("273d43"), 4);
        roof.RotationDegrees = new(0, 45, 0);
        Art.Cylinder(body, new(0, 3.9f, .2f), .13f, .02f, .5f, trim, 8);
    }
    public void Animate(double time, double alpha, bool selected)
    {
        Position = Art.World(V2.Lerp(Model.PreviousPosition, Model.Position, alpha));
        Visible = Model.Alive;
        if (!Visible) return;
        if (selection is not null) selection.Visible = selected || Model.IsPlayer;
        if (Model.IsStructure)
        {
            if (crystal is not null) { crystal.Rotation = new(0, (float)time * .5f, 0); crystal.Position = new(0, 2.15f + Mathf.Sin((float)time * 1.4f) * .08f, 0); }
            return;
        }
        var facing = Art.World(Model.Facing);
        if (facing.LengthSquared() > .001f) Rotation = new(0, Mathf.LerpAngle(Rotation.Y, Mathf.Atan2(-facing.X, -facing.Z), .25f), 0);
        var walking = V2.Distance(Model.Position, Model.PreviousPosition) > .0001;
        var stride = walking ? Mathf.Sin((float)time * 11) * .5f : Mathf.Sin((float)time * 2) * .025f;
        leftLeg.Rotation = new(stride, 0, 0); rightLeg.Rotation = new(-stride, 0, 0);
        leftArm.Rotation = new(-stride * .7f, 0, -.08f); rightArm.Rotation = new(stride * .7f, 0, .08f);
        body.Position = new(0, walking ? Mathf.Abs(Mathf.Sin((float)time * 11)) * .035f : 0, 0);
        body.Rotation = Vector3.Zero;
        if (time < punchUntil) rightArm.Rotation = new(-1.7f, 0, -.1f);
        if (time < kickUntil) { rightLeg.Rotation = new(-1.5f, 0, 0); body.Rotation = new(.15f, 0, 0); }
        if (Model.StunnedUntil > time) body.Rotation = new(0, 0, 1.25f);
        if (Model.LeapLandAt.HasValue)
        {
            var progress = Mathf.Clamp((float)((time - Model.LeapStartedAt) / .55), 0, 1);
            Position += Vector3.Up * Mathf.Sin(progress * Mathf.Pi) * 3.7f;
            rightArm.Rotation = leftArm.Rotation = new(-2.6f, 0, 0);
        }
        if (time < hitUntil) body.Position += new Vector3(.035f, 0, 0);
        UpdateEquipment();
    }
    void UpdateEquipment()
    {
        var key = Model.Loadout[EquipSlot.MainHand]?.Id ?? "bare";
        key += "/" + (Model.Loadout[EquipSlot.OffHand]?.Id ?? "bare");
        if (key == gearKey) return;
        gearKey = key; equipment?.QueueFree(); equipment = new Node3D(); rightArm.AddChild(equipment);
        var metal = Art.Mat("b9c5bf", .7f); var dark = Art.Mat("3d3534");
        switch (Model.Loadout.AttackKind)
        {
            case WeaponKind.Blade:
                Art.Box(equipment, new(0, -.65f, -.65f), new(.07f, .12f, 1.35f), metal);
                Art.Box(equipment, new(0, -.65f, -.08f), new(.25f, .08f, .1f), Art.Mat("c9aa63", .6f)); break;
            case WeaponKind.Crossbow:
                Art.Box(equipment, new(0, -.65f, -.3f), new(.13f, .14f, .7f), dark);
                Art.Box(equipment, new(0, -.65f, -.5f), new(.7f, .06f, .1f), metal); break;
            case WeaponKind.CastingStaff:
                Art.Cylinder(equipment, new(0, -.15f, -.15f), .055f, .045f, 1.9f, dark, 8);
                Art.Sphere(equipment, new(0, .85f, -.15f), new(.28f, .35f, .28f), Art.Mat("85dcdf", 0, 1.5f)); break;
            default:
                if (Model.Loadout.BrassCount + Model.Loadout.SpikedCount > 0)
                    Art.Box(equipment, new(0, -.67f, -.12f), new(.26f, .11f, .12f), Art.Mat("c7a067", .6f));
                break;
        }
    }
    public void Event(GameEvent e, double now)
    {
        if (e.SourceId == Model.Id)
        {
            if (e.Kind is EventKind.Punch or EventKind.Attack) punchUntil = now + .18;
            if (e.Kind is EventKind.Kick or EventKind.Sweep) kickUntil = now + .25;
        }
        if (e.TargetId == Model.Id && e.Kind == EventKind.Damage) hitUntil = now + .09;
    }
}
