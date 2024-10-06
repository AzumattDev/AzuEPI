using HarmonyLib;
using static AzuExtendedPlayerInventory.EPI.ExtendedPlayerInventory;

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
    static class InventoryGuiOnSelectedItemPatch
    {
        static void Prefix(InventoryGui __instance, InventoryGrid grid)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting())
                return;

            // If the item is one of the equipped armor pieces, unequip it
            if (__instance.m_dragGo && localPlayer.IsItemEquiped(__instance.m_dragItem))
                if (EquipmentSlots.IsInSlot(grid.m_inventory, __instance.m_dragItem))
                    localPlayer.UnequipItem(__instance.m_dragItem, false);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class InventoryGuiUpdatePatch
    {
        private static void Postfix()
        {
            if (!Player.m_localPlayer)
                return;

            EquipmentSlots.UpdatePlayerInventoryEquipmentSlots();

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
}