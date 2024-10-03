using System;
using System.Collections.Generic;
using AzuExtendedPlayerInventory.EPI.Patches;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using static AzuExtendedPlayerInventory.AzuExtendedPlayerInventoryPlugin;

namespace AzuExtendedPlayerInventory.EPI
{
    internal class ExtendedPlayerInventory
    {
        public static List<HotkeyBar> HotkeyBars { get; set; } = null!;

        public static int SelectedHotkeyBarIndex { get; set; } = -1;

        private static GameObject _elementPrefab = null!;

        public static Vector2 LastSlotPosition { get; set; }

        internal static ItemDrop.ItemData?[] equipItems = new ItemDrop.ItemData[5];

        public static Vector3 lastMousePos;
        public static string currentlyDragging = null!;
        internal static readonly int Visible = Animator.StringToHash("visible");
        public const string QABName = "QuickAccessBar";
        public const string AzuBkgName = "AzuEquipmentBkg";
        public const string DropAllButtonName = "AzuDropAllButton";
        public const string MinimalUiguid = "Azumatt.MinimalUI";

        public static void SetSlotText(string value, Transform transform, bool isQuickSlot = true)
        {
            Transform binding = transform.Find("binding");
            
            if (!binding)
            {
                binding = Object.Instantiate(_elementPrefab.transform.Find("binding"), transform);
                binding.name = "binding";
            }
            
            // Make component size of parent to let TMP_Text do its job on text positioning
            RectTransform rt = binding.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            TMP_Text textComp = binding.GetComponent<TMP_Text>();
            textComp.enableAutoSizing = true;
            textComp.text = value;
            textComp.enabled = true;
            textComp.overflowMode = TextOverflowModes.Overflow;
            textComp.fontSizeMin = 10f;
            textComp.fontSizeMax = isQuickSlot ? quickSlotLabelFontSize.Value : equipmentSlotLabelFontSize.Value;
            textComp.color = isQuickSlot ? quickSlotLabelFontColor.Value : equipmentSlotLabelFontColor.Value;
            textComp.margin = isQuickSlot ? quickSlotLabelMargin.Value : equipmentSlotLabelMargin.Value;
            textComp.textWrappingMode = isQuickSlot ? quickSlotLabelWrappingMode.Value : equipmentSlotLabelWrappingMode.Value;
            textComp.horizontalAlignment = isQuickSlot ? quickSlotLabelAlignment.Value : equipmentSlotLabelAlignment.Value;
            textComp.verticalAlignment = VerticalAlignmentOptions.Top;
        }

        internal static bool IsEquipmentSlotFree(Inventory inventory, ItemDrop.ItemData item, out int which)
        {
            var addedRows = API.GetAddedRows(inventory.GetWidth());
            which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is InventoryGuiPatches.EquipmentSlot slot && slot.Valid(item));
            return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
        }

        internal static bool IsAtEquipmentSlot(Inventory inventory, ItemDrop.ItemData item, out int which)
        {
            var inventoryRows = inventory.GetHeight() - API.GetAddedRows(inventory.GetWidth());
            if (AddEquipmentRow.Value == Toggle.Off || item.m_gridPos.y < inventoryRows || (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length)
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
            if (QuickAccessX.Value == 9999.0)
                QuickAccessX.Value = transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x - 32f;
            if (QuickAccessY.Value == 9999.0)
                QuickAccessY.Value = transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y - 870f;
            transform.Find(QABName).GetComponent<RectTransform>().anchoredPosition = new Vector2(QuickAccessX.Value, QuickAccessY.Value);
            transform.Find(QABName).GetComponent<RectTransform>().localScale = new Vector3(QuickAccessScale.Value, QuickAccessScale.Value, 1f);
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
}