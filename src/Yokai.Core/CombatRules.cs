namespace Yokai.Core;

public enum QWindow { Kick, Neutral, Prepare }
public static class CombatRules
{
    public const double BaseAttackInterval = 1.6;
    public const double KickBleedDps = 5;
    public const double KickBleedDuration = 5;
    public static double AttackInterval(double attackSpeedBonus) => BaseAttackInterval / (1 + Math.Clamp(attackSpeedBonus, -.75, 4));
    public static double Cooldown(double baseline, double cdr, double attackSpeedBonus = 0, bool synchronized = false) =>
        baseline * (1 - Math.Clamp(cdr, 0, .5)) / (synchronized ? 1 + Math.Clamp(attackSpeedBonus, -.75, 4) : 1);
    public static double QBaseCooldown(int rank) => 4.2 - .3 * (Math.Clamp(rank, 1, 5) - 1);
    public static QWindow Window(double secondsSinceHit, double interval)
    {
        if (interval <= 0 || !double.IsFinite(interval)) throw new ArgumentOutOfRangeException(nameof(interval));
        if (secondsSinceHit >= -1e-8 && secondsSinceHit < interval * .25 - 1e-8) return QWindow.Kick;
        if (secondsSinceHit >= interval * .75 - 1e-8 && secondsSinceHit <= interval + 1e-8) return QWindow.Prepare;
        return QWindow.Neutral;
    }
    public static double Mitigate(double raw, DamageKind kind, double armor, double resistance)
    {
        var defense = kind == DamageKind.Physical ? armor : resistance;
        var multiplier = defense >= 0 ? 100 / (100 + defense) : 2 - 100 / (100 - defense);
        return Math.Max(0, raw) * (kind == DamageKind.True ? 1 : multiplier);
    }
    public static double NaturalManaRegen(IEnumerable<(double Mana, double Cooldown)> abilities) =>
        .25 * abilities.Sum(a => a.Cooldown > 0 ? a.Mana / a.Cooldown : throw new ArgumentOutOfRangeException(nameof(abilities)));
    public static bool BloodScent(V2 position, V2 direction, V2 enemyPosition, bool isEnemyChampion, bool bleeding, double radius = 10) =>
        isEnemyChampion && bleeding && direction.LengthSquared > 1e-12 &&
        V2.Distance(position, enemyPosition) <= radius && V2.Dot(direction.Normalized, (enemyPosition - position).Normalized) > .25;
}

public sealed class Bleed
{
    public int SourceId { get; }
    public string Source { get; }
    public DamageKind Kind { get; }
    public double Dps { get; }
    public double Duration { get; }
    public double Remaining { get; private set; }
    public Bleed(int sourceId, string source, DamageKind kind, double dps, double duration)
    {
        if (dps < 0 || duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
        SourceId = sourceId; Source = source; Kind = kind; Dps = dps; Duration = duration; Remaining = duration;
    }
    public void Refresh() => Remaining = Duration;
    public double Advance(double dt, double armor, double resistance)
    {
        if (dt < 0 || !double.IsFinite(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
        var active = Math.Min(dt, Remaining);
        Remaining = Math.Max(0, Remaining - dt);
        return CombatRules.Mitigate(Dps * active, Kind, armor, resistance);
    }
}

/// <summary>Target-owned fractional carry; tiny mitigated ticks are never rounded away.</summary>
public sealed class DamageAccumulator
{
    public double Fraction { get; private set; }
    public int Add(double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        Fraction += value;
        var whole = (int)Math.Floor(Fraction + 1e-9);
        Fraction = Math.Max(0, Fraction - whole);
        return whole;
    }
    public void Clear() => Fraction = 0;
}
