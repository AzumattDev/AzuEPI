using AzuEPI.Core.Slots;
using AzuEPI.Game.Patches;

namespace AzuEPI.Core.InventoryHandlers;

public static class InventoryExtensions
{
    public static bool IsPlayerInventory(this Inventory inv)
    {
        return inv != null && Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory();
    }

    public static void TryAddItemToInventory(this Inventory inventory, ItemDrop.ItemData itemData)
    {
        if (inventory.CanAddItem(itemData))
        {
            Player.m_localPlayer.GetInventory().RemoveItem(itemData);
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
        // Prioritize API-added slots over built-in slots to avoid placing items in generic slots when they have dedicated slots
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: true } slot && slot.Valid(item) && !slot.Occupied);

        if (which < 0)
        {
            which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: false } slot && slot.Valid(item) && !slot.Occupied);
        }

        if (which < 0)
            return false;

        Vector2i pos = inventory.EpiIndexToGridPos(which);
        return inventory.GetItemAt(pos.x, pos.y) == null;
    }

    internal static bool IsEquipmentSlotFree(this Inventory inventory, out int which)
    {
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is Model.EquipmentSlot { IsQuickSlot: false, EquipmentSlot: not null, Occupied: false });

        if (which < 0)
            return false;

        Vector2i pos = inventory.EpiIndexToGridPos(which);
        return inventory.GetItemAt(pos.x, pos.y) == null;
    }

    internal static bool IsQuickSlotFree(this Inventory inventory, out int which)
    {
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is { IsQuickSlot: true, EquipmentSlot: null, Occupied: false });

        if (which < 0)
            return false;

        Vector2i pos = inventory.EpiIndexToGridPos(which);
        return inventory.GetItemAt(pos.x, pos.y) == null;
    }

    internal static bool IsAtEquipmentSlot(this Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        var normalRows = Layout.NormalRows(inventory);
        if (AddEquipmentRow.Value.isOff() || item.m_gridPos.y < normalRows || (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length)
        {
            which = -1;
            return false;
        }

        which = (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x;
        return true;
    }

    internal static bool IsAtQuickSlot(this Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        var normalRows = Layout.NormalRows(inventory);
        if (AddEquipmentRow.Value.isOff() || item.m_gridPos.y < normalRows || (item.m_gridPos.y - normalRows) * inventory.GetWidth() + item.m_gridPos.x < InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length)
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
        return li >= 0 && li >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
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
        int total = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;

        int quickCount = Hotkeys.Length;
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
        int total = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
        int quickCount = Hotkeys.Length;
        int equipmentCount = total - quickCount;

        for (int i = 0; i < equipmentCount; ++i)
        {
            int li = firstLinear + i;
            yield return new Vector2i(li % width, li / width);
        }
    }

    internal static bool TryFindEmptyQuickCell(this Inventory inv, out Vector2i pos)
    {
        foreach (var p in EnumerateQuickCells(inv))
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

        return inv.TryFindEmptyQuickCell(out var q) ? q : new Vector2i(-1, -1);
    }
}