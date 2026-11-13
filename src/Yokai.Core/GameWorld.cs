namespace Yokai.Core;

/// <summary>Single-player authoritative match simulation. No claim of network lockstep determinism.</summary>
public sealed class GameWorld
{
    public const double FixedDelta = 1.0 / 120;
    public const double GridDelta = .06;
    readonly List<Unit> units = new();
    readonly Queue<GameEvent> events = new();
    int nextId = 1;
    double accumulator, nextGridAt, nextWaveAt = 2;
    public double Time { get; private set; }
    public double RenderAlpha => accumulator / FixedDelta;
    public IReadOnlyList<Unit> Units => units;
    public List<Disc> Terrain { get; } = [
        new(new(-10, -4.5), 1.7), new(new(10, 4.5), 1.7),
        new(new(-7, 5), 1.4), new(new(7, -5), 1.4),
        new(new(-19, -13), 1.8), new(new(19, 13), 1.8),
        new(new(0, -4.5), 1.7), new(new(0, 4.5), 1.7),
        new(new(0, -14.5), 2.2), new(new(0, 14.5), 2.2)
    ];
    public Navigation Navigation { get; }
    public Unit Player { get; }
    public Team? Winner { get; private set; }
    public int Wave { get; private set; }
    public string Notice { get; private set; } = "Protect the Dawn shrine. Break the Dusk shrine.";
    public double NoticeUntil { get; private set; } = 8;
    public bool Training { get; }
    public GameWorld(bool training = false)
    {
        Training = training; Navigation = new(Terrain);
        Player = Add("Vaelira", Team.Dawn, UnitKind.Champion, new(-22, 1.7)); Player.IsPlayer = true;
        if (!training)
        {
            Add("Dusk duelist", Team.Dusk, UnitKind.Champion, new(22, -1.7));
            foreach (var team in Enum.GetValues<Team>())
            {
                var sign = team == Team.Dawn ? -1 : 1;
                Add(team + " shrine", team, UnitKind.Core, new(sign * 25, 0));
                foreach (var lane in new[] { -1, 0, 1 }) Add("Ward tower", team, UnitKind.Tower, new(sign * 14, lane * 9)).Lane = lane;
            }
        }
    }
    public Unit Add(string name, Team team, UnitKind kind, V2 position)
    {
        var unit = new Unit { Id = nextId++, Name = name, Team = team, Kind = kind, Position = position, PreviousPosition = position };
        unit.ResetLife(position); units.Add(unit); return unit;
    }
    public Unit? Find(int id) => units.Find(u => u.Id == id);
    public static V2 Spawn(Team team) => new(team == Team.Dawn ? -22 : 22, team == Team.Dawn ? 1.7 : -1.7);
    public double EdgeDistance(Unit a, Unit b) => Math.Max(0, V2.Distance(a.Position, b.Position) - a.Radius - b.Radius);
    public bool CanReach(Unit a, Unit b, double range) => b.Alive && a.Team != b.Team && EdgeDistance(a, b) <= range + 1e-7;
    public bool AtBase(Unit u) => V2.Distance(u.Position, Spawn(u.Team)) < 5.5;
    public IEnumerable<GameEvent> DrainEvents() { while (events.TryDequeue(out var e)) yield return e; }
    void Emit(EventKind kind, Unit source, Unit target, double amount = 0, DamageKind damage = DamageKind.Physical) =>
        events.Enqueue(new(kind, source.Id, target.Id, target.Position, source.Position, amount, damage));
    void NoticePlayer(string message) { Notice = message; NoticeUntil = Time + 4; }

    public void OrderMove(Unit u, V2 destination, bool attackMove = false)
    {
        if (!u.Alive || !destination.IsFinite || u.IsStructure || u.LeapLandAt.HasValue) return;
        u.TargetId = 0; u.AttackLandAt = null; u.AttackMove = attackMove; u.MoveDestination = destination;
        u.Path.Clear(); foreach (var p in Navigation.FindPath(u.Position, destination, u.Radius)) u.Path.Enqueue(p);
        if (u.Path.Count == 0 && u.IsPlayer) NoticePlayer("That destination is blocked.");
    }
    public void OrderAttack(Unit u, int targetId)
    {
        var target = Find(targetId);
        if (target is null || !target.Alive || target.Team == u.Team || !u.Alive) return;
        u.TargetId = targetId; u.MoveDestination = null; u.Path.Clear(); u.AttackMove = true;
    }
    public void Stop(Unit u) { u.TargetId = 0; u.MoveDestination = null; u.Path.Clear(); u.AttackLandAt = null; u.AttackMove = false; }
    public void Advance(double dt)
    {
        if (!double.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
        accumulator += Math.Min(dt, .25);
        while (accumulator + 1e-10 >= FixedDelta) { Step(); accumulator -= FixedDelta; }
        accumulator = Math.Max(0, accumulator);
    }
    public void Step()
    {
        if (Winner.HasValue) return;
        Time += FixedDelta;
        if (!Training && Time >= nextWaveAt)
        {
            nextWaveAt += 22; Wave++;
            foreach (var team in Enum.GetValues<Team>()) foreach (var lane in new[] { -1, 0, 1 })
                for (var i = 0; i < 3; i++)
                {
                    var sign = team == Team.Dawn ? -1 : 1;
                    var minion = Add("Shrine spirit", team, UnitKind.Minion, new(sign * (21.5 + i * .8), lane * 9 + (i - 1) * .45));
                    minion.Lane = lane;
                }
        }
        foreach (var u in units.ToArray()) Tick(u);
        if (Time >= nextGridAt)
        {
            nextGridAt += GridDelta;
            foreach (var u in units) u.Cell = Hex.FromWorld(u.Position);
        }
        // Keep dead units briefly for death presentation, but do not grow the simulation indefinitely.
        units.RemoveAll(u => u.Kind == UnitKind.Minion && !u.Alive && Time > u.RespawnAt + 2);
        while (events.Count > 4096) events.Dequeue();
    }
    void Tick(Unit u)
    {
        u.PreviousPosition = u.Position;
        if (!u.Alive)
        {
            if (u.Kind == UnitKind.Champion && Time >= u.RespawnAt)
            { u.ResetLife(Spawn(u.Team)); Emit(EventKind.Respawn, u, u); }
            return;
        }
        for (var i = u.Bleeds.Count - 1; i >= 0; i--)
        {
            var bleed = u.Bleeds[i]; var damage = u.BleedCarry.Add(bleed.Advance(FixedDelta, u.Armor, u.Resistance));
            if (damage > 0) ApplyResolvedDamage(Find(bleed.SourceId), u, damage, bleed.Kind);
            if (!u.Alive) return;
            if (bleed.Remaining <= 1e-9) u.Bleeds.RemoveAt(i);
        }
        if (u.Kind == UnitKind.Champion)
        {
            var home = AtBase(u);
            u.Health = Math.Min(u.MaxHealth, u.Health + (u.HealthRegen + (home ? u.MaxHealth * .12 : 0)) * FixedDelta);
            u.Mana = Math.Min(u.MaxMana, u.Mana + (u.ManaRegen + (home ? 35 : 0)) * FixedDelta);
            u.Gold += 2 * FixedDelta;
        }
        if (u.LeapLandAt.HasValue)
        {
            var progress = (Time - u.LeapStartedAt) / .55;
            u.Position = V2.Lerp(u.LeapStart, u.LeapEnd, progress);
            if (Time >= u.LeapLandAt.Value)
            {
                u.LeapLandAt = null; u.Position = u.LeapEnd;
                foreach (var victim in units.Where(v => v.Alive && v.Team != u.Team && V2.Distance(v.Position, u.Position) <= 3.5 + v.Radius).ToArray())
                    Deal(u, victim, (130 + u.Stats.Strength * 1.2) * (victim.StunnedUntil > Time ? 1.5 : 1), DamageKind.Physical);
                Emit(EventKind.Slam, u, u);
            }
            return;
        }
        if (u.StunnedUntil > Time) { u.AttackLandAt = null; return; }
        if (!u.IsPlayer && !Training) Think(u);
        var target = Find(u.TargetId);
        if (target is not null && !target.Alive) { target = null; u.TargetId = 0; }
        if (u.AttackLandAt.HasValue)
        {
            if (Time >= u.AttackLandAt.Value)
            {
                var victim = Find(u.AttackVictimId); var impactTime = u.AttackLandAt.Value; u.AttackLandAt = null;
                if (victim is not null && CanReach(u, victim, u.AttackRange + .15)) BasicHit(u, victim, impactTime);
                else { u.Empowered = false; u.NextAttackStartAt = Time + .1; }
            }
            return;
        }
        if (target is not null)
        {
            u.Facing = (target.Position - u.Position).Normalized;
            if (CanReach(u, target, u.AttackRange))
            {
                if (Time >= u.NextAttackStartAt && u.Kind != UnitKind.Core) BeginAttack(u, target);
                return;
            }
            if (!u.IsStructure)
            {
                if (Time >= u.NextPathAt)
                {
                    u.NextPathAt = Time + .45; u.Path.Clear();
                    foreach (var p in Navigation.FindPath(u.Position, target.Position, u.Radius)) u.Path.Enqueue(p);
                }
                Move(u);
            }
        }
        else if (!u.IsStructure) Move(u);
    }
    void Think(Unit u)
    {
        if (Time < u.NextThinkAt || u.Kind == UnitKind.Core) return;
        u.NextThinkAt = Time + .25;
        if (u.Kind == UnitKind.Champion && u.Health < u.MaxHealth * .25)
        { OrderMove(u, Spawn(u.Team)); return; }
        var nearby = units.Where(v => v.Alive && v.Team != u.Team && EdgeDistance(u, v) <= (u.IsStructure ? u.AttackRange : 7))
            .OrderBy(v => v.Kind == UnitKind.Minion ? 0 : v.Kind == UnitKind.Champion ? 1 : 2)
            .ThenBy(v => V2.Distance(v.Position, u.Position)).FirstOrDefault();
        if (nearby is not null)
        {
            u.TargetId = nearby.Id;
            if (u.Kind == UnitKind.Champion)
            {
                if (EdgeDistance(u, nearby) < 2.2) { TryCast(u, Ability.Q, nearby.Position); TryCast(u, Ability.E, nearby.Position); }
                if (nearby.StunnedUntil > Time) TryCast(u, Ability.R, nearby.Position);
            }
        }
        else if (!u.IsStructure)
        {
            u.TargetId = 0;
            var end = new V2(u.Team == Team.Dawn ? 25 : -25, 0);
            var destination = Math.Abs(u.Position.X) < 19 ? new V2(u.Team == Team.Dawn ? 20 : -20, u.Lane * 9) : end;
            if ((u.Team == Team.Dawn && u.Position.X < -18) || (u.Team == Team.Dusk && u.Position.X > 18))
                destination = new(u.Team == Team.Dawn ? -17 : 17, u.Lane * 9);
            if (u.Path.Count == 0) OrderMove(u, destination, true);
        }
    }
    void Move(Unit u)
    {
        if (u.Path.Count == 0) { u.BloodScentActive = false; return; }
        var goal = u.Path.Peek();
        if (V2.Distance(u.Position, goal) < .12) { u.Path.Dequeue(); if (u.Path.Count == 0) return; goal = u.Path.Peek(); }
        var direction = (goal - u.Position).Normalized;
        u.BloodScentActive = u.Kind == UnitKind.Champion && units.Any(v => v.Alive && v.Team != u.Team &&
            CombatRules.BloodScent(u.Position, direction, v.Position, v.Kind == UnitKind.Champion, v.Bleeds.Count > 0));
        // Local avoidance steers around bodies, while the swept terrain test remains authoritative.
        var steering = direction;
        foreach (var other in units.Where(v => v.Id != u.Id && v.Alive && v.Id != u.TargetId))
        {
            var offset = u.Position - other.Position; var distance = offset.Length;
            var limit = u.Radius + other.Radius + .25;
            if (distance > .001 && distance < limit) steering += offset / distance * ((limit - distance) / limit * 2);
        }
        var speed = u.Speed * (u.BloodScentActive ? 1.1 : 1);
        var desired = u.Position + steering.Normalized * Math.Min(speed * FixedDelta, V2.Distance(u.Position, goal));
        u.Position = Collision.SafeMove(u.Position, desired, u.Radius, Terrain);
        if (steering.LengthSquared > .01) u.Facing = steering.Normalized;
        if (u.IsPlayer && u.AttackMove && u.TargetId == 0)
        {
            var enemy = units.Where(v => v.Alive && v.Team != u.Team && EdgeDistance(u, v) < u.AttackRange + 2)
                .OrderBy(v => V2.Distance(u.Position, v.Position)).FirstOrDefault();
            if (enemy is not null) u.TargetId = enemy.Id;
        }
    }
    void BeginAttack(Unit u, Unit target)
    {
        u.AttackVictimId = target.Id; u.AttackLandAt = Time + u.Interval * .25;
        Emit(EventKind.Attack, u, target);
    }
    void BasicHit(Unit u, Unit target, double impactTime)
    {
        var empowered = u.Empowered && u.Loadout.FistCompatible;
        var kind = u.Loadout.AttackKind == WeaponKind.CastingStaff ? DamageKind.Magical : DamageKind.Physical;
        Deal(u, target, u.AttackDamage, kind, true);
        if (empowered && target.Alive) Deal(u, target, QPunchDamage(u), DamageKind.Physical);
        if (target.Alive)
        {
            // One application per BASIC ATTACK, not per animation strike, even for empowered Q.
            target.PlateBreakStacks = Math.Min(16, target.PlateBreakStacks + u.Loadout.BrassCount);
            if (u.Loadout.SpikedCount > 0)
            {
                foreach (var bleed in target.Bleeds) bleed.Refresh();
                for (var i = 0; i < u.Loadout.SpikedCount; i++) target.Bleeds.Add(new(u.Id, "Spiked Knuckles", DamageKind.Physical, 1, 30));
            }
        }
        u.Empowered = false; u.LastHitAt = impactTime;
        u.FreeKickUntil = empowered ? impactTime + u.Interval * .25 : -1;
        u.NextAttackStartAt = impactTime + u.Interval * .75;
        Emit(EventKind.Punch, u, target, empowered ? 2 : 1);
    }
    static double QPunchDamage(Unit u) => 35 + u.Stats.Strength * .35 + u.Stats.Dexterity * .3;
    public bool TryCast(Unit u, Ability ability, V2 aim)
    {
        if (!u.Alive || u.IsStructure || u.StunnedUntil > Time || u.LeapLandAt.HasValue || !aim.IsFinite || Winner.HasValue) return false;
        var target = Find(u.TargetId);
        target ??= units.Where(v => v.Alive && v.Team != u.Team && V2.Distance(v.Position, aim) < 2)
            .OrderBy(v => V2.Distance(v.Position, aim)).FirstOrDefault();
        if (ability == Ability.Q && u.FreeKickUntil > Time && target is not null && CanReach(u, target, 1.45))
        { u.FreeKickUntil = -1; Kick(u, target); return true; }
        var index = (int)ability;
        if (Time + 1e-9 < u.ReadyAt[index]) return false;
        var cost = ability switch { Ability.Q => 20, Ability.W => 35, Ability.E => 40, _ => 75 };
        if (u.Mana < cost) return false;
        if (ability is Ability.Q or Ability.W)
        {
            if (target is null || !CanReach(u, target, ability == Ability.Q ? (u.Loadout.FistCompatible ? 1.45 : 1.45) : u.AttackRange)) return false;
            if (ability == Ability.Q && u.Loadout.FistCompatible && u.CurrentQWindow(Time) != QWindow.Kick && !CanReach(u, target, .8)) return false;
        }
        if (ability == Ability.R)
        {
            aim = V2.MoveTowards(u.Position, aim, 8);
            if (!Navigation.Walkable(aim, u.Radius)) return false;
        }
        u.Mana -= cost; u.ReadyAt[index] = Time + u.CooldownFor(ability);
        switch (ability)
        {
            case Ability.Q:
                if (!u.Loadout.FistCompatible || u.CurrentQWindow(Time) == QWindow.Kick) Kick(u, target!);
                else if (u.CurrentQWindow(Time) == QWindow.Prepare) { u.Empowered = true; u.TargetId = target!.Id; }
                else { Deal(u, target!, QPunchDamage(u), DamageKind.Physical); Emit(EventKind.Punch, u, target!); }
                break;
            case Ability.W:
                u.ReadyAt[0] = Time; u.NextAttackStartAt = Time; u.AttackLandAt = null; u.TargetId = target!.Id; BeginAttack(u, target);
                break;
            case Ability.E:
                foreach (var victim in units.Where(v => v.Alive && v.Team != u.Team && !v.IsStructure && V2.Distance(v.Position, u.Position) < 2.7 + v.Radius).ToArray())
                { victim.StunnedUntil = Math.Max(victim.StunnedUntil, Time + 1.15); Deal(u, victim, 55 + .6 * u.Stats.Strength, DamageKind.Physical); }
                Emit(EventKind.Sweep, u, u);
                break;
            case Ability.R:
                u.LeapStart = u.Position; u.LeapEnd = aim; u.LeapStartedAt = Time; u.LeapLandAt = Time + .55;
                u.AttackLandAt = null; u.Path.Clear(); Emit(EventKind.Leap, u, u);
                break;
        }
        return true;
    }
    void Kick(Unit source, Unit target)
    {
        Deal(source, target, QPunchDamage(source), DamageKind.Physical);
        if (target.Alive)
        {
            // Reapplication refreshes this caster's Q bleed; distinct casters retain separate effects.
            var own = target.Bleeds.Find(b => b.SourceId == source.Id && b.Source == "Q kick");
            if (own is null) target.Bleeds.Add(new(source.Id, "Q kick", DamageKind.True, 5, 5)); else own.Refresh();
        }
        Emit(EventKind.Kick, source, target);
    }
    void Deal(Unit source, Unit target, double raw, DamageKind kind, bool basic = false)
    {
        var armor = target.Armor * (basic ? 1 - Math.Min(.8, source.Loadout.BrassCount * .05) : 1);
        ApplyResolvedDamage(source, target, CombatRules.Mitigate(raw, kind, armor, target.Resistance), kind);
    }
    void ApplyResolvedDamage(Unit? source, Unit target, double amount, DamageKind kind)
    {
        if (!target.Alive) return;
        target.Health = Math.Max(0, target.Health - amount);
        if (source is not null) Emit(EventKind.Damage, source, target, amount, kind);
        if (target.Alive) return;
        target.RespawnAt = Time + (target.Kind == UnitKind.Champion ? 8 + target.Level * 2 : 0);
        target.Deaths++; target.AttackLandAt = null;
        if (source is not null)
        {
            Emit(EventKind.Death, source, target);
            if (source.Kind == UnitKind.Champion)
            {
                source.Gold += target.Kind switch { UnitKind.Champion => 220, UnitKind.Tower => 250, _ => 32 };
                if (target.Kind == UnitKind.Champion) source.Kills++; else if (target.Kind == UnitKind.Minion) source.LastHits++;
            }
            foreach (var champion in units.Where(v => v.Alive && v.Kind == UnitKind.Champion && v.Team == source.Team && V2.Distance(v.Position, target.Position) < 12))
            {
                champion.Experience += target.Kind == UnitKind.Champion ? 100 : 28;
                while (champion.Experience >= champion.NextLevelExperience && champion.Level < 18)
                {
                    champion.Experience -= champion.NextLevelExperience; champion.Level++; champion.AttributePoints += 3; champion.AbilityPoints++;
                    champion.Health = Math.Min(champion.MaxHealth, champion.Health + 60); Emit(EventKind.LevelUp, champion, champion);
                }
            }
        }
        if (target.Kind == UnitKind.Core)
        { Winner = target.Team == Team.Dawn ? Team.Dusk : Team.Dawn; Emit(EventKind.Victory, source ?? target, target); }
    }
    public bool Buy(Item item, bool offHand = false)
    {
        var u = Player;
        if (!u.Alive || !AtBase(u)) { NoticePlayer("Return to your shrine to buy or swap equipment."); return false; }
        if (u.Gold < item.Cost) { NoticePlayer("Not enough gold."); return false; }
        var slot = offHand && item.Slot == EquipSlot.MainHand ? EquipSlot.OffHand : item.Slot;
        var old = u.Loadout[slot];
        if (!u.Loadout.TryEquip(item, slot, u.Innate, out var reason)) { NoticePlayer(reason); return false; }
        u.Gold -= item.Cost; if (old is not null) u.Storage.Add(old);
        u.Health = Math.Min(u.Health, u.MaxHealth); u.Mana = Math.Min(u.Mana, u.MaxMana);
        NoticePlayer(reason + (old is null ? "" : "; previous item stored at shrine.")); Emit(EventKind.Purchase, u, u); return true;
    }
    public bool EquipStored(int index, bool offHand = false)
    {
        var u = Player; if (!u.Alive || !AtBase(u) || index < 0 || index >= u.Storage.Count) return false;
        var item = u.Storage[index]; var slot = offHand && item.Slot == EquipSlot.MainHand ? EquipSlot.OffHand : item.Slot;
        var old = u.Loadout[slot];
        if (!u.Loadout.TryEquip(item, slot, u.Innate, out var reason)) { NoticePlayer(reason); return false; }
        u.Storage.RemoveAt(index); if (old is not null) u.Storage.Add(old); NoticePlayer(reason); return true;
    }
    public bool AllocateAttribute(int index)
    {
        if (Player.AttributePoints <= 0 || index < 0 || index > 2) return false;
        Player.Allocated += index switch { 0 => new Attributes(1, 0, 0), 1 => new Attributes(0, 1, 0), _ => new Attributes(0, 0, 1) };
        Player.AttributePoints--; return true;
    }
    public bool RankUp(Ability ability)
    {
        var i = (int)ability; if (Player.AbilityPoints <= 0 || Player.Ranks[i] >= 5) return false;
        Player.Ranks[i]++; Player.AbilityPoints--; return true;
    }
}
