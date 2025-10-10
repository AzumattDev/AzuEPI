namespace AzuEPI.Core.Slots;

internal static class SlotOverlays
{
    private static readonly Dictionary<GameObject, GameObject> InvalidByGo = new();
    private static Sprite? checkSprite;

    public static GameObject EnsureInvalidOverlay(GameObject slotGo)
    {
        if (InvalidByGo.TryGetValue(slotGo, out var overlay) && overlay)
            return overlay;

        if (!checkSprite)
        {
            checkSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == "mapicon_checked");
        }

        var rt = slotGo.GetComponent<RectTransform>();

        var ovBkg = new GameObject("EPI_InvalidOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var ovBkgRT = (RectTransform)ovBkg.transform;
        ovBkgRT.SetParent(rt, false);
        ovBkgRT.anchorMin = Vector2.zero;
        ovBkgRT.anchorMax = Vector2.one;
        ovBkgRT.offsetMin = Vector2.zero;
        ovBkgRT.offsetMax = Vector2.zero;
        ovBkgRT.pivot = new Vector2(0.5f, 0.5f);

        var imgBkg = ovBkg.GetComponent<Image>();
        imgBkg.raycastTarget = false;
        imgBkg.color = new Color(0f, 0f, 0f, 0.95f);

        var ov = new GameObject("EPI_InvalidOverlayCheck", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var ovRT = (RectTransform)ov.transform;
        ovRT.SetParent(ovBkgRT, false);
        ovRT.anchorMin = Vector2.zero;
        ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = Vector2.zero;
        ovRT.offsetMax = Vector2.zero;
        ovRT.pivot = new Vector2(0.5f, 0.5f);

        var img = ov.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = checkSprite;

        ovBkg.SetActive(false);
        InvalidByGo[slotGo] = ovBkg;
        return ovBkg;
    }

    public static void SetInvalidVisible(GameObject slotGo, bool visible)
    {
        if (!InvalidByGo.TryGetValue(slotGo, out var ov) || !ov)
            ov = EnsureInvalidOverlay(slotGo);

        if (ov.activeSelf != visible)
            ov.SetActive(visible);
    }
}