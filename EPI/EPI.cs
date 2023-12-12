using System;
using System.Collections.Generic;
using AzuExtendedPlayerInventory.EPI.Patches;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AzuExtendedPlayerInventory.EPI
{
    internal class ExtendedPlayerInventory
    {
        public static List<HotkeyBar> HotkeyBars { get; set; }

        public static int SelectedHotkeyBarIndex { get; set; } = -1;

        private static GameObject _elementPrefab;

        public static Vector2 LastSlotPosition { get; set; }

        internal static ItemDrop.ItemData?[] equipItems = new ItemDrop.ItemData[5];

        public static Vector3 lastMousePos;
        public static string currentlyDragging;
        internal static readonly int Visible = Animator.StringToHash("visible");
        public const string QABName = "QuickAccessBar";
        public const string AzuBkgName = "AzuEquipmentBkg";
        public const string MinimalUiguid = "Azumatt.MinimalUI";

        public static void SetSlotText(string value, Transform transform, bool center = true)
        {
            Transform transform1 = transform.Find("binding");
            if (!transform1)
                transform1 = Object.Instantiate(_elementPrefab.transform.Find("binding"), transform);
            var textComp = transform1.GetComponent<TMP_Text>();
            textComp.enabled = true;
            textComp.overflowMode = TextOverflowModes.Overflow;
            textComp.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
            textComp.fontSizeMin = 10f;
            textComp.fontSizeMax = 18f;
            textComp.enableAutoSizing = true;
            textComp.text = value;
            if (!center)
                return;
            transform1.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 17f);
            transform1.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -10f);
        }

        internal static bool IsEquipmentSlotFree(
            Inventory inventory,
            ItemDrop.ItemData item,
            out int which)
        {
            var addedRows = API.GetAddedRows(inventory.GetWidth());
            which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is InventoryGuiPatches.EquipmentSlot slot && slot.Valid(item));
            return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
        }

        internal static bool IsAtEquipmentSlot(
            Inventory inventory,
            ItemDrop.ItemData item,
            out int which)
        {
            var inventoryRows = inventory.GetHeight() - API.GetAddedRows(inventory.GetWidth());
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || item.m_gridPos.y < inventoryRows || (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length)
            {
                which = -1;
                return false;
            }

            which = (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x;
            return true;
        }

        public static void SetElementPositions()
        {
            Transform transform = Hud.instance.transform.Find("hudroot");
            if (!(transform.Find(QABName)?.GetComponent<RectTransform>() != null))
                return;
            if (AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value == 9999.0)
                AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value =
                    transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x - 32f;
            if (AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value == 9999.0)
                AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value =
                    transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y - 870f;
            transform.Find(QABName).GetComponent<RectTransform>().anchoredPosition = new Vector2(AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value, AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value);
            transform.Find(QABName).GetComponent<RectTransform>().localScale = new Vector3(AzuExtendedPlayerInventoryPlugin.QuickAccessScale.Value, AzuExtendedPlayerInventoryPlugin.QuickAccessScale.Value, 1f);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_TakeAllRespons))]
    static class ContainerRPCRequestTakeAllPatch
    {
        static void Postfix(Container __instance, ref bool granted)
        {
            if (Player.m_localPlayer == null)
                return;
            if (granted)
            {
                Utilities.Utilities.InventoryFix();
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
    static class MoveAllToPatch // This should fix issues with AzuContainerSizes
    {
        static void Postfix(Inventory __instance, Inventory fromInventory)
        {
            if (Player.m_localPlayer == null)
                return;
            if (__instance == Player.m_localPlayer.GetInventory())
            {
                Utilities.Utilities.InventoryFix();
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone))]
    public static class ItemData_Clone_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref ItemDrop.ItemData __result)
        {
            // Fixes bug in vanilla valheim with cloning items with custom data
            __result.m_customData = new Dictionary<string, string>(__instance.m_customData);
        }
    }
}