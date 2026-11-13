namespace Yokai.Core;

/// <summary>Hex A* supplies waypoints; actors still move continuously, with swept-body collision.</summary>
public sealed class Navigation
{
    static readonly Hex[] Neighbors = [new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)];
    public IReadOnlyList<Disc> Obstacles { get; }
    public Navigation(IReadOnlyList<Disc> obstacles) => Obstacles = obstacles;
    public bool Walkable(V2 p, double radius) => Math.Abs(p.X) < 28 - radius && Math.Abs(p.Y) < 17 - radius
        && !Obstacles.Any(o => V2.Distance(o.Center, p) < o.Radius + radius);
    public bool Clear(V2 a, V2 b, double radius) => Walkable(b, radius)
        && !Obstacles.Any(o => Collision.SweptDiscHits(a, b, radius, o));

    public List<V2> FindPath(V2 from, V2 destination, double radius)
    {
        if (!from.IsFinite || !destination.IsFinite) return [];
        destination = new(Math.Clamp(destination.X, -27, 27), Math.Clamp(destination.Y, -16, 16));
        if (Clear(from, destination, radius)) return [destination];
        var start = NearestWalkable(from, radius); var end = NearestWalkable(destination, radius);
        if (start is null || end is null) return [];
        var frontier = new PriorityQueue<Hex, (double, int)>();
        var costs = new Dictionary<Hex, double> { [start.Value] = 0 };
        var parent = new Dictionary<Hex, Hex>();
        var sequence = 0; frontier.Enqueue(start.Value, (0, sequence++));
        var visited = new HashSet<Hex>();
        while (frontier.TryDequeue(out var current, out _) && visited.Count < 6000)
        {
            if (!visited.Add(current)) continue;
            if (current == end.Value)
            {
                var nodes = new List<V2> { current.Center() };
                while (parent.TryGetValue(current, out var previous)) { current = previous; nodes.Add(current.Center()); }
                nodes.Reverse();
                if (Walkable(destination, radius) && Clear(nodes[^1], destination, radius)) nodes.Add(destination);
                // String-pull only over collision-tested segments, including the start connection.
                var smooth = new List<V2>(); var anchor = from;
                for (var i = 0; i < nodes.Count;)
                {
                    var furthest = i;
                    if (!Clear(anchor, nodes[i], radius)) return [];
                    while (furthest + 1 < nodes.Count && Clear(anchor, nodes[furthest + 1], radius)) furthest++;
                    smooth.Add(nodes[furthest]); anchor = nodes[furthest]; i = furthest + 1;
                }
                return smooth;
            }
            foreach (var d in Neighbors)
            {
                var next = new Hex(current.Q + d.Q, current.R + d.R);
                if (visited.Contains(next) || !Clear(current.Center(), next.Center(), radius)) continue;
                var cost = costs[current] + V2.Distance(current.Center(), next.Center());
                if (costs.TryGetValue(next, out var old) && cost >= old) continue;
                costs[next] = cost; parent[next] = current;
                frontier.Enqueue(next, (cost + V2.Distance(next.Center(), end.Value.Center()), sequence++));
            }
        }
        return [];
    }
    Hex? NearestWalkable(V2 p, double radius)
    {
        var origin = Hex.FromWorld(p);
        if (Walkable(origin.Center(), radius)) return origin;
        Hex? best = null; var distance = double.MaxValue;
        for (var q = -4; q <= 4; q++) for (var r = -4; r <= 4; r++)
        {
            var cell = new Hex(origin.Q + q, origin.R + r); var d = V2.Distance(cell.Center(), p);
            if (d < distance && Walkable(cell.Center(), radius)) { best = cell; distance = d; }
        }
        return best;
    }
}
