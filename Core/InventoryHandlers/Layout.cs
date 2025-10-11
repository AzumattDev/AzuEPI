using AzuEPI.Game.Loadout;
using AzuEPI.Vanity;
using AzuExtendedPlayerInventory;

namespace AzuEPI.Core.InventoryHandlers;

public class Layout
{
    public const int BaseInventoryHeight = 4;
    public const int BaseInventoryWidth = 8;

    internal const float tileSize = 70f;

    internal static float equipOriginX = -430f;
    internal static float equipOriginY = -75f;
    internal static float columnGapTiles = 4f;

    internal static readonly Vector2 PlayerBkgAnchorMin = new(-0.80f, 0f);

    internal static readonly Vector2 RepairMovement = new Vector2(-460f, 0f);

    internal static readonly Vector2 PreviewAnchorMin = new(0f, 0.14f);
    internal static readonly Vector2 PreviewAnchorMax = new(1f, 0.885f);
    internal static readonly Vector2 PreviewSizeDelta = new(-300f, 0f);
    internal static readonly Vector2 PreviewAnchoredPos = new(-507f, 0f);
    internal static readonly Vector2 PlayerPreviewImageSize = new(500f, 630f);

    internal static readonly Vector2 DropAllSize = new(100f, 30f);
    internal static readonly Vector2 DropAllAnchorMin = new(0.0f, 1.0f);
    internal static readonly Vector2 DropAllAnchorMax = new(0.0f, 1.0f);
    internal static readonly Vector2 DropAllPivot = new(0.0f, 1.0f);

    public static int NormalRows(Inventory inv)
    {
        int width = inv.GetWidth();
        int height = inv.GetHeight();
        int addedRows = API.GetAddedRows(width);
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

    public static void BuildToggleButtonHlg(InventoryGui invGui)
    {
        var hlgGo = new GameObject("AzuEPI_ToggleButtonsHlg", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var hlgRt = (RectTransform)hlgGo.transform;
        hlgRt.SetParent(invGui.m_crafting.transform, false);
        hlgRt.anchorMin = new Vector2(0f, 1f);
        hlgRt.anchorMax = new Vector2(0f, 1f);
        hlgRt.pivot = new Vector2(0.5f, 1f);
        hlgRt.anchoredPosition = new Vector2(-222.5f, -30f);
        hlgRt.sizeDelta = new Vector2(270f, 32f);
        var hlg = hlgGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 35f;

        VanityPanelController.ToggleButtonParentHlg = hlgRt;
        PersonalLoadoutGui.ToggleButtonParentHlg = hlgRt;
    }
}