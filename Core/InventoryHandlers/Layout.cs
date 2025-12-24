using AzuEPI.EPI;
using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Core.InventoryHandlers;

public class Layout
{
    public const int BaseInventoryHeight = 4;
    public const int BaseInventoryWidth = 8;

    internal const float tileSize = 70f;

    public const int MaxQuickSlots = 8;
    public const int OldLayoutQuickslotsPerColumn = 3;
    public const int OldLayoutRegularSlotsPerColumn = 3;
    public const int NewLayoutQuickslotsFirstRow = 4;

    internal static float equipOriginX = -430f;
    internal static float equipOriginY = -75f;
    internal static float columnGapTiles = 4f;

    internal static Transform AzuPlayerBkg = null!;
    internal static readonly Vector2 PlayerBkgAnchorMin = new(-0.80f, 0f);

    internal static readonly Vector2 RepairMovement = new(-460f, 0f);

    internal static Vector2 PreviewAnchorMin = new(0f, 0.14f);
    internal static readonly Vector2 PreviewAnchorMax = new(1f, 0.885f);
    internal static readonly Vector2 PreviewSizeDelta = new(-297f, 0f);
    internal static readonly Vector2 PreviewAnchoredPos = new(-507f, 0f);
    internal static readonly Vector2 PlayerPreviewImageSize = new(500f, 630f);
    internal static readonly Vector2 ToggleButtonsGlgAnchoredPos = new(-222.5f, -30f);
    internal static readonly Vector2 ToggleButtonsHlgAnchoredPosOld = new(-552.5f, -145f);
    internal static readonly Vector2 ToggleButtonsGlgAnchoredPosOldVert = new(810f, -231f);

    internal static readonly Vector2 DropAllSize = new(100f, 30f);
    internal static readonly Vector2 DropAllAnchorMin = new(0.0f, 1.0f);
    internal static readonly Vector2 DropAllAnchorMax = new(0.0f, 1.0f);
    internal static readonly Vector2 DropAllPivot = new(0.0f, 1.0f);

    public static Vector2 SelectedFrameOrigAnchMin;
    public static Vector2 RepairSimpleOrigAnchoredPos;
    public static Vector2 RepairButtonOrigAnchoredPos;

    public static void UpdateInventorySize()
    {
        if (InventoryGui.instance == null) return;
        if (Player.m_localPlayer == null) return;
        int height = API.GetFullHeight(Player.m_localPlayer.m_inventory.GetWidth());
        Player.m_localPlayer.m_inventory.m_height = height;
        Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height = height;

        Player.m_localPlayer.m_inventory.Changed();
        InventoryHealth.InventoryFix();
    }

    public static void UpdateContainerPosition(bool addAPIRows = false)
    {
        InventoryGui? instance = InventoryGui.instance;
        if (!instance) return;
        InventoryGrid? playerGrid = instance.m_playerGrid;
        if (!playerGrid) return;
        float extraSpace = addAPIRows ? (ExtraRows.Value + API.GetAddedRows(instance.m_playerGrid.m_width)) : ExtraRows.Value;
        instance.m_container.pivot = ExtraRows.Value > 0 ? new Vector2(0f, 1f + extraSpace * 0.2f) : new Vector2(0f, 1f);
    }

    public static int NormalRows(Inventory inv)
    {
        int width = inv.GetWidth();
        int height = inv.GetHeight();
        int addedRows = API.GetAddedRows(width);
        return height - addedRows;
    }

    public static int GetBaseSlotIndex(Inventory inv)
    {
        return inv.GetWidth() * NormalRows(inv);
    }

    public static Vector2i ClampToVisible(Inventory inv, Vector2i p)
    {
        return new(Mathf.Clamp(p.x, 0, inv.GetWidth() - 1), Mathf.Clamp(p.y, 0, inv.GetHeight() - 1));
    }

    public static Vector2 GetEquipmentBackAnchorMax()
    {
        if (OldLayout.Value.isOff())
        {
            return new(1.13f + Math.Max(Hotkeys.Length, (InventoryGuiPatches.UpdateInventory_Patch.slots.Count - 1) / NewLayoutQuickslotsFirstRow) * tileSize / 570, 1f);
        }

        int totalSlots = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
        int regularSlotCount = totalSlots - Hotkeys.Length;

        int regularColumns = (regularSlotCount + OldLayoutRegularSlotsPerColumn - 1) / OldLayoutRegularSlotsPerColumn;

        float totalColumns;

        int quickslotColumns = (Hotkeys.Length + OldLayoutQuickslotsPerColumn - 1) / OldLayoutQuickslotsPerColumn;
        totalColumns = regularColumns + 0.5f + quickslotColumns;

        float extraWidth = totalColumns * tileSize / 570;
        return new(1.15f + extraWidth, 1f);
    }

    public static void ApplyLayoutCorrections()
    {
        if (!InventoryGui.instance) return;
        InventoryGui instance = InventoryGui.instance;
        RectTransform? selectedFrame = instance.m_crafting.Find("selected_frame").GetComponent<RectTransform>();
        RectTransform? repairSimple = instance.m_crafting.Find("RepairSimple").GetComponent<RectTransform>();
        RectTransform? repairButton = instance.m_crafting.Find("RepairButton").GetComponent<RectTransform>();
        Image? craftingBkg = instance.m_crafting.Find("Bkg").GetComponent<Image>();

        if (OldLayout.Value.isOn())
        {
            selectedFrame.anchorMin = SelectedFrameOrigAnchMin;
            repairSimple.anchoredPosition = RepairSimpleOrigAnchoredPos;
            repairButton.anchoredPosition = RepairButtonOrigAnchoredPos;
            if (!craftingBkg.isActiveAndEnabled) craftingBkg.enabled = true;
            if (!GlgGo) return;
            if (InventoryGui.instance)
                GlgGo.WithParent(OldLayout.Value.isOff() ? InventoryGui.instance.m_crafting.transform : InventoryGui.instance.m_player.transform, false);
            GlgRt.anchoredPosition = ToggleButtonsGlgAnchoredPosOldVert;
            GUICache.ButtonGridLayoutGroup.constraintCount = QuickSlotsAmount.Value < 3 ? 1 : 2;
        }
        else
        {
            selectedFrame.anchorMin = PlayerBkgAnchorMin;
            repairSimple.anchoredPosition += RepairMovement;
            repairButton.anchoredPosition += RepairMovement;
            if (craftingBkg.isActiveAndEnabled) craftingBkg.enabled = false;
            if (!GlgGo) return;
            if (InventoryGui.instance)
                GlgGo.WithParent(OldLayout.Value.isOff() ? InventoryGui.instance.m_crafting.transform : InventoryGui.instance.m_player.transform, false);
            GlgRt.anchoredPosition = ToggleButtonsGlgAnchoredPos;
        }
    }

    public static void FixPlayerPreview()
    {
        if (!PreviewParent) return;
        PreviewAnchorMin = QuickSlotsAmount.Value > 3 ? new Vector2(0f, 0.24f) : QuickSlotsAmount.Value != 0 ? new Vector2(0f, 0.14f) : Vector2.zero;
        RectTransform previewParentRT = (RectTransform)PreviewParent.transform;
        previewParentRT.anchorMin = PreviewAnchorMin;
    }

    public static void ProjectEquippedIntoGridTail(Player player, InventoryGrid playerGrid)
    {
        Inventory playerInventory = player.GetInventory();
        int inventoryWidth = playerInventory.GetWidth();
        int inventoryHeight = playerInventory.GetHeight();

        int reservedTailRows = API.GetAddedRows(inventoryWidth);
        int firstTailRow = inventoryHeight - reservedTailRows;

        List<Model.Slot?> allSlots = InventoryGuiPatches.UpdateInventory_Patch.slots;

        int equipmentTailStartIndex = GetBaseSlotIndex(playerInventory);
        ItemDrop.ItemData?[] projectedEquippedItemsBySlot = new ItemDrop.ItemData[allSlots.Count];

        for (int i = 0; i < allSlots.Count; ++i)
        {
            Model.Slot? slot = allSlots[i];
            if (slot is not Model.EquipmentSlot equipmentSlot) continue;

            Vector2i destPos = new(equipmentTailStartIndex % inventoryWidth, equipmentTailStartIndex / inventoryWidth);

            if (equipmentSlot.Get?.Invoke(player) is { } equippedItem)
            {
                Vector2i srcPos = equippedItem.m_gridPos;

                if (srcPos != destPos)
                {
                    ItemDrop.ItemData? occupant = playerInventory.GetItemAt(destPos.x, destPos.y);

                    if (occupant != null && occupant != equippedItem)
                    {
                        bool srcInNormalRegion = srcPos.y < firstTailRow;

                        if (srcInNormalRegion)
                        {
                            occupant.m_gridPos = srcPos;
                        }
                        else
                        {
                            ItemDrop.ItemData? itemAtSrc = playerInventory.GetItemAt(srcPos.x, srcPos.y);
                            if (itemAtSrc == null || itemAtSrc == occupant)
                            {
                                occupant.m_gridPos = srcPos;
                            }
                            else
                            {
                                Vector2i free = FindFirstFreeNonSlotCell(playerInventory, inventoryWidth, firstTailRow);
                                if (free.x >= 0)
                                {
                                    occupant.m_gridPos = free;
                                }
                                else
                                {
                                    AzuExtendedPlayerInventoryLogger.LogDebug($"ProjectEquippedIntoGridTail: no free non-slot cell for '{occupant.m_shared.m_name}', leaving it in tail.");
                                }
                            }
                        }
                    }

                    equippedItem.m_gridPos = destPos;
                }

                projectedEquippedItemsBySlot[i] = equippedItem;
            }

            ++equipmentTailStartIndex;
        }

        ExtendedPlayerInventory.equipItems = projectedEquippedItemsBySlot;

        if (AzuEPICharacterPanel.playerPreviewComp && Player.m_localPlayer)
            VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }

    private static Vector2i FindFirstFreeNonSlotCell(Inventory inv, int width, int firstTailRow)
    {
        for (int y = 0; y < firstTailRow; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                Vector2i pos = new(x, y);

                if (API.TryGetSlotIndexAtGridPos(inv, pos, out _))
                    continue;

                if (inv.GetItemAt(x, y) == null)
                    return pos;
            }
        }

        return new Vector2i(-1, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ApplyRepairShift()
    {
        if (GUICache._selectedFrameRT) GUICache._selectedFrameRT.anchorMin = PlayerBkgAnchorMin;
        if (GUICache._repairSimpleRT) GUICache._repairSimpleRT.anchoredPosition += RepairMovement;
        if (GUICache._repairButtonRT) GUICache._repairButtonRT.anchoredPosition += RepairMovement;
    }
}