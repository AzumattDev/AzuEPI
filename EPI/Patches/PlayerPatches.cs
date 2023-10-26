using AzuExtendedPlayerInventory.EPI.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class PlayerPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class PlayerAwakePatch
    {
        private static void Prefix(Player __instance, Inventory ___m_inventory)
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("Player_Awake");

            int height = 4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value + (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On ? API.GetAddedRows(__instance.m_inventory.GetWidth()) : 0);
            __instance.m_inventory.m_height = height;
            __instance.m_tombstone.GetComponent<Container>().m_height = height;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    static class InventoryGuiAwakePatch
    {
        static void Postfix(Player __instance)
        {
            if (Player.m_localPlayer == null)
                return;
            var playerItems = Player.m_localPlayer.GetInventory().GetAllItems();
            // Print them to the console
            foreach (var item in playerItems)
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Item: {item.m_shared.m_name} - {item.m_stack}");
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    private static class PlayerUpdatePatch
    {
        private static void Postfix(Player __instance, ref Inventory ___m_inventory)
        {
            int width = ___m_inventory.GetWidth();
            int height = 4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value + (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On ? API.GetAddedRows(width) : 0);
            ___m_inventory.m_height = height;
            __instance.m_tombstone.GetComponent<Container>().m_height = height;
            if (Utilities.Utilities.IgnoreKeyPresses(true) || AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
                return;

            int hotkey = 0;
            while (!AzuExtendedPlayerInventoryPlugin.Hotkeys[hotkey].Value.IsKeyDown())
            {
                if (++hotkey == AzuExtendedPlayerInventoryPlugin.Hotkeys.Length)
                {
                    return;
                }
            }

            int index = (4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value) * width + InventoryGuiPatches.UpdateInventory_Patch.slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length + hotkey;
            ItemDrop.ItemData itemAt = ___m_inventory.GetItemAt(index % width, index / width);
            if (itemAt == null)
                return;
            __instance.UseItem(null, itemAt, true);
        }

        private static void CreateTombStone()
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Height {Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height}");
            GameObject gameObject = Object.Instantiate(Player.m_localPlayer.m_tombstone, Player.m_localPlayer.GetCenterPoint(), Player.m_localPlayer.transform.rotation);
            TombStone component = gameObject.GetComponent<TombStone>();
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Height {gameObject.GetComponent<Container>().m_height}");
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Inv height {gameObject.GetComponent<Container>().GetInventory().GetHeight()}");
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Inv slots {gameObject.GetComponent<Container>().GetInventory().GetEmptySlots()}");
            for (int index = 0; index < gameObject.GetComponent<Container>().GetInventory().GetEmptySlots(); ++index)
                gameObject.GetComponent<Container>().GetInventory().AddItem("SwordBronze", 1, 1, 0, 0L, "");
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"No items: {gameObject.GetComponent<Container>().GetInventory().NrOfItems()}");
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
        }
    }
}