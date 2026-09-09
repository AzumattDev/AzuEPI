namespace AzuEPI.Core.Slots;

public class SlotHelpers
{
    internal const int EquipRowsPerColumn = 8;
    private const float LeftOffset = -625f;
    private const float LeftOffsetOld = 650f;
    private const float VerticalOffset = 160f;

    internal static void UpdateEquipmentBackgroundAnchors()
    {
        if (!InventoryGui.instance) return;
        Transform? equipmentBkg = InventoryGui.instance.m_player.Find(AzuEquipmentBkgName);
        if (!equipmentBkg) return;

        Vector2 maxAnchor = Layout.GetEquipmentBackAnchorMax();
        if (Chainloader.PluginInfos.TryGetValue(MinimalUiguid, out PluginInfo? pi) && pi != null)
            maxAnchor.x += 0.03f;

        RectTransform? equipBkgRT = equipmentBkg.GetComponent<RectTransform>();
        equipBkgRT.anchorMax = maxAnchor;

        if (OldLayout.Value.isOn() && VanityOption.Value.isOff() && LoadoutOption.Value.isOff())
        {
            equipBkgRT.offsetMin = new Vector2(0f, (Layout.tileSize - 10));
        }
        else
        {
            equipBkgRT.offsetMin = new Vector2(-10, -10);
        }

    }

    internal static void ResizeSlots()
    {
        if (OldLayout.Value.isOn())
        {
            const int expectedRegularSlots = 9;

            int totalSlots = slots.Count;
            int regularSlotCount = totalSlots - QuickSlotsAmount.Value;

            for (int i = 0; i < regularSlotCount; ++i)
            {
                int rowIndex = i % Layout.OldLayoutRegularSlotsPerColumn;
                int columnIndex = i / Layout.OldLayoutRegularSlotsPerColumn;

                float y = rowIndex * -Layout.tileSize;
                float baseX = LeftOffsetOld + columnIndex * Layout.tileSize;

                int lastSlotRowIndex = (regularSlotCount - 1) % Layout.OldLayoutRegularSlotsPerColumn;
                int currentRowIsBeyondLastSlot = rowIndex > lastSlotRowIndex ? 1 : 0;

                int totalExpectedSlots = expectedRegularSlots + QuickSlotsAmount.Value;
                int emptySlotCount = Math.Max(totalExpectedSlots - totalSlots - 1, 0);
                int emptyColumnCount = emptySlotCount / Layout.OldLayoutRegularSlotsPerColumn;

                float centeringOffset = (currentRowIsBeyondLastSlot + emptyColumnCount) * Layout.tileSize / 2;

                slots[i].Position = new Vector2(baseX + centeringOffset, y);
            }

            int totalColumns = (regularSlotCount + Layout.OldLayoutRegularSlotsPerColumn - 1) / Layout.OldLayoutRegularSlotsPerColumn;
            float quickslotStartX = LeftOffsetOld + (totalColumns + 0.5f) * Layout.tileSize;

            for (int i = 0; i < QuickSlotsAmount.Value; ++i)
            {
                int slotIndex = regularSlotCount + i;
                int quickslotColumn = i / Layout.OldLayoutQuickslotsPerColumn;
                int quickslotRow = i % Layout.OldLayoutQuickslotsPerColumn;

                float quickslotX = quickslotStartX + quickslotColumn * Layout.tileSize;
                float quickslotY = quickslotRow * -Layout.tileSize;

                slots[slotIndex].Position = new Vector2(quickslotX, quickslotY);
            }
        }
        else
        {
            float leftX = Layout.equipOriginX;
            float rightX = Layout.equipOriginX + Layout.tileSize * (1f + Layout.columnGapTiles);
            float yBase = Layout.equipOriginY;

            int equipCount = 0;
            while (equipCount < slots.Count
                   && slots[equipCount] is Model.EquipmentSlot)
                equipCount++;

            int leftUsed = Math.Min(EquipRowsPerColumn, equipCount);
            int rightUsed = Math.Max(0, Math.Min(EquipRowsPerColumn, equipCount - EquipRowsPerColumn));

            for (int i = 0; i < equipCount; ++i)
            {
                bool leftCol = i < EquipRowsPerColumn;
                int row = leftCol ? i : (i - EquipRowsPerColumn);
                float x = leftCol ? leftX : rightX;
                float y = yBase - row * Layout.tileSize;
                slots[i]!.Position = new Vector2(x, y);
            }

            int quickCount = QuickSlotsAmount.Value;
            int quickStart = equipCount;

            if (quickStart + quickCount > slots.Count)
            {
                AzuExtendedPlayerInventoryLogger.LogWarning($"ResizeSlots: Not enough slots in list. Expected {quickStart + quickCount}, but have {slots.Count}. Skipping quickslot positioning.");
                return;
            }

            if (quickCount > 0)
            {
                float spanWidth = (rightX - leftX) + Layout.tileSize;

                const int assumedMaxRows = 8;
                float row7Y = yBase - (assumedMaxRows - 2) * Layout.tileSize;

                if (quickCount <= 4)
                {
                    float rowWidth = quickCount * Layout.tileSize;
                    float startX = leftX + (spanWidth - rowWidth) * 0.5f;
                    float quickslotY = row7Y - Layout.tileSize;

                    for (int i = 0; i < quickCount; ++i)
                        slots[quickStart + i]!.Position = new Vector2(startX + i * Layout.tileSize, quickslotY);
                }
                else
                {
                    int row1Count = Math.Min(quickCount, Layout.NewLayoutQuickslotsFirstRow);
                    int row2Count = quickCount - row1Count;

                    float row1Width = row1Count * Layout.tileSize;
                    float row2Width = row2Count * Layout.tileSize;
                    float row1StartX = leftX + (spanWidth - row1Width) * 0.5f;
                    float row2StartX = leftX + (spanWidth - row2Width) * 0.5f;

                    float bottomRowY = row7Y - Layout.tileSize;
                    float topRowY = row7Y;

                    for (int i = 0; i < row1Count; ++i)
                        slots[quickStart + i]!.Position = new Vector2(row1StartX + i * Layout.tileSize, topRowY);

                    for (int i = 0; i < row2Count; ++i)
                        slots[quickStart + row1Count + i]!.Position = new Vector2(row2StartX + i * Layout.tileSize, bottomRowY);
                }
            }
        }
    }
}
