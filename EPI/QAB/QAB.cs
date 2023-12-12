using System.Linq;
using AzuExtendedPlayerInventory.EPI.Patches;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AzuExtendedPlayerInventory.EPI.QAB
{
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    internal static class QuickAccessBar
    {
        [HarmonyPriority(Priority.Last)]
        internal static bool Prefix(HotkeyBar __instance, Player player)
        {
            if (__instance.name != ExtendedPlayerInventory.QABName)
            {
                return true;
            }

            if (AzuExtendedPlayerInventoryPlugin.ShowQuickSlots.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
            {
                ClearElements(__instance);
            }
            else
            {
                if (!player || player.IsDead())
                {
                    ClearElements(__instance);
                }
                else
                {
                    __instance.m_items.Clear();
                    Inventory inventory = player.GetInventory();
                    int width = inventory.GetWidth();
                    int adjustedHeight = inventory.GetHeight() - API.GetAddedRows(width);
                    int firstHotkeyIndex = adjustedHeight * width + InventoryGuiPatches.UpdateInventory_Patch.slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length;

                    for (int i = 0; i < AzuExtendedPlayerInventoryPlugin.Hotkeys.Length; ++i)
                    {
                        int index = firstHotkeyIndex + i;
                        if (inventory.GetItemAt(index % width, index / width) is { } item)
                        {
                            __instance.m_items.Add(item);
                        }
                    }


                    __instance.m_items.Sort((x, y) => (x.m_gridPos.x + x.m_gridPos.y * width).CompareTo(y.m_gridPos.x + y.m_gridPos.y * width));
                    int num = __instance.m_items.Select(itemData => itemData.m_gridPos.x + itemData.m_gridPos.y * width - firstHotkeyIndex + 1).Concat(new[] { 0 }).Max(); // GPT

                    if (__instance.m_elements.Count != num)
                    {
                        foreach (HotkeyBar.ElementData element in __instance.m_elements)
                            Object.Destroy(element.m_go);
                        __instance.m_elements.Clear();
                        for (int index = 0; index < num; ++index)
                        {
                            HotkeyBar.ElementData elementData = new()
                            {
                                m_go = Object.Instantiate(__instance.m_elementPrefab, __instance.transform),
                            };
                            elementData.m_go.transform.localPosition = new Vector3(index * __instance.m_elementSpace, 0.0f, 0.0f);
                            if (index < AzuExtendedPlayerInventoryPlugin.HotkeyTexts.Length && index < AzuExtendedPlayerInventoryPlugin.Hotkeys.Length)
                            {
                                ExtendedPlayerInventory.SetSlotText(AzuExtendedPlayerInventoryPlugin.HotkeyTexts[index].Value.IsNullOrWhiteSpace()
                                    ? AzuExtendedPlayerInventoryPlugin.Hotkeys[index].Value.ToString()
                                    : AzuExtendedPlayerInventoryPlugin.HotkeyTexts[index].Value, elementData.m_go.transform, false);
                            }

                            elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
                            elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
                            elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<TMP_Text>();
                            elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
                            elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
                            elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;
                            __instance.m_elements.Add(elementData);
                        }
                    }

                    foreach (HotkeyBar.ElementData element in __instance.m_elements)
                        element.m_used = false;
                    bool flag = ZInput.IsGamepadActive();
                    foreach (ItemDrop.ItemData itemData in __instance.m_items)
                    {
                        int index = itemData.m_gridPos.x + itemData.m_gridPos.y * width - firstHotkeyIndex;
                        if (index >= 0 && index < __instance.m_elements.Count)
                        {
                            HotkeyBar.ElementData element = __instance.m_elements[index];
                            element.m_used = true;
                            element.m_icon.gameObject.SetActive(true);
                            element.m_icon.sprite = itemData.GetIcon();
                            element.m_durability.gameObject.SetActive(itemData.m_shared.m_useDurability);
                            if (itemData.m_shared.m_useDurability)
                            {
                                if (itemData.m_durability <= 0.0)
                                {
                                    element.m_durability.SetValue(1f);
                                    element.m_durability.SetColor(Mathf.Sin(Time.time * 10f) > 0.0
                                        ? Color.red
                                        : new Color(0.0f, 0.0f, 0.0f, 0.0f));
                                }
                                else
                                {
                                    element.m_durability.SetValue(itemData.GetDurabilityPercentage());
                                    element.m_durability.ResetColor();
                                }
                            }

                            element.m_equiped.SetActive(itemData.m_equipped);
                            element.m_queued.SetActive(player.IsEquipActionQueued(itemData));
                            if (itemData.m_shared.m_maxStackSize > 1)
                            {
                                element.m_amount.gameObject.SetActive(true);
                                if (element.m_stackText != itemData.m_stack)
                                {
                                    element.m_amount.text = AzuExtendedPlayerInventoryPlugin.WbInstalled
                                        ? Utilities.Utilities.FormatNumberSimpleNoDecimal((float)itemData.m_stack)
                                        : $"{itemData.m_stack} / {itemData.m_shared.m_maxStackSize}";

                                    element.m_stackText = itemData.m_stack;
                                }
                            }
                            else
                            {
                                element.m_amount.gameObject.SetActive(false);
                            }
                        }
                    }


                    for (int index = 0; index < __instance.m_elements.Count; ++index)
                    {
                        HotkeyBar.ElementData element = __instance.m_elements[index];
                        element.m_selection.SetActive(flag && index == __instance.m_selected);
                        if (element.m_used) continue;
                        element.m_icon.gameObject.SetActive(false);
                        element.m_durability.gameObject.SetActive(false);
                        element.m_equiped.SetActive(false);
                        element.m_queued.SetActive(false);
                        element.m_amount.gameObject.SetActive(false);
                    }
                }

                return false;
            }

            return false;
        }

        private static void ClearElements(HotkeyBar __instance, bool destroy = true)
        {
            foreach (HotkeyBar.ElementData element in __instance.m_elements)
            {
                if (destroy)
                    Object.Destroy(element.m_go);
                else
                {
                    element.m_icon.gameObject.SetActive(false);
                    element.m_durability.gameObject.SetActive(false);
                    element.m_equiped.SetActive(false);
                    element.m_queued.SetActive(false);
                    element.m_amount.gameObject.SetActive(false);
                }
            }

            __instance.m_elements.Clear();
        }
    }

    public static class HotkeyBarController
    {
        [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
        public static class Hud_Update_Patch
        {
            public static void Postfix(Hud __instance)
            {
                var player = Player.m_localPlayer;
                if (ExtendedPlayerInventory.HotkeyBars == null)
                {
                    ExtendedPlayerInventory.HotkeyBars = __instance.transform.parent.GetComponentsInChildren<HotkeyBar>().ToList();
                }

                if (player != null)
                {
                    if (IsValidHotkeyBarIndex())
                    {
                        var currentHotKeyBar = ExtendedPlayerInventory.HotkeyBars[ExtendedPlayerInventory.SelectedHotkeyBarIndex];
                        UpdateHotkeyBarInput(currentHotKeyBar);
                    }
                    else
                    {
                        UpdateInitialHotkeyBarInput();
                    }
                }

                foreach (var hotkeyBar in ExtendedPlayerInventory.HotkeyBars)
                {
                    ValidateHotkeyBarSelection(hotkeyBar);
                    hotkeyBar.UpdateIcons(player);
                }
            }

            private static bool IsValidHotkeyBarIndex() => ExtendedPlayerInventory.SelectedHotkeyBarIndex >= 0 && ExtendedPlayerInventory.SelectedHotkeyBarIndex < ExtendedPlayerInventory.HotkeyBars.Count;

            private static void UpdateInitialHotkeyBarInput()
            {
                if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight"))
                {
                    SelectHotkeyBar(0, false);
                }
            }

            public static void UpdateHotkeyBarInput(HotkeyBar hotkeyBar)
            {
                var player = Player.m_localPlayer;
                var canUseItem = hotkeyBar.m_selected >= 0 && player != null && !InventoryGui.IsVisible()
                                 && !Menu.IsVisible() && !GameCamera.InFreeFly();
                if (canUseItem)
                {
                    if (ZInput.GetButtonDown("JoyDPadLeft"))
                    {
                        if (hotkeyBar.m_selected == 0 && AzuExtendedPlayerInventoryPlugin.ShowQuickSlots.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
                        {
                            GotoHotkeyBar(ExtendedPlayerInventory.SelectedHotkeyBarIndex - 1);
                        }
                        else
                        {
                            hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_selected - 1);
                        }
                    }
                    else if (ZInput.GetButtonDown("JoyDPadRight"))
                    {
                        if (hotkeyBar.m_selected == hotkeyBar.m_elements.Count - 1 && AzuExtendedPlayerInventoryPlugin.ShowQuickSlots.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
                        {
                            GotoHotkeyBar(ExtendedPlayerInventory.SelectedHotkeyBarIndex + 1);
                        }
                        else
                        {
                            hotkeyBar.m_selected = Mathf.Min(hotkeyBar.m_elements.Count - 1, hotkeyBar.m_selected + 1);
                        }
                    }

                    if (ZInput.GetButtonDown("JoyDPadUp"))
                    {
                        if (hotkeyBar.name == "QuickAccessBar" && AzuExtendedPlayerInventoryPlugin.ShowQuickSlots.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
                        {
                            var quickSlotInventory = player.m_inventory;
                            int width = quickSlotInventory.GetWidth();
                            int adjustedHeight = quickSlotInventory.GetHeight() - API.GetAddedRows(width);
                            int index = adjustedHeight * width + InventoryGuiPatches.UpdateInventory_Patch.slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length + hotkeyBar.m_selected;

                            var item = quickSlotInventory.GetItemAt(index % width, index / width);
                            if (item != null)
                            {
                                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"QuickAccessBar item {item.m_shared.m_name}");
                                player.UseItem(null, item, false);
                            }
                        }
                        else
                        {
                            if (ZInput.GetButtonDown("JoyHotbarUse") && !ZInput.GetButton("JoyAltKeys"))
                                player.UseHotbarItem(hotkeyBar.m_selected + 1);
                        }
                    }
                }

                ValidateHotkeyBarSelection(hotkeyBar);
            }

            private static void ValidateHotkeyBarSelection(HotkeyBar hotkeyBar)
            {
                if (hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1)
                {
                    hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_elements.Count - 1);
                }
            }

            public static void GotoHotkeyBar(int newIndex)
            {
                if (newIndex < 0 || newIndex >= ExtendedPlayerInventory.HotkeyBars.Count)
                {
                    return;
                }

                var fromRight = newIndex < ExtendedPlayerInventory.SelectedHotkeyBarIndex;
                SelectHotkeyBar(newIndex, fromRight);
            }

            public static void SelectHotkeyBar(int index, bool fromRight)
            {
                if (index < 0 || index >= ExtendedPlayerInventory.HotkeyBars.Count)
                {
                    return;
                }

                ExtendedPlayerInventory.SelectedHotkeyBarIndex = index;
                for (var i = 0; i < ExtendedPlayerInventory.HotkeyBars.Count; ++i)
                {
                    var hotkeyBar = ExtendedPlayerInventory.HotkeyBars[i];
                    if (i == index)
                    {
                        hotkeyBar.m_selected = fromRight ? hotkeyBar.m_elements.Count - 1 : 0;
                    }
                    else
                    {
                        hotkeyBar.m_selected = -1;
                    }
                }
            }

            public static void DeselectHotkeyBar()
            {
                ExtendedPlayerInventory.SelectedHotkeyBarIndex = -1;
                foreach (var hotkeyBar in ExtendedPlayerInventory.HotkeyBars)
                {
                    hotkeyBar.m_selected = -1;
                }
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
        public static class Hud_OnDestroy_Patch
        {
            public static void Postfix(Hud __instance)
            {
                ExtendedPlayerInventory.HotkeyBars = null;
                ExtendedPlayerInventory.SelectedHotkeyBarIndex = -1;
            }
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
    public static class HotkeyBar_Update_Patch
    {
        public static bool Prefix(HotkeyBar __instance)
        {
            // Everything controlled in above update
            return false;
        }
    }
}