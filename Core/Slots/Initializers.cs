using AzuEPI.Game.Slots.QAB;

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
            new(KeyCode.Z, KeyCode.LeftAlt),
            new(KeyCode.X, KeyCode.LeftAlt),
            new(KeyCode.C, KeyCode.LeftAlt),
            new(KeyCode.V, KeyCode.LeftAlt),
            new(KeyCode.B, KeyCode.LeftAlt),
            new(KeyCode.N, KeyCode.LeftAlt),
            new(KeyCode.Alpha1, KeyCode.LeftAlt),
            new(KeyCode.Alpha2, KeyCode.LeftAlt)
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

    internal static void SetupEventHandlers()
    {
        QuickSlotsAmount.SettingChanged += (sender, args) =>
        {
            InitializeHotkeys();
            context.FullRebuild();
        };
        ExtraRows.SettingChanged += (sender, args) => { context.FullRebuild(); };
        AddEquipmentRow.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        DisplayEquipmentRowSeparate.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        ShowQuickSlots.SettingChanged += (sender, args) => { HotkeyBarController.Hud_Update_Patch.DeselectHotkeyBar(); };
        SelectedPlayerStats.SettingChanged += StatsPanelController.OnStatsConfigChanged;
        SelectedLiveStats.SettingChanged += StatsPanelController.OnStatsConfigChanged;
        HelmetText.SettingChanged += (sender, args) => { API.RelocalizeSlots(); };
        ChestText.SettingChanged += (sender, args) => { API.RelocalizeSlots(); };
        BackText.SettingChanged += (sender, args) => { API.RelocalizeSlots(); };
        LegsText.SettingChanged += (sender, args) => { API.RelocalizeSlots(); };
        TrinketText.SettingChanged += (sender, args) => { API.RelocalizeSlots(); };
        UtilityText.SettingChanged += (sender, args) => { API.RelocalizeSlots(); };
        QuickSlotsPerRow.SettingChanged += (sender, args) =>
        {
            if (!Hud.instance) return;
            Transform hudroot = Hud.instance.transform.Find("hudroot");
            if (!hudroot) return;
            Transform qabTransform = hudroot.Find(QabName);
            if (!qabTransform || !qabTransform.TryGetComponent<HotkeyBar>(out HotkeyBar? qab)) return;
            foreach (HotkeyBar.ElementData? element in qab.m_elements)
                if (element.m_go)
                    GameObject.Destroy(element.m_go);
            qab.m_elements.Clear();
            context.FullRebuild();
        };

        RemovedEquipmentSlots.SettingChanged += (sender, args) =>
        {
            ApplySlotChanges();
            context.FullRebuild();
        };

        UserAddedSlots.SettingChanged += (sender, args) =>
        {
            ApplySlotChanges();
            context.FullRebuild();
        };

        WishboneSlot.SettingChanged += (sender, args) =>
        {
            if (WishboneSlot.Value.isOn())
            {
                // Don't add slot if Jewelcrafting has Wishbone configured as a gem
                if (!IsSlotMarkedForRemoval("$item_wishbone") && !JewelcraftingCompat.IsWishboneAGem())
                {
                    API.AddSlot("$item_wishbone", "Wishbone", 5);
                }
            }
            else
            {
                API.RemoveSlot("$item_wishbone");
                if (Localization.instance != null)
                    API.RemoveSlot(Localization.instance.Localize("$item_wishbone"));
                InventoryHealth.FixHiddenItems();
            }

            context.FullRebuild();
        };

        WispLightSlot.SettingChanged += (sender, args) =>
        {
            if (WispLightSlot.Value.isOn())
            {
                // Don't add slot if Jewelcrafting has Wisplight configured as a gem
                if (!IsSlotMarkedForRemoval("$item_demister") && !JewelcraftingCompat.IsWisplightAGem())
                {
                    int index = WishboneSlot.Value.isOn() && !JewelcraftingCompat.IsWishboneAGem() ? 6 : 5;
                    API.AddSlot("$item_demister", "Demister", index);
                }
            }
            else
            {
                API.RemoveSlot("$item_demister");
                if (Localization.instance != null)
                    API.RemoveSlot(Localization.instance.Localize("$item_demister"));
                InventoryHealth.FixHiddenItems();
            }

            context.FullRebuild();
        };

        VanityOption.SettingChanged += (sender, args) =>
        {
            if (VanityPanelController.VanityButtonGo)
            {
                VanityPanelController.VanityButtonGo.gameObject.SetActive(VanityOption.Value.isOn());
            }

            SlotHelpers.UpdateEquipmentBackgroundAnchors();
        };

        LoadoutOption.SettingChanged += (sender, args) =>
        {
            if (PersonalLoadoutGui.LoadoutsToggleButton)
            {
                PersonalLoadoutGui.LoadoutsToggleButton.gameObject.SetActive(LoadoutOption.Value.isOn());
            }

            SlotHelpers.UpdateEquipmentBackgroundAnchors();
        };

        VanityToggleGamepadKey.SettingChanged += (sender, args) => { RefreshPanelButtonBindings(); };
        LoadoutToggleGamepadKey.SettingChanged += (sender, args) => { RefreshPanelButtonBindings(); };
        StatsToggleGamepadKey.SettingChanged += (sender, args) => { RefreshPanelButtonBindings(); };

        OldLayout.SettingChanged += (sender, args) =>
        {
            SlotHelpers.ResizeSlots();
            Layout.UpdateInventorySize();
            Layout.ApplyLayoutCorrections();
            RebuildUI();
            context.FullRebuild();
            SlotHelpers.UpdateEquipmentBackgroundAnchors();
        };
    }
}