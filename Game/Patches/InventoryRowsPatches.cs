namespace AzuEPI.Game.Patches;

public class InventoryRowsPatches
{
    [HarmonyPatch(typeof(Player), "EquipInventoryItems")]
    private static class RestoreLoadedInventorySize
    {
        private static void Prefix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            Layout.RefreshVanillaRows(__instance);
            __instance.GetInventory().m_height = API.GetFullHeight(__instance.GetInventory().GetWidth());
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetInventorySize))]
    private static class PlayerSetInventorySizePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance, int rows)
        {
            if (Player.m_localPlayer != __instance) return;
            Inventory inventory = __instance.GetInventory();
            int previousRows = Layout.NormalRows(inventory);
            Layout.SetVanillaRows(rows);
            Layout.ResizeInventory(inventory, previousRows, API.GetFullHeight(inventory.GetWidth()));
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Player __instance)
        {
            if (Player.m_localPlayer != __instance) return;

            InventoryGuiPatches.UpdateInventory_Patch.InvalidateElements();
            Layout.UpdateInventorySize();
            Layout.UpdateContainerPosition();
            InventoryGui.instance?.m_playerGrid?.ResetView();
        }
    }

    // vanilla drops anything past m_height, which would throw the whole EPI tail on the ground
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropInvalidItems))]
    private static class DropInvalidItemsGuardPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Humanoid __instance)
        {
            if (Player.m_localPlayer != __instance) return;

            Inventory inv = __instance.GetInventory();
            if (inv == null) return;

            int full = API.GetFullHeight(inv.GetWidth());
            if (inv.m_height < full) inv.m_height = full;
        }
    }
}
