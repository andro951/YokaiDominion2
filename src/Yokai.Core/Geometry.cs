namespace Yokai.Core;

/// <summary>Simulation coordinates: X is east, Y is north. Rendering maps Y to Godot Z.</summary>
public readonly record struct V2(double X, double Y)
{
    public static readonly V2 Zero = new(0, 0);
    public double LengthSquared => X * X + Y * Y;
    public double Length => Math.Sqrt(LengthSquared);
    public V2 Normalized => Length > 1e-10 ? this / Length : Zero;
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
    public static V2 operator +(V2 a, V2 b) => new(a.X + b.X, a.Y + b.Y);
    public static V2 operator -(V2 a, V2 b) => new(a.X - b.X, a.Y - b.Y);
    public static V2 operator *(V2 a, double b) => new(a.X * b, a.Y * b);
    public static V2 operator /(V2 a, double b) => new(a.X / b, a.Y / b);
    public static double Dot(V2 a, V2 b) => a.X * b.X + a.Y * b.Y;
    public static double Distance(V2 a, V2 b) => (a - b).Length;
    public static V2 Lerp(V2 a, V2 b, double t) => a + (b - a) * Math.Clamp(t, 0, 1);
    public static V2 MoveTowards(V2 a, V2 b, double distance) =>
        (b - a).Length <= distance ? b : a + (b - a).Normalized * distance;
}

/// <summary>Pointy-top axial hex coordinates. Grid position is derived, never movement authority.</summary>
public readonly record struct Hex(int Q, int R)
{
    public int S => -Q - R;
    public V2 Center(double radius = .6) => new(radius * Math.Sqrt(3) * (Q + R / 2.0), radius * 1.5 * R);
    public static Hex FromWorld(V2 p, double radius = .6)
    {
        if (!p.IsFinite || radius <= 0) throw new ArgumentOutOfRangeException(nameof(p));
        var q = (Math.Sqrt(3) / 3 * p.X - p.Y / 3) / radius;
        var r = (2.0 / 3 * p.Y) / radius;
        var s = -q - r;
        var qi = (int)Math.Round(q, MidpointRounding.AwayFromZero);
        var ri = (int)Math.Round(r, MidpointRounding.AwayFromZero);
        var si = (int)Math.Round(s, MidpointRounding.AwayFromZero);
        var dq = Math.Abs(qi - q); var dr = Math.Abs(ri - r); var ds = Math.Abs(si - s);
        // Explicit tie order keeps boundaries reproducible on a given runtime.
        if (dq >= dr && dq >= ds) qi = -ri - si;
        else if (dr >= ds) ri = -qi - si;
        return new(qi, ri);
    }
}

public readonly record struct Disc(V2 Center, double Radius);

public static class Collision
{
    public static double SegmentDistanceSquared(V2 a, V2 b, V2 p)
    {
        var d = b - a;
        var t = d.LengthSquared <= 1e-14 ? 0 : Math.Clamp(V2.Dot(p - a, d) / d.LengthSquared, 0, 1);
        return (p - (a + d * t)).LengthSquared;
    }

    /// <summary>Sweep the entire segment against an expanded obstacle, not just its endpoint.</summary>
    public static bool SweptDiscHits(V2 a, V2 b, double bodyRadius, Disc obstacle) =>
        SegmentDistanceSquared(a, b, obstacle.Center) < Math.Pow(bodyRadius + obstacle.Radius, 2) - 1e-10;

    public static V2 SafeMove(V2 from, V2 desired, double radius, IReadOnlyList<Disc> obstacles)
    {
        desired = new(Math.Clamp(desired.X, -28 + radius, 28 - radius), Math.Clamp(desired.Y, -17 + radius, 17 - radius));
        bool Clear(V2 point) => !obstacles.Any(o => SweptDiscHits(from, point, radius, o));
        if (Clear(desired)) return desired;
        // Sliding retains collision safety. Full path planning is separate from this sweep.
        var horizontal = new V2(desired.X, from.Y);
        var vertical = new V2(from.X, desired.Y);
        if (Clear(horizontal) && (horizontal - from).LengthSquared > 1e-12) return horizontal;
        if (Clear(vertical)) return vertical;
        var low = 0.0; var high = 1.0;
        for (var i = 0; i < 18; i++)
        {
            var mid = (low + high) * .5;
            if (Clear(V2.Lerp(from, desired, mid))) low = mid; else high = mid;
        }
        return V2.Lerp(from, desired, Math.Max(0, low - .0001));
    }
}
