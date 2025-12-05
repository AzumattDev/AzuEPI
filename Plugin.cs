using APIManager;
using AzuEPI.Core.InventoryHandlers;
using AzuEPI.Core.Slots;
using AzuEPI.Game.Compatibility;
using AzuEPI.Game.Compatibility.AdvBackpacks;
using AzuEPI.Game.Loadout;
//using AzuEPI.Game.Moveable;
using AzuEPI.Game.Patches;
using AzuEPI.Game.Slots.QAB;
using AzuEPI.Game.Vanity;
using BepInEx.Logging;
using LocalizationManager;
using ServerSync;

namespace AzuEPI;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("vapok.mods.adventurebackpacks", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ishid4.mods.betterarchery", BepInDependency.DependencyFlags.SoftDependency)]
[BepInIncompatibility("shudnal.ExtraSlots")]
public class AzuExtendedPlayerInventoryPlugin : BaseUnityPlugin
{
    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    internal const string ModName = "AzuExtendedPlayerInventory";
    internal const string ModVersion = "1.4.12";
    internal const string Author = "Azumatt";
    internal const string ModGUID = Author + "." + ModName;
    private static readonly string ConfigFileName = ModGUID + ".cfg";
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static string ConnectionError = "";
    public static readonly ManualLogSource AzuExtendedPlayerInventoryLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    internal static AzuExtendedPlayerInventoryPlugin context = null!;
    internal static bool WbInstalled;
    internal readonly Harmony _harmony = new(ModGUID);

    private FileSystemWatcher _cfgWatcher;
    private System.Timers.Timer _debounce;

    public static readonly int FakeType = "AzuEPIFakeType".GetStableHashCode();

    static AzuExtendedPlayerInventoryPlugin()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var req = new AssemblyName(e.Name);
            return req.Name == "AzuExtendedPlayerInventory" ? typeof(AzuExtendedPlayerInventoryPlugin).Assembly : null;
        };
    }

    private void Awake()
    {
        Localizer.Load();
        Patcher.Patch(new[]
        {
            "AzuExtendedPlayerInventory",
            "AzuExtendedPlayerInventory.EPI.Patches"
        });

        context = this;
        _serverConfigLocked = config("1 - General", "Lock Configuration", On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        WishboneSlot = config("1.5 - Slots", "Wishbone Slot", On, "If on, adds a wishbone slot to the equipment row.");
        WispLightSlot = config("1.5 - Slots", "Demister (Wisplight) Slot", On, "If on, adds a demister slot to the equipment row.");

        WishboneSlot.SettingChanged += (sender, args) =>
        {
            if (WishboneSlot.Value.isOn())
            {
                API.AddSlot("$item_wishbone", "Wishbone", 5);
            }
            else
            {
                API.RemoveSlot("$item_wishbone");
                if (Localization.instance != null)
                    API.RemoveSlot(Localization.instance.Localize("$item_wishbone"));
            }
        };

        WispLightSlot.SettingChanged += (sender, args) =>
        {
            if (WispLightSlot.Value.isOn())
            {
                API.AddSlot("$item_demister", "Demister", WishboneSlot.Value.isOn() ? 6 : 5);
            }
            else
            {
                API.RemoveSlot("$item_demister");
                if (Localization.instance != null)
                    API.RemoveSlot(Localization.instance.Localize("$item_demister"));
            }
        };

        /* Extended Player Inventory Config options */
        AutoEquip = config("2 - Extended Inventory", "Auto Equip", On, "Automatically equip items that go into the gear slots. Applies when picking up items, transferring between containers, or picking up your tombstone.");
        ShowQuickSlots = config("2 - Extended Inventory", "Show Quickslots", On, "Should the quickslots in the main hud be shown? (not the inventory quickslots)");
        ShowQuickSlots.SettingChanged += (sender, args) => { HotkeyBarController.Hud_Update_Patch.DeselectHotkeyBar(); };
        ExtraRows = config("2 - Extended Inventory", "Extra Inventory Rows", 0, "Number of extra ordinary rows. (This can cause overlap with chest GUI, make sure you hold CTRL (the default key) and drag to desired position)");
        ExtraRows.SettingChanged += (sender, args) => { Layout.UpdateInventorySize(); };
        AddEquipmentRow = config("2 - Extended Inventory", "Add Equipment Row", On, "Add special row for equipped items and quick slots. (IF YOU ARE USING RANDY KNAPPS EAQs KEEP THIS VALUE OFF)");
        AddEquipmentRow.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        DisplayEquipmentRowSeparate = config("2 - Extended Inventory", "Display Equipment Row Separate", On, "Display equipment and quickslots in their own area. (IF YOU ARE USING RANDY KNAPPS EAQs KEEP THIS VALUE OFF)");

        DisplayEquipmentRowSeparate.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };

        HelmetText = config("2 - Extended Inventory", "Helmet Text", "Head", "Text to show for helmet slot.", false);
        ChestText = config("2 - Extended Inventory", "Chest Text", "Chest", "Text to show for chest slot.", false);
        LegsText = config("2 - Extended Inventory", "Legs Text", "Legs", "Text to show for legs slot.", false);
        BackText = config("2 - Extended Inventory", "Back Text", "Back", "Text to show for back slot.", false);
        UtilityText = config("2 - Extended Inventory", "Utility Text", "Utility", "Text to show for utility slot.", false);
        TrinketText = config("2 - Extended Inventory", "Trinket Text", "Trinket", "Text to show for trinket slot.", false);

        QuickAccessScale = config("2 - Extended Inventory", "QuickAccess Scale", 0.85f, "Scale of quick access bar. ", false);

        HotKey1 = config("2 - Extended Inventory", "HotKey (Quickslot 1)", new KeyboardShortcut(KeyCode.Z), "Hotkey 1 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
        HotKey2 = config("2 - Extended Inventory", "HotKey (Quickslot 2)", new KeyboardShortcut(KeyCode.X), "Hotkey 2 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
        HotKey3 = config("2 - Extended Inventory", "HotKey (Quickslot 3)", new KeyboardShortcut(KeyCode.C), "Hotkey 3 - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);

        HotKey1Text = config("2 - Extended Inventory", "HotKey (Quickslot 1) Text", "", "Hotkey 1 Display Text. Leave blank to use the hotkey itself.", false);
        HotKey2Text = config("2 - Extended Inventory", "HotKey (Quickslot 2) Text", "", "Hotkey 2 Display Text. Leave blank to use the hotkey itself.", false);
        HotKey3Text = config("2 - Extended Inventory", "HotKey (Quickslot 3) Text", "", "Hotkey 3 Display Text. Leave blank to use the hotkey itself.", false);

        QuickslotDragKeys = config("2 - Extended Inventory", "Drag Keys (Quickslot Drag)", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key or keys to move quick slots. It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.", false);

        QuickAccessX = config("2 - Extended Inventory", "Quickslot X", 9999f, "Current X of Quick Slots", false);
        QuickAccessY = config("2 - Extended Inventory", "Quickslot Y", 9999f, "Current Y of Quick Slots", false);

        /*
        /* Moveable Chest Inventory #1#
        MoveableChestInventory.ChestInventoryX = config("3 - Chest Inventory", "Chest Inventory X", -1f, "Current X of chest", false);
        MoveableChestInventory.ChestInventoryY = config("3 - Chest Inventory", "Chest Inventory Y", -1f, "Current Y of chest", false);
        MoveableChestInventory.ChestDragKeys = config("3 - Chest Inventory", "Drag Keys (Chest Drag)", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key or keys (to move the container). It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.", false);
        */

        MakeDropAllButton = config("3 - Button", "Drop All Button", Off, "Key or keys (to move the container). It is recommended to use the BepInEx Configuration Manager to do this fast and easy. If you're doing it manually in the config file Use https://docs.unity3d.com/Manual/class-InputManager.html format.", false);
        DropAllButtonPosition = config("3 - Button", "Button Position", new Vector2(880.00f, 10.00f), "Button position relative to the inventory background's top left corner", false);
        Hotkeys = new[]
        {
            HotKey1,
            HotKey2,
            HotKey3
        };
        HotkeyTexts = new[]
        {
            HotKey1Text,
            HotKey2Text,
            HotKey3Text
        };

        VanityOption = config("4 - UI", "Vanity Option", On, "If on, the vanity button will be shown in the inventory.");
        LoadoutOption = config("4 - UI", "Loadout Option", On, "If on, the loadout button will be shown in the inventory.");
        OldLayout = config("4 - UI", "Old Layout", Off, "If on, the old layout will be used.");

        VanityOption.SettingChanged += (sender, args) =>
        {
            if (VanityPanelController.VanityButtonGo)
            {
                VanityPanelController.VanityButtonGo.gameObject.SetActive(VanityOption.Value.isOn());
            }
        };

        LoadoutOption.SettingChanged += (sender, args) =>
        {
            if (PersonalLoadoutGui.LoadoutsToggleButton)
            {
                PersonalLoadoutGui.LoadoutsToggleButton.gameObject.SetActive(LoadoutOption.Value.isOn());
            }
        };

        OldLayout.SettingChanged += (sender, args) =>
        {
            SlotHelpers.ResizeSlots();
            Layout.UpdateInventorySize();
            Layout.ApplyLayoutCorrections();
            RebuildUI();
        };

        BetterArchery.CheckBetterArchery();

        _harmony.PatchAll();
        InitializeConfigWatcher();

        if (WishboneSlot.Value.isOn())
        {
            API.AddSlot("$item_wishbone", "Wishbone", 5);
        }

        if (WispLightSlot.Value.isOn())
        {
            API.AddSlot("$item_demister", "Demister", WishboneSlot.Value.isOn() ? 6 : 5);
        }

        var index = InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length;
        API.UpdateSlots(index, 1);
        InventoryGuiPatches.UpdateInventory_Patch.slots.Insert(index, new Model.EquipmentSlot { Name = TrinketText.Value, IsQuickSlot = false, Get = player => player.m_trinketItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket });
        SlotHelpers.ResizeSlots();

        Localization.OnLanguageChange += new Action(API.RelocalizeSlots);
    }

    private void Start()
    {
        EAQ.CheckRandy();
        WeightBase.CheckWeightBase();

        Localizer.OnLocalizationComplete += () =>
        {
            AdvBackpacksCompat.Init();
            JudesEquipmentCompat.Init();
            RustyBagsCompat.Init();

            if (Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out PluginInfo? randyEl) && randyEl is not null)
            {
                API.AddSlot("$azuepi_fingerslot", new[] { "Andvaranaut", "GoldRubyRing", "SilverRing" });
            }
        };
    }

    private void OnDestroy()
    {
        _cfgWatcher.Changed -= (_, __) => _debounce?.Start();
        _cfgWatcher.Created -= (_, __) => _debounce?.Start();
        _cfgWatcher.Renamed -= (_, __) => _debounce?.Start();
        _cfgWatcher.Dispose();
        _debounce.Elapsed -= (_, __) => ReadConfigValues(null!, null!);
        _debounce.Dispose();

        WishboneSlot.SettingChanged -= (sender, args) => { };
        WispLightSlot.SettingChanged -= (sender, args) => { };
        ExtraRows.SettingChanged -= (sender, args) => { };
        AddEquipmentRow.SettingChanged -= (sender, args) => { };
        DisplayEquipmentRowSeparate.SettingChanged -= (sender, args) => { };
        VanityOption.SettingChanged -= (sender, args) => { };
        LoadoutOption.SettingChanged -= (sender, args) => { };
        OldLayout.SettingChanged -= (sender, args) => { };

        Localization.OnLanguageChange -= new Action(API.RelocalizeSlots);
        Config.Save();
    }

    private void InitializeConfigWatcher()
    {
        _debounce = new System.Timers.Timer(150) { AutoReset = false };
        _debounce.Elapsed += (_, __) => ReadConfigValues(null!, null!);

        _cfgWatcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            IncludeSubdirectories = false,
            SynchronizingObject = ThreadingHelper.SynchronizingObject,
            EnableRaisingEvents = true
        };
        _cfgWatcher.Changed += (_, __) => _debounce?.Start();
        _cfgWatcher.Created += (_, __) => _debounce?.Start();
        _cfgWatcher.Renamed += (_, __) => _debounce?.Start();
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"There was an issue loading your {ConfigFileName}");
            AzuExtendedPlayerInventoryLogger.LogError($"Please check your config entries for spelling and format!{Environment.NewLine}{ex}");
        }
    }

    #region ConfigOptions

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    public static ConfigEntry<Toggle> AutoEquip = null!;

    public static ConfigEntry<Toggle> WishboneSlot = null!;
    public static ConfigEntry<Toggle> WispLightSlot = null!;

    public static ConfigEntry<Toggle> AddEquipmentRow = null!;
    public static ConfigEntry<Toggle> DisplayEquipmentRowSeparate = null!;
    public static ConfigEntry<Toggle> ShowQuickSlots = null!;
    public static ConfigEntry<Toggle> MakeDropAllButton = null!;
    public static ConfigEntry<Vector2> DropAllButtonPosition = null!;
    public static ConfigEntry<int> ExtraRows = null!;
    public static ConfigEntry<string> HelmetText = null!;
    public static ConfigEntry<string> ChestText = null!;
    public static ConfigEntry<string> LegsText = null!;
    public static ConfigEntry<string> BackText = null!;
    public static ConfigEntry<string> TrinketText = null!;
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
    public static ConfigEntry<string>[] HotkeyTexts = null!;

    public static ConfigEntry<float> QuickAccessX = null!;
    public static ConfigEntry<float> QuickAccessY = null!;

    public static ConfigEntry<Vector2> UIAnchor = null!;
    public static ConfigEntry<Vector3> LocalScale = null!;

    public static ConfigEntry<Toggle> VanityOption = null!;
    public static ConfigEntry<Toggle> LoadoutOption = null!;
    public static ConfigEntry<Toggle> OldLayout = null!;

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public bool? Browsable;
        [UsedImplicitly] public string? Category;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        [UsedImplicitly] public int? Order;
    }

    private class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value)
        {
            return value;
        }

        public override bool IsValid(object value)
        {
            return true;
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }
    }

    #endregion
}

public static class ToggleExtensions
{
    public static bool isOn(this AzuExtendedPlayerInventoryPlugin.Toggle toggle)
    {
        return toggle == On;
    }

    public static bool isOff(this AzuExtendedPlayerInventoryPlugin.Toggle toggle)
    {
        return toggle == Off;
    }
}

public static class LoggerExtensions
{
    public static void LogInfoDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogInfo(message);
#endif
    }

    public static void LogWarningDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogWarning(message);
#endif
    }

    public static void LogErrorDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogError(message);
#endif
    }

    public static void LogDebugDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogDebug(message);
#endif
    }
}