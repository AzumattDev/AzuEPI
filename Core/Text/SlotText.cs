using AzuEPI.EPI;

namespace AzuEPI.Core.Text;

public class SlotText
{
    public static void Set(string value, Transform transform)
    {
        Transform transform1 = transform.Find("binding");
        if (!transform1)
            transform1 = Object.Instantiate(_elementPrefab.transform.Find("binding"), transform);
        if (!transform1.TryGetComponent(out RectTransform rectTransform)) return;
        rectTransform.WithAnchorMin(Vector2.zero).WithAnchorMax(Vector2.one).WithOffsetMin(Vector2.zero).WithOffsetMax(Vector2.zero).WithSizeDelta(Vector2.zero).WithAnchoredPosition(Vector2.zero);

        TMP_Text? textComp = transform1.GetComponent<TMP_Text>();
        textComp.enabled = true;
        textComp.overflowMode = TextOverflowModes.Overflow;
        textComp.textWrappingMode = TextWrappingModes.Normal;
        textComp.fontSizeMin = 10f;
        textComp.fontSizeMax = 14f;
        textComp.enableAutoSizing = true;
        textComp.text = value;
        textComp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        textComp.verticalAlignment = VerticalAlignmentOptions.Top;
    }
}