using AzuEPI.Game.Compatibility.AdvBackpacks;

namespace AzuEPI.Core.InventoryHandlers;

public static class InventoryExtensions
{
    extension(Inventory inv)
    {
        public bool IsPlayerInventory()
        {
            return inv != null && Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory();
        }

        public List<ItemDrop.ItemData> GetEquippedItemsFiltered()
        {
            List<ItemDrop.ItemData> equippedItems = [];
            foreach (ItemDrop.ItemData itemData in inv.m_inventory)
            {
                if (itemData.m_equipped &&
                    itemData.m_shared.m_teleportable
                    && itemData.m_dropPrefab && (
                        !AdvBackpacksCompat.Backpacks.Contains(itemData.m_dropPrefab.name)
                        && !RustyBagsCompat.Backpacks.Contains(itemData.m_dropPrefab.name)
                        && itemData.m_dropPrefab.name != "bp_explorer"
                        && itemData.m_dropPrefab.name != "JC_Gem_Bag")
                   )
                {
                    equippedItems.Add(itemData);
                }
            }

            return equippedItems;
        }

        public void TryAddItemToInventory(ItemDrop.ItemData itemData)
        {
            Vector2i newPos = inv.FindEmptyQuickAware(itemData, true);
            if (newPos.x >= 0 && newPos.y >= 0)
            {
                if (!inv.m_inventory.Contains(itemData))
                {
                    AzuExtendedPlayerInventoryLogger.LogWarning($"TryAddItemToInventory: {Localization.instance?.Localize(itemData.m_shared.m_name) ?? itemData.m_shared.m_name} is not in this inventory, skipping relocation.");
                    return;
                }

                int stack = itemData.m_stack;
                inv.RemoveItem(itemData);
                if (!inv.AddItem(itemData, stack, newPos.x, newPos.y))
                {
                    AzuExtendedPlayerInventoryLogger.LogWarning($"Failed to relocate {Localization.instance?.Localize(itemData.m_shared.m_name) ?? itemData.m_shared.m_name} to ({newPos.x},{newPos.y}), dropping item.");
                    Player.m_localPlayer.DropItem(inv, itemData, stack);
                }
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"No empty slot for {Localization.instance?.Localize(itemData.m_shared.m_name) ?? itemData.m_shared.m_name}, dropping item.");
                Player.m_localPlayer.DropItem(inv, itemData, itemData.m_stack);
            }
        }

        internal Vector2i EpiIndexToGridPos(int slotIndex)
        {
            int width = inv.GetWidth();
            int normalRows = Layout.NormalRows(inv); // vanilla rows
            int x = slotIndex % width;
            int y = normalRows + slotIndex / width;
            return new Vector2i(x, y);
        }

        internal bool IsEquipmentSlotFreeAndItemValid(ItemDrop.ItemData item, out int which)
        {
            which = -1;

            if (slots == null || slots.Count == 0)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug("IsEquipmentSlotFreeAndItemValid: Slots not initialized yet");
                return false;
            }

            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Checking item '{item.m_shared.m_name}' (Type: {item.m_shared.m_itemType})");

            // Prioritize API-added slots over built-in slots to avoid placing items in generic slots when they have dedicated slots
            which = slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: true } slot && slot.Valid(item) && !slot.Occupied);

            if (which >= 0)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Found API-added slot {which} ({slots[which]?.Name})");
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: No free API-added slot found, checking built-in slots");

                for (int i = 0; i < slots.Count; i++)
                {
                    Model.Slot? s = slots[i];
                    if (s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: true } slot)
                    {
                        bool validates = slot.Valid(item);
                        bool occupied = slot.Occupied;
                        AzuExtendedPlayerInventoryLogger.LogDebugDebug($"  API Slot {i} ({s.Name}): Validates={validates}, Occupied={occupied}");
                    }
                }
            }

            if (which < 0)
            {
                which = slots.FindIndex(s => s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: false } slot && slot.Valid(item) && !slot.Occupied);

                if (which >= 0)
                {
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Found built-in slot {which} ({slots[which]?.Name})");
                }
                else
                {
                    AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: No free built-in slot found either");

                    for (int i = 0; i < slots.Count; i++)
                    {
                        Model.Slot? s = slots[i];
                        if (s is Model.EquipmentSlot { Valid: not null, IsAPIAdded: false } slot)
                        {
                            bool validates = slot.Valid(item);
                            bool occupied = slot.Occupied;
                            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"  Built-in Slot {i} ({s.Name}): Validates={validates}, Occupied={occupied}");
                        }
                    }
                }
            }

            if (which < 0)
            {
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: No valid free slot found for '{item.m_shared.m_name}'");
                return false;
            }

            Vector2i pos = inv.EpiIndexToGridPos(which);

            if (pos.x < 0 || pos.x >= inv.GetWidth() || pos.y < 0 || pos.y >= inv.GetHeight())
            {
                AzuExtendedPlayerInventoryLogger.LogWarningDebug($"Calculated equipment slot position ({pos.x}, {pos.y}) is out of inventory bounds ({inv.GetWidth()}x{inv.GetHeight()}). Skipping auto-equip.");
                which = -1;
                return false;
            }

            bool isEmpty = inv.GetItemAt(pos.x, pos.y) == null;
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"IsEquipmentSlotFreeAndItemValid: Slot {which} at ({pos.x}, {pos.y}) is {(isEmpty ? "empty" : "occupied")}");

            return isEmpty;
        }

        internal bool IsEquipmentSlotFree(out int which)
        {
            which = slots.FindIndex(s => s is Model.EquipmentSlot { IsQuickSlot: false, EquipmentSlot: not null, Occupied: false });

            if (which < 0)
                return false;

            Vector2i pos = inv.EpiIndexToGridPos(which);
            return inv.GetItemAt(pos.x, pos.y) == null;
        }

        internal bool IsQuickSlotFree(out int which)
        {
            which = slots.FindIndex(s => s is { IsQuickSlot: true, EquipmentSlot: null, Occupied: false });

            if (which < 0)
                return false;

            Vector2i pos = inv.EpiIndexToGridPos(which);
            return inv.GetItemAt(pos.x, pos.y) == null;
        }

        internal bool IsAtEquipmentSlot(ItemDrop.ItemData item, out int which)
        {
            int normalRows = Layout.NormalRows(inv);
            if (AddEquipmentRow.Value.isOff() || item.m_gridPos.y < normalRows || (item.m_gridPos.y - normalRows) * inv.GetWidth() + item.m_gridPos.x >= slots.Count - QuickSlotsAmount.Value)
            {
                which = -1;
                return false;
            }

            which = (item.m_gridPos.y - normalRows) * inv.GetWidth() + item.m_gridPos.x;
            return true;
        }

        internal bool IsAtQuickSlot(ItemDrop.ItemData item, out int which)
        {
            int normalRows = Layout.NormalRows(inv);
            if (AddEquipmentRow.Value.isOff() || item.m_gridPos.y < normalRows || (item.m_gridPos.y - normalRows) * inv.GetWidth() + item.m_gridPos.x < slots.Count - QuickSlotsAmount.Value)
            {
                which = -1;
                return false;
            }

            which = (item.m_gridPos.y - normalRows) * inv.GetWidth() + item.m_gridPos.x;
            return true;
        }

        internal bool IsHiddenCell(int x, int y)
        {
            int li = inv.LinearIndexIntoEpiBlock(x, y);
            return li >= 0 && li >= slots.Count;
        }

        private int LinearIndexIntoEpiBlock(int x, int y)
        {
            int width = inv.GetWidth();
            int normalRows = Layout.NormalRows(inv); // vanilla rows only
            if (y < normalRows) return -1;

            int baseLinear = normalRows * width;
            int linear = y * width + x;
            return linear - baseLinear;
        }

        internal IEnumerable<Vector2i> EnumerateQuickCells()
        {
            int width = inv.GetWidth();
            int normalRows = Layout.NormalRows(inv);

            int firstLinear = normalRows * width;
            int total = slots.Count;

            int quickCount = QuickSlotsAmount.Value;
            int quickStart = total - quickCount;
            for (int i = quickStart; i < total; ++i)
            {
                int li = firstLinear + i;
                yield return new Vector2i(li % width, li / width);
            }
        }

        internal IEnumerable<Vector2i> EnumerateEquipmentCells()
        {
            int width = inv.GetWidth();
            int normalRows = Layout.NormalRows(inv);

            int firstLinear = normalRows * width;
            int total = slots.Count;
            int quickCount = QuickSlotsAmount.Value;
            int equipmentCount = total - quickCount;

            for (int i = 0; i < equipmentCount; ++i)
            {
                int li = firstLinear + i;
                yield return new Vector2i(li % width, li / width);
            }
        }

        internal bool TryFindEmptyQuickCell(out Vector2i pos)
        {
            foreach (Vector2i p in EnumerateQuickCells(inv))
            {
                if (inv.GetItemAt(p.x, p.y) != null) continue;
                pos = p;
                return true;
            }

            pos = new Vector2i(-1, -1);
            return false;
        }

        internal bool ShouldProtectInventorySlots()
        {
            return Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory() && AddEquipmentRow.Value.isOn();
        }

        public Vector2i FindEmptyQuickAware(bool topFirst)
        {
            int width = inv.GetWidth();
            int normalRows = Layout.NormalRows(inv);

            if (topFirst)
            {
                for (int y = 0; y < normalRows; ++y)
                for (int x = 0; x < width; ++x)
                    if (inv.GetItemAt(x, y) == null)
                        return new Vector2i(x, y);
            }
            else
            {
                for (int y = normalRows - 1; y >= 0; --y)
                for (int x = 0; x < width; ++x)
                    if (inv.GetItemAt(x, y) == null)
                        return new Vector2i(x, y);
            }

            if (inv.TryFindEmptyQuickCell(out Vector2i q))
                return q;

            return new Vector2i(-1, -1);
        }

        public Vector2i FindEmptyQuickAware(ItemDrop.ItemData item, bool topFirst)
        {
            int width = inv.GetWidth();
            int normalRows = Layout.NormalRows(inv);

            if (topFirst)
            {
                for (int y = 0; y < normalRows; ++y)
                for (int x = 0; x < width; ++x)
                    if (inv.GetItemAt(x, y) == null)
                        return new Vector2i(x, y);
            }
            else
            {
                for (int y = normalRows - 1; y >= 0; --y)
                for (int x = 0; x < width; ++x)
                    if (inv.GetItemAt(x, y) == null)
                        return new Vector2i(x, y);
            }

            if (inv.TryFindEmptyQuickCell(out Vector2i q))
                return q;

            int height = inv.GetHeight();
            for (int y = normalRows; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                if (inv.GetItemAt(x, y) != null) continue;

                Vector2i pos = new(x, y);
                if (API.TryGetSlotIndexAtGridPos(inv, pos, out int slotIndex) && API.SlotValidates(slotIndex, item))
                {
                    return pos;
                }
            }

            return new Vector2i(-1, -1);
        }
    }
}
