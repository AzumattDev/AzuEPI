using AzuEPI.Game.Loadout;
using AzuEPI.Game.Patches;
using AzuEPI.Vanity;

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
        int addedRows = API.API.GetAddedRows(width);
        return height - addedRows;
    }

    public static int BaseIndex(Inventory inv)
    {
        return inv.GetWidth() * NormalRows(inv);
    }

    public static Vector2i ClampToVisible(Inventory inv, Vector2i p)
    {
        return new(Mathf.Clamp(p.x, 0, inv.GetWidth() - 1), Mathf.Clamp(p.y, 0, inv.GetHeight() - 1));
    }

    public static Vector2 GetEquipmentBackAnchorMax()
    {
        return new(1.13f + Math.Max(Hotkeys.Length, (InventoryGuiPatches.UpdateInventory_Patch.slots.Count - 1) / 3) * Layout.tileSize / 570, 1f);
    }

    public static void FixLayout()
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
}