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
    public static class InventoryGui_OnSelectedItem_PreventDragEndIfItemUnfit
    {
        public static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting())
                return true;

            if (!__instance.m_dragGo)
                return true;

            // If the dragged item is unfit for target slot
            if (grid == __instance.m_playerGrid && EquipmentSlots.TryGetSlotIndex(pos, out int slotIndex) && __instance.m_dragItem != null && !EquipmentSlots.IsValidItemForSlot(__instance.m_dragItem, slotIndex))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Prevented dragging {__instance.m_dragItem.m_shared.m_name} into unfit slot {slots[slotIndex]}");
                return false;
            }

            // If drag item is in slot and interchanged item is unfit for dragged item slot
            if (item != null && EquipmentSlots.TryGetItemSlot(__instance.m_dragItem, out int slotIndex1) && !EquipmentSlots.IsValidItemForSlot(item, slotIndex1))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Prevented swapping {__instance.m_dragItem.m_shared.m_name} {slots[slotIndex1]} with unfit item {item.m_shared.m_name}");
                return false;
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    public static class InventoryGrid_DropItem_SwapItemKeepEquipped
    {
        public static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
        {
            ItemDrop.ItemData itemAt = __instance.m_inventory.GetItemAt(pos.x, pos.y);
            if (itemAt == item)
                return true;

            if (__instance.m_inventory != PlayerInventory || __instance != InventoryGui.instance.m_playerGrid)
                return true;

            bool targetEquipment = EquipmentSlots.TryGetSlotIndex(pos, out int targetSlot);

            // If the dropped item is unfit for target slot
            if (item != null && targetEquipment && !EquipmentSlots.IsValidItemForSlot(item, targetSlot))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Prevented dragging {item.m_shared.m_name} {item.m_gridPos} into unfit slot {slots[targetSlot]}");
                return false;
            }

            // If dropped item is in slot and interchanged item is unfit for dragged item slot
            if (fromInventory == PlayerInventory && EquipmentSlots.TryGetItemSlot(item, out int currentSlot) && itemAt != null && !EquipmentSlots.IsValidItemForSlot(itemAt, currentSlot))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Prevented swapping {item.m_shared.m_name} {slots[currentSlot]} with unfit item {itemAt.m_shared.m_name} {pos}");
                return false;
            }

            // If dropped item is fit in slot and inventory is the same - fast swap positions
            if (fromInventory == PlayerInventory && targetEquipment && EquipmentSlots.IsValidItemForSlot(item, targetSlot))
            {
                if (itemAt != null && item != null)
                {
                    bool wasEquipped = Player.m_localPlayer.IsItemEquiped(itemAt) || Player.m_localPlayer.IsItemEquiped(item);

                    Player.m_localPlayer.RemoveEquipAction(itemAt);
                    Player.m_localPlayer.UnequipItem(itemAt, false);
                    Player.m_localPlayer.RemoveEquipAction(item);
                    Player.m_localPlayer.UnequipItem(item, false);

                    AzuExtendedPlayerInventoryLogger.LogInfo($"Item {item.m_shared.m_name} {item.m_gridPos} was swapped with {itemAt.m_shared.m_name} {itemAt.m_gridPos} on InventoryGrid.DropItem");

                    (itemAt.m_gridPos, item.m_gridPos) = (item.m_gridPos, itemAt.m_gridPos);

                    if ((AutoEquip.Value.IsOn() || wasEquipped) && !Player.m_localPlayer.IsItemEquiped(item) && item.IsEquipable() && item.m_durability > 0)
                        Player.m_localPlayer.EquipItem(item, false);
                }
                else if (itemAt == null && item != null)
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"Item {item.m_shared.m_name} {item.m_gridPos} moved into {pos}");
                    item.m_gridPos = pos;
                }

                if (itemAt != null && Player.m_localPlayer.IsItemEquiped(itemAt))
                {
                    Player.m_localPlayer.RemoveEquipAction(itemAt);
                    Player.m_localPlayer.UnequipItem(itemAt, false);
                }

                ItemDrop.ItemData itemSlot = EquipmentSlots.GetItemInSlot(targetSlot);

                if (AutoEquip.Value.IsOn() && itemSlot.IsEquipable() && itemSlot.m_durability > 0)
                    Player.m_localPlayer.EquipItem(itemSlot, false);

                PlayerInventory.Changed();

                __result = true;
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