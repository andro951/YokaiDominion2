using Godot;
using Yokai.Core;

namespace Yokai.Client;

/// <summary>
/// First-pass Character01 game visual. Procedural posing is a development animation layer,
/// not mocap, cloth simulation, tissue physics, or final approved Vaelira art.
/// </summary>
public partial class CharacterAvatar : Node3D
{
    const string AssetPath = "res://Assets/Characters/Character01_Game001.glb";
    Skeleton3D skeleton = null!;
    readonly Dictionary<string, int> bones = new(StringComparer.OrdinalIgnoreCase);
    Quaternion[] rests = [];
    Basis[] globalRestInverse = [];
    string gearKey = "";
    readonly List<Node3D> attachments = new();
    public int BoneCount => skeleton?.GetBoneCount() ?? 0;
    static string Key(string name) => name.Replace(".", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
    public static CharacterAvatar? Create()
    {
        if (!ResourceLoader.Exists(AssetPath)) return null;
        var scene = GD.Load<PackedScene>(AssetPath);
        if (scene is null) return null;
        var avatar = new CharacterAvatar { Name = "Character01_DevelopmentAvatar" };
        var model = scene.Instantiate<Node3D>(); avatar.AddChild(model);
        avatar.skeleton = FindSkeleton(model)!;
        if (avatar.skeleton is null) { avatar.Free(); GD.PushWarning("Character01 has no skeleton; retaining proxy."); return null; }
        var count = avatar.skeleton.GetBoneCount();
        avatar.rests = new Quaternion[count]; avatar.globalRestInverse = new Basis[count];
        avatar.skeleton.ResetBonePoses();
        for (var i = 0; i < count; i++)
        {
            avatar.bones[Key(avatar.skeleton.GetBoneName(i))] = i;
            avatar.rests[i] = avatar.skeleton.GetBoneRest(i).Basis.Orthonormalized().GetRotationQuaternion();
            avatar.globalRestInverse[i] = avatar.skeleton.GetBoneGlobalRest(i).Basis.Orthonormalized().Inverse();
        }
        return avatar;
    }
    static Skeleton3D? FindSkeleton(Node node)
    {
        if (node is Skeleton3D result) return result;
        foreach (var child in node.GetChildren()) { var found = FindSkeleton(child); if (found is not null) return found; }
        return null;
    }
    void Rotate(string name, Vector3 modelAxis, float angle)
    {
        if (!bones.TryGetValue(Key(name), out var index)) return;
        var localAxis = (globalRestInverse[index] * modelAxis).Normalized();
        var rotation = skeleton.GetBonePoseRotation(index) * new Quaternion(localAxis, angle);
        skeleton.SetBonePoseRotation(index, rotation.Normalized());
    }
    public void Animate(Unit unit, double time, bool walking, double punchUntil, double kickUntil)
    {
        for (var i = 0; i < rests.Length; i++) skeleton.SetBonePoseRotation(i, rests[i]);
        var t = (float)time; var cycle = t * 10.5f; var stride = walking ? Mathf.Sin(cycle) * .34f : 0;
        Rotate("Chest", Vector3.Right, Mathf.Sin(t * 2) * .012f);
        Rotate("UpperArm.L", Vector3.Back, -.30f);
        Rotate("UpperArm.R", Vector3.Back, .30f);
        Rotate("LowerArm.L", Vector3.Right, -.18f);
        Rotate("LowerArm.R", Vector3.Right, -.18f);
        Rotate("UpperLeg.L", Vector3.Right, -stride);
        Rotate("UpperLeg.R", Vector3.Right, stride);
        Rotate("LowerLeg.L", Vector3.Right, Math.Max(0, stride) * .8f);
        Rotate("LowerLeg.R", Vector3.Right, Math.Max(0, -stride) * .8f);
        Rotate("Foot.L", Vector3.Right, stride * .2f);
        Rotate("Foot.R", Vector3.Right, -stride * .2f);
        Rotate("UpperArm.L", Vector3.Right, stride * .55f);
        Rotate("UpperArm.R", Vector3.Right, -stride * .55f);
        Rotate("Skirt.Front", Vector3.Right, -Mathf.Abs(stride) * .85f);
        Rotate("Skirt.Back", Vector3.Right, Mathf.Abs(stride) * .5f);
        for (var i = 0; i < 5; i++) Rotate("Tail." + i, Vector3.Up, Mathf.Sin(t * 2.2f - i * .4f) * .035f);
        if (time < punchUntil)
        {
            var strength = Mathf.Sin(Mathf.Clamp((float)((punchUntil - time) / .24), 0, 1) * Mathf.Pi);
            Rotate("UpperArm.R", Vector3.Right, -1.2f * strength);
            Rotate("LowerArm.R", Vector3.Right, -.35f * strength);
            Rotate("Chest", Vector3.Up, -.12f * strength);
            Rotate("UpperArm.L", Vector3.Right, -.5f * strength);
        }
        if (time < kickUntil)
        {
            var strength = Mathf.Sin(Mathf.Clamp((float)((kickUntil - time) / .34), 0, 1) * Mathf.Pi);
            Rotate("UpperLeg.R", Vector3.Right, -1.05f * strength);
            Rotate("LowerLeg.R", Vector3.Right, .15f * strength);
            Rotate("Skirt.Front", Vector3.Right, -1.12f * strength);
            Rotate("Spine", Vector3.Right, .13f * strength);
        }
        if (unit.LeapLandAt.HasValue)
        {
            Rotate("UpperArm.L", Vector3.Right, -2.2f);
            Rotate("UpperArm.R", Vector3.Right, -2.2f);
            Rotate("UpperLeg.L", Vector3.Right, -.3f);
            Rotate("UpperLeg.R", Vector3.Right, -.3f);
            Rotate("LowerLeg.L", Vector3.Right, .6f);
            Rotate("LowerLeg.R", Vector3.Right, .6f);
        }
        UpdateEquipment(unit.Loadout);
    }
    void UpdateEquipment(Loadout loadout)
    {
        var key = (loadout[EquipSlot.MainHand]?.Id ?? "bare") + "/" + (loadout[EquipSlot.OffHand]?.Id ?? "bare");
        if (key == gearKey) return;
        gearKey = key;
        foreach (var attachment in attachments) { attachment.Visible = false; attachment.QueueFree(); }
        attachments.Clear();
        foreach (var slot in new[] { EquipSlot.MainHand, EquipSlot.OffHand })
        {
            var item = loadout[slot]; if (item is null) continue;
            var name = slot == EquipSlot.MainHand ? "Hand.R" : "Hand.L";
            if (!bones.TryGetValue(Key(name), out var index)) continue;
            var socket = new BoneAttachment3D { BoneName = skeleton.GetBoneName(index) };
            skeleton.AddChild(socket); attachments.Add(socket);
            var metal = Art.Mat("bdc6bd", .7f); var wood = Art.Mat("493e3c");
            switch (item.Weapon)
            {
                case WeaponKind.Blade:
                    Art.Box(socket, new(0, -.09f, .50f), new(.04f, .07f, 1.05f), metal);
                    Art.Box(socket, new(0, -.09f, .03f), new(.22f, .06f, .08f), Art.Mat("c7a067", .6f)); break;
                case WeaponKind.Crossbow:
                    Art.Box(socket, new(0, -.08f, .22f), new(.09f, .1f, .55f), wood);
                    Art.Box(socket, new(0, -.08f, .36f), new(.6f, .05f, .08f), metal); break;
                case WeaponKind.CastingStaff:
                    Art.Cylinder(socket, new(0, .1f, .06f), .035f, .035f, 1.6f, wood, 8);
                    Art.Sphere(socket, new(0, .93f, .06f), new(.22f, .28f, .22f), Art.Mat("85dcdf", 0, 1.5f)); break;
                case WeaponKind.Fist:
                    Art.Box(socket, new(0, -.055f, .035f), new(.14f, .05f, .09f), Art.Mat("c7a067", .6f)); break;
                default:
                    Art.Sphere(socket, new(-.08f, -.05f, .05f), new(.10f, .62f, .45f), metal); break;
            }
        }
    }
}
