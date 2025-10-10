using System.Reflection.Emit;
using AzuEPI;
using AzuEPI.EPI;
using AzuEPI.EPI.Utilities;
using AzuEPI.InventoryHandlers;
using AzuEPI.Loadout;
using AzuEPI.PlayerPreview;
using AzuEPI.Slots;
using AzuEPI.Vanity;
using AzuExtendedPlayerInventory;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryGuiPatches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    [HarmonyPriority(Priority.Last)]
    static class ReparentPlayerGridInventoryGuiAwakePatch
    {
        internal static RectTransform _epiPreviewRect;

        static void Postfix(InventoryGui __instance)
        {
            var selectedFrame = __instance.m_crafting.Find("selected_frame").GetComponent<RectTransform>();
            selectedFrame.anchorMin = new Vector2(-0.80f, 0);

            Transform bkg = Object.Instantiate(__instance.m_crafting.Find("Bkg"), __instance.m_crafting);
            var index = selectedFrame.GetSiblingIndex();
            bkg.SetSiblingIndex(index + 2);
            bkg.name = "AzuPlayerBkg";
            bkg.GetComponent<RectTransform>().anchorMin = new Vector2(-0.80f, 0);
            __instance.m_crafting.Find("RepairSimple").GetComponent<RectTransform>().anchoredPosition += new Vector2(-460f, 0f);
            __instance.m_crafting.Find("RepairButton").GetComponent<RectTransform>().anchoredPosition += new Vector2(-460f, 0f);
            __instance.m_crafting.SetSiblingIndex(1);

            if (AzuEPICharacterPanel.instance == null)
                new GameObject("AzuEPI_RuntimePanel").AddComponent<AzuEPICharacterPanel>();

            var panel = AzuEPICharacterPanel.instance;

            var previewParent = new GameObject("AzuEPI_PlayerPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(PlayerRotationController));
            var previewParentRT = (RectTransform)previewParent.transform;
            previewParentRT.SetParent(__instance.m_crafting, false);

            var img = previewParent.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.565f);
            previewParentRT.anchorMin = new Vector2(0f, 0.14f);
            previewParentRT.anchorMax = new Vector2(1f, 0.885f);
            previewParentRT.sizeDelta = new Vector2(-300f, 0f);
            previewParentRT.anchoredPosition = new Vector2(-507f, 0f);

            var go = new GameObject("PlayerPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(previewParentRT, false);

            Vector2 size = new Vector2(500f, 630f);
            rt.sizeDelta = size;

            rt.anchoredPosition = new Vector2(0, 0);

            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;

            panel.render = rt;
            panel.renderRawImage = raw;

            PlayerPreviewManager.Initialize();
            PlayerPreviewManager.Instance.CreatePreviewCamera();
            PlayerPreviewManager.Instance.CreatePreviewLights();
            PlayerPreviewManager.Instance.UpdateRenderTexture();

            _epiPreviewRect = rt;

            VanityPanelController.EnsureBuilt(__instance);
            VanityPanelController.SetVisible(false);

            PersonalLoadoutGui.BuildToggleButton(__instance);

            var charName = Object.Instantiate(__instance.m_info.transform.Find("TitlePanel"), previewParentRT);
            charName.name = "AzuEPI_CharacterName";
            charName.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault()!.text = Game.instance.GetPlayerProfile().GetName();
            foreach (Transform child in charName)
                if (child.name.Contains("BraidLine"))
                    Object.Destroy(child.gameObject);
            __instance.m_crafting.Find("Bkg").GetComponent<Image>().enabled = false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class InventoryGuiShowPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer == null)
                return;
            InventoryHealth.InventoryFix();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    private static class InventoryGuiOnSelectedItemPatch
    {
        private static void Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting())
                return;
            if (__instance.m_dragGo && localPlayer.IsItemEquiped(__instance.m_dragItem))
            {
                if (grid.m_inventory.IsAtEquipmentSlot(__instance.m_dragItem, out _))
                {
                    localPlayer.UnequipItem(__instance.m_dragItem, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class InventoryGuiUpdatePatch
    {
        private static void Postfix(InventoryGui __instance, InventoryGrid ___m_playerGrid, Animator ___m_animator)
        {
            if (!Player.m_localPlayer || !InventoryGui.instance.m_playerGrid)
                return;

            if (AddEquipmentRow.Value.isOn())
            {
                var player = Player.m_localPlayer;
                Inventory inventory = player.GetInventory();
                List<ItemDrop.ItemData> allItems = inventory.GetAllItems();

                int width = inventory.GetWidth();
                int height = inventory.GetHeight();
                int requiredRows = API.GetAddedRows(width);

                int num = width * (height - requiredRows);
                ItemDrop.ItemData?[] equippedItems = new ItemDrop.ItemData[UpdateInventory_Patch.slots.Count];
                for (int i = 0; i < UpdateInventory_Patch.slots.Count; ++i)
                {
                    Model.Slot? slot = UpdateInventory_Patch.slots[i];
                    if (slot is Model.EquipmentSlot equipmentSlot)
                    {
                        if (equipmentSlot.Get(player) is { } item)
                        {
                            item.m_gridPos = new Vector2i(num % width, num / width);
                            equippedItems[i] = item;
                        }

                        ++num;
                    }
                }

                for (int index = 0; index < allItems.Count; ++index)
                {
                    ItemDrop.ItemData t = allItems[index];

                    if (inventory.IsAtEquipmentSlot(t, out int which) &&
                        (which <= -1 || t != equippedItems[which]) &&
                        (which <= -1 || UpdateInventory_Patch.slots[which] is not Model.EquipmentSlot slot || !slot.Valid(t) || ExtendedPlayerInventory.equipItems[which] == t || (AutoEquip.Value.isOn() && !player.EquipItem(t, false))))
                    {
                        Vector2i vector2I = inventory.FindEmptySlot(true);
                        if (vector2I.x < 0 || vector2I.y < 0 || vector2I.y >= height - requiredRows)
                        {
                            // it will drop them simply because it cannot be added to the inventory and it's "outside" the normal inventory when it breaks.
                            if (t.m_durability > 0 && !inventory.CanAddItem(t))
                                player.DropItem(inventory, t, t.m_stack);
                        }
                        else
                        {
                            t.m_gridPos = vector2I;
                            ___m_playerGrid.UpdateInventory(inventory, player, null);
                        }
                    }
                }

                ExtendedPlayerInventory.equipItems = equippedItems;

                if (___m_animator.GetBool(ExtendedPlayerInventory.Visible))
                {
                    if (AzuEPICharacterPanel.playerPreviewComp && Player.m_localPlayer)
                        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
                }
            }

            if (!___m_animator.GetBool(ExtendedPlayerInventory.Visible))
                return;
            if (__instance.m_player.transform.Find("PlayerScroll") == null) // If ValheimPlus didn't add a scrollbar
            {
                RectTransform bkgRect = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();

                bkgRect.anchorMin = new Vector2(0.0f, (ExtraRows.Value
                                                       + (AddEquipmentRow.Value.isOff()
                                                          || DisplayEquipmentRowSeparate.Value.isOn()
                                                           ? 0
                                                           : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) * -0.25f);
            }

            if (AddEquipmentRow.Value.isOff())
                return;

            var equipmentBkgTransform = __instance.m_player.Find(ExtendedPlayerInventory.AzuBkgName);
            var dropallButton = __instance.m_player.Find(ExtendedPlayerInventory.DropAllButtonName);

            switch (DisplayEquipmentRowSeparate.Value)
            {
                /*case AzuExtendedPlayerInventoryPlugin.Toggle.On when equipmentBkgTransform == null:
                {
                    Transform transform = Object.Instantiate(bkgRect.transform, __instance.m_player);
                    transform.SetAsFirstSibling();
                    transform.name = ExtendedPlayerInventory.AzuBkgName;
                    RectTransform rectTransform = transform.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(1f, 0.0f);
                    Vector2 maxAnchor = new(1.13f + Math.Max(AzuExtendedPlayerInventoryPlugin.Hotkeys.Length, (UpdateInventory_Patch.slots.Count - 1) / 3) * UpdateInventory_Patch.tileSize / 570, 1f);
                    if (Chainloader.PluginInfos.TryGetValue(ExtendedPlayerInventory.MinimalUiguid, out var pluginInfo) && pluginInfo is not null) maxAnchor.x += 0.03f;

                    rectTransform.anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;

                    break;
                }*/
                case AzuExtendedPlayerInventoryPlugin.Toggle.On when equipmentBkgTransform == null:
                {
                    /*Transform transform = Object.Instantiate(bkgRect.transform, __instance.m_player);
                    transform.SetAsFirstSibling();
                    transform.name = ExtendedPlayerInventory.AzuBkgName;
                    RectTransform rectTransform = transform.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(1f, 0f);*/

                    float columns = 2f;
                    float gapTiles = 4f;
                    float padding = 0.6f;

                    float extraTiles = columns + gapTiles + padding;
                    float extraX = (extraTiles * Layout.tileSize) / 570f;

                    Vector2 maxAnchor = new(1f + extraX, 1f);
                    if (Chainloader.PluginInfos.TryGetValue(ExtendedPlayerInventory.MinimalUiguid, out var pi) && pi != null)
                        maxAnchor.x += 0.03f;

                    //rectTransform.anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;
                    break;
                }

                case AzuExtendedPlayerInventoryPlugin.Toggle.Off when equipmentBkgTransform:
                    Object.DestroyImmediate(equipmentBkgTransform.gameObject);
                    break;
            }

            if (MakeDropAllButton.Value.isOn())
            {
                RectTransform dropAllButtonTransform = null!;

                if (dropallButton == null)
                {
                    Transform dropAllButtonPrefab = __instance.m_takeAllButton.transform;
                    dropAllButtonTransform = Object.Instantiate(dropAllButtonPrefab, __instance.m_player).GetComponent<RectTransform>();
                    dropAllButtonTransform.name = ExtendedPlayerInventory.DropAllButtonName;
                    dropAllButtonTransform.GetComponentInChildren<TMP_Text>().text = "Drop All";
                    var buttonComp = dropAllButtonTransform.GetComponent<Button>();
                    buttonComp.onClick.RemoveAllListeners();
                    buttonComp.onClick.AddListener(() => Console.instance.TryRunCommand("azuepi.dropall"));
                }
                else
                {
                    dropAllButtonTransform = dropallButton.GetComponent<RectTransform>();
                }

                dropAllButtonTransform.SetAsFirstSibling();
                dropAllButtonTransform.anchorMin = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.anchorMax = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.pivot = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.anchoredPosition = DropAllButtonPosition.Value;
                dropAllButtonTransform.sizeDelta = new Vector2(100, 30);
            }
            else
            {
                if (dropallButton != null) Object.DestroyImmediate(dropallButton.gameObject);
            }

            UpdateInvalidDropOverlays(__instance, ___m_playerGrid, Player.m_localPlayer);
        }

        private static void UpdateInvalidDropOverlays(InventoryGui ig, InventoryGrid playerGrid, Player player)
        {
            var dragGo = ig.m_dragGo;
            var dragItem = ig.m_dragItem;
            bool dragging = dragGo && dragItem != null;
            var inv = player.GetInventory();
            int width = inv.GetWidth();
            int requiredRows = API.GetAddedRows(width);
            int baseIndex = width * (inv.GetHeight() - requiredRows);

            for (int i = 0; i < UpdateInventory_Patch.slots.Count; ++i)
            {
                var slot = UpdateInventory_Patch.slots[i];
                if (slot == null) continue;

                var elemIdx = baseIndex + i;
                if (elemIdx < 0 || elemIdx >= playerGrid.m_elements.Count) continue;

                var elem = playerGrid.m_elements[elemIdx];
                var slotGo = elem.m_go;
                if (!slotGo) continue;

                var _ = SlotOverlays.EnsureInvalidOverlay(slotGo);

                if (!dragging)
                {
                    SlotOverlays.SetInvalidVisible(slotGo, false);
                    continue;
                }

                // We skip plain inventory tiles to avoid fighting vanilla stacking logic.
                bool isLogicalSlot = (slot.IsQuickSlot || slot is Model.EquipmentSlot);
                if (!isLogicalSlot)
                {
                    SlotOverlays.SetInvalidVisible(slotGo, false);
                    continue;
                }

                bool allowed = SlotAcceptRules.CanItemGoToSlot(slot, dragItem);
                SlotOverlays.SetInvalidVisible(slotGo, !allowed);
            }
        }
        
        internal static class SlotOverlays
        {
            // Cache to avoid repeated GetComponent lookups
            private static readonly Dictionary<GameObject, GameObject> _invalidByGo = new();

            public static GameObject EnsureInvalidOverlay(GameObject slotGo)
            {
                if (_invalidByGo.TryGetValue(slotGo, out var overlay) && overlay)
                    return overlay;
                var rt = slotGo.GetComponent<RectTransform>();
                var ovBkg = new GameObject("EPI_InvalidOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var ovBkgRT = (RectTransform)ovBkg.transform;
                ovBkgRT.SetParent(rt, false);
                ovBkgRT.anchorMin = Vector2.zero;
                ovBkgRT.anchorMax = Vector2.one;
                ovBkgRT.offsetMin = Vector2.zero;
                ovBkgRT.offsetMax = Vector2.zero;
                ovBkgRT.pivot = new Vector2(0.5f, 0.5f);
                var imgBkg = ovBkg.GetComponent<Image>();
                imgBkg.raycastTarget = false;
                imgBkg.color = new Color(0f, 0f, 0f, 0.95f);

                var ov = new GameObject("EPI_InvalidOverlayCheck", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var ovRT = (RectTransform)ov.transform;
                ovRT.SetParent(ovBkgRT, false);
                ovRT.anchorMin = Vector2.zero;
                ovRT.anchorMax = Vector2.one;
                ovRT.offsetMin = Vector2.zero;
                ovRT.offsetMax = Vector2.zero;
                ovRT.pivot = new Vector2(0.5f, 0.5f);

                var img = ov.GetComponent<Image>();
                img.raycastTarget = false;
                img.sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == "mapicon_checked");

                ovBkg.SetActive(false);
                _invalidByGo[slotGo] = ovBkg;
                return ovBkg;
            }

            public static void SetInvalidVisible(GameObject slotGo, bool visible)
            {
                if (!_invalidByGo.TryGetValue(slotGo, out var ov) || !ov)
                    ov = EnsureInvalidOverlay(slotGo);

                if (ov.activeSelf != visible)
                    ov.SetActive(visible);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip), typeof(ItemDrop.ItemData), typeof(UITooltip))]
    public static class ItemTooltipControllerFollowSelectionPatch
    {
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(ItemDrop.ItemData item, UITooltip tooltip, out string __state)
        {
            __state = null;
            if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
            {
                tooltip.Set(item.m_shared.m_name, item.GetTooltip());
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
    internal static class UpdateInventory_Patch
    {

        internal static readonly List<Model.Slot?> slots = new()
        {
            new Model.EquipmentSlot { Name = HelmetText.Value, IsQuickSlot = false, Get = player => player.m_helmetItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet },
            new Model.EquipmentSlot { Name = ChestText.Value, IsQuickSlot = false, Get = player => player.m_chestItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest },
            new Model.EquipmentSlot { Name = LegsText.Value, IsQuickSlot = false, Get = player => player.m_legItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs },
            new Model.EquipmentSlot { Name = BackText.Value, IsQuickSlot = false, Get = player => player.m_shoulderItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder },
            new Model.EquipmentSlot { Name = UtilityText.Value, IsQuickSlot = false, Get = player => player.m_utilityItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility },
        };

        static UpdateInventory_Patch()
        {
            API.BeforeQuickSlotsAdded();
            for (int i = 0; i < Hotkeys.Length; ++i)
                slots.Add(new Model.Slot
                {
                    Name = HotkeyTexts[i].Value.IsNullOrWhiteSpace()
                        ? Hotkeys[i].Value.ToString()
                        : HotkeyTexts[i].Value,
                    IsQuickSlot = true,
                });
            API.QuickSlotsAdded();
        }

        internal static void ResizeSlots()
        {
            const int rowsPerCol = 8;
            float leftX = Layout.equipOriginX;
            float rightX = Layout.equipOriginX + Layout.tileSize * (1f + Layout.columnGapTiles);
            float yBase = Layout.equipOriginY;

            int equipCount = 0;
            while (equipCount < slots.Count && slots[equipCount] is Model.EquipmentSlot) equipCount++;

            int leftUsed = Math.Min(rowsPerCol, equipCount);
            int rightUsed = Math.Max(0, Math.Min(rowsPerCol, equipCount - rowsPerCol));

            for (int i = 0; i < equipCount; ++i)
            {
                bool leftCol = i < rowsPerCol;
                int row = leftCol ? i : (i - rowsPerCol);
                float x = leftCol ? leftX : rightX;
                float y = yBase - row * Layout.tileSize;
                slots[i]!.Position = new Vector2(x, y);
            }

            int quickCount = Hotkeys.Length;
            int quickStart = equipCount;

            float tallestRows = Mathf.Max(leftUsed, rightUsed);
            float bottomY = yBase - (tallestRows - 1) * Layout.tileSize;

            float spanWidth = (rightX - leftX) + Layout.tileSize;
            float rowWidth = quickCount * Layout.tileSize;
            float startX = leftX + (spanWidth - rowWidth) * 0.5f;

            for (int i = 0; i < quickCount; ++i)
                slots[quickStart + i]!.Position = new Vector2(startX + i * Layout.tileSize, bottomY);
        }

        private static void Postfix(InventoryGrid ___m_playerGrid)
        {
            if (AddEquipmentRow.Value.isOff())
                return;

            try
            {
                Player? player = Player.m_localPlayer;
                Inventory inventory = player.GetInventory();

                int requiredRows = API.GetAddedRows(inventory.GetWidth());

                int baseIndex = inventory.GetWidth() * (inventory.GetHeight() - requiredRows);

                Vector2 baseGridPos = new((___m_playerGrid.GetComponent<RectTransform>().rect.width - ___m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);

                for (int i = 0; i < slots.Count; ++i)
                {
                    var currentElement = ___m_playerGrid.m_elements[baseIndex + i];
                    GameObject currentChild = currentElement.m_go;
                    currentChild.SetActive(true);
                    currentChild.name = $"AzuEPI_Slot_{slots[i]?.Name}";
                    // if .m_used assume it's occupied
                    slots[i].Occupied = currentElement.m_used;
                    ExtendedPlayerInventory.SetSlotText(slots[i]?.Name, currentChild.transform);
                    if (DisplayEquipmentRowSeparate.Value.isOn())
                    {
                        if (InventoryGui.instance)
                            currentChild.GetComponent<RectTransform>().SetParent(InventoryGui.instance.m_crafting);
                        currentChild.GetComponent<RectTransform>().anchoredPosition = slots[i].Position;
                    }
                    else
                    {
                        currentChild.GetComponent<RectTransform>().anchoredPosition = baseGridPos + new Vector2((baseIndex + i) % inventory.GetWidth() * ___m_playerGrid.m_elementSpace, (baseIndex + i) / inventory.GetWidth() * -___m_playerGrid.m_elementSpace);
                    }
                }

                for (int i = baseIndex + slots.Count; i < ___m_playerGrid.m_elements.Count; ++i)
                {
                    ___m_playerGrid.m_elements[i].m_go.SetActive(false);
                    ___m_playerGrid.m_elements[i].m_used = true;
                }
            }
            catch (Exception ex)
            {
                AzuExtendedPlayerInventoryLogger.LogDebug($"Exception in EPI Update Inventory: {ex}");
            }
        }
    }
}