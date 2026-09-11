using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Core.InventoryHandlers;

public class Layout
{
    public const int BaseInventoryHeight = 4;
    public const int BaseInventoryWidth = 8;

    // 1.0 lets the trader sell inventory rows, tracked on the player as the "invrows" unique key
    public const int MaxVanillaRows = 9;
    private static int _vanillaRows = BaseInventoryHeight;

    public static int VanillaRows => _vanillaRows;

    public static int NormalInventoryRows => _vanillaRows + ExtraRows.Value;

    public static void SetVanillaRows(int rows) => _vanillaRows = Mathf.Clamp(rows, BaseInventoryHeight, MaxVanillaRows);

    public static int RefreshVanillaRows(Player? player = null)
    {
        player ??= Player.m_localPlayer;
        if (player && player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
            _vanillaRows = Mathf.Clamp(rows, BaseInventoryHeight, MaxVanillaRows);
        else
            _vanillaRows = BaseInventoryHeight;

        return _vanillaRows;
    }

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
    internal static readonly Vector2 ToggleButtonsGlgAnchoredPos = new(-222.5f, -5f);
    internal static readonly Vector2 ToggleButtonsHlgAnchoredPosOld = new(-552.5f, -145f);
    internal static readonly Vector2 ToggleButtonsGlgAnchoredPosOldVert = new(810f, -221f);

    internal static readonly Vector2 DropAllSize = new(100f, 30f);
    internal static readonly Vector2 DropAllAnchorMin = new(0.0f, 1.0f);
    internal static readonly Vector2 DropAllAnchorMax = new(0.0f, 1.0f);
    internal static readonly Vector2 DropAllPivot = new(0.0f, 1.0f);

    public static Vector2 SelectedFrameOrigAnchMin;
    public static Vector2 RepairSimpleOrigAnchoredPos;
    public static Vector2 RepairButtonOrigAnchoredPos;
    public static Vector2 EnchantmentMenuOrigAnchoredPos;
    public static Vector2 EnchantmentMenuBkgOrigAnchoredPos;
    public static Vector2 ContainerOrigAnchoredPos;

    public static void UpdateInventorySize() => UpdateInventorySize(null);

    internal static void UpdateInventorySize(int? previousSlotCount)
    {
        if (Player.m_localPlayer == null) return;
        ValheimPlusCompat.SyncRowsToConfig();
        Inventory inventory = Player.m_localPlayer.GetInventory();
        int width = inventory.GetWidth();
        int height = API.GetFullHeight(width);
        int previousRows = inventory.GetHeight() - (AddEquipmentRow.Value.isOn()
            ? Mathf.CeilToInt((float)(previousSlotCount ?? slots.Count) / width) : 0);
        ResizeInventory(inventory, previousRows, height);
        Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height = height;

        Player.m_localPlayer.m_inventory.Changed();
        InventoryHealth.InventoryFix();
    }

    internal static void ResizeInventory(Inventory inventory, int previousNormalRows, int height)
    {
        int reservedRows = AddEquipmentRow.Value.isOn() ? API.GetAddedRows(inventory.GetWidth()) : 0;
        int normalRows = height - reservedRows;
        int delta = normalRows - previousNormalRows;
        List<ItemDrop.ItemData> displaced = [];
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item.m_gridPos.y >= previousNormalRows && reservedRows > 0)
                item.m_gridPos.y += delta;
            else if (item.m_gridPos.y >= normalRows)
                displaced.Add(item);
        }
        inventory.SetHeight(height);
        foreach (ItemDrop.ItemData item in displaced)
            inventory.TryAddItemToInventory(item);
    }

    public static int VisiblePlayerRows(Inventory inv)
    {
        return AddEquipmentRow.Value.isOn() && DisplayEquipmentRowSeparate.Value.isOn() ? NormalRows(inv) : inv.GetHeight();
    }

    public static void UpdateContainerPosition()
	{
		return; // Not sure if I need this shit anymore. My other work arounds make it almost better.
        InventoryGui? instance = InventoryGui.instance;
        if (!instance || !instance.m_container) return;
        Player? player = Player.m_localPlayer;
        if (!player) return;
        // vanilla only grows m_player when rows are added, so the container has to slide down by the same amount
        float rowHeight = instance.m_invGridHeight > 0f ? instance.m_invGridHeight : tileSize;
        float shift = (VisiblePlayerRows(player.GetInventory()) - BaseInventoryHeight) * rowHeight;
        instance.m_container.anchoredPosition = ContainerOrigAnchoredPos - new Vector2(0f, shift);
    }

    public static int NormalRows(Inventory inv)
    {
        int height = inv.GetHeight();
        if (AddEquipmentRow.Value.isOff()) return height;
        int addedRows = API.GetAddedRows(inv.GetWidth());
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
            return new(1.13f + Math.Max(QuickSlotsAmount.Value, (slots.Count - 1) / NewLayoutQuickslotsFirstRow) * tileSize / 570, 1f);
        }

        int totalSlots = slots.Count;
        int regularSlotCount = totalSlots - QuickSlotsAmount.Value;

        int regularColumns = (regularSlotCount + OldLayoutRegularSlotsPerColumn - 1) / OldLayoutRegularSlotsPerColumn;

        float totalColumns;

        int quickslotColumns = (QuickSlotsAmount.Value + OldLayoutQuickslotsPerColumn - 1) / OldLayoutQuickslotsPerColumn;
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
        RectTransform? enchantmentMenu = VESCompat.IsVesInstalled ? instance.m_crafting.Find("enchantment_menu").GetComponent<RectTransform>() : null;
        RectTransform? enchantmentMenuBkg = VESCompat.IsVesInstalled ? instance.m_crafting.Find("RepairSimple(Clone)").GetComponent<RectTransform>() : null;
        Image? craftingBkg = instance.m_crafting.Find("Bkg").GetComponent<Image>();

        if (OldLayout.Value.isOn())
        {
            selectedFrame.anchorMin = SelectedFrameOrigAnchMin;
            repairSimple.anchoredPosition = RepairSimpleOrigAnchoredPos;
            repairButton.anchoredPosition = RepairButtonOrigAnchoredPos;
            if(enchantmentMenu)enchantmentMenu.anchoredPosition = EnchantmentMenuOrigAnchoredPos;
            if(enchantmentMenuBkg)enchantmentMenuBkg.anchoredPosition = EnchantmentMenuBkgOrigAnchoredPos;
            craftingBkg.enabled = true;
            if (!GlgGo) return;
            if (InventoryGui.instance)
                GlgGo.WithParent(OldLayout.Value.isOff() ? InventoryGui.instance.m_crafting.transform : InventoryGui.instance.m_player.transform, false);
            GlgRt.anchoredPosition = ToggleButtonsGlgAnchoredPosOldVert;
            GUICache.ButtonGridLayoutGroup.constraintCount = QuickSlotsAmount.Value < 1 && slots.Count < 10  ? 2 : 3;
        }
        else
        {
            selectedFrame.anchorMin = PlayerBkgAnchorMin;
            repairSimple.anchoredPosition = RepairSimpleOrigAnchoredPos + RepairMovement;
            repairButton.anchoredPosition = RepairButtonOrigAnchoredPos + RepairMovement;
            if(enchantmentMenu)enchantmentMenu.anchoredPosition = EnchantmentMenuOrigAnchoredPos + RepairMovement;
            if(enchantmentMenuBkg)enchantmentMenuBkg.anchoredPosition = EnchantmentMenuBkgOrigAnchoredPos + RepairMovement;
            craftingBkg.enabled = false;
            if (!GlgGo) return;
            if (InventoryGui.instance)
                GlgGo.WithParent(OldLayout.Value.isOff() ? InventoryGui.instance.m_crafting.transform : InventoryGui.instance.m_player.transform, false);
            GlgRt.anchoredPosition = ToggleButtonsGlgAnchoredPos;
        }
    }

    public static void FixPlayerPreview()
    {
        if (!PreviewParent) return;
        PreviewAnchorMin = QuickSlotsAmount.Value > 4 ? new Vector2(0f, 0.24f) : QuickSlotsAmount.Value != 0 ? new Vector2(0f, 0.14f) : Vector2.zero;
        RectTransform previewParentRT = (RectTransform)PreviewParent.transform;
        previewParentRT.anchorMin = PreviewAnchorMin;
    }

    private static ItemDrop.ItemData?[] _projectedItems = [];
    private static readonly Dictionary<ItemDrop.ItemData, Vector2i> _plannedMoves = new();
    private static readonly HashSet<Vector2i> _targetPositions = [];

    public static void ProjectEquippedIntoGridTail(Player player, InventoryGrid playerGrid)
    {
        Inventory playerInventory = player.GetInventory();
        int inventoryWidth = playerInventory.GetWidth();
        int inventoryHeight = playerInventory.GetHeight();

        int reservedTailRows = API.GetAddedRows(inventoryWidth);
        int firstTailRow = inventoryHeight - reservedTailRows;

        List<Model.Slot?> allSlots = slots;

        int equipmentTailStartIndex = GetBaseSlotIndex(playerInventory);
        if (_projectedItems.Length != allSlots.Count)
            _projectedItems = new ItemDrop.ItemData?[allSlots.Count];
        else
            Array.Clear(_projectedItems, 0, _projectedItems.Length);
        ItemDrop.ItemData?[] projectedEquippedItemsBySlot = _projectedItems;

        _plannedMoves.Clear();
        _targetPositions.Clear();
        Dictionary<ItemDrop.ItemData, Vector2i> plannedMoves = _plannedMoves;
        HashSet<Vector2i> targetPositions = _targetPositions;

        for (int i = 0; i < allSlots.Count; ++i)
        {
            Model.Slot? slot = allSlots[i];
            if (slot is not Model.EquipmentSlot equipmentSlot) continue;

            int linear = equipmentTailStartIndex + i;
            Vector2i destPos = new(linear % inventoryWidth, linear / inventoryWidth);

            if (equipmentSlot.Get?.Invoke(player) is { } equippedItem)
            {
                projectedEquippedItemsBySlot[i] = equippedItem;
                if (equippedItem.m_gridPos != destPos)
                {
                    plannedMoves[equippedItem] = destPos;
                    targetPositions.Add(destPos);
                }
            }

        }

        foreach (KeyValuePair<ItemDrop.ItemData, Vector2i> kvp in plannedMoves)
        {
            ItemDrop.ItemData equippedItem = kvp.Key;
            Vector2i destPos = kvp.Value;
            Vector2i srcPos = equippedItem.m_gridPos;

            ItemDrop.ItemData? occupant = playerInventory.GetItemAt(destPos.x, destPos.y);

            if (occupant != null && occupant != equippedItem)
            {
                // Don't move the occupant if it's also being moved to a different equipment slot
                if (plannedMoves.ContainsKey(occupant))
                {
                    continue;
                }

                bool srcInNormalRegion = srcPos.y < firstTailRow;

                if (srcInNormalRegion)
                {
                    occupant.m_gridPos = srcPos;
                }
                else
                {
                    ItemDrop.ItemData? itemAtSrc = playerInventory.GetItemAt(srcPos.x, srcPos.y);

                    bool srcIsFree = itemAtSrc == null || itemAtSrc == occupant || itemAtSrc == equippedItem;
                    bool srcItemMoving = itemAtSrc != null && plannedMoves.ContainsKey(itemAtSrc);

                    if (srcIsFree || srcItemMoving)
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

        equipItems = projectedEquippedItemsBySlot;

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
        if (GUICache._enchantmentMenuButtonRT) GUICache._enchantmentMenuButtonRT.anchoredPosition += RepairMovement;
        if (GUICache._enchantmentMenuBkgButtonRT) GUICache._enchantmentMenuBkgButtonRT.anchoredPosition += RepairMovement;
    }
}
