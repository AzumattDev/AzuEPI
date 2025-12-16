namespace AzuEPI.Core.Slots;

public class SlotHelpers
{
    internal const int EquipRowsPerColumn = 8;
    private const float LeftOffset = -625f;
    private const float LeftOffsetOld = 655f;
    private const float VerticalOffset = 160f;

    internal static void ResizeSlots()
    {
        if (OldLayout.Value.isOn())
        {
            const int rowsPerColumn = 3;
            const int expectedRegularSlots = 9;

            int totalSlots = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
            int regularSlotCount = totalSlots - Hotkeys.Length;

            for (int i = 0; i < regularSlotCount; ++i)
            {
                int rowIndex = i % rowsPerColumn;
                int columnIndex = i / rowsPerColumn;

                float y = rowIndex * -Layout.tileSize;
                float baseX = LeftOffsetOld + columnIndex * Layout.tileSize;

                int lastSlotRowIndex = (regularSlotCount - 1) % rowsPerColumn;
                int currentRowIsBeyondLastSlot = rowIndex > lastSlotRowIndex ? 1 : 0;

                int totalExpectedSlots = expectedRegularSlots + Hotkeys.Length;
                int emptySlotCount = Math.Max(totalExpectedSlots - totalSlots - 1, 0);
                int emptyColumnCount = emptySlotCount / rowsPerColumn;

                float centeringOffset = (currentRowIsBeyondLastSlot + emptyColumnCount) * Layout.tileSize / 2;

                InventoryGuiPatches.UpdateInventory_Patch.slots[i].Position = new Vector2(baseX + centeringOffset, y);
            }

            if (QuickSlotsVerticalLayout.Value.isOn())
            {
                const int quickslotsPerColumn = 3;
                int totalColumns = (regularSlotCount + rowsPerColumn - 1) / rowsPerColumn;
                float quickslotStartX = LeftOffsetOld + (totalColumns + 0.5f) * Layout.tileSize;

                for (int i = 0; i < Hotkeys.Length; ++i)
                {
                    int slotIndex = regularSlotCount + i;
                    int quickslotColumn = i / quickslotsPerColumn;
                    int quickslotRow = i % quickslotsPerColumn;

                    float quickslotX = quickslotStartX + quickslotColumn * Layout.tileSize;
                    float quickslotY = quickslotRow * -Layout.tileSize;

                    InventoryGuiPatches.UpdateInventory_Patch.slots[slotIndex].Position = new Vector2(quickslotX, quickslotY);
                }
            }
            else
            {
                float hotkeyRowY = rowsPerColumn * -Layout.tileSize;
                for (int i = 0; i < Hotkeys.Length; ++i)
                {
                    int slotIndex = regularSlotCount + i;
                    float hotkeyX = LeftOffsetOld + i * Layout.tileSize;
                    InventoryGuiPatches.UpdateInventory_Patch.slots[slotIndex].Position = new Vector2(hotkeyX, hotkeyRowY);
                }
            }
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

            int quickCount = QuickSlotsAmount.Value;
            int quickStart = equipCount;

            if (quickStart + quickCount > InventoryGuiPatches.UpdateInventory_Patch.slots.Count)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning($"ResizeSlots: Not enough slots in list. Expected {quickStart + quickCount}, but have {InventoryGuiPatches.UpdateInventory_Patch.slots.Count}. Skipping quickslot positioning.");
                return;
            }

            if (quickCount > 0)
            {
                float spanWidth = (rightX - leftX) + Layout.tileSize;

                const int assumedMaxRows = 8;
                float row7Y = yBase - (assumedMaxRows - 2) * Layout.tileSize;

                if (quickCount <= 3)
                {
                    float rowWidth = quickCount * Layout.tileSize;
                    float startX = leftX + (spanWidth - rowWidth) * 0.5f;
                    float quickslotY = row7Y - Layout.tileSize;

                    for (int i = 0; i < quickCount; ++i)
                        InventoryGuiPatches.UpdateInventory_Patch.slots[quickStart + i]!.Position = new Vector2(startX + i * Layout.tileSize, quickslotY);
                }
                else
                {
                    int row1Count = 3;
                    int row2Count = quickCount - 3;

                    float row1Width = row1Count * Layout.tileSize;
                    float row2Width = row2Count * Layout.tileSize;
                    float row1StartX = leftX + (spanWidth - row1Width) * 0.5f;
                    float row2StartX = leftX + (spanWidth - row2Width) * 0.5f;

                    float bottomRowY = row7Y - Layout.tileSize;
                    float topRowY = row7Y;

                    for (int i = 0; i < row1Count; ++i)
                        InventoryGuiPatches.UpdateInventory_Patch.slots[quickStart + i]!.Position = new Vector2(row1StartX + i * Layout.tileSize, topRowY);

                    for (int i = 0; i < row2Count; ++i)
                        InventoryGuiPatches.UpdateInventory_Patch.slots[quickStart + row1Count + i]!.Position = new Vector2(row2StartX + i * Layout.tileSize, bottomRowY);
                }
            }
        }
    }
}