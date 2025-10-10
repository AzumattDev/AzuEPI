using AzuEPI.EPI;
using AzuEPI.InventoryHandlers;
using AzuEPI.Slots;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryPatches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    private static class FindEmptySlot_FilterHidden_AddQuick_Patch
    {
        private static bool Prefix(Inventory __instance, ref Vector2i __result, bool topFirst)
        {
            if (!Placement.ShouldGuard(__instance)) return true;
            __result = Placement.FindEmptyQuickAware(__instance, topFirst);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
    private static class GetEmptySlots_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ref int __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
        {
            if (!Placement.ShouldGuard(__instance)) return true;
            __result = Capacity.FreeNormalCells(__instance) + Capacity.FreeQuickCells(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
    private static class HaveEmptySlot_QuickAware_Patch
    {
        private static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, int ___m_width, int ___m_height)
        {
            if (!Placement.ShouldGuard(__instance)) return true;

            int normalRows = Layout.NormalRows(__instance);

            int normalUsed = ___m_inventory.Count(i => i.m_gridPos.y < normalRows);
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
        private static bool Prefix(Inventory __instance, ref bool __result, List<ItemDrop.ItemData> ___m_inventory, ItemDrop.ItemData item)
        {
            if (Player.m_localPlayer == null) return true;
            if (AddEquipmentRow.Value.isOff() || !Player.m_localPlayer || __instance != Player.m_localPlayer.GetInventory())
                return true;
            AzuExtendedPlayerInventoryLogger.LogDebug("AddItem");
            if (!__instance.IsEquipmentSlotFree(item, out int which))
                return true;

            int normalRows = Layout.NormalRows(__instance);

            __instance.AddItem(item, item.m_stack, which % __instance.GetWidth(), normalRows + which / __instance.GetWidth());
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
            if (!Placement.ShouldGuard(__instance)) return true;

            if (__instance.IsHiddenCell(x, y))
            {
                __result = __instance.AddItem(item, amount, /*x*/ Mathf.Clamp(item.m_gridPos.x, 0, __instance.GetWidth() - 1), /*y*/ Mathf.Clamp(item.m_gridPos.y, 0, __instance.GetHeight() - 1))
                           || __instance.AddItem(item);
                return false;
            }

            // Equipment cells must validate
            if (__instance.IsEquipmentCell(x, y, out int which))
            {
                var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[which] as Model.EquipmentSlot;
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
            if (!Placement.ShouldGuard(__instance)) return true;

            if (__instance.IsHiddenCell(pos.x, pos.y))
            {
                __result = __instance.AddItem(item);
                return false;
            }

            if (__instance.IsEquipmentCell(pos.x, pos.y, out int which))
            {
                var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[which] as Model.EquipmentSlot;
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
            if (!Placement.ShouldGuard(__instance)) return true;

            if (stack <= 0) stack = item.m_stack;

            int maxStack = Mathf.Max(1, item.m_shared.m_maxStackSize);

            int freeStackSpace = Capacity.FreeStackSpace(__instance, item);

            int normalFreeCells = Capacity.FreeNormalCells(__instance);

            int quickFreeCells = Capacity.FreeQuickCells(__instance);

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
            if (!Placement.ShouldGuard(__instance)) return;

            var stuck = new List<ItemDrop.ItemData>();
            foreach (var it in __instance.GetAllItems())
            {
                if (__instance.IsHiddenCell(it.m_gridPos.x, it.m_gridPos.y))
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
            if (!Placement.ShouldGuard(__instance)) return true;

            if (__instance.IsHiddenCell(x, y))
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
            if (__instance.IsEquipmentCell(x, y, out int which))
            {
                var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[which] as Model.EquipmentSlot;
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
            AzuExtendedPlayerInventoryLogger.LogDebug("MoveInventoryToGrave");

            AzuExtendedPlayerInventoryLogger.LogDebug($"inv: {__instance.GetHeight()} orig: {original.GetHeight()}");
        }
    }
}