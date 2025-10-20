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

    public static void ProjectEquippedIntoGridTail(Player player, InventoryGrid playerGrid, Animator animator)
    {
        Inventory inventory = player.GetInventory();
        int width = inventory.GetWidth();
        int height = inventory.GetHeight();
        int requiredRows = API.GetAddedRows(width);
        List<ItemDrop.ItemData> allItems = inventory.GetAllItems();
        List<Model.Slot?> slots = InventoryGuiPatches.UpdateInventory_Patch.slots;

        int slotBaseIndex = GetBaseSlotIndex(inventory);
        ItemDrop.ItemData?[] equippedItems = new ItemDrop.ItemData[slots.Count];
        for (int i = 0; i < slots.Count; ++i)
        {
            Model.Slot? slot = slots[i];
            if (slot is not Model.EquipmentSlot equipmentSlot) continue;
            if (equipmentSlot.Get?.Invoke(player) is { } item)
            {
                item.m_gridPos = new Vector2i(slotBaseIndex % width, slotBaseIndex / width);
                equippedItems[i] = item;
            }

            ++slotBaseIndex;
        }

        for (int index = 0; index < allItems.Count; ++index)
        {
            ItemDrop.ItemData t = allItems[index];

            if (inventory.IsAtEquipmentSlot(t, out int which) &&
                (which <= -1 || t != equippedItems[which]) &&
                (which <= -1 || slots[which] is not Model.EquipmentSlot slot
                             || (slot.Valid != null && !slot.Valid(t)) || ExtendedPlayerInventory.equipItems[which] == t
                             || (AutoEquip.Value.isOn() && !slot.IsQuickSlot && !player.EquipItem(t, false))))
            {
                Vector2i vector2I = inventory.FindEmptySlot(true);
                if (vector2I.x < 0 || vector2I.y < 0 || vector2I.y >= height - requiredRows)
                {
                    // it will drop them simply because it cannot be added to the inventory and it's "outside" the normal inventory when it breaks.
                    if (t.m_durability > 0 && !inventory.CanAddItem(t))
                        player.DropItem(inventory, t, t.m_stack);
                }
                else
                {
                    t.m_gridPos = vector2I;
                    playerGrid.UpdateInventory(inventory, player, null);
                }
            }
        }

        ExtendedPlayerInventory.equipItems = equippedItems;

        if (!animator.GetBool(GUICache.Visible)) return;
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