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

    internal static bool IsEquipmentSlotFree(this Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        var addedRows = API.API.GetAddedRows(inventory.GetWidth());
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null } slot && slot.Valid(item) && !slot.Occupied);
        return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
    }

    internal static bool IsEquipmentSlotFree(this Inventory inventory, out int which)
    {
        var addedRows = API.API.GetAddedRows(inventory.GetWidth());
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is Model.EquipmentSlot { EquipmentSlot: not null, Occupied: false });
        return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
    }

    internal static bool IsQuickSlotFree(this Inventory inventory, out int which)
    {
        var addedRows = API.API.GetAddedRows(inventory.GetWidth());
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is { IsQuickSlot: true, EquipmentSlot: null, Occupied: false });
        return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
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

    internal static bool IsEquipmentCell(this Inventory inv, int x, int y, out int whichSlot)
    {
        whichSlot = -1;
        int li = inv.LinearIndexIntoEpiBlock(x, y);
        if (li < 0) return false;
        if (li >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count) return false;
        if (InventoryGuiPatches.UpdateInventory_Patch.slots[li] is not Model.EquipmentSlot { IsQuickSlot: false }) return false;
        whichSlot = li;
        return true;
    }

    internal static bool IsQuickCell(this Inventory inv, int x, int y)
    {
        int li = inv.LinearIndexIntoEpiBlock(x, y);
        if (li < 0) return false;
        if (li >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count) return false;
        Model.Slot? s = InventoryGuiPatches.UpdateInventory_Patch.slots[li];
        return s is not Model.EquipmentSlot && s is { IsQuickSlot: true };
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

    internal static bool ShouldGuard(this Inventory inv)
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