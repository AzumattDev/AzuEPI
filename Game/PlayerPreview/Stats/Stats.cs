namespace AzuEPI.Game.PlayerPreview.Stats;

public class StatsUI
{
    private static RectTransform? _statsRect;
    private static readonly List<StatElement> _statElements = new();
    private static TMP_FontAsset? _fontAsset;

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
        _statsRect.sizeDelta = new Vector2(0f, 80f);

        Image bgImage = statsPanel.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f);
        bgImage.raycastTarget = false;

        GameObject contentContainer = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        contentRect.SetParent(_statsRect, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(8f, 8f);
        contentRect.offsetMax = new Vector2(-8f, -8f);

        HorizontalLayoutGroup layout = contentContainer.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        CreateStatElement(contentContainer, "Armor", "🛡️");
        CreateStatElement(contentContainer, "Health", "❤️");
        CreateStatElement(contentContainer, "Stamina", "⚡");
        CreateStatElement(contentContainer, "Eitr", "✨");
        CreateStatElement(contentContainer, "Weight", "⚖️");
        CreateStatElement(contentContainer, "Speed", "👟");

        return _statsRect;
    }

    private static void CreateStatElement(GameObject parent, string statName, string icon)
    {
        GameObject statObj = new GameObject($"Stat_{statName}", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform statRect = statObj.GetComponent<RectTransform>();
        statRect.SetParent(parent.transform, false);

        VerticalLayoutGroup vLayout = statObj.GetComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.spacing = 2f;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.SetParent(statRect, false);

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) iconText.font = _fontAsset;
        iconText.text = icon;
        iconText.fontSize = 20f;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = Color.white;
        iconText.raycastTarget = false;

        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredHeight = 24f;

        GameObject valueObj = new GameObject("Value", typeof(RectTransform));
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.SetParent(statRect, false);

        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) valueText.font = _fontAsset;
        valueText.text = "0";
        valueText.fontSize = 14f;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        valueText.raycastTarget = false;
        valueText.fontStyle = FontStyles.Bold;

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 18f;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(statRect, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset != null) labelText.font = _fontAsset;
        labelText.text = statName;
        labelText.fontSize = 10f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        labelText.raycastTarget = false;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 14f;

        _statElements.Add(new StatElement
        {
            Name = statName,
            ValueText = valueText,
            IconText = iconText,
            LabelText = labelText
        });
    }

    public static void UpdateStats(Player player)
    {
        if (player == null || _statElements.Count == 0) return;

        foreach (StatElement element in _statElements)
        {
            switch (element.Name)
            {
                case "Armor":
                    element.ValueText.text = player.GetBodyArmor().ToString("F0");
                    break;
                case "Health":
                    element.ValueText.text = $"{player.GetHealth():F0}/{player.GetMaxHealth():F0}";
                    break;
                case "Stamina":
                    element.ValueText.text = $"{player.GetStamina():F0}/{player.GetMaxStamina():F0}";
                    break;
                case "Eitr":
                    element.ValueText.text = player.GetMaxEitr() > 0 ? $"{player.GetEitr():F0}/{player.GetMaxEitr():F0}" : "N/A";
                    break;
                case "Weight":
                    Inventory inv = player.GetInventory();
                    float totalWeight = inv.GetTotalWeight();
                    element.ValueText.text = $"{totalWeight:F1}/{player.GetMaxCarryWeight():F0}";
                    float weightRatio = totalWeight / player.GetMaxCarryWeight();
                    element.ValueText.color = weightRatio > 0.9f ? new Color(1f, 0.3f, 0.3f, 1f) :
                                              weightRatio > 0.75f ? new Color(1f, 0.8f, 0.3f, 1f) :
                                              new Color(0.8f, 0.8f, 0.8f, 1f);
                    break;
                case "Speed":
                    float moveSpeed = player.GetJogSpeedFactor() * 100f;
                    element.ValueText.text = $"{moveSpeed:F0}%";
                    break;
            }
        }
    }

    private class StatElement
    {
        public string Name { get; set; } = string.Empty;
        public TextMeshProUGUI ValueText { get; set; } = null!;
        public TextMeshProUGUI IconText { get; set; } = null!;
        public TextMeshProUGUI LabelText { get; set; } = null!;
    }
}
