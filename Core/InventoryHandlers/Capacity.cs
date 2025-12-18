namespace AzuEPI.Core.InventoryHandlers;

public class Capacity
{
    public static int FreeStackSpace(Inventory inv, ItemDrop.ItemData item)
    {
        int freeStackSpace = 0;
        foreach (ItemDrop.ItemData? it in inv.m_inventory)
        {
            if (it.m_shared.m_name != item.m_shared.m_name) continue;
            if (it.m_worldLevel != item.m_worldLevel) continue;
            if (item.m_shared.m_maxQuality > 1 && it.m_quality != item.m_quality) continue;
            if (it.m_stack < it.m_shared.m_maxStackSize)
                freeStackSpace += (it.m_shared.m_maxStackSize - it.m_stack);
        }

        return freeStackSpace;
    }

    // 2) count *empty* normal cells only in vanilla area
    public static int FreeNormalCells(Inventory inv)
    {
        int normalRows = Layout.NormalRows(inv);
        int normalUsed = inv.m_inventory.Count(i => i.m_gridPos.y < normalRows);
        int normalFreeCells = (normalRows * inv.GetWidth()) - normalUsed;
        return normalFreeCells;
    }

    public static int FreeQuickCells(Inventory inv)
    {
        int quickFreeCells = 0;
        foreach (Vector2i p in inv.EnumerateQuickCells())
            if (inv.GetItemAt(p.x, p.y) == null)
                ++quickFreeCells;
        return quickFreeCells;
    }

    public static int FreeValidEquipmentCells(Inventory inv, ItemDrop.ItemData item)
    {
        int extendedFreeCells = 0;

        int totalSlots = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
        int quickCount = Hotkeys.Length;
        int equipCount = totalSlots - quickCount;

        for (int i = 0; i < equipCount; ++i)
        {
            if (InventoryGuiPatches.UpdateInventory_Patch.slots[i] is not Model.EquipmentSlot es)
                continue;

            if (es.Valid == null || !es.Valid(item))
                continue;

            Vector2i pos = inv.EpiIndexToGridPos(i);
            if (inv.GetItemAt(pos.x, pos.y) == null)
                ++extendedFreeCells;
        }

        return extendedFreeCells;
    }

    public static int FreeEquipmentCells(Inventory inv)
    {
        int extendedFreeCells = 0;
        foreach (Vector2i p in inv.EnumerateEquipmentCells())
            if (inv.GetItemAt(p.x, p.y) == null)
                ++extendedFreeCells;
        return extendedFreeCells;
    }
}