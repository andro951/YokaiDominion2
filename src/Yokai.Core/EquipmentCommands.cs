namespace Yokai.Core;

/// <summary>Base-storage transactions; removing an attribute item cannot invalidate held weapons.</summary>
public static class EquipmentCommands
{
    public static bool TryUnequip(this GameWorld world, EquipSlot slot, out string reason)
    {
        var unit = world.Player;
        if (!Enum.IsDefined(slot)) { reason = "Unknown equipment slot."; return false; }
        if (!unit.Alive || !world.AtBase(unit)) { reason = "Return to your shrine to store equipment."; return false; }
        var item = unit.Loadout[slot];
        if (item is null) { reason = "That slot is already empty."; return false; }
        unit.Loadout.Remove(slot);
        var requirement = (unit.Loadout[EquipSlot.MainHand]?.StrengthRequired ?? 0)
            + (unit.Loadout[EquipSlot.OffHand]?.StrengthRequired ?? 0);
        if (unit.Stats.Strength < requirement)
        {
            if (!unit.Loadout.TryEquip(item, slot, unit.Innate, out _))
                throw new InvalidOperationException("Could not restore an equipment transaction.");
            reason = "Store a held weapon first; it needs this item's Strength.";
            return false;
        }
        unit.Storage.Add(item);
        unit.Health = Math.Min(unit.Health, unit.MaxHealth);
        unit.Mana = Math.Min(unit.Mana, unit.MaxMana);
        reason = "Stored " + item.Name + " at your shrine.";
        return true;
    }
}
