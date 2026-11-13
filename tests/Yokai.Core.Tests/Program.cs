using Yokai.Core;

var passed = 0;
void Check(string name, Action test)
{
    try { test(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception e) { Console.Error.WriteLine("FAIL " + name + ": " + e.Message); Environment.ExitCode = 1; }
}
void Near(double expected, double actual, double epsilon = 1e-7)
{ if (!double.IsFinite(actual) || Math.Abs(expected - actual) > epsilon) throw new Exception($"Expected {expected}, got {actual}"); }
void True(bool condition) { if (!condition) throw new Exception("Assertion failed"); }

Check("11 equipment slots", () => Near(11, Enum.GetValues<EquipSlot>().Length));
Check("CDR hard cap and Q attack-speed synchronization", () => {
    Near(1.28, CombatRules.AttackInterval(.25));
    Near(1.2, CombatRules.Cooldown(3, .9, .25, true));
    Near(6, CombatRules.Cooldown(12, .9, 2));
});
Check("Q window boundaries remain fractional", () => {
    True(CombatRules.Window(.399, 1.6) == QWindow.Kick);
    True(CombatRules.Window(.4, 1.6) == QWindow.Neutral);
    True(CombatRules.Window(1.2, 1.6) == QWindow.Prepare);
    True(CombatRules.Window(1.3, 1.6) == QWindow.Prepare);
});
Check("Mana reference matches approved 25 percent formula", () =>
    Near(7.633928571428571, CombatRules.NaturalManaRegen([(30, 7), (50, 40), (70, 3), (100, 60)])));
Check("Single glove retains full unarmed scaling", () => {
    var l = new Loadout(); var a = new Attributes(50, 100, 0);
    True(l.TryEquip(Items.Brass, EquipSlot.MainHand, a, out _)); Near(155, l.AttackDamage(a));
    True(l.TryEquip(Items.Spiked, EquipSlot.OffHand, a, out _)); Near(165, l.AttackDamage(a));
});
Check("Dual scores multiply scaling, never flat bases", () => {
    var l = new Loadout(); var a = new Attributes(50, 100, 0);
    var katana = Items.Katana with { FlatDamage = 80 };
    True(l.TryEquip(katana, EquipSlot.MainHand, a, out _)); Near(290, l.AttackDamage(a));
    True(l.TryEquip(katana, EquipSlot.OffHand, a, out _)); Near(391, l.AttackDamage(a));
});
Check("Failed equipment transaction rolls back", () => {
    var l = new Loadout(); var a = new Attributes(30, 10, 10);
    True(l.TryEquip(Items.Katana, EquipSlot.MainHand, a, out _));
    True(!l.TryEquip(Items.Shield, EquipSlot.OffHand, a, out _));
    True(l[EquipSlot.OffHand] is null); Near(30 + 24 + 17, l.AttackDamage(a));
});
Check("2+3 armor sets retain cumulative thresholds", () => {
    var l = new Loadout(); var a = new Attributes(100, 0, 0);
    for (var i = 0; i < 5; i++) {
        var prefix = i < 2 ? "iron-" : "mist-";
        var item = Items.Catalog.Single(x => x.Id == prefix + Items.ArmorSlots[i]);
        True(l.TryEquip(item, item.Slot, a, out _));
    }
    Near(36, l.Armor); Near(66, l.Resistance); Near(24, l.HeavyPlateArmor);
});
Check("Bleed refresh preserves each source duration", () => {
    var q = new Bleed(1, "Q", DamageKind.True, 5, 5);
    var spike = new Bleed(1, "Spikes", DamageKind.Physical, 1, 30);
    q.Advance(2, 999, 0); spike.Advance(2, 100, 0); q.Refresh(); spike.Refresh();
    Near(5, q.Remaining); Near(30, spike.Remaining);
});
Check("Fractional physical bleed totals 15 through 100 armor", () => {
    var b = new Bleed(1, "Spikes", DamageKind.Physical, 1, 30); var carry = new DamageAccumulator(); var total = 0;
    for (var i = 0; i < 500; i++) total += carry.Add(b.Advance(.06, 100, 0));
    Near(15, total); Near(0, carry.Fraction, 1e-6);
});
Check("Q true bleed totals exactly 25 despite arbitrary time steps", () => {
    var b = new Bleed(1, "Q", DamageKind.True, 5, 5); var total = 0.0;
    for (var i = 0; i < 100; i++) total += b.Advance(.06, 10000, 10000);
    Near(25, total);
});
Check("Blood Scent only pursues bleeding enemy champions", () => {
    True(CombatRules.BloodScent(new(0, 0), new(1, 0), new(5, 0), true, true));
    True(!CombatRules.BloodScent(new(0, 0), new(-1, 0), new(5, 0), true, true));
    True(!CombatRules.BloodScent(new(0, 0), new(1, 0), new(5, 0), false, true));
    True(!CombatRules.BloodScent(new(0, 0), new(1, 0), new(5, 0), true, false));
});
Check("Continuous movement has no diagonal speed advantage", () => {
    Near(5, V2.MoveTowards(V2.Zero, new(50, 0), 5).Length);
    Near(5, V2.MoveTowards(V2.Zero, new(50, 50), 5).Length);
});
Check("Swept body cannot tunnel through obstacle", () => {
    Disc[] obstacles = [new(new(0, 0), 1)];
    True(Collision.SweptDiscHits(new(-5, 0), new(5, 0), .5, obstacles[0]));
    True(Collision.SafeMove(new(-5, 0), new(5, 0), .5, obstacles).X <= -1.49);
});
Check("Hex centers round-trip including negative coordinates", () => {
    for (var q = -20; q <= 20; q++) for (var r = -20; r <= 20; r++) {
        var h = new Hex(q, r); True(Hex.FromWorld(h.Center()) == h);
    }
});
Check("Off-hand-only weapons retain ranged and magical attack identity", () => {
    var h = new Unit { Kind = UnitKind.Champion };
    True(h.Loadout.TryEquip(Items.Staff, EquipSlot.OffHand, h.Innate, out _));
    True(h.Loadout.AttackKind == WeaponKind.CastingStaff); True(h.IsRanged); True(!h.Loadout.FistCompatible);
    Near(18 + h.Stats.Scale(Items.Staff.Scaling), h.AttackDamage);
    h.Loadout.Remove(EquipSlot.OffHand);
    True(h.Loadout.TryEquip(Items.Crossbow, EquipSlot.OffHand, h.Innate, out _));
    True(h.Loadout.AttackKind == WeaponKind.Crossbow); True(h.IsRanged); Near(10.8, h.AttackRange);
});
Check("All four ability ranks have a benefit without inflating natural mana regeneration", () => {
    var h = new Unit { Kind = UnitKind.Champion }; var mana = h.ManaRegen;
    foreach (var ability in Enum.GetValues<Ability>()) {
        var previous = h.CooldownFor(ability);
        for (var rank = 2; rank <= 5; rank++) {
            h.Ranks[(int)ability] = rank; var next = h.CooldownFor(ability);
            True(next > 0 && next < previous); previous = next; Near(mana, h.ManaRegen);
        }
    }
    Near(3, h.CooldownFor(Ability.Q)); Near(8, h.CooldownFor(Ability.W));
    Near(9, h.CooldownFor(Ability.E)); Near(44, h.CooldownFor(Ability.R));
});
Check("Respawn clears transient pursuit and combo flags", () => {
    var h = new Unit { Kind = UnitKind.Champion, BloodScentActive = true, AttackMove = true, Empowered = true, FreeKickUntil = 999 };
    h.ResetLife(new(-22, 1.7)); True(!h.BloodScentActive && !h.AttackMove && !h.Empowered); True(h.FreeKickUntil < 0);
});
Console.WriteLine($"{passed} regression tests passed.");
