namespace Yokai.Core;

public enum EquipSlot { Head, Cape, Neck, Ammunition, MainHand, Body, OffHand, Legs, Hands, Feet, Ring }
public enum WeaponKind { None, Fist, Blade, Crossbow, CastingStaff }
public enum ArmorMaterial { None, Cloth, Leather, Chain, HeavyPlate }
public enum DamageKind { Physical, Magical, True }
public readonly record struct Attributes(double Strength, double Dexterity, double Intelligence)
{
    public static Attributes operator +(Attributes a, Attributes b) => new(a.Strength + b.Strength, a.Dexterity + b.Dexterity, a.Intelligence + b.Intelligence);
    public double Scale(Attributes coefficients) => Strength * coefficients.Strength + Dexterity * coefficients.Dexterity + Intelligence * coefficients.Intelligence;
}

public sealed record Item(
    string Id, string Name, EquipSlot Slot, int Cost, string Description,
    WeaponKind Weapon = WeaponKind.None, double FlatDamage = 0, Attributes Scaling = default,
    double DualScore = .5, double MainScore = .9, double StrengthRequired = 0, double RangeBonus = 0,
    Attributes Bonus = default, double Armor = 0, double Resistance = 0, double Health = 0,
    double ManaRegen = 0, double Cdr = 0, ArmorMaterial Material = ArmorMaterial.None, string? Set = null);

/// <summary>Numbers not explicitly approved in design notes are prototype tuning, not new design decisions.</summary>
public static class Items
{
    public static readonly Item Brass = new("brass", "Brass Knuckles", EquipSlot.MainHand, 120,
        "+5 flat damage, +5% penetration. Punches break heavy PLATE only. Full fist combo.", WeaponKind.Fist, 5, new(1, 1, 0), StrengthRequired: 5);
    public static readonly Item Spiked = new("spiked", "Spiked Knuckles", EquipSlot.MainHand, 160,
        "+10 flat damage. One physical bleed per glove per basic attack; refreshes every bleed's own duration.", WeaponKind.Fist, 10, new(1, 1, 0), StrengthRequired: 5);
    public static readonly Item Katana = new("katana", "Moonsteel Katana", EquipSlot.MainHand, 280,
        "Longer melee reach. Q becomes a close-range kick. Both flat bases count when dual wielding.", WeaponKind.Blade, 30, new(.8, 1.7, 0), .55, .85, 25, .8);
    public static readonly Item Crossbow = new("crossbow", "Shrine Crossbow", EquipSlot.MainHand, 280,
        "Ranged basic attacks. Q stays melee; the crossbow never adds a free melee bash.", WeaponKind.Crossbow, 24, new(.4, 1.4, .1), .5, .8, 18, 10);
    public static readonly Item Staff = new("staff", "Kitsune Casting Staff", EquipSlot.MainHand, 280,
        "Magical basic attacks. No melee double-hit. Q remains a melee kick.", WeaponKind.CastingStaff, 18, new(.1, .3, 1.8), .5, 1, 5, 9);
    public static readonly Item Shield = new("shield", "Mirror Guard", EquipSlot.OffHand, 180,
        "35 armor and 20 resistance. Active spell parry is not implemented in this milestone.", StrengthRequired: 15, Armor: 35, Resistance: 20);
    public static readonly Item Ring = new("ring", "Ring of Returning Rain", EquipSlot.Ring, 200,
        "+10% cooldown reduction and +3 mana per second.", ManaRegen: 3, Cdr: .1);
    public static readonly Item Cape = new("cape", "Pilgrim's Mantle", EquipSlot.Cape, 150,
        "+10 Intelligence, +15 magic resistance.", Bonus: new(0, 0, 10), Resistance: 15);
    public static readonly Item Neck = new("neck", "Oni Heart Pendant", EquipSlot.Neck, 180,
        "+12 Strength and +80 maximum health.", Bonus: new(12, 0, 0), Health: 80);
    public static readonly Item Ammo = new("ammo", "Moonlit Bolt Case", EquipSlot.Ammunition, 120,
        "+6 Dexterity. Ammunition consumption is not modeled yet.", Bonus: new(0, 6, 0));
    public static readonly EquipSlot[] ArmorSlots = [EquipSlot.Head, EquipSlot.Body, EquipSlot.Legs, EquipSlot.Hands, EquipSlot.Feet];
    public static readonly Item[] Catalog = Build();
    static Item[] Build()
    {
        var list = new List<Item> { Brass, Spiked, Katana, Crossbow, Staff, Shield, Ring, Cape, Neck, Ammo };
        foreach (var slot in ArmorSlots)
        {
            list.Add(new($"iron-{slot}", $"Iron Oni {slot}", slot, 160,
                "Heavy plate. Set 2: +12 armor; 3: +18 armor; 5: +65 armor, -20 magic resistance.",
                Armor: 12, Material: ArmorMaterial.HeavyPlate, Set: "Iron Oni"));
            list.Add(new($"mist-{slot}", $"Mistweave {slot}", slot, 160,
                "Cloth. Set 2: +12 resistance; 3: +18 resistance; 5: +65 resistance, -20 armor.",
                Resistance: 12, ManaRegen: 1, Material: ArmorMaterial.Cloth, Set: "Mistweave"));
        }
        return list.ToArray();
    }
}

public sealed class Loadout
{
    readonly Dictionary<EquipSlot, Item> equipped = new();
    public IReadOnlyDictionary<EquipSlot, Item> Equipped => equipped;
    public Item? this[EquipSlot slot] => equipped.GetValueOrDefault(slot);
    public Attributes Bonuses => equipped.Values.Aggregate(default(Attributes), (a, i) => a + i.Bonus);
    public bool FistCompatible => (this[EquipSlot.MainHand]?.Weapon ?? WeaponKind.Fist) == WeaponKind.Fist
        && (this[EquipSlot.OffHand]?.Weapon ?? WeaponKind.Fist) == WeaponKind.Fist;
    public int BrassCount => equipped.Values.Count(i => i.Id == "brass");
    public int SpikedCount => equipped.Values.Count(i => i.Id == "spiked");
    public double Cdr => Math.Clamp(equipped.Values.Sum(i => i.Cdr), 0, .5);
    public double BonusHealth => equipped.Values.Sum(i => i.Health);
    public double BonusManaRegen => equipped.Values.Sum(i => i.ManaRegen);
    public double HeavyPlateArmor => equipped.Values.Where(i => i.Material == ArmorMaterial.HeavyPlate).Sum(i => i.Armor);
    public double Armor => equipped.Values.Sum(i => i.Armor) + SetBonus("Iron Oni") - (SetCount("Mistweave") == 5 ? 20 : 0);
    public double Resistance => equipped.Values.Sum(i => i.Resistance) + SetBonus("Mistweave") - (SetCount("Iron Oni") == 5 ? 20 : 0);
    public int SetCount(string set) => equipped.Where(p => Items.ArmorSlots.Contains(p.Key) && p.Value.Set == set).Count();
    double SetBonus(string set) { var n = SetCount(set); return (n >= 2 ? 12 : 0) + (n >= 3 ? 18 : 0) + (n >= 5 ? 65 : 0); }
    public double RangeBonus => Math.Max(this[EquipSlot.MainHand]?.RangeBonus ?? 0, this[EquipSlot.OffHand]?.RangeBonus ?? 0);
    public WeaponKind AttackKind => this[EquipSlot.MainHand]?.Weapon ?? WeaponKind.Fist;

    public bool TryEquip(Item item, EquipSlot slot, Attributes innate, out string reason)
    {
        var hand = slot is EquipSlot.MainHand or EquipSlot.OffHand;
        if (hand ? item.Slot is not (EquipSlot.MainHand or EquipSlot.OffHand) : item.Slot != slot)
        { reason = "That item does not fit this slot."; return false; }
        if (slot == EquipSlot.MainHand && item.Weapon == WeaponKind.None)
        { reason = "A shield belongs in the off hand."; return false; }
        var before = equipped.GetValueOrDefault(slot);
        equipped[slot] = item;
        var main = this[EquipSlot.MainHand]; var off = this[EquipSlot.OffHand];
        // Mixed ranged/melee dual wield is unresolved in the design; do not invent its behavior.
        var incompatible = main is not null && off?.Weapon is not (null or WeaponKind.None)
            && main.Weapon != off.Weapon;
        var strength = (innate + Bonuses).Strength;
        if (incompatible || strength < (main?.StrengthRequired ?? 0) + (off?.StrengthRequired ?? 0))
        {
            if (before is null) equipped.Remove(slot); else equipped[slot] = before;
            reason = incompatible ? "Mixed weapon categories are not defined yet." : "Not enough Strength for the combined hand requirements.";
            return false;
        }
        reason = "Equipped " + item.Name; return true;
    }
    public Item? Remove(EquipSlot slot) { equipped.Remove(slot, out var item); return item; }
    public double AttackDamage(Attributes stats)
    {
        var a = this[EquipSlot.MainHand]; var b = this[EquipSlot.OffHand];
        if (a is null && b is null) return stats.Strength + stats.Dexterity;
        var weapons = new[] { a, b }.Where(i => i is not null && i.Weapon != WeaponKind.None).Select(i => i!).ToArray();
        if (weapons.Length == 0) return stats.Strength + stats.Dexterity;
        if (weapons.Length == 2) return weapons.Sum(i => i.FlatDamage + i.DualScore * stats.Scale(i.Scaling));
        var w = weapons[0]; var other = ReferenceEquals(a, w) ? b : a;
        if (w.Weapon == WeaponKind.Fist && other is null)
            return w.FlatDamage + w.DualScore * stats.Scale(w.Scaling) + .5 * (stats.Strength + stats.Dexterity);
        return w.FlatDamage + (other is null ? 1 : w.MainScore) * stats.Scale(w.Scaling);
    }
}
