using System;
using HarmonyLib;
using UnityEngine;

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
        private static void Prefix() => Player.m_localPlayer.m_maxCarryWeight += 150f;
        private static void Postfix() => Utilities.Utilities.InventoryFix();
        private static void Finalizer() => Player.m_localPlayer.m_maxCarryWeight -= 150f;
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
    public static class PlayerFixedUpdatePatch
    {
        private static bool Prefix(Player __instance)
        {
            if (__instance == null || !__instance.m_nview.IsOwner())
                return true;
            if (__instance != Player.m_localPlayer) return true;
            try
            {
                __instance.AutoPickup(Time.fixedDeltaTime); // This should fix the issue that Ashlands creates with the FloatingTerrainDummy
                                                            // and tombstone destruction. Patch above exposes an issue
            }
            catch (Exception ex)
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning($"Exception in AutoPickup: {ex.Message}\n{ex.StackTrace}");
            }
            return true;
        }
    }
}