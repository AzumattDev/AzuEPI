using System.Linq;
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
        private static bool slotsIsDirty = false;

        [HarmonyPriority(Priority.Last)]
        internal static bool Prefix(HotkeyBar __instance, Player player)
        {
            if (__instance.name != ExtendedPlayerInventory.QABName)
            {
                return true;
            }

            if (AzuExtendedPlayerInventoryPlugin.ShowQuickSlots.Value.IsOff())
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
                    int firstHotkeyIndex = adjustedHeight * width + AzuExtendedPlayerInventoryPlugin.EquipmentSlotsCount;

                    for (int i = 0; i < AzuExtendedPlayerInventoryPlugin.QuickSlotsCount; ++i)
                    {
                        int index = firstHotkeyIndex + i;
                        if (inventory.GetItemAt(index % width, index / width) is { } item)
                        {
                            __instance.m_items.Add(item);
                        }
                    }


                    __instance.m_items.Sort((x, y) => (x.m_gridPos.x + x.m_gridPos.y * width).CompareTo(y.m_gridPos.x + y.m_gridPos.y * width));
                    int num = __instance.m_items.Select(itemData => itemData.m_gridPos.x + itemData.m_gridPos.y * width - firstHotkeyIndex + 1).Concat(new[] { 0 }).Max(); // GPT

                    if (__instance.m_elements.Count != num || slotsIsDirty)
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
                            if (index < AzuExtendedPlayerInventoryPlugin.HotkeyTexts.Length && index < AzuExtendedPlayerInventoryPlugin.QuickSlotsCount)
                                ExtendedPlayerInventory.SetSlotText(AzuExtendedPlayerInventoryPlugin.GetHotkeyText(index), elementData.m_go.transform, isQuickSlot: true);

                            elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
                            elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
                            elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<TMP_Text>();
                            elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
                            elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
                            elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;
                            __instance.m_elements.Add(elementData);
                        }

                        slotsIsDirty = false;
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

        internal static void UpdateSlots()
        {
            slotsIsDirty = true;
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
    public static class HotkeyBar_Update_Patch
    {
        public static bool Prefix()
        {
            // Everything controlled in above update
            return false;
        }
    }
}