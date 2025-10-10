namespace AzuEPI.Core.Slots;

internal static class SlotAcceptRules
{
    public static bool QuickslotAccepts(ItemDrop.ItemData item) => true; // TODO: Currently accepts anything, maybe later restrict to usable items or by api option?

    public static bool CanItemGoToSlot(Model.Slot slot, ItemDrop.ItemData item)
    {
        if (slot is Model.EquipmentSlot { IsAPIAdded: true } es)
            return es.Valid(item);
        if (slot is Model.EquipmentSlot es2)
            return es2.Valid(item);

        if (slot.IsQuickSlot)
            return QuickslotAccepts(item);
        return true;
    }
}