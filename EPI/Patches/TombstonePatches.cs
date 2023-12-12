using HarmonyLib;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class TombstonePatches
{
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Awake))]
    private static class TombStoneAwakePatch
    {
        private static void Prefix(TombStone __instance)
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Awake");

            int height = 4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value + (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On ? API.GetAddedRows(__instance.GetComponent<Container>().m_width) : 0);

            __instance.GetComponent<Container>().m_height = height;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    private static class TombStoneInteractPatch
    {
        private static void Prefix(TombStone __instance, Container ___m_container)
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Interact");
            int num = 4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value + (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On ? API.GetAddedRows(__instance.GetComponent<Container>().m_width) : 0);
            __instance.GetComponent<Container>().m_height = num;
            string base64String = ___m_container.m_nview.GetZDO().GetString("items");
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
        private static void Prefix() => Player.m_localPlayer.m_maxCarryWeight += 150f;
        private static void Postfix() => Utilities.Utilities.InventoryFix();
        private static void Finalizer() => Player.m_localPlayer.m_maxCarryWeight -= 150f;
    }
}