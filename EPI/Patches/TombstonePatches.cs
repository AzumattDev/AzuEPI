using HarmonyLib;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class TombstonePatches
{
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    private static class ContainerAwakePatch
    {
        private static void Prefix(Container __instance)
        {
            // Patch tombstone container to always fit player inventory even with custom tombstone container size
            if (!__instance.GetComponent<TombStone>())
                return;

            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Awake");

            int targetHeight = ExtendedPlayerInventory.GetTargetInventoryHeight(ExtendedPlayerInventory.InventorySizeFull, __instance.m_width);
            // Let it be if height is sufficient
            if (targetHeight > __instance.m_height)
                __instance.m_height = targetHeight;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    private static class TombStoneInteractPatch
    {
        private static void Prefix(Container ___m_container)
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Interact");

            int targetHeight = ExtendedPlayerInventory.GetTargetInventoryHeight(ExtendedPlayerInventory.InventorySizeFull, ___m_container.m_width);
            
            if (targetHeight > ___m_container.m_height)
            {
                ___m_container.m_height = targetHeight;
                ___m_container.m_inventory.m_height = targetHeight;
            }

            string base64String = ___m_container.m_nview.GetZDO().GetString(ZDOVars.s_items);
            if (string.IsNullOrEmpty(base64String))
                return;

            ZPackage pkg = new(base64String);
            ___m_container.m_loading = true;
            ___m_container.m_inventory.Load(pkg);
            ___m_container.m_loading = false;
            ___m_container.m_lastRevision = ___m_container.m_nview.GetZDO().DataRevision;
            ___m_container.m_lastDataString = base64String;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
    private static class TemporarilyIncreaseCarryWeight
    {
        static float megingjordCarryWeight = 0f;
        static bool playerCurrentPickupState = false;

        private static void Prefix()
        {
            playerCurrentPickupState = Player.m_enableAutoPickup;
            Player.m_enableAutoPickup = false; // Temporarily disable auto pickup to prevent NRE.   shudnal: Game version 0.218.21 is it still needed?

            if (megingjordCarryWeight == 0f)
                megingjordCarryWeight = (ObjectDB.instance.GetStatusEffect("BeltStrength".GetStableHashCode()) as SE_Stats)?.m_addMaxCarryWeight ?? 0f;

            Player.m_localPlayer.m_maxCarryWeight += megingjordCarryWeight;
        }
        private static void Postfix() => ExtendedPlayerInventory.CheckPlayerInventoryItemsOverlappingOrOutOfGrid();
        private static void Finalizer()
        {
            Player.m_enableAutoPickup = playerCurrentPickupState;
            Player.m_localPlayer.m_maxCarryWeight -= megingjordCarryWeight;
        }
    }
}