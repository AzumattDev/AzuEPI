namespace AzuEPI.Core.Slots;

internal static class EpiDropRouter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ValidatePlannedDrop(InventoryGrid grid, Inventory fromInventory, ItemDrop.ItemData dragged, int draggedAmount, Vector2i destPos)
    {
        try
        {
            if (!Player.m_localPlayer || dragged == null || fromInventory == null || !grid)
                return true;

            Player player = Player.m_localPlayer;
            Inventory playerInv = player.GetInventory();
            if (playerInv == null)
                return true;

            bool isFromPlayer = ReferenceEquals(playerInv, fromInventory);
            bool isToPlayer = ReferenceEquals(playerInv, grid.m_inventory);

            if (isFromPlayer && !isToPlayer)
            {
                bool sourceIsSlot = TryResolveSlot(playerInv, dragged.m_gridPos, out int srcSlotIndex, out SlotDescriptor srcSlot);
                if (!sourceIsSlot)
                    return true;

                ItemDrop.ItemData destItem = grid.m_inventory.GetItemAt(destPos.x, destPos.y);
                if (destItem == null)
                    return true;

                // Check if vanilla would swap (not stack)
                if (!WillVanillaSwap(dragged, draggedAmount, destItem))
                    return true;

                if (API.SlotValidates(srcSlotIndex, destItem)) return true;
                AzuExtendedPlayerInventoryLogger.LogDebug($"Blocked drop to container: '{destItem.m_shared.m_name}' cannot swap back to EPI slot '{srcSlot.OriginalName}'.");
                return false;
            }

            if (!isFromPlayer || !isToPlayer)
                return true;

            if (dragged.m_gridPos == destPos)
                return true;

            bool sourceIsSlotP2P = TryResolveSlot(playerInv, dragged.m_gridPos, out int srcSlotIndexP2P, out SlotDescriptor srcSlotP2P);
            bool destIsSlot = TryResolveSlot(playerInv, destPos, out int dstSlotIndex, out SlotDescriptor dstSlot);

            if (!sourceIsSlotP2P && !destIsSlot)
                return true;

            ItemDrop.ItemData destItemP2P = playerInv.GetItemAt(destPos.x, destPos.y);

            if (destIsSlot && !API.SlotValidates(dstSlotIndex, dragged))
            {
                AzuExtendedPlayerInventoryLogger.LogDebug($"Blocked drop of '{dragged.m_shared.m_name}' into slot '{dstSlot.OriginalName}' (invalid for slot).");
                return false;
            }

            if (destItemP2P == null || !WillVanillaSwap(dragged, draggedAmount, destItemP2P))
                return true;

            if (destIsSlot && sourceIsSlotP2P)
            {
                if (API.SlotValidates(srcSlotIndexP2P, destItemP2P)) return true;
                AzuExtendedPlayerInventoryLogger.LogDebug($"Blocked swap: '{destItemP2P.m_shared.m_name}' in slot '{dstSlot.OriginalName}' cannot relocate to source slot '{srcSlotP2P.OriginalName}'.");
                return false;
            }

            if (destIsSlot || !sourceIsSlotP2P) return true;
            if (API.SlotValidates(srcSlotIndexP2P, destItemP2P)) return true;
            AzuExtendedPlayerInventoryLogger.LogDebug($"Blocked swap: '{destItemP2P.m_shared.m_name}' at {DescribeSlotOrPos(dstSlot, destPos, destIsSlot)} cannot swap into source slot '{srcSlotP2P.OriginalName}'.");
            return false;
        }
        catch (Exception e)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"EpiDropRouter.ValidatePlannedDrop: {e}");
            return true;
        }
    }

    private static bool TryResolveSlot(Inventory playerInv, Vector2i gridPos, out int slotIndex, out SlotDescriptor desc)
    {
        slotIndex = -1;
        desc = default;

        return API.TryGetSlotIndexAtGridPos(playerInv, gridPos, out slotIndex) && API.TryGetSlotDescriptor(slotIndex, out desc);
    }

    private static bool WillVanillaSwap(ItemDrop.ItemData dragged, int amount, ItemDrop.ItemData dest)
    {
        if (dest == null)
            return false;

        bool fullStack = amount <= 0 || dragged.m_stack == amount;

        if (!fullStack)
            return false;

        bool sameName = dest.m_shared.m_name == dragged.m_shared.m_name;
        bool sameQualityOrNotUpgradable = dragged.m_shared.m_maxQuality <= 1 || dest.m_quality == dragged.m_quality;
        bool destStackable = dest.m_shared.m_maxStackSize != 1;

        bool wouldMerge = sameName && sameQualityOrNotUpgradable && destStackable;

        return !wouldMerge;
    }

    private static string DescribeSlotOrPos(in SlotDescriptor desc, Vector2i pos, bool hasSlot)
    {
        if (!hasSlot)
            return $"grid({pos.x},{pos.y})";

        if (desc.IsEquipmentSlot)
            return $"equipment '{desc.OriginalName}'";

        return desc.IsQuickSlot ? $"quickslot '{desc.OriginalName}'" : $"{desc.OriginalName} @ ({pos.x},{pos.y})";
    }
}