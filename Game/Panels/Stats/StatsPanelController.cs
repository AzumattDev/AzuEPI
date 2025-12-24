using AzuEPI.Game.Loadout;
using AzuEPI.Game.Vanity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AzuEPI.Game.PlayerPreview.Stats;

public static class StatsPanelController
{
    public const string StatsPanelName = $"{Prefix}StatsPanel";
    public const string StatsScrollRootName = "StatsPanelScrollRoot";
    public const string StatsViewportName = "StatsPanelViewport";
    public const string StatsContentName = "StatsPanelContent";
    public const string StatsScrollbarName = "StatsPanelScrollbar";
    public const string StatsToggleButtonName = $"{Prefix}StatsToggleButton";

    private static readonly Vector2 ToggleBtnAnchorMin = new(0f, 1f);
    private static readonly Vector2 ToggleBtnAnchorMax = new(0f, 1f);
    private static readonly Vector2 ToggleBtnPivot = new(0f, 1f);
    private static readonly Vector2 ToggleBtnSize = new(90f, 32f);

    private static readonly Vector2 ScrollRootOffsetMin = new(10f, 10f);
    private static readonly Vector2 ScrollRootOffsetMax = new(-10f, -10f);

    private static readonly Vector2 BarAnchorMin = new(1f, 0f);
    private static readonly Vector2 BarAnchorMax = new(1f, 1f);
    private static readonly Vector2 BarPivot = new(1f, 0.5f);
    private static readonly Vector2 BarOffsetMin = new(-20f, 10f);
    private static readonly Vector2 BarOffsetMax = new(-10f, -10f);

    private static readonly Vector2 CellSize = new(85f, 70f);
    private static readonly Vector2 Spacing = new(6f, 6f);
    private const int Columns = 4;

    private static bool _visible;
    private static RectTransform? _panel;
    private static ScrollRect? _scroll;
    private static RectTransform? _viewport;
    private static RectTransform? _content;
    private static Scrollbar? _vbar;
    private static Button? _toggleBtn;
    private static Transform? StatsButtonGo;
    private static TMP_FontAsset? _fontAsset;
    private static Transform? _tabBorderTemplate;
    private static readonly List<StatElement> _statElements = new();

    public static RectTransform? ToggleButtonParentGlg;

    // TODO: Maybe allow pinning specific stats to player preview again.
    private static readonly Dictionary<PlayerStatType, string> StatIcons = new()
    {
        { PlayerStatType.EnemyKills, "⚔️" },
        { PlayerStatType.Deaths, "💀" },
        { PlayerStatType.ArrowsShot, "🏹" },
        { PlayerStatType.EnemyHits, "🎯" },
        { PlayerStatType.HitsTakenEnemies, "🛡️" },
        { PlayerStatType.PlayerKills, "⚔️" },
        { PlayerStatType.PlayerHits, "💥" },
        { PlayerStatType.BossKills, "👹" },
        { PlayerStatType.Builds, "🏗️" },
        { PlayerStatType.Crafts, "🔨" },
        { PlayerStatType.Upgrades, "⬆️" },
        { PlayerStatType.ItemsPickedUp, "📦" },
        { PlayerStatType.DistanceTraveled, "🗺️" },
        { PlayerStatType.DistanceWalk, "🚶" },
        { PlayerStatType.DistanceRun, "🏃" },
        { PlayerStatType.DistanceSail, "⛵" },
        { PlayerStatType.DistanceAir, "✈️" },
        { PlayerStatType.TreeChops, "🌲" },
        { PlayerStatType.MineHits, "⛏️" },
        { PlayerStatType.FoodEaten, "🍖" },
        { PlayerStatType.PortalsUsed, "🌀" },
        { PlayerStatType.Jumps, "🦘" },
        { PlayerStatType.Sleep, "😴" },
        { PlayerStatType.TimeInBase, "🏠" },
        { PlayerStatType.TimeOutOfBase, "🧭" },
        { PlayerStatType.CraftsOrUpgrades, "🔧" },
        { PlayerStatType.Cheats, "💻" },
        { PlayerStatType.WorldLoads, "🌍" },
        { PlayerStatType.CreatureTamed, "🐺" },
        { PlayerStatType.DoorsOpened, "🚪" },
        { PlayerStatType.BeesHarvested, "🐝" },
    };

    private static readonly Dictionary<PlayerStatType, string> StatLabels = new()
    {
        { PlayerStatType.EnemyKills, "Kills" },
        { PlayerStatType.EnemyHits, "Hits" },
        { PlayerStatType.HitsTakenEnemies, "Hit Taken" },
        { PlayerStatType.PlayerKills, "PvP Kills" },
        { PlayerStatType.PlayerHits, "PvP Hits" },
        { PlayerStatType.BossKills, "Bosses" },
        { PlayerStatType.ItemsPickedUp, "Items" },
        { PlayerStatType.DistanceTraveled, "Distance" },
        { PlayerStatType.DistanceWalk, "Walked" },
        { PlayerStatType.DistanceRun, "Run" },
        { PlayerStatType.DistanceSail, "Sailed" },
        { PlayerStatType.DistanceAir, "Air" },
        { PlayerStatType.TreeChops, "Trees" },
        { PlayerStatType.MineHits, "Mines" },
        { PlayerStatType.FoodEaten, "Food" },
        { PlayerStatType.PortalsUsed, "Portals" },
        { PlayerStatType.TimeInBase, "In Base" },
        { PlayerStatType.TimeOutOfBase, "Explored" },
        { PlayerStatType.CraftsOrUpgrades, "Craft/Upgrades" },
        { PlayerStatType.WorldLoads, "Loads" },
        { PlayerStatType.CreatureTamed, "Tamed" },
        { PlayerStatType.DoorsOpened, "Doors" },
        { PlayerStatType.BeesHarvested, "Bees" },
    };

    public static bool IsVisible() => _visible;

    public static void SetVisible(bool visible)
    {
        _visible = visible;
        if (_panel) _panel.gameObject.SetActive(visible);
        if (visible && _panel)
        {
            _panel.SetAsLastSibling();
            if (Player.m_localPlayer != null)
            {
                UpdateStats(Player.m_localPlayer);
            }
        }
    }

    public static void Hide() => SetVisible(false);

    public static void EnsureBuilt(InventoryGui gui)
    {
        if (_panel) return;

        TMP_Text? fontSample = gui.m_craftButton?.GetComponentInChildren<TMP_Text>()
                             ?? gui.m_takeAllButton?.GetComponentInChildren<TMP_Text>()
                             ?? gui.m_info?.GetComponentInChildren<TMP_Text>();

        if (fontSample != null && fontSample.font != null)
        {
            _fontAsset = fontSample.font;
        }
        else
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("Could not find TMP font asset for stats panel. Text may not display correctly.");
        }

        _tabBorderTemplate = gui.m_crafting.transform.Find("TabsButtons/TabBorder");

        BuildPanel(gui);
        BuildScrollTree();
        PopulateStats();

        SelectedPlayerStats.SettingChanged += OnStatsConfigChanged;

        _panel?.gameObject.SetActive(false);
    }

    private static void OnStatsConfigChanged(object sender, EventArgs e)
    {
        RebuildStats();
    }

    public static void RebuildStats()
    {
        if (!_content) return;

        foreach (Transform child in _content)
        {
            Object.Destroy(child.gameObject);
        }

        PopulateStats();

        if (_visible && Player.m_localPlayer != null)
        {
            UpdateStats(Player.m_localPlayer);
        }
    }

    private static void BuildPanel(InventoryGui gui)
    {
        RectTransform? crafting = gui.m_crafting;
        Transform? srcBkg = crafting.Find("Bkg");
        if (!srcBkg) return;

        Transform? statsPanel = Object.Instantiate(srcBkg, crafting);
        statsPanel.name = StatsPanelName;

        _panel = statsPanel.GetComponent<RectTransform>();
        RectTransform? srcRT = srcBkg.GetComponent<RectTransform>();
        _panel.anchorMin = srcRT.anchorMin;
        _panel.anchorMax = srcRT.anchorMax;
        _panel.SetAsLastSibling();

        Image? img = statsPanel.GetComponent<Image>();
        if (img) img.raycastTarget = false;
    }

    private static void BuildScrollTree()
    {
        if (!_panel) return;

        GameObject scrollRoot;
        StoreGui? store = StoreGui.instance;
        if (store && store.GetComponentInChildren<ScrollRect>())
        {
            GameObject src = store.GetComponentInChildren<ScrollRect>().gameObject;
            scrollRoot = Object.Instantiate(src, _panel);
        }
        else
        {
            scrollRoot = new GameObject(StatsScrollRootName, typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(_panel, false);
        }

        RectTransform rootRT = (RectTransform)scrollRoot.transform;
        AnchorFill(rootRT);
        rootRT.offsetMin = ScrollRootOffsetMin;
        rootRT.offsetMax = ScrollRootOffsetMax;

        _scroll = scrollRoot.GetComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.inertia = true;
        _scroll.decelerationRate = 0.135f;
        _scroll.scrollSensitivity = 800f;

        _viewport = BuildViewport(rootRT);
        BuildContentStack(_viewport);

        _scroll.viewport = _viewport;
        _scroll.content = _content;

        _vbar = BuildScrollbar(_panel);
        _scroll.verticalScrollbar = _vbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    private static RectTransform BuildViewport(RectTransform parent)
    {
        GameObject go = new(StatsViewportName, typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        AnchorFill(rt);

        Image? img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.565f);
        img.raycastTarget = true;

        return rt;
    }

    private static void BuildContentStack(RectTransform parent)
    {
        GameObject go = new(StatsContentName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

        _content = (RectTransform)go.transform;
        _content.SetParent(parent, false);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.offsetMin = Vector2.zero;
        _content.offsetMax = Vector2.zero;

        VerticalLayoutGroup? vLayout = go.GetComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.spacing = 8f;
        vLayout.padding = new RectOffset(16, 16, 16, 16);
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        ContentSizeFitter? fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static Scrollbar BuildScrollbar(Transform panelParent)
    {
        GameObject barGO;
        if (InventoryGui.instance.m_recipeListScroll)
        {
            barGO = Object.Instantiate(InventoryGui.instance.m_recipeListScroll.gameObject, panelParent);
            barGO.name = StatsScrollbarName;
        }
        else
        {
            barGO = new GameObject(StatsScrollbarName, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGO.transform.SetParent(panelParent, false);
        }

        RectTransform barRT = (RectTransform)barGO.transform;
        barRT.anchorMin = BarAnchorMin;
        barRT.anchorMax = BarAnchorMax;
        barRT.pivot = BarPivot;
        barRT.offsetMin = BarOffsetMin;
        barRT.offsetMax = BarOffsetMax;

        Scrollbar? bar = barGO.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;

        Image? bg = bar.GetComponent<Image>();
        if (bg) bg.enabled = true;

        Image? handle = bar.transform.Find("Sliding Area/Handle")?.GetComponent<Image>();
        if (handle) handle.enabled = true;
        else
        {
            GameObject h = new("Handle", typeof(RectTransform), typeof(Image));
            RectTransform hRT = (RectTransform)h.transform;
            hRT.SetParent(barRT, false);
            AnchorFill(hRT);
            bar.targetGraphic = h.GetComponent<Image>();
            bar.handleRect = hRT;
        }

        return bar;
    }

    private static void PopulateStats()
    {
        if (!_content) return;

        _statElements.Clear();

        CreateLiveStatsSection();

        List<PlayerStatType> selectedStats = ParseStatsList(SelectedPlayerStats.Value);
        if (selectedStats.Count == 0)
            selectedStats = GetDefaultStats();

        List<PlayerStatType> generalStats = new();
        List<PlayerStatType> combatStats = new();
        List<PlayerStatType> explorationStats = new();
        List<PlayerStatType> activityStats = new();
        List<PlayerStatType> otherStats = new();

        foreach (PlayerStatType stat in selectedStats)
        {
            switch (stat)
            {
                case PlayerStatType.WorldLoads:
                case PlayerStatType.Deaths:
                case PlayerStatType.Cheats:
                    generalStats.Add(stat);
                    break;

                case PlayerStatType.EnemyKills:
                case PlayerStatType.BossKills:
                case PlayerStatType.EnemyHits:
                case PlayerStatType.HitsTakenEnemies:
                case PlayerStatType.ArrowsShot:
                case PlayerStatType.PlayerKills:
                case PlayerStatType.PlayerHits:
                    combatStats.Add(stat);
                    break;

                case PlayerStatType.DistanceTraveled:
                case PlayerStatType.DistanceWalk:
                case PlayerStatType.DistanceRun:
                case PlayerStatType.DistanceSail:
                case PlayerStatType.DistanceAir:
                case PlayerStatType.Jumps:
                case PlayerStatType.PortalsUsed:
                    explorationStats.Add(stat);
                    break;

                case PlayerStatType.Builds:
                case PlayerStatType.Crafts:
                case PlayerStatType.Upgrades:
                case PlayerStatType.CraftsOrUpgrades:
                case PlayerStatType.ItemsPickedUp:
                case PlayerStatType.TreeChops:
                case PlayerStatType.MineHits:
                case PlayerStatType.FoodEaten:
                case PlayerStatType.Sleep:
                    activityStats.Add(stat);
                    break;

                default:
                    otherStats.Add(stat);
                    break;
            }
        }

        if (generalStats.Count > 0)
            CreateSection("General", generalStats.ToArray());
        if (combatStats.Count > 0)
            CreateSection("Combat", combatStats.ToArray());
        if (explorationStats.Count > 0)
            CreateSection("Exploration", explorationStats.ToArray());
        if (activityStats.Count > 0)
            CreateSection("Activity", activityStats.ToArray());
        if (otherStats.Count > 0)
            CreateSection("Other", otherStats.ToArray());
    }

    private static void CreateLiveStatsSection()
    {
        if (!_content) return;

        GameObject headerObj = new("Section_Attributes", typeof(RectTransform));
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.SetParent(_content, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            headerText.font = _fontAsset;
            headerText.fontSharedMaterial = _fontAsset.material;
        }
        headerText.text = "Attributes";
        headerText.fontSize = 20f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.color = new Color(1f, 0.84f, 0f, 1f);
        headerText.raycastTarget = false;

        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 20f;
        headerLayout.minHeight = 20f;

        CreateLiveStatRow("Health");
        CreateLiveStatRow("Stamina");
        CreateLiveStatRow("Eitr");
        CreateLiveStatRow("Stamina Regen");
        CreateLiveStatRow("Eitr Regen");
        CreateLiveStatRow("Movement Speed");
        CreateLiveStatRow("Run Speed");
        CreateLiveStatRow("Swim Speed");

        GameObject spacer = new("Spacer_Attributes", typeof(RectTransform));
        RectTransform spacerRect = spacer.GetComponent<RectTransform>();
        spacerRect.SetParent(_content, false);
        LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 8f;
        spacerLayout.minHeight = 8f;
    }

    private static void CreateLiveStatRow(string statName)
    {
        GameObject rowObj = new($"LiveStatRow_{statName}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.SetParent(_content, false);

        HorizontalLayoutGroup hLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = 4f;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = false;

        LayoutElement rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 18f;
        rowLayout.minHeight = 18f;

        GameObject labelObj = new("Label", typeof(RectTransform));
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(rowRect, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            labelText.font = _fontAsset;
            labelText.fontSharedMaterial = _fontAsset.material;
        }
        labelText.text = statName;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        labelText.raycastTarget = false;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;
        labelLayout.minWidth = 100f;

        GameObject valueObj = new("Value", typeof(RectTransform));
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.SetParent(rowRect, false);

        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            valueText.font = _fontAsset;
            valueText.fontSharedMaterial = _fontAsset.material;
        }
        valueText.text = "0";
        valueText.fontSize = 18f;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        valueText.fontStyle = FontStyles.Bold;

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.minWidth = 60f;
        valueLayout.preferredWidth = 60f;

        _statElements.Add(new StatElement { Name = statName, StatType = (PlayerStatType)(-1), ValueText = valueText, IsLiveStat = true });

        CreateSeparator();
    }

    private static void CreateSeparator()
    {
        if (!_content || !_tabBorderTemplate) return;

        GameObject separator = Object.Instantiate(_tabBorderTemplate.gameObject, _content);
        separator.name = "Separator";
        RectTransform sepRT = separator.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0f, 0f);
        sepRT.anchorMax = new Vector2(1f, 0f);
        sepRT.pivot = new Vector2(0.5f, 0.5f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        LayoutElement sepLayout = separator.AddComponent<LayoutElement>();
        sepLayout.preferredHeight = 2f;
        sepLayout.minHeight = 2f;

        Image? img = separator.GetComponent<Image>();
        if (img)
        {
            Color c = img.color;
            c.a = 0.3f;
            img.color = c;
        }
    }

    private static void CreateSection(string title, PlayerStatType[] stats)
    {
        if (!_content) return;

        GameObject headerObj = new($"Section_{title}", typeof(RectTransform));
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.SetParent(_content, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            headerText.font = _fontAsset;
            headerText.fontSharedMaterial = _fontAsset.material;
        }
        headerText.text = title;
        headerText.fontSize = 20f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.color = new Color(1f, 0.84f, 0f, 1f);
        headerText.raycastTarget = false;

        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 20f;
        headerLayout.minHeight = 20f;

        foreach (PlayerStatType statType in stats)
        {
            string label = StatLabels.TryGetValue(statType, out string? statLabel) ? statLabel : statType.ToString();
            CreateStatRow(_content.gameObject, label, statType);
        }

        GameObject spacer = new($"Spacer_{title}", typeof(RectTransform));
        RectTransform spacerRect = spacer.GetComponent<RectTransform>();
        spacerRect.SetParent(_content, false);
        LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 8f;
        spacerLayout.minHeight = 8f;
    }

    private static List<PlayerStatType> GetDefaultStats()
    {
        return Enum.GetValues(typeof(PlayerStatType))
            .Cast<PlayerStatType>()
            .Where(stat => stat != PlayerStatType.Count)
            .ToList();
    }

    private static void CreateStatRow(GameObject parent, string statName, PlayerStatType statType)
    {
        GameObject rowObj = new($"StatRow_{statName}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.SetParent(parent.transform, false);

        HorizontalLayoutGroup hLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = 4f;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        LayoutElement rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 18f;
        rowLayout.minHeight = 18f;

        GameObject labelObj = new("Label", typeof(RectTransform));
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(rowRect, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            labelText.font = _fontAsset;
            labelText.fontSharedMaterial = _fontAsset.material;
        }
        labelText.text = statName;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        labelText.raycastTarget = false;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;
        labelLayout.minWidth = 100f;

        GameObject valueObj = new("Value", typeof(RectTransform));
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.SetParent(rowRect, false);

        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            valueText.font = _fontAsset;
            valueText.fontSharedMaterial = _fontAsset.material;
        }
        valueText.text = "0";
        valueText.fontSize = 18f;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        valueText.fontStyle = FontStyles.Bold;

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.minWidth = 60f;
        valueLayout.preferredWidth = 60f;

        _statElements.Add(new StatElement { Name = statName, StatType = statType, ValueText = valueText });

        CreateSeparator();
    }

    internal static void BuildToggleButton(InventoryGui gui)
    {
        Transform? src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src || ToggleButtonParentGlg == null) return;

        StatsButtonGo = CloneButton(src, ToggleButtonParentGlg, StatsToggleButtonName, ToggleBtnAnchorMin, ToggleBtnAnchorMax, ToggleBtnPivot, new Vector2(-50f, -30f), ToggleBtnSize);
        Button? btn = StatsButtonGo.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            _visible = !_visible;
            SetVisible(_visible);
            if (VanityPanelController.IsVisible()) VanityPanelController.SetVisible(false);
            if (PersonalLoadoutGui.IsVisible()) PersonalLoadoutGui.Hide();
            if (InventoryGui.instance)
            {
                RectTransform? craftingPanel = InventoryGui.instance.m_crafting;
                craftingPanel.transform.Find("TabsButtons").SafeSetActive(!_visible);
                craftingPanel.transform.Find("RecipeList").SafeSetActive(!_visible);
                craftingPanel.transform.Find("Decription").SafeSetActive(!_visible);
            }
        });

        if (StatsButtonGo.TryGetComponent<UIGamePad>(out UIGamePad? gp))
        {
            if (ZInput.instance != null)
            {
                gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text = ZInput.instance.GetBoundKeyString("JoyTabLeft", true);
            }
            else
            {
                ZInput.Initialize();
                gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text = ZInput.instance.GetBoundKeyString("JoyTabLeft", true);
            }

            gp.m_zinputKey = "JoyTabLeft";
            gp.m_keyCode = KeyCode.JoystickButton4;
        }

        TMP_Text? label = StatsButtonGo.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.text = "📋";
            label.fontSize = 20;
        }

        _toggleBtn = btn;
        StatsButtonGo.gameObject.SetActive(true);
    }

    public static void UpdateStats(Player player)
    {
        if (player == null || _statElements.Count == 0) return;

        PlayerProfile? profile = global::Game.instance?.GetPlayerProfile();
        if (profile == null) return;

        foreach (StatElement element in _statElements)
        {
            if (element.IsLiveStat)
            {
                switch (element.Name)
                {
                    case "Health":
                        element.ValueText.text = $"{player.GetHealth():F0} / {player.GetMaxHealth():F0}";
                        break;
                    case "Stamina":
                        element.ValueText.text = $"{player.GetStamina():F0} / {player.GetMaxStamina():F0}";
                        break;
                    case "Eitr":
                        element.ValueText.text = $"{player.GetEitr():F0} / {player.GetMaxEitr():F0}";
                        break;
                    case "Stamina Regen":
                        element.ValueText.text = $"{player.m_staminaRegen:F1}/s";
                        break;
                    case "Eitr Regen":
                        element.ValueText.text = $"{player.m_eiterRegen:F1}/s";
                        break;
                    case "Movement Speed":
                        element.ValueText.text = $"{player.GetJogSpeedFactor() * 100:F0}%";
                        break;
                    case "Run Speed":
                        element.ValueText.text = $"{player.GetRunSpeedFactor() * 100:F0}%";
                        break;
                    case "Swim Speed":
                        element.ValueText.text = $"{player.m_swimSpeed * player.GetAttackSpeedFactorMovement():F0}%";
                        break;
                }
                continue;
            }

            float statValue = profile.m_playerStats[element.StatType];

            switch (element.StatType)
            {
                case PlayerStatType.DistanceTraveled:
                case PlayerStatType.DistanceWalk:
                case PlayerStatType.DistanceRun:
                case PlayerStatType.DistanceSail:
                case PlayerStatType.DistanceAir:
                    element.ValueText.text = statValue >= 1000f ? $"{(statValue / 1000f):F1}km" : $"{statValue:F0}m";
                    break;

                case PlayerStatType.TimeInBase:
                case PlayerStatType.TimeOutOfBase:
                case PlayerStatType.Sleep:
                    if (statValue >= 86400)
                        element.ValueText.text = $"{(statValue / 86400f):F1}d";
                    else if (statValue >= 3600)
                        element.ValueText.text = $"{(statValue / 3600f):F1}h";
                    else if (statValue >= 60)
                        element.ValueText.text = $"{(statValue / 60f):F0}m";
                    else
                        element.ValueText.text = $"{statValue:F0}s";
                    break;

                default:
                    if (statValue >= 1000000)
                        element.ValueText.text = $"{statValue / 1000000f:F1}M";
                    else if (statValue >= 10000)
                        element.ValueText.text = $"{statValue / 1000f:F1}k";
                    else
                        element.ValueText.text = statValue.ToString("N0");
                    break;
            }
        }
    }

    private static Transform CloneButton(Transform src, Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        Transform clone = Object.Instantiate(src, parent);
        clone.name = name;
        clone.SetAsLastSibling();

        RectTransform rt = (RectTransform)clone;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        return clone;
    }

    private static void AnchorFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private class StatElement
    {
        public string Name { get; set; } = string.Empty;
        public PlayerStatType StatType { get; set; }
        public TextMeshProUGUI ValueText { get; set; } = null!;
        public bool IsLiveStat { get; set; }
    }
}
