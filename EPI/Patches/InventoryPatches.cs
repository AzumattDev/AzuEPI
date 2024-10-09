using HarmonyLib;
using static AzuExtendedPlayerInventory.AzuExtendedPlayerInventoryPlugin;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryPatches
{
    public static bool IsInventoryToPatch(Inventory inventory) => AddEquipmentRow.Value.IsOn() && inventory == ExtendedPlayerInventory.PlayerInventory;

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class Inventory_FindEmptySlot_
    {
        private static bool Prefix(Inventory __instance, ref Vector2i __result, ref bool __state)
        {
            if (!IsInventoryToPatch(__instance))
                return true;

            if (InventoryGui.instance.m_craftTimer >=
                InventoryGui.instance.m_craftDuration &&
                InventoryGui.instance.m_craftUpgradeItem is { } item
                && ExtendedPlayerInventory.EquipmentSlots.TryFindFreeSlotForItem(item, out int x, out int y))
            {
                __result = new Vector2i(x, y);
                return false;
            }

            __state = true;
            __instance.m_height = ExtendedPlayerInventory.InventoryHeightPlayer;
            return true;
        }

        private static void Postfix(Inventory __instance, bool __state, ref Vector2i __result)
        {
            if (!__state)
                return;

            __instance.m_height = ExtendedPlayerInventory.InventoryHeightFull;

            if (__result == new Vector2i(-1, -1))
                __result = ExtendedPlayerInventory.QuickSlots.FindEmptySlot();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    private static class Inventory_GetEmptySlots_CheckRegularInventoryAndQuickslots
    {
        private static void Prefix(Inventory __instance, ref bool __state)
        {
            if (!IsInventoryToPatch(__instance))
                return;

            __instance.m_height = ExtendedPlayerInventory.InventoryHeightPlayer;
            __state = true;
        }

        private static void Postfix(Inventory __instance, ref int __result, bool __state)
        {
            if (!__state)
                return;

            __instance.m_height = ExtendedPlayerInventory.InventoryHeightFull;

            __result += ExtendedPlayerInventory.QuickSlots.GetEmptySlots();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    private static class Inventory_HaveEmptySlot_CheckRegularInventoryAndQuickslots
    {
        private static void Prefix(Inventory __instance, ref bool __state)
        {
            if (!IsInventoryToPatch(__instance))
                return;

            __instance.m_height = ExtendedPlayerInventory.InventoryHeightPlayer;
            __state = true;
        }

        private static void Postfix(Inventory __instance, ref bool __result, bool __state)
        {
            if (!__state)
                return;

            __instance.m_height = ExtendedPlayerInventory.InventoryHeightFull;
            __result = __result || ExtendedPlayerInventory.QuickSlots.HaveEmptySlot();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
    private static class Inventory_AddItem_ItemData_AutoEquipIfFitAtSlot
    {
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (!IsInventoryToPatch(__instance))
                return true;

            if (AutoEquip.Value.IsOff() && KeepUnequippedInSlot.Value.IsOff())
                return true;

            if (!ExtendedPlayerInventory.EquipmentSlots.TryFindFreeSlotForItem(item, out int x, out int y))
                return true;

            LogInfo($"{__instance.AddItem(item, item.m_stack, x, y)} {item.m_shared.m_name} {x} {y}");
           
            if (ExtendedPlayerInventory.EquipmentSlots.TryGetSlotIndex(new Vector2i(x, y), out int slotIndex) && ExtendedPlayerInventory.EquipmentSlots.GetItemInSlot(slotIndex) is ItemDrop.ItemData itemSlot)
                Player.m_localPlayer.EquipItem(itemSlot, false);

            __instance.Changed();
            __result = true;
            
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    private static class Inventory_AddItem_ItemData_amount_x_y_AutoFixInventorySize
    {
        private static void Prefix(Inventory __instance, int y)
        {
            if (!IsInventoryToPatch(__instance))
                return;

            if (y < __instance.m_height)
                return;

            __instance.m_height = ExtendedPlayerInventory.InventoryHeightFull;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
    private static class Inventory_MoveInventoryToGrave_LogHeight
    {
        private static void Postfix(Inventory __instance, Inventory original)
        {
            LogInfo("MoveInventoryToGrave");

            LogInfo($"inv: {__instance.GetHeight()} orig: {original.GetHeight()}");
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.SlotsUsedPercentage))]
    private static class Inventory_SlotsUsedPercentage_ExcludeRedundantSlots
    {
        private static bool Prefix(Inventory __instance, ref float __result)
        {
            __result = (float)__instance.m_inventory.Count / ExtendedPlayerInventory.InventorySizeFull * 100f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
    static class Inventory_MoveAll_InventoryFixAttempt // This should fix issues with AzuContainerSizes
    {
        static void Postfix(Inventory __instance)
        {
            if (__instance == ExtendedPlayerInventory.PlayerInventory)
                ExtendedPlayerInventory.CheckPlayerInventoryItemsOverlappingOrOutOfGrid();
        }
    }
}