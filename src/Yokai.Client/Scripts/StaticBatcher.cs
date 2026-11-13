using Godot;
namespace Yokai.Client;

/// <summary>
/// Combines immobile opaque arena geometry by material and spatial chunk.
/// Keeps actors, transparent rings, shader-displaced surfaces, and grass instancing separate.
/// Chunking retains useful camera/shadow culling instead of merging the entire map into one AABB.
/// </summary>
public partial class StaticBatcher : Node
{
    public override void _Ready() => Callable.From(Batch).CallDeferred();
    static IEnumerable<MeshInstance3D> Descendants(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is MeshInstance3D mesh) yield return mesh;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    void Batch()
    {
        var arena = GetParent().GetNodeOrNull<Node3D>("ShrineOfTheVeil");
        if (arena is null) return;
        var candidates = Descendants(arena).Where(n => n.Mesh is not null && n.Mesh.GetSurfaceCount() == 1
            && n.MaterialOverride is StandardMaterial3D m && m.Transparency == BaseMaterial3D.TransparencyEnum.Disabled).ToArray();
        var groups = candidates.GroupBy(n => (
            Material: n.MaterialOverride,
            Shadow: n.CastShadow,
            X: (int)Math.Floor(n.GlobalPosition.X / 12),
            Z: (int)Math.Floor(n.GlobalPosition.Z / 12)));
        var removed = 0; var added = 0;
        foreach (var group in groups)
        {
            if (group.Count() < 2) continue;
            using var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
            foreach (var source in group)
            {
                surface.AppendFrom(source.Mesh, 0, arena.GlobalTransform.AffineInverse() * source.GlobalTransform);
                source.Visible = false; source.QueueFree(); removed++;
            }
            surface.Index(); surface.OptimizeIndicesForCache();
            arena.AddChild(new MeshInstance3D {
                Name = "StaticBatch_" + added++, Mesh = surface.Commit(),
                MaterialOverride = group.Key.Material, CastShadow = group.Key.Shadow
            });
        }
        GD.Print($"STATIC_BATCH_OK sources={removed} batches={added}");
    }
}
