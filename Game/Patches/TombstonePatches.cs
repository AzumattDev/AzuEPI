using AzuEPI.Core.InventoryHandlers;
using AzuExtendedPlayerInventory;

namespace AzuEPI.Game.Patches;

public class TombstonePatches
{
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Awake))]
    private static class TombStoneAwakePatch
    {
        private static void Prefix(TombStone __instance)
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Awake");

            int height = Layout.BaseInventoryHeight + ExtraRows.Value + (AddEquipmentRow.Value.isOn() ? API.GetAddedRows(__instance.GetComponent<Container>().m_width) : 0);

            __instance.GetComponent<Container>().m_height = height;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    private static class TombStoneInteractPatch
    {
        private static void Prefix(TombStone __instance, Container ___m_container)
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Interact");
            int num = Layout.BaseInventoryHeight + ExtraRows.Value + (AddEquipmentRow.Value.isOn() ? API.GetAddedRows(__instance.GetComponent<Container>().m_width) : 0);
            __instance.GetComponent<Container>().m_height = num;
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
        private static bool _playerCurrentPickupState;

        private static void Prefix(out int __state)
        {
            _playerCurrentPickupState = Player.m_enableAutoPickup;
            Player.m_enableAutoPickup = false; // Temporarily disable auto pickup to prevent NRE
            __state = AddEquipmentRow.Value.isOn() ? API.GetAddedRows(Player.m_localPlayer.m_inventory.m_width) : 0;
            Player.m_localPlayer.m_maxCarryWeight += 150f;
            Player.m_localPlayer.m_inventory.m_height += __state;
        }

        private static void Postfix()
        {
            InventoryHealth.InventoryFix();
        }

        private static void Finalizer(int __state)
        {
            Player.m_enableAutoPickup = _playerCurrentPickupState;
            Player.m_localPlayer.m_maxCarryWeight -= 150f;
            Player.m_localPlayer.m_inventory.m_height -= __state;
        }
    }
}