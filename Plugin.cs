using System.Reflection;
using APIManager;
using AzuEPI.Game.Compatibility.AdvBackpacks;
using AzuEPI.Game.Panels;
using AzuEPI.Game.Slots;
using AzuEPI.Game.Slots.QAB;
using BepInEx.Logging;
using LocalizationManager;
using ServerSync;

namespace AzuEPI;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("vapok.mods.adventurebackpacks", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ishid4.mods.betterarchery", BepInDependency.DependencyFlags.SoftDependency)]
[BepInIncompatibility("shudnal.ExtraSlots")]
[BepInIncompatibility("shudnal.ExtraSlotsCustomSlots")]
public class AzuExtendedPlayerInventoryPlugin : BaseUnityPlugin
{
    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    internal const string ModName = "AzuExtendedPlayerInventory";
    internal const string ModVersion = "2.2.3";
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
            AssemblyName req = new(e.Name);
            return req.Name == "AzuExtendedPlayerInventory" ? typeof(AzuExtendedPlayerInventoryPlugin).Assembly : null;
        };
    }

    private void Awake()
    {
        Localizer.Load();
        Patcher.Patch([
            "AzuExtendedPlayerInventory",
            "AzuExtendedPlayerInventory.EPI.Patches"
        ]);

        context = this;

        /* 1 - Server & Sync */
        ResetConfigOrder();
        _serverConfigLocked = config("1 - Server & Sync", "Lock Configuration", On, "When enabled, only server admins can modify configuration settings. All players will use the server's settings.", NextOrder);
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        /* 2 - Inventory */
        ResetConfigOrder();
        ExtraRows = config("2 - Inventory", "Extra Inventory Rows", 0, new ConfigDescription("Add extra rows to your inventory (0-5).", new AcceptableValueRange<int>(0, 5)), NextOrder);
        AddEquipmentRow = config("2 - Inventory", "Enable Equipment Row", On, "Adds a dedicated row for equipped items and quick slots. IMPORTANT: Turn OFF if using Randy Knapp's Equipment and Quick Slots mod to avoid conflicts.", NextOrder);
        DisplayEquipmentRowSeparate = config("2 - Inventory", "Display Equipment in Separate Panel", On, "Shows equipped items and quick slots in their own dedicated panel instead of inline. IMPORTANT: Turn OFF if using Randy Knapp's Equipment and Quick Slots mod.", NextOrder);
        AutoEquip = config("2 - Inventory", "Auto-Equip Items", On, "Automatically equip items when picked up, moved from containers, or recovered from tombstones. Disable if you prefer manual equipping.", NextOrder);

        /* 3 - Quick Slots */
        ResetConfigOrder();
        QuickSlotsAmount = config("3 - Quick Slots", "Number of Quick Slots", 3, new ConfigDescription("How many quick slots to add (0-8). Quick slots let you hotkey items for instant access. Default is 3.", new AcceptableValueRange<int>(0, 8)), NextOrder, true);
        ShowQuickSlots = config("3 - Quick Slots", "Show Quick Slots on HUD", On, "Display the quick slots bar on your screen during gameplay. Turn off to hide the bar while keeping the slots functional.", NextOrder);
        AlwaysShowQuickSlotsInUI = config("3 - Quick Slots", "Always Show All Slots", On, "Display all available quick slots on the HUD. When disabled, only shows slots up to the highest occupied one.", NextOrder);
        QuickSlotsPerRow = config("3 - Quick Slots", "Slots Per Row", 8, new ConfigDescription("How many quick slots to show per row. Set to your total slots for horizontal layout, or use lower values (3-4) for vertical stacking.", new AcceptableValueRange<int>(1, 8)), NextOrder, false);

        /* 4 - Special Equipment Slots */
        ResetConfigOrder();
        WishboneSlot = config("4 - Special Equipment Slots", "Enable Wishbone Slot", On, "Adds a dedicated equipment slot specifically for the Wishbone. When equipped here, the Wishbone's detection works without occupying utility slots.", NextOrder);
        WispLightSlot = config("4 - Special Equipment Slots", "Enable Demister Slot", On, "Adds a dedicated equipment slot specifically for the Demister/Wisplight. Keeps the mist clear without using utility slots.", NextOrder);

        /* 4.5 - Equipment Slot Management */
        ResetConfigOrder();

        _ = config("4.5 - Equipment Slot Management", "Slot Manager (UI)", "",
            new ConfigDescription("If you are using the Configuration Manager, this will be a custom drawer. Use this for easier slot management with buttons and prefab browser.\n" +
                                  "If you aren't, edit the configs below directly",
                null,
                new ConfigurationManagerAttributes { CustomDrawer = EquipmentSlotsConfigDrawer, Order = NextOrder }),
            NextOrder);

        RemovedEquipmentSlots = config("4.5 - Equipment Slot Management", "Removed Equipment Slots", "",
            new ConfigDescription(
                "Comma or semicolon-separated list of equipment slot names to remove from the game.\n" +
                "WARNING: Removing slots will unequip items in those slots!\n" +
                "Example: Head, Utility, Trinket\n" +
                "Built-in slots: Head, Chest, Legs, Back, Utility, Trinket, Wishbone, Demister",
                null,
                new ConfigurationManagerAttributes { Order = NextOrder, Browsable = false }),
            NextOrder);

        UserAddedSlots = config("4.5 - Equipment Slot Management", "Custom Equipment Slots", "",
            new ConfigDescription(
                "Add custom equipment slots. Format: SlotName:PrefabName1,PrefabName2;SlotName2:Prefab3\n" +
                "Semicolon separates different slots. Colon separates slot name from prefabs. Comma separates multiple prefabs for one slot.\n" +
                "Example: Ring:RingIron,RingGold;Quiver:QuiverBone,QuiverLeather",
                null,
                new ConfigurationManagerAttributes { Order = NextOrder, Browsable = false }),
            NextOrder);

        /* 5 - UI Features */
        ResetConfigOrder();
        OldLayout = config("5 - UI Features", "Use Legacy Layout", Off, "Reverts to the old inventory layout from previous versions. Only enable if you prefer the classic style or have compatibility issues.", NextOrder);
        VanityOption = config("5 - UI Features", "Show Vanity Button", On, "Shows the vanity button (👔) in the inventory. Use this to customize your character's appearance with cosmetic overrides.", NextOrder);
        LoadoutOption = config("5 - UI Features", "Show Loadout Button", On, "Shows the loadout button (🎯) in the inventory. Use this to save and quickly swap between different equipment sets.", NextOrder);
        string defaultStats = string.Join(",",
            Enum.GetValues(typeof(PlayerStatType))
                .Cast<PlayerStatType>()
                .Where(stat => stat != PlayerStatType.Count)
                .Select(stat => stat.ToString()));
        SelectedPlayerStats = config("5 - UI Features", "Player Stats to Display", defaultStats,
            new ConfigDescription("Choose which character stats to display in the stats panel (📋 button). Use the config manager UI to select/deselect stats.", null, new ConfigurationManagerAttributes { CustomDrawer = StatsConfigDrawer }),
            NextOrder, false);

        string defaultLiveStats = string.Join(",",
            Enum.GetValues(typeof(LiveStatType))
                .Cast<LiveStatType>()
                .Select(stat => stat.ToString()));
        SelectedLiveStats = config("5 - UI Features", "Live Stats to Display", defaultLiveStats,
            new ConfigDescription("Choose which live character stats to display in the stats panel (📋 button). These include health, damage modifiers, resistances, etc. Use the config manager UI to select/deselect stats.", null, new ConfigurationManagerAttributes { CustomDrawer = LiveStatsConfigDrawer }),
            NextOrder, false);

        /* 6 - Equipment Slot Labels */
        ResetConfigOrder();
        HelmetText = config("6 - Equipment Slot Labels", "Head Slot Label", "", "Customize the display text for the helmet/head equipment slot. Use a localization key (starts with $) to use translations, or set custom text.", NextOrder, false);
        ChestText = config("6 - Equipment Slot Labels", "Chest Slot Label", "", "Customize the display text for the chest armor equipment slot. Use a localization key (starts with $) to use translations, or set custom text.", NextOrder, false);
        LegsText = config("6 - Equipment Slot Labels", "Legs Slot Label", "", "Customize the display text for the leg armor equipment slot. Use a localization key (starts with $) to use translations, or set custom text.", NextOrder, false);
        BackText = config("6 - Equipment Slot Labels", "Back Slot Label", "", "Customize the display text for the cape/back equipment slot. Use a localization key (starts with $) to use translations, or set custom text.", NextOrder, false);
        UtilityText = config("6 - Equipment Slot Labels", "Utility Slot Label", "", "Customize the display text for the utility equipment slot. Use a localization key (starts with $) to use translations, or set custom text.", NextOrder, false);
        TrinketText = config("6 - Equipment Slot Labels", "Trinket Slot Label", "", "Customize the display text for the trinket equipment slot. Use a localization key (starts with $) to use translations, or set custom text.", NextOrder, false);

        /* 7 - Quick Slots Customization */
        ResetConfigOrder();
        QuickAccessScale = config("7 - Quick Slots Customization", "HUD Size", 1f, "Scale/size multiplier for the quick slots bar on your HUD. 1.0 = default size, 0.5 = half size, 2.0 = double size.", NextOrder, false);
        QuickAccessLocation = config("7 - Quick Slots Customization", "HUD Position", Vector2.one, "Screen position of the quick slots bar. Use the drag keys (default: CTRL+LeftClick) to reposition, or set to (9999, 9999) for automatic positioning.", NextOrder, false);
        QuickslotDragKeys = config("7 - Quick Slots Customization", "Drag to Reposition Keys", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key combination to drag and reposition the quick slots bar on screen. Default: Hold CTRL and drag with left mouse button.", NextOrder, false);

        /* 8.5 - Panel Toggle Keys (Gamepad) */
        ResetConfigOrder();
        VanityToggleGamepadKey = config("8.5 - Panel Toggle Keys (Gamepad)", "Vanity Panel Toggle Key", KeyCode.JoystickButton8, "Gamepad button to toggle the Vanity panel. Default: Left Stick Press (JoyLStick).", NextOrder, false);
        LoadoutToggleGamepadKey = config("8.5 - Panel Toggle Keys (Gamepad)", "Loadout Panel Toggle Key", KeyCode.JoystickButton9, "Gamepad button to toggle the Loadout panel. Default: Right Stick Press (JoyRStick).", NextOrder, false);
        StatsToggleGamepadKey = config("8.5 - Panel Toggle Keys (Gamepad)", "Stats Panel Toggle Key", KeyCode.JoystickButton3, "Gamepad button to toggle the Stats panel. Default: Y (JoyButtonY).", NextOrder, false);

        /* 9 - Additional Features */
        ResetConfigOrder();
        MakeDropAllButton = config("9 - Additional Features", "Enable Drop All Button", Off, "Adds a 'Drop All' button to your inventory for quickly dropping all items. USE WITH CAUTION!", NextOrder, false);
        DropAllButtonPosition = config("9 - Additional Features", "Drop All Button Position", new Vector2(880.00f, 10.00f), "Position of the Drop All button in the inventory window (X, Y coordinates).", NextOrder, false);

        InitSlotsAndKeys();

        SetupEventHandlers();

        _harmony.PatchAll();
        InitializeConfigWatcher();

        if (WishboneSlot.Value.isOn() && !IsSlotMarkedForRemoval("$item_wishbone"))
        {
            API.AddSlot("$item_wishbone", "Wishbone", 5);
        }

        if (WispLightSlot.Value.isOn() && !IsSlotMarkedForRemoval("$item_demister"))
        {
            API.AddSlot("$item_demister", "Demister", WishboneSlot.Value.isOn() ? 6 : 5);
        }

        int index = slots.Count - QuickSlotsAmount.Value;
        API.UpdateSlots(index, 1);
        slots.Insert(index, new Model.EquipmentSlot { Name = TrinketText.Value, OriginalName = "$azu_epi_trinket", IsQuickSlot = false, Get = player => player.m_trinketItem, Valid = item => item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket });
        SlotHelpers.ResizeSlots();

        SlotBackupManager.InitializeBuiltInSlotBackups();
        ApplySlotChanges();
        //API.RelocalizeSlots();
        Localization.OnLanguageChange += new Action(API.RelocalizeSlots);
        Localizer.OnLocalizationComplete += new Action(API.RelocalizeSlots);
    }

    internal void FullRebuild()
    {
        if (Player.m_localPlayer == null || InventoryGui.instance == null)
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("FullRebuild skipped - player or inventory not initialized");
            return;
        }

        try
        {
            InventoryGuiPatches.UpdateInventory_Patch.RebuildQuickslots();
            SlotHelpers.ResizeSlots();
            Layout.UpdateInventorySize();
            InventoryHealth.FixHiddenItems();
            SlotHelpers.UpdateEquipmentBackgroundAnchors();
            RebuildUI();
            QuickAccessBar.ForceRefresh();
            API.RelocalizeSlots();
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"Error during FullRebuild: {ex.Message}\n{ex.StackTrace}");
        }
    }

    internal static void RefreshPanelButtonBindings()
    {
        if (VanityPanelController.VanityButtonGo != null)
        {
            PanelUtilities.UpdateButtonBinding(VanityPanelController.VanityButtonGo, VanityToggleGamepadKey.Value);
        }

        if (PersonalLoadoutGui.LoadoutsToggleButton != null)
        {
            PanelUtilities.UpdateButtonBinding(PersonalLoadoutGui.LoadoutsToggleButton, LoadoutToggleGamepadKey.Value);
        }

        if (StatsPanelController.StatsButtonGo != null)
        {
            PanelUtilities.UpdateButtonBinding(StatsPanelController.StatsButtonGo, StatsToggleGamepadKey.Value);
        }
    }

    private void Start()
    {
        EAQ.CheckRandy();
        WeightBase.CheckWeightBase();

        Localizer.OnLocalizationComplete += () =>
        {
            AdvBackpacksCompat.Init();
            JudesEquipmentCompat.Init();
            //RustyBagsCompat.Init();
            Hunter_LegacyCompat.Init();
            WizardryCompat.Init();
            EpicLootCompat.Init();
            JewelcraftingCompat.Init();
        };

        ArmoireCompat.CheckForArmoire();
        ZenUICompat.DisableEquipmentSlots();
    }

    private void OnDestroy()
    {
        if (_cfgWatcher != null)
        {
            _cfgWatcher.EnableRaisingEvents = false;
            _cfgWatcher.Dispose();
        }

        if (_debounce != null)
        {
            _debounce.Stop();
            _debounce.Dispose();
        }

        try
        {
            Localization.OnLanguageChange -= new Action(API.RelocalizeSlots);
            Localizer.OnLocalizationComplete -= new Action(API.RelocalizeSlots);
        }
        catch
        {
            /* Already unsubscribed */
        }

        JewelcraftingCompat.Cleanup();

        // Clear all config SettingChanged events to prevent memory leaks
        // Note: I can't unsubscribe lambdas directly, so I clear all handlers
        try
        {
            if (QuickSlotsAmount != null) ClearSettingChangedEvent(QuickSlotsAmount);
            if (ExtraRows != null) ClearSettingChangedEvent(ExtraRows);
            if (AddEquipmentRow != null) ClearSettingChangedEvent(AddEquipmentRow);
            if (DisplayEquipmentRowSeparate != null) ClearSettingChangedEvent(DisplayEquipmentRowSeparate);
            if (ShowQuickSlots != null) ClearSettingChangedEvent(ShowQuickSlots);
            if (SelectedPlayerStats != null) ClearSettingChangedEvent(SelectedPlayerStats);
            if (QuickSlotsPerRow != null) ClearSettingChangedEvent(QuickSlotsPerRow);
            if (RemovedEquipmentSlots != null) ClearSettingChangedEvent(RemovedEquipmentSlots);
            if (UserAddedSlots != null) ClearSettingChangedEvent(UserAddedSlots);
            if (WishboneSlot != null) ClearSettingChangedEvent(WishboneSlot);
            if (WispLightSlot != null) ClearSettingChangedEvent(WispLightSlot);
            if (VanityOption != null) ClearSettingChangedEvent(VanityOption);
            if (LoadoutOption != null) ClearSettingChangedEvent(LoadoutOption);
            if (VanityToggleGamepadKey != null) ClearSettingChangedEvent(VanityToggleGamepadKey);
            if (LoadoutToggleGamepadKey != null) ClearSettingChangedEvent(LoadoutToggleGamepadKey);
            if (StatsToggleGamepadKey != null) ClearSettingChangedEvent(StatsToggleGamepadKey);
            if (OldLayout != null) ClearSettingChangedEvent(OldLayout);

            if (HotkeyTexts != null)
            {
                foreach (var hotkeyText in HotkeyTexts)
                {
                    if (hotkeyText != null) ClearSettingChangedEvent(hotkeyText);
                }
            }
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error during event cleanup: {ex.Message}");
        }

        Config.Save();
    }

    private static void ClearSettingChangedEvent<T>(ConfigEntry<T> configEntry)
    {
        // Use reflection to clear the SettingChanged event, bypasses c# limitation with lambda unsubscription
        FieldInfo? eventField = typeof(ConfigEntry<T>).GetField("SettingChanged", BindingFlags.Instance | BindingFlags.Public);
        if (eventField != null)
        {
            eventField.SetValue(configEntry, null);
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
    public static ConfigEntry<string> RemovedEquipmentSlots = null!;
    public static ConfigEntry<string> UserAddedSlots = null!;

    public static ConfigEntry<Toggle> AddEquipmentRow = null!;
    public static ConfigEntry<Toggle> DisplayEquipmentRowSeparate = null!;
    public static ConfigEntry<int> QuickSlotsAmount = null!;
    public static ConfigEntry<Toggle> ShowQuickSlots = null!;
    public static ConfigEntry<Toggle> MakeDropAllButton = null!;
    public static ConfigEntry<Vector2> DropAllButtonPosition = null!;
    public static ConfigEntry<string> SelectedPlayerStats = null!;
    public static ConfigEntry<string> SelectedLiveStats = null!;
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

    public static ConfigEntry<Vector2> QuickAccessLocation = null!;
    public static ConfigEntry<int> QuickSlotsPerRow = null!;
    public static ConfigEntry<Toggle> AlwaysShowQuickSlotsInUI = null!;

    public static ConfigEntry<Vector2> UIAnchor = null!;
    public static ConfigEntry<Vector3> LocalScale = null!;

    public static ConfigEntry<Toggle> VanityOption = null!;
    public static ConfigEntry<Toggle> LoadoutOption = null!;
    public static ConfigEntry<Toggle> OldLayout = null!;

    public static ConfigEntry<KeyCode> VanityToggleGamepadKey = null!;
    public static ConfigEntry<KeyCode> LoadoutToggleGamepadKey = null!;
    public static ConfigEntry<KeyCode> StatsToggleGamepadKey = null!;

    internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    internal ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, int order, bool synchronizedSetting = true)
    {
        ConfigurationManagerAttributes attributes = new() { Order = order };
        object[] tags = description.Tags.Length > 0 ? description.Tags.Append(attributes).ToArray() : [attributes];
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    internal ConfigEntry<T> config<T>(string group, string name, T value, string description, int order, bool synchronizedSetting = true)
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

    private static void StatsConfigDrawer(ConfigEntryBase entry)
    {
        PlayerStatType[] allStats = (PlayerStatType[])Enum.GetValues(typeof(PlayerStatType));
        allStats = allStats.Where(x => x != PlayerStatType.Count).ToArray();
        List<PlayerStatType> selectedStats = ParseStatsList(SelectedPlayerStats.Value);

        GUILayout.Space(5);

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
        {
            SelectedPlayerStats.Value = string.Join(",", allStats.Select(s => s.ToString()));
        }

        if (GUILayout.Button("Clear All", GUILayout.ExpandWidth(false)))
        {
            SelectedPlayerStats.Value = "";
        }

        GUILayout.EndHorizontal();
        int columns = 3;
        int itemsPerColumn = Mathf.CeilToInt(allStats.Length / (float)columns);

        GUILayout.BeginHorizontal();
        for (int col = 0; col < columns; ++col)
        {
            GUILayout.BeginVertical();
            int startIdx = col * itemsPerColumn;
            int endIdx = Math.Min(startIdx + itemsPerColumn, allStats.Length);

            for (int i = startIdx; i < endIdx && i < allStats.Length; ++i)
            {
                PlayerStatType stat = allStats[i];
                bool isSelected = selectedStats.Contains(stat);
                bool newValue = GUILayout.Toggle(isSelected, stat.ToString(), GUILayout.ExpandWidth(false));

                if (newValue == isSelected) continue;
                if (newValue)
                    selectedStats.Add(stat);
                else
                    selectedStats.Remove(stat);

                SelectedPlayerStats.Value = string.Join(",", selectedStats.Select(s => s.ToString()));
            }

            GUILayout.EndVertical();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public static List<PlayerStatType> ParseStatsList(string statsString)
    {
        List<PlayerStatType> result = [];
        if (string.IsNullOrWhiteSpace(statsString))
            return result;

        string[] statNames = statsString.Split([','], StringSplitOptions.RemoveEmptyEntries);
        foreach (string statName in statNames)
        {
            if (Enum.TryParse(statName.Trim(), out PlayerStatType stat))
                result.Add(stat);
        }

        return result;
    }

    private static void LiveStatsConfigDrawer(ConfigEntryBase entry)
    {
        LiveStatType[] allStats = (LiveStatType[])Enum.GetValues(typeof(LiveStatType));
        List<LiveStatType> selectedStats = ParseLiveStatsList(SelectedLiveStats.Value);

        GUILayout.Space(5);

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
        {
            SelectedLiveStats.Value = string.Join(",", allStats.Select(s => s.ToString()));
        }

        if (GUILayout.Button("Clear All", GUILayout.ExpandWidth(false)))
        {
            SelectedLiveStats.Value = "";
        }

        GUILayout.EndHorizontal();

        int columns = 3;
        int itemsPerColumn = Mathf.CeilToInt(allStats.Length / (float)columns);

        GUILayout.BeginHorizontal();
        for (int col = 0; col < columns; ++col)
        {
            GUILayout.BeginVertical();
            int startIdx = col * itemsPerColumn;
            int endIdx = Math.Min(startIdx + itemsPerColumn, allStats.Length);

            for (int i = startIdx; i < endIdx && i < allStats.Length; ++i)
            {
                LiveStatType stat = allStats[i];
                bool isSelected = selectedStats.Contains(stat);
                bool newValue = GUILayout.Toggle(isSelected, FormatLiveStatName(stat), GUILayout.ExpandWidth(false));

                if (newValue == isSelected) continue;
                if (newValue)
                    selectedStats.Add(stat);
                else
                    selectedStats.Remove(stat);

                SelectedLiveStats.Value = string.Join(",", selectedStats.Select(s => s.ToString()));
            }

            GUILayout.EndVertical();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public static List<LiveStatType> ParseLiveStatsList(string statsString)
    {
        List<LiveStatType> result = [];
        if (string.IsNullOrWhiteSpace(statsString))
            return result;

        string[] statNames = statsString.Split([','], StringSplitOptions.RemoveEmptyEntries);
        foreach (string statName in statNames)
        {
            if (Enum.TryParse(statName.Trim(), out LiveStatType stat))
                result.Add(stat);
        }

        return result;
    }

    private static string FormatLiveStatName(LiveStatType stat)
    {
        string name = stat.ToString();
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
    }

    #region Equipment Slots Config Drawer

    private static string _newSlotName = "";
    private static string _newSlotPrefabs = "";
    private static Vector2 _slotsScrollPosition = Vector2.zero;
    private static Vector2 _prefabScrollPosition = Vector2.zero;
    private static bool _showPrefabList = false;

    private static void EquipmentSlotsConfigDrawer(ConfigEntryBase entry)
    {
        List<string> allSlots = GetAllCurrentSlots();
        List<string> removedSlots = ParseSlotList(RemovedEquipmentSlots.Value);
        List<string> builtInSlots = GetBuiltInSlotNames();
        List<string> userAddedSlotNames = GetUserAddedSlotNames();

        GUILayout.Space(5);
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("REMOVE EQUIPMENT SLOTS", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(5);

        GUILayout.Label("Click to remove slots (WARNING: unequips items!):", GUILayout.ExpandWidth(true));
        GUILayout.Space(5);

        _slotsScrollPosition = GUILayout.BeginScrollView(_slotsScrollPosition, GUI.skin.box, GUILayout.Height(200));

        foreach (string slot in allSlots)
        {
            bool isRemoved = removedSlots.Contains(slot);
            bool isBuiltIn = builtInSlots.Contains(slot);
            bool isUserAdded = userAddedSlotNames.Contains(slot);

            GUILayout.BeginHorizontal();

            Color oldColor = GUI.backgroundColor;
            if (isRemoved) GUI.backgroundColor = Color.red;

            if (GUILayout.Button(isRemoved ? "✓ Removed" : "Remove", GUILayout.Width(100)))
            {
                if (isRemoved)
                {
                    removedSlots.Remove(slot);
                    RemovedEquipmentSlots.Value = string.Join(", ", removedSlots.Distinct().OrderBy(s => s));
                }
                else
                {
                    if (isUserAdded)
                    {
                        // For user-added slots: just delete them entirely (don't add to removal list)
                        List<string> userSlots = ParseUserAddedSlots();
                        userSlots.RemoveAll(s => s.StartsWith(slot.Trim() + ":"));
                        UserAddedSlots.Value = string.Join(";", userSlots.Distinct());
                    }
                    else
                    {
                        if (!removedSlots.Contains(slot))
                            removedSlots.Add(slot);
                        RemovedEquipmentSlots.Value = string.Join(", ", removedSlots.Distinct().OrderBy(s => s));
                    }
                }
            }

            GUI.backgroundColor = oldColor;

            GUILayout.Label($"{slot}", GUILayout.ExpandWidth(false));
            if (isBuiltIn) GUILayout.Label("(Built-in)", new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Italic }, GUILayout.ExpandWidth(false));
            else if (isUserAdded) GUILayout.Label("(Your Custom)", new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Italic, normal = new GUIStyleState { textColor = Color.cyan } }, GUILayout.ExpandWidth(false));
            else GUILayout.Label("(API/Mod)", new GUIStyle(GUI.skin.label) { fontSize = 10 }, GUILayout.ExpandWidth(false));

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.Space(15);

        GUILayout.Label("ADD CUSTOM EQUIPMENT SLOTS", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Slot Name:", GUILayout.Width(80));
        _newSlotName = GUILayout.TextField(_newSlotName, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Prefab Names:", GUILayout.Width(80));
        _newSlotPrefabs = GUILayout.TextField(_newSlotPrefabs, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("?", GUILayout.Width(25)))
        {
            _showPrefabList = !_showPrefabList;
        }

        GUILayout.EndHorizontal();

        GUILayout.Label("(Comma-separated list of item prefab names, e.g., 'HelmetBronze,HelmetIron')", new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Italic });

        if (_showPrefabList)
        {
            GUILayout.Space(5);
            GUILayout.Label("Available Item Prefabs:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            _prefabScrollPosition = GUILayout.BeginScrollView(_prefabScrollPosition, GUI.skin.box, GUILayout.Height(150));

            List<string> prefabs = GetAvailableItemPrefabs();
            foreach (string prefab in prefabs)
            {
                if (!GUILayout.Button(prefab, GUILayout.ExpandWidth(false))) continue;
                if (string.IsNullOrEmpty(_newSlotPrefabs))
                    _newSlotPrefabs = prefab;
                else if (!_newSlotPrefabs.Contains(prefab))
                    _newSlotPrefabs += ", " + prefab;
            }

            GUILayout.EndScrollView();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Add Slot", GUILayout.Height(30)))
        {
            if (!string.IsNullOrWhiteSpace(_newSlotName) && !string.IsNullOrWhiteSpace(_newSlotPrefabs))
            {
                string trimmedSlotName = _newSlotName.Trim();
                string slotEntry = $"{trimmedSlotName}:{_newSlotPrefabs.Trim()}";
                List<string> userSlots = ParseUserAddedSlots();

                if (removedSlots.Contains(trimmedSlotName))
                {
                    removedSlots.Remove(trimmedSlotName);
                    RemovedEquipmentSlots.Value = string.Join(", ", removedSlots.Distinct().OrderBy(s => s));
                }

                userSlots.RemoveAll(s => s.StartsWith(trimmedSlotName + ":"));
                userSlots.Add(slotEntry);
                UserAddedSlots.Value = string.Join(";", userSlots.Distinct());

                _newSlotName = "";
                _newSlotPrefabs = "";
            }
        }

        List<string> currentUserSlots = ParseUserAddedSlots();
        if (currentUserSlots.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"YOUR CUSTOM SLOTS ({currentUserSlots.Count})", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(3);
            GUILayout.Label("Valid item prefabs for each custom slot:", new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(5);

            foreach (string userSlot in currentUserSlots)
            {
                string[] parts = userSlot.Split(':');
                if (parts.Length != 2) continue;

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"• {parts[0]}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                GUILayout.Label($"   Accepts: {parts[1]}", new GUIStyle(GUI.skin.label) { fontSize = 10, normal = new GUIStyleState { textColor = new Color(0.7f, 0.7f, 0.7f) } });
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }

            GUILayout.Space(3);
            GUILayout.Label("(Use 'Remove Equipment Slots' section above to remove slots)", new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter });
        }

        GUILayout.Space(15);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("RESET TO DEFAULTS", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(5);
        GUILayout.Label("This will:", new GUIStyle(GUI.skin.label) { fontSize = 10 });
        GUILayout.Label("  • Remove all your custom slots", new GUIStyle(GUI.skin.label) { fontSize = 10 });
        GUILayout.Label("  • Restore all hidden built-in slots", new GUIStyle(GUI.skin.label) { fontSize = 10 });
        GUILayout.Label("  • Keep API/Mod-added slots (from other mods)", new GUIStyle(GUI.skin.label) { fontSize = 10 });
        GUILayout.Space(5);

        Color oldBgColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
        {
            UserAddedSlots.Value = "";
            RemovedEquipmentSlots.Value = "";
            _newSlotName = "";
            _newSlotPrefabs = "";
            _showPrefabList = false;
        }

        GUI.backgroundColor = oldBgColor;
        GUILayout.EndVertical();

        GUILayout.Space(5);
        GUILayout.EndVertical();
    }

    private static List<string> GetAllCurrentSlots()
    {
        List<string> curslots = [];

        try
        {
            foreach (Model.Slot? slot in slots)
            {
                if (slot == null || slot.IsQuickSlot) continue;
                if (slot is not Model.EquipmentSlot equipSlot) continue;
                string name = equipSlot.OriginalName ?? equipSlot.Name;
                if (!string.IsNullOrEmpty(name))
                    curslots.Add(name);
            }
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting current slots: {ex.Message}");
        }

        return curslots.Distinct().ToList();
    }

    public static List<string> GetBuiltInSlotNames()
    {
        return ["$azu_epi_helmet", "$azu_epi_chest", "$azu_epi_legs", "$azu_epi_shoulder", "$azu_epi_utility", "$item_wishbone", "$item_demister", "$azu_epi_trinket"];
    }

    private static List<string> GetUserAddedSlotNames()
    {
        return ParseUserAddedSlots().Select(s => s.Split(':')[0].Trim())
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private static List<string> GetAvailableItemPrefabs()
    {
        List<string> prefabs = [];

        try
        {
            if (ObjectDB.instance != null && ObjectDB.instance.m_items != null)
            {
                foreach (GameObject itemPrefab in ObjectDB.instance.m_items)
                {
                    if (itemPrefab != null)
                    {
                        prefabs.Add(itemPrefab.name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting item prefabs: {ex.Message}");
        }

        return prefabs.OrderBy(p => p).ToList();
    }

    private static List<string> ParseSlotList(string slotsString)
    {
        List<string> result = [];
        if (string.IsNullOrWhiteSpace(slotsString))
            return result;

        string[] slotNames = slotsString.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string slotName in slotNames)
        {
            string trimmed = slotName.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }

        return result;
    }

    private static List<string> ParseUserAddedSlots()
    {
        List<string> result = [];
        if (string.IsNullOrWhiteSpace(UserAddedSlots?.Value))
            return result;

        string[] entries = UserAddedSlots.Value.Split([';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            string trimmed = entry.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }

        return result;
    }

    internal static bool IsSlotMarkedForRemoval(string slotName)
    {
        if (string.IsNullOrWhiteSpace(RemovedEquipmentSlots?.Value))
            return false;

        List<string> removedSlots = ParseSlotList(RemovedEquipmentSlots.Value);

        if (removedSlots.Contains(slotName))
            return true;

        if (Localization.instance == null) return false;
        if (slotName.StartsWith("$"))
        {
            string localizedName = Localization.instance.Localize(slotName);
            if (removedSlots.Contains(localizedName))
                return true;
        }
        else
        {
            string tokenName = "$item_" + slotName.ToLower();
            if (removedSlots.Contains(tokenName) || removedSlots.Contains(Localization.instance.Localize(tokenName)))
                return true;
        }

        return false;
    }

    internal static void ApplySlotChanges()
    {
        try
        {
            List<string> removedSlots = ParseSlotList(RemovedEquipmentSlots?.Value ?? "");
            HashSet<string> slotsToRemove = [];

            foreach (string backupKey in SlotBackupManager.GetBackupKeys())
            {
                if (SlotBackupManager._userConfigSlotNames.Contains(backupKey))
                    continue;

                bool shouldBeRemoved = removedSlots.Contains(backupKey);

                if (!shouldBeRemoved && Localization.instance != null)
                {
                    if (backupKey.StartsWith("$"))
                    {
                        string localized = Localization.instance.Localize(backupKey);
                        shouldBeRemoved = removedSlots.Contains(localized);
                    }
                    else
                    {
                        string tokenName = "$item_" + backupKey.ToLower();
                        shouldBeRemoved = removedSlots.Contains(tokenName) ||
                                          removedSlots.Contains(Localization.instance.Localize(tokenName));
                    }
                }

                if (!shouldBeRemoved)
                {
                    Model.Slot? existingSlot = slots.FirstOrDefault(s =>
                        (s is Model.EquipmentSlot es && es.OriginalName == backupKey) || s?.Name == backupKey);
                    if (existingSlot != null && removedSlots.Contains(existingSlot.Name))
                    {
                        shouldBeRemoved = true;
                    }
                }

                if (shouldBeRemoved)
                {
                    slotsToRemove.Add(backupKey);
                }
                else if (SlotBackupManager.RestoreSlotFromBackup(backupKey))
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"Restored slot '{backupKey}' from backup");
                }
            }

            foreach (string backupKey in slotsToRemove)
            {
                if (API.RemoveSlot(backupKey))
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"Removed slot: {backupKey}");
                }
            }

            foreach (string slotName in removedSlots)
            {
                if (slotsToRemove.Contains(slotName)) continue;

                bool removed = API.RemoveSlot(slotName);

                if (!removed && Localization.instance != null)
                {
                    if (slotName.StartsWith("$"))
                    {
                        string localizedName = Localization.instance.Localize(slotName);
                        if (localizedName != slotName)
                        {
                            removed = API.RemoveSlot(localizedName);
                        }
                    }
                    else
                    {
                        string tokenName = "$item_" + slotName.ToLower();
                        removed = API.RemoveSlot(tokenName);
                    }
                }

                if (removed)
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"Removed slot: {slotName}");
                }
            }

            List<string> userSlots = ParseUserAddedSlots();
            HashSet<string> newUserSlotNames = [];

            foreach (string userSlot in userSlots)
            {
                string[] parts = userSlot.Split(':');
                if (parts.Length == 2)
                {
                    newUserSlotNames.Add(parts[0].Trim());
                }
            }

            foreach (string oldSlotName in SlotBackupManager._userConfigSlotNames.ToList())
            {
                if (!newUserSlotNames.Contains(oldSlotName) && API.RemoveSlot(oldSlotName))
                {
                    AzuExtendedPlayerInventoryLogger.LogInfo($"Removed user slot '{oldSlotName}' (no longer in config)");
                }
            }

            SlotBackupManager._userConfigSlotNames.Clear();
            SlotBackupManager._userConfigSlotNames.UnionWith(newUserSlotNames);

            foreach (string userSlot in userSlots)
            {
                string[] parts = userSlot.Split(':');
                if (parts.Length != 2) continue;

                string slotName = parts[0].Trim();
                string[] prefabs = parts[1].Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();

                if (string.IsNullOrEmpty(slotName) || prefabs.Length == 0) continue;

                if (IsSlotMarkedForRemoval(slotName))
                {
                    AzuExtendedPlayerInventoryLogger.LogWarning($"User slot '{slotName}' is in both add and remove lists - skipping addition (remove takes precedence)");
                    continue;
                }

                if (!API.TryGetSlotIndexByName(slotName, out _, false))
                {
                    if (prefabs.Length == 1)
                        API.AddSlot(slotName, prefabs[0]);
                    else
                        API.AddSlot(slotName, prefabs);

                    AzuExtendedPlayerInventoryLogger.LogInfo($"Added user slot: {slotName} with prefabs: {string.Join(", ", prefabs)}");
                }
                else
                {
                    AzuExtendedPlayerInventoryLogger.LogDebug($"User slot '{slotName}' already exists, skipping addition");
                }
            }
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"Error applying slot changes: {ex.Message}");
        }
    }

    #endregion

    #endregion
}