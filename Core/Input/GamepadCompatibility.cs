using AzuEPI.Core.Slots;
using AzuEPI.Game.Patches;

namespace AzuEPI.Core.Input;

internal static class GamepadCompatibility
{
#if DEBUG
    private static bool LogMoves = false;
#endif

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGamepad))]
    private static class InventoryGrid_UpdateGamepad_GamepadSupport
    {
        private static bool Prefix(InventoryGrid __instance)
        {
            if (__instance != InventoryGui.instance?.m_playerGrid) return true;
            if (!__instance.m_uiGroup.IsActive) return true;
            if (Console.IsVisible()) return true;

            EpiGridMap.Snapshot s = EpiGridMap.BuildForPlayer();
            Vector2i cur = __instance.m_selected;

            bool left = ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft");
            bool right = ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight");
            bool up = ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp");
            bool down = ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown");

            if (!(left || right || up || down)) return true;

            Vector2i next = cur;

            switch (EpiGridMap.Classify(s, cur, out int equipIdx, out int quickIdx))
            {
                case EpiGridMap.CellKind.Equipment:
                {
                    EpiGridMap.EquipIndexToRowCol(s, equipIdx, out int row, out int col);
                    bool isOldLayout = OldLayout.Value.isOn();

                    if (left)
                    {
                        int prevCol = col - 1;
                        if (prevCol >= 0 && EpiGridMap.TryEquipIndexFromRowCol(s, row, prevCol, out int e2))
                        {
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        }
                        else if (!isOldLayout && col > 0 && s.QuickCount > 0)
                        {
                            next = EpiGridMap.QuickIndexToGrid(s, 0);
                        }
                        else
                        {
                            next = new Vector2i(s.Width - 1, Math.Min(row, s.NormalInventoryHeight - 1));
                        }
                    }
                    else if (right)
                    {
                        if (++col < s.EquipCols && EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out int e2))
                        {
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        }
                        else if (s.IsVerticalQuickslotLayout && s.QuickCount > 0 && row < s.EquipRowsPerColumn)
                        {
                            int q = Mathf.Clamp(row, 0, s.QuickCount - 1);
                            next = EpiGridMap.QuickIndexToGrid(s, q);
                        }
                        else if (!isOldLayout && s.QuickCount > 0)
                        {
                            next = EpiGridMap.QuickIndexToGrid(s, 0);
                        }
                        else
                        {
                            next = new Vector2i(0, Math.Min(row, s.NormalInventoryHeight - 1));
                        }
                    }
                    else if (up)
                    {
                        if (--row >= 0 && EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out int e2))
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        else
                            next = new Vector2i(Mathf.Clamp(cur.x, 0, s.Width - 1), 0);
                    }
                    else if (down)
                    {
                        if (++row < s.EquipRowsPerColumn && EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out int e2))
                        {
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        }
                        else if (s.QuickCount > 0 && !s.IsVerticalQuickslotLayout && isOldLayout)
                        {
                            int q = Mathf.Clamp(col, 0, s.QuickCount - 1);
                            next = EpiGridMap.QuickIndexToGrid(s, q);
                        }
                        else if (!isOldLayout && s.QuickCount > 0)
                        {
                            next = EpiGridMap.QuickIndexToGrid(s, 0);
                        }
                        else
                        {
                            next = new Vector2i(Mathf.Clamp(cur.x, 0, s.Width - 1), s.NormalInventoryHeight - 1);
                        }
                    }

                    break;
                }

                case EpiGridMap.CellKind.Quick:
                {
                    bool isOldLayout = OldLayout.Value.isOn();

                    if (s.IsVerticalQuickslotLayout)
                    {
                        const int quickslotsPerColumn = 3;
                        int quickColumn = quickIdx / quickslotsPerColumn;
                        int quickRow = quickIdx % quickslotsPerColumn;

                        if (left)
                        {
                            if (quickColumn > 0)
                            {
                                int prevQuickIdx = (quickColumn - 1) * quickslotsPerColumn + quickRow;
                                if (prevQuickIdx < s.QuickCount)
                                    next = EpiGridMap.QuickIndexToGrid(s, prevQuickIdx);
                                else
                                    next = cur;
                            }
                            else if (s.EquipCount > 0)
                            {
                                int targetRow = Mathf.Clamp(quickRow, 0, s.EquipRowsPerColumn - 1);
                                int targetCol = s.EquipCols - 1;
                                while (targetCol >= 0 && !EpiGridMap.TryEquipIndexFromRowCol(s, targetRow, targetCol, out _))
                                {
                                    targetCol--;
                                    if (targetCol < 0 && targetRow > 0)
                                    {
                                        targetRow--;
                                        targetCol = s.EquipCols - 1;
                                    }
                                }
                                if (targetCol >= 0 && EpiGridMap.TryEquipIndexFromRowCol(s, targetRow, targetCol, out int eIdx))
                                    next = EpiGridMap.EquipIndexToGrid(s, eIdx);
                                else
                                    next = new Vector2i(s.Width - 1, Math.Min(quickRow, s.NormalInventoryHeight - 1));
                            }
                            else
                            {
                                next = new Vector2i(s.Width - 1, Math.Min(quickRow, s.NormalInventoryHeight - 1));
                            }
                        }
                        else if (right)
                        {
                            int nextQuickIdx = (quickColumn + 1) * quickslotsPerColumn + quickRow;
                            if (nextQuickIdx < s.QuickCount)
                                next = EpiGridMap.QuickIndexToGrid(s, nextQuickIdx);
                            else
                                next = new Vector2i(0, Math.Min(quickRow, s.NormalInventoryHeight - 1));
                        }
                        else if (up)
                        {
                            if (quickRow > 0)
                            {
                                int upQuickIdx = quickIdx - 1;
                                next = EpiGridMap.QuickIndexToGrid(s, upQuickIdx);
                            }
                            else
                            {
                                next = new Vector2i(cur.x, 0);
                            }
                        }
                        else if (down)
                        {
                            int downQuickIdx = quickIdx + 1;
                            if (downQuickIdx < s.QuickCount && (downQuickIdx / quickslotsPerColumn) == quickColumn)
                            {
                                next = EpiGridMap.QuickIndexToGrid(s, downQuickIdx);
                            }
                            else
                            {
                                next = new Vector2i(cur.x, s.NormalInventoryHeight - 1);
                            }
                        }
                    }
                    else if (!isOldLayout)
                    {
                        if (left)
                        {
                            if (quickIdx > 0)
                            {
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx - 1);
                            }
                            else if (s.EquipCount > 0)
                            {
                                int targetRow = Math.Min(s.EquipRowsPerColumn - 1, s.EquipCount - 1);
                                if (EpiGridMap.TryEquipIndexFromRowCol(s, targetRow, 0, out int eIdx))
                                    next = EpiGridMap.EquipIndexToGrid(s, eIdx);
                                else
                                    next = new Vector2i(s.Width - 1, s.NormalInventoryHeight - 1);
                            }
                            else
                            {
                                next = new Vector2i(s.Width - 1, s.NormalInventoryHeight - 1);
                            }
                        }
                        else if (right)
                        {
                            if (quickIdx + 1 < s.QuickCount)
                            {
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx + 1);
                            }
                            else if (s.EquipCols > 1 && s.EquipCount > s.EquipRowsPerColumn)
                            {
                                int targetRow = Math.Min(s.EquipRowsPerColumn - 1, s.EquipCount - s.EquipRowsPerColumn - 1);
                                if (EpiGridMap.TryEquipIndexFromRowCol(s, targetRow, 1, out int eIdx))
                                    next = EpiGridMap.EquipIndexToGrid(s, eIdx);
                                else
                                    next = new Vector2i(0, s.NormalInventoryHeight - 1);
                            }
                            else
                            {
                                next = new Vector2i(0, s.NormalInventoryHeight - 1);
                            }
                        }
                        else if (up)
                        {
                            if (quickIdx > 0)
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx - 1);
                            else
                                next = new Vector2i(cur.x, s.NormalInventoryHeight - 1);
                        }
                        else if (down)
                        {
                            if (quickIdx + 1 < s.QuickCount)
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx + 1);
                            else if (__instance.jumpToNextContainer)
                                __instance.OnMoveToLowerInventoryGrid?.Invoke(cur);
                        }
                    }
                    else
                    {
                        if (left)
                        {
                            if (quickIdx > 0)
                            {
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx - 1);
                            }
                            else
                            {
                                next = new Vector2i(s.Width - 1, s.NormalInventoryHeight - 1);
                            }
                        }
                        else if (right)
                        {
                            next = (quickIdx + 1 < s.QuickCount)
                                ? EpiGridMap.QuickIndexToGrid(s, quickIdx + 1)
                                : new Vector2i(0, s.NormalInventoryHeight - 1);
                        }
                        else if (up)
                        {
                            if (s.EquipCount > 0)
                            {
                                int col = Mathf.Clamp(quickIdx, 0, s.EquipCols - 1);
                                int row = Math.Min(s.EquipRowsPerColumn - 1, s.EquipCount - 1);
                                while (row >= 0 && !EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out _)) row--;
                                next = (row >= 0)
                                    ? EpiGridMap.EquipIndexToGrid(s, col * s.EquipRowsPerColumn + row)
                                    : new Vector2i(cur.x, s.NormalInventoryHeight - 1);
                            }
                            else
                            {
                                next = new Vector2i(cur.x, s.NormalInventoryHeight - 1);
                            }
                        }
                        else if (down)
                        {
                            if (__instance.jumpToNextContainer)
                                __instance.OnMoveToLowerInventoryGrid?.Invoke(cur);
                            next = cur;
                        }
                    }

                    break;
                }

                case EpiGridMap.CellKind.Inventory:
                {
                    if (left)
                    {
                        if (cur.x > 0)
                        {
                            Vector2i candidate = new Vector2i(cur.x - 1, cur.y);
                            if (EpiGridMap.IsValidInventoryCell(s, candidate))
                                next = candidate;
                            else
                                next = cur;
                        }
                        else if (cur.y > 0)
                        {
                            next = new Vector2i(s.Width - 1, cur.y - 1);
                        }
                        else
                        {
                            next = new Vector2i(0, 0);
                        }
                    }
                    else if (right)
                    {
                        if (cur.x < s.Width - 1)
                        {
                            Vector2i candidate = new Vector2i(cur.x + 1, cur.y);
                            if (EpiGridMap.IsValidInventoryCell(s, candidate))
                                next = candidate;
                            else
                                next = cur;
                        }
                        else
                        {
                            bool foundEquipment = false;
                            if (cur.y <= s.EquipRowsPerColumn - 1 && s.EquipCount > 0 &&
                                EpiGridMap.TryEquipIndexFromRowCol(s, cur.y, 0, out int e0))
                            {
                                next = EpiGridMap.EquipIndexToGrid(s, e0);
                                foundEquipment = true;
                            }

                            if (!foundEquipment && s.QuickCount > 0)
                            {
                                next = EpiGridMap.QuickIndexToGrid(s, 0);
                            }
                            else if (!foundEquipment)
                            {
                                next = cur;
                            }
                        }
                    }
                    else if (up)
                    {
                        if (cur.y > 0)
                        {
                            Vector2i candidate = new Vector2i(cur.x, cur.y - 1);
                            if (EpiGridMap.IsValidInventoryCell(s, candidate))
                                next = candidate;
                            else
                                next = cur;
                        }
                        else
                        {
                            next = cur;
                        }
                    }
                    else if (down)
                    {
                        if (cur.y < s.NormalInventoryHeight - 1)
                        {
                            Vector2i candidate = new Vector2i(cur.x, cur.y + 1);
                            if (EpiGridMap.IsValidInventoryCell(s, candidate))
                                next = candidate;
                            else
                                next = cur;
                        }
                        else if (__instance.jumpToNextContainer)
                        {
                            __instance.OnMoveToLowerInventoryGrid?.Invoke(cur);
                        }
                    }

                    break;
                }

                case EpiGridMap.CellKind.Unknown:
                default: break;
            }

            next.x = Mathf.Clamp(next.x, 0, s.Width - 1);
            next.y = Mathf.Clamp(next.y, 0, s.PlayerHeight - 1);

#if DEBUG
            if (LogMoves && (next.x != cur.x || next.y != cur.y))
                Debug.Log($"[EPI/Gamepad] {cur} -> {next}");
#endif

            __instance.m_selected = next;
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.SetSelection))]
    private static class InventoryGrid_SetSelection_GamepadSupport
    {
        private static void Postfix(InventoryGrid __instance, Vector2i pos)
        {
            if (__instance != InventoryGui.instance?.m_playerGrid) return;
            EpiGridMap.Snapshot s = EpiGridMap.BuildForPlayer();
            Vector2i clamped = new(Mathf.Clamp(pos.x, 0, s.Width - 1), Mathf.Clamp(pos.y, 0, s.PlayerHeight - 1));
            if (clamped != pos)
            {
                __instance.m_selected = clamped;
#if DEBUG
                if (LogMoves) Debug.Log($"[EPI/Gamepad] Clamp {pos} -> {clamped}");
#endif
            }
        }
    }
}

internal static class EpiGridMap
{
    internal readonly struct Snapshot
    {
        public readonly int Width;
        public readonly int PlayerHeight;
        public readonly int BaseIndex;
        public readonly int EquipCount;
        public readonly int QuickCount;
        public readonly int EquipCols;
        public readonly int EquipRowsPerColumn;
        public readonly bool IsVerticalQuickslotLayout;
        public readonly int NormalInventoryHeight;

        public Snapshot(int width, int playerHeight, int baseIndex, int equipCount, int quickCount, int equipCols, int equipRowsPerColumn, bool isVerticalQuickslotLayout, int normalInventoryHeight)
        {
            Width = width;
            PlayerHeight = playerHeight;
            BaseIndex = baseIndex;
            EquipCount = equipCount;
            QuickCount = quickCount;
            EquipCols = equipCols;
            EquipRowsPerColumn = equipRowsPerColumn;
            IsVerticalQuickslotLayout = isVerticalQuickslotLayout;
            NormalInventoryHeight = normalInventoryHeight;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountEquipmentSlots()
    {
        List<Model.Slot?> list = InventoryGuiPatches.UpdateInventory_Patch.slots;
        int i = 0;
        while (i < list.Count && list[i] is Model.EquipmentSlot) ++i;
        return i;
    }

    internal static Snapshot BuildForPlayer()
    {
        Player? p = Player.m_localPlayer;
        Inventory? inv = p?.GetInventory();

        int width = inv?.GetWidth() ?? InventoryHandlers.Layout.BaseInventoryWidth;
        int baseInvHeight = InventoryHandlers.Layout.BaseInventoryHeight;
        int extraRows = ExtraRows.Value;

        int equipCount = CountEquipmentSlots();
        int quickCount = Hotkeys.Length;

        if (OldLayout.Value.isOn())
        {
            int equipRowsPerCol = 3;
            int equipCols = Mathf.CeilToInt(equipCount / (float)equipRowsPerCol);
            bool isVerticalQuickslotLayout = QuickSlotsVerticalLayout.Value.isOn();

            int usedEquipRows = Math.Min(equipRowsPerCol, Math.Max(1, equipCount));
            int quickRows = (quickCount > 0 && !isVerticalQuickslotLayout) ? 1 : 0;
            int bandHeight = (equipCount > 0 ? usedEquipRows : 0) + quickRows;

            int playerHeight = baseInvHeight + extraRows + bandHeight;
            int normalInventoryHeight = baseInvHeight + extraRows;
            int baseIndex = width * (playerHeight - bandHeight);

            return new Snapshot(width, playerHeight, baseIndex, equipCount, quickCount, equipCols, equipRowsPerCol, isVerticalQuickslotLayout, normalInventoryHeight);
        }
        else
        {
            int epiRows = (AddEquipmentRow.Value.isOn()) ? API.GetAddedRows(width) : 0;
            int playerHeight = baseInvHeight + extraRows + epiRows;
            int normalInventoryHeight = baseInvHeight + extraRows;

            int equipRowsPerCol = SlotHelpers.EquipRowsPerColumn;
            int equipCols = (equipCount + equipRowsPerCol - 1) / equipRowsPerCol;

            int baseIndex = InventoryHandlers.Layout.GetBaseSlotIndex(inv);

            return new Snapshot(width, playerHeight, baseIndex, equipCount, quickCount, equipCols, equipRowsPerCol, false, normalInventoryHeight);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryEquipIndexFromRowCol(in Snapshot s, int row, int col, out int equipIndex)
    {
        equipIndex = col * s.EquipRowsPerColumn + row;
        return (equipIndex >= 0 && equipIndex < s.EquipCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2i EquipIndexToGrid(in Snapshot s, int equipIndex)
    {
        int flat = s.BaseIndex + equipIndex;
        return new Vector2i(flat % s.Width, flat / s.Width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGridToEquipIndex(in Snapshot s, Vector2i pos, out int equipIndex)
    {
        equipIndex = -1;
        int flat = pos.y * s.Width + pos.x;
        int epiLinear = flat - s.BaseIndex;
        if (epiLinear < 0 || epiLinear >= s.EquipCount) return false;
        equipIndex = epiLinear;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EquipIndexToRowCol(in Snapshot s, int equipIndex, out int row, out int col)
    {
        row = equipIndex % s.EquipRowsPerColumn;
        col = equipIndex / s.EquipRowsPerColumn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGridToQuickIndex(in Snapshot s, Vector2i pos, out int quickIndex)
    {
        quickIndex = -1;
        int flat = pos.y * s.Width + pos.x;
        int epiLinear = flat - s.BaseIndex;
        int local = epiLinear - s.EquipCount;
        if (local < 0 || local >= s.QuickCount) return false;
        quickIndex = local;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2i QuickIndexToGrid(in Snapshot s, int quickIndex)
    {
        int flat = s.BaseIndex + s.EquipCount + quickIndex;
        return new Vector2i(flat % s.Width, flat / s.Width);
    }

    internal enum CellKind
    {
        Inventory,
        Equipment,
        Quick,
        Unknown
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CellKind Classify(in Snapshot s, Vector2i pos, out int equipIdx, out int quickIdx)
    {
        equipIdx = -1;
        quickIdx = -1;

        if (TryGridToEquipIndex(s, pos, out equipIdx)) return CellKind.Equipment;
        if (TryGridToQuickIndex(s, pos, out quickIdx)) return CellKind.Quick;

        if (pos.y >= 0 && pos.y < s.PlayerHeight && pos.x >= 0 && pos.x < s.Width)
            return CellKind.Inventory;

        return CellKind.Unknown;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsValidInventoryCell(in Snapshot s, Vector2i pos)
    {
        CellKind kind = Classify(s, pos, out _, out _);
        return kind == CellKind.Inventory;
    }
}