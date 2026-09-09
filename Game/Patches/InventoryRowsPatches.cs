namespace AzuEPI.Game.Patches;

// 1.0 sells inventory rows at the trader, so the vanilla row count is no longer a constant 4
public class InventoryRowsPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetInventorySize))]
    private static class PlayerSetInventorySizePatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance, int rows)
        {
            if (Player.m_localPlayer != __instance) return;
            Layout.SetVanillaRows(rows);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Player __instance)
        {
            if (Player.m_localPlayer != __instance) return;

            Layout.UpdateInventorySize();
            Layout.UpdateContainerPosition(true);
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
