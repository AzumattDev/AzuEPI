using HarmonyLib;

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

            Utilities.Utilities.InventoryFix();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
    static class InventoryGui_OnDestroy_ClearObjects
    {
        static void Postfix()
        {
            ExtendedPlayerInventory.EquipmentPanel.ClearPanel();

            ExtendedPlayerInventory.DropButton.ClearButton();
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

            if (__instance.m_dragGo && localPlayer.IsItemEquiped(__instance.m_dragItem))
                // If the item is one of the equipped armor pieces, unequip it
                if (ExtendedPlayerInventory.EquipmentSlots.IsSlot(grid.m_inventory, __instance.m_dragItem))
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

            ExtendedPlayerInventory.EquipmentSlots.UpdatePlayerInventoryEquipmentSlots();

            if (!ExtendedPlayerInventory.IsVisible())
                return;

            ExtendedPlayerInventory.EquipmentPanel.UpdateEquipmentBackground();

            ExtendedPlayerInventory.DropButton.UpdateButton();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
    internal static class UpdateInventory_Patch
    {
        private static void Postfix()
        {
            ExtendedPlayerInventory.EquipmentPanel.UpdateInventorySlots();
        }
    }
}