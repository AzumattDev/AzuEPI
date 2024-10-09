using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using static AzuExtendedPlayerInventory.AzuExtendedPlayerInventoryPlugin;
using static AzuExtendedPlayerInventory.EPI.ExtendedPlayerInventory;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class PlayerPatches
{
    private static bool IsValidPlayer(Player player) => player != null && Player.m_localPlayer == player && player.m_nview.IsValid() && player.m_nview.IsOwner();

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class Player_Awake_ResizeInventory
    {
        private static void Prefix(Player __instance)
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("Player_Awake");

            __instance.m_inventory.m_height = InventoryHeightFull;
            __instance.m_tombstone.GetComponent<Container>().m_height = InventoryHeightFull;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class Player_OnSpawned_LoadEAQSData
    {
        public const string Sentinel = "<|>";
        public static Inventory QuickSlotInventory = new(nameof(QuickSlotInventory), null, 3, 1);
        public static Inventory EquipmentSlotInventory = new(nameof(EquipmentSlotInventory), null, 5, 1);
        private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
        public static T Clone<T>(T input) where T : notnull => (T)MemberwiseCloneMethod.Invoke(input, Array.Empty<object>());

        static void Postfix(Player __instance)
        {
            if (!IsValidPlayer(__instance))
                return;

            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out _))
                Load(__instance);
        }

        public static void Load(Player fromPlayer)
        {
            if (fromPlayer == null)
            {
                AzuExtendedPlayerInventoryLogger.LogError("Tried to load an ExtendedPlayerData with a null player!");
                return;
            }

            LoadValue(fromPlayer, "ExtendedPlayerData", out _);

            if (LoadValue(fromPlayer, "QuickSlotInventory", out string quickSlotData))
            {
                ZPackage pkg = new(quickSlotData);
                QuickSlotInventory.Load(pkg);
                fromPlayer.m_inventory.MoveAll(QuickSlotInventory);
                foreach (ItemDrop.ItemData item in QuickSlotInventory.GetAllItems().Where(item => item != null))
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
                ZPackage pkg = new(equipSlotData);
                EquipmentSlotInventory.Load(pkg);
                //fromPlayer.m_inventory.MoveAll(EquipmentSlotInventory);
                foreach (ItemDrop.ItemData item in EquipmentSlotInventory.GetAllItems().Where(item => item != null))
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

            var foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out _);
            if (!foundInKnownTexts)
                key = Sentinel + key;

            foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (foundInKnownTexts)
                AzuExtendedPlayerInventoryLogger.LogWarning("Loaded data from knownTexts. Will be converted to customData on save.");

            return foundInKnownTexts;
        }

        private static void SaveValue(Player player, string key, string value)
        {
            if (player.m_knownTexts.ContainsKey(key))
            {
                AzuExtendedPlayerInventoryLogger.LogWarning("Found KnownText for save data, converting to customData");
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
                AzuExtendedPlayerInventoryLogger.LogInfo($"Adding {Localization.instance.Localize(itemData.m_shared.m_name)} to inventory");
                fromInventory.RemoveItem(itemData);
                player.m_inventory.AddItem(itemData);

                if (useItem)
                {
                    player.UseItem(player.GetInventory(), itemData, false);
                }
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Dropping {Localization.instance.Localize(itemData.m_shared.m_name)}");
                Transform transform = player.transform;
                ItemDrop itemDrop = ItemDrop.DropItem(itemData, itemData.m_stack, transform.position + transform.forward + transform.up, transform.rotation);
                if (itemDrop == null) return;
                itemDrop.m_itemData.m_equipped = false;

                bool pickedUp = player.Pickup(itemDrop.gameObject, false, false);
                if (pickedUp && useItem)
                {
                    player.UseItem(player.GetInventory(), itemDrop.m_itemData, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    private static class Player_Update_UpdateInventoryHeightAndQuickSlots
    {
        private static Container tombstoneContainer = null!;

        private static void Postfix(Player __instance, Inventory ___m_inventory)
        {
            ___m_inventory.m_height = InventoryHeightFull;

            tombstoneContainer ??= __instance.m_tombstone.GetComponent<Container>();
            tombstoneContainer.m_height = ___m_inventory.m_height;

            if (!IsValidPlayer(__instance))
                return;

            QuickSlots.UpdateItemUse();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
    private static class Player_OnInventoryChanged_ValidateInventory
    {
        private static void Postfix(Player __instance)
        {
            if (IsValidPlayer(__instance) && !__instance.m_isLoading)
                EquipmentSlots.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    private static class Humanoid_UnequipItem_ValidateInventory
    {
        private static void Postfix(Humanoid __instance)
        {
            if (__instance is Player player && IsValidPlayer(player) && !player.m_isLoading)
                EquipmentSlots.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static class Humanoid_EquipItem_ValidateInventory
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && __instance.m_utilityItem != null)
            {
                if (EquipmentSlots.TryGetItemSlot(item, out int slotIndex) && EquipmentSlots.IsValidItemForSlot(item, slotIndex))
                {
                    item.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)727;
                }
                else if (EquipmentSlots.TryFindFreeSlotForItem(item, out int x, out int y))
                {
                    item.m_gridPos = new Vector2i(x, y);
                    item.m_shared.m_itemType = (ItemDrop.ItemData.ItemType)727;
                }
            }
        }

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (item.m_shared.m_itemType == (ItemDrop.ItemData.ItemType)727)
                item.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;

            if (__instance.IsItemEquiped(item))
            {
                item.m_equipped = true;
                __result = true;
            }

            if (__instance is Player player && IsValidPlayer(player) && !player.m_isLoading)
                EquipmentSlots.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
    private static class Humanoid_IsItemEquiped_SimilarSlots
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            __result = __result || EquipmentSlots.TryGetItemSlot(item, out int slotIndex) && slots[slotIndex] is EquipmentSlot slot && slot.Get == null;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
    private static class Humanoid_UnequipAllItems_UnequipmEquipmentSlots
    {
        private static void Postfix(Humanoid __instance)
        {
            EquipmentSlots.GetItems().Do(item => __instance.UnequipItem(item));
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
    private static class Humanoid_UpdateEquipmentStatusEffects_EquipmentSlotsEquipEffect
    {
        private static void Prefix(Humanoid __instance, ref HashSet<StatusEffect> __state)
        {
            HashSet<StatusEffect> hashSet = new();

            EquipmentSlots.GetItems().DoIf(item => (bool)item.m_shared.m_equipStatusEffect, item => hashSet.Add(item.m_shared.m_equipStatusEffect));

            EquipmentSlots.GetItems().DoIf(item => __instance.HaveSetEffect(item), item => hashSet.Add(item.m_shared.m_setStatusEffect));

            foreach (StatusEffect equipmentStatusEffect in __instance.m_equipmentStatusEffects)
            {
                if (!hashSet.Contains(equipmentStatusEffect))
                {
                    __instance.m_seman.RemoveStatusEffect(equipmentStatusEffect.NameHash());
                }
            }

            __state = hashSet;
        }

        private static void Postfix(Humanoid __instance, HashSet<StatusEffect> __state)
        {
            foreach (StatusEffect item in __state)
            {
                if (!__instance.m_equipmentStatusEffects.Contains(item))
                {
                    __instance.m_seman.AddStatusEffect(item);
                }
            }

            __instance.m_equipmentStatusEffects.UnionWith(__state);
        }
    }
}