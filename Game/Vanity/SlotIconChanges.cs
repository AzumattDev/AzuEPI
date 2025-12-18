using HarmonyLib;
using UnityEngine;

namespace AzuEPI.Game.Vanity;

[HarmonyPatch]
public static class IconPatches
{
    private static Sprite TryOverride(ItemDrop.ItemData item, Sprite fallback)
    {
        if (item == null) return fallback;
        return VanityAPI.TryGetEquippedVanityIcon(item, out Sprite spr) ? spr : fallback;
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetIcon))]
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void ItemIcon_Postfix(ItemDrop.ItemData __instance, ref Sprite __result)
    {
        __result = TryOverride(__instance, __result);
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupUpgradeItem))]
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void SetupUpgradeItem_Postfix(InventoryGui __instance, Recipe recipe, ItemDrop.ItemData item)
    {
        ItemDrop.ItemData? it = item ?? recipe?.m_item?.m_itemData;
        if (it == null) return;
        __instance.m_upgradeItemIcon.sprite = TryOverride(it, __instance.m_upgradeItemIcon.sprite);
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void UpdateRecipe_Postfix(InventoryGui __instance)
    {
        ItemDrop.ItemData? it = __instance.m_selectedRecipe.ItemData;
        if (it == null) return;
        __instance.m_recipeIcon.sprite = TryOverride(it, __instance.m_recipeIcon.sprite);
    }
}