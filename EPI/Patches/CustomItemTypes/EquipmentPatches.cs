using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace AzuEPI.EPI.Patches.CustomItemTypes;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    [HarmonyPriority(Priority.Last)]
    static void Postfix(ZNetScene __instance)
    {
        var itemDrop = __instance.GetPrefab("BeltStrength").GetComponent<ItemDrop>();
        if (itemDrop != null)
        {
            DoTheThing(itemDrop);
        }

        var wishbone = __instance.GetPrefab("Wishbone").GetComponent<ItemDrop>();
        if (wishbone != null)
        {
            DoTheThing(wishbone);
        }
    }

    private static void DoTheThing(ItemDrop itemDrop)
    {
        // Directly setting the custom item type based on the utility name.
        if (CustomItemTypeManager.TryGetItemType("Utility2", out var itemType))
        {
            itemDrop.m_itemData.m_shared.m_itemType = itemType;
        }
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
internal static class ItemDropItemDataPatch
{
    static void Postfix(ref ItemDrop.ItemData? __instance, ref bool __result)
    {
        __result = __result || IsSecondUtility(__instance);
    }

    internal static bool IsSecondUtility(ItemDrop.ItemData? item)
    {
        // Ensure item is not null and m_dropPrefab is not null.
        if (item == null || item.m_dropPrefab == null) return false;

        // Get the ItemDrop component and check for the custom item type "Utility2".
        var itemDropComponent = item.m_dropPrefab.GetComponent<ItemDrop>();
        if (itemDropComponent == null) return false; // Ensure the component exists.

        bool isCustomTypeUtility2 = CustomItemTypeManager.CheckCustomItemType(itemDropComponent, "Utility2");

        // Check if the prefab's name matches and if it's of the custom type "Utility2".
        return isCustomTypeUtility2 && item.m_dropPrefab.name is "BeltStrength" or "Wishbone";
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
static class HumanoidEquipItemPatch
{
    static void Postfix(ref Humanoid __instance, ref bool __result, ItemDrop.ItemData? item, bool triggerEquipEffects = true)
    {
        if (!__instance.IsPlayer()) return;
        if (ItemDropItemDataPatch.IsSecondUtility(item))
        {
            EquipSecondUtility(__instance, item, triggerEquipEffects);
        }
    }

    private static void EquipSecondUtility(Humanoid __instance, ItemDrop.ItemData? item, bool triggerEquipEffects)
    {
        // If the user already has a backpack equipped, unequip it
        List<ItemDrop.ItemData> secondUtils = __instance.m_inventory.GetEquippedItems().Where(ItemDropItemDataPatch.IsSecondUtility).ToList();
        foreach (ItemDrop.ItemData secondUtil in secondUtils)
        {
            __instance.UnequipItem(secondUtil, triggerEquipEffects);
        }

        if (item != null && ItemDropItemDataPatch.IsSecondUtility(item) && !item.m_equipped)
        {
            item.m_equipped = true;
            __instance.m_visEquipment.AttachArmor(item.m_dropPrefab.name.GetStableHashCode()); // Adds the armor to the player, but hard to remove after. Even when prefixing unequip with a check for backpacks, it still doesn't remove armor the way I want.
        }


        __instance.SetupEquipment();
        if (triggerEquipEffects)
            __instance.TriggerEquipEffect(item);
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
static class HumanoidUnequipItemPatch
{
    static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
    {
        if (!__instance.IsPlayer()) return;
        if (__instance.m_inventory != null)
        {
            List<ItemDrop.ItemData> secondUtils = __instance.m_inventory.GetEquippedItems().Where(ItemDropItemDataPatch.IsSecondUtility).ToList();
            foreach (ItemDrop.ItemData secondUtil in secondUtils)
            {
                if (item != null && item == secondUtil)
                {
                    secondUtil.m_equipped = false;
                }
            }
        }

        __instance.SetupEquipment();
        __instance.UpdateEquipmentStatusEffects();
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
static class HumaPatch
{
    static void Postfix(ref Humanoid __instance, ref bool __result, ItemDrop.ItemData? item)
    {
        if (!__instance.IsPlayer()) return;
        if (!ItemDropItemDataPatch.IsSecondUtility(item))
            return;
        __result = __instance.m_inventory.GetEquippedItems().Contains(item);
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
static class HumanoidUnequipAllItemsPatch
{
    static void Postfix(ref Humanoid __instance)
    {
        if (!__instance.IsPlayer()) return;
        foreach (ItemDrop.ItemData equippedItem in __instance.m_inventory.GetEquippedItems())
        {
            if (ItemDropItemDataPatch.IsSecondUtility(equippedItem))
            {
                __instance.UnequipItem(equippedItem, false);
            }
        }
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
static class UpdateEquipmentStatusEffects_Patch
{
    static void Prefix(Humanoid __instance, ItemDrop.ItemData ___m_utilityItem, SEMan ___m_seman)
    {
        try
        {
            if (!__instance.IsPlayer())
                return;
            var list = __instance.GetInventory().GetAllItems().FindAll(i => !i.m_equipped && i.m_dropPrefab && ItemDropItemDataPatch.IsSecondUtility(i) && i.m_shared.m_equipStatusEffect);
            var list2 = __instance.GetInventory().GetEquippedItems().FindAll(i => i.m_dropPrefab && ItemDropItemDataPatch.IsSecondUtility(i) && i.m_shared.m_equipStatusEffect);

            foreach (var item in list)
            {
                foreach (StatusEffect statusEffect in ___m_seman.m_statusEffects)
                {
                    if (statusEffect.name == item.m_shared.m_equipStatusEffect.name && (___m_utilityItem is null || ___m_utilityItem.m_shared.m_equipStatusEffect.name != statusEffect.name) && !list2.Exists(i => i.m_shared.m_equipStatusEffect.name == statusEffect.name))
                    {
                        ___m_seman.RemoveStatusEffect(statusEffect.NameHash(), false);
                    }
                }
            }
        }
        catch
        {
            //Dbgl($"Error: {Environment.StackTrace}");
        }
    }

    static void Postfix(Humanoid __instance, ItemDrop.ItemData ___m_utilityItem, SEMan ___m_seman)
    {
        try
        {
            if (!__instance.IsPlayer())
                return;
            var list = __instance.GetInventory().GetEquippedItems().FindAll(i => i.m_dropPrefab && ItemDropItemDataPatch.IsSecondUtility(i) && i.m_shared.m_equipStatusEffect);

            foreach (var item in list)
            {
                ___m_seman.AddStatusEffect(item.m_shared.m_equipStatusEffect, false);
            }
        }
        catch
        {
            //Dbgl($"Error: {Environment.StackTrace}");
        }
    }
}