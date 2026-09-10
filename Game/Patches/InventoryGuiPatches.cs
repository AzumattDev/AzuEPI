using AzuEPI.Core.Text;

namespace AzuEPI.Game.Patches;

public class InventoryGuiPatches
{
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateInventory))]
    private static class PlayerInventoryElementLifecycle
    {
        private static void Prefix(InventoryGrid __instance, Inventory inventory, Player player, out List<InventoryElement>? __state)
        {
            __state = null;
            if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid) return;
            if (player && !player.m_isLoading && AddEquipmentRow.Value.isOn())
                Layout.ProjectEquippedIntoGridTail(player, __instance);
            if (__instance.m_width == inventory.GetWidth() && __instance.m_height == inventory.GetHeight()) return;
            __state = [];
            foreach (InventoryElement element in __instance.m_elements)
            {
                if (!element || !element.gameObject.activeSelf) continue;
                __state.Add(element);
                element.gameObject.SetActive(false);
            }
            UpdateInventory_Patch.InvalidateElements();
        }

        private static void Postfix(InventoryGrid __instance, List<InventoryElement>? __state)
        {
            if (__state == null) return;
            foreach (InventoryElement element in __state)
                if (element && __instance.m_elements.Contains(element)) element.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.ResetView))]
    private static class PlayerInventoryResetView
    {
        private static void Postfix(InventoryGrid __instance)
        {
            if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid) return;
            if (InventoryGui.instance.m_player.Find("PlayerScroll")) return;
            UpdateInventory_Patch.AlignPlayerGrid(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    static class ReparentPlayerGridInventoryGuiAwakePatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(InventoryGui __instance)
        {
            GUICache._craftingBkgRT = __instance.m_crafting.Find("Bkg").GetComponent<RectTransform>();
            GUICache._selectedFrameRT = __instance.m_crafting.Find("selected_frame").GetComponent<RectTransform>();
            GUICache._repairSimpleRT = __instance.m_crafting.Find("RepairSimple").GetComponent<RectTransform>();
            GUICache._repairButtonRT = __instance.m_crafting.Find("RepairButton").GetComponent<RectTransform>();
            GUICache._enchantmentMenuButtonRT = VESCompat.IsVesInstalled ? __instance.m_crafting.Find("enchantment_menu").GetComponent<RectTransform>() : null;
            GUICache._enchantmentMenuBkgButtonRT = VESCompat.IsVesInstalled ? __instance.m_crafting.Find("RepairSimple(Clone)").GetComponent<RectTransform>() : null;
            GUICache._playerBkgRT = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
            GUICache._playerGridRootRT = __instance.m_playerGrid ? __instance.m_playerGrid.m_gridRoot?.GetComponent<RectTransform>() : null;
            GUICache._playerGridRootImage = __instance.m_playerGrid ? __instance.m_playerGrid.m_gridRoot?.GetComponent<Image>() : null;

            Layout.SelectedFrameOrigAnchMin = GUICache._selectedFrameRT.anchorMin;
            Layout.RepairSimpleOrigAnchoredPos = GUICache._repairSimpleRT.anchoredPosition;
            Layout.RepairButtonOrigAnchoredPos = GUICache._repairButtonRT.anchoredPosition;
            Layout.EnchantmentMenuOrigAnchoredPos = GUICache._enchantmentMenuButtonRT is not null ? GUICache._enchantmentMenuButtonRT.anchoredPosition : new Vector2();
            Layout.EnchantmentMenuBkgOrigAnchoredPos = GUICache._enchantmentMenuBkgButtonRT is not null ? GUICache._enchantmentMenuBkgButtonRT.anchoredPosition : new Vector2();
            Layout.ContainerOrigAnchoredPos = __instance.m_container.anchoredPosition;

            if (OldLayout.Value.isOff())
                Layout.ApplyRepairShift();

            CreateExtendedCraftingPanel(__instance, GUICache._selectedFrameRT);
            CreateRuntimePanel();

            CreateAzuEpiPreview(__instance, out RectTransform previewParentRT);
            CreatePlayerPreviewImage(previewParentRT);
            SetupPreviewPanel();

            BuildToggleButtonGlg(__instance);
            BuildCraftingToggleButton(__instance);
            Layout.FixPlayerPreview();
            EnsureVanityPanelBuilt(__instance);
            VanityPanelController.SetVisible(false);
            BuildLoadoutToggles(__instance);
            EnsureStatsPanelBuilt(__instance);
            BuildStatsToggleButton(__instance);
            StatsPanelController.SetVisible(false);
            CreateCharacterName(__instance, previewParentRT);

            if (GUICache._craftingBkgRT)
                GUICache._craftingBkgRT.GetComponent<Image>().enabled = OldLayout.Value.isOn();

            BuildDropAllButton(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class InventoryGuiShowPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
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
            if (__instance.m_dragItem == null) return;
            if (__instance.m_dragGo && localPlayer.IsItemEquiped(__instance.m_dragItem))
            {
                if (__instance.m_dragInventory != null && __instance.m_dragInventory.IsAtEquipmentSlot(__instance.m_dragItem, out _))
                {
                    localPlayer.UnequipItem(__instance.m_dragItem, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    public static class InventoryGui_OnSelectedItem_EpiValidation
    {
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            Player player = Player.m_localPlayer;
            if (!player || player.IsTeleporting())
                return true;
            if (item == null) return true;
            if (!__instance.m_dragGo || __instance.m_dragItem == null || __instance.m_dragInventory == null)
                return true;

            return EpiDropRouter.ValidatePlannedDrop(grid, __instance.m_dragInventory, __instance.m_dragItem, __instance.m_dragAmount, pos);
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    public static class InventoryGrid_DropItem_EpiValidation
    {
        private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos, ref bool __result)
        {
            Player player = Player.m_localPlayer;
            if (!player || player.IsTeleporting())
                return true;

            if (EpiDropRouter.ValidatePlannedDrop(__instance, fromInventory, item, amount, pos)) return true;
            __result = false;
            return false;
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
        private static RectTransform _cachedBkgRect;
        private static Transform _cachedPlayerScrollCheck;
        private static RectTransform _cachedPlayerGridRect;
        private static Transform _cachedEquipmentBkg;
        private static InventoryGui _lastInstance;
        private static Vector2 _lastBkgAnchorMin = new(float.NaN, float.NaN);
        private static Vector2[] _cachedSlotPositions = [];
        private static string[] _cachedSlotNames = [];
        private static InventoryElement[] _cachedSlotElements = [];
        private static int _visibleRows = -1;

        internal static void AlignPlayerGrid(InventoryGrid grid)
        {
            RectTransform root = grid.m_gridRoot;
            root.anchorMin = new Vector2(root.anchorMin.x, 1f);
            root.anchorMax = new Vector2(root.anchorMax.x, 1f);
            root.pivot = new Vector2(root.pivot.x, 1f);
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, 0f);
        }

        internal static void InvalidateElements()
        {
            _cachedSlotPositions = [];
            _cachedSlotNames = [];
            _cachedSlotElements = [];
            _overlaysInitialized = false;
            _visibleRows = -1;
        }

        internal static void RebuildQuickslots()
        {
            _cachedSlotPositions = [];
            _cachedSlotNames = [];
            if (Hotkeys == null || HotkeyTexts == null)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning("RebuildQuickslots called with null Hotkeys or HotkeyTexts");
                return;
            }

            if (Hotkeys.Length != HotkeyTexts.Length)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning($"RebuildQuickslots: Hotkeys.Length ({Hotkeys.Length}) != HotkeyTexts.Length ({HotkeyTexts.Length})");
                return;
            }

            slots.RemoveAll(s => s is { IsQuickSlot: true } && s is not Model.EquipmentSlot);

            API.BeforeQuickSlotsAdded();
            int count = QuickSlotsAmount.Value;
            for (int i = 0; i < count && i < Hotkeys.Length && i < HotkeyTexts.Length; ++i)
            {
                if (Hotkeys[i] == null || HotkeyTexts[i] == null)
                {
                    AzuExtendedPlayerInventoryLogger.LogWarning($"Skipping quick slot {i} due to null config entry");
                    continue;
                }

                slots.Add(new Model.Slot
                {
                    Name = HotkeyTexts[i].Value.IsNullOrWhiteSpace() ? Hotkeys[i].Value.ToString() : HotkeyTexts[i].Value,
                    IsQuickSlot = true,
                });
            }

            API.QuickSlotsAdded();

            Layout.FixPlayerPreview();
        }

        private static void Postfix(InventoryGui __instance, Player player, InventoryGrid ___m_playerGrid)
        {
            if (!player || !___m_playerGrid) return;
            if (_lastInstance != __instance || _cachedBkgRect == null)
            {
                _lastInstance = __instance;
                _cachedBkgRect = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
                _cachedPlayerScrollCheck = __instance.m_player.transform.Find("PlayerScroll");
                _cachedPlayerGridRect = ___m_playerGrid?.GetComponent<RectTransform>();
                _lastBkgAnchorMin = new(float.NaN, float.NaN);
                _cachedSlotPositions = [];
                _cachedSlotNames = [];
                _cachedSlotElements = [];
                _cachedEquipmentBkg = null!;
                _overlaysInitialized = false;
                _visibleRows = -1;
            }

            RectTransform bkgRect = _cachedBkgRect;
            if (_cachedPlayerScrollCheck == null) // If ValheimPlus didn't add a scrollbar
            {
                int visibleRows = Layout.VisiblePlayerRows(player.GetInventory());
                if (_visibleRows != visibleRows)
                {
                    __instance.SetInventorySize(visibleRows);
                    Layout.UpdateContainerPosition();
                    _visibleRows = visibleRows;
                    AlignPlayerGrid(___m_playerGrid);
                }
                Vector2 newMin = Vector2.zero;
                if (newMin != _lastBkgAnchorMin)
                {
                    bkgRect.anchorMin = newMin;
                    _lastBkgAnchorMin = newMin;
                }
            }

            Inventory inventory = player.GetInventory();

            int baseIndex = Layout.GetBaseSlotIndex(inventory);

            for (int i = 0; i < ___m_playerGrid.m_elements.Count; ++i)
            {
                InventoryElement? elem = ___m_playerGrid.m_elements[i];
                if (elem?.gameObject != null && !elem.m_used)
                {
                    SlotOverlays.SetVanityOverlayVisible(elem.gameObject, new VanityState());
                }
            }

            Vector2 baseGridPos = new((_cachedPlayerGridRect.rect.width - ___m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);

            if (AddEquipmentRow.Value.isOff())
            {
                return;
            }
            int slotCount = slots.Count;
            if (_cachedSlotPositions.Length != slotCount)
            {
                _cachedSlotPositions = new Vector2[slotCount];
                for (int j = 0; j < slotCount; j++) _cachedSlotPositions[j] = new Vector2(float.NaN, float.NaN);
            }
            if (_cachedSlotNames.Length != slotCount)
                _cachedSlotNames = new string[slotCount];
            if (_cachedSlotElements.Length != slotCount)
                _cachedSlotElements = new InventoryElement[slotCount];

            for (int i = 0; i < slotCount && baseIndex + i < ___m_playerGrid.m_elements.Count; ++i)
            {
                InventoryElement? currentElement = ___m_playerGrid.m_elements[baseIndex + i];
                GameObject currentChild = currentElement.gameObject;
                if (!currentChild)
                    continue;

                Model.Slot? slot = slots[i];
                if (slot == null)
                    continue;

                if (!currentChild.activeSelf) currentChild.SetActive(true);

                if (!currentChild.name.StartsWith($"{Prefix}Slot_"))
                    currentChild.name = $"{Prefix}Slot_{slots[i]?.Name}";

                // if .m_used assume it's occupied
                slots[i].Occupied = currentElement.m_used;

                BindSlotElement(i, currentElement, slot.Name ?? "");

                RectTransform childRT = currentChild.GetComponent<RectTransform>();
                if (DisplayEquipmentRowSeparate.Value.isOn())
                {
                    bool reparented = false;
                    if (InventoryGui.instance)
                    {
                        Transform targetParent = OldLayout.Value.isOff()
                            ? InventoryGui.instance.m_crafting
                            : InventoryGui.instance.m_player;
                        reparented = childRT.parent != targetParent;
                        if (reparented) childRT.SetParent(targetParent, false);
                    }

                    Vector2 pos = slots[i].Position;
                    if (reparented || _cachedSlotPositions[i] != pos)
                    {
                        childRT.anchoredPosition = pos;
                        _cachedSlotPositions[i] = pos;
                    }

                    if (reparented) childRT.SetAsLastSibling();
                }
                else
                {
                    bool reparented = childRT.parent != ___m_playerGrid.m_gridRoot;
                    if (reparented) childRT.SetParent(___m_playerGrid.m_gridRoot, false);
                    Vector2 pos = baseGridPos + new Vector2((baseIndex + i) % inventory.GetWidth() * ___m_playerGrid.m_elementSpace, (baseIndex + i) / inventory.GetWidth() * -___m_playerGrid.m_elementSpace);
                    if (reparented || _cachedSlotPositions[i] != pos)
                    {
                        childRT.anchoredPosition = pos;
                        _cachedSlotPositions[i] = pos;
                    }
                }
            }

            for (int i = baseIndex + slotCount; i < ___m_playerGrid.m_elements.Count; ++i)
            {
                InventoryElement? tailElement = ___m_playerGrid.m_elements[i];
                if (tailElement.gameObject.activeSelf) tailElement.gameObject.SetActive(false);
                tailElement.m_used = true;
            }

            if (!__instance.m_playerGrid)
                return;

            if (_cachedEquipmentBkg == null || _lastInstance != __instance)
                _cachedEquipmentBkg = __instance.m_player.Find(AzuEquipmentBkgName);

            Transform? equipmentBkgTransform = _cachedEquipmentBkg;

            switch (DisplayEquipmentRowSeparate.Value)
            {
                case On when !equipmentBkgTransform && OldLayout.Value.isOn():
                {
                    BuildEquipmentBkg(__instance, bkgRect);
                    break;
                }
                case On when OldLayout.Value.isOff():
                {
                    if (!equipmentBkgTransform)
                    {
                        BuildEquipmentBkg(__instance, bkgRect);
                    }

                    break;
                }

                case Off when equipmentBkgTransform:
                    equipmentBkgTransform.gameObject.SetActive(false);
                    break;
            }

            if (StatsPanelController.IsVisible() && player != null && Time.frameCount % 10 == 0 && !StatsPanelController.IsViewingRemotePlayer())
                StatsPanelController.UpdateStats(player);

            UpdateInvalidDropOverlays(__instance, ___m_playerGrid, player);
        }

        private static void BindSlotElement(int index, InventoryElement element, string name)
        {
            if (_cachedSlotElements[index] != element)
            {
                _cachedSlotElements[index] = element;
                _cachedSlotNames[index] = null!;
                _cachedSlotPositions[index] = new Vector2(float.NaN, float.NaN);
                _overlaysInitialized = false;
            }
            if (_cachedSlotNames[index] == name) return;
            SlotText.Set(name, element.transform);
            _cachedSlotNames[index] = name;
        }

        private static bool _overlaysInitialized;

        private static void UpdateInvalidDropOverlays(InventoryGui ig, InventoryGrid playerGrid, Player player)
        {
            GameObject? dragGo = ig.m_dragGo;
            ItemDrop.ItemData? dragItem = ig.m_dragItem;
            bool dragging = dragGo && dragItem != null;

            if (!_overlaysInitialized)
            {
                int baseIndex = Layout.GetBaseSlotIndex(player.GetInventory());
                for (int i = 0; i < slots.Count; ++i)
                {
                    Model.Slot? slot = slots[i];
                    if (slot == null) continue;

                    int elemIdx = baseIndex + i;
                    if (elemIdx < 0 || elemIdx >= playerGrid.m_elements.Count) continue;

                    InventoryElement? elem = playerGrid.m_elements[elemIdx];
                    GameObject? slotGo = elem.gameObject;
                    if (!slotGo) continue;

                    _ = SlotOverlays.EnsureInvalidOverlay(slotGo);
                    _ = SlotOverlays.EnsureVanityStateOverlay(slotGo);
                }

                _overlaysInitialized = true;
            }

            int baseIndex1 = Layout.GetBaseSlotIndex(player.GetInventory());

            for (int i = 0; i < slots.Count; ++i)
            {
                Model.Slot? slot = slots[i];
                if (slot == null) continue;

                int elemIdx = baseIndex1 + i;
                if (elemIdx < 0 || elemIdx >= playerGrid.m_elements.Count) continue;

                InventoryElement? elem = playerGrid.m_elements[elemIdx];
                GameObject? slotGo = elem.gameObject;
                if (!slotGo) continue;

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
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
static class SetupEquipment_GridSync
{
    static void Postfix(Humanoid __instance)
    {
        if (__instance is not Player p || p != Player.m_localPlayer) return;
        if (InventoryGui.IsVisible()) return;
        if (AddEquipmentRow.Value.isOff()) return;
        if (InventoryPatches.IsInAutoEquip) return; // Don't reposition mid-MoveAll

        Layout.ProjectEquippedIntoGridTail(p, null!);
    }
}
