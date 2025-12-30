namespace AzuEPI.Game.Panels;

public static class PanelUtilities
{
    public struct ButtonConfig
    {
        public readonly string Name;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPosition;
        public Vector2 Size;
        public readonly string GamepadKey;
        public readonly KeyCode GamepadKeyCode;
        public readonly string Label;
        public readonly float LabelFontSize;
        public readonly System.Action OnClick;

        public ButtonConfig(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string gamepadKey, KeyCode gamepadKeyCode, string label, float labelFontSize, Action onClick)
        {
            Name = name;
            AnchorMin = anchorMin;
            AnchorMax = anchorMax;
            Pivot = pivot;
            AnchoredPosition = anchoredPosition;
            Size = size;
            GamepadKey = gamepadKey;
            GamepadKeyCode = gamepadKeyCode;
            Label = label;
            LabelFontSize = labelFontSize;
            OnClick = onClick;
        }
    }

    public struct ScrollbarConfig
    {
        public string Name;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 OffsetMin;
        public Vector2 OffsetMax;
    }

    #region UI Element Creation

    public static Transform CloneButton(Transform src, Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
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

    public static void AnchorFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static RectTransform BuildPanel(InventoryGui gui, string panelName)
    {
        RectTransform crafting = gui.m_crafting;
        Transform srcBkg = crafting.Find("Bkg");
        if (!srcBkg) return null;

        Transform panel = Object.Instantiate(srcBkg, crafting);
        panel.name = panelName;

        RectTransform panelRT = panel.GetComponent<RectTransform>();
        RectTransform srcRT = srcBkg.GetComponent<RectTransform>();
        panelRT.anchorMin = srcRT.anchorMin;
        panelRT.anchorMax = srcRT.anchorMax;
        panelRT.SetAsLastSibling();

        Image img = panel.GetComponent<Image>();
        if (img) img.raycastTarget = false;

        return panelRT;
    }

    public static RectTransform BuildViewport(RectTransform parent, string viewportName, Color? backgroundColor = null)
    {
        GameObject go = new(viewportName, typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        AnchorFill(rt);

        Image img = go.GetComponent<Image>();
        img.color = backgroundColor ?? new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        return rt;
    }

    public static RectTransform BuildVerticalContent(RectTransform parent, string contentName, RectOffset padding, float spacing = 10f)
    {
        GameObject go = new(contentName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

        RectTransform content = (RectTransform)go.transform;
        content.SetParent(parent, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup vLayout = go.GetComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.padding = padding;
        vLayout.spacing = spacing;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return content;
    }

    public static Scrollbar BuildScrollbar(Transform panelParent, ScrollbarConfig config)
    {
        GameObject barGO;
        if (InventoryGui.instance.m_recipeListScroll)
        {
            barGO = Object.Instantiate(InventoryGui.instance.m_recipeListScroll.gameObject, panelParent);
            barGO.name = config.Name;
        }
        else
        {
            barGO = new GameObject(config.Name, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGO.transform.SetParent(panelParent, false);
        }

        RectTransform barRT = (RectTransform)barGO.transform;
        barRT.anchorMin = config.AnchorMin;
        barRT.anchorMax = config.AnchorMax;
        barRT.pivot = config.Pivot;
        barRT.offsetMin = config.OffsetMin;
        barRT.offsetMax = config.OffsetMax;

        Scrollbar bar = barGO.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;

        Image bg = bar.GetComponent<Image>();
        if (bg) bg.enabled = true;

        Image handle = bar.transform.Find("Sliding Area/Handle")?.GetComponent<Image>();
        if (handle)
        {
            handle.enabled = true;
        }
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

    public static (ScrollRect scroll, RectTransform viewport, RectTransform content, Scrollbar scrollbar) BuildScrollView(RectTransform panel, string scrollRootName, string viewportName, string contentName, ScrollbarConfig scrollbarConfig, Vector2 scrollRootOffsetMin, Vector2 scrollRootOffsetMax, RectOffset contentPadding, float contentSpacing = 10f, Color? viewportColor = null)
    {
        GameObject scrollRoot;
        StoreGui store = StoreGui.instance;
        if (store && store.GetComponentInChildren<ScrollRect>())
        {
            GameObject src = store.GetComponentInChildren<ScrollRect>().gameObject;
            scrollRoot = Object.Instantiate(src, panel);
            scrollRoot.name = scrollRootName;
        }
        else
        {
            scrollRoot = new GameObject(scrollRootName, typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(panel, false);
        }

        RectTransform rootRT = (RectTransform)scrollRoot.transform;
        AnchorFill(rootRT);
        rootRT.offsetMin = scrollRootOffsetMin;
        rootRT.offsetMax = scrollRootOffsetMax;

        ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 800f;

        RectTransform viewport = BuildViewport(rootRT, viewportName, viewportColor);

        RectTransform content = BuildVerticalContent(viewport, contentName, contentPadding, contentSpacing);

        Scrollbar scrollbar = BuildScrollbar(panel, scrollbarConfig);

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        return (scroll, viewport, content, scrollbar);
    }

    public static (Transform buttonGo, Button button) BuildToggleButton(InventoryGui gui, Transform parent, ButtonConfig config)
    {
        Transform src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src) return (null, null);

        Transform buttonGo = CloneButton(src, parent, config.Name, config.AnchorMin, config.AnchorMax, config.Pivot, config.AnchoredPosition, config.Size);

        BindGamePad(buttonGo, config.GamepadKey, config.GamepadKeyCode);

        Button btn = buttonGo.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        if (config.OnClick != null)
        {
            btn.onClick.AddListener(() => config.OnClick());
        }

        TMP_Text label = buttonGo.GetComponentInChildren<TMP_Text>();
        if (!label) return (buttonGo, btn);
        label.text = config.Label;
        label.fontSize = config.LabelFontSize;

        return (buttonGo, btn);
    }

    #endregion

    #region UI Helpers

    public static void BindGamePad(Transform buttonGo, string GamepadKey, KeyCode GamepadKeyCode)
    {
        if (buttonGo.TryGetComponent(out UIGamePad gp))
        {
            if (ZInput.instance != null)
            {
                gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text =
                    ZInput.instance.GetBoundKeyString(GamepadKey, true);
            }
            else
            {
                ZInput.Initialize();
                gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text = ZInput.instance.GetBoundKeyString(GamepadKey, true);
            }

            gp.m_zinputKey = GamepadKey;
            gp.m_keyCode = GamepadKeyCode;
        }
    }

    public static void UpdateButtonBinding(Transform buttonGo, KeyCode newKeyCode)
    {
        if (!buttonGo.TryGetComponent(out UIGamePad gp)) return;
        string zinputKey = KeyCodeToZInputKey(newKeyCode);
        gp.m_keyCode = newKeyCode;
        gp.m_zinputKey = zinputKey;

        if (ZInput.instance != null)
        {
            gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text =
                ZInput.instance.GetBoundKeyString(zinputKey, true);
        }
    }

    public static string KeyCodeToZInputKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.JoystickButton0 => "JoyButtonA",
            KeyCode.JoystickButton1 => "JoyButtonB",
            KeyCode.JoystickButton2 => "JoyButtonX",
            KeyCode.JoystickButton3 => "JoyButtonY",
            KeyCode.JoystickButton4 => "JoyLBumper",
            KeyCode.JoystickButton5 => "JoyRBumper",
            KeyCode.JoystickButton6 => "JoyBack",
            KeyCode.JoystickButton7 => "JoyStart",
            KeyCode.JoystickButton8 => "JoyLStick",
            KeyCode.JoystickButton9 => "JoyRStick",
            KeyCode.JoystickButton10 => "JoyDPadLeft",
            KeyCode.JoystickButton11 => "JoyDPadRight",
            KeyCode.JoystickButton12 => "JoyDPadUp",
            KeyCode.JoystickButton13 => "JoyDPadDown",
            KeyCode.JoystickButton14 => "JoyLTrigger",
            KeyCode.JoystickButton15 => "JoyRTrigger",
            KeyCode.JoystickButton16 => "JoyButtonA",
            KeyCode.JoystickButton17 => "JoyButtonB",
            KeyCode.JoystickButton18 => "JoyButtonX",
            KeyCode.JoystickButton19 => "JoyButtonY",
            _ => "JoyButtonA"
        };
    }

    public static void HideCraftingElements(bool hide)
    {
        if (!InventoryGui.instance) return;

        RectTransform craftingPanel = InventoryGui.instance.m_crafting;
        craftingPanel.transform.Find("TabsButtons").SafeSetActive(!hide);
        craftingPanel.transform.Find("RecipeList").SafeSetActive(!hide);
        craftingPanel.transform.Find("Decription").SafeSetActive(!hide);
    }

    public static TMP_FontAsset GetFontAsset(InventoryGui gui)
    {
        TMP_Text fontSample = gui.m_craftButton?.GetComponentInChildren<TMP_Text>()
                              ?? gui.m_takeAllButton?.GetComponentInChildren<TMP_Text>()
                              ?? gui.m_info?.GetComponentInChildren<TMP_Text>();

        return fontSample?.font;
    }

    public static void DisableChild(Transform root, string childName)
    {
        Transform t = root.Find(childName);
        if (t) t.gameObject.SetActive(false);
    }

    public static void DisableChildrenContaining(Transform root, string contains)
    {
        int count = root.childCount;
        for (int i = 0; i < count; ++i)
        {
            Transform t = root.GetChild(i);
            if (t.name.Contains(contains))
                t.gameObject.SetActive(false);
        }
    }

    public static void ClearChildren(Transform t)
    {
        int count = t.childCount;
        for (int i = count - 1; i >= 0; --i)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    #endregion
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateGamepad))]
static class Vanity_GamepadUpdate
{
    static void Prefix(InventoryGui __instance)
    {
        if (!__instance.m_inventoryGroup.IsActive)
            return;
        if (VanityPanelController.IsVisible())
            VanityPanelController.UpdateGamepadNavigation();
        if (PersonalLoadoutGui.IsVisible())
        {
            PersonalLoadoutGui.instance!.HandleUIUpdates();
        }

        if (StatsPanelController.IsVisible())
        {
            StatsPanelController.HandleScrollInput();
        }
    }
}