namespace AzuEPI.Core.Slots;

public static class Initializers
{
    public static void InitSlotsAndKeys()
    {
        InitializeBuiltInSlots();
        InitializeHotkeys();
        InitializeQuickslots();
        API.RelocalizeSlots();
    }

    public static void InitializeBuiltInSlots()
    {
        slots.Add(new Model.EquipmentSlot { Name = HelmetText.Value, OriginalName = "$azu_epi_helmet", IsQuickSlot = false, Get = player => player.m_helmetItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet && !SlotAcceptRules.HasDedicatedAPISlot(item) });
        slots.Add(new Model.EquipmentSlot { Name = ChestText.Value, OriginalName = "$azu_epi_chest", IsQuickSlot = false, Get = player => player.m_chestItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest && !SlotAcceptRules.HasDedicatedAPISlot(item) });
        slots.Add(new Model.EquipmentSlot { Name = LegsText.Value, OriginalName = "$azu_epi_legs", IsQuickSlot = false, Get = player => player.m_legItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs && !SlotAcceptRules.HasDedicatedAPISlot(item) });
        slots.Add(new Model.EquipmentSlot
        {
            Name = BackText.Value, OriginalName = "$azu_epi_shoulder", IsQuickSlot = false, Get = player =>
            {
                ItemDrop.ItemData? shoulderItem = player.m_shoulderItem;
                // If the shoulder slot contains an item with a dedicated API slot (like a backpack),
                if (shoulderItem != null && SlotAcceptRules.HasDedicatedAPISlot(shoulderItem))
                {
                    return player.GetInventory()?.GetEquippedItems()
                        ?.FirstOrDefault(i => i != null && i != shoulderItem && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder && !SlotAcceptRules.HasDedicatedAPISlot(i));
                }

                return shoulderItem;
            },
            Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder && !SlotAcceptRules.HasDedicatedAPISlot(item)
        });
        slots.Add(new Model.EquipmentSlot
        {
            Name = UtilityText.Value, OriginalName = "$azu_epi_utility", IsQuickSlot = false, Get = player =>
            {
                ItemDrop.ItemData? utilityItem = player.m_utilityItem;
                if (utilityItem != null && SlotAcceptRules.HasDedicatedAPISlot(utilityItem))
                {
                    return player.GetInventory()?.GetEquippedItems()
                        ?.FirstOrDefault(i => i != null && i != utilityItem && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && !SlotAcceptRules.HasDedicatedAPISlot(i));
                }

                return utilityItem;
            },
            Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility && !SlotAcceptRules.HasDedicatedAPISlot(item)
        });
    }

    public static void InitializeQuickslots()
    {
        API.BeforeQuickSlotsAdded();
        for (int i = 0; i < Hotkeys.Length; ++i)
            slots.Add(new Model.Slot
            {
                Name = HotkeyTexts[i].Value.IsNullOrWhiteSpace() ? Hotkeys[i].Value.ToString() : HotkeyTexts[i].Value,
                IsQuickSlot = true,
            });
        API.QuickSlotsAdded();
    }

    internal static void InitializeHotkeys()
    {
        int count = QuickSlotsAmount.Value;
        KeyboardShortcut[] defaultKeys =
        [
            new KeyboardShortcut(KeyCode.Z, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.X, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.C, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.V, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.B, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt)
        ];

        Hotkeys = new ConfigEntry<KeyboardShortcut>[count];
        HotkeyTexts = new ConfigEntry<string>[count];

        for (int i = 0; i < count; ++i)
        {
            KeyboardShortcut keyboardShortcut = i < defaultKeys.Length ? defaultKeys[i] : KeyboardShortcut.Empty;
            Hotkeys[i] = context.config("8 - Quick Slot Hotkeys", $"Hotkey {i + 1}", keyboardShortcut,
                $"Keyboard shortcut for quick slot {i + 1}. See https://docs.unity3d.com/Manual/ConventionalGameInput.html for valid key names.", false);
            HotkeyTexts[i] = context.config("8 - Quick Slot Hotkeys", $"Hotkey {i + 1} Display Text", $"Alt + {keyboardShortcut.MainKey.ToString().Replace("Alpha", string.Empty)}",
                $"Custom text to display for quick slot {i + 1} hotkey on the HUD. Leave blank to auto-generate from the hotkey itself.", false);
            HotkeyTexts[i].SettingChanged += (_, _) =>
            {
                if (Player.m_localPlayer != null && InventoryGui.instance != null)
                {
                    context.FullRebuild();
                }
            };
        }
    }
}