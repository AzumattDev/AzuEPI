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
        public const string Sentinel = "<|>";
        public static Inventory QuickSlotInventory = new Inventory(nameof(QuickSlotInventory), null, 3, 1);
        public static Inventory EquipmentSlotInventory = new Inventory(nameof(EquipmentSlotInventory), null, 5, 1);

        static void Postfix(Player __instance)
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer != __instance)
                return;

            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out var RandyEAQ))
            {
                Load(__instance);
            }
        }

        public static void Load(Player fromPlayer)
        {
            if (fromPlayer == null)
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogError("Tried to load an ExtendedPlayerData with a null player!");
                return;
            }

            LoadValue(fromPlayer, "ExtendedPlayerData", out string init);

            if (LoadValue(fromPlayer, "QuickSlotInventory", out string quickSlotData))
            {
                ZPackage pkg = new ZPackage(quickSlotData);
                QuickSlotInventory.Load(pkg);
                //fromPlayer.m_inventory.MoveAll(QuickSlotInventory);
                foreach (ItemDrop.ItemData? item in QuickSlotInventory.GetAllItems())
                {
                    if (item.m_dropPrefab != null)
                    {
                        SpawnAndPickupItems(fromPlayer, item, item.m_dropPrefab.name, item.m_stack, false);
                    }
                }


                // Clear QuickSlotInventory after moving items
                QuickSlotInventory.RemoveAll();

                // Update saved state of QuickSlotInventory
                pkg = new ZPackage();
                QuickSlotInventory.Save(pkg);
                SaveValue(fromPlayer, "QuickSlotInventory", pkg.GetBase64());
            }

            if (LoadValue(fromPlayer, "EquipmentSlotInventory", out string equipSlotData))
            {
                ZPackage pkg = new ZPackage(equipSlotData);
                EquipmentSlotInventory.Load(pkg);
                fromPlayer.m_inventory.MoveAll(EquipmentSlotInventory);
                foreach (ItemDrop.ItemData? item in EquipmentSlotInventory.GetAllItems())
                {
                    if (item.m_dropPrefab != null)
                        SpawnAndPickupItems(fromPlayer, item, item.m_dropPrefab.name, item.m_stack);
                }

                // Clear EquipmentSlotInventory after moving items
                EquipmentSlotInventory.RemoveAll();

                // Update saved state of EquipmentSlotInventory
                pkg = new ZPackage();
                EquipmentSlotInventory.Save(pkg);
                SaveValue(fromPlayer, "EquipmentSlotInventory", pkg.GetBase64());
            }
        }

        private static bool LoadValue(Player player, string key, out string value)
        {
            if (player.m_customData.TryGetValue(key, out value))
                return true;

            var foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (!foundInKnownTexts)
                key = Sentinel + key;
            foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (foundInKnownTexts)
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning("Loaded data from knownTexts. Will be converted to customData on save.");

            return foundInKnownTexts;
        }

        private static void SaveValue(Player player, string key, string value)
        {
            if (player.m_knownTexts.ContainsKey(key))
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning("Found KnownText for save data, converting to customData");
                player.m_knownTexts.Remove(key);
            }

            if (player.m_customData.ContainsKey(key))
                player.m_customData[key] = value;
            else
                player.m_customData.Add(key, value);
        }

        public static void SpawnAndPickupItems(Player player, ItemDrop.ItemData itemData, string prefabName, int count, bool useItem = true)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab == null)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"Missing object {prefabName}", 0, null);
                return;
            }

            var transform = player.transform;
            Vector3 spawnPosition = transform.position + transform.forward * 2f + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.5f;
            GameObject itemObj = UnityEngine.Object.Instantiate(prefab, spawnPosition, Quaternion.identity);

            // Additional setup for the item (like setting quality, level, etc.)

            ItemDrop itemDrop = itemObj.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                if (itemData.m_equipped)
                {
                    itemData.m_equipped = false;
                }

                itemDrop.m_itemData = itemData.Clone();
                bool pickedUp = player.Pickup(itemObj, false, false);
                if (pickedUp && useItem)
                {
                    player.UseItem(player.GetInventory(), itemDrop.m_itemData, false);
                }
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