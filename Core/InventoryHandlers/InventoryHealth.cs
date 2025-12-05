namespace AzuEPI.Core.InventoryHandlers;

public class InventoryHealth
{
    public static void InventoryFix()
    {
        if (Player.m_localPlayer == null)
            return;
        Inventory? playerInventory = Player.m_localPlayer.GetInventory();
        List<Vector2i> curPositions = new();
        List<ItemDrop.ItemData> itemsToFix = new();
        if (playerInventory == null) return;
        if (playerInventory?.m_inventory != null)
            for (int index = 0; index < playerInventory.m_inventory.Count; ++index)
            {
                ItemDrop.ItemData? itemData = playerInventory.m_inventory[index];
                bool overlappingItem = curPositions.Exists(pos => pos == itemData.m_gridPos);
                if (overlappingItem || itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= playerInventory.m_width || itemData.m_gridPos.y < 0 || itemData.m_gridPos.y >= playerInventory.m_height || itemData.m_stack < 1)
                {
                    if (itemData.m_stack < 1) playerInventory.RemoveItem(itemData);

                    AzuExtendedPlayerInventoryLogger.LogWarning(
                        overlappingItem
                            ? $"Item {Localization.instance.Localize(itemData.m_shared.m_name)} was overlapping another item in the player inventory grid, moving to first available slot or dropping if no slots are available."
                            : $"Item {Localization.instance.Localize(itemData.m_shared.m_name)} was outside player inventory grid, moving to first available slot or dropping if no slots are available.");
                    itemsToFix.Add(itemData);
                }

                curPositions.Add(itemData.m_gridPos);
            }
#if DEBUG
        AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning("Fixing player inventory.");
#endif
        foreach (ItemDrop.ItemData brokenItem in itemsToFix) playerInventory!.TryAddItemToInventory(brokenItem);
    }
    
    public static bool IgnoreKeyPresses(bool extra = false)
    {
        if (!extra)
            return !ZNetScene.instance || !Player.m_localPlayer || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true;
        return !ZNetScene.instance || !Player.m_localPlayer || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true
               || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
    }
}