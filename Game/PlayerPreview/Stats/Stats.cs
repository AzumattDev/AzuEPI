namespace AzuEPI.Game.PlayerPreview.Stats;

public class StatsUI
{
    private static RectTransform? _statsRect;
    private static readonly List<StatElement> _statElements = new();
    private static TMP_FontAsset? _fontAsset;
    private static bool _isHovering;

    public static RectTransform CreateUI(InventoryGui inventoryGui, RectTransform previewParentRect)
    {
        TMP_Text? fontSample = inventoryGui.m_craftButton?.GetComponentInChildren<TMP_Text>();
        if (fontSample != null)
            _fontAsset = fontSample.font;

        GameObject statsPanel = new GameObject("StatsUI", typeof(RectTransform), typeof(Image));
        _statsRect = statsPanel.GetComponent<RectTransform>();
        _statsRect.SetParent(previewParentRect, false);

        _statsRect.anchorMin = new Vector2(0f, 0f);
        _statsRect.anchorMax = new Vector2(1f, 0f);
        _statsRect.pivot = new Vector2(0.5f, 0f);
        _statsRect.anchoredPosition = Vector2.zero;
        _statsRect.sizeDelta = new Vector2(0f, 220f);

        Image bgImage = statsPanel.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.8f);
        bgImage.raycastTarget = false;

        statsPanel.SetActive(false);

        GameObject contentContainer = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        contentRect.SetParent(_statsRect, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(8f, 8f);
        contentRect.offsetMax = new Vector2(-8f, -8f);

        GridLayoutGroup gridLayout = contentContainer.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.spacing = new Vector2(6f, 6f);
        gridLayout.cellSize = new Vector2(85f, 48f);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        CreateStatElement(contentContainer, "Kills", "⚔️", PlayerStatType.EnemyKills);
        CreateStatElement(contentContainer, "Deaths", "💀", PlayerStatType.Deaths);
        CreateStatElement(contentContainer, "Arrows", "🏹", PlayerStatType.ArrowsShot);

        CreateStatElement(contentContainer, "Builds", "🏗️", PlayerStatType.Builds);
        CreateStatElement(contentContainer, "Crafts", "🔨", PlayerStatType.Crafts);
        CreateStatElement(contentContainer, "Upgrades", "⬆️", PlayerStatType.Upgrades);

        CreateStatElement(contentContainer, "Distance", "🗺️", PlayerStatType.DistanceTraveled);
        CreateStatElement(contentContainer, "Portals", "🌀", PlayerStatType.PortalsUsed);
        CreateStatElement(contentContainer, "Jumps", "🦘", PlayerStatType.Jumps);

        CreateStatElement(contentContainer, "Trees", "🌲", PlayerStatType.TreeChops);
        CreateStatElement(contentContainer, "Mines", "⛏️", PlayerStatType.MineHits);
        CreateStatElement(contentContainer, "Food", "🍖", PlayerStatType.FoodEaten);

        return _statsRect;
    }

    private static void CreateStatElement(GameObject parent, string statName, string icon, PlayerStatType statType)
    {
        GameObject statObj = new GameObject($"Stat_{statName}", typeof(RectTransform));
        RectTransform statRect = statObj.GetComponent<RectTransform>();
        statRect.SetParent(parent.transform, false);

        GameObject contentColumn = new GameObject("ContentColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
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

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
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

        GameObject valueObj = new GameObject("Value", typeof(RectTransform));
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

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
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

        _statElements.Add(new StatElement
        {
            Name = statName,
            StatType = statType,
            ValueText = valueText,
            IconText = iconText,
            LabelText = labelText
        });
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
                    element.ValueText.text = $"{(statValue / 1000f):F1}km";
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

    private class StatElement
    {
        public string Name { get; set; } = string.Empty;
        public PlayerStatType StatType { get; set; }
        public TextMeshProUGUI ValueText { get; set; } = null!;
        public TextMeshProUGUI IconText { get; set; } = null!;
        public TextMeshProUGUI LabelText { get; set; } = null!;
    }
}
