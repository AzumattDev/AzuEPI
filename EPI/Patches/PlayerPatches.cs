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
            LogInfo("Player_Awake");

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
                LogInfo($"Adding {Localization.instance.Localize(itemData.m_shared.m_name)} to inventory");
                fromInventory.RemoveItem(itemData);
                player.m_inventory.AddItem(itemData);

                if (useItem)
                {
                    player.UseItem(player.GetInventory(), itemData, false);
                }
            }
            else
            {
                LogInfo($"Dropping {Localization.instance.Localize(itemData.m_shared.m_name)}");
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
            {
                EquipmentSlots.MarkDirty();
                
                if (Player.m_localPlayer.GetExtraUtility(0) != null && !PlayerInventory.ContainsItem(Player.m_localPlayer.GetExtraUtility(0)))
                {
                    Player.m_localPlayer.SetExtraUtility(0, null);
                    Player.m_localPlayer.SetupEquipment();
                }

                if (Player.m_localPlayer.GetExtraUtility(1) != null && !PlayerInventory.ContainsItem(Player.m_localPlayer.GetExtraUtility(1)))
                {
                    Player.m_localPlayer.SetExtraUtility(1, null);
                    Player.m_localPlayer.SetupEquipment();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    private static class Humanoid_UnequipItem_ValidateInventory
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (__instance.GetExtraUtility(0) == item)
            {
                __instance.SetExtraUtility(0, null);
                __instance.SetupEquipment();
            }
            else if (__instance.GetExtraUtility(1) == item)
            {
                __instance.SetExtraUtility(1, null);
                __instance.SetupEquipment();
            }

            if (__instance is Player player && IsValidPlayer(player) && !player.m_isLoading)
                EquipmentSlots.MarkDirty();
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
    private static class Humanoid_UpdateEquipmentStatusEffects_ExtraUtility
    {
        private static void Postfix(Humanoid __instance)
        {
            HashSet<StatusEffect> hashSet = new();

            EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true).DoIf(item => (bool)item.m_shared.m_equipStatusEffect, item => hashSet.Add(item.m_shared.m_equipStatusEffect));

            EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true).DoIf(item => __instance.HaveSetEffect(item), item => hashSet.Add(item.m_shared.m_setStatusEffect));

            foreach (StatusEffect item in hashSet.Where(item => !__instance.m_equipmentStatusEffects.Contains(item)))
                __instance.m_seman.AddStatusEffect(item);

            __instance.m_equipmentStatusEffects.UnionWith(hashSet);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
    private static class Humanoid_GetEquipmentWeight_ExtraUtility
    {
        private static void Postfix(Humanoid __instance, ref float __result)
        {
            if (__instance == Player.m_localPlayer)
                foreach (ItemDrop.ItemData item in EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true))
                    __result += item.m_shared.m_weight;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static class Humanoid_EquipItem_ExtraUtility
    {
        private static readonly ItemDrop.ItemData.ItemType tempType = (ItemDrop.ItemData.ItemType)727;

        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && __instance.m_utilityItem != null)
            {
                if (EquipmentSlots.TryGetItemSlot(item, out int slotIndex) && EquipmentSlots.IsValidItemForSlot(item, slotIndex))
                {
                    item.m_shared.m_itemType = tempType;
                }
                else if (EquipmentSlots.TryFindFreeSlotForItem(item, out int x, out int y))
                {
                    item.m_gridPos = new Vector2i(x, y);
                    item.m_shared.m_itemType = tempType;

                    if (__instance.m_visEquipment && __instance.m_visEquipment.m_isPlayer)
                        item.m_shared.m_equipEffect.Create(__instance.transform.position + Vector3.up, __instance.transform.rotation);
                }
            }
        }

        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result)
        {
            if (item == null || item.m_shared.m_itemType != tempType)
                return;

            item.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;

            if (!EquipmentSlots.ExtraUtilitySlots.TryGetUtilityItemIndex(item, out int utilityIndex))
                return;

            if (__instance.GetExtraUtility(utilityIndex) != null)
                __instance.UnequipItem(__instance.GetExtraUtility(utilityIndex), triggerEquipEffects);

            __instance.SetExtraUtility(utilityIndex, item);

            if (__instance.IsItemEquiped(item))
            {
                item.m_equipped = true;
                __result = true;
            }

            __instance.SetupEquipment();

            if (__instance is Player player && IsValidPlayer(player) && !player.m_isLoading)
                EquipmentSlots.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
    private static class Humanoid_IsItemEquiped_ExtraUtility
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            __result = __result || EquipmentSlots.ExtraUtilitySlots.IsItemEquipped(__instance, item);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UnequipDeathDropItems))]
    private static class Player_UnequipDeathDropItems_ExtraUtility
    {
        private static void Prefix(Humanoid __instance)
        {
            EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true).Do(item => __instance.UnequipItem(item, triggerEquipEffects: false));
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
    private static class Player_GetEquipmentEitrRegenModifier_ExtraUtility
    {
        private static void Postfix(Humanoid __instance, ref float __result)
        {
            foreach (ItemDrop.ItemData item in EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true))
                __result += item.m_shared.m_eitrRegenModifier;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods))]
    private static class Player_ApplyArmorDamageMods_ExtraUtility
    {
        private static void Postfix(Humanoid __instance, ref HitData.DamageModifiers mods)
        {
            foreach (ItemDrop.ItemData item in EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true))
                mods.Apply(item.m_shared.m_damageModifiers);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
    private static class Player_GetBodyArmor_ExtraUtility
    {
        private static void Postfix(Humanoid __instance, ref float __result)
        {
            foreach (ItemDrop.ItemData item in EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true))
                __result += item.GetArmor();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
    private static class Player_UpdateModifiers_ExtraUtility
    {
        private static void Postfix(Player __instance)
        {
            if (Player.s_equipmentModifierSourceFields == null)
                return;

            for (int i = 0; i < __instance.m_equipmentModifierValues.Length; i++)
                foreach (ItemDrop.ItemData item in EquipmentSlots.ExtraUtilitySlots.GetItems(equippedOnly: true))
                    __instance.m_equipmentModifierValues[i] += (float)Player.s_equipmentModifierSourceFields[i].GetValue(item.m_shared);
        }
    }
}