using System;
using System.IO;
using AzuExtendedPlayerInventory.EPI;
using AzuExtendedPlayerInventory.EPI.QAB;
using AzuExtendedPlayerInventory.EPI.Utilities;
using AzuExtendedPlayerInventory.Moveable;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace AzuExtendedPlayerInventory;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("vapok.mods.adventurebackpacks", BepInDependency.DependencyFlags.SoftDependency)] // To make sure we load after Adventure Backpacks
[BepInDependency("ishid4.mods.betterarchery", BepInDependency.DependencyFlags.SoftDependency)] // To make sure we load after Better Archery
public class AzuExtendedPlayerInventoryPlugin : BaseUnityPlugin
{
    internal const string ModName = "AzuExtendedPlayerInventory";
    internal const string ModVersion = "1.4.3";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;
    private static readonly string ConfigFileName = ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    private readonly Harmony _harmony = new(ModGUID);
    public static readonly ManualLogSource AzuExtendedPlayerInventoryLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    internal static AzuExtendedPlayerInventoryPlugin context = null!;
    internal static bool WbInstalled;

    public enum Toggle
    {
        On = 1,
        Off = 0,
    }

    private void Awake()
    {
        APIManager.Patcher.Patch();

        context = this;
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        

        /* Extended Player Inventory Config options */
        AutoEquip = config("2 - Extended Inventory", "Auto Equip", Toggle.On, "Automatically equip items that go into the gear slots. Applies when picking up items, transferring between containers, or picking up your tombstone.");
        ShowQuickSlots = config("2 - Extended Inventory", "Show Quickslots", Toggle.On, "Should the quickslots be shown?");
        ShowQuickSlots.SettingChanged += (sender, args) => { ExtendedPlayerInventory.QuickSlots.DeselectHotkeyBars(); };
        ExtraRows = config("2 - Extended Inventory", "Extra Inventory Rows", 0, "Number of extra ordinary rows. (This can cause overlap with chest GUI, make sure you hold CTRL (the default key) and drag to desired position)");
        // Fire an event handler on setting change for ExtraRows that will update the inventory size
        ExtraRows.SettingChanged += (sender, args) => { UpdateInventorySize(); };
        AddEquipmentRow = config("2 - Extended Inventory", "Add Equipment Row", Toggle.On, "Add special row for equipped items and quick slots. (IF YOU ARE USING RANDY KNAPPS EAQs KEEP THIS VALUE OFF)");
        AddEquipmentRow.SettingChanged += (sender, args) => { CheckRandy(); };
        DisplayEquipmentRowSeparate = config("2 - Extended Inventory", "Display Equipment Row Separate", Toggle.On, "Display equipment and quickslots in their own area. (IF YOU ARE USING RANDY KNAPPS EAQs KEEP THIS VALUE OFF)");
        DisplayEquipmentRowSeparatePanel = config("2 - Extended Inventory", "Display Equipment Row Separate Panel", Toggle.Off, "Display equipment and quickslots in their own panel. (depends on \"Display Equipment Row Separate\" config value)");

        DisplayEquipmentRowSeparate.SettingChanged += (sender, args) => { CheckRandy(); };
        DisplayEquipmentRowSeparatePanel.SettingChanged += (sender, args) => { ExtendedPlayerInventory.EquipmentPanel.UpdateInventoryBackground(); ExtendedPlayerInventory.EquipmentPanel.ResizeSlots(); };
        

        HelmetText = config("2 - Extended Inventory", "Helmet Text", "Head", "Text to show for helmet slot.", false);
        ChestText = config("2 - Extended Inventory", "Chest Text", "Chest", "Text to show for chest slot.", false);
        LegsText = config("2 - Extended Inventory", "Legs Text", "Legs", "Text to show for legs slot.", false);
        BackText = config("2 - Extended Inventory", "Back Text", "Back", "Text to show for back slot.", false);
        UtilityText = config("2 - Extended Inventory", "Utility Text", "Utility", "Text to show for utility slot.", false);
        RightHandText = config("2 - Extended Inventory", "Right Hand Text", "R Hand", "Text to show for right hand slot.", false);
        LeftHandText = config("2 - Extended Inventory", "Left Hand Text", "L Hand", "Text to show for left hand slot.", false);

        HelmetText.SettingChanged += (s, e) => ExtendedPlayerInventory.EquipmentPanel.UpdateVanillaSlotNames();
        ChestText.SettingChanged += (s, e) => ExtendedPlayerInventory.EquipmentPanel.UpdateVanillaSlotNames();
        LegsText.SettingChanged += (s, e) => ExtendedPlayerInventory.EquipmentPanel.UpdateVanillaSlotNames();
        BackText.SettingChanged += (s, e) => ExtendedPlayerInventory.EquipmentPanel.UpdateVanillaSlotNames();
        UtilityText.SettingChanged += (s, e) => ExtendedPlayerInventory.EquipmentPanel.UpdateVanillaSlotNames();

        QuickAccessScale = config("2 - Extended Inventory", "QuickAccess Scale", 0.85f, "Scale of quick access bar. ", false);

        HotKey1 = config("2 - Extended Inventory", "HotKey (Quickslot 1)", new KeyboardShortcut(KeyCode.Z), "Hotkey 1 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
        HotKey2 = config("2 - Extended Inventory", "HotKey (Quickslot 2)", new KeyboardShortcut(KeyCode.X), "Hotkey 2 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
        HotKey3 = config("2 - Extended Inventory", "HotKey (Quickslot 3)", new KeyboardShortcut(KeyCode.C), "Hotkey 3 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);

        HotKey1Text = config("2 - Extended Inventory", "HotKey (Quickslot 1) Text", "", "Hotkey 1 Display Text. Leave blank to use the hotkey itself.", false);
        HotKey2Text = config("2 - Extended Inventory", "HotKey (Quickslot 2) Text", "", "Hotkey 2 Display Text. Leave blank to use the hotkey itself.", false);
        HotKey3Text = config("2 - Extended Inventory", "HotKey (Quickslot 3) Text", "", "Hotkey 3 Display Text. Leave blank to use the hotkey itself.", false);

        HotKey1.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        HotKey2.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        HotKey3.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        HotKey1Text.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        HotKey2Text.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        HotKey3Text.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();

        QuickslotDragKeys = config("2 - Extended Inventory", "Drag Keys (Quickslot Drag)", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key or keys to move quick slots. It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.", false);

        QuickAccessX = config("2 - Extended Inventory", "Quickslot X", 9999f, "Current X of Quick Slots", false);
        QuickAccessY = config("2 - Extended Inventory", "Quickslot Y", 9999f, "Current Y of Quick Slots", false);

        string order = $"{EquipmentSlot.helmetSlotID},{EquipmentSlot.legsSlotID},{EquipmentSlot.utilitySlotID},{EquipmentSlot.chestSlotID},{EquipmentSlot.backSlotID}";
        VanillaSlotsOrder = config("2 - Extended Inventory", "Vanilla slots order", order, "Comma separated list defining order of vanilla slots", false);

        VanillaSlotsOrder.SettingChanged += (s, e) => ExtendedPlayerInventory.EquipmentPanel.ReorderVanillaSlots();

        /* Moveable Chest Inventory */
        MoveableChestInventory.ChestInventoryX = config("3 - Chest Inventory", "Chest Inventory X", -1f, "Current X of chest", false);
        MoveableChestInventory.ChestInventoryY = config("3 - Chest Inventory", "Chest Inventory Y", -1f, "Current Y of chest", false);
        MoveableChestInventory.ChestDragKeys = config("3 - Chest Inventory", "Drag Keys (Chest Drag)", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key or keys (to move the container). It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.", false);
        
        
        MakeDropAllButton = config("3 - Button", "Drop All Button", Toggle.Off, "Key or keys (to move the container). It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.", false);
        DropAllButtonPosition = config("3 - Button", "Button Position", new Vector2(880.00f, 10.00f), "Button position relative to the inventory background's top left corner", false);
        DropAllButtonText = config("3 - Button", "Button Text", "Drop all", "Button text", false);

        equipmentSlotLabelAlignment = config("4 - Equipment slots - Label style", "Horizontal alignment", TMPro.HorizontalAlignmentOptions.Center, "Horizontal alignment of text component in equipment slot label", false);
        equipmentSlotLabelWrappingMode = config("4 - Equipment slots - Label style", "Text wrapping mode", TMPro.TextWrappingModes.PreserveWhitespaceNoWrap, "Size of text component in slot label", false);
        equipmentSlotLabelMargin = config("4 - Equipment slots - Label style", "Margin", new Vector4(5f, 0f, 5f, 0f), "Margin: left top right bottom", false);
        equipmentSlotLabelFontSize = config("4 - Equipment slots - Label style", "Font size", 18f, "Max text size in slot label", false);
        equipmentSlotLabelFontColor = config("4 - Equipment slots - Label style", "Font color", new Color(0.596f, 0.816f, 1f), "Text color in slot label", false);

        quickSlotLabelAlignment = config("4 - Quick slots - Label style", "Horizontal alignment", TMPro.HorizontalAlignmentOptions.Left, "Horizontal alignment of text component in slot label", false);
        quickSlotLabelWrappingMode = config("4 - Quick slots - Label style", "Text wrapping mode", TMPro.TextWrappingModes.PreserveWhitespaceNoWrap, "Size of text component in slot label", false);
        quickSlotLabelMargin = config("4 - Quick slots - Label style", "Margin", new Vector4(5f, 0f, 5f, 0f), "Margin: left top right bottom", false);
        quickSlotLabelFontSize = config("4 - Quick slots - Label style", "Font size", 18f, "Max text size in slot label", false);
        quickSlotLabelFontColor = config("4 - Quick slots - Label style", "Font color", new Color(0.596f, 0.816f, 1f), "Text color in slot label", false);

        quickSlotLabelAlignment.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        quickSlotLabelWrappingMode.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        quickSlotLabelMargin.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        quickSlotLabelFontSize.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();
        quickSlotLabelFontColor.SettingChanged += (s, e) => QuickAccessBar.UpdateSlots();

        Hotkeys = new[]
        {
            HotKey1,
            HotKey2,
            HotKey3,
        };

        HotkeyTexts = new[]
        {
            HotKey1Text,
            HotKey2Text,
            HotKey3Text,
        };

        if (Chainloader.PluginInfos.TryGetValue("ishid4.mods.betterarchery", out var BetterArchery))
        {
            // Force disable the configuration for BetterArchery. Turn off the quiver
            var tryGetEntry = BetterArchery.Instance.Config.TryGetEntry<bool>("Quiver", "Enable Quiver", out var entry);
            if (tryGetEntry && entry.Value)
            {
                entry.Value = false;
                AzuExtendedPlayerInventoryLogger.LogWarning(
                    $"{Environment.NewLine}BetterArchery's quiver feature has been forcibly disabled to prevent potential issues with your inventory. " +
                    $"Logging into your world now may cause you to lose all arrows in your quiver. {Environment.NewLine}If you accept this risk, please proceed. " +
                    $"{Environment.NewLine}If you prefer to avoid any potential issues, please disable/remove {ModName}, restart the game, empty your quiver, remove " +
                    $"the BetterArchery mod, and then reinstall {ModName}.");
            }
        }

        _harmony.PatchAll();
        SetupWatcher();

        // Version 1.7.6 GetEquippedBackpack will be always null despite wearing a backpack, damn
        /*if (AdventureBackpacks.API.ABAPI.IsLoaded())
            API.AddSlot("AdvPack", player => AdventureBackpacks.API.ABAPI.GetEquippedBackpack(Player.m_localPlayer)?.ItemData, AdventureBackpacks.API.ABAPI.IsBackpack);*/
    }

    public static int QuickSlotsCount => Hotkeys.Length;

    public static int EquipmentSlotsCount => ExtendedPlayerInventory.slots.Count - QuickSlotsCount;

    public static string GetHotkeyText(int index) => HotkeyTexts[index].Value.IsNullOrWhiteSpace() ? GetHotkey(index).ToString() : HotkeyTexts[index].Value;

    public static KeyboardShortcut GetHotkey(int index) => Hotkeys[index].Value;

    private void Start()
    {
        CheckRandy();
        CheckWeightBase();

        ExtendedPlayerInventory.EquipmentPanel.InitializeVanillaSlotsOrder();
        ExtendedPlayerInventory.EquipmentPanel.ReorderVanillaSlots();
    }

    private void OnDestroy()
    {
        Config.Save();
    }

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            AzuExtendedPlayerInventoryLogger.LogError($"There was an issue loading your {ConfigFileName}");
            AzuExtendedPlayerInventoryLogger.LogError("Please check your config entries for spelling and format!");
        }
    }

    // Create the UpdateInventorySize method
    public static void UpdateInventorySize()
    {
        if (InventoryGui.instance == null)
            return;

        if (Player.m_localPlayer == null)
            return;

        int height = 4 + ExtraRows.Value + (AddEquipmentRow.Value.IsOn() ? API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()) : 0);
        
        Player.m_localPlayer.m_inventory.m_height = height;
        Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height = height;
        Player.m_localPlayer.m_inventory.Changed();
        
        Utilities.InventoryFix();
    }

    private static void CheckRandy()
    {
        if (DisplayEquipmentRowSeparate.Value.IsOff() && AddEquipmentRow.Value.IsOff()) return;
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out _)) return;
        DisplayEquipmentRowSeparate.Value = Toggle.Off;
        AddEquipmentRow.Value = Toggle.Off;
        context.Config.Save();
        AzuExtendedPlayerInventoryLogger.LogWarning($"{Environment.NewLine}RandyKnapp's Equipment and Quickslots mod has been detected. " +
                                                    $"This mod is not fully compatible with his. " +
                                                    $"As a result, the Display Equipment Row Separate and Add Equipment Row options for this mod have been disabled and the configuration saved.");
    }

    private static void CheckWeightBase()
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("MadBuffoon.WeightBase", out _)) return;
        WbInstalled = true;
    }

    #region ConfigOptions

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    public static ConfigEntry<Toggle> AutoEquip = null!;

    public static ConfigEntry<Toggle> AddEquipmentRow = null!;
    public static ConfigEntry<Toggle> DisplayEquipmentRowSeparate = null!;
    public static ConfigEntry<Toggle> DisplayEquipmentRowSeparatePanel = null!;
    public static ConfigEntry<Toggle> ShowQuickSlots = null!;
    public static ConfigEntry<Toggle> MakeDropAllButton = null!;
    public static ConfigEntry<Vector2> DropAllButtonPosition = null!;
    public static ConfigEntry<string> DropAllButtonText = null!;
    public static ConfigEntry<int> ExtraRows = null!;
    public static ConfigEntry<string> HelmetText = null!;
    public static ConfigEntry<string> ChestText = null!;
    public static ConfigEntry<string> LegsText = null!;
    public static ConfigEntry<string> BackText = null!;
    public static ConfigEntry<string> UtilityText = null!;
    public static ConfigEntry<string> RightHandText = null!;
    public static ConfigEntry<string> LeftHandText = null!;
    public static ConfigEntry<float> QuickAccessScale = null!;
    public static ConfigEntry<string> VanillaSlotsOrder = null!;

    public static ConfigEntry<KeyboardShortcut> HotKey1 = null!;
    public static ConfigEntry<KeyboardShortcut> HotKey2 = null!;
    public static ConfigEntry<KeyboardShortcut> HotKey3 = null!;
    public static ConfigEntry<string> HotKey1Text = null!;
    public static ConfigEntry<string> HotKey2Text = null!;
    public static ConfigEntry<string> HotKey3Text = null!;
    public static ConfigEntry<KeyboardShortcut> QuickslotDragKeys = null!;
    public static ConfigEntry<KeyboardShortcut> ModKeyTwo = null!;

    public static ConfigEntry<KeyboardShortcut>[] Hotkeys = null!;
    public static ConfigEntry<string>[] HotkeyTexts = null!;

    public static ConfigEntry<float> QuickAccessX = null!;
    public static ConfigEntry<float> QuickAccessY = null!;

    public static ConfigEntry<Vector2> UIAnchor = null!;
    public static ConfigEntry<Vector3> LocalScale = null!;

    public static ConfigEntry<TMPro.HorizontalAlignmentOptions> equipmentSlotLabelAlignment = null!;
    public static ConfigEntry<TMPro.TextWrappingModes> equipmentSlotLabelWrappingMode = null!;
    public static ConfigEntry<Vector4> equipmentSlotLabelMargin = null!;
    public static ConfigEntry<float> equipmentSlotLabelFontSize = null!;
    public static ConfigEntry<Color> equipmentSlotLabelFontColor = null!;

    public static ConfigEntry<TMPro.HorizontalAlignmentOptions> quickSlotLabelAlignment = null!;
    public static ConfigEntry<TMPro.TextWrappingModes> quickSlotLabelWrappingMode = null!;
    public static ConfigEntry<Vector4> quickSlotLabelMargin = null!;
    public static ConfigEntry<float> quickSlotLabelFontSize = null!;
    public static ConfigEntry<Color> quickSlotLabelFontColor = null!;

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

#nullable enable
    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order = null!;
        [UsedImplicitly] public bool? Browsable = null!;
        [UsedImplicitly] public string? Category = null!;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
    }
#nullable disable

    class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value) => value;
        public override bool IsValid(object value) => true;

        public override string ToDescriptionString() =>
            "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
    }

    #endregion
}

public static class ToggleExtentions
{
    public static bool IsOn(this AzuExtendedPlayerInventoryPlugin.Toggle value)
    {
        return value == AzuExtendedPlayerInventoryPlugin.Toggle.On;
    }

    public static bool IsOff(this AzuExtendedPlayerInventoryPlugin.Toggle value)
    {
        return value == AzuExtendedPlayerInventoryPlugin.Toggle.Off;
    }
}