namespace AzuEPI.Game.Patches;

public class InventoryPatches
{
    internal static bool IsInMigration = false;
    internal static bool IsInAutoEquip = false;
    internal static bool IsInTombstoneTakeAll = false;
    private static bool _isLoadingInventory = false;

    private static ItemDrop.ItemData? _itemBeingAdded = null;

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class FindEmptySlot_FilterHidden_AddQuick_Patch
    {
        private static bool Prefix(Inventory __instance, ref Vector2i __result, bool topFirst)
        {
            if (!__instance.ShouldProtectInventorySlots()) return true;

            if (_itemBeingAdded != null)
            {
                __result = __instance.FindEmptyQuickAware(_itemBeingAdded, topFirst);
            }
            else
            {
                __result = __instance.FindEmptyQuickAware(topFirst);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    private static class GetEmptySlots_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ref int __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
        {
            if (!__instance.ShouldProtectInventorySlots()) return true;
            __result = Capacity.FreeNormalCells(__instance) + Capacity.FreeQuickCells(__instance) + Capacity.FreeEquipmentCells(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    private static class HaveEmptySlot_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
        {
            if (!__instance.ShouldProtectInventorySlots()) return true;

            int normalRows = Layout.NormalRows(__instance);

            int normalUsed = 0;
            foreach (ItemDrop.ItemData item in ___m_inventory)
            {
                if (item.m_gridPos.y < normalRows)
                    normalUsed++;
            }

            bool normalHas = normalUsed < (normalRows * ___m_width);

            if (normalHas)
            {
                __result = true;
                return false;
            }

            __result = __instance.TryFindEmptyQuickCell(out _);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
    private static class InventoryAddItemPatch1
    {
        private static bool _inAutoEquipCall = false;

        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (__instance.ShouldProtectInventorySlots() && item?.m_shared != null)
            {
                _itemBeingAdded = item;
            }

            // Prevent recursion
            if (_inAutoEquipCall)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug("AddItem: Recursion detected, skipping auto-equip");
                return true;
            }

            // Don't interfere during migration from old storage systems
            if (IsInMigration)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug("AddItem: Migration in progress, skipping auto-equip");
                return true;
            }

            // Don't interfere during inventory load
            if (_isLoadingInventory)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug("AddItem: Inventory loading, skipping auto-equip");
                return true;
            }

            if (item?.m_shared == null)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug("AddItem: Item or shared data is null, skipping auto-equip");
                return true;
            }

            if (Player.m_localPlayer == null)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: No local player, skipping auto-equip");
                return true;
            }

            if (Player.m_localPlayer.m_isLoading)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Player is loading, skipping auto-equip");
                return true;
            }

            if (AddEquipmentRow.Value.isOff())
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: AddEquipmentRow is OFF, skipping auto-equip");
                return true;
            }

            if (__instance != Player.m_localPlayer.GetInventory())
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Not player inventory, skipping auto-equip");
                return true;
            }

            if (!__instance.IsEquipmentSlotFreeAndItemValid(item, out int which))
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: No valid free equipment slot found, using vanilla placement");
                return true;
            }

            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Found equipment slot {which}, attempting to place");

            Vector2i pos = __instance.EpiIndexToGridPos(which);

            try
            {
                _inAutoEquipCall = true;
                IsInAutoEquip = true;

                bool placed = __instance.AddItem(item, item.m_stack, pos.x, pos.y);
                if (!placed)
                {
                    AzuExtendedPlayerInventoryLogger.LogWarningDebug($"AddItem {item.m_shared.m_name}: Failed to place in equipment slot at ({pos.x}, {pos.y}), letting vanilla handle it");
                    return true;
                }

                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Successfully placed at ({pos.x}, {pos.y})");

                ItemDrop.ItemData? actualItem = __instance.GetItemAt(pos.x, pos.y);
                if (actualItem == null)
                {
                    AzuExtendedPlayerInventoryLogger.LogWarningDebug($"AddItem {item.m_shared.m_name}: Item not found at ({pos.x}, {pos.y}) after placement");
                    __instance.Changed();
                    __result = true;
                    return false;
                }

                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Retrieved item from inventory - IsEquipped={actualItem.m_equipped}, InInventory={__instance.ContainsItem(actualItem)}");

                if (AutoEquip.Value.isOn() && !IsInTombstoneTakeAll)
                {
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Calling EquipItem...");
                    bool equipResult = Player.m_localPlayer.EquipItem(actualItem, false);
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: EquipItem returned {equipResult}, item.m_equipped={actualItem.m_equipped}");
                }
                else if (IsInTombstoneTakeAll)
                {
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: Tombstone TakeAll in progress – placement done, deferring equip to AutoEquipAfterTombstoneGrab.");
                }
                else
                {
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"AddItem {item.m_shared.m_name}: AutoEquip is OFF, skipping equip");
                }

                __instance.Changed();
                __result = true;
                return false;
            }
            finally
            {
                _inAutoEquipCall = false;
                IsInAutoEquip = false;
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Finalizer()
        {
            _itemBeingAdded = null;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    private static class AddItem_XY_Guard_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, int amount, int x, int y)
        {
            if (item?.m_shared == null) return true;

            if (!__instance.ShouldProtectInventorySlots()) return true;

            if (_isLoadingInventory) return true;
            if (IsInMigration) return true;
            if (Player.m_localPlayer != null && Player.m_localPlayer.m_isLoading) return true;

            if (__instance.IsHiddenCell(x, y))
            {
                __result = __instance.AddItem(item, amount,
                               Mathf.Clamp(item.m_gridPos.x, 0, __instance.GetWidth() - 1),
                               Mathf.Clamp(item.m_gridPos.y, 0, __instance.GetHeight() - 1))
                           || __instance.AddItem(item);
                return false;
            }

            // Equipment cells must validate
            if (API.TryGetSlotIndexAtGridPos(__instance, new Vector2i(x, y), out int slotIndex) && !API.SlotValidates(slotIndex, item))
            {
                __result = false;
                return false;
            }

            // Quick cells accept anything; let vanilla continue
            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
    internal static class AddItem_Pos_Guard_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, ref Vector2i pos)
        {
            if (item?.m_shared == null) return true;

            if (!__instance.ShouldProtectInventorySlots()) return true;

            if (_isLoadingInventory) return true;
            if (IsInMigration) return true;

            if (__instance.IsHiddenCell(pos.x, pos.y))
            {
                __result = __instance.AddItem(item);
                return false;
            }

            if (API.TryGetSlotIndexAtGridPos(__instance, pos, out int slotIndex) && !API.SlotValidates(slotIndex, item))
            {
                Vector2i altPos = __instance.FindEmptyQuickAware(item, topFirst: true);
                if (altPos.x >= 0)
                {
                    // Found alternative position, update pos parameter and let vanilla continue
                    pos = altPos;
                    return true;
                }

                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
    static class CanAddItem_QuickAware_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result)
        {
            if (item?.m_shared == null) return true;
            if (!__instance.ShouldProtectInventorySlots()) return true;

            if (stack <= 0) stack = item.m_stack;

            int maxStack = Mathf.Max(1, item.m_shared.m_maxStackSize);

            int freeStackSpace = Capacity.FreeStackSpace(__instance, item);

            int normalFreeCells = Capacity.FreeNormalCells(__instance);

            int quickFreeCells = Capacity.FreeQuickCells(__instance);

            int equipmentFreeCells = Capacity.FreeValidEquipmentCells(__instance, item);

            long capacity = freeStackSpace + (long)(normalFreeCells + quickFreeCells + equipmentFreeCells) * maxStack;
            __result = capacity >= stack;
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    internal static class Load_TrackAndFixHiddenItems_Patch
    {
        private static readonly List<ItemDrop.ItemData> _stuckItems = new(16);

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Inventory __instance) => _isLoadingInventory = true;

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Inventory __instance)
        {
            try
            {
                if (!__instance.ShouldProtectInventorySlots()) return;

                int width = __instance.GetWidth();
                int height = __instance.GetHeight();

                _stuckItems.Clear();
                foreach (ItemDrop.ItemData? it in __instance.GetAllItems())
                {
                    bool isOutOfBounds = it.m_gridPos.x < 0 || it.m_gridPos.x >= width || it.m_gridPos.y < 0 || it.m_gridPos.y >= height;
                    bool isHidden = __instance.IsHiddenCell(it.m_gridPos.x, it.m_gridPos.y);

                    if (isOutOfBounds || isHidden)
                    {
                        _stuckItems.Add(it);
                    }
                }

                if (_stuckItems.Count == 0) return;

                AzuExtendedPlayerInventoryLogger.LogWarning($"Found {_stuckItems.Count} items in hidden/out-of-bounds cells during load. Relocating...");

                foreach (ItemDrop.ItemData? it in _stuckItems)
                {
                    Vector2i originalPos = it.m_gridPos;

                    Vector2i newPos = __instance.FindEmptyQuickAware(topFirst: true);

                    if (newPos.x < 0)
                    {
                        ItemDrop.ItemData? stackTarget = it.m_shared.m_maxStackSize > 1
                            ? __instance.m_inventory.FirstOrDefault(i =>
                                i != it &&
                                i.m_shared.m_name == it.m_shared.m_name &&
                                i.m_worldLevel == it.m_worldLevel &&
                                i.m_quality == it.m_quality &&
                                i.m_stack < i.m_shared.m_maxStackSize &&
                                !__instance.IsHiddenCell(i.m_gridPos.x, i.m_gridPos.y) &&
                                i.m_gridPos.x >= 0 && i.m_gridPos.x < width &&
                                i.m_gridPos.y >= 0 && i.m_gridPos.y < height)
                            : null;

                        if (stackTarget != null)
                        {
                            int canAdd = Mathf.Min(it.m_stack, stackTarget.m_shared.m_maxStackSize - stackTarget.m_stack);
                            stackTarget.m_stack += canAdd;
                            it.m_stack -= canAdd;

                            AzuExtendedPlayerInventoryLogger.LogDebug($"Merged {canAdd}x {it.m_shared.m_name} from hidden cell ({originalPos.x}, {originalPos.y}) into stack at ({stackTarget.m_gridPos.x}, {stackTarget.m_gridPos.y})");

                            if (it.m_stack <= 0)
                            {
                                __instance.m_inventory.Remove(it);
                                continue;
                            }

                            newPos = __instance.FindEmptyQuickAware(topFirst: true);
                        }

                        if (newPos.x < 0)
                        {
                            newPos = new Vector2i(0, 0);
                            AzuExtendedPlayerInventoryLogger.LogWarning($"No free slot for {it.m_shared.m_name}, placing at (0,0) - may overlap!");
                        }
                    }

                    it.m_gridPos = newPos;
                    AzuExtendedPlayerInventoryLogger.LogDebug($"Moved {it.m_shared.m_name} from hidden cell ({originalPos.x}, {originalPos.y}) to ({newPos.x}, {newPos.y})");
                }

                __instance.Changed();
            }
            finally
            {
                _isLoadingInventory = false;
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    internal static class MoveItemToThis_XY_Guard_Patch
    {
        private static bool Prefix(Inventory __instance, ref bool __result, Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)
        {
            if (item?.m_shared == null) return true;
            if (!__instance.ShouldProtectInventorySlots()) return true;

            if (__instance.IsHiddenCell(x, y))
            {
                bool ok = __instance.AddItem(item, amount, Mathf.Clamp(item.m_gridPos.x, 0, __instance.GetWidth() - 1), Mathf.Clamp(item.m_gridPos.y, 0, __instance.GetHeight() - 1));

                if (!ok)
                    ok = __instance.AddItem(item);

                if (ok)
                {
                    if (item.m_stack == 0)
                        fromInventory.RemoveItem(item);
                    else
                        fromInventory.Changed();
                }

                __result = ok;
                return false;
            }

            // Equipment target must validate
            if (API.TryGetSlotIndexAtGridPos(__instance, new Vector2i(x, y), out int slotIndex) && !API.SlotValidates(slotIndex, item))
            {
                __result = false;
                return false;
            }

            return true;
        }

        private static void Postfix(Inventory __instance, bool __result, ItemDrop.ItemData item, int x, int y)
        {
            if (!__result) return;
            if (!AutoEquip.Value.isOn()) return;
            if (!AddEquipmentRow.Value.isOn()) return;
            if (IsInAutoEquip || IsInMigration || IsInTombstoneTakeAll) return;
            if (Player.m_localPlayer == null) return;
            if (__instance != Player.m_localPlayer.GetInventory()) return;
            if (!API.TryGetSlotIndexAtGridPos(__instance, new Vector2i(x, y), out int slotIdx)) return;
            if (slots[slotIdx] is not Model.EquipmentSlot) return;
            ItemDrop.ItemData? movedItem = __instance.GetItemAt(x, y);
            if (movedItem == null || movedItem.m_equipped) return;
            Player.m_localPlayer.EquipItem(movedItem, false);
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
    private static class MoveInventoryToGravePatch
    {
        private static void Prefix(Inventory __instance, Inventory original)
        {
            if (original.IsPlayerInventory())
            {
                original.m_height = API.GetFullHeight(original.GetWidth());
                // Also expand the tombstone inventory so items in EPI rows (y >= 4) pass the vanilla bounds check
                __instance.m_height = API.GetFullHeight(__instance.GetWidth());
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"MoveInventoryToGrave: Set original inventory height to {original.m_height}, grave height to {__instance.m_height}");
            }
        }

        private static void Postfix(Inventory __instance, Inventory original)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"MoveInventoryToGrave: grave height={__instance.GetHeight()}, original height={original.GetHeight()}");
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
    internal static class MoveAllToPatch
    {
        private static void Postfix(Inventory __instance, Inventory fromInventory)
        {
            if (__instance.IsPlayerInventory()) InventoryHealth.InventoryFix();
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_TakeAllRespons))]
    internal static class ContainerRPCRequestTakeAllPatch
    {
        private static void Prefix(Container __instance, bool granted)
        {
            if (granted && __instance.GetComponent<TombStone>() != null)
            {
                AzuExtendedPlayerInventoryLogger.LogDebug("TombStone TakeAll starting – suppressing per-item EquipItem calls during MoveAll.");
                IsInTombstoneTakeAll = true;
            }
        }

        private static void Postfix(Container __instance, bool granted)
        {
            if (granted) InventoryHealth.InventoryFix();
        }

        private static void Finalizer()
        {
            IsInTombstoneTakeAll = false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    private static class DoCrafting_TrackUpgradeEquipped_Patch
    {
        internal static bool UpgradeItemWasEquipped = false;

        private static void Prefix(InventoryGui __instance)
        {
            UpgradeItemWasEquipped = __instance.m_craftUpgradeItem?.m_equipped ?? false;
        }

        private static void Finalizer()
        {
            UpgradeItemWasEquipped = false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool))]
    internal static class AddItem_String_TrackItemForUpgrading_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Inventory __instance, string name, int quality, int variant)
        {
            if (!__instance.IsPlayerInventory()) return;

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
            if (itemPrefab == null) return;

            if (!itemPrefab.TryGetComponent(out ItemDrop component)) return;

            _itemBeingAdded = component.m_itemData.Clone();
            _itemBeingAdded.m_quality = quality;
            _itemBeingAdded.m_variant = variant;
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Inventory __instance, ItemDrop.ItemData? __result)
        {
            if (__result == null || !DoCrafting_TrackUpgradeEquipped_Patch.UpgradeItemWasEquipped) return;
            if (!AutoEquip.Value.isOn()) return;
            if (!AddEquipmentRow.Value.isOn()) return;
            if (_isLoadingInventory || IsInMigration) return;
            if (Player.m_localPlayer == null) return;
            if (__instance != Player.m_localPlayer.GetInventory()) return;
            if (!API.TryGetSlotIndexAtGridPos(__instance, __result.m_gridPos, out int slotIdx)) return;
            if (slots[slotIdx] is not Model.EquipmentSlot) return;
            __result.m_equipped = false;
            Player.m_localPlayer.EquipItem(__result, false);
        }

        [HarmonyPriority(Priority.Last)]
        private static void Finalizer()
        {
            _itemBeingAdded = null;
        }
    }
}