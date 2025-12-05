using AzuEPI.Core.Slots;
using AzuEPI.EPI;
using AzuEPI.Game.Patches;
using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Core.InventoryHandlers;

public class Layout
{
    public const int BaseInventoryHeight = 4;
    public const int BaseInventoryWidth = 8;

    internal const float tileSize = 70f;

    internal static float equipOriginX = -430f;
    internal static float equipOriginY = -75f;
    internal static float columnGapTiles = 4f;

    internal static Transform AzuPlayerBkg = null!;
    internal static readonly Vector2 PlayerBkgAnchorMin = new(-0.80f, 0f);

    internal static readonly Vector2 RepairMovement = new Vector2(-460f, 0f);

    internal static readonly Vector2 PreviewAnchorMin = new(0f, 0.14f);
    internal static readonly Vector2 PreviewAnchorMax = new(1f, 0.885f);
    internal static readonly Vector2 PreviewSizeDelta = new(-300f, 0f);
    internal static readonly Vector2 PreviewAnchoredPos = new(-507f, 0f);
    internal static readonly Vector2 PlayerPreviewImageSize = new(500f, 630f);
    internal static readonly Vector2 ToggleButtonsHlgAnchoredPos = new(-222.5f, -30f);
    internal static readonly Vector2 ToggleButtonsHlgAnchoredPosOld = new(-552.5f, -145f);

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
        return new(1.13f + Math.Max(Hotkeys.Length, (InventoryGuiPatches.UpdateInventory_Patch.slots.Count - 1) / 3) * tileSize / 570, 1f);
    }

    public static void ApplyLayoutCorrections()
    {
        if (!InventoryGui.instance) return;
        var instance = InventoryGui.instance;
        var selectedFrame = instance.m_crafting.Find("selected_frame").GetComponent<RectTransform>();
        var repairSimple = instance.m_crafting.Find("RepairSimple").GetComponent<RectTransform>();
        var repairButton = instance.m_crafting.Find("RepairButton").GetComponent<RectTransform>();
        var craftingBkg = instance.m_crafting.Find("Bkg").GetComponent<Image>();

        if (OldLayout.Value.isOn())
        {
            selectedFrame.anchorMin = SelectedFrameOrigAnchMin;
            repairSimple.anchoredPosition = RepairSimpleOrigAnchoredPos;
            repairButton.anchoredPosition = RepairButtonOrigAnchoredPos;
            if (!craftingBkg.isActiveAndEnabled) craftingBkg.enabled = true;
            if (HlgGo) HlgRt.anchoredPosition = ToggleButtonsHlgAnchoredPosOld;
        }
        else
        {
            selectedFrame.anchorMin = PlayerBkgAnchorMin;
            repairSimple.anchoredPosition += RepairMovement;
            repairButton.anchoredPosition += RepairMovement;
            if (craftingBkg.isActiveAndEnabled) craftingBkg.enabled = false;
            if (HlgGo) HlgRt.anchoredPosition = ToggleButtonsHlgAnchoredPos;
        }
    }

    public static void ProjectEquippedIntoGridTail(Player player, InventoryGrid playerGrid)
    {
        Inventory playerInventory = player.GetInventory();
        int inventoryWidth = playerInventory.GetWidth();
        int inventoryHeight = playerInventory.GetHeight();
        int reservedTailRows = API.GetAddedRows(inventoryWidth);
        List<ItemDrop.ItemData> inventoryItems = playerInventory.GetAllItems();
        List<Model.Slot?> allSlots = InventoryGuiPatches.UpdateInventory_Patch.slots;

        int equipmentTailStartIndex = GetBaseSlotIndex(playerInventory);
        ItemDrop.ItemData?[] projectedEquippedItemsBySlot = new ItemDrop.ItemData[allSlots.Count];

        for (int i = 0; i < allSlots.Count; ++i)
        {
            Model.Slot? slot = allSlots[i];
            if (slot is not Model.EquipmentSlot equipmentSlot) continue;
            if (equipmentSlot.Get?.Invoke(player) is { } equippedItem)
            {
                equippedItem.m_gridPos = new Vector2i(equipmentTailStartIndex % inventoryWidth, equipmentTailStartIndex / inventoryWidth);
                projectedEquippedItemsBySlot[i] = equippedItem;
            }

            ++equipmentTailStartIndex;
        }

        for (int itemIndex = 0; itemIndex < inventoryItems.Count; ++itemIndex)
        {
            ItemDrop.ItemData inventoryItem = inventoryItems[itemIndex];

            if (!playerInventory.IsAtEquipmentSlot(inventoryItem, out int equipmentSlotIndex)) continue;

            bool slotIndexInvalid = equipmentSlotIndex <= -1;
            bool isSameAsProjected = !slotIndexInvalid && inventoryItem == projectedEquippedItemsBySlot[equipmentSlotIndex];
            bool targetIsEquipmentSlot = !slotIndexInvalid && allSlots[equipmentSlotIndex] is Model.EquipmentSlot;

            Model.EquipmentSlot? epiEquipmentSlot = targetIsEquipmentSlot ? (Model.EquipmentSlot)allSlots[equipmentSlotIndex]! : null;

            bool invalidBySlotRules = epiEquipmentSlot?.Valid != null && !epiEquipmentSlot.Valid(inventoryItem);
            bool alreadyEquippedHere = !slotIndexInvalid && ExtendedPlayerInventory.equipItems[equipmentSlotIndex] == inventoryItem;
            bool slotOccupied = epiEquipmentSlot is { Occupied: true };

            bool isBroken = inventoryItem.m_shared.m_useDurability && inventoryItem.m_durability <= 0f;
            if (isBroken && alreadyEquippedHere) continue;

            if (!isSameAsProjected && (slotIndexInvalid || !targetIsEquipmentSlot || invalidBySlotRules || alreadyEquippedHere || slotOccupied))
            {
                Vector2i firstFreeSlot = playerInventory.FindEmptySlot(true);
                bool noFreeSlot = firstFreeSlot.x < 0 || firstFreeSlot.y < 0;
                bool freeSlotIntrudesIntoReservedTail = !noFreeSlot && firstFreeSlot.y >= (inventoryHeight - reservedTailRows);

                AzuExtendedPlayerInventoryLogger.LogError("Item " + inventoryItem.m_dropPrefab.name + " moved or dropped");
                AzuExtendedPlayerInventoryLogger.LogError("Reasons: ");
                AzuExtendedPlayerInventoryLogger.LogError("slotIndexInvalid: " + slotIndexInvalid);
                AzuExtendedPlayerInventoryLogger.LogError("isSameAsProjected: " + isSameAsProjected);
                AzuExtendedPlayerInventoryLogger.LogError("targetIsEquipmentSlot: " + targetIsEquipmentSlot);
                AzuExtendedPlayerInventoryLogger.LogError("invalidBySlotRules: " + invalidBySlotRules);
                AzuExtendedPlayerInventoryLogger.LogError("alreadyEquippedHere: " + alreadyEquippedHere);
                AzuExtendedPlayerInventoryLogger.LogError("slotOccupied: " + slotOccupied);
                AzuExtendedPlayerInventoryLogger.LogError("isBroken: " + isBroken);

                if (noFreeSlot || freeSlotIntrudesIntoReservedTail)
                {
                    // it will drop them simply because it cannot be added to the inventory and it's "outside" the normal inventory when it breaks.
                    if (inventoryItem.m_durability > 0 && !playerInventory.CanAddItem(inventoryItem))
                        player.DropItem(playerInventory, inventoryItem, inventoryItem.m_stack);
                }
                else
                {
                    inventoryItem.m_gridPos = firstFreeSlot;
                    playerGrid.UpdateInventory(playerInventory, player, null);
                }
            }
        }

        ExtendedPlayerInventory.equipItems = projectedEquippedItemsBySlot;

        if (AzuEPICharacterPanel.playerPreviewComp && Player.m_localPlayer)
            VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ApplyRepairShift()
    {
        if (GUICache._selectedFrameRT) GUICache._selectedFrameRT.anchorMin = PlayerBkgAnchorMin;
        if (GUICache._repairSimpleRT) GUICache._repairSimpleRT.anchoredPosition += RepairMovement;
        if (GUICache._repairButtonRT) GUICache._repairButtonRT.anchoredPosition += RepairMovement;
    }
}