using AzuEPI.EPI;

namespace AzuEPI.Core.Text;

public class SlotText
{
    public static void Set(string value, Transform transform, bool center = true)
    {
        Transform transform1 = transform.Find("binding");
        if (!transform1)
            transform1 = Object.Instantiate(ExtendedPlayerInventory._elementPrefab.transform.Find("binding"), transform);
        var textComp = transform1.GetComponent<TMP_Text>();
        textComp.enabled = true;
        textComp.overflowMode = TextOverflowModes.Overflow;
        textComp.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        textComp.fontSizeMin = 10f;
        textComp.fontSizeMax = 18f;
        textComp.enableAutoSizing = true;
        textComp.text = value;
        if (!center)
            return;
        transform1.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 17f);
        transform1.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -10f);
    }
}