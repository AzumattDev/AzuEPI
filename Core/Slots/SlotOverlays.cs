using AzuEPI.Game.Vanity;

namespace AzuEPI.Core.Slots;

internal static class SlotOverlays
{
    private static readonly Dictionary<GameObject, GameObject> InvalidByGo = new();
    private static readonly Dictionary<GameObject, GameObject> VanityStateByGo = new();

    private static Sprite? checkSprite;
    private static Sprite? hiddenSprite;
    private static Sprite? hasVanitySprite;

    private const string InvalidRootName = "EPI_InvalidOverlay";
    private const string InvalidSpriteObjectName = "EPI_InvalidOverlayCheck";
    private const string VanityRootName = "EPI_VanityStateOverlay";
    private const string HiddenIconName = "EPI_VanityHidden";
    private const string HasIconName = "EPI_VanityHasVanity";

    private static Sprite? FindSpriteByName(string name)
    {
        return Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == name);
    }

    private static void EnsureSharedSpritesLoaded()
    {
        if (!checkSprite) checkSprite = FindSpriteByName("mapicon_checked");
        if (!hiddenSprite) hiddenSprite = checkSprite ? checkSprite : FindSpriteByName("sneak_hidden");
        if (!hasVanitySprite) hasVanitySprite = FindSpriteByName("staminaupgrade");
    }

    public static GameObject EnsureInvalidOverlay(GameObject slotGo)
    {
        if (InvalidByGo.TryGetValue(slotGo, out var overlay) && overlay)
            return overlay;
        EnsureSharedSpritesLoaded();

        var rt = slotGo.GetComponent<RectTransform>();

        var root = new GameObject(InvalidRootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rootRT = (RectTransform)root.transform;
        rootRT.SetParent(rt, false);
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;
        rootRT.pivot = new Vector2(0.5f, 0.5f);

        var bkg = root.GetComponent<Image>();
        bkg.raycastTarget = false;
        bkg.color = new Color(0f, 0f, 0f, 0.95f);

        var check = new GameObject(InvalidSpriteObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var ovRT = (RectTransform)check.transform;
        ovRT.SetParent(rootRT, false);
        ovRT.anchorMin = Vector2.zero;
        ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = Vector2.zero;
        ovRT.offsetMax = Vector2.zero;
        ovRT.pivot = new Vector2(0.5f, 0.5f);

        var checkImg = check.GetComponent<Image>();
        checkImg.raycastTarget = false;
        checkImg.sprite = checkSprite;

        root.SetActive(false);
        InvalidByGo[slotGo] = root;
        return root;
    }

    public static void SetInvalidVisible(GameObject slotGo, bool visible)
    {
        if (!InvalidByGo.TryGetValue(slotGo, out var ov) || !ov)
            ov = EnsureInvalidOverlay(slotGo);

        if (ov.activeSelf != visible)
            ov.SetActive(visible);
    }

    public static GameObject EnsureVanityStateOverlay(GameObject slotGo)
    {
        if (VanityStateByGo.TryGetValue(slotGo, out var overlay) && overlay)
            return overlay;
        EnsureSharedSpritesLoaded();

        var rt = slotGo.GetComponent<RectTransform>();

        var root = new GameObject(VanityRootName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rootRT = (RectTransform)root.transform;
        rootRT.SetParent(rt, false);
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;
        rootRT.pivot = new Vector2(0.5f, 0.5f);

        var bkg = root.GetComponent<Image>();
        bkg.raycastTarget = false;
        bkg.color = new Color(0f, 0f, 0f, 0.70f);

        const float badgeScale = 0.4f;

        var hiddenGo = new GameObject(HiddenIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var hiddenRT = (RectTransform)hiddenGo.transform;
        hiddenRT.SetParent(rootRT, false);
        hiddenRT.anchorMin = new Vector2(1f - badgeScale, 0f);
        hiddenRT.anchorMax = new Vector2(1f, badgeScale);
        hiddenRT.offsetMin = Vector2.zero;
        hiddenRT.offsetMax = Vector2.zero;
        hiddenRT.pivot = new Vector2(0.5f, 0.5f);

        var hiddenImg = hiddenGo.GetComponent<Image>();
        hiddenImg.raycastTarget = false;
        hiddenImg.sprite = hiddenSprite;
        hiddenImg.enabled = false;
        hiddenImg.preserveAspect = true;

        var hasGo = new GameObject(HasIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var hasRT = (RectTransform)hasGo.transform;
        hasRT.SetParent(rootRT, false);

        hasRT.anchorMin = new Vector2(1f - badgeScale, 0f);
        hasRT.anchorMax = new Vector2(1f, badgeScale);
        hasRT.offsetMin = Vector2.zero;
        hasRT.offsetMax = Vector2.zero;
        hasRT.pivot = new Vector2(0.5f, 0.5f);

        var hasImg = hasGo.GetComponent<Image>();
        hasImg.raycastTarget = false;
        hasImg.sprite = hasVanitySprite;
        hasImg.enabled = false;
        hasImg.preserveAspect = true;

        root.SetActive(false);
        VanityStateByGo[slotGo] = root;
        return root;
    }

    public static void ToggleVanityStateOverlay(GameObject slotGo, VanityState vs)
    {
        if (!slotGo) return;
        if (!VanityStateByGo.TryGetValue(slotGo, out var root) || !root)
            root = EnsureVanityStateOverlay(slotGo);

        var rootImage = root.GetComponent<Image>();
        var hiddenT = root.transform.Find(HiddenIconName);
        var hasVanityT = root.transform.Find(HasIconName);

        var hiddenImg = hiddenT ? hiddenT.GetComponent<Image>() : null;
        var hasVanityImg = hasVanityT ? hasVanityT.GetComponent<Image>() : null;

        bool showHidden = vs.IsHidden && hiddenImg && hiddenImg.sprite != null;
        bool showHasVanity = vs.HasVanity && hasVanityImg && hasVanityImg.sprite != null;

        if (hiddenImg) hiddenImg.enabled = showHidden;
        if (hasVanityImg) hasVanityImg.enabled = showHasVanity;

        bool any = showHidden || showHasVanity;

        if (root.activeSelf != any) root.SetActive(any);
        if (rootImage) rootImage.enabled = showHidden;
    }
}