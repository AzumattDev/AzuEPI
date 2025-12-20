using AzuEPI.Core.Text;
using AzuEPI.EPI;

namespace AzuEPI.Game.Slots.QAB;

[HarmonyPatch]
internal static class SettingsMenuClosedRecentlyPatchs
{
    [HarmonyPatch(typeof(Settings), nameof(Settings.OnDestroy))]
    [HarmonyPatch(typeof(Settings), nameof(Settings.OnOk))]
    [HarmonyPatch(typeof(Settings), nameof(Settings.OnBack))]
    private static void Postfix(Settings __instance)
    {
        QuickAccessBar.SettingsMenuClosedRecently = true;
    }
}

[HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
internal static class QuickAccessBar
{
    internal static bool SettingsMenuClosedRecently;

    [HarmonyPriority(Priority.Last)]
    internal static bool Prefix(HotkeyBar __instance, Player player)
    {
        if (__instance.name != QabName) return true;

        if (ShowQuickSlots.Value.isOff())
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
                if (SettingsMenuClosedRecently)
                {
                    ClearElements(__instance);
                    SettingsMenuClosedRecently = false;
                }

                __instance.m_items.Clear();
                Inventory inventory = player.GetInventory();
                int width = inventory.GetWidth();
                int adjustedHeight = inventory.GetHeight() - API.GetAddedRows(width);
                int firstHotkeyIndex = adjustedHeight * width + InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length;

                for (int i = 0; i < Hotkeys.Length; ++i)
                {
                    int index = firstHotkeyIndex + i;
                    if (inventory.GetItemAt(index % width, index / width) is { } item) __instance.m_items.Add(item);
                }

                __instance.m_items.Sort((x, y) => (x.m_gridPos.x + x.m_gridPos.y * width).CompareTo(y.m_gridPos.x + y.m_gridPos.y * width));

                int amountToShow = 0;
                if (AlwaysShowQuickSlotsInUI.Value.isOn())
                {
                    amountToShow = Hotkeys.Length;
                }
                else
                {
                    for (int i = 0; i < __instance.m_items.Count; ++i)
                    {
                        int value = __instance.m_items[i].m_gridPos.x + __instance.m_items[i].m_gridPos.y * width - firstHotkeyIndex + 1;
                        if (value > amountToShow) amountToShow = value;
                    }
                }

                if (__instance.m_elements.Count != amountToShow)
                {
                    foreach (HotkeyBar.ElementData element in __instance.m_elements)
                        Object.Destroy(element.m_go);
                    __instance.m_elements.Clear();
                    for (int index = 0; index < amountToShow; ++index)
                    {
                        HotkeyBar.ElementData elementData = new()
                        {
                            m_go = Object.Instantiate(__instance.m_elementPrefab, __instance.transform)
                        };

                        int slotsPerRow = Mathf.Max(1, QuickSlotsPerRow.Value);
                        int column = index % slotsPerRow;
                        int row = index / slotsPerRow;
                        elementData.m_go.transform.localPosition = new Vector3(column * __instance.m_elementSpace, -row * __instance.m_elementSpace, 0.0f);

                        if (index < HotkeyTexts.Length && index < Hotkeys.Length)
                            SlotText.Set(HotkeyTexts[index].Value.IsNullOrWhiteSpace()
                                ? Hotkeys[index].Value.ToString()
                                : HotkeyTexts[index].Value, elementData.m_go.transform);

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
                        bool hideDurabilityFlag = itemData.m_shared.m_useDurability && itemData.m_durability < (double)itemData.GetMaxDurability();
                        element.m_durability.gameObject.SetActive(hideDurabilityFlag);
                        if (itemData.m_shared.m_useDurability && hideDurabilityFlag)
                        {
                            if (itemData.m_durability <= 0.0)
                            {
                                element.m_durability.SetValue(1f);
                                element.m_durability.SetColor(Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : new Color(0.0f, 0.0f, 0.0f, 0.0f));
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
                                element.m_amount.text = WbInstalled ? Formatting.FormatNumberSimpleNoDecimal(itemData.m_stack) : $"{itemData.m_stack} / {itemData.m_shared.m_maxStackSize}";

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
            if (destroy)
            {
                Object.Destroy(element.m_go);
            }
            else
            {
                element.m_icon.gameObject.SetActive(false);
                element.m_durability.gameObject.SetActive(false);
                element.m_equiped.SetActive(false);
                element.m_queued.SetActive(false);
                element.m_amount.gameObject.SetActive(false);
            }

        __instance.m_elements.Clear();
    }

    public static void SetElementPositions()
    {
        Transform transform = Hud.instance.transform.Find("hudroot");
        if (!(transform.Find(QabName)?.GetComponent<RectTransform>() != null))
            return;
        RectTransform? healthPanel = Hud.instance.m_healthPanel;
        RectTransform? healthPanelRect = healthPanel.GetComponent<RectTransform>();
        if (QuickAccessLocation.Value == Vector2.one)
            QuickAccessLocation.Value = new Vector2(healthPanelRect.anchoredPosition.x - 2.5f, healthPanelRect.anchoredPosition.y - 380.87f);
        transform.Find(QabName).GetComponent<RectTransform>().anchoredPosition = QuickAccessLocation.Value;
        transform.Find(QabName).GetComponent<RectTransform>().localScale = new Vector3(QuickAccessScale.Value, QuickAccessScale.Value, 1f);
    }
}

public static class HotkeyBarController
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    public static class Hud_Update_Patch
    {
        public static void Postfix(Hud __instance)
        {
            Player? player = Player.m_localPlayer;
            if (ExtendedPlayerInventory.HotkeyBars == null)
                try
                {
                    ExtendedPlayerInventory.HotkeyBars = __instance.transform.parent.GetComponentsInChildren<HotkeyBar>().ToList();
                }
                catch
                {
                    AzuExtendedPlayerInventoryLogger.LogError($"Failed to get hotkey bars from Hud. The parent transform may have changed. {__instance.transform.parent.name}");
                    return;
                }

            if (player != null)
            {
                if (IsValidHotkeyBarIndex())
                {
                    HotkeyBar? currentHotKeyBar = ExtendedPlayerInventory.HotkeyBars[ExtendedPlayerInventory.SelectedHotkeyBarIndex];
                    UpdateHotkeyBarInput(currentHotKeyBar);
                }
                else
                {
                    UpdateInitialHotkeyBarInput();
                }
            }

            foreach (HotkeyBar? hotkeyBar in ExtendedPlayerInventory.HotkeyBars)
                if (hotkeyBar != null && hotkeyBar.m_elements != null)
                {
                    ValidateHotkeyBarSelection(hotkeyBar);
                    hotkeyBar.UpdateIcons(player);
                }
        }

        private static bool IsValidHotkeyBarIndex()
        {
            return ExtendedPlayerInventory.SelectedHotkeyBarIndex >= 0 && ExtendedPlayerInventory.SelectedHotkeyBarIndex < ExtendedPlayerInventory.HotkeyBars.Count;
        }

        private static void UpdateInitialHotkeyBarInput()
        {
            if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight")) SelectHotkeyBar(0, false);
        }

        public static void UpdateHotkeyBarInput(HotkeyBar hotkeyBar)
        {
            Player? player = Player.m_localPlayer;
            bool canUseItem = hotkeyBar.m_selected >= 0 && player != null && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly() && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !StoreGui.IsVisible() && !Console.IsVisible() && !Chat.instance.HasFocus() && !PlayerCustomizaton.IsBarberGuiVisible() && !Hud.InRadial();
            if (canUseItem && player != null)
            {
                if (ZInput.GetButtonDown("JoyDPadLeft"))
                {
                    if (hotkeyBar.m_selected == 0 && ShowQuickSlots.Value.isOn())
                        GotoHotkeyBar(ExtendedPlayerInventory.SelectedHotkeyBarIndex - 1);
                    else
                        hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_selected - 1);
                }
                else if (ZInput.GetButtonDown("JoyDPadRight"))
                {
                    if (hotkeyBar.m_selected == hotkeyBar.m_elements.Count - 1 && ShowQuickSlots.Value.isOn())
                        GotoHotkeyBar(ExtendedPlayerInventory.SelectedHotkeyBarIndex + 1);
                    else
                        hotkeyBar.m_selected = Mathf.Min(hotkeyBar.m_elements.Count - 1, hotkeyBar.m_selected + 1);
                }

                if (ZInput.GetButtonDown("JoyDPadUp"))
                {
                    if (hotkeyBar.name == "QuickAccessBar" && ShowQuickSlots.Value.isOn())
                    {
                        Inventory? quickSlotInventory = player.m_inventory;
                        int width = quickSlotInventory.GetWidth();
                        int adjustedHeight = quickSlotInventory.GetHeight() - API.GetAddedRows(width);
                        int index = adjustedHeight * width + InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length + hotkeyBar.m_selected;

                        ItemDrop.ItemData? item = quickSlotInventory.GetItemAt(index % width, index / width);
                        if (item != null)
                        {
                            AzuExtendedPlayerInventoryLogger.LogInfo($"QuickAccessBar item {item.m_shared.m_name}");
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
            if (hotkeyBar.m_elements != null && hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1) hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_elements.Count - 1);
        }

        public static void GotoHotkeyBar(int newIndex)
        {
            if (newIndex < 0 || newIndex >= ExtendedPlayerInventory.HotkeyBars.Count) return;

            bool fromRight = newIndex < ExtendedPlayerInventory.SelectedHotkeyBarIndex;
            SelectHotkeyBar(newIndex, fromRight);
        }

        public static void SelectHotkeyBar(int index, bool fromRight)
        {
            if (index < 0 || index >= ExtendedPlayerInventory.HotkeyBars.Count) return;

            ExtendedPlayerInventory.SelectedHotkeyBarIndex = index;
            for (int i = 0; i < ExtendedPlayerInventory.HotkeyBars.Count; ++i)
            {
                HotkeyBar? hotkeyBar = ExtendedPlayerInventory.HotkeyBars[i];
                if (i == index)
                    hotkeyBar.m_selected = fromRight ? hotkeyBar.m_elements.Count - 1 : 0;
                else
                    hotkeyBar.m_selected = -1;
            }
        }

        public static void DeselectHotkeyBar()
        {
            ExtendedPlayerInventory.SelectedHotkeyBarIndex = -1;
            foreach (HotkeyBar? hotkeyBar in ExtendedPlayerInventory.HotkeyBars) hotkeyBar.m_selected = -1;
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
    public static class Hud_OnDestroy_Patch
    {
        public static void Postfix(Hud __instance)
        {
            ExtendedPlayerInventory.HotkeyBars = null!;
            ExtendedPlayerInventory.SelectedHotkeyBarIndex = -1;
        }
    }
}

[HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
public static class HotkeyBar_Update_Patch
{
    public static bool Prefix(HotkeyBar __instance)
    {
        return false;
    }
}