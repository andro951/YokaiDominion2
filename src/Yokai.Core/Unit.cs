namespace Yokai.Core;

public enum Team { Dawn, Dusk }
public enum UnitKind { Champion, Minion, Tower, Core }
public enum Ability { Q, W, E, R }
public enum EventKind { Attack, Punch, Kick, Sweep, Leap, Slam, Damage, Death, Respawn, LevelUp, Purchase, Victory }
public readonly record struct GameEvent(EventKind Kind, int SourceId, int TargetId, V2 Position, V2 Origin, double Amount = 0, DamageKind Damage = DamageKind.Physical);

public sealed class Unit
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public Team Team { get; init; }
    public UnitKind Kind { get; init; }
    public V2 Position { get; set; }
    public V2 PreviousPosition { get; set; }
    public V2 Facing { get; set; } = new(1, 0);
    public Hex Cell { get; set; }
    public double Radius => Kind switch { UnitKind.Champion => .48, UnitKind.Minion => .30, UnitKind.Tower => .9, _ => 1.5 };
    public bool IsStructure => Kind is UnitKind.Tower or UnitKind.Core;
    public bool Alive => Health > 0;
    public Attributes BaseAttributes { get; set; } = new(30, 24, 16);
    public Attributes Allocated { get; set; }
    public Attributes Innate => BaseAttributes + Allocated + new Attributes((Level - 1) * 2, (Level - 1) * 1.5, Level - 1);
    public Attributes Stats => Innate + Loadout.Bonuses;
    public Loadout Loadout { get; } = new();
    public List<Item> Storage { get; } = new();
    public List<Bleed> Bleeds { get; } = new();
    public DamageAccumulator BleedCarry { get; } = new();
    public int PlateBreakStacks { get; set; }
    public double Health { get; set; }
    public double Mana { get; set; }
    public double Gold { get; set; } = 450;
    public int Level { get; set; } = 1;
    public double Experience { get; set; }
    public double NextLevelExperience => 90 + Level * 45;
    public int AttributePoints { get; set; }
    public int AbilityPoints { get; set; }
    public int[] Ranks { get; } = [1, 1, 1, 1];
    public double[] ReadyAt { get; } = new double[4];
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int LastHits { get; set; }
    public bool BloodScentActive { get; set; }
    public double RespawnAt { get; set; }
    public double StunnedUntil { get; set; }
    public double LastHitAt { get; set; } = -100;
    public double NextAttackStartAt { get; set; }
    public double? AttackLandAt { get; set; }
    public int AttackVictimId { get; set; }
    public int TargetId { get; set; }
    public bool Empowered { get; set; }
    public double FreeKickUntil { get; set; } = -1;
    public V2? MoveDestination { get; set; }
    public Queue<V2> Path { get; } = new();
    public bool AttackMove { get; set; }
    public double NextPathAt { get; set; }
    public double? LeapLandAt { get; set; }
    public double LeapStartedAt { get; set; }
    public V2 LeapStart { get; set; }
    public V2 LeapEnd { get; set; }
    public int Lane { get; set; }
    public bool IsPlayer { get; set; }
    public double NextThinkAt { get; set; }
    public double MaxHealth => Kind switch { UnitKind.Champion => 650 + Stats.Strength * 12 + Loadout.BonusHealth, UnitKind.Minion => 230, UnitKind.Tower => 1750, _ => 3200 };
    public double MaxMana => 160 + Stats.Intelligence * 8;
    public double Armor => Kind switch { UnitKind.Champion => 10 + Stats.Dexterity * .25 + Loadout.Armor - Loadout.HeavyPlateArmor * Math.Min(.8, PlateBreakStacks * .05), UnitKind.Minion => 5, UnitKind.Tower => 30, _ => 20 };
    public double Resistance => Kind == UnitKind.Champion ? 10 + Stats.Intelligence * .2 + Loadout.Resistance : 10;
    public double AttackSpeedBonus => Kind == UnitKind.Champion ? Math.Max(0, Stats.Dexterity - BaseAttributes.Dexterity) * .015 : 0;
    public double Interval => Kind switch { UnitKind.Champion => CombatRules.AttackInterval(AttackSpeedBonus), UnitKind.Minion => 1.8, _ => 1.7 };
    public double AttackRange => Kind switch { UnitKind.Champion => .8 + Loadout.RangeBonus, UnitKind.Minion => .6, UnitKind.Tower => 8, _ => 0 };
    public double AttackDamage => Kind switch { UnitKind.Champion => Loadout.AttackDamage(Stats), UnitKind.Minion => 23, UnitKind.Tower => 105, _ => 0 };
    public double Speed => Kind == UnitKind.Champion ? 4.8 + Stats.Dexterity * .007 : 2.6;
    public double HealthRegen => Kind == UnitKind.Champion ? .6 + Stats.Strength * .025 + MaxHealth * Stats.Strength * .000006 : 0;
    // Baseline deliberately excludes rank, allocated Intelligence, CDR and attack speed changes.
    public double ManaRegen => CombatRules.NaturalManaRegen([(20, 4.2), (35, 10), (40, 12), (75, 60)])
        + Math.Max(0, Stats.Intelligence - BaseAttributes.Intelligence) * .08 + Loadout.BonusManaRegen;
    public bool IsRanged => Kind == UnitKind.Tower || Loadout.AttackKind is WeaponKind.Crossbow or WeaponKind.CastingStaff;
    public QWindow CurrentQWindow(double now) => AttackLandAt.HasValue ? QWindow.Prepare : CombatRules.Window(now - LastHitAt, Interval);
    // W/E/R curves are provisional balance. Every purchased rank now has a real cooldown benefit.
    public double CooldownFor(Ability a) => CombatRules.Cooldown(a switch {
        Ability.Q => CombatRules.QBaseCooldown(Ranks[0]),
        Ability.W => 10 - .5 * (Math.Clamp(Ranks[1], 1, 5) - 1),
        Ability.E => 12 - .75 * (Math.Clamp(Ranks[2], 1, 5) - 1),
        Ability.R => 60 - 4 * (Math.Clamp(Ranks[3], 1, 5) - 1),
        _ => throw new ArgumentOutOfRangeException(nameof(a))
    }, Loadout.Cdr, AttackSpeedBonus, a is Ability.Q or Ability.W);
    public void ResetLife(V2 at)
    {
        Position = PreviousPosition = at; Health = MaxHealth; Mana = MaxMana;
        Bleeds.Clear(); BleedCarry.Clear(); PlateBreakStacks = 0; StunnedUntil = 0;
        AttackLandAt = null; LeapLandAt = null; TargetId = 0; MoveDestination = null; Path.Clear();
        Empowered = false; FreeKickUntil = -1; LastHitAt = -100; NextAttackStartAt = 0;
        BloodScentActive = false; AttackMove = false;
    }
}
