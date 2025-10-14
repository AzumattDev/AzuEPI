using AzuEPI.Core.InventoryHandlers;
using AzuEPI.Game.Patches;

namespace AzuEPI.Core.Slots;

public class SlotHelpers
{
    internal const int EquipRowsPerColumn = 8;
    private const float LeftOffset = -625f;
    private const float VerticalOffset = 160f;

    internal static void ResizeSlots()
    {
        if (OldLayout.Value.isOn())
        {
            for (int i = 0; i < InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length; ++i)
            {
                float y = (i % 3) * -Layout.tileSize + VerticalOffset;
                float x = LeftOffset + (i / 3) * Layout.tileSize + ((i % 3 > (InventoryGuiPatches.UpdateInventory_Patch.slots.Count - 1) % 3 ? 1 : 0) + Math.Max(9 + Hotkeys.Length - InventoryGuiPatches.UpdateInventory_Patch.slots.Count - 1, 0) / 3) * Layout.tileSize / 2;
                InventoryGuiPatches.UpdateInventory_Patch.slots[i].Position = new Vector2(x, y);
            }

            for (int i = 0; i < Hotkeys.Length; ++i) InventoryGuiPatches.UpdateInventory_Patch.slots[InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length + i].Position = new Vector2(LeftOffset + i * Layout.tileSize, 3 * -Layout.tileSize + VerticalOffset);
        }
        else
        {
            float leftX = Layout.equipOriginX;
            float rightX = Layout.equipOriginX + Layout.tileSize * (1f + Layout.columnGapTiles);
            float yBase = Layout.equipOriginY;

            int equipCount = 0;
            while (equipCount < InventoryGuiPatches.UpdateInventory_Patch.slots.Count
                   && InventoryGuiPatches.UpdateInventory_Patch.slots[equipCount] is Model.EquipmentSlot)
                equipCount++;

            int leftUsed = Math.Min(EquipRowsPerColumn, equipCount);
            int rightUsed = Math.Max(0, Math.Min(EquipRowsPerColumn, equipCount - EquipRowsPerColumn));

            for (int i = 0; i < equipCount; ++i)
            {
                bool leftCol = i < EquipRowsPerColumn;
                int row = leftCol ? i : (i - EquipRowsPerColumn);
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
}