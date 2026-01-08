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
            if (!ZInput.IsGamepadActive() || __instance != InventoryGui.instance?.m_playerGrid || !__instance.m_uiGroup.IsActive || Console.IsVisible())
                return true;

            EpiGridMap.Snapshot s = EpiGridMap.BuildForPlayer();
            Vector2i cur = __instance.m_selected;

            bool left = ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft");
            bool right = ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight");
            bool up = ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp");
            bool down = ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown");

            if (!(left || right || up || down)) return true;

            Vector2i next = cur;
            bool isOldLayout = OldLayout.Value.isOn();

            switch (EpiGridMap.Classify(s, cur, out int equipIdx, out int quickIdx))
            {
                case EpiGridMap.CellKind.Equipment:
                {
                    EpiGridMap.EquipIndexToRowCol(s, equipIdx, out int row, out int col);

                    if (left)
                    {
                        int prevCol = col - 1;
                        if (prevCol >= 0 && EpiGridMap.TryEquipIndexFromRowCol(s, row, prevCol, out int e2))
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        else if (!isOldLayout && col > 0 && s.QuickCount > 0)
                            next = EpiGridMap.QuickIndexToGrid(s, 0);
                        else
                            next = EpiGridMap.LeftInventoryEdge(s, row);
                    }
                    else if (right)
                    {
                        if (++col < s.EquipCols && EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out int e2))
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        else if (s is { QuickCount: > 0 } && row < s.EquipRowsPerColumn)
                            next = EpiGridMap.QuickIndexToGrid(s, Mathf.Clamp(row, 0, s.QuickCount - 1));
                        else if (!isOldLayout && s.QuickCount > 0)
                            next = EpiGridMap.QuickIndexToGrid(s, 0);
                        else
                            next = EpiGridMap.RightInventoryEdge(s, row);
                    }
                    else if (up)
                    {
                        if (--row >= 0 && EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out int e2))
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        else
                            next = EpiGridMap.TopInventoryEdge(s, cur.x);
                    }
                    else if (down)
                    {
                        if (++row < s.EquipRowsPerColumn && EpiGridMap.TryEquipIndexFromRowCol(s, row, col, out int e2))
                            next = EpiGridMap.EquipIndexToGrid(s, e2);
                        else if (!isOldLayout && s.QuickCount > 0)
                            next = EpiGridMap.QuickIndexToGrid(s, 0);
                        else
                            next = EpiGridMap.BottomInventoryEdge(s, cur.x);
                    }

                    break;
                }

                case EpiGridMap.CellKind.Quick:
                {
                    if (isOldLayout)
                    {
                        int quickColumn = quickIdx / Layout.OldLayoutQuickslotsPerColumn;
                        int quickRow = quickIdx % Layout.OldLayoutQuickslotsPerColumn;

                        if (left)
                        {
                            if (quickColumn > 0)
                            {
                                int prevQuickIdx = (quickColumn - 1) * Layout.OldLayoutQuickslotsPerColumn + quickRow;
                                next = prevQuickIdx < s.QuickCount ? EpiGridMap.QuickIndexToGrid(s, prevQuickIdx) : cur;
                            }
                            else if (s.EquipCount > 0)
                            {
                                next = EpiGridMap.TryFindValidEquipmentSlot(s, quickRow, s.EquipCols - 1, out int eIdx)
                                    ? EpiGridMap.EquipIndexToGrid(s, eIdx)
                                    : EpiGridMap.LeftInventoryEdge(s, quickRow);
                            }
                            else
                                next = EpiGridMap.LeftInventoryEdge(s, quickRow);
                        }
                        else if (right)
                        {
                            int nextQuickIdx = (quickColumn + 1) * Layout.OldLayoutQuickslotsPerColumn + quickRow;
                            next = nextQuickIdx < s.QuickCount ? EpiGridMap.QuickIndexToGrid(s, nextQuickIdx) : EpiGridMap.RightInventoryEdge(s, quickRow);
                        }
                        else if (up)
                        {
                            next = quickRow > 0 ? EpiGridMap.QuickIndexToGrid(s, quickIdx - 1) : EpiGridMap.TopInventoryEdge(s, cur.x);
                        }
                        else if (down)
                        {
                            int downQuickIdx = quickIdx + 1;
                            next = downQuickIdx < s.QuickCount && (downQuickIdx / Layout.OldLayoutQuickslotsPerColumn) == quickColumn
                                ? EpiGridMap.QuickIndexToGrid(s, downQuickIdx)
                                : EpiGridMap.BottomInventoryEdge(s, cur.x);
                        }
                    }
                    else if (!isOldLayout)
                    {
                        if (left)
                        {
                            if (quickIdx > 0)
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx - 1);
                            else if (s.EquipCount > 0)
                            {
                                int targetRow = Math.Min(s.EquipRowsPerColumn - 1, s.EquipCount - 1);
                                next = EpiGridMap.TryEquipIndexFromRowCol(s, targetRow, 0, out int eIdx)
                                    ? EpiGridMap.EquipIndexToGrid(s, eIdx)
                                    : EpiGridMap.LeftInventoryEdge(s, s.NormalInventoryHeight - 1);
                            }
                            else
                                next = EpiGridMap.LeftInventoryEdge(s, s.NormalInventoryHeight - 1);
                        }
                        else if (right)
                        {
                            if (quickIdx + 1 < s.QuickCount)
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx + 1);
                            else if (s.EquipCols > 1 && s.EquipCount > s.EquipRowsPerColumn)
                            {
                                int targetRow = Math.Min(s.EquipRowsPerColumn - 1, s.EquipCount - s.EquipRowsPerColumn - 1);
                                next = EpiGridMap.TryEquipIndexFromRowCol(s, targetRow, 1, out int eIdx)
                                    ? EpiGridMap.EquipIndexToGrid(s, eIdx)
                                    : EpiGridMap.RightInventoryEdge(s, s.NormalInventoryHeight - 1);
                            }
                            else
                                next = EpiGridMap.RightInventoryEdge(s, s.NormalInventoryHeight - 1);
                        }
                        else if (up)
                            next = quickIdx > 0 ? EpiGridMap.QuickIndexToGrid(s, quickIdx - 1) : EpiGridMap.BottomInventoryEdge(s, cur.x);
                        else if (down)
                        {
                            if (quickIdx + 1 < s.QuickCount)
                                next = EpiGridMap.QuickIndexToGrid(s, quickIdx + 1);
                            else if (__instance.jumpToNextContainer)
                                __instance.OnMoveToLowerInventoryGrid?.Invoke(cur);
                        }
                    }

                    break;
                }

                case EpiGridMap.CellKind.Inventory:
                {
                    if (left)
                    {
                        if (cur.x > 0)
                            EpiGridMap.TryMoveInInventory(s, cur, -1, 0, out next);
                        else if (cur.y > 0)
                            next = new Vector2i(s.Width - 1, cur.y - 1);
                        else
                            next = new Vector2i(0, 0);
                    }
                    else if (right)
                    {
                        if (cur.x < s.Width - 1)
                        {
                            EpiGridMap.TryMoveInInventory(s, cur, 1, 0, out next);
                        }
                        else
                        {
                            if (cur.y <= s.EquipRowsPerColumn - 1 && s.EquipCount > 0 && EpiGridMap.TryEquipIndexFromRowCol(s, cur.y, 0, out int e0))
                                next = EpiGridMap.EquipIndexToGrid(s, e0);
                            else if (s.QuickCount > 0)
                                next = EpiGridMap.QuickIndexToGrid(s, 0);
                            else
                                next = cur;
                        }
                    }
                    else if (up)
                    {
                        if (cur.y > 0)
                            EpiGridMap.TryMoveInInventory(s, cur, 0, -1, out next);
                        else
                            next = cur;
                    }
                    else if (down)
                    {
                        if (cur.y < s.NormalInventoryHeight - 1)
                            EpiGridMap.TryMoveInInventory(s, cur, 0, 1, out next);
                        else if (__instance.jumpToNextContainer)
                            __instance.OnMoveToLowerInventoryGrid?.Invoke(cur);
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

            if (clamped == pos) return;

            __instance.m_selected = clamped;
#if DEBUG
            if (LogMoves) Debug.Log($"[EPI/Gamepad] Clamp {pos} -> {clamped}");
#endif
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
        public readonly int NormalInventoryHeight;

        public Snapshot(int width, int playerHeight, int baseIndex, int equipCount, int quickCount, int equipCols, int equipRowsPerColumn, int normalInventoryHeight)
        {
            Width = width;
            PlayerHeight = playerHeight;
            BaseIndex = baseIndex;
            EquipCount = equipCount;
            QuickCount = quickCount;
            EquipCols = equipCols;
            EquipRowsPerColumn = equipRowsPerColumn;
            NormalInventoryHeight = normalInventoryHeight;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountEquipmentSlots()
    {
        List<Model.Slot?> list = slots;
        int i = 0;
        while (i < list.Count && list[i] is Model.EquipmentSlot) ++i;
        return i;
    }

    internal static Snapshot BuildForPlayer()
    {
        Player? p = Player.m_localPlayer;
        Inventory? inv = p?.GetInventory();

        int width = inv?.GetWidth() ?? Layout.BaseInventoryWidth;
        int baseInvHeight = Layout.BaseInventoryHeight;
        int extraRows = ExtraRows.Value;

        int equipCount = CountEquipmentSlots();
        int quickCount = Hotkeys.Length;

        if (OldLayout.Value.isOn())
        {
            int equipCols = Mathf.CeilToInt(equipCount / (float)Layout.OldLayoutRegularSlotsPerColumn);

            int usedEquipRows = Math.Min(Layout.OldLayoutRegularSlotsPerColumn, Math.Max(1, equipCount));
            int bandHeight = (equipCount > 0 ? usedEquipRows : 0);

            int playerHeight = baseInvHeight + extraRows + bandHeight;
            int normalInventoryHeight = baseInvHeight + extraRows;
            int baseIndex = width * (playerHeight - bandHeight);

            return new Snapshot(width, playerHeight, baseIndex, equipCount, quickCount, equipCols, Layout.OldLayoutRegularSlotsPerColumn, normalInventoryHeight);
        }
        else
        {
            int epiRows = AddEquipmentRow.Value.isOn() ? API.GetAddedRows(width) : 0;
            int playerHeight = baseInvHeight + extraRows + epiRows;
            int normalInventoryHeight = baseInvHeight + extraRows;
            int equipRowsPerCol = SlotHelpers.EquipRowsPerColumn;
            int equipCols = (equipCount + equipRowsPerCol - 1) / equipRowsPerCol;
            int baseIndex = Layout.GetBaseSlotIndex(inv);

            return new Snapshot(width, playerHeight, baseIndex, equipCount, quickCount, equipCols, equipRowsPerCol, normalInventoryHeight);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryEquipIndexFromRowCol(in Snapshot s, int row, int col, out int equipIndex)
    {
        equipIndex = col * s.EquipRowsPerColumn + row;
        return equipIndex >= 0 && equipIndex < s.EquipCount;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2i LeftInventoryEdge(in Snapshot s, int y) =>
        new(s.Width - 1, Math.Min(y, s.NormalInventoryHeight - 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2i RightInventoryEdge(in Snapshot s, int y) =>
        new(0, Math.Min(y, s.NormalInventoryHeight - 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2i TopInventoryEdge(in Snapshot s, int x) =>
        new(Mathf.Clamp(x, 0, s.Width - 1), 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector2i BottomInventoryEdge(in Snapshot s, int x) =>
        new(Mathf.Clamp(x, 0, s.Width - 1), s.NormalInventoryHeight - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryMoveInInventory(in Snapshot s, Vector2i from, int dx, int dy, out Vector2i to)
    {
        to = new Vector2i(from.x + dx, from.y + dy);
        if (!IsValidInventoryCell(s, to))
        {
            to = from;
            return false;
        }

        return true;
    }

    internal static bool TryFindValidEquipmentSlot(in Snapshot s, int preferredRow, int preferredCol, out int equipIndex)
    {
        int targetRow = Mathf.Clamp(preferredRow, 0, s.EquipRowsPerColumn - 1);
        int targetCol = preferredCol;

        while (targetCol >= 0 && !TryEquipIndexFromRowCol(s, targetRow, targetCol, out _))
        {
            targetCol--;
            if (targetCol >= 0 || targetRow <= 0) continue;
            targetRow--;
            targetCol = s.EquipCols - 1;
        }

        bool yes = TryEquipIndexFromRowCol(s, targetRow, targetCol, out equipIndex);
        return targetCol >= 0 && yes;
    }
}