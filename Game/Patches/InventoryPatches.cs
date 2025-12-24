using ItemDataManager;

namespace AzuEPI.Game.Patches;

public class InventoryPatches
{
    // Not proud of this, but for now it's a quickfix. Find a permanent fix later. TODO
    private static bool IsBackpackItem(ItemDrop.ItemData item)
    {
        if (item?.m_shared == null) return false;

        string name = item.m_shared.m_name.ToLowerInvariant();
        string prefabName = item.m_dropPrefab?.name?.ToLowerInvariant() ?? "";

        if (name.Contains("backpack") || prefabName.Contains("backpack") || prefabName.StartsWith("bp_"))
            return true;

        // Check for ItemContainer via reflection (Backpacks mod specific)
        try
        {
            var itemData = item.Data();
            if (itemData != null)
            {
                var getMethod = itemData.GetType().GetMethod("Get");
                if (getMethod != null)
                {
                    var containerType = Type.GetType("Backpacks.ItemContainer, Backpacks");
                    if (containerType != null)
                    {
                        var genericMethod = getMethod.MakeGenericMethod(containerType);
                        var container = genericMethod.Invoke(itemData, null);
                        if (container != null)
                            return true;
                    }
                }
            }
        }
        catch { /* Not a backpack */ }

        return false;
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class FindEmptySlot_FilterHidden_AddQuick_Patch
    {
        private static bool Prefix(Inventory __instance, ref Vector2i __result, bool topFirst)
        {
            if (!__instance.ShouldProtectInventorySlots()) return true;
            __result = __instance.FindEmptyQuickAware(topFirst);
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
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return true;

            if (Player.m_localPlayer == null)
                return true;

            if (AddEquipmentRow.Value.isOff() || __instance != Player.m_localPlayer.GetInventory())
                return true;

            if (!__instance.IsEquipmentSlotFreeAndItemValid(item, out int which))
                return true;

            Vector2i pos = __instance.EpiIndexToGridPos(which);

            bool placed = __instance.AddItem(item, item.m_stack, pos.x, pos.y);
            if (!placed)
            {
                __result = false;
                return false;
            }

            if (AutoEquip.Value.isOn())
                Player.m_localPlayer.EquipItem(item, false);
            __instance.Changed();
            __result = true;
            return false;
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

            if (IsBackpackItem(item)) return true;

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
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, Vector2i pos)
        {
            if (item?.m_shared == null) return true;

            if (!__instance.ShouldProtectInventorySlots()) return true;

            if (__instance.IsHiddenCell(pos.x, pos.y))
            {
                __result = __instance.AddItem(item);
                return false;
            }

            if (API.TryGetSlotIndexAtGridPos(__instance, pos, out int slotIndex) && !API.SlotValidates(slotIndex, item))
            {
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
    internal static class Load_FixHiddenItems_Patch
    {
        private static readonly List<ItemDrop.ItemData> _stuckItems = new(16);

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Inventory __instance)
        {
            if (!__instance.ShouldProtectInventorySlots()) return;

            _stuckItems.Clear();
            foreach (ItemDrop.ItemData? it in __instance.GetAllItems())
            {
                if (__instance.IsHiddenCell(it.m_gridPos.x, it.m_gridPos.y))
                {
                    // Skip backpack items - let them stay where they are to avoid conflicts
                    if (IsBackpackItem(it))
                    {
                        AzuExtendedPlayerInventoryLogger.LogDebug($"Skipping backpack item {it.m_shared.m_name} in hidden cell ({it.m_gridPos.x}, {it.m_gridPos.y})");
                        continue;
                    }

                    _stuckItems.Add(it);
                }
            }

            if (_stuckItems.Count == 0) return;

            foreach (ItemDrop.ItemData? it in _stuckItems)
            {
                Vector2i originalPos = it.m_gridPos;

                if (!__instance.RemoveItem(it))
                {
                    AzuExtendedPlayerInventoryLogger.LogWarning($"Failed to remove stuck item {it.m_shared.m_name} from hidden cell ({originalPos.x}, {originalPos.y})");
                    continue;
                }

                bool added = __instance.AddItem(it);

                if (added && !__instance.m_inventory.Contains(it))
                {
                    added = false;
                    AzuExtendedPlayerInventoryLogger.LogWarning($"AddItem claimed success but {it.m_shared.m_name} not in inventory");
                }

                if (!added)
                {
                    it.m_gridPos = new Vector2i(0, 0);
                    added = __instance.AddItem(it, it.m_stack, 0, 0);

                    if (added && !__instance.m_inventory.Contains(it))
                    {
                        added = false;
                    }
                }

                if (!added)
                {
                    AzuExtendedPlayerInventoryLogger.LogError($"CRITICAL: Could not add {it.m_shared.m_name} back to inventory, forcing add at (0,0)");
                    it.m_gridPos = new Vector2i(0, 0);
                    __instance.m_inventory.Add(it);
                }
                else
                {
                    AzuExtendedPlayerInventoryLogger.LogDebug($"Moved {it.m_shared.m_name} from hidden cell ({originalPos.x}, {originalPos.y}) to ({it.m_gridPos.x}, {it.m_gridPos.y})");
                }
            }

            __instance.Changed();
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
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
    private static class MoveInventoryToGravePatch
    {
        private static void Postfix(Inventory __instance, Inventory original)
        {
            if (original.IsPlayerInventory())
            {
                original.m_height = API.GetFullHeight(original.GetWidth());
            }

            AzuExtendedPlayerInventoryLogger.LogDebugDebug("MoveInventoryToGrave");

            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"inv: {__instance.GetHeight()} orig: {original.GetHeight()}");
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
        private static void Postfix(Container __instance, ref bool granted)
        {
            if (granted) InventoryHealth.InventoryFix();
        }
    }
}