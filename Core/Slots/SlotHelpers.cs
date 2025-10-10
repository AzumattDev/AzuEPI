using AzuEPI.Core.InventoryHandlers;
using AzuEPI.Game.Patches;

namespace AzuEPI.Core.Slots;

public class SlotHelpers
{
    internal static void ResizeSlots()
    {
        const int rowsPerCol = 8;
        float leftX = Layout.equipOriginX;
        float rightX = Layout.equipOriginX + Layout.tileSize * (1f + Layout.columnGapTiles);
        float yBase = Layout.equipOriginY;

        int equipCount = 0;
        while (equipCount < InventoryGuiPatches.UpdateInventory_Patch.slots.Count && InventoryGuiPatches.UpdateInventory_Patch.slots[equipCount] is Model.EquipmentSlot) equipCount++;

        int leftUsed = Math.Min(rowsPerCol, equipCount);
        int rightUsed = Math.Max(0, Math.Min(rowsPerCol, equipCount - rowsPerCol));

        for (int i = 0; i < equipCount; ++i)
        {
            bool leftCol = i < rowsPerCol;
            int row = leftCol ? i : (i - rowsPerCol);
            float x = leftCol ? leftX : rightX;
            float y = yBase - row * Layout.tileSize;
            InventoryGuiPatches.UpdateInventory_Patch.slots[i]!.Position = new Vector2(x, y);
        }

        int quickCount = Hotkeys.Length;
        int quickStart = equipCount;

        float tallestRows = Mathf.Max(leftUsed, rightUsed);
        float bottomY = yBase - (tallestRows - 1) * Layout.tileSize;

        float spanWidth = (rightX - leftX) + Layout.tileSize;
        float rowWidth = quickCount * Layout.tileSize;
        float startX = leftX + (spanWidth - rowWidth) * 0.5f;

        for (int i = 0; i < quickCount; ++i)
            InventoryGuiPatches.UpdateInventory_Patch.slots[quickStart + i]!.Position = new Vector2(startX + i * Layout.tileSize, bottomY);
    }
}