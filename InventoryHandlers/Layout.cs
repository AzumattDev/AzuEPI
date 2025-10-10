using AzuExtendedPlayerInventory;

namespace AzuEPI.InventoryHandlers;

public class Layout
{
    internal const float tileSize = 70f;
    //internal static float leftOffset = 693f;

    internal static float equipOriginX = -430f;

    internal static float equipOriginY = -75f;

    internal static float columnGapTiles = 4f;

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
}