using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryPatches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class FindEmptySlotPatch
    {
        private static bool Prefix(Inventory __instance, ref int ___m_height, ref Vector2i __result)
        {
            bool addEquipmentRow = AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On;
            if (!addEquipmentRow || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                return true;
            bool equippedWeaponUpgrade = InventoryGui.instance.m_craftTimer >=
                                         InventoryGui.instance.m_craftDuration &&
                                         InventoryGui.instance.m_craftUpgradeItem is { } item
                                         && ExtendedPlayerInventory.IsEquipmentSlotFree(__instance, item, out _);
            if (equippedWeaponUpgrade)
            {
                // When upgrading equipment: AddItem only checks for space. Return an arbitrary slot here. The AddItem(ItemData) patch will move it to the right slot.
                __result = Vector2i.zero;
            }
            
            int addedRows = API.GetAddedRows(__instance.GetWidth());
            ___m_height -= addedRows;
            return !equippedWeaponUpgrade;
        }

        private static void Postfix(Inventory __instance, ref int ___m_height)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || !Player.m_localPlayer ||
                __instance != Player.m_localPlayer.GetInventory())
                return;
            
            int addedRows = API.GetAddedRows(__instance.GetWidth());
            
            ___m_height += addedRows;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    private static class GetEmptySlotsPatch
    {
        private static bool Prefix(
            Inventory __instance,
            ref int __result,
            List<ItemDrop.ItemData> ___m_inventory,
            int ___m_width,
            int ___m_height)
        {
            if (Player.m_localPlayer == null) return true;
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || __instance != Player.m_localPlayer.GetInventory())
                return true;
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("GetEmptySlots");

            int addedRows = API.GetAddedRows(__instance.GetWidth());
            int adjustedHeight = ___m_height - addedRows;

            int count = ___m_inventory.FindAll(i => i.m_gridPos.y < adjustedHeight).Count;
            __result = (adjustedHeight) * ___m_width - count;
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    private static class HaveEmptySlotPatch
    {
        private static bool Prefix(
            Inventory __instance,
            ref bool __result,
            List<ItemDrop.ItemData> ___m_inventory,
            int ___m_width,
            int ___m_height)
        {
            if (Player.m_localPlayer == null) return true;
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || __instance != Player.m_localPlayer.GetInventory())
                return true;

            int addedRows = API.GetAddedRows(___m_width);

            int adjustedHeight = ___m_height - addedRows;

            int count = ___m_inventory.FindAll(i => i.m_gridPos.y < adjustedHeight).Count;
            __result = count < ___m_width * (adjustedHeight);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
    private static class InventoryAddItemPatch1
    {
        private static bool Prefix(
            Inventory __instance,
            ref bool __result,
            List<ItemDrop.ItemData> ___m_inventory,
            ItemDrop.ItemData item)
        {
            if (Player.m_localPlayer == null) return true;
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                return true;
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("AddItem");
            if (!ExtendedPlayerInventory.IsEquipmentSlotFree(__instance, item, out int which))
                return true;

            int addedRows = API.GetAddedRows(__instance.GetWidth());

            int adjustedHeight = __instance.GetHeight() - addedRows;


            __instance.AddItem(item, item.m_stack, which % __instance.GetWidth(), adjustedHeight + which / __instance.GetWidth());
            Player.m_localPlayer.EquipItem(item, false);
            __instance.Changed();
            __result = true;
            return false;
        }
    }


    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int),
        typeof(int))]
    private static class InventoryAddItemPatch2
    {
        private static void Prefix(
            Inventory __instance,
            ref int ___m_width,
            ref int ___m_height,
            int x,
            int y)
        {
            int addedRows = API.GetAddedRows(___m_width);

            if (y < ___m_height)
                return;
            ___m_height = y + addedRows;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
    private static class MoveInventoryToGravePatch
    {
        private static void Postfix(Inventory __instance, Inventory original)
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("MoveInventoryToGrave");

            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"inv: {__instance.GetHeight()} orig: {original.GetHeight()}");
        }
    }
}