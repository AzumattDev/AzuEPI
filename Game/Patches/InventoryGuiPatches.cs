using AzuEPI.Core.Text;

namespace AzuEPI.Game.Patches;

public class InventoryGuiPatches
{
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

            Layout.SelectedFrameOrigAnchMin = GUICache._selectedFrameRT.anchorMin;
            Layout.RepairSimpleOrigAnchoredPos = GUICache._repairSimpleRT.anchoredPosition;
            Layout.RepairButtonOrigAnchoredPos = GUICache._repairButtonRT.anchoredPosition;
            Layout.EnchantmentMenuOrigAnchoredPos = GUICache._enchantmentMenuButtonRT is not null ? GUICache._enchantmentMenuButtonRT.anchoredPosition : new Vector2();
            Layout.EnchantmentMenuBkgOrigAnchoredPos = GUICache._enchantmentMenuBkgButtonRT is not null ? GUICache._enchantmentMenuBkgButtonRT.anchoredPosition : new Vector2();
            Layout.ContainerOrigPivot = __instance.m_container.pivot;

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

        internal static void RebuildQuickslots()
        {
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

            slots.RemoveAll(s => s is { IsQuickSlot: true });

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
            if (_lastInstance != __instance || _cachedBkgRect == null)
            {
                _lastInstance = __instance;
                _cachedBkgRect = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
                _cachedPlayerScrollCheck = __instance.m_player.transform.Find("PlayerScroll");
                _cachedPlayerGridRect = ___m_playerGrid?.GetComponent<RectTransform>();
            }

            RectTransform bkgRect = _cachedBkgRect;
            if (_cachedPlayerScrollCheck == null) // If ValheimPlus didn't add a scrollbar
            {
                bkgRect.anchorMin = new Vector2(0.0f, (ExtraRows.Value
                                                       + (AddEquipmentRow.Value.isOff()
                                                          || DisplayEquipmentRowSeparate.Value.isOn()
                                                           ? 0
                                                           : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) * -0.25f);
            }
            else
            {
                //TODO: Remember what the fuck I was doing here:
                //bkgRect.anchorMin = new Vector2(0.0f, (ExtraRows.Value + (AddEquipmentRow.Value.isOff() || DisplayEquipmentRowSeparate.Value.isOn() ? 0 : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) * -0.25f);
            }

            if (AddEquipmentRow.Value.isOff())
                return;

            if (!player) return;
            Inventory inventory = player.GetInventory();

            int baseIndex = Layout.GetBaseSlotIndex(inventory);

            for (int i = 0; i < ___m_playerGrid.m_elements.Count; ++i)
            {
                InventoryGrid.Element? elem = ___m_playerGrid.m_elements[i];
                if (elem?.m_go != null && !elem.m_used)
                {
                    SlotOverlays.SetVanityOverlayVisible(elem.m_go, new VanityState());
                }
            }

            Vector2 baseGridPos = new((_cachedPlayerGridRect.rect.width - ___m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);

            for (int i = 0; i < slots.Count; ++i)
            {
                InventoryGrid.Element? currentElement = ___m_playerGrid.m_elements[baseIndex + i];
                GameObject currentChild = currentElement.m_go;
                if (!currentChild)
                    continue;

                Model.Slot? slot = slots[i];
                if (slot == null)
                    continue;

                currentChild.SetActive(true);

                if (!currentChild.name.StartsWith($"{Prefix}Slot_"))
                    currentChild.name = $"{Prefix}Slot_{slots[i]?.Name}";

                // if .m_used assume it's occupied
                slots[i].Occupied = currentElement.m_used;

                SlotText.Set(slots[i]?.Name, currentChild.transform);

                RectTransform childRT = currentChild.GetComponent<RectTransform>();
                if (DisplayEquipmentRowSeparate.Value.isOn())
                {
                    if (InventoryGui.instance)
                    {
                        if (OldLayout.Value.isOff() && childRT.parent != InventoryGui.instance.m_crafting)
                            childRT.SetParent(InventoryGui.instance.m_crafting, false);
                        else if (OldLayout.Value.isOn() && childRT.parent != InventoryGui.instance.m_playerGrid.transform)
                            childRT.SetParent(InventoryGui.instance.m_playerGrid.transform.parent, false);
                    }

                    childRT.anchoredPosition = slots[i].Position;
                }
                else
                {
                    childRT.anchoredPosition = baseGridPos + new Vector2((baseIndex + i) % inventory.GetWidth() * ___m_playerGrid.m_elementSpace, (baseIndex + i) / inventory.GetWidth() * -___m_playerGrid.m_elementSpace);
                }
            }

            for (int i = baseIndex + slots.Count; i < ___m_playerGrid.m_elements.Count; ++i)
            {
                InventoryGrid.Element? tailElement = ___m_playerGrid.m_elements[i];
                tailElement.m_go.SetActive(false);
                tailElement.m_used = true;
            }

            if (!__instance.m_playerGrid)
                return;

            Layout.ProjectEquippedIntoGridTail(player, ___m_playerGrid);

            if (_cachedEquipmentBkg == null || _lastInstance != __instance)
                _cachedEquipmentBkg = __instance.m_player.Find(AzuEquipmentBkgName);

            Transform? equipmentBkgTransform = _cachedEquipmentBkg;

            switch (DisplayEquipmentRowSeparate.Value)
            {
                case On when !equipmentBkgTransform && OldLayout.Value.isOn():
                {
                    Layout.UpdateContainerPosition();
                    BuildEquipmentBkg(__instance, bkgRect);
                    break;
                }
                case On when OldLayout.Value.isOff():
                {
                    Layout.UpdateContainerPosition();
                    if (!equipmentBkgTransform)
                    {
                        BuildEquipmentBkg(__instance, bkgRect);
                    }

                    float extraX = (extraTiles * Layout.tileSize) / totalWidth;

                    Vector2 maxAnchor = new(1f + extraX, 1f);
                    if (Chainloader.PluginInfos.TryGetValue(MinimalUiguid, out PluginInfo? pi) && pi != null)
                        maxAnchor.x += 0.03f;

                    //rectTransform.anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;
                    break;
                }

                case Off when equipmentBkgTransform:
                    Layout.UpdateContainerPosition(true);
                    equipmentBkgTransform.gameObject.SetActive(false);
                    break;
            }

            if (StatsPanelController.IsVisible() && player != null && Time.frameCount % 10 == 0 && !StatsPanelController.IsViewingRemotePlayer())
                StatsPanelController.UpdateStats(player);

            UpdateInvalidDropOverlays(__instance, ___m_playerGrid, player);
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

                    InventoryGrid.Element? elem = playerGrid.m_elements[elemIdx];
                    GameObject? slotGo = elem.m_go;
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

                InventoryGrid.Element? elem = playerGrid.m_elements[elemIdx];
                GameObject? slotGo = elem.m_go;
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