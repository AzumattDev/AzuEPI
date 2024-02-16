using System;
using System.Collections.Generic;
using System.Reflection;
using AzuExtendedPlayerInventory.EPI.Utilities;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

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
        private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
        public static T Clone<T>(T input) where T : notnull => (T)MemberwiseCloneMethod.Invoke(input, Array.Empty<object>());

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
                fromPlayer.m_inventory.MoveAll(QuickSlotInventory);
                foreach (ItemDrop.ItemData? item in QuickSlotInventory.GetAllItems())
                {
                    if (item.m_dropPrefab == null) continue;
                    TryAddItemToInventory(fromPlayer, item, fromPlayer.m_inventory, false);
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
                //fromPlayer.m_inventory.MoveAll(EquipmentSlotInventory);
                foreach (ItemDrop.ItemData? item in EquipmentSlotInventory.GetAllItems())
                {
                    if (item.m_dropPrefab == null) continue;
                    TryAddItemToInventory(fromPlayer, item, fromPlayer.m_inventory);
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

        public static void TryAddItemToInventory(Player player, ItemDrop.ItemData itemData, Inventory fromInventory, bool useItem = true)
        {
            if (player.m_inventory.CanAddItem(itemData))
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Adding {Localization.instance.Localize(itemData.m_shared.m_name)} to inventory");
                fromInventory.RemoveItem(itemData);
                player.m_inventory.AddItem(itemData);

                if (useItem)
                {
                    player.UseItem(player.GetInventory(), itemData, false);
                }
            }
            else
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Dropping {Localization.instance.Localize(itemData.m_shared.m_name)}");
                Transform transform = player.transform;
                ItemDrop itemDrop = ItemDrop.DropItem(itemData, itemData.m_stack, transform.position + transform.forward + transform.up, transform.rotation);
                if (itemDrop == null) return;
                if (itemDrop.m_itemData.m_equipped)
                {
                    itemDrop.m_itemData.m_equipped = false;
                }

                bool pickedUp = player.Pickup(itemDrop.gameObject, false, false);
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

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    static class InventoryGuiOnRightClickItemPatch
    {
        static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
        {
            if (item == null || !Player.m_localPlayer || grid.GetInventory() == null)
                return true;
            Player p = Player.m_localPlayer;
            if (grid.m_inventory == Player.m_localPlayer.GetInventory())
            {
                if (ExtendedPlayerInventory.IsAtEquipmentSlot(p.m_inventory, item, out int which) && (item == p.m_helmetItem || item == p.m_chestItem || item == p.m_legItem || item == p.m_shoulderItem || item == p.m_utilityItem))
                {
                    if (!p.m_inventory.CanAddItem(item))
                    {
                        AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo("Inventory full, blocking item unequip");
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$inventory_full");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}