namespace AzuEPI.Core.InventoryHandlers;

public class InventoryHealth
{
    public static void InventoryFix()
    {
        if (Player.m_localPlayer == null)
            return;
        Inventory? playerInventory = Player.m_localPlayer.GetInventory();
        HashSet<Vector2i> curPositions = [];
        List<ItemDrop.ItemData> itemsToFix = [];
        if (playerInventory == null) return;
        int normalRows = Layout.BaseInventoryHeight + ExtraRows.Value;
        if (playerInventory?.m_inventory != null)
            for (int index = 0; index < playerInventory.m_inventory.Count; ++index)
            {
                ItemDrop.ItemData? itemData = playerInventory.m_inventory[index];
                bool overlappingItem = curPositions.Contains(itemData.m_gridPos);

                // Don't apply a y-upper-bound check there — the height is dynamic and may be
                bool inEpiArea = itemData.m_gridPos.y >= normalRows;
                bool outOfBounds = itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= playerInventory.m_width
                    || (!inEpiArea && (itemData.m_gridPos.y < 0 || itemData.m_gridPos.y >= normalRows));

                if (overlappingItem || outOfBounds || itemData.m_stack < 1)
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
    
    public static void FixHiddenItems()
    {
        if (Player.m_localPlayer == null) return;

        Inventory? inventory = Player.m_localPlayer.GetInventory();
        if (inventory == null) return;

        int width = inventory.GetWidth();
        int normalRows = Layout.NormalRows(inventory);
        bool hasExtendedSlots = AddEquipmentRow.Value.isOn() && slots.Count > 0;

        // 3. Items in equipment slots that don't validate for those slots
        List<ItemDrop.ItemData> stuck = [];
        foreach (ItemDrop.ItemData? it in inventory.GetAllItems())
        {
            bool isInExtendedArea = it.m_gridPos.y >= normalRows;

            if (!isInExtendedArea) continue;
            if (!hasExtendedSlots || inventory.IsHiddenCell(it.m_gridPos.x, it.m_gridPos.y) || API.TryGetSlotIndexAtGridPos(inventory, it.m_gridPos, out int slotIndex) && !API.SlotValidates(slotIndex, it))
            {
                stuck.Add(it);
            }
        }

        if (stuck.Count == 0) return;

        AzuExtendedPlayerInventoryLogger.LogWarning($"Found {stuck.Count} items in hidden/orphaned cells after slot configuration change. Relocating...");

        foreach (ItemDrop.ItemData it in stuck)
        {
            try
            {
                inventory.TryAddItemToInventory(it);
            }
            catch (Exception ex)
            {
                AzuExtendedPlayerInventoryLogger.LogError($"Error relocating item {it.m_shared?.m_name}: {ex}");
            }
        }

        inventory.Changed();
    }

    public static bool IgnoreKeyPresses(bool extra = false)
    {
        if (!extra)
            return !ZNetScene.instance || !Player.m_localPlayer || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true;
        return !ZNetScene.instance || !Player.m_localPlayer || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true
               || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
    }
}