using AzuEPI.Core.Slots;
using AzuEPI.Game.Patches;
using AzuEPI.Slots;
using AzuExtendedPlayerInventory;

namespace AzuEPI.Core.Input;

internal static class GamepadCompatibility
{
    private const int EquipRowsPerColumn = 8; // must match UpdateInventory_Patch.ResizeSlots()
    private static bool LogMoves = false;

    private enum CellKind
    {
        Inventory,
        Equipment,
        Quick,
        Unknown
    }

    private readonly struct SlotCell
    {
        public readonly Vector2i Grid;
        public readonly int EquipIndex;
        public readonly int QuickIndex;
        public readonly int EquipRow;
        public readonly int EquipCol;
        public readonly CellKind Kind;

        public SlotCell(Vector2i g, CellKind k, int eIdx = -1, int qIdx = -1, int eRow = -1, int eCol = -1)
        {
            Grid = g;
            Kind = k;
            EquipIndex = eIdx;
            QuickIndex = qIdx;
            EquipRow = eRow;
            EquipCol = eCol;
        }

        public override string ToString()
        {
            return $"{Kind} @ {Grid} (eIdx={EquipIndex},qIdx={QuickIndex},row={EquipRow},col={EquipCol})";
        }
    }

    private sealed class Layout
    {
        public int Width;
        public int HeightPlayer;
        public int RequiredRowsAdded;
        public int BaseIndex;
        public int EquipCount;
        public int QuickCount;
        public List<SlotCell> Equipment;
        public List<SlotCell> Quick;
        public HashSet<Vector2i> EpiAll;

        public bool IsEpiCell(Vector2i g) => EpiAll.Contains(g);

        public SlotCell? TryGetEquipmentByRowCol(int row, int col)
        {
            if (EquipCount == 0) return null;
            int idx = col * EquipRowsPerColumn + row;
            if (idx < 0 || idx >= EquipCount) return null;
            return Equipment[idx];
        }

        public SlotCell? TryGetQuick(int quickIdx)
        {
            if (quickIdx < 0 || quickIdx >= QuickCount) return null;
            return Quick[quickIdx];
        }
    }

    private static Layout BuildLayout(InventoryGrid grid)
    {
        var player = Player.m_localPlayer;
        var inv = player?.GetInventory();
        var L = new Layout();

        L.Width = inv?.GetWidth() ?? 8;
        int invHeight = inv?.GetHeight() ?? 4;

        int reqRows = API.GetAddedRows(L.Width);
        L.RequiredRowsAdded = reqRows;
        L.HeightPlayer = 4 + ExtraRows.Value
                           + (AddEquipmentRow.Value.isOn() ? reqRows : 0);

        L.EquipCount = CountEquipmentSlots();
        L.QuickCount = Hotkeys.Length;
        L.BaseIndex = L.Width * (L.HeightPlayer - reqRows);

        L.Equipment = new List<SlotCell>(L.EquipCount);
        L.Quick = new List<SlotCell>(L.QuickCount);
        L.EpiAll = new HashSet<Vector2i>();

        for (int i = 0; i < L.EquipCount; ++i)
        {
            int flat = L.BaseIndex + i;
            var g = new Vector2i(flat % L.Width, flat / L.Width);
            int row = i % EquipRowsPerColumn;
            int col = i / EquipRowsPerColumn;
            var c = new SlotCell(g, CellKind.Equipment, eIdx: i, qIdx: -1, eRow: row, eCol: col);
            L.Equipment.Add(c);
            L.EpiAll.Add(g);
        }

        for (int q = 0; q < L.QuickCount; ++q)
        {
            int i = L.EquipCount + q;
            int flat = L.BaseIndex + i;
            var g = new Vector2i(flat % L.Width, flat / L.Width);
            var c = new SlotCell(g, CellKind.Quick, eIdx: -1, qIdx: q);
            L.Quick.Add(c);
            L.EpiAll.Add(g);
        }

        return L;
    }

    private static int CountEquipmentSlots()
    {
        int n = 0;
        var list = InventoryGuiPatches.UpdateInventory_Patch.slots;
        while (n < list.Count && list[n] is Model.EquipmentSlot) ++n;
        return n;
    }

    private static CellKind Classify(Layout L, Vector2i pos, out SlotCell cell)
    {
        for (int i = 0; i < L.EquipCount; ++i)
        {
            var c = L.Equipment[i];
            if (c.Grid == pos)
            {
                cell = c;
                return CellKind.Equipment;
            }
        }

        for (int q = 0; q < L.QuickCount; ++q)
        {
            var c = L.Quick[q];
            if (c.Grid == pos)
            {
                cell = c;
                return CellKind.Quick;
            }
        }

        // Inventory (player grid area)
        if (pos.y >= 0 && pos.y < L.HeightPlayer && pos.x >= 0 && pos.x < L.Width)
        {
            cell = new SlotCell(pos, CellKind.Inventory);
            return CellKind.Inventory;
        }

        cell = new SlotCell(pos, CellKind.Unknown);
        return CellKind.Unknown;
    }

    private static Vector2i MoveLeft(Layout L, Vector2i cur)
    {
        var kind = Classify(L, cur, out var c);
        if (kind == CellKind.Equipment)
        {
            int row = c.EquipRow;
            int col = c.EquipCol - 1;
            while (col >= 0)
            {
                var next = L.TryGetEquipmentByRowCol(row, col);
                if (next.HasValue) return next.Value.Grid;
                col--;
            }

            return new Vector2i(L.Width - 1, Math.Min(row, L.HeightPlayer - 1));
        }

        if (kind == CellKind.Quick)
        {
            int idx = c.QuickIndex - 1;
            if (idx >= 0) return L.Quick[idx].Grid;
            return new Vector2i(L.Width - 1, L.HeightPlayer - 1);
        }

        if (kind == CellKind.Inventory)
        {
            var nx = cur.x - 1;
            if (nx >= 0) return new Vector2i(nx, cur.y);
            return cur.y > 0 ? new Vector2i(L.Width - 1, cur.y - 1) : new Vector2i(0, 0);
        }

        return cur;
    }

    private static Vector2i MoveRight(Layout L, InventoryGrid grid, Vector2i cur)
    {
        var kind = Classify(L, cur, out var c);
        if (kind == CellKind.Equipment)
        {
            int row = c.EquipRow;
            int col = c.EquipCol + 1;
            while (true)
            {
                var next = L.TryGetEquipmentByRowCol(row, col);
                if (next.HasValue) return next.Value.Grid;
                if (col > (L.EquipCount + EquipRowsPerColumn - 1) / EquipRowsPerColumn) break;
                ++col;
                if (col > 32) break;
            }

            return new Vector2i(0, Math.Min(row, L.HeightPlayer - 1));
        }

        if (kind == CellKind.Quick)
        {
            int idx = c.QuickIndex + 1;
            if (idx < L.QuickCount) return L.Quick[idx].Grid;

            return new Vector2i(0, L.HeightPlayer - 1);
        }

        if (kind == CellKind.Inventory)
        {
            if (cur.x >= L.Width - 1)
            {
                if (cur.y <= EquipRowsPerColumn - 1 && L.EquipCount > 0)
                {
                    var next = L.TryGetEquipmentByRowCol(Math.Min(cur.y, EquipRowsPerColumn - 1), 0);
                    if (next.HasValue) return next.Value.Grid;
                }

                // otherwise fall through (stay at boundary)
                return cur;
            }

            return new Vector2i(cur.x + 1, cur.y);
        }

        return cur;
    }

    private static Vector2i MoveUp(Layout L, Vector2i cur)
    {
        var kind = Classify(L, cur, out var c);
        if (kind == CellKind.Quick)
        {
            int col = Mathf.Clamp(c.QuickIndex, 0, (L.EquipCount + EquipRowsPerColumn - 1) / EquipRowsPerColumn);
            int row = Math.Min(EquipRowsPerColumn - 1, L.EquipCount - 1);
            while (row >= 0)
            {
                var eq = L.TryGetEquipmentByRowCol(row, col);
                if (eq.HasValue) return eq.Value.Grid;
                row--;
            }

            return new Vector2i(cur.x, Mathf.Max(0, L.HeightPlayer - 2));
        }

        if (kind == CellKind.Equipment)
        {
            int row = c.EquipRow - 1;
            while (row >= 0)
            {
                var up = L.TryGetEquipmentByRowCol(row, c.EquipCol);
                if (up.HasValue) return up.Value.Grid;
                row--;
            }

            return new Vector2i(Mathf.Clamp(cur.x, 0, L.Width - 1), 0);
        }

        if (kind == CellKind.Inventory)
        {
            int ny = cur.y - 1;
            if (ny >= 0) return new Vector2i(cur.x, ny);
            return cur;
        }

        return cur;
    }

    private static Vector2i MoveDown(Layout L, InventoryGrid grid, Vector2i cur, bool jumpToNextContainer)
    {
        var kind = Classify(L, cur, out var c);
        if (kind == CellKind.Equipment)
        {
            int row = c.EquipRow + 1;
            while (row < EquipRowsPerColumn)
            {
                var dn = L.TryGetEquipmentByRowCol(row, c.EquipCol);
                if (dn.HasValue) return dn.Value.Grid;
                ++row;
            }

            if (L.QuickCount > 0)
            {
                int qIdx = Mathf.Clamp(c.EquipCol, 0, L.QuickCount - 1);
                var q = L.TryGetQuick(qIdx);
                if (q.HasValue) return q.Value.Grid;
            }

            return new Vector2i(Mathf.Clamp(cur.x, 0, L.Width - 1), L.HeightPlayer - 1);
        }

        if (kind == CellKind.Quick)
        {
            if (!jumpToNextContainer) return cur;
            grid.OnMoveToLowerInventoryGrid?.Invoke(cur);
            return cur;
        }

        if (kind == CellKind.Inventory)
        {
            if (cur.y >= L.HeightPlayer - 1)
            {
                if (jumpToNextContainer) grid.OnMoveToLowerInventoryGrid?.Invoke(cur);
                return cur;
            }

            return new Vector2i(cur.x, cur.y + 1);
        }

        return cur;
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGamepad))]
    private static class InventoryGrid_UpdateGamepad_GamepadSupport
    {
        private static bool Prefix(InventoryGrid __instance)
        {
            // Only handle the player grid; let vanilla handle others
            if (__instance != InventoryGui.instance?.m_playerGrid) return true;
            if (!__instance.m_uiGroup.IsActive) return true;
            if (Console.IsVisible()) return true;

            var L = BuildLayout(__instance);
            var cur = __instance.m_selected;

            bool left = ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft");
            bool right = ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight");
            bool up = ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp");
            bool down = ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown");

            if (!(left || right || up || down)) return true; // no directional input; keep vanilla

            Vector2i next = cur;

            if (left)
            {
                next = MoveLeft(L, cur);
            }
            else if (right)
            {
                next = MoveRight(L, __instance, cur);
            }
            else if (up)
            {
                next = MoveUp(L, cur);
            }
            else if (down)
            {
                next = MoveDown(L, __instance, cur, __instance.jumpToNextContainer);
            }

            next.y = Mathf.Clamp(next.y, 0, L.HeightPlayer - 1);
            next.x = Mathf.Clamp(next.x, 0, L.Width - 1);

            if (LogMoves && (next.x != cur.x || next.y != cur.y))
                Debug.Log($"[EPI/Gamepad] {cur} -> {next}");

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

            var L = BuildLayout(__instance);
            var clamped = new Vector2i(Mathf.Clamp(pos.x, 0, L.Width - 1), Mathf.Clamp(pos.y, 0, L.HeightPlayer - 1));

            if (clamped != pos)
            {
                __instance.m_selected = clamped;
                if (LogMoves) Debug.Log($"[EPI/Gamepad] Clamp SetSelection {pos} -> {clamped}");
            }
            else if (LogMoves)
            {
                Debug.Log($"[EPI/Gamepad] SetSelection {pos}");
            }
        }
    }
}