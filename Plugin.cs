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
    internal const string ModVersion = "2.0.0";
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

        /* 1 - General Settings */
        ResetConfigOrder();
        _serverConfigLocked = config("1 - General Settings", "Lock Configuration", On, "If on, the configuration is locked and can be changed by server admins only.", NextOrder);
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        AutoEquip = config("1 - General Settings", "Auto Equip Items", On, "Automatically equip items when picked up, transferred between containers, or recovered from tombstone.", NextOrder);

        /* 2 - Inventory Settings */
        ResetConfigOrder();
        ExtraRows = config("2 - Inventory Settings", "Extra Inventory Rows", 0, "Number of extra inventory rows to add. Note: May overlap with chest windows. Use CTRL+drag to reposition if needed.", NextOrder);
        AddEquipmentRow = config("2 - Inventory Settings", "Enable Equipment Row", On, "Adds a special row for equipped items and quick slots. Keep OFF if using Randy Knapp's Equipment and Quick Slots mod.", NextOrder);
        DisplayEquipmentRowSeparate = config("2 - Inventory Settings", "Display Equipment Separately", On, "Shows equipment and quick slots in their own dedicated panel. Keep OFF if using Randy Knapp's Equipment and Quick Slots mod.", NextOrder);

        /* 3 - Equipment Slot Labels */
        ResetConfigOrder();
        HelmetText = config("3 - Equipment Slot Labels", "Head Slot Label", "Head", "Text shown for the head/helmet equipment slot.", NextOrder, false);
        ChestText = config("3 - Equipment Slot Labels", "Chest Slot Label", "Chest", "Text shown for the chest armor equipment slot.", NextOrder, false);
        LegsText = config("3 - Equipment Slot Labels", "Legs Slot Label", "Legs", "Text shown for the legs armor equipment slot.", NextOrder, false);
        BackText = config("3 - Equipment Slot Labels", "Back Slot Label", "Back", "Text shown for the back/cape equipment slot.", NextOrder, false);
        UtilityText = config("3 - Equipment Slot Labels", "Utility Slot Label", "Utility", "Text shown for the utility equipment slot.", NextOrder, false);
        TrinketText = config("3 - Equipment Slot Labels", "Trinket Slot Label", "Trinket", "Text shown for the trinket equipment slot.", NextOrder, false);

        /* 4 - Quick Slots */
        ResetConfigOrder();
        QuickSlotsAmount = config("4 - Quick Slots", "Number of Quick Slots", 3, new ConfigDescription("Number of quick slots to add (0-6).", new AcceptableValueRange<int>(0, 6)), NextOrder, true);
        ShowQuickSlots = config("4 - Quick Slots", "Show Quick Slots on HUD", On, "Shows the quick slots bar on screen during gameplay.", NextOrder); 
        QuickAccessScale = config("4 - Quick Slots", "Quick Slots Size", 0.85f, "Size/scale of the quick slots bar.", NextOrder, false);
        QuickslotDragKeys = config("4 - Quick Slots", "Quick Slots Drag Keys", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key combination to drag and reposition the quick slots bar.", NextOrder, false);
        QuickAccessX = config("4 - Quick Slots", "Quick Slots Position X", 9999f, "Horizontal position of quick slots (9999 = automatic).", NextOrder, false);
        QuickAccessY = config("4 - Quick Slots", "Quick Slots Position Y", 9999f, "Vertical position of quick slots (9999 = automatic).", NextOrder, false);

        /* 5 - Additional Equipment Slots */
        ResetConfigOrder();
        WishboneSlot = config("5 - Additional Equipment Slots", "Enable Wishbone Slot", On, "Adds a dedicated equipment slot for the Wishbone item.", NextOrder);
        WispLightSlot = config("5 - Additional Equipment Slots", "Enable Demister Slot", On, "Adds a dedicated equipment slot for the Demister/Wisplight item.", NextOrder);

        /* 6 - UI Options */
        ResetConfigOrder();
        VanityOption = config("6 - UI Options", "Show Vanity Button", On, "Shows the vanity button in the inventory panel.", NextOrder);
        LoadoutOption = config("6 - UI Options", "Show Loadout Button", On, "Shows the loadout button in the inventory panel.", NextOrder);
        OldLayout = config("6 - UI Options", "Use Legacy Layout", Off, "Uses the old inventory layout instead of the new one.", NextOrder);

        /* 7 - Buttons */
        ResetConfigOrder();
        MakeDropAllButton = config("7 - Buttons", "Enable Drop All Button", Off, "Adds a button to drop all items from your inventory.", NextOrder, false);
        DropAllButtonPosition = config("7 - Buttons", "Drop All Button Position", new Vector2(880.00f, 10.00f), "Position of the Drop All button in the inventory window.", NextOrder, false);

        InitializeHotkeys();

        QuickSlotsAmount.SettingChanged += (sender, args) =>
        {
            InitializeHotkeys();
            InventoryGuiPatches.UpdateInventory_Patch.RebuildQuickslots();
            SlotHelpers.ResizeSlots();
            Layout.UpdateInventorySize();
            InventoryHealth.FixHiddenItems();
        };
        ExtraRows.SettingChanged += (sender, args) => { Layout.UpdateInventorySize(); };
        AddEquipmentRow.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        DisplayEquipmentRowSeparate.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        ShowQuickSlots.SettingChanged += (sender, args) => { HotkeyBarController.Hud_Update_Patch.DeselectHotkeyBar(); };

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
                InventoryHealth.FixHiddenItems();
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
                InventoryHealth.FixHiddenItems();
            }
        };

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

    private void InitializeHotkeys()
    {
        int count = QuickSlotsAmount.Value;
        var defaultKeys = new[] { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N };

        Hotkeys = new ConfigEntry<KeyboardShortcut>[count];
        HotkeyTexts = new ConfigEntry<string>[count];

        for (int i = 0; i < count; i++)
        {
            var key = i < defaultKeys.Length ? defaultKeys[i] : KeyCode.None;
            Hotkeys[i] = config("4 - Quick Slots", $"HotKey (Quickslot {i + 1})", new KeyboardShortcut(key),
                $"Hotkey {i + 1} - Use https://docs.unity3d.com/Manual/ConventionalGameInput.html", false);
            HotkeyTexts[i] = config("4 - Quick Slots", $"HotKey (Quickslot {i + 1}) Display Text", "",
                $"Hotkey {i + 1} Display Text. Leave blank to use the hotkey itself.", false);
        }
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

    private int _configOrder = 100;
    private void ResetConfigOrder(int order = 100) => _configOrder = order;
    private int NextOrder => _configOrder--;

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    public static ConfigEntry<Toggle> AutoEquip = null!;

    public static ConfigEntry<Toggle> WishboneSlot = null!;
    public static ConfigEntry<Toggle> WispLightSlot = null!;

    public static ConfigEntry<Toggle> AddEquipmentRow = null!;
    public static ConfigEntry<Toggle> DisplayEquipmentRowSeparate = null!;
    public static ConfigEntry<int> QuickSlotsAmount = null!;
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

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, int order, bool synchronizedSetting = true)
    {
        var attributes = new ConfigurationManagerAttributes { Order = order };
        var tags = description.Tags.Length > 0 ? description.Tags.Append(attributes).ToArray() : new object[] { attributes };
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description, int order, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), order, synchronizedSetting);
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