namespace AzuEPI.Game.Slots;

internal static class SlotBackupManager
{
    internal static readonly HashSet<string> _userConfigSlotNames = [];

    private class SlotBackup
    {
        public Model.EquipmentSlot Slot { get; set; } = null!;
        public int OriginalIndex { get; set; }
    }

    private static readonly Dictionary<string, SlotBackup> _slotBackups = new();

    internal static void InitializeBuiltInSlotBackups()
    {
        foreach (Model.Slot? slot in slots)
        {
            if (slot is Model.EquipmentSlot equipSlot && !equipSlot.IsQuickSlot)
            {
                BackupSlot(equipSlot);
            }
        }
    }

    public static void BackupSlot(Model.EquipmentSlot slot)
    {
        string backupKey = string.IsNullOrWhiteSpace(slot.OriginalName) ? slot.Name : slot.OriginalName;
        if (_slotBackups.ContainsKey(backupKey)) return;
        int currentIndex = slots.FindIndex(s => s == slot);

        _slotBackups[backupKey] = new SlotBackup
        {
            OriginalIndex = currentIndex,
            Slot = new Model.EquipmentSlot
            {
                Name = slot.Name,
                OriginalName = slot.OriginalName,
                Get = slot.Get,
                Valid = slot.Valid,
                IsAPIAdded = slot.IsAPIAdded,
                IsQuickSlot = slot.IsQuickSlot,
			},
		};
        AzuExtendedPlayerInventoryLogger.LogDebug($"Created backup for slot '{backupKey}' at index {currentIndex}");
    }

    public static bool RestoreSlotFromBackup(string slotKey)
    {
        if (!_slotBackups.TryGetValue(slotKey, out SlotBackup? backup))
        {
            AzuExtendedPlayerInventoryLogger.LogDebug($"No backup found for slot '{slotKey}'");
            return false;
        }

        Model.EquipmentSlot slotData = backup.Slot;

        if (API.TryGetSlotIndexByName(slotData.Name, out _, false) ||
            (!string.IsNullOrWhiteSpace(slotData.OriginalName) && API.TryGetSlotIndexByName(slotData.OriginalName, out _, false)))
        {
            AzuExtendedPlayerInventoryLogger.LogDebug($"Slot '{slotKey}' already exists, skipping restore");
            return false;
        }

        AzuExtendedPlayerInventoryLogger.LogInfo($"Restoring slot '{slotKey}' from backup to index {backup.OriginalIndex}");

        Model.EquipmentSlot restoredSlot = RecreateSlotWithDelegates(slotData);

        int quickSlotsStart = slots.Count - QuickSlotsAmount.Value;
        int targetIndex = Math.Max(0, Math.Min(backup.OriginalIndex, quickSlotsStart));

        API.UpdateSlots(targetIndex, 1);
        slots.Insert(targetIndex, restoredSlot);

        if (restoredSlot.IsAPIAdded)
        {
            API.CustomSlots.Add(restoredSlot);
        }

        SlotHelpers.ResizeSlots();
        SlotHelpers.UpdateEquipmentBackgroundAnchors();

        return true;
    }

    public static IEnumerable<string> GetBackupKeys() => _slotBackups.Keys.ToList();

    private static Model.EquipmentSlot RecreateSlotWithDelegates(Model.EquipmentSlot slotData)
    {
        return new Model.EquipmentSlot
        {
            Name = slotData.Name,
            OriginalName = slotData.OriginalName,
            IsAPIAdded = slotData.IsAPIAdded,
            IsQuickSlot = slotData.IsQuickSlot,
            Get = slotData.Get,
            Valid = slotData.Valid,
		};
    }
}