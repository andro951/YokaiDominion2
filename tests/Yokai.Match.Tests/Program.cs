using Yokai.Core;

var passed = 0;
void Check(string name, Action test)
{
    try { test(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception e) { Console.Error.WriteLine("FAIL " + name + ": " + e); Environment.ExitCode = 1; }
}
void Near(double expected, double actual, double tolerance = 1e-6)
{ if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance) throw new Exception($"Expected {expected}, got {actual}"); }
void True(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
(GameWorld World, Unit Hero, Unit Target) Fixture()
{
    var w = new GameWorld(true); var hero = w.Player;
    hero.Position = hero.PreviousPosition = new(-5, 0);
    var target = w.Add("Practice target", Team.Dusk, UnitKind.Minion, new(-3.7, 0));
    target.Health = 10000; w.OrderAttack(hero, target.Id);
    return (w, hero, target);
}
void Steps(GameWorld w, int count) { for (var i = 0; i < count; i++) w.Step(); }

Check("Pre-hit Q queues damage, empowered hit opens one free kick", () => {
    var (w, h, t) = Fixture(); h.LastHitAt = -1.3;
    var health = t.Health;
    True(w.TryCast(h, Ability.Q, t.Position)); Near(health, t.Health); True(h.Empowered);
    var originalCooldown = h.ReadyAt[0];
    Steps(w, 60);
    True(t.Health < health); True(!h.Empowered); True(h.FreeKickUntil > w.Time);
    var mana = h.Mana; var beforeKick = t.Health;
    True(w.TryCast(h, Ability.Q, t.Position)); Near(mana, h.Mana); Near(originalCooldown, h.ReadyAt[0]);
    True(t.Health < beforeKick); True(t.Bleeds.Any(b => b.Source == "Q kick"));
    True(!w.TryCast(h, Ability.Q, t.Position), "A consumed free recast cannot be reused during cooldown");
});
Check("Empowered basic event applies one spike stack per equipped glove", () => {
    var (w, h, t) = Fixture();
    True(h.Loadout.TryEquip(Items.Spiked, EquipSlot.MainHand, h.Innate, out _));
    True(h.Loadout.TryEquip(Items.Spiked, EquipSlot.OffHand, h.Innate, out _));
    h.LastHitAt = -1.3; True(w.TryCast(h, Ability.Q, t.Position)); Steps(w, 60);
    Near(2, t.Bleeds.Count(b => b.Source == "Spiked Knuckles"));
});
Check("Kick neither procs knuckles nor refreshes unrelated bleeds", () => {
    var (w, h, t) = Fixture();
    True(h.Loadout.TryEquip(Items.Brass, EquipSlot.MainHand, h.Innate, out _));
    True(h.Loadout.TryEquip(Items.Spiked, EquipSlot.OffHand, h.Innate, out _));
    var foreign = new Bleed(99, "Ally bleed", DamageKind.Physical, 1, 30);
    foreign.Advance(20, 0, 0); t.Bleeds.Add(foreign); h.LastHitAt = 0;
    True(w.TryCast(h, Ability.Q, t.Position));
    Near(0, t.PlateBreakStacks); Near(0, t.Bleeds.Count(b => b.Source == "Spiked Knuckles"));
    Near(10, foreign.Remaining); Near(1, t.Bleeds.Count(b => b.Source == "Q kick"));
});
Check("Crossbow Q cannot inherit ranged basic-attack reach", () => {
    var (w, h, t) = Fixture(); True(h.Loadout.TryEquip(Items.Crossbow, EquipSlot.MainHand, h.Innate, out _));
    t.Position = new(1, 0); True(w.CanReach(h, t, h.AttackRange));
    var mana = h.Mana; True(!w.TryCast(h, Ability.Q, t.Position)); Near(mana, h.Mana); Near(0, h.ReadyAt[0]);
    t.Position = new(-3.2, 0); True(w.TryCast(h, Ability.Q, t.Position));
    True(t.Bleeds.Any(b => b.Source == "Q kick")); True(!h.Empowered); True(h.FreeKickUntil < w.Time);
});
Check("W refreshes Q but respects the new attack's windup", () => {
    var (w, h, t) = Fixture(); h.ReadyAt[0] = 100; var health = t.Health;
    True(w.TryCast(h, Ability.W, t.Position)); Near(w.Time, h.ReadyAt[0]);
    Near(health, t.Health); True(h.AttackLandAt > w.Time);
    Steps(w, 30); Near(health, t.Health); Steps(w, 30); True(t.Health < health);
});
Check("Q reapplication refreshes only that caster's Q source", () => {
    var (w, h, t) = Fixture(); w.Stop(h); h.TargetId = t.Id; h.LastHitAt = 0;
    True(w.TryCast(h, Ability.Q, t.Position)); var own = t.Bleeds.Single();
    own.Advance(3, 0, 0); t.Bleeds.Add(new(99, "Q kick", DamageKind.True, 5, 5));
    h.ReadyAt[0] = 0; True(w.TryCast(h, Ability.Q, t.Position));
    Near(2, t.Bleeds.Count); Near(5, own.Remaining);
});
Check("Heavy plate break does not destroy innate armor or cloth defense", () => {
    var w = new GameWorld(true); var h = w.Player;
    var iron = Items.Catalog.Single(i => i.Id == "iron-Body");
    var mist = Items.Catalog.Single(i => i.Id == "mist-Legs");
    True(h.Loadout.TryEquip(iron, iron.Slot, h.Innate, out _)); True(h.Loadout.TryEquip(mist, mist.Slot, h.Innate, out _));
    var initial = h.Armor; h.PlateBreakStacks = 16; Near(initial - 12 * .8, h.Armor);
});
Check("Failed shop purchase preserves gold and inventory", () => {
    var w = new GameWorld(true); var h = w.Player; h.Gold = 10;
    True(!w.Buy(Items.Katana)); Near(10, h.Gold); True(h.Loadout[EquipSlot.MainHand] is null);
    h.Gold = 1000; h.Position = new(0, 0); True(!w.Buy(Items.Brass)); Near(1000, h.Gold);
});
Check("Replaced equipment goes to storage and swaps without a second charge", () => {
    var w = new GameWorld(true); var h = w.Player; h.Gold = 1000;
    True(w.Buy(Items.Brass)); True(w.Buy(Items.Spiked)); Near(1, h.Storage.Count);
    var gold = h.Gold; True(w.EquipStored(0)); Near(gold, h.Gold);
    True(h.Loadout[EquipSlot.MainHand]?.Id == "brass"); True(h.Storage[0].Id == "spiked");
});
Check("Navigation routes around terrain with every smoothed segment swept", () => {
    var nav = new Navigation([new(new(0, 0), 1.5)]); var from = new V2(-5, 0); var to = new V2(5, 0);
    var path = nav.FindPath(from, to, .48); True(path.Count > 1);
    foreach (var point in path) { True(nav.Clear(from, point, .48)); from = point; }
    Near(0, V2.Distance(from, to));
});
Check("Visual frame cadence does not change movement simulation", () => {
    var a = new GameWorld(true); var b = new GameWorld(true);
    a.OrderMove(a.Player, new(-10, 0)); b.OrderMove(b.Player, new(-10, 0));
    for (var i = 0; i < 120; i++) a.Advance(1.0 / 60);
    for (var i = 0; i < 240; i++) b.Advance(1.0 / 120);
    Near(a.Time, b.Time); Near(0, V2.Distance(a.Player.Position, b.Player.Position));
});
Check("Two-minute autonomous match remains finite and bounded", () => {
    var w = new GameWorld(); w.OrderMove(w.Player, new(-9, 0), true);
    Steps(w, 120 * 120);
    True(w.Time > 30); True(w.Units.Count < 250); True(w.Wave >= 2);
    foreach (var u in w.Units) {
        True(u.Position.IsFinite && double.IsFinite(u.Health) && double.IsFinite(u.Mana));
        True(Math.Abs(u.Position.X) <= 28 && Math.Abs(u.Position.Y) <= 17);
    }
});
Console.WriteLine($"{passed} match integration tests passed.");
