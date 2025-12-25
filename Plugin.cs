using APIManager;
using AzuEPI.Game.Compatibility;
using AzuEPI.Game.Compatibility.AdvBackpacks;
using AzuEPI.Game.Loadout;
using AzuEPI.Game.Panels.Vanity;
//using AzuEPI.Game.Moveable;
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
    internal const string ModVersion = "2.0.1";
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
        Patcher.Patch(new[]
        {
            "AzuExtendedPlayerInventory",
            "AzuExtendedPlayerInventory.EPI.Patches"
        });

        context = this;

        /* 1 - Server & Sync */
        ResetConfigOrder();
        _serverConfigLocked = config("1 - Server & Sync", "Lock Configuration", On, "When enabled, only server admins can modify configuration settings. All players will use the server's settings.", NextOrder);
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        /* 2 - Inventory */
        ResetConfigOrder();
        ExtraRows = config("2 - Inventory", "Extra Inventory Rows", 0, new ConfigDescription("Add extra rows to your inventory (0-6). WARNING: Adding too many rows may overlap with chest windows. Use CTRL+drag to reposition the inventory if needed.", new AcceptableValueRange<int>(0, 6)), NextOrder);
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

        /* 6 - Equipment Slot Labels */
        ResetConfigOrder();
        HelmetText = config("6 - Equipment Slot Labels", "Head Slot Label", "Head", "Customize the display text for the helmet/head equipment slot.", NextOrder, false);
        ChestText = config("6 - Equipment Slot Labels", "Chest Slot Label", "Chest", "Customize the display text for the chest armor equipment slot.", NextOrder, false);
        LegsText = config("6 - Equipment Slot Labels", "Legs Slot Label", "Legs", "Customize the display text for the leg armor equipment slot.", NextOrder, false);
        BackText = config("6 - Equipment Slot Labels", "Back Slot Label", "Back", "Customize the display text for the cape/back equipment slot.", NextOrder, false);
        UtilityText = config("6 - Equipment Slot Labels", "Utility Slot Label", "Utility", "Customize the display text for the utility equipment slot.", NextOrder, false);
        TrinketText = config("6 - Equipment Slot Labels", "Trinket Slot Label", "Trinket", "Customize the display text for the trinket equipment slot.", NextOrder, false);

        /* 7 - Quick Slots Customization */
        ResetConfigOrder();
        QuickAccessScale = config("7 - Quick Slots Customization", "HUD Size", 1f, "Scale/size multiplier for the quick slots bar on your HUD. 1.0 = default size, 0.5 = half size, 2.0 = double size.", NextOrder, false);
        QuickAccessLocation = config("7 - Quick Slots Customization", "HUD Position", Vector2.one, "Screen position of the quick slots bar. Use the drag keys (default: CTRL+LeftClick) to reposition, or set to (9999, 9999) for automatic positioning.", NextOrder, false);
        QuickslotDragKeys = config("7 - Quick Slots Customization", "Drag to Reposition Keys", new KeyboardShortcut(KeyCode.Mouse0, KeyCode.LeftControl), "Key combination to drag and reposition the quick slots bar on screen. Default: Hold CTRL and drag with left mouse button.", NextOrder, false);

        /* 9 - Additional Features */
        ResetConfigOrder();
        MakeDropAllButton = config("9 - Additional Features", "Enable Drop All Button", Off, "Adds a 'Drop All' button to your inventory for quickly dropping all items. USE WITH CAUTION!", NextOrder, false);
        DropAllButtonPosition = config("9 - Additional Features", "Drop All Button Position", new Vector2(880.00f, 10.00f), "Position of the Drop All button in the inventory window (X, Y coordinates).", NextOrder, false);

        InitializeHotkeys();

        QuickSlotsAmount.SettingChanged += (sender, args) =>
        {
            InitializeHotkeys();
            FullRebuild();
        };
        ExtraRows.SettingChanged += (sender, args) => { FullRebuild(); };
        AddEquipmentRow.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        DisplayEquipmentRowSeparate.SettingChanged += (sender, args) => { EAQ.CheckRandy(); };
        ShowQuickSlots.SettingChanged += (sender, args) => { HotkeyBarController.Hud_Update_Patch.DeselectHotkeyBar(); };
        SelectedPlayerStats.SettingChanged += (sender, args) =>
        {
            // if (InventoryGui.instance != null)
            //     StatsUI.RebuildUI(InventoryGui.instance, PreviewParent?.GetComponent<RectTransform>());
        };
        QuickSlotsPerRow.SettingChanged += (sender, args) =>
        {
            if (!Hud.instance) return;
            Transform hudroot = Hud.instance.transform.Find("hudroot");
            if (!hudroot) return;
            Transform qabTransform = hudroot.Find(QabName);
            if (!qabTransform || !qabTransform.TryGetComponent<HotkeyBar>(out HotkeyBar? qab)) return;
            foreach (HotkeyBar.ElementData? element in qab.m_elements)
                if (element.m_go)
                    Destroy(element.m_go);
            qab.m_elements.Clear();
            FullRebuild();
        };

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

            FullRebuild();
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

            FullRebuild();
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

        OldLayout.SettingChanged += (sender, args) =>
        {
            SlotHelpers.ResizeSlots();
            Layout.UpdateInventorySize();
            Layout.ApplyLayoutCorrections();
            RebuildUI();
            FullRebuild();
            SlotHelpers.UpdateEquipmentBackgroundAnchors();
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

        int index = InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length;
        API.UpdateSlots(index, 1);
        InventoryGuiPatches.UpdateInventory_Patch.slots.Insert(index, new Model.EquipmentSlot { Name = TrinketText.Value, IsQuickSlot = false, Get = player => player.m_trinketItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket });
        SlotHelpers.ResizeSlots();

        Localization.OnLanguageChange += new Action(API.RelocalizeSlots);
    }

    internal void FullRebuild()
    {
        InventoryGuiPatches.UpdateInventory_Patch.RebuildQuickslots();
        SlotHelpers.ResizeSlots();
        Layout.UpdateInventorySize();
        InventoryHealth.FixHiddenItems();
        SlotHelpers.UpdateEquipmentBackgroundAnchors();
        RebuildUI();
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

            if (Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out PluginInfo? randyEl) && randyEl is not null)
            {
                API.AddSlot("$azuepi_fingerslot", new[] { "Andvaranaut", "GoldRubyRing", "SilverRing" });
            }
        };

        ArmoireCompat.CheckForArmoire();
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
        KeyboardShortcut[] defaultKeys = new[]
        {
            new KeyboardShortcut(KeyCode.Z, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.X, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.C, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.V, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.B, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.Alpha1, KeyCode.LeftAlt),
            new KeyboardShortcut(KeyCode.Alpha2, KeyCode.LeftAlt),
        };

        Hotkeys = new ConfigEntry<KeyboardShortcut>[count];
        HotkeyTexts = new ConfigEntry<string>[count];

        for (int i = 0; i < count; ++i)
        {
            KeyboardShortcut keyboardShortcut = i < defaultKeys.Length ? defaultKeys[i] : KeyboardShortcut.Empty;
            Hotkeys[i] = config("8 - Quick Slot Hotkeys", $"Hotkey {i + 1}", keyboardShortcut,
                $"Keyboard shortcut for quick slot {i + 1}. See https://docs.unity3d.com/Manual/ConventionalGameInput.html for valid key names.", false);
            HotkeyTexts[i] = config("8 - Quick Slot Hotkeys", $"Hotkey {i + 1} Display Text", $"Alt + {keyboardShortcut.MainKey.ToString().Replace("Alpha", string.Empty)}",
                $"Custom text to display for quick slot {i + 1} hotkey on the HUD. Leave blank to auto-generate from the hotkey itself.", false);
            HotkeyTexts[i].SettingChanged += (_, _) =>
            {
                InitializeHotkeys();
                FullRebuild();
            };
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
    public static ConfigEntry<string> SelectedPlayerStats = null!;
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
        ConfigurationManagerAttributes attributes = new() { Order = order };
        object[] tags = description.Tags.Length > 0 ? description.Tags.Append(attributes).ToArray() : new object[] { attributes };
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
        List<PlayerStatType> result = new();
        if (string.IsNullOrWhiteSpace(statsString))
            return result;

        string[] statNames = statsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string statName in statNames)
        {
            if (Enum.TryParse(statName.Trim(), out PlayerStatType stat))
                result.Add(stat);
        }
        return result;
    }

    #endregion
}