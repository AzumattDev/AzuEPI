using HarmonyLib;
using static AzuExtendedPlayerInventory.EPI.ExtendedPlayerInventory;
using static AzuExtendedPlayerInventory.AzuExtendedPlayerInventoryPlugin;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public static class InventoryGuiPatches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    static class InventoryGuiShowPatch
    {
        static void Postfix()
        {
            if (Player.m_localPlayer == null)
                return;

            EquipmentPanel.UpdatePanel();

            EquipmentSlots.MarkDirty();

            CheckPlayerInventoryItemsOverlappingOrOutOfGrid();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
    static class InventoryGui_OnDestroy_ClearObjects
    {
        static void Postfix()
        {
            EquipmentPanel.ClearPanel();

            DropButton.ClearButton();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    public static class InventoryGui_OnSelectedItem_DragValidation_AutoEquip
    {
        public static bool Prefix(InventoryGui __instance, InventoryGrid grid, ref ItemDrop.ItemData item, ref Vector2i pos, ref Vector2i __state)
        {
            __state = new Vector2i(-1, -1);

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting())
                return true;

            if (!__instance.m_dragGo)
                return true;

            if (grid == __instance.m_playerGrid && EquipmentSlots.TryGetSlotIndex(pos, out int slotIndex))
            {
                // If the dragged item is unfit for target slot
                if (__instance.m_dragItem != null && !EquipmentSlots.IsValidItemForSlot(__instance.m_dragItem, slotIndex))
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"OnSelectedItem Prevented dragging {__instance.m_dragItem.m_shared.m_name} {__instance.m_dragItem.m_gridPos} into unfit slot {slots[slotIndex]}");
                    return false;
                }

                // If item is unequipped and will not be automatically equipped
                if (__instance.m_dragItem != null && AutoEquip.Value.IsOff() && KeepUnequippedInSlot.Value.IsOff() && !Player.m_localPlayer.IsItemEquiped(__instance.m_dragItem))
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"OnSelectedItem Dragging converted into Queued equip action on {__instance.m_dragItem.m_shared.m_name} {__instance.m_dragItem.m_gridPos}");
                    
                    Player.m_localPlayer.QueueEquipAction(__instance.m_dragItem);
                    
                    // Clear item and position to prevent autoequip and unequip
                    item = null;
                    pos = __state;
                    __instance.SetupDragItem(null, null, 1);
                    return false;
                }
            }

            // If drag item is in slot and interchanged item is unfit for dragged item slot
            if (__instance.m_dragItem != null && item != null && EquipmentSlots.TryGetItemSlot(__instance.m_dragItem, out int slotIndex1) && !EquipmentSlots.IsValidItemForSlot(item, slotIndex1))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"OnSelectedItem Prevented swapping {__instance.m_dragItem.m_shared.m_name} {slots[slotIndex1]} with unfit item {item.m_shared.m_name}");
                return false;
            }

            // Save position dragged from to check on postfix
            if (__instance.m_dragInventory == PlayerInventory && __instance.m_dragItem != null)
                __state = __instance.m_dragItem.m_gridPos;

            return true;
        }

        public static void Postfix(InventoryGui __instance, InventoryGrid grid, Vector2i pos, ref Vector2i __state)
        {
            // If dragging is in progress
            if (__instance.m_dragGo)
                return;

            if (pos == __state)
                return;

            if (grid == __instance.m_playerGrid)
                CheckAutoEquip(pos);

            if (__state != new Vector2i(-1, -1))
                CheckAutoEquip(__state);
        }

        private static void CheckAutoEquip(Vector2i pos)
        {
            ItemDrop.ItemData item = PlayerInventory.GetItemAt(pos.x, pos.y);
            if (EquipmentSlots.IsItemAtSlot(item) && AutoEquip.Value.IsOn())
                Player.m_localPlayer.EquipItem(item);
            else
                Player.m_localPlayer.UnequipItem(item);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    public static class InventoryGrid_DropItem_DropPrevention
    {
        public static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos)
        {
            ItemDrop.ItemData itemAt = __instance.m_inventory.GetItemAt(pos.x, pos.y);
            if (itemAt == item)
                return true;

            if (__instance.m_inventory != PlayerInventory && __instance != InventoryGui.instance.m_playerGrid)
                return true;

            bool targetEquipment = EquipmentSlots.TryGetSlotIndex(pos, out int targetSlot) && __instance.m_inventory == PlayerInventory;

            // If the dropped item is unfit for target slot
            if (item != null && targetEquipment && !EquipmentSlots.IsValidItemForSlot(item, targetSlot))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"DropItem Prevented dropping {item.m_shared.m_name} {item.m_gridPos} into unfit slot {slots[targetSlot]}");
                return false;
            }

            // If dropped item is in slot and interchanged item is unfit for dragged item slot
            if (item != null && itemAt != null && fromInventory == PlayerInventory && EquipmentSlots.TryGetItemSlot(item, out int currentSlot)  && !EquipmentSlots.IsValidItemForSlot(itemAt, currentSlot))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"DropItem Prevented swapping {item.m_shared.m_name} {slots[currentSlot]} with unfit item {itemAt.m_shared.m_name} {pos}");
                return false;
            }

            // If item is unequipped and will not be automatically equipped after drop
            if (itemAt == null && item != null && AutoEquip.Value.IsOff() && KeepUnequippedInSlot.Value.IsOff() && targetEquipment)
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"DropItem Prevented dropping {item.m_shared.m_name} {item.m_gridPos} into slot {slots[targetSlot]} with both autoequip and keep unequipped disabled");
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class InventoryGuiUpdatePatch
    {
        private static void Postfix()
        {
            if (!Player.m_localPlayer)
                return;

            if (!IsVisible())
                return;

            EquipmentPanel.UpdateEquipmentBackground();

            DropButton.UpdateButton();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
    internal static class UpdateInventory_Patch
    {
        private static void Postfix()
        {
            EquipmentPanel.UpdateInventorySlots();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    private static class InventoryGui_OnRightClickItem_PreventUnequipOnFullInventory
    {
        static bool Prefix(InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (item == null || !Player.m_localPlayer || grid.GetInventory() == null)
                return true;

            if (grid.m_inventory != PlayerInventory)
                return true;

            if (EquipmentSlots.IsItemAtSlot(item) && item.IsEquipable() && Player.m_localPlayer.IsItemEquiped(item) && KeepUnequippedInSlot.Value.IsOff() && !PlayerInventory.CanAddItem(item))
            {
                AzuExtendedPlayerInventoryLogger.LogDebug("Inventory full, blocking item unequip");
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$inventory_full");
                return false;
            }

            return true;
        }
    }
}