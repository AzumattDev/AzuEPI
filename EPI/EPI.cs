using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using static AzuExtendedPlayerInventory.AzuExtendedPlayerInventoryPlugin;
using AzuExtendedPlayerInventory.EPI.Utilities;
using System.Linq;
using System;


namespace AzuExtendedPlayerInventory.EPI
{
    public class Slot
    {
#nullable enable
        public string Name = null!;
        public Vector2 Position;
        public EquipmentSlot? EquipmentSlot => this as EquipmentSlot;
#nullable disable
    }

    public class EquipmentSlot : Slot
    {
        public const string helmetSlotID = "Helmet";
        public const string legsSlotID = "Legs";
        public const string utilitySlotID = "Utility";
        public const string chestSlotID = "Chest";
        public const string backSlotID = "Back";

#nullable enable
        public Func<Player, ItemDrop.ItemData?> Get = null!;
        public Func<ItemDrop.ItemData, bool> Valid = null!;
#nullable disable
    }

    internal static class ExtendedPlayerInventory
    {
        public const string QABName = "QuickAccessBar";
        public const string AzuBkgName = "AzuEquipmentBkg";

        public static List<HotkeyBar> HotkeyBars { get; set; } = null!;

        public static int SelectedHotkeyBarIndex { get; set; } = -1;

        private static Vector3 _lastMousePos;
        private static string _currentlyDragging = null!;
        
        private static readonly int _visible = Animator.StringToHash("visible");
        
        internal static bool IsVisible() => InventoryGui.instance && InventoryGui.instance.m_animator.GetBool(_visible);

        internal static void SetSlotText(string value, Transform transform, bool isQuickSlot)
        {
            Transform binding = transform.Find("binding");

            if (!binding)
                return;
            
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
            textComp.fontSizeMin = isQuickSlot ? quickSlotLabelFontSize.Value.x : equipmentSlotLabelFontSize.Value.x;
            textComp.fontSizeMax = isQuickSlot ? quickSlotLabelFontSize.Value.y : equipmentSlotLabelFontSize.Value.y;
            textComp.color = isQuickSlot ? quickSlotLabelFontColor.Value : equipmentSlotLabelFontColor.Value;
            textComp.margin = isQuickSlot ? quickSlotLabelMargin.Value : equipmentSlotLabelMargin.Value;
            textComp.textWrappingMode = isQuickSlot ? quickSlotLabelWrappingMode.Value : equipmentSlotLabelWrappingMode.Value;
            textComp.horizontalAlignment = isQuickSlot ? quickSlotLabelAlignment.Value : equipmentSlotLabelAlignment.Value;
            textComp.verticalAlignment = VerticalAlignmentOptions.Top;
        }
        
        internal static readonly List<Slot> slots = new()
            {
                new EquipmentSlot { Name = HelmetText.Value,   Get = player => player.m_helmetItem,    Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet, },
                new EquipmentSlot { Name = LegsText.Value,     Get = player => player.m_legItem,       Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs, },
                new EquipmentSlot { Name = UtilityText.Value,  Get = player => player.m_utilityItem,   Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility, },
                new EquipmentSlot { Name = ChestText.Value,    Get = player => player.m_chestItem,     Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest, },
                new EquipmentSlot { Name = BackText.Value,     Get = player => player.m_shoulderItem,  Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder, },
            };

        internal static Slot[] GetQuickSlots()
        {
            string[] fixedSlotNames = Hotkeys.Select(hk => hk.Value.ToString()).ToArray();

            Slot[] quickSlots = slots.Where(slot => fixedSlotNames.Contains(slot.Name)).ToArray();
            return quickSlots;
        }

        static ExtendedPlayerInventory()
        {
            // ensure the fixed slots are at end
            for (int i = 0; i < QuickSlotsCount; ++i)
            {
                slots.Add(new Slot { Name = GetHotkeyText(i) });
            }
        }

        internal static class EquipmentPanel
        {
            internal static float leftOffsetBase = 643f;
            internal static float tileSize = 70f;
            internal static float leftOffsetSeparatePanel = 20f;
            internal static float leftOffsetMinimalUI = 10f;
            
            internal static float LeftOffset
            {
                get => leftOffsetBase
                    + (IsSeparatePanel() ? leftOffsetSeparatePanel : 0)
                    + (IsMinimalUI() ? leftOffsetMinimalUI : 0)
                    + SeparatePanelOffset.Value;
            }

            internal static Dictionary<string, Slot> vanillaSlots = new();

            internal static void InitializeVanillaSlotsOrder()
            {
                vanillaSlots.Add(EquipmentSlot.helmetSlotID, slots[0]);
                vanillaSlots.Add(EquipmentSlot.legsSlotID, slots[1]);
                vanillaSlots.Add(EquipmentSlot.utilitySlotID, slots[2]);
                vanillaSlots.Add(EquipmentSlot.chestSlotID, slots[3]);
                vanillaSlots.Add(EquipmentSlot.backSlotID, slots[4]);
            }

            internal static void ReorderVanillaSlots()
            {
                string[] newSlotsOrder = VanillaSlotsOrder.Value.Split(',').Select(str => str.Trim()).Distinct().ToArray();

                for (int i = 0; i < Mathf.Min(newSlotsOrder.Length, vanillaSlots.Count); i++)
                {
                    if (!vanillaSlots.TryGetValue(newSlotsOrder[i], out Slot slot))
                        continue;

                    int newSlotIndex = slots.IndexOf(slot);
                    int currentSlotIndex = slots.IndexOf(slots.Where(slot => vanillaSlots.ContainsValue(slot)).ToArray()[i]);

                    (slots[newSlotIndex], slots[currentSlotIndex]) = (slots[currentSlotIndex], slots[newSlotIndex]);
                }

                SetSlotsPositions();
            }

            internal static void UpdateVanillaSlotNames()
            {
                vanillaSlots.Do(slot => slot.Value.Name = GetVanillaSlotName(slot.Key));
            }

            internal static string GetVanillaSlotName(string slotName)
            {
                return slotName switch
                {
                    EquipmentSlot.helmetSlotID => HelmetText.Value,
                    EquipmentSlot.legsSlotID => LegsText.Value,
                    EquipmentSlot.utilitySlotID => UtilityText.Value,
                    EquipmentSlot.chestSlotID => ChestText.Value,
                    EquipmentSlot.backSlotID => BackText.Value,
                    _ => ""
                };
            }

            // Runs every frame InventoryGui.UpdateInventory if visible
            internal static void UpdateInventorySlots()
            {
                if (AddEquipmentRow.Value.IsOff())
                    return;

                Inventory inventory = Player.m_localPlayer?.GetInventory();
                if (inventory == null)
                    return;

                int requiredRows = API.GetAddedRows(inventory.GetWidth());

                // Update baseIndex based on the dynamic rows
                int baseIndex = inventory.GetWidth() * (inventory.GetHeight() - requiredRows);

                for (int i = 0; i < slots.Count; ++i)
                {
                    if (baseIndex + i >= InventoryGui.instance.m_playerGrid.m_elements.Count)
                        break;

                    GameObject currentChild = InventoryGui.instance.m_playerGrid.m_elements[baseIndex + i]?.m_go;
                    if (!currentChild)
                        break;

                    currentChild.SetActive(true);
                        
                    SetSlotText(slots[i].Name, currentChild.transform, isQuickSlot: i > EquipmentSlotsCount - 1);
                        
                    if (DisplayEquipmentRowSeparate.Value.IsOn())
                    {
                        currentChild.GetComponent<RectTransform>().anchoredPosition = slots[i].Position;
                    }
                    else
                    {
                        Vector2 baseGridPos = new((InventoryGui.instance.m_playerGrid.GetComponent<RectTransform>().rect.width - InventoryGui.instance.m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);
                        currentChild.GetComponent<RectTransform>().anchoredPosition = baseGridPos + new Vector2((baseIndex + i) % inventory.GetWidth() * InventoryGui.instance.m_playerGrid.m_elementSpace, (baseIndex + i) / inventory.GetWidth() * -InventoryGui.instance.m_playerGrid.m_elementSpace);
                    }
                }

                for (int i = baseIndex + slots.Count; i < InventoryGui.instance.m_playerGrid.m_elements.Count; ++i)
                    InventoryGui.instance.m_playerGrid.m_elements[i].m_go.SetActive(false);
            }

            public static float sideButtonSize = 1.13f;
            public static float offsetMinimalUI = 0.03f;
            public static float offsetSeparatePanel = 0.03f;
            public static float offsetSeparatePanelMax = 0.01f;
            public static float anchorSizeFactor = 570f;

            public static RectTransform inventoryDarken = null!;
            public static RectTransform equipmentDarken = null!;
            public static RectTransform equipmentBackground = null!;

            public static bool IsMinimalUI() => BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("Azumatt.MinimalUI", out var pluginInfo) && pluginInfo is not null;

            public static bool IsSeparatePanel() => DisplayEquipmentRowSeparatePanel.Value.IsOn();

            public static void UpdateInventoryBackground()
            {
                if (!equipmentBackground)
                    return;

                float offset = sideButtonSize;
                if (IsMinimalUI())
                    offset += offsetMinimalUI;

                if (IsSeparatePanel())
                    offset += offsetSeparatePanel;

                float size = Math.Max(QuickSlotsCount, (slots.Count - 1) / 3) * tileSize / anchorSizeFactor;

                Vector2 maxAnchor = new(offset + size + (IsSeparatePanel() ? offsetSeparatePanelMax : 0), 1f);

                inventoryDarken.anchorMax = IsSeparatePanel() ? Vector2.one : maxAnchor;
                equipmentBackground.anchorMax = maxAnchor;
                InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;

                equipmentBackground.anchorMin = IsSeparatePanel() ? new Vector2(offset, 0.0f) : new Vector2(1f, 0.0f);
                equipmentDarken.gameObject.SetActive(IsSeparatePanel());
            }

            internal static void SetSlotsPositions()
            {
                for (int i = 0; i < slots.Count; ++i)
                    slots[i].Position = GetSlotOffset(i);

                static int Column(int i) => i / 3;
                static int Row(int i) => i % 3;
                static int LastEquipmentRow() => Row(EquipmentSlotsCount - 1);
                static int LastEquipmentColumn() => Column(EquipmentSlotsCount - 1);
                static void GetTileOffset(int i, out int x, out int y)
                {
                    // Result in grid size of half tiles
                    if (i < EquipmentSlotsCount)
                    {
                        x = Column(i) * 2 + (EquipmentSlotsAlignment.Value == SlotAlignment.Horizontal && Row(i) > LastEquipmentRow() ? 1 : 0) + (LastEquipmentColumn() < 3 ? 1 : 0);
                        y = Row(i) * 2 + Math.Max(EquipmentSlotsAlignment.Value == SlotAlignment.Vertical && Column(i) == LastEquipmentColumn() ? 2 - LastEquipmentRow() : 0, 0);
                    }
                    else
                    {
                        x = (i - EquipmentSlotsCount) * 2;
                        y = QuickSlotsCount * 2;
                    }
                }
                static Vector2 GetSlotOffset(int i)
                {
                    GetTileOffset(i, out int x, out int y);
                    return new Vector2(LeftOffset + x * tileSize / 2, -y * tileSize / 2);
                }
            }

            // Runs every frame InventoryGui.Update if visible
            internal static void UpdateEquipmentBackground()
            {
                if (!InventoryGui.instance)
                    return;

                if (AddEquipmentRow.Value.IsOff())
                    return;

                RectTransform bkgRect = InventoryGui.instance.m_player.Find("Bkg").GetComponent<RectTransform>();
                bkgRect.anchorMin = new Vector2(0.0f,
                    (ExtraRows.Value +
                     (AddEquipmentRow.Value.IsOff() || DisplayEquipmentRowSeparate.Value.IsOn() ? 0 : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) *
                    -0.25f);

                if (AddEquipmentRow.Value.IsOff())
                    return;

                if (DisplayEquipmentRowSeparate.Value.IsOn() && !equipmentBackground)
                {
                    inventoryDarken = InventoryGui.instance.m_player.Find("Darken").GetComponent<RectTransform>();

                    equipmentBackground = new GameObject(AzuBkgName, typeof(RectTransform)).GetComponent<RectTransform>();
                    equipmentBackground.gameObject.layer = bkgRect.gameObject.layer;
                    equipmentBackground.SetParent(InventoryGui.instance.m_player, worldPositionStays: false);
                    equipmentBackground.SetSiblingIndex(inventoryDarken.GetSiblingIndex() + 1); // In front of Darken element
                    equipmentBackground.offsetMin = Vector2.zero;
                    equipmentBackground.offsetMax = Vector2.zero;

                    equipmentDarken = Object.Instantiate(inventoryDarken, equipmentBackground);
                    equipmentDarken.name = "Darken";
                    equipmentDarken.sizeDelta = Vector2.one * 70f; // Original 100 is too much

                    Transform equipmentBkg = Object.Instantiate(bkgRect.transform, equipmentBackground);
                    equipmentBkg.name = "Bkg";

                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;

                    UpdateInventoryBackground();
                }
                else if (DisplayEquipmentRowSeparate.Value.IsOff() && equipmentBackground)
                {
                    Object.DestroyImmediate(equipmentBackground.gameObject);
                    equipmentBackground = null!;
                }
            }

            internal static void ClearPanel()
            {
                equipmentDarken = null!;
                equipmentBackground = null!;
            }

        }

        internal static class EquipmentSlots
        {
            private static ItemDrop.ItemData[] equippedItems = new ItemDrop.ItemData[5];
            
            internal static bool TryGetSlot(Inventory inventory, ItemDrop.ItemData item, out int position)
            {
                var inventoryRows = inventory.GetHeight() - API.GetAddedRows(inventory.GetWidth());
                if (AddEquipmentRow.Value.IsOff() || item.m_gridPos.y < inventoryRows || (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x >= EquipmentSlotsCount)
                {
                    position = -1;
                    return false;
                }

                position = (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x;
                return true;
            }

            internal static bool IsSlot(Inventory inventory, ItemDrop.ItemData item) => TryGetSlot(inventory, item, out _);

            internal static bool IsFreeSlot(Inventory inventory, ItemDrop.ItemData item, out int position)
            {
                var addedRows = API.GetAddedRows(inventory.GetWidth());
                position = slots.FindIndex(s => s is EquipmentSlot slot && slot.Valid(item));
                return position >= 0 && inventory.GetItemAt(position, inventory.GetHeight() - addedRows) == null;
            }

            // Runs every frame InventoryGui.Update
            internal static void UpdatePlayerInventoryEquipmentSlots()
            {
                if (AddEquipmentRow.Value.IsOff())
                    return;

                var player = Player.m_localPlayer;
                Inventory inventory = player.GetInventory();
                List<ItemDrop.ItemData> allItems = inventory.GetAllItems();

                int width = inventory.GetWidth(); // cache the width
                int height = inventory.GetHeight(); // cache the height
                int requiredRows = API.GetAddedRows(width);

                int num = width * (height - requiredRows);
                ItemDrop.ItemData[] equippedItemsNew = new ItemDrop.ItemData[slots.Count];
                for (int i = 0; i < equippedItemsNew.Length; ++i)
                {
                    Slot slot = slots[i];
                    if (slot is EquipmentSlot equipmentSlot)
                    {
                        if (equipmentSlot.Get(player) is { } item)
                        {
                            item.m_gridPos = new Vector2i(num % width, num / width);
                            equippedItemsNew[i] = item;
                        }

                        ++num;
                    }
                }

                for (int index = 0; index < allItems.Count; ++index)
                {
                    ItemDrop.ItemData item = allItems[index];
                    try
                    {
                        if (TryGetSlot(inventory, item, out int position) &&
                            (position <= -1 || item != equippedItemsNew[position]) &&
                            (position <= -1 || slots[position] is not EquipmentSlot slot || !slot.Valid(item) || equippedItems[position] == item || (AutoEquip.Value.IsOn() && !player.EquipItem(item, false))))
                        {
                            Vector2i vector2I = inventory.FindEmptySlot(true);
                            if (vector2I.x < 0 || vector2I.y < 0 || vector2I.y >= height - requiredRows)
                            {
                                // Technically, the code will handle when it cannot be added before this, but in the case of low durability items
                                // it will drop them simply because it cannot be added to the inventory and it's "outside" the normal inventory when it breaks.
                                // Check if it's a valid item to drop based on manually checking inventory and durability here as well.
                                if (item.m_durability > 0 && !inventory.CanAddItem(item))
                                    player.DropItem(inventory, item, item.m_stack);
                            }
                            else
                            {
                                item.m_gridPos = vector2I;
                                InventoryGui.instance.m_playerGrid?.UpdateInventory(inventory, player, null);
                            }
                        }
                    }
                    catch
                    {
                        // I'm not proud of this one, but it prevents the occasional NRE spam when spawning in for the first time. (and you have weapons in the hidden left/right slots)
                    }
                }

                equippedItems = equippedItemsNew;
            }
        }

        internal static class QuickSlots
        {
            private static RectTransform _quickAccessBar = null!;
            private static float _scaleFactor = 1f;

            internal static void CreateBar()
            {
                _quickAccessBar = Object.Instantiate(Hud.instance.m_rootObject.transform.Find("HotKeyBar"), Hud.instance.m_rootObject.transform, true).GetComponent<RectTransform>();
                _quickAccessBar.name = QABName;
                _quickAccessBar.localPosition = Vector3.zero;
            }

            internal static void ClearBars()
            {
                _quickAccessBar = null!;
                HotkeyBars = null!;
                SelectedHotkeyBarIndex = -1;
            }

            // Runs every frame Hud.Update
            internal static void UpdateHotkeyBars() 
            {
                HotkeyBarController.UpdateBars();
            }

            // Runs every frame Player.Update
            internal static void UpdateItemUse()
            {
                if (!Player.m_localPlayer)
                    return;

                if (Utilities.Utilities.IgnoreKeyPresses(includeExtra: true) || AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.IsOff())
                    return;

                int hotkey = 0;
                while (!AzuExtendedPlayerInventoryPlugin.GetHotkey(hotkey).IsKeyDown())
                    if (++hotkey == AzuExtendedPlayerInventoryPlugin.QuickSlotsCount)
                        return;

                Inventory inventory = Player.m_localPlayer.GetInventory();
                int width = inventory.GetWidth();

                int index = (4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value) * width + AzuExtendedPlayerInventoryPlugin.EquipmentSlotsCount + hotkey;
                ItemDrop.ItemData itemAt = inventory.GetItemAt(index % width, index / width);

                if (itemAt == null)
                    return;

                Player.m_localPlayer.UseItem(inventory, itemAt, true);
            }

            // Runs every frame Hud.Update
            internal static void UpdatePosition()
            {
                if (!_quickAccessBar)
                    return;

                if (QuickAccessX.Value == 9999.0)
                    QuickAccessX.Value = Hud.instance.m_rootObject.transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x - 32f;

                if (QuickAccessY.Value == 9999.0)
                    QuickAccessY.Value = Hud.instance.m_rootObject.transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y - 870f;

                _quickAccessBar.anchoredPosition = new Vector2(QuickAccessX.Value, QuickAccessY.Value);
                _quickAccessBar.localScale = new Vector3(QuickAccessScale.Value, QuickAccessScale.Value, 1f);
            }

            // Runs every frame Hud.Update
            internal static void UpdateDrag()
            {
                Vector3 mousePosition = Input.mousePosition;

                if (_lastMousePos == Vector3.zero)
                    _lastMousePos = mousePosition;

                if (QuickslotDragKeys.Value.IsPressed() && _quickAccessBar != null)
                {
                    Vector2 anchoredPosition = _quickAccessBar.anchoredPosition;
                    Vector2 sizeDelta = _quickAccessBar.sizeDelta;
                    float quickAccessScale = QuickAccessScale.Value;

                    Rect rect = new(
                        anchoredPosition.x * _scaleFactor,
                        (float)(anchoredPosition.y * _scaleFactor + Screen.height - sizeDelta.y * _scaleFactor * quickAccessScale),
                        (float)(sizeDelta.x * _scaleFactor * quickAccessScale * 0.375),
                        sizeDelta.y * _scaleFactor * quickAccessScale);

                    if (rect.Contains(_lastMousePos) && (_currentlyDragging is "" or QABName))
                    {
                        float deltaX = (mousePosition.x - _lastMousePos.x) / _scaleFactor;
                        float deltaY = (mousePosition.y - _lastMousePos.y) / _scaleFactor;

                        QuickAccessX.Value += deltaX;
                        QuickAccessY.Value += deltaY;
                        _currentlyDragging = QABName;
                    }
                    else
                    {
                        _currentlyDragging = "";
                    }
                }
                else
                {
                    _currentlyDragging = "";
                }

                _lastMousePos = mousePosition;
            }

            internal static void DeselectHotkeyBars()
            {
                HotkeyBarController.DeselectBars();
            }

            internal static class HotkeyBarController
            {
                internal static void UpdateBars()
                {
                    var player = Player.m_localPlayer;
                    if (HotkeyBars == null)
                    {
                        try
                        {
                            HotkeyBars = Hud.instance.transform.parent.GetComponentsInChildren<HotkeyBar>().ToList();
                        }
                        catch
                        {
                            AzuExtendedPlayerInventoryLogger.LogError($"Failed to get hotkey bars from Hud. The parent transform may have changed. {Hud.instance.transform.parent.name}");
                            return;
                        }
                    }

                    if (player != null)
                        if (IsValidHotkeyBarIndex())
                            UpdateHotkeyBarInput(HotkeyBars[SelectedHotkeyBarIndex]);
                        else
                            UpdateInitialHotkeyBarInput();

                    foreach (var hotkeyBar in HotkeyBars)
                    {
                        if (hotkeyBar != null && hotkeyBar.m_elements != null)
                        {
                            ValidateHotkeyBarSelection(hotkeyBar);
                            hotkeyBar.UpdateIcons(player);
                        }
                    }
                }
                
                internal static void DeselectBars()
                {
                    SelectedHotkeyBarIndex = -1;

                    if (HotkeyBars != null)
                        foreach (var hotkeyBar in HotkeyBars)
                            hotkeyBar.m_selected = -1;
                }

                private static bool IsValidHotkeyBarIndex() => SelectedHotkeyBarIndex >= 0 && SelectedHotkeyBarIndex < HotkeyBars.Count;

                private static void UpdateInitialHotkeyBarInput()
                {
                    if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight"))
                        SelectHotkeyBar(0, false);
                }

                private static void UpdateHotkeyBarInput(HotkeyBar hotkeyBar)
                {
                    var player = Player.m_localPlayer;
                    var canUseItem = hotkeyBar.m_selected >= 0 && player != null && !InventoryGui.IsVisible()
                                     && !Menu.IsVisible() && !GameCamera.InFreeFly();
                    if (canUseItem && player != null)
                    {
                        if (ZInput.GetButtonDown("JoyDPadLeft"))
                        {
                            if (hotkeyBar.m_selected == 0 && ShowQuickSlots.Value.IsOn())
                            {
                                GoToHotkeyBar(SelectedHotkeyBarIndex - 1);
                            }
                            else
                            {
                                hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_selected - 1);
                            }
                        }
                        else if (ZInput.GetButtonDown("JoyDPadRight"))
                        {
                            if (hotkeyBar.m_selected == hotkeyBar.m_elements.Count - 1 && ShowQuickSlots.Value.IsOn())
                            {
                                GoToHotkeyBar(SelectedHotkeyBarIndex + 1);
                            }
                            else
                            {
                                hotkeyBar.m_selected = Mathf.Min(hotkeyBar.m_elements.Count - 1, hotkeyBar.m_selected + 1);
                            }
                        }

                        if (ZInput.GetButtonDown("JoyDPadUp"))
                        {
                            if (hotkeyBar.name == "QuickAccessBar" && ShowQuickSlots.Value.IsOn())
                            {
                                var quickSlotInventory = player.m_inventory;
                                int width = quickSlotInventory.GetWidth();
                                int adjustedHeight = quickSlotInventory.GetHeight() - API.GetAddedRows(width);
                                int index = adjustedHeight * width + EquipmentSlotsCount + hotkeyBar.m_selected;

                                var item = quickSlotInventory.GetItemAt(index % width, index / width);
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
                    if (hotkeyBar.m_elements != null && hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1)
                        hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_elements.Count - 1);
                }

                private static void GoToHotkeyBar(int newIndex)
                {
                    if (newIndex < 0 || newIndex >= HotkeyBars.Count)
                        return;

                    var fromRight = newIndex < SelectedHotkeyBarIndex;
                    SelectHotkeyBar(newIndex, fromRight);
                }

                private static void SelectHotkeyBar(int index, bool fromRight)
                {
                    if (index < 0 || index >= HotkeyBars.Count)
                        return;

                    SelectedHotkeyBarIndex = index;
                    for (var i = 0; i < HotkeyBars.Count; ++i)
                    {
                        var hotkeyBar = HotkeyBars[i];
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
            }

            [HarmonyPatch(typeof(GuiScaler), nameof(GuiScaler.UpdateScale))]
            private static class GuiScaler_UpdateScale_GetCurrentScale
            {
                private static void Postfix(GuiScaler __instance)
                {
                    if (__instance.name == "LoadingGUI")
                        _scaleFactor = __instance.m_canvasScaler.scaleFactor;
                }
            }
        }

        internal static class DropButton
        {
            public const string DropAllButtonName = "AzuDropAllButton";

            private static RectTransform _dropallButton = null!;
            private static TMP_Text _buttonText = null!;

            // Runs every frame InventoryGui.Update if visible
            internal static void UpdateButton()
            {
                if (AddEquipmentRow.Value.IsOff())
                    return;

                if (MakeDropAllButton.Value.IsOn() && _dropallButton == null)
                {
                    CreateDropButton();
                }
                else if (MakeDropAllButton.Value.IsOff() && _dropallButton != null)
                {
                    Object.DestroyImmediate(_dropallButton.gameObject);
                    _dropallButton = null!;
                }

                if (_dropallButton == null)
                    return;

                _dropallButton.anchoredPosition = DropAllButtonPosition.Value;
                _buttonText?.SetText(DropAllButtonText.Value);
            }

            private static void CreateDropButton()
            {
                if (!InventoryGui.instance)
                    return;

                Transform dropAllButtonPrefab = InventoryGui.instance.m_takeAllButton.transform; // Assuming cloning the take all button
                _dropallButton = Object.Instantiate(dropAllButtonPrefab, InventoryGui.instance.m_player).GetComponent<RectTransform>();
                _dropallButton.name = DropAllButtonName;
                
                // Set the button text
                _buttonText = _dropallButton.GetComponentInChildren<TMP_Text>();

                // Dropall button
                var buttonComp = _dropallButton.GetComponent<Button>();
                // Remove all listeners from the take all button
                buttonComp.onClick.RemoveAllListeners();
                // Add the new listener to the drop all button
                buttonComp.onClick.AddListener(DropAll);

                // Position the drop all button in the top left
                _dropallButton.SetAsFirstSibling();
                _dropallButton.anchorMin = new Vector2(0.0f, 1.0f);
                _dropallButton.anchorMax = new Vector2(0.0f, 1.0f);
                _dropallButton.pivot = new Vector2(0.0f, 1.0f);
                _dropallButton.sizeDelta = new Vector2(100, 30);
            }

            private static void DropAll()
            {
                Console.instance?.TryRunCommand("azuepi.dropall");
            }

            internal static void ClearButton()
            {
                _dropallButton = null!;
                _buttonText = null!;
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_TakeAllRespons))]
    static class ContainerRPCRequestTakeAllPatch
    {
        static void Postfix(bool granted)
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
        static void Postfix(Inventory __instance)
        {
            if (__instance == Player.m_localPlayer?.GetInventory())
                Utilities.Utilities.InventoryFix();
        }
    }
}