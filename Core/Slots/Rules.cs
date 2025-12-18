namespace AzuEPI.Core.Slots;

internal static class SlotAcceptRules
{
    public static bool QuickslotAccepts(ItemDrop.ItemData item) => true; // TODO: Currently accepts anything, maybe later restrict to usable items or by api option?

    public static bool HasDedicatedAPISlot(ItemDrop.ItemData item)
    {
        if (item == null) return false;

        return InventoryGuiPatches.UpdateInventory_Patch.slots.Any(s => s is Model.EquipmentSlot { IsAPIAdded: true, Valid: not null } slot && slot.Valid(item));
    }

    public static bool CanItemGoToSlot(Model.Slot slot, ItemDrop.ItemData? item)
    {
        if (item == null)
            return true;
        if (slot is Model.EquipmentSlot { IsAPIAdded: true, IsQuickSlot: false } es)
            return es.Valid != null && es.Valid(item);
        if (slot is Model.EquipmentSlot { IsQuickSlot: false } es2)
            return es2.Valid != null && es2.Valid(item);

        if (slot.IsQuickSlot)
            return QuickslotAccepts(item);
        return true;
    }
}