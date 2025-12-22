using AzuEPI.Game.Loadout;
using AzuEPI.Game.Vanity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AzuEPI.Game.PlayerPreview.Stats;

public static class StatsPanelController
{
    public const string StatsPanelName = "StatsPanel";
    public const string StatsScrollRootName = "ScrollRoot";
    public const string StatsViewportName = "Viewport";
    public const string StatsContentName = "Content";
    public const string StatsScrollbarName = "Scrollbar";
    public const string StatsToggleButtonName = "AzuEPIStatsToggleButton";

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
    private static readonly List<StatElement> _statElements = new();

    public static RectTransform? ToggleButtonParentGlg;

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
        { PlayerStatType.CraftsOrUpgrades, "Craft/Up" },
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

        TMP_Text? fontSample = gui.m_craftButton?.GetComponentInChildren<TMP_Text>();
        if (fontSample != null)
            _fontAsset = fontSample.font;

        BuildPanel(gui);
        BuildScrollTree();
        PopulateStats();

        _panel?.gameObject.SetActive(false);
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
        BuildContentGrid(_viewport);

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
        img.color = new Color(0, 0, 0, 0.85f);
        img.raycastTarget = true;

        return rt;
    }

    private static void BuildContentGrid(RectTransform parent)
    {
        GameObject go = new(StatsContentName,
            typeof(RectTransform),
            typeof(GridLayoutGroup),
            typeof(ContentSizeFitter));

        _content = (RectTransform)go.transform;
        _content.SetParent(parent, false);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);

        GridLayoutGroup? grid = go.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.spacing = Spacing;
        grid.cellSize = CellSize;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(12, 12, 12, 12);

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

        List<PlayerStatType> selectedStats = ParseStatsList(SelectedPlayerStats.Value);
        if (selectedStats.Count == 0)
            selectedStats = GetDefaultStats();

        foreach (PlayerStatType statType in selectedStats)
        {
            string icon = StatIcons.TryGetValue(statType, out string? statIcon) ? statIcon : "📊";
            string label = StatLabels.TryGetValue(statType, out string? statLabel) ? statLabel : statType.ToString();
            CreateStatElement(_content.gameObject, label, icon, statType);
        }
    }

    private static List<PlayerStatType> GetDefaultStats()
    {
        return new List<PlayerStatType>
        {
            PlayerStatType.EnemyKills, PlayerStatType.Deaths, PlayerStatType.ArrowsShot, PlayerStatType.EnemyHits,
            PlayerStatType.HitsTakenEnemies, PlayerStatType.PlayerKills, PlayerStatType.PlayerHits, PlayerStatType.BossKills,
            PlayerStatType.Builds, PlayerStatType.Crafts, PlayerStatType.Upgrades, PlayerStatType.ItemsPickedUp,
            PlayerStatType.DistanceTraveled, PlayerStatType.DistanceWalk, PlayerStatType.DistanceRun, PlayerStatType.DistanceSail,
            PlayerStatType.TreeChops, PlayerStatType.MineHits, PlayerStatType.FoodEaten, PlayerStatType.PortalsUsed,
            PlayerStatType.Jumps, PlayerStatType.Sleep, PlayerStatType.TimeInBase, PlayerStatType.TimeOutOfBase
        };
    }

    private static void CreateStatElement(GameObject parent, string statName, string icon, PlayerStatType statType)
    {
        GameObject statObj = new($"Stat_{statName}", typeof(RectTransform));
        RectTransform statRect = statObj.GetComponent<RectTransform>();
        statRect.SetParent(parent.transform, false);

        GameObject contentColumn = new("ContentColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform columnRect = contentColumn.GetComponent<RectTransform>();
        columnRect.SetParent(statRect, false);
        columnRect.anchorMin = Vector2.zero;
        columnRect.anchorMax = Vector2.one;
        columnRect.offsetMin = Vector2.zero;
        columnRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vLayout = contentColumn.GetComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.spacing = 1f;
        vLayout.padding = new RectOffset(2, 2, 2, 2);
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;

        GameObject iconObj = new("Icon", typeof(RectTransform));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.SetParent(columnRect, false);

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) iconText.font = _fontAsset;
        iconText.text = icon;
        iconText.fontSize = 16f;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = Color.white;
        iconText.raycastTarget = false;

        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredHeight = 18f;

        GameObject valueObj = new("Value", typeof(RectTransform));
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.SetParent(columnRect, false);

        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) valueText.font = _fontAsset;
        valueText.text = "0";
        valueText.fontSize = 13f;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        valueText.raycastTarget = false;
        valueText.fontStyle = FontStyles.Bold;

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 16f;

        GameObject labelObj = new("Label", typeof(RectTransform));
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(columnRect, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) labelText.font = _fontAsset;
        labelText.text = statName;
        labelText.fontSize = 9f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        labelText.raycastTarget = false;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 12f;

        _statElements.Add(new StatElement { Name = statName, StatType = statType, ValueText = valueText, IconText = iconText, LabelText = labelText });
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
                var craftingPanel = InventoryGui.instance.m_crafting;
                craftingPanel.transform.Find("TabsButtons").SafeSetActive(!_visible);
                craftingPanel.transform.Find("RecipeList").SafeSetActive(!_visible);
                craftingPanel.transform.Find("Decription").SafeSetActive(!_visible);
            }
        });

        if (StatsButtonGo.TryGetComponent<UIGamePad>(out var gp))
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

    private static Transform CloneButton(Transform src, Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
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
        public TextMeshProUGUI IconText { get; set; } = null!;
        public TextMeshProUGUI LabelText { get; set; } = null!;
    }
}
