using AzuEPI.EPI;
using AzuEPI.EPI.Patches;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryPatches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class FindEmptySlot_FilterHidden_AddQuick_Patch
    {
        private static bool Prefix(Inventory __instance, ref Vector2i __result, bool topFirst)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            int width = __instance.GetWidth();
            int height = __instance.GetHeight();
            int addedRows = API.GetAddedRows(width);
            int adjustedHeight = height - addedRows;

            if (topFirst)
            {
                for (int y = 0; y < adjustedHeight; ++y)
                for (int x = 0; x < width; ++x)
                    if (__instance.GetItemAt(x, y) == null)
                    {
                        __result = new Vector2i(x, y);
                        return false;
                    }
            }
            else
            {
                for (int y = adjustedHeight - 1; y >= 0; --y)
                for (int x = 0; x < width; ++x)
                    if (__instance.GetItemAt(x, y) == null)
                    {
                        __result = new Vector2i(x, y);
                        return false;
                    }
            }

            if (ExtendedPlayerInventory.TryFindEmptyQuickCell(__instance, out var q))
            {
                __result = q;
                return false;
            }

            __result = new Vector2i(-1, -1);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    private static class GetEmptySlots_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ref int __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            int addedRows = API.GetAddedRows(___m_width);
            int adjustedHeight = ___m_height - addedRows;

            int normalUsed = ___m_inventory.Count(i => i.m_gridPos.y < adjustedHeight);
            int normalFree = (adjustedHeight * ___m_width) - normalUsed;

            int quickFree = 0;
            foreach (var p in ExtendedPlayerInventory.EnumerateQuickCells(__instance))
                if (__instance.GetItemAt(p.x, p.y) == null)
                    quickFree++;

            __result = normalFree + quickFree;
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    private static class HaveEmptySlot_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            int addedRows = API.GetAddedRows(___m_width);
            int adjustedHeight = ___m_height - addedRows;

            int normalUsed = ___m_inventory.Count(i => i.m_gridPos.y < adjustedHeight);
            bool normalHas = normalUsed < (adjustedHeight * ___m_width);

            if (normalHas)
            {
                __result = true;
                return false;
            }

            __result = ExtendedPlayerInventory.TryFindEmptyQuickCell(__instance, out _);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
    private static class InventoryAddItemPatch1
    {
        private static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, ItemDrop.ItemData item)
        {
            if (Player.m_localPlayer == null) return true;
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.isOff() || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                return true;
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("AddItem");
            if (!ExtendedPlayerInventory.IsEquipmentSlotFree(__instance, item, out int which))
                return true;

            int addedRows = API.GetAddedRows(__instance.GetWidth());

            int adjustedHeight = __instance.GetHeight() - addedRows;

            __instance.AddItem(item, item.m_stack, which % __instance.GetWidth(), adjustedHeight + which / __instance.GetWidth());
            Player.m_localPlayer.EquipItem(item, false);
            __instance.Changed();
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    private static class AddItem_XY_Guard_Patch
    {
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, int amount, int x, int y)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            if (ExtendedPlayerInventory.IsHiddenCell(__instance, x, y))
            {
                __result = __instance.AddItem(item, amount, /*x*/ Mathf.Clamp(item.m_gridPos.x, 0, __instance.GetWidth() - 1), /*y*/ Mathf.Clamp(item.m_gridPos.y, 0, __instance.GetHeight() - 1))
                           || __instance.AddItem(item);
                return false;
            }

            // Equipment cells must validate
            if (ExtendedPlayerInventory.IsEquipmentCell(__instance, x, y, out int which))
            {
                var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[which] as InventoryGuiPatches.EquipmentSlot;
                if (slot == null || slot.Valid == null || !slot.Valid(item))
                {
                    __result = false;
                    return false;
                }
            }

            // Quick cells accept anything; let vanilla continue
            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
    internal static class AddItem_Pos_Guard_Patch
    {
        private static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, Vector2i pos)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            if (ExtendedPlayerInventory.IsHiddenCell(__instance, pos.x, pos.y))
            {
                __result = __instance.AddItem(item);
                return false;
            }

            if (ExtendedPlayerInventory.IsEquipmentCell(__instance, pos.x, pos.y, out int which))
            {
                var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[which] as InventoryGuiPatches.EquipmentSlot;
                if (slot == null || slot.Valid == null || !slot.Valid(item))
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
    static class CanAddItem_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            if (stack <= 0) stack = item.m_stack;

            int width = __instance.GetWidth();
            int height = __instance.GetHeight();
            int addedRows = API.GetAddedRows(width);
            int normalRows = height - addedRows;

            int maxStack = Mathf.Max(1, item.m_shared.m_maxStackSize);

            int freeStackSpace = 0;
            foreach (var it in __instance.m_inventory)
            {
                if (it.m_shared.m_name != item.m_shared.m_name) continue;
                if (it.m_worldLevel != item.m_worldLevel) continue;
                if (item.m_shared.m_maxQuality > 1 && it.m_quality != item.m_quality) continue;
                if (it.m_stack < it.m_shared.m_maxStackSize)
                    freeStackSpace += (it.m_shared.m_maxStackSize - it.m_stack);
            }

            // 2) count *empty* normal cells only in vanilla area
            int normalUsed = __instance.m_inventory.Count(i => i.m_gridPos.y < normalRows);
            int normalFreeCells = (normalRows * width) - normalUsed;

            int quickFreeCells = 0;
            foreach (var p in ExtendedPlayerInventory.EnumerateQuickCells(__instance))
                if (__instance.GetItemAt(p.x, p.y) == null)
                    quickFreeCells++;

            long capacity = freeStackSpace + (long)(normalFreeCells + quickFreeCells) * maxStack;
            __result = capacity >= stack;
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    internal static class Load_FixHiddenItems_Patch
    {
        private static void Postfix(Inventory __instance)
        {
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return;

            var stuck = new List<ItemDrop.ItemData>();
            foreach (var it in __instance.GetAllItems())
            {
                if (ExtendedPlayerInventory.IsHiddenCell(__instance, it.m_gridPos.x, it.m_gridPos.y))
                    stuck.Add(it);
            }

            if (stuck.Count == 0) return;

            foreach (var it in stuck)
            {
                if (__instance.RemoveItem(it))
                {
                    // Vanilla AddItem(ItemData) now uses our FindEmptySlot (quick-aware)
                    if (!__instance.AddItem(it))
                    {
                        it.m_gridPos = new Vector2i(0, 0);
                        __instance.AddItem(it, it.m_stack, 0, 0);
                    }
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
            if (!ExtendedPlayerInventory.ShouldGuard(__instance)) return true;

            if (ExtendedPlayerInventory.IsHiddenCell(__instance, x, y))
            {
                bool ok = __instance.AddItem(item, amount, Mathf.Clamp(item.m_gridPos.x, 0, __instance.GetWidth() - 1), Mathf.Clamp(item.m_gridPos.y, 0, __instance.GetHeight() - 1));
                if (!ok)
                {
                    ok = __instance.AddItem(item);
                }

                if (item.m_stack == 0) fromInventory.RemoveItem(item);
                else fromInventory.Changed();

                __result = ok;
                return false;
            }

            // Equipment target must validate
            if (ExtendedPlayerInventory.IsEquipmentCell(__instance, x, y, out int which))
            {
                var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[which] as InventoryGuiPatches.EquipmentSlot;
                if (slot == null || slot.Valid == null || !slot.Valid(item))
                {
                    __result = false;
                    return false;
                }
            }

            // Quick target allowed; vanilla handles the move
            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
    private static class MoveInventoryToGravePatch
    {
        private static void Postfix(Inventory __instance, Inventory original)
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug("MoveInventoryToGrave");

            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"inv: {__instance.GetHeight()} orig: {original.GetHeight()}");
        }
    }
}