namespace AzuEPI.Core.InventoryHandlers;

public static class InventoryExtensions
{
    public static bool IsPlayerInventory(this Inventory inv)
    {
        return inv != null && Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory();
    }

    public static void TryAddItemToInventory(this Inventory inventory, ItemDrop.ItemData itemData)
    {
        Vector2i newPos = inventory.FindEmptyQuickAware(itemData, true);
        if (newPos.x >= 0 && newPos.y >= 0)
        {
            Player.m_localPlayer.GetInventory().RemoveItem(itemData);
            itemData.m_gridPos = newPos;
            inventory.AddItem(itemData);
        }
        else
        {
            AzuExtendedPlayerInventoryLogger.LogInfo($"Dropping {Localization.instance.Localize(itemData.m_shared.m_name)} in TryAddItemToInventory");
            Player.m_localPlayer.DropItem(inventory, itemData, itemData.m_stack);
        }
    }

    internal static Vector2i EpiIndexToGridPos(this Inventory inv, int slotIndex)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv); // vanilla rows
        int x = slotIndex % width;
        int y = normalRows + slotIndex / width;
        return new Vector2i(x, y);
    }

    internal static bool IsEquipmentSlotFreeAndItemValid(this Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        which = -1;

        if (slots == null || slots.Count == 0)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug("IsEquipmentSlotFreeAndItemValid: Slots not initialized yet");
            return false;
        }

        AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Checking item '{item.m_shared.m_name}' (Type: {item.m_shared.m_itemType})");

        // Prioritize API-added slots over built-in slots to avoid placing items in generic slots when they have dedicated slots
        which = slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: true } slot && slot.Valid(item) && !slot.Occupied);

        if (which >= 0)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Found API-added slot {which} ({slots[which]?.Name})");
        }
        else
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: No free API-added slot found, checking built-in slots");

            for (int i = 0; i < slots.Count; i++)
            {
                Model.Slot? s = slots[i];
                if (s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: true } slot)
                {
                    bool validates = slot.Valid(item);
                    bool occupied = slot.Occupied;
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"  API Slot {i} ({s.Name}): Validates={validates}, Occupied={occupied}");
                }
            }
        }

        if (which < 0)
        {
            which = slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: false } slot && slot.Valid(item) && !slot.Occupied);

            if (which >= 0)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Found built-in slot {which} ({slots[which]?.Name})");
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: No free built-in slot found either");

                for (int i = 0; i < slots.Count; i++)
                {
                    Model.Slot? s = slots[i];
                    if (s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: false } slot)
                    {
                        bool validates = slot.Valid(item);
                        bool occupied = slot.Occupied;
                        AzuExtendedPlayerInventoryLogger.LogDebugDebug($"  Built-in Slot {i} ({s.Name}): Validates={validates}, Occupied={occupied}");
                    }
                }
            }
        }

        if (which < 0)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: No valid free slot found for '{item.m_shared.m_name}'");
            return false;
        }

        Vector2i pos = inventory.EpiIndexToGridPos(which);

        if (pos.x < 0 || pos.x >= inventory.GetWidth() || pos.y < 0 || pos.y >= inventory.GetHeight())
        {
            AzuExtendedPlayerInventoryLogger.LogWarningDebug($"Calculated equipment slot position ({pos.x}, {pos.y}) is out of inventory bounds ({inventory.GetWidth()}x{inventory.GetHeight()}). Skipping auto-equip.");
            which = -1;
            return false;
        }

        bool isEmpty = inventory.GetItemAt(pos.x, pos.y) == null;
        AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Slot {which} at ({pos.x}, {pos.y}) is {(isEmpty ? "empty" : "occupied")}");

        return isEmpty;
    }

    internal static bool IsEquipmentSlotFree(this Inventory inventory, out int which)
    {
        which = slots.FindIndex(s => s is Model.EquipmentSlot { IsQuickSlot: false, EquipmentSlot: not null, Occupied: false });

        if (which < 0)
            return false;

        Vector2i pos = inventory.EpiIndexToGridPos(which);
        return inventory.GetItemAt(pos.x, pos.y) == null;
    }

    internal static bool IsQuickSlotFree(this Inventory inventory, out int which)
    {
        which = slots.FindIndex(s => s is { IsQuickSlot: true, EquipmentSlot: null, Occupied: false });

        if (which < 0)
            return false;

        Vector2i pos = inventory.EpiIndexToGridPos(which);
        return inventory.GetItemAt(pos.x, pos.y) == null;
    }

    internal static bool IsAtEquipmentSlot(this Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        int normalRows = Layout.NormalRows(inventory);
        if (AddEquipmentRow.Value.isOff() || item.m_gridPos.y < normalRows || (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x >= slots.Count - QuickSlotsAmount.Value)
        {
            which = -1;
            return false;
        }

        which = (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x;
        return true;
    }

    internal static bool IsAtQuickSlot(this Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        int normalRows = Layout.NormalRows(inventory);
        if (AddEquipmentRow.Value.isOff() || item.m_gridPos.y < normalRows || (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x < slots.Count - QuickSlotsAmount.Value)
        {
            which = -1;
            return false;
        }

        which = (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x;
        return true;
    }

    internal static bool IsHiddenCell(this Inventory inv, int x, int y)
    {
        int li = inv.LinearIndexIntoEpiBlock(x, y);
        return li >= 0 && li >= slots.Count;
    }

    private static int LinearIndexIntoEpiBlock(this Inventory inv, int x, int y)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv); // vanilla rows only
        if (y < normalRows) return -1;

        int baseLinear = normalRows * width;
        int linear = y * width + x;
        return linear - baseLinear;
    }

    internal static IEnumerable<Vector2i> EnumerateQuickCells(this Inventory inv)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv);

        int firstLinear = normalRows * width;
        int total = slots.Count;

        int quickCount = QuickSlotsAmount.Value;
        int quickStart = total - quickCount;
        for (int i = quickStart; i < total; ++i)
        {
            int li = firstLinear + i;
            yield return new Vector2i(li % width, li / width);
        }
    }

    internal static IEnumerable<Vector2i> EnumerateEquipmentCells(this Inventory inv)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv);

        int firstLinear = normalRows * width;
        int total = slots.Count;
        int quickCount = QuickSlotsAmount.Value;
        int equipmentCount = total - quickCount;

        for (int i = 0; i < equipmentCount; ++i)
        {
            int li = firstLinear + i;
            yield return new Vector2i(li % width, li / width);
        }
    }

    internal static bool TryFindEmptyQuickCell(this Inventory inv, out Vector2i pos)
    {
        foreach (Vector2i p in EnumerateQuickCells(inv))
        {
            if (inv.GetItemAt(p.x, p.y) != null) continue;
            pos = p;
            return true;
        }

        pos = new Vector2i(-1, -1);
        return false;
    }

    internal static bool ShouldProtectInventorySlots(this Inventory inv)
    {
        return Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory() && AddEquipmentRow.Value.isOn();
    }

    public static Vector2i FindEmptyQuickAware(this Inventory inv, bool topFirst)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv);

        if (topFirst)
        {
            for (int y = 0; y < normalRows; ++y)
            for (int x = 0; x < width; ++x)
                if (inv.GetItemAt(x, y) == null)
                    return new Vector2i(x, y);
        }
        else
        {
            for (int y = normalRows - 1; y >= 0; --y)
            for (int x = 0; x < width; ++x)
                if (inv.GetItemAt(x, y) == null)
                    return new Vector2i(x, y);
        }

        if (inv.TryFindEmptyQuickCell(out Vector2i q))
            return q;

        return new Vector2i(-1, -1);
    }

    public static Vector2i FindEmptyQuickAware(this Inventory inv, ItemDrop.ItemData item, bool topFirst)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv);

        if (topFirst)
        {
            for (int y = 0; y < normalRows; ++y)
            for (int x = 0; x < width; ++x)
                if (inv.GetItemAt(x, y) == null)
                    return new Vector2i(x, y);
        }
        else
        {
            for (int y = normalRows - 1; y >= 0; --y)
            for (int x = 0; x < width; ++x)
                if (inv.GetItemAt(x, y) == null)
                    return new Vector2i(x, y);
        }

        if (inv.TryFindEmptyQuickCell(out Vector2i q))
            return q;

        int height = inv.GetHeight();
        for (int y = normalRows; y < height; ++y)
        for (int x = 0; x < width; ++x)
        {
            if (inv.GetItemAt(x, y) != null) continue;

            Vector2i pos = new(x, y);
            if (API.TryGetSlotIndexAtGridPos(inv, pos, out int slotIndex))
            {
                if (API.SlotValidates(slotIndex, item))
                    return pos;
            }
            else
            {
                return pos;
            }
        }

        return new Vector2i(-1, -1);
    }
}