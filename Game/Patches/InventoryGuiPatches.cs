using AzuEPI.Core.InventoryHandlers;
using AzuEPI.Core.Slots;
using AzuEPI.Core.Text;
using AzuEPI.Game.Vanity;

namespace AzuEPI.Game.Patches;

public class InventoryGuiPatches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    [HarmonyPriority(Priority.Last)]
    static class ReparentPlayerGridInventoryGuiAwakePatch
    {
        static void Postfix(InventoryGui __instance)
        {
            GUICache._craftingBkgRT = __instance.m_crafting.Find("Bkg").GetComponent<RectTransform>();
            GUICache._selectedFrameRT = __instance.m_crafting.Find("selected_frame").GetComponent<RectTransform>();
            GUICache._repairSimpleRT = __instance.m_crafting.Find("RepairSimple").GetComponent<RectTransform>();
            GUICache._repairButtonRT = __instance.m_crafting.Find("RepairButton").GetComponent<RectTransform>();
            GUICache._playerBkgRT = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
            GUICache._playerGridRootRT = __instance.m_playerGrid ? __instance.m_playerGrid.m_gridRoot?.GetComponent<RectTransform>() : null;

            Layout.SelectedFrameOrigAnchMin = GUICache._selectedFrameRT.anchorMin;
            Layout.RepairSimpleOrigAnchoredPos = GUICache._repairSimpleRT.anchoredPosition;
            Layout.RepairButtonOrigAnchoredPos = GUICache._repairButtonRT.anchoredPosition;

            if (OldLayout.Value.isOff())
                Layout.ApplyRepairShift();

            CreateExtendedCraftingPanel(__instance, GUICache._selectedFrameRT);
            CreateRuntimePanel();

            CreateAzuEpiPreview(__instance, out var previewParentRT);
            CreatePlayerPreviewImage(previewParentRT);
            SetupPreviewPanel();

            BuildToggleButtonHlg(__instance);
            EnsureVanityPanelBuilt(__instance);
            VanityPanelController.SetVisible(false);
            BuildLoadoutToggles(__instance);
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
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            Player localPlayer = Player.m_localPlayer;
            if (!localPlayer) return true;
            if (localPlayer.IsTeleporting())
                return true;

            if (!__instance.m_dragGo || grid.m_inventory != localPlayer.GetInventory()) return true;
            bool wasDraggingItemEquipped = localPlayer.IsItemEquiped(__instance.m_dragItem);
            bool wasTargetItemEquipped = item != null && localPlayer.IsItemEquiped(item);
            Vector2i originalDragGridPos = __instance.m_dragItem.m_gridPos;

            if (API.TryGetSlotIndexAtGridPos(grid.m_inventory, pos, out int slotIndex))
            {
                if (!API.SlotValidates(slotIndex, __instance.m_dragItem))
                {
                    grid.DropItem(__instance.m_dragInventory, __instance.m_dragItem, __instance.m_dragAmount, originalDragGridPos);
                    return false;
                }

                if (!wasDraggingItemEquipped)
                    AutoEquipAfterDraggingItemWasNotEquipped(localPlayer, grid, pos, __instance);

                if (wasTargetItemEquipped)
                    UnequipAfterTargetItemWasEquipped(localPlayer, __instance, originalDragGridPos, item);
            }
            else if (wasDraggingItemEquipped)
            {
                if (grid.m_inventory.IsAtEquipmentSlot(__instance.m_dragItem, out _))
                {
                    localPlayer.UnequipItem(__instance.m_dragItem, false);
                }
            }

            return true;
        }

        private static void AutoEquipAfterDraggingItemWasNotEquipped(Player localPlayer, InventoryGrid grid, Vector2i pos, InventoryGui ig)
        {
            if (!AutoEquip.Value.isOn()) return;
            ItemDrop.ItemData itemAtNewPos = grid.GetInventory().GetItemAt(pos.x, pos.y);
            if (itemAtNewPos != null)
                localPlayer.EquipItem(itemAtNewPos, false);

            if (localPlayer.GetInventory().ContainsItem(ig.m_dragItem))
                localPlayer.EquipItem(ig.m_dragItem, false);
        }

        private static void UnequipAfterTargetItemWasEquipped(Player localPlayer, InventoryGui ig, Vector2i originalDragGridPos, ItemDrop.ItemData item)
        {
            ItemDrop.ItemData itemAtOriginalPos = ig.m_dragInventory.GetItemAt(originalDragGridPos.x, originalDragGridPos.y);
            if (itemAtOriginalPos != null)
                localPlayer.UnequipItem(itemAtOriginalPos, false);

            if (localPlayer.GetInventory().ContainsItem(item))
                localPlayer.UnequipItem(item, false);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnRightClickItem))]
    private static class InventoryGuiOnRightClickItemPatch
    {
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
        {
            if (item == null || !Player.m_localPlayer || grid.GetInventory() == null)
                return true;
            Player p = Player.m_localPlayer;
            if (grid.m_inventory.IsPlayerInventory())
                if (p.m_inventory.IsAtEquipmentSlot(item, out int which) && (item == p.m_helmetItem || item == p.m_chestItem || item == p.m_legItem || item == p.m_shoulderItem || item == p.m_utilityItem || item == p.m_trinketItem))
                    if (!p.m_inventory.CanAddItem(item))
                    {
                        AzuExtendedPlayerInventoryLogger.LogInfo("Inventory full, blocking item unequip");
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$inventory_full");
                        return false;
                    }

            return true;
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
            new Model.EquipmentSlot { Name = HelmetText.Value, IsQuickSlot = false, Get = player => player.m_helmetItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet },
            new Model.EquipmentSlot { Name = ChestText.Value, IsQuickSlot = false, Get = player => player.m_chestItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest },
            new Model.EquipmentSlot { Name = LegsText.Value, IsQuickSlot = false, Get = player => player.m_legItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs },
            new Model.EquipmentSlot { Name = BackText.Value, IsQuickSlot = false, Get = player => player.m_shoulderItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder },
            new Model.EquipmentSlot { Name = UtilityText.Value, IsQuickSlot = false, Get = player => player.m_utilityItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility },
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

        private static void Postfix(InventoryGui __instance, Player player, InventoryGrid ___m_playerGrid)
        {
            RectTransform bkgRect = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
            if (!__instance.m_player.transform.Find("PlayerScroll")) // If ValheimPlus didn't add a scrollbar
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

            Vector2 baseGridPos = new((___m_playerGrid.GetComponent<RectTransform>().rect.width - ___m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);

            for (int i = 0; i < slots.Count; ++i)
            {
                var currentElement = ___m_playerGrid.m_elements[baseIndex + i];
                GameObject currentChild = currentElement.m_go;
                if (!currentChild)
                    continue;
                currentChild.SetActive(true);

                Model.Slot? slot = slots[i];
                if (slot == null)
                    continue;

                currentChild.name = $"AzuEPI_Slot_{slots[i]?.Name}";

                // if .m_used assume it's occupied
                slots[i].Occupied = currentElement.m_used;

                SlotText.Set(slots[i]?.Name, currentChild.transform);
                RectTransform childRT = currentChild.GetComponent<RectTransform>();
                if (DisplayEquipmentRowSeparate.Value.isOn())
                {
                    if (InventoryGui.instance && childRT.parent != InventoryGui.instance.m_crafting)
                        childRT.SetParent(InventoryGui.instance.m_crafting, false);

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

            var equipmentBkgTransform = __instance.m_player.Find(AzuEquipmentBkgName);

            switch (DisplayEquipmentRowSeparate.Value)
            {
                case On when equipmentBkgTransform == null && OldLayout.Value.isOn():
                {
                    BuildEquipmentBkg(__instance, bkgRect);
                    break;
                }
                case On when OldLayout.Value.isOff():
                {
                    if (equipmentBkgTransform == null)
                    {
                        BuildEquipmentBkg(__instance, bkgRect);
                    }

                    float extraX = (extraTiles * Layout.tileSize) / totalWidth;

                    Vector2 maxAnchor = new(1f + extraX, 1f);
                    if (Chainloader.PluginInfos.TryGetValue(MinimalUiguid, out var pi) && pi != null)
                        maxAnchor.x += 0.03f;

                    //rectTransform.anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;
                    break;
                }

                case Off when equipmentBkgTransform:
                    equipmentBkgTransform.gameObject.SetActive(false);
                    break;
            }

            UpdateInvalidDropOverlays(__instance, ___m_playerGrid, Player.m_localPlayer);
        }

        private static void UpdateInvalidDropOverlays(InventoryGui ig, InventoryGrid playerGrid, Player player)
        {
            var dragGo = ig.m_dragGo;
            var dragItem = ig.m_dragItem;
            bool dragging = dragGo && dragItem != null;
            int baseIndex = Layout.GetBaseSlotIndex(player.GetInventory());

            for (int i = 0; i < UpdateInventory_Patch.slots.Count; ++i)
            {
                var slot = UpdateInventory_Patch.slots[i];
                if (slot == null) continue;

                var elemIdx = baseIndex + i;
                if (elemIdx < 0 || elemIdx >= playerGrid.m_elements.Count) continue;

                var elem = playerGrid.m_elements[elemIdx];
                var slotGo = elem.m_go;
                if (!slotGo) continue;

                _ = SlotOverlays.EnsureInvalidOverlay(slotGo);

                _ = SlotOverlays.EnsureVanityStateOverlay(slotGo);

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