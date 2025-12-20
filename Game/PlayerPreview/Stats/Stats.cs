namespace AzuEPI.Game.PlayerPreview.Stats;

public class StatsUI
{
    private static RectTransform? _statsRect;
    private static readonly List<StatElement> _statElements = new();
    private static TMP_FontAsset? _fontAsset;
    private static bool _isHovering;

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

    public static RectTransform CreateUI(InventoryGui inventoryGui, RectTransform previewParentRect)
    {
        TMP_Text? fontSample = inventoryGui.m_craftButton?.GetComponentInChildren<TMP_Text>();
        if (fontSample != null)
            _fontAsset = fontSample.font;

        _statElements.Clear();

        List<PlayerStatType> selectedStats = ParseStatsList(SelectedPlayerStats.Value);
        if (selectedStats.Count == 0)
            selectedStats = GetDefaultStats();

        int columns = Math.Min(4, selectedStats.Count);
        int rows = (int)Math.Ceiling(selectedStats.Count / (float)columns);
        float height = Math.Max(120f, rows * 55f + 16f);

        GameObject statsPanel = new("StatsUI", typeof(RectTransform), typeof(Image));
        _statsRect = statsPanel.GetComponent<RectTransform>();
        _statsRect.SetParent(previewParentRect, false);

        _statsRect.anchorMin = new Vector2(0f, 0f);
        _statsRect.anchorMax = new Vector2(1f, 0f);
        _statsRect.pivot = new Vector2(0.5f, 0f);
        _statsRect.anchoredPosition = Vector2.zero;
        _statsRect.sizeDelta = new Vector2(0f, height);

        Image bgImage = statsPanel.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.95f);
        bgImage.raycastTarget = false;

        statsPanel.SetActive(false);

        GameObject contentContainer = new("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        contentRect.SetParent(_statsRect, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(8f, 8f);
        contentRect.offsetMax = new Vector2(-8f, -8f);

        GridLayoutGroup gridLayout = contentContainer.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.spacing = new Vector2(5f, 5f);
        gridLayout.cellSize = new Vector2(65f, 50f);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        foreach (PlayerStatType statType in selectedStats)
        {
            string icon = StatIcons.TryGetValue(statType, out string? statIcon) ? statIcon : "📊";
            string label = StatLabels.TryGetValue(statType, out string? statLabel) ? statLabel : statType.ToString();
            CreateStatElement(contentContainer, label, icon, statType);
        }

        return _statsRect;
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

    public static void SetHoverState(bool isHovering)
    {
        _isHovering = isHovering;
        if (_statsRect != null)
            _statsRect.gameObject.SetActive(isHovering);
    }

    public static void RebuildUI(InventoryGui inventoryGui, RectTransform? previewParentRect)
    {
        if (previewParentRect == null) return;

        if (_statsRect != null)
        {
            Object.Destroy(_statsRect.gameObject);
            _statsRect = null;
        }

        _statElements.Clear();

        CreateUI(inventoryGui, previewParentRect);

        if (Player.m_localPlayer != null)
            UpdateStats(Player.m_localPlayer);
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