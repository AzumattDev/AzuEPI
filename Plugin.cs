using System;
using System.IO;
using AzuExtendedPlayerInventory.EPI;
using AzuExtendedPlayerInventory.EPI.Patches;
using AzuExtendedPlayerInventory.EPI.QAB;
using AzuExtendedPlayerInventory.EPI.Utilities;
using AzuExtendedPlayerInventory.Moveable;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace AzuExtendedPlayerInventory;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class AzuExtendedPlayerInventoryPlugin : BaseUnityPlugin
{
    internal const string ModName = "AzuExtendedPlayerInventory";
    internal const string ModVersion = "1.3.4";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
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
        ShowQuickSlots = config("2 - Extended Inventory", "Show Quickslots", Toggle.On, "Should the quickslots be shown?");
        ShowQuickSlots.SettingChanged += (sender, args) => { HotkeyBarController.Hud_Update_Patch.DeselectHotkeyBar(); };
        ExtraRows = config("2 - Extended Inventory", "Extra Inventory Rows", 0, "Number of extra ordinary rows. (This can cause overlap with chest GUI, make sure you hold CTRL (the default key) and drag to desired position)");
        // Fire an event handler on setting change for ExtraRows that will update the inventory size
        ExtraRows.SettingChanged += (sender, args) => { UpdateInventorySize(); };
        AddEquipmentRow = config("2 - Extended Inventory", "Add Equipment Row", Toggle.On, "Add special row for equipped items and quick slots. (IF YOU ARE USING RANDY KNAPPS EAQs KEEP THIS VALUE OFF)");
        AddEquipmentRow.SettingChanged += (sender, args) => { CheckRandy(); };
        DisplayEquipmentRowSeparate = config("2 - Extended Inventory", "Display Equipment Row Separate", Toggle.On, "Display equipment and quickslots in their own area. (IF YOU ARE USING RANDY KNAPPS EAQs KEEP THIS VALUE OFF)");

        DisplayEquipmentRowSeparate.SettingChanged += (sender, args) => { CheckRandy(); };

        HelmetText = config("2 - Extended Inventory", "Helmet Text", "Head",
            "Text to show for helmet slot.", false);
        ChestText = config("2 - Extended Inventory", "Chest Text", "Chest",
            "Text to show for chest slot.", false);
        LegsText = config("2 - Extended Inventory", "Legs Text", "Legs",
            "Text to show for legs slot.", false);
        BackText = config("2 - Extended Inventory", "Back Text", "Back",
            "Text to show for back slot.", false);
        UtilityText = config("2 - Extended Inventory", "Utility Text", "Utility",
            "Text to show for utility slot.", false);

        QuickAccessScale = config("2 - Extended Inventory", "QuickAccess Scale", 1f,
            "Scale of quick access bar. ", false);

        HotKey1 = config("2 - Extended Inventory", "HotKey (Quickslot 1)", new KeyboardShortcut(KeyCode.Z),
            "Hotkey 1 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
        HotKey2 = config("2 - Extended Inventory", "HotKey (Quickslot 2)", new KeyboardShortcut(KeyCode.X),
            "Hotkey 2 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
        HotKey3 = config("2 - Extended Inventory", "HotKey (Quickslot 3)", new KeyboardShortcut(KeyCode.C),
            "Hotkey 3 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);

        HotKey1Text = config("2 - Extended Inventory", "HotKey (Quickslot 1) Text", "",
            "Hotkey 1 Display Text. Leave blank to use the hotkey itself.", false);
        HotKey2Text = config("2 - Extended Inventory", "HotKey (Quickslot 2) Text", "",
            "Hotkey 2 Display Text. Leave blank to use the hotkey itself.", false);
        HotKey3Text = config("2 - Extended Inventory", "HotKey (Quickslot 3) Text", "",
            "Hotkey 3 Display Text. Leave blank to use the hotkey itself.", false);

        QuickslotDragKeys = config("2 - Extended Inventory", "Drag Keys (Quickslot Drag)", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl),
            "Key or keys to move quick slots. It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.",
            false);

        QuickAccessX = config("2 - Extended Inventory", "Quickslot X", 9999f,
            "Current X of Quick Slots", false);
        QuickAccessY = config("2 - Extended Inventory", "Quickslot Y", 9999f,
            "Current Y of Quick Slots", false);

        /* Moveable Chest Inventory */
        MoveableChestInventory.ChestInventoryX = config("3 - Chest Inventory", "Chest Inventory X", -1f,
            "Current X of chest", false);
        MoveableChestInventory.ChestInventoryY = config("3 - Chest Inventory", "Chest Inventory Y", -1f,
            "Current Y of chest", false);
        MoveableChestInventory.ChestDragKeys = config("3 - Chest Inventory", "Drag Keys (Chest Drag)",
            new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl),
            "Key or keys (to move the container). It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.",
            false);

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

        _harmony.PatchAll();
        SetupWatcher();
    }

    private void Start()
    {
        if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("ishid4.mods.betterarchery", out var BetterArchery))
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

        CheckRandy();
        CheckWeightBase();

        if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(ExtendedPlayerInventory.MinimalUiguid, out var MinimalUI) && MinimalUI is not null)
        {
            InventoryGuiPatches.UpdateInventory_Patch.leftOffset += 10;
        }
        InventoryGuiPatches.UpdateInventory_Patch.ResizeSlots();
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
        if (InventoryGui.instance == null) return;
        if (Player.m_localPlayer == null) return;
        int height = 4 + ExtraRows.Value + (AddEquipmentRow.Value == Toggle.On ? API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()) : 0);
        Player.m_localPlayer.m_inventory.m_height = height;
        Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height = height;

        Player.m_localPlayer.m_inventory.Changed();
        Utilities.InventoryFix();
    }

    private static void CheckRandy()
    {
        if (DisplayEquipmentRowSeparate.Value == Toggle.Off && AddEquipmentRow.Value == Toggle.Off) return;
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out var RandyEAQ)) return;
        DisplayEquipmentRowSeparate.Value = Toggle.Off;
        AddEquipmentRow.Value = Toggle.Off;
        context.Config.Save();
        AzuExtendedPlayerInventoryLogger.LogWarning(
            $"{Environment.NewLine}RandyKnapp's Equipment and Quickslots mod has been detected. " +
            $"This mod is not fully compatible with his. As a result, the Display Equipment Row Separate and Add Equipment Row options for this mod have been disabled and the configuration saved.");
    }

    private static void CheckWeightBase()
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("MadBuffoon.WeightBase", out var WbInfo)) return;
        WbInstalled = true;
    }
    
    #region ConfigOptions

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;

    public static ConfigEntry<Toggle> AddEquipmentRow = null!;
    public static ConfigEntry<Toggle> DisplayEquipmentRowSeparate = null!;
    public static ConfigEntry<Toggle> ShowQuickSlots = null!;
    public static ConfigEntry<int> ExtraRows = null!;
    public static ConfigEntry<string> HelmetText = null!;
    public static ConfigEntry<string> ChestText = null!;
    public static ConfigEntry<string> LegsText = null!;
    public static ConfigEntry<string> BackText = null!;
    public static ConfigEntry<string> UtilityText = null!;
    public static ConfigEntry<float> QuickAccessScale = null!;

    public static ConfigEntry<KeyboardShortcut> HotKey1 = null!;
    public static ConfigEntry<KeyboardShortcut> HotKey2 = null!;
    public static ConfigEntry<KeyboardShortcut> HotKey3 = null!;
    public static ConfigEntry<string> HotKey1Text = null!;
    public static ConfigEntry<string> HotKey2Text = null!;
    public static ConfigEntry<string> HotKey3Text = null!;
    public static ConfigEntry<KeyboardShortcut> QuickslotDragKeys = null!;
    public static ConfigEntry<KeyboardShortcut> ModKeyTwo = null!;

    public static ConfigEntry<KeyboardShortcut>[] Hotkeys = null!;
    public static ConfigEntry<string>[] HotkeyTexts;

    public static ConfigEntry<float> QuickAccessX = null!;
    public static ConfigEntry<float> QuickAccessY = null!;

    public static ConfigEntry<Vector2> UIAnchor = null!;
    public static ConfigEntry<Vector3> LocalScale = null!;

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order = null!;
        [UsedImplicitly] public bool? Browsable = null!;
        [UsedImplicitly] public string? Category = null!;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
    }

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