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
using System.Runtime.CompilerServices;

namespace AzuExtendedPlayerInventory.EPI
{
    public class Slot
    {
        public const string helmetSlotID = "Helmet";
        public const string legsSlotID = "Legs";
        public const string utilitySlotID = "Utility";
        public const string chestSlotID = "Chest";
        public const string backSlotID = "Back";
        public const string ExtraUtilitySlotID = "ExtraUtility";
        public const string QuickSlotID = "QuickSlot";

#nullable enable
        public string Name = null!;
        public Vector2 Position;
        public EquipmentSlot? EquipmentSlot => this as EquipmentSlot;
        public QuickSlot? QuickSlot => this as QuickSlot;
#nullable disable

        public virtual string GetName() => Name;

        public override string ToString() => GetName();
    }

    public class EquipmentSlot : Slot
    {
#nullable enable
        public Func<Player, ItemDrop.ItemData?> Get = null!;
        public Func<ItemDrop.ItemData, bool> Valid = null!;
#nullable disable
    }

    public class QuickSlot : Slot
    {
        public BepInEx.Configuration.KeyboardShortcut Hotkey;
        public string HotkeyText;

        public override string GetName() => GetHotkeyText();

        public string GetHotkeyText() => HotkeyText == "" ? Hotkey.ToString() : HotkeyText;

        public bool IsDown() => Hotkey.IsKeyDown();
    }

    public class ExtraUtilitySlot : EquipmentSlot
    {
        public override string GetName() => UtilityText.Value;
    }

    public static class ExtendedPlayerInventory
    {
        public const string QABName = "QuickAccessBar";
        public const string AzuBkgName = "AzuEPIEquipmentPanel";

        public const int vanillaInventoryHeight = 4;

        public static int EquipmentSlotsCount => slots.Count - QuickSlotsCount;
        public static int QuickSlotsCount => Hotkeys.Count;
        public static int ExtraUtilitySlotsCount => ExtraUtilitySlotsAmount.Value;
        public static int ExtraUtilitySlotsIndex => Math.Min(ExtraUtilitySlotsPosition.Value, EquipmentSlotsCount);

        public static Inventory PlayerInventory => Player.m_localPlayer?.GetInventory();
        public static int InventoryWidth => PlayerInventory != null ? PlayerInventory.GetWidth() : 8;
        public static int InventoryHeightPlayer => vanillaInventoryHeight + ExtraRows.Value; // The value is stable to temporary changes in inventory height.
        public static int InventoryHeightFull => InventoryHeightPlayer + (AddEquipmentRow.Value.IsOn() ? API.GetAddedRows(InventoryWidth) : 0); // The value is stable to temporary changes in inventory height.

        public static int InventorySizePlayer => InventoryHeightPlayer * InventoryWidth; // The value is stable to temporary changes in inventory height.
        public static int InventorySizeFull => InventorySizePlayer + slots.Count; // The value is stable to temporary changes in inventory height and excludes redundats hidden slots.

        public static int GetTargetInventoryHeight(int inventorySize, int inventoryWidth) => GetExtraRowsForItemsToFit(inventorySize, inventoryWidth);

        public static int GetExtraRowsForItemsToFit(int itemsAmount, int rowWidth) => ((itemsAmount - 1) / rowWidth) + 1;

        private static readonly int _visible = Animator.StringToHash("visible");

        internal static bool IsVisible() => InventoryGui.instance && InventoryGui.instance.m_animator.GetBool(_visible);

        internal static void SetSlotText(string value, Transform transform, bool isQuickSlot)
        {
            if (equipmentSlotLabelHideQuality.Value.IsOn())
            {
                Transform quality = transform.Find("quality");
                if (quality)
                {
                    quality.GetComponent<TMP_Text>().SetText("");
                    quality.gameObject.SetActive(false);
                }
            }

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

        internal static void ShiftExtraInventorySlots(int startingIndex, int shift, bool forceUpdateInventory = false)
        {
            if (!Player.m_localPlayer)
                return;

            bool itemsChanged = false;

            // slots list should be changed appropriately before this call
            while (shift != 0)
            {
                foreach (ItemDrop.ItemData item in Player.m_localPlayer.GetInventory()?.GetAllItems())
                {
                    // Check to ignore regular inventory
                    if (item.m_gridPos.y < InventoryHeightPlayer)
                        continue;

                    if ((item.m_gridPos.y - InventoryHeightPlayer) * InventoryWidth + item.m_gridPos.x >= startingIndex)
                    {
                        itemsChanged = true;
                        item.m_gridPos.x += shift;
                        if (item.m_gridPos.x < 0)
                        {
                            item.m_gridPos.x = InventoryWidth - 1;
                            --item.m_gridPos.y;
                        }
                        if (item.m_gridPos.x >= InventoryWidth)
                        {
                            item.m_gridPos.x = 0;
                            ++item.m_gridPos.y;
                        }
                    }
                }

                if (shift > 0)
                    --shift;
                else
                    ++shift;
            }

            if (itemsChanged || forceUpdateInventory)
                UpdatePlayerInventorySize();

            EquipmentPanel.UpdatePanel();
            QuickSlots.MarkDirty();
        }

        public static void UpdatePlayerInventorySize()
        {
            if (Player.m_localPlayer == null)
                return;

            Player.m_localPlayer.m_inventory.m_height = InventoryHeightFull;
            Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height = InventoryHeightFull;
            Player.m_localPlayer.m_inventory.Changed();

            CheckPlayerInventoryItemsOverlappingOrOutOfGrid();
        }

        public static void CheckPlayerInventoryItemsOverlappingOrOutOfGrid()
        {
            if (Player.m_localPlayer == null)
                return;

            if (PlayerInventory == null || PlayerInventory.m_inventory == null)
                return;

            PlayerInventory.m_inventory.RemoveAll(item => item == null || item.m_stack <= 0);

            HashSet<Vector2i> occupiedPositions = new();
            List<ItemDrop.ItemData> itemsToFix = new();
            for (int index = 0; index < PlayerInventory.m_inventory.Count; index++)
            {
                ItemDrop.ItemData itemData = PlayerInventory.m_inventory[index];

                bool overlappingItem = occupiedPositions.Contains(itemData.m_gridPos);
                if (overlappingItem || IsOutOfGrid(itemData) || !EquipmentSlots.ItemFitAtCurrentSlot(itemData))
                {
                    AzuExtendedPlayerInventoryLogger.LogWarning(
                        overlappingItem
                            ? $"Item {Localization.instance.Localize(itemData.m_shared.m_name)} was overlapping another item in the player inventory grid, moving to first available slot or dropping if no slots are available."
                            : $"Item {Localization.instance.Localize(itemData.m_shared.m_name)} was outside player inventory grid, moving to first available slot or dropping if no slots are available.");
                    itemsToFix.Add(itemData);
                }

                occupiedPositions.Add(itemData.m_gridPos);
            }

            itemsToFix.Do(TryRemoveAndAddItemToInventory);

            if (itemsToFix.Count > 0)
                PlayerInventory.Changed();

            static bool IsOutOfGrid(ItemDrop.ItemData itemData) => itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= InventoryWidth 
                                                                || itemData.m_gridPos.y < 0 || itemData.m_gridPos.y >= InventoryHeightFull
                                                                || itemData.m_gridPos.y == InventoryHeightFull - 1 && itemData.m_gridPos.x >= InventorySizeFull % InventoryWidth;
        }

        private static void TryRemoveAndAddItemToInventory(ItemDrop.ItemData itemData)
        {
            // If item removed, but can't be added back or add attempt failed - drop item
            if (!(PlayerInventory.CanAddItem(itemData) && PlayerInventory.RemoveItem(itemData) && PlayerInventory.AddItem(itemData)))
            {
                if (!Player.m_localPlayer.DropItem(PlayerInventory, itemData, itemData.m_stack))
                    DropAnyway(Player.m_localPlayer, itemData);
                
                AzuExtendedPlayerInventoryLogger.LogWarning($"Item {Localization.instance.Localize(itemData.m_shared.m_name)} dropped after failed attempt to add to player inventory");
            }

            static void DropAnyway(Player player, ItemDrop.ItemData item)
            {
                player.RemoveEquipAction(item);
                player.UnequipItem(item, triggerEquipEffects: false);
                if (player.m_hiddenLeftItem == item)
                {
                    player.m_hiddenLeftItem = null;
                    player.SetupVisEquipment(player.m_visEquipment, isRagdoll: false);
                }

                if (player.m_hiddenRightItem == item)
                {
                    player.m_hiddenRightItem = null;
                    player.SetupVisEquipment(player.m_visEquipment, isRagdoll: false);
                }

                player.GetInventory().RemoveItem(item);

                ItemDrop itemDrop = ItemDrop.DropItem(item, item.m_stack, player.transform.position + player.transform.forward + player.transform.up, player.transform.rotation);
                    itemDrop.OnPlayerDrop();

                player.m_zanim.SetTrigger("interact");
                player.m_dropEffects.Create(player.transform.position, Quaternion.identity);
                player.Message(MessageHud.MessageType.TopLeft, "$msg_dropped " + itemDrop.m_itemData.m_shared.m_name, itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
            }
        }

        internal static void UpdateQuickSlots()
        {
            int slotsCount = slots.Count;

            slots.RemoveAll(slot => slot.Name.StartsWith(Slot.QuickSlotID));
            
            for (int i = 0; i < QuickSlotsCount; i++)
                slots.Add(new QuickSlot { Name = $"{Slot.QuickSlotID}{i + 1}", Hotkey = Hotkeys[i].Item1.Value, HotkeyText = Hotkeys[i].Item2.Value });

            ShiftExtraInventorySlots(slots.Count, slots.Count - slotsCount);
        }

        internal static void UpdateExtraUtilitySlots()
        {
            int slotsCount = slots.Count;
            
            slots.RemoveAll(slot => slot.Name.StartsWith(Slot.ExtraUtilitySlotID));

            if (ExtraUtilitySlotsCount > 1)
                slots.Insert(ExtraUtilitySlotsIndex, new ExtraUtilitySlot { Name = $"{Slot.ExtraUtilitySlotID}2", Get = EquipmentSlots.ExtraUtilitySlots.GetExtraSlot2, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility });
            
            if (ExtraUtilitySlotsCount > 0)
                slots.Insert(ExtraUtilitySlotsIndex, new ExtraUtilitySlot { Name = $"{Slot.ExtraUtilitySlotID}1", Get = EquipmentSlots.ExtraUtilitySlots.GetExtraSlot1, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility });

            ShiftExtraInventorySlots(ExtraUtilitySlotsIndex, slots.Count - slotsCount, forceUpdateInventory: true);
        }

        internal static class EquipmentPanel
        {
            internal static Dictionary<string, Slot> vanillaSlots = new();

            internal static void InitializeVanillaSlotsOrder()
            {
                vanillaSlots.Add(EquipmentSlot.helmetSlotID,    slots[0]);
                vanillaSlots.Add(EquipmentSlot.legsSlotID,      slots[1]);
                vanillaSlots.Add(EquipmentSlot.utilitySlotID,   slots[2]);
                vanillaSlots.Add(EquipmentSlot.chestSlotID,     slots[3]);
                vanillaSlots.Add(EquipmentSlot.backSlotID,      slots[4]);
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

            internal static void UpdatePanel()
            {
                UpdateBackground();
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
                    Slot.helmetSlotID => HelmetText.Value,
                    Slot.legsSlotID => LegsText.Value,
                    Slot.utilitySlotID => UtilityText.Value,
                    Slot.chestSlotID => ChestText.Value,
                    Slot.backSlotID => BackText.Value,
                    _ => ""
                };
            }

            // Runs every frame InventoryGui.UpdateInventory if visible
            internal static void UpdateInventorySlots()
            {
                if (AddEquipmentRow.Value.IsOff())
                    return;

                if (PlayerInventory == null)
                    return;

                if (!InventoryGui.instance.m_playerGrid)
                    return;

                int startIndex = InventorySizePlayer;
                for (int i = 0; i < slots.Count; ++i)
                {
                    if (startIndex + i >= InventoryGui.instance.m_playerGrid.m_elements.Count)
                        break;

                    GameObject currentChild = InventoryGui.instance.m_playerGrid.m_elements[startIndex + i]?.m_go;
                    if (!currentChild)
                        continue;

                    currentChild.SetActive(true);

                    SetSlotText(slots[i].GetName(), currentChild.transform, isQuickSlot: i > EquipmentSlotsCount - 1);

                    if (DisplayEquipmentRowSeparate.Value.IsOn())
                    {
                        currentChild.GetComponent<RectTransform>().anchoredPosition = slots[i].Position;
                    }
                    else
                    {
                        Vector2 baseGridPos = new((InventoryGui.instance.m_playerGrid.GetComponent<RectTransform>().rect.width - InventoryGui.instance.m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);
                        currentChild.GetComponent<RectTransform>().anchoredPosition = baseGridPos + new Vector2((startIndex + i) % InventoryWidth * InventoryGui.instance.m_playerGrid.m_elementSpace, 
                                                                                                                (startIndex + i) / InventoryWidth * -InventoryGui.instance.m_playerGrid.m_elementSpace);
                    }
                }

                for (int i = startIndex + slots.Count; i < InventoryGui.instance.m_playerGrid.m_elements.Count; i++)
                    InventoryGui.instance.m_playerGrid.m_elements[i]?.m_go?.SetActive(false);
            }

            private const float separatePanelLeftOffsetExtra = 20f;
            private const float tileSpace = 6f;
            private static float TileSize => 64f + tileSpace;
            private static float InventoryWidth => InventoryGui.instance ? InventoryGui.instance.m_player.rect.width : 0;
            private static float PanelWidth => Math.Max(QuickSlotsCount, LastEquipmentColumn() + 1) * TileSize + tileSpace;
            private static float PanelHeight => (QuickSlotsCount > 0 ? 4 : 3) * TileSize + (IsMinimalUI() ? 0 : tileSpace);
            private static float PanelLeftOffset => EquipmentPanelLeftOffset.Value;

            internal static Vector2 PanelOffset
            {
                // Top Left
                get 
                {
                    if (!IsSeparatePanel())
                        return Vector2.zero;

                    Vector2 vector = SeparatePanelOffset.Value
                                  + (SeparatePanelOffset.Value == Vector2.zero && IsMinimalUI() ? new Vector2(10f, 0f) : Vector2.zero)
                                  + (SeparatePanelOffset.Value == Vector2.zero ? new Vector2(separatePanelLeftOffsetExtra, 0f) : Vector2.zero);

                    return new Vector2(vector.x, -vector.y);
                }
            }

            internal static Vector2 PanelPosition => new Vector2(InventoryWidth + PanelLeftOffset, 0f) + PanelOffset;

            public static RectTransform inventoryDarken = null!;
            public static RectTransform inventoryBackground = null!;
            public static Image inventoryBackgroundImage = null!;
            public static RectTransform equipmentDarken = null!;
            public static RectTransform equipmentBackground = null!;
            public static Image equipmentBackgroundImage = null!;

            public static bool IsMinimalUI() => BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("Azumatt.MinimalUI", out var pluginInfo) && pluginInfo is not null;

            public static bool IsSeparatePanel() => DisplayEquipmentRowSeparatePanel.Value.IsOn();

            public static void UpdateBackground()
            {
                if (!equipmentBackground)
                    return;

                equipmentBackground.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                equipmentBackground.anchoredPosition = PanelPosition + new Vector2(PanelWidth / 2, -PanelHeight / 2);

                if (!IsSeparatePanel())
                    equipmentBackground.offsetMin -= new Vector2(InventoryWidth / 2f, 0f);

                inventoryDarken.offsetMax = new Vector2(inventoryDarken.offsetMax.y + (IsSeparatePanel() ? 0f : PanelOffset.x + PanelWidth + PanelLeftOffset), inventoryDarken.offsetMax.y); ;
                
                equipmentDarken.gameObject.SetActive(IsSeparatePanel());
            }

            internal static void SetSlotsPositions()
            {
                for (int i = 0; i < slots.Count; ++i)
                    slots[i].Position = GetSlotOffset(i);
            }

            private static int Column(int i) => i / 3;
            private static int Row(int i) => i % 3;
            private static int LastEquipmentRow() => Row(EquipmentSlotsCount - 1);
            private static int LastEquipmentColumn() => Column(EquipmentSlotsCount - 1);
            private static void GetTileOffset(int i, out int x, out int y)
            {
                // Result in grid size of half tiles
                if (i < EquipmentSlotsCount)
                {
                    x = Column(i) * 2
                        // Horizontal offset for rows with insuccifient columns
                        + (EquipmentSlotsAlignment.Value == SlotAlignment.VerticalTopHorizontalMiddle && Row(i) > LastEquipmentRow() ? 1 : 0)
                        // Offset for equipment positioning in the middle if quickslots amount is more than equipment columns
                        + Math.Max(QuickSlotsCount - 1 - LastEquipmentColumn(), 0);
                    
                    y = Row(i) * 2 
                        // Offset for last column and vertical alignment in the middle
                        + Math.Max(EquipmentSlotsAlignment.Value == SlotAlignment.VerticalMiddleHorizontalLeft && Column(i) == LastEquipmentColumn() ? 2 - LastEquipmentRow() : 0, 0);
                }
                else
                {
                    x = (i - EquipmentSlotsCount) * 2
                        // Offset for quickslots positioning in the middle if equipment columns is more than quickslots
                        + Math.Max(QuickSlotsAlignmentCenter.Value.IsOn() ? LastEquipmentColumn() + 1 - QuickSlotsCount : 0, 0);
                    y = 3 * 2;
                }
            }
            private static Vector2 GetSlotOffset(int i)
            {
                GetTileOffset(i, out int x, out int y);
                return PanelPosition + new Vector2(x * TileSize / 2, -y * TileSize / 2);
            }

            // Runs every frame InventoryGui.Update if visible
            internal static void UpdateEquipmentBackground()
            {
                if (!InventoryGui.instance)
                    return;

                if (AddEquipmentRow.Value.IsOff())
                    return;

                inventoryBackground ??= InventoryGui.instance.m_player.Find("Bkg").GetComponent<RectTransform>();
                if (!inventoryBackground)
                    return;

                inventoryBackground.anchorMin = new Vector2(0.0f,
                    (ExtraRows.Value +
                     (AddEquipmentRow.Value.IsOff() || DisplayEquipmentRowSeparate.Value.IsOn() ? 0 : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) *
                    -0.25f);

                if (AddEquipmentRow.Value.IsOff())
                    return;

                if (DisplayEquipmentRowSeparate.Value.IsOn() && !equipmentBackground)
                {
                    inventoryDarken = InventoryGui.instance.m_player.Find("Darken").GetComponent<RectTransform>();

                    equipmentBackground = new GameObject(AzuBkgName, typeof(RectTransform)).GetComponent<RectTransform>();
                    equipmentBackground.gameObject.layer = inventoryBackground.gameObject.layer;
                    equipmentBackground.SetParent(InventoryGui.instance.m_player, worldPositionStays: false);
                    equipmentBackground.SetSiblingIndex(inventoryDarken.GetSiblingIndex() + 1); // In front of Darken element
                    equipmentBackground.offsetMin = Vector2.zero;
                    equipmentBackground.offsetMax = Vector2.zero;
                    equipmentBackground.sizeDelta = Vector2.zero;
                    equipmentBackground.anchoredPosition = Vector2.zero;
                    equipmentBackground.anchorMin = new Vector2(0f, 1f);
                    equipmentBackground.anchorMax = new Vector2(0f, 1f);

                    equipmentDarken = Object.Instantiate(inventoryDarken, equipmentBackground);
                    equipmentDarken.name = "Darken";
                    equipmentDarken.sizeDelta = Vector2.one * 70f; // Original 100 is too much

                    Transform equipmentBkg = Object.Instantiate(inventoryBackground.transform, equipmentBackground);
                    equipmentBkg.name = "Bkg";

                    equipmentBackgroundImage = equipmentBkg.GetComponent<Image>();
                    inventoryBackgroundImage = inventoryBackground.transform.GetComponent<Image>();

                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false; // shudnal: Is it really needed?

                    UpdateBackground();
                }
                else if (DisplayEquipmentRowSeparate.Value.IsOff() && equipmentBackground)
                {
                    Object.DestroyImmediate(equipmentBackground.gameObject);
                    equipmentBackground = null!;
                }

                if (inventoryBackgroundImage && equipmentBackgroundImage)
                {
                    equipmentBackgroundImage.sprite = inventoryBackgroundImage.sprite;
                    equipmentBackgroundImage.overrideSprite = inventoryBackgroundImage.overrideSprite;
                    equipmentBackgroundImage.color = inventoryBackgroundImage.color;
                }
            }

            internal static void ClearPanel()
            {
                inventoryDarken = null!;
                inventoryBackground = null!;
                equipmentDarken = null!;
                equipmentBackground = null!;
                equipmentBackgroundImage = null!;
                inventoryBackgroundImage = null!;
            }
        }

        public static class EquipmentSlots
        {
            private static bool isDirty = false;

            public static List<ItemDrop.ItemData> GetItems(bool equippedOnly = false)
            {
                List<ItemDrop.ItemData> equipmentSlotItems = new();

                if (PlayerInventory == null)
                    return equipmentSlotItems;

                for (int i = 0; i < EquipmentSlotsCount; i++)
                {
                    ItemDrop.ItemData item = GetItemInSlot(i);
                    if (item != null && (!equippedOnly || Player.m_localPlayer.IsItemEquiped(item)))
                        equipmentSlotItems.Add(item);
                }

                return equipmentSlotItems;
            }

            /// <summary>
            /// Checks if slots position could be transformed into inventory grid position
            /// </summary>
            /// <param name="slotIndex"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public static bool TryGetSlotGridPosition(int slotIndex, out int x, out int y)
            {
                x = slotIndex % InventoryWidth;
                y = (slotIndex / InventoryWidth) + InventoryHeightPlayer;
                return AddEquipmentRow.Value.IsOn() && 0 <= slotIndex && slotIndex < EquipmentSlotsCount;
            }

            /// <summary>
            /// Tries to get item from given slot index
            /// </summary>
            /// <param name="slotIndex"></param>
            /// <returns></returns>
            public static ItemDrop.ItemData GetItemInSlot(int slotIndex)
            {
                if (TryGetSlotGridPosition(slotIndex, out int x, out int y) && PlayerInventory != null)
                    return PlayerInventory.GetItemAt(x, y);

                return null;
            }

            /// <summary>
            /// Checks if slot is occupied by any item
            /// </summary>
            /// <param name="slotIndex"></param>
            /// <returns></returns>
            public static bool HasItemInSlot(int slotIndex)
            {
                return GetItemInSlot(slotIndex) != null;
            }

            /// <summary>
            /// Checks if inventory grid position could be transformed in slots position
            /// </summary>
            /// <param name="gridPos"></param>
            /// <param name="slotIndex"></param>
            /// <returns></returns>
            public static bool TryGetSlotIndex(Vector2i gridPos, out int slotIndex)
            {
                slotIndex = (gridPos.y - InventoryHeightPlayer) * InventoryWidth + gridPos.x;
                return AddEquipmentRow.Value.IsOn() && 0 <= slotIndex && slotIndex < EquipmentSlotsCount;
            }

            /// <summary>
            /// Checks if inventory grid position is equipment slot
            /// </summary>
            /// <param name="gridPos"></param>
            /// <returns></returns>
            public static bool IsEquipmentSlotInGrid(Vector2i gridPos) => TryGetSlotIndex(gridPos, out _);

            /// <summary>
            /// Returns true and item position in slots if item is at slot, returns false otherwise
            /// </summary>
            /// <param name="item"></param>
            /// <param name="slotIndex"></param>
            /// <returns></returns>
            public static bool TryGetItemSlot(ItemDrop.ItemData item, out int slotIndex)
            {
                slotIndex = -1;
                return item != null && TryGetSlotIndex(item.m_gridPos, out slotIndex) && AddEquipmentRow.Value.IsOn();
            }

            /// <summary>
            /// Checks for is item located in any of equipment slots
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public static bool IsItemAtSlot(ItemDrop.ItemData item) => TryGetItemSlot(item, out _);

            /// <summary>
            /// Finds a free valid slot for item and returns its position in slots
            /// </summary>
            /// <param name="item"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public static bool TryFindFreeSlotForItem(ItemDrop.ItemData item, out int x, out int y)
            {
                return TryGetSlotGridPosition(slots.FindIndex(slot => IsFreeSlotForItem(item, slots.IndexOf(slot))), out x, out y);
            }

            /// <summary>
            /// Checks if slot at position is valid for item
            /// </summary>
            /// <param name="item"></param>
            /// <param name="slotIndex"></param>
            /// <returns></returns>
            public static bool IsValidItemForSlot(ItemDrop.ItemData item, int slotIndex)
            {
                if (item == null)
                    return false;

                if (slotIndex < 0 || slotIndex >= EquipmentSlotsCount)
                    return false;

                return slots[slotIndex] is EquipmentSlot slot && slot.Valid(item);
            }

            /// <summary>
            /// Checks if item is at incorrect equipment slot
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public static bool ItemFitAtCurrentSlot(ItemDrop.ItemData item)
            {
                if (!TryGetItemSlot(item, out int slotIndex))
                    return true;
    
                return IsValidItemForSlot(item, slotIndex);
            }

            /// <summary>
            /// Checks if slot at position is valid for item and is free
            /// </summary>
            /// <param name="item"></param>
            /// <param name="slotIndex"></param>
            /// <returns></returns>
            public static bool IsFreeSlotForItem(ItemDrop.ItemData item, int slotIndex)
            {
                return IsValidItemForSlot(item, slotIndex) && !HasItemInSlot(slotIndex);
            }

            // Runs on Player Inventory Changed
            internal static void MarkDirty()
            {
                isDirty = true;
            }

            internal static void ValidateSlotsAndAutoEquip()
            {
                if (!isDirty || !Player.m_localPlayer || Player.m_localPlayer.m_isLoading)
                    return;

                isDirty = false;

                if (AddEquipmentRow.Value.IsOff())
                    return;

                for (int i = 0; i < EquipmentSlotsCount; i++)
                {
                    if (slots[i] is not EquipmentSlot slot)
                        continue;

                    ItemDrop.ItemData itemGrid = GetItemInSlot(i);
                    ItemDrop.ItemData itemSlot = slot.Get != null ? slot.Get(Player.m_localPlayer) : null;

                    if (itemGrid == null && itemSlot == null)
                        continue;

                    if (!TryGetSlotGridPosition(i, out int x, out int y))
                        continue;

                    Vector2i pos = new(x, y);

                    // Put item to slot if slot function tells us to
                    if (itemGrid == null && itemSlot != null)
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Item {itemSlot.m_shared.m_name} {itemSlot.m_gridPos} was moved into slot {slot} {pos}");

                        itemSlot.m_gridPos = pos;
                        itemGrid = GetItemInSlot(i);

                        if (AutoEquip.Value.IsOn() && itemGrid.IsEquipable() && itemGrid.m_durability > 0)
                            Player.m_localPlayer.EquipItem(itemGrid, false);
                    }
                    // Swap items if slot function tells us to
                    else if (itemGrid != null && itemSlot != null && itemSlot.m_gridPos != pos && itemGrid.m_gridPos == pos)
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Item {itemSlot.m_shared.m_name} {itemSlot.m_gridPos} was swapped into slot {slot} with item {itemGrid.m_shared.m_name} {itemGrid.m_gridPos}");

                        bool wasEquipped = Player.m_localPlayer.IsItemEquiped(itemGrid) || Player.m_localPlayer.IsItemEquiped(itemSlot);

                        Player.m_localPlayer.UnequipItem(itemSlot);
                        Player.m_localPlayer.UnequipItem(itemGrid);

                        (itemSlot.m_gridPos, itemGrid.m_gridPos) = (itemGrid.m_gridPos, itemSlot.m_gridPos);

                        itemGrid = GetItemInSlot(i);

                        if ((AutoEquip.Value.IsOn() || wasEquipped) && !Player.m_localPlayer.IsItemEquiped(itemGrid) && itemGrid.IsEquipable() && itemGrid.m_durability > 0)
                            Player.m_localPlayer.EquipItem(itemSlot, false);
                    }

                    ValidateItem(itemGrid, i);
                }

                static void ValidateItem(ItemDrop.ItemData item, int slotIndex)
                {
                    if (item == null)
                        return;

                    if (!IsValidItemForSlot(item, slotIndex))
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Item {item.m_shared.m_name} unfit slot {slots[slotIndex]}");
                        // Keep item in slot until there is emtpy slot to move it
                        if (!PutIntoFirstEmptySlot(item))
                            return;
                    }

                    if (AutoEquip.Value.IsOn() && !Player.m_localPlayer.IsItemEquiped(item) && item.IsEquipable() && item.m_durability > 0)
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Autoequip item {item.m_shared.m_name} {item.m_gridPos} {slots[slotIndex]}");
                        Player.m_localPlayer.EquipItem(item, false);
                    }

                    if (KeepUnequippedInSlot.Value.IsOff() && !Player.m_localPlayer.IsItemEquiped(item) && item.IsEquipable())
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Item {item.m_shared.m_name} {item.m_gridPos} at slot {slots[slotIndex]} is unequipped and should be removed");
                        // Keep item in slot until there is emtpy slot to move it
                        if (!PutIntoFirstEmptySlot(item))
                            return;
                    }
                }

                static bool PutIntoFirstEmptySlot(ItemDrop.ItemData item)
                {
                    Vector2i gridPos = PlayerInventory.FindEmptySlot(true);
                    if (gridPos.x > -1 && gridPos.y > -1)
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Item {item.m_shared.m_name} {item.m_gridPos} was put into first free slot {gridPos}");
                        item.m_gridPos = gridPos;
                        return true;
                    }

                    if (TryFindFreeSlotForItem(item, out int x, out int y))
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo($"Item {item.m_shared.m_name} {item.m_gridPos} was put into first free valid equipment slot {new Vector2i(x, y)}");
                        item.m_gridPos = new Vector2i(x, y);
                        return true;
                    }

                    return false;
                }
            }

            public static class ExtraUtilitySlots
            {
                public static ItemDrop.ItemData GetExtraSlot1(Player player)
                {
                    return player.GetExtraUtility(0);
                }

                public static ItemDrop.ItemData GetExtraSlot2(Player player)
                {
                    return player.GetExtraUtility(1);
                }

                public static IEnumerable<ItemDrop.ItemData> GetItems(bool equippedOnly = false)
                {
                    return EquipmentSlots.GetItems(equippedOnly).Where(item => item != Player.m_localPlayer.m_utilityItem && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility);
                }

                public static bool TryGetUtilityItemIndex(ItemDrop.ItemData item, out int utilityIndex)
                {
                    utilityIndex = -1;
                    if (!TryGetItemSlot(item, out int slotIndex))
                        return false;

                    utilityIndex = slotIndex - EquipmentPanel.vanillaSlots.Count;
                    return 0 <= utilityIndex && utilityIndex < 2;
                }

                public static bool IsItemEquipped(Humanoid human, ItemDrop.ItemData item)
                {
                    return item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && TryGetUtilityItemIndex(item, out int utilityIndex) && human.GetExtraUtility(utilityIndex) != null;
                }
            }
        }

        public static class QuickSlots
        {
            private static RectTransform _quickAccessBar = null!;
            private static float _scaleFactor = 1f;

            private static Vector3 _lastMousePos;
            private static string _currentlyDragging = null!;

            private static bool isDirty = false;

            public static QuickSlot[] GetSlots() => slots.Where(slot => slot.QuickSlot is not null).Select(slot => slot.QuickSlot).ToArray();

            public static bool HaveEmptySlot() => TryFindFreeSlotForItem(out _, out _);

            public static int GetEmptySlots()
            {
                return QuickSlotsAmount.Value - GetItems().Count;
            }

            public static Vector2i FindEmptySlot()
            {
                return TryFindFreeSlotForItem(out int x, out int y) ? new Vector2i(x, y) : new Vector2i(-1, -1);
            }

            public static List<ItemDrop.ItemData> GetItems()
            {
                List<ItemDrop.ItemData> quickSlotItems = new();

                if (PlayerInventory == null)
                    return quickSlotItems;

                for (int i = 0; i < QuickSlotsCount; i++)
                {
                    ItemDrop.ItemData item = GetItemInSlot(EquipmentSlotsCount + i);
                    if (item != null)
                        quickSlotItems.Add(item);
                }

                return quickSlotItems;
            }

            public static List<ItemDrop.ItemData> GetItemsToRender()
            {
                List<ItemDrop.ItemData> list = new();
                if (PlayerInventory == null)
                    return list;

                for (int i = 1; i <= QuickSlotsCount; i++)
                {
                    ItemDrop.ItemData item = GetItemInSlot(slots.Count - i);
                    if (item != null || list.Count > 0)
                        list.Add(item);
                }
                
                list.Reverse();
                return list;
            }

            public static bool TryGetSlotGridPosition(int slotIndex, out int x, out int y)
            {
                x = slotIndex % InventoryWidth;
                y = (slotIndex / InventoryWidth) + InventoryHeightPlayer;
                return AddEquipmentRow.Value.IsOn() && 0 <= slotIndex - EquipmentSlotsCount && slotIndex - EquipmentSlotsCount < QuickSlotsCount;
            }

            public static bool TryFindFreeSlotForItem(out int x, out int y)
            {
                return TryGetSlotGridPosition(slots.FindIndex(slot => slot is QuickSlot && !HasItemInSlot(slots.IndexOf(slot))), out x, out y);
            }

            public static ItemDrop.ItemData GetItemInSlot(int slotIndex)
            {
                if (TryGetSlotGridPosition(slotIndex, out int x, out int y) && PlayerInventory != null)
                    return PlayerInventory.GetItemAt(x, y);

                return null;
            }

            public static bool HasItemInSlot(int slotIndex)
            {
                return GetItemInSlot(slotIndex) != null;
            }

            internal static void CreateBar()
            {
                _quickAccessBar = Object.Instantiate(Hud.instance.m_rootObject.transform.Find("HotKeyBar"), Hud.instance.m_rootObject.transform, true).GetComponent<RectTransform>();
                _quickAccessBar.name = QABName;
                _quickAccessBar.localPosition = Vector3.zero;
            }

            internal static void MarkDirty()
            {
                isDirty = true;
            }

            internal static void UpdateHotkeyBar(HotkeyBar __instance, Player player)
            {
                if (ShowQuickSlots.Value.IsOff() || !player || player.IsDead())
                {
                    foreach (HotkeyBar.ElementData element in __instance.m_elements)
                        Object.Destroy(element.m_go);

                    __instance.m_elements.Clear();
                    return;
                }

                List<ItemDrop.ItemData> itemsToRender = GetItemsToRender();

                __instance.m_items.Clear();
                __instance.m_items.AddRange(itemsToRender.Where(item => item != null));

                if (__instance.m_elements.Count != itemsToRender.Count || isDirty)
                {
                    foreach (HotkeyBar.ElementData element in __instance.m_elements)
                        Object.Destroy(element.m_go);

                    __instance.m_elements.Clear();
                    for (int index = 0; index < itemsToRender.Count; index++)
                    {
                        HotkeyBar.ElementData elementData = new()
                        {
                            m_go = Object.Instantiate(__instance.m_elementPrefab, __instance.transform),
                        };

                        elementData.m_go.transform.localPosition = new Vector3(index * __instance.m_elementSpace, 0.0f, 0.0f);
                        elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
                        elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
                        elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<TMP_Text>();
                        elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
                        elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
                        elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;

                        SetSlotText(slots[EquipmentSlotsCount + index].GetName(), elementData.m_go.transform, isQuickSlot: true);

                        __instance.m_elements.Add(elementData);
                    }

                    isDirty = false;
                }

                bool flag = ZInput.IsGamepadActive();
                for (int index = 0; index < __instance.m_elements.Count; index++)
                {
                    ItemDrop.ItemData itemData = itemsToRender[index];
                    HotkeyBar.ElementData element = __instance.m_elements[index];

                    element.m_used = itemData != null;
                    element.m_selection.SetActive(flag && index == __instance.m_selected);

                    if (!element.m_used)
                    {
                        element.m_icon.gameObject.SetActive(false);
                        element.m_durability.gameObject.SetActive(false);
                        element.m_equiped.SetActive(false);
                        element.m_queued.SetActive(false);
                        element.m_amount.gameObject.SetActive(false);
                        continue;
                    }

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
                            element.m_amount.text = WbInstalled
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

            internal static void ClearBar()
            {
                _quickAccessBar = null!;
            }

            // Runs every frame Player.Update
            internal static void UpdateItemUse()
            {
                if (!Player.m_localPlayer)
                    return;

                if (Utilities.Utilities.IgnoreKeyPresses(includeExtra: true) || AddEquipmentRow.Value.IsOff())
                    return;

                QuickSlot[] quickSlots = GetSlots();
                if (quickSlots.Length == 0)
                    return;

                int hotkey = 0;
                while (!quickSlots[hotkey].IsDown())
                    if (++hotkey == QuickSlotsCount)
                        return;

                ItemDrop.ItemData itemAt = GetItemInSlot(EquipmentSlotsCount + hotkey);
                if (itemAt == null)
                    return;

                Player.m_localPlayer.UseItem(PlayerInventory, itemAt, true);
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
                ExtendedPlayerInventory.CheckPlayerInventoryItemsOverlappingOrOutOfGrid();
        }
    }

    [Serializable]
    public class HumanoidExtraUtilitySlots
    {
        public ItemDrop.ItemData utility1 = null!;
        public ItemDrop.ItemData utility2 = null!;
    }

    public static class HumanoidExtension
    {
        private static readonly ConditionalWeakTable<Humanoid, HumanoidExtraUtilitySlots> data = new ConditionalWeakTable<Humanoid, HumanoidExtraUtilitySlots>();

        public static HumanoidExtraUtilitySlots GetExtraUtilityData(this Humanoid humanoid) => data.GetOrCreateValue(humanoid);

        public static ItemDrop.ItemData GetExtraUtility(this Humanoid humanoid, int index) => index == 0 ? humanoid.GetExtraUtilityData().utility1 : humanoid.GetExtraUtilityData().utility2;

        public static ItemDrop.ItemData SetExtraUtility(this Humanoid humanoid, int index, ItemDrop.ItemData item) => index == 0 ? humanoid.GetExtraUtilityData().utility1 = item : humanoid.GetExtraUtilityData().utility2 = item;
    }
}