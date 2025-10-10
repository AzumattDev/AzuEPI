using AzuEPI.EPI;
using AzuExtendedPlayerInventory;

namespace AzuEPI.InventoryHandlers;

public class Placement
{
    internal static bool ShouldGuard(Inventory inv)
    {
        return Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory() && AddEquipmentRow.Value.isOn();
    }
    
    public static Vector2i FindEmptyQuickAware(Inventory inv, bool topFirst)
    {
        int width = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv);

        if (topFirst)
        {
            for (int y = 0; y < normalRows; ++y)
            for (int x = 0; x < width; ++x)
                if (inv.GetItemAt(x, y) == null) return new Vector2i(x, y);
        }
        else
        {
            for (int y = normalRows - 1; y >= 0; --y)
            for (int x = 0; x < width; ++x)
                if (inv.GetItemAt(x, y) == null) return new Vector2i(x, y);
        }

        return inv.TryFindEmptyQuickCell(out var q) ? q : new Vector2i(-1, -1);
    }
}