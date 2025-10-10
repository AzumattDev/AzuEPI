using AzuEPI.PlayerPreview;

namespace AzuEPI.Vanity;

internal static class VanityPanelController
{
    public const string VanityPanelName = "VanityPanel";
    public const string VanityScrollRootName = "ScrollRoot";
    public const string VanityViewportName = "Viewport";
    public const string VanityContentName = "Content";
    public const string VanityScrollbarName = "Scrollbar";
    public const string VanityToggleButtonName = "VanityToggleButton";
    public const string ResetAllVanityButtonName = "ResetAllVanityButton";

    private static readonly Vector2 CellSize = new(70, 70);
    private static readonly Vector2 Spacing = new(6, 6);
    private static readonly Vector2 Padding = new(12, 12);
    private const int Columns = 7;

    private static readonly Vector2 ScrollRootOffsetMin = new(10f, 10f);
    private static readonly Vector2 ScrollRootOffsetMax = new(-24f, -50f);

    private static readonly Vector2 ToggleBtnAnchorMin = new(0f, 1f);
    private static readonly Vector2 ToggleBtnAnchorMax = new(0f, 1f);
    private static readonly Vector2 ToggleBtnPivot = new(0f, 1f);
    private static readonly Vector2 ToggleBtnPos = new(-360f, -30f);
    private static readonly Vector2 ToggleBtnSize = new(120f, 32f);

    private static readonly Vector2 ResetBtnAnchorMin = new(0.5f, 1f);
    private static readonly Vector2 ResetBtnAnchorMax = new(0.5f, 1f);
    private static readonly Vector2 ResetBtnPivot = new(0f, 1f);
    private static readonly Vector2 ResetBtnPos = new(-45f, -20f);
    private static readonly Vector2 ResetBtnSize = new(120f, 32f);

    private static readonly Vector2 BarAnchorMin = new(1f, 0f);
    private static readonly Vector2 BarAnchorMax = new(1f, 1f);
    private static readonly Vector2 BarPivot = new(1f, 1f);
    private static readonly Vector2 BarOffsetMin = new(-12f, 12f);
    private static readonly Vector2 BarOffsetMax = new(-4f, -12f);

    private const string UpTriangle = "\u25B2";
    private const string DownTriangle = "\u25BC";
    private const string LeftTriangle = "\u25C0";
    private const string RightTriangle = "\u25B6";
    private const string HeaderTriangleSizeTag = "<size=10>";

    private static RectTransform _panel;
    private static ScrollRect _scroll;
    private static RectTransform _viewport;
    private static RectTransform _content;
    private static VerticalLayoutGroup _stack;
    private static ContentSizeFitter _fitter;
    private static Scrollbar _vbar;
    private static Button _toggleBtn;
    private static Button _resetVanitiesBtn;
    private static TMP_Text _fontSample;
    private static bool _visible;

    private static readonly Dictionary<VisSlot, List<VanityCell>> _cellsBySlot = new();

    public static void EnsureBuilt(InventoryGui gui)
    {
        if (!gui) return;

        if (!_panel) BuildPanel(gui);
        if (!_scroll) BuildScrollTree();
        if (!_toggleBtn) BuildToggleButton(gui);
        if (!_resetVanitiesBtn) BuildResetButton(gui);

        _fontSample = gui.m_craftButton?.GetComponentInChildren<TMP_Text>();

        RefreshGrid();
        SetVisible(_visible);
    }

    public static void SetVisible(bool visible)
    {
        _visible = visible;
        if (_panel) _panel.gameObject.SetActive(visible);
        if (visible && _panel) _panel.SetAsLastSibling();
    }

    public static void RefreshGrid()
    {
        if (!_content) return;

        var player = Player.m_localPlayer;
        var odb = ObjectDB.instance;
        if (!player || !odb) return;

        _cellsBySlot.Clear();
        ClearChildren(_content);

        var fromRecipes = odb.m_recipes
            .Where(r => r && r.m_item)
            .Select(r => r.m_item.gameObject)
            .ToList();

        var fromDB = odb.m_items?.Where(go => go).ToList() ?? new List<GameObject>();

        static IEnumerable<ItemDrop> AllItemDrops(IEnumerable<GameObject> roots) =>
            roots.SelectMany(go => go.GetComponentsInChildren<ItemDrop>(true))
                .Where(id => id && id.m_itemData?.m_shared != null);

        var drops = AllItemDrops(fromRecipes)
            .Concat(AllItemDrops(fromDB))
            .GroupBy(id => id.m_itemData.m_shared.m_name)
            .Select(g => g.First())
            .ToList();

        var vanityItems = drops
            .Select(d => d.m_itemData)
            .Where(d => d != null &&
                        d.m_shared != null &&
                        d.m_shared.m_icons != null &&
                        d.m_shared.m_icons.Length > 0 &&
                        string.IsNullOrWhiteSpace(d.m_shared.m_dlc) &&
                        (d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet ||
                         d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
                         d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs ||
                         d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder ||
                         d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility))
            .OrderBy(d => d.m_shared.m_itemType)
            .ThenBy(d => d.m_shared.m_name)
            .ToList();

        var order = new[]
        {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility
        };

        var grouped = vanityItems
            .GroupBy(d => d.m_shared.m_itemType)
            .OrderBy(g => Array.IndexOf(order, g.Key));

        var slotPrefab = InventoryGui.instance.m_playerGrid.m_elementPrefab;
        foreach (var group in grouped)
        {
            var headerLocKey = group.Key switch
            {
                ItemDrop.ItemData.ItemType.Helmet => "$item_helmet",
                ItemDrop.ItemData.ItemType.Chest => "$item_chest",
                ItemDrop.ItemData.ItemType.Legs => "$item_legs",
                ItemDrop.ItemData.ItemType.Shoulder => "$item_shoulder",
                ItemDrop.ItemData.ItemType.Utility => "$item_utility",
                _ => group.Key.ToString()
            };

            var headerLabel = Localization.instance.Localize(headerLocKey);
            AddHeader(headerLabel);
            var grid = AddGrid(headerLabel);

            CreateNoneCell(slotPrefab, grid, MapItemTypeToVisSlot(group.Key));

            foreach (var data in group)
                CreateCell(slotPrefab, grid, data, MapItemTypeToVisSlot(group.Key));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scroll.velocity = Vector2.zero;
        _scroll.normalizedPosition = new Vector2(0, 1);

        foreach (var slot in _cellsBySlot.Keys.ToList())
            UpdateSelectedVisuals(slot);
    }

    internal static void UpdateSelectedVisuals(VisSlot slot)
    {
        var ve = Player.m_localPlayer?.m_visEquipment;
        if (!ve) return;

        int v = slot switch
        {
            VisSlot.Helmet => VanityAPI.Get(ve, VanityZdoKeys.Helmet),
            VisSlot.Chest => VanityAPI.Get(ve, VanityZdoKeys.Chest),
            VisSlot.Legs => VanityAPI.Get(ve, VanityZdoKeys.Legs),
            VisSlot.Shoulder => VanityAPI.Get(ve, VanityZdoKeys.Shoulder),
            VisSlot.Utility => VanityAPI.Get(ve, VanityZdoKeys.Utility),
            _ => 0
        };

        bool hidden = VanityAPI.IsHidden(v);

        if (_cellsBySlot.TryGetValue(slot, out var list))
        {
            foreach (var cell in list)
            {
                bool isSelected = cell.IsNone
                    ? hidden
                    : (!hidden && v != 0 &&
                       cell.Item?.m_dropPrefab &&
                       cell.Item.m_dropPrefab.name.GetStableHashCode() == v);

                if (cell.SelectedBadge) cell.SelectedBadge.SetActive(isSelected);
            }
        }
    }

    public static void ResetAllVanities()
    {
        var player = Player.m_localPlayer;
        if (!player) return;
        var ve = player.m_visEquipment;
        if (!ve) return;

        VanityAPI.ClearVanity(ve, VisSlot.Helmet);
        VanityAPI.ClearVanity(ve, VisSlot.Chest);
        VanityAPI.ClearVanity(ve, VisSlot.Legs);
        VanityAPI.ClearVanity(ve, VisSlot.Shoulder);
        VanityAPI.ClearVanity(ve, VisSlot.Utility);

        var previewVe = AzuEPICharacterPanel.playerPreviewComp?.m_visEquipment;
        if (previewVe)
        {
            VanityAPI.ClearVanity(previewVe, VisSlot.Helmet);
            VanityAPI.ClearVanity(previewVe, VisSlot.Chest);
            VanityAPI.ClearVanity(previewVe, VisSlot.Legs);
            VanityAPI.ClearVanity(previewVe, VisSlot.Shoulder);
            VanityAPI.ClearVanity(previewVe, VisSlot.Utility);
        }

        foreach (var slot in _cellsBySlot.Keys.ToList())
            UpdateSelectedVisuals(slot);
    }

    private static void BuildPanel(InventoryGui gui)
    {
        var crafting = gui.m_crafting;
        var srcBkg = crafting.Find("Bkg");
        if (!srcBkg) return;

        var vanity = Object.Instantiate(srcBkg, crafting);
        vanity.name = VanityPanelName;

        _panel = vanity.GetComponent<RectTransform>();
        var srcRT = srcBkg.GetComponent<RectTransform>();
        _panel.anchorMin = srcRT.anchorMin;
        _panel.anchorMax = srcRT.anchorMax;
        _panel.SetAsLastSibling();

        var img = vanity.GetComponent<Image>();
        if (img) img.raycastTarget = false;
    }

    private static void BuildScrollTree()
    {
        if (!_panel) return;

        GameObject scrollRoot;
        var store = StoreGui.instance;
        if (store && store.GetComponentInChildren<ScrollRect>())
        {
            var src = store.GetComponentInChildren<ScrollRect>().gameObject;
            scrollRoot = Object.Instantiate(src, _panel);
        }
        else
        {
            scrollRoot = new GameObject(VanityScrollRootName, typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(_panel, false);
        }

        var rootRT = (RectTransform)scrollRoot.transform;
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
        var go = new GameObject(VanityViewportName, typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        AnchorFill(rt);

        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        return rt;
    }

    private static void BuildContentStack(RectTransform parent)
    {
        var go = new GameObject(VanityContentName,
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));

        _content = (RectTransform)go.transform;
        _content.SetParent(parent, false);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(0f, 1f);
        _content.pivot = new Vector2(0f, 1f);

        _stack = go.GetComponent<VerticalLayoutGroup>();
        _stack.childAlignment = TextAnchor.UpperLeft;
        _stack.padding = new RectOffset(12, 12, 12, 12);
        _stack.spacing = 10f;
        _stack.childControlWidth = true;
        _stack.childControlHeight = true;
        _stack.childForceExpandWidth = true;
        _stack.childForceExpandHeight = false;

        _fitter = go.GetComponent<ContentSizeFitter>();
        _fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static Scrollbar BuildScrollbar(Transform panelParent)
    {
        GameObject barGO;
        if (InventoryGui.instance.m_recipeListScroll)
        {
            barGO = Object.Instantiate(InventoryGui.instance.m_recipeListScroll.gameObject, panelParent);
            barGO.name = VanityScrollbarName;
        }
        else
        {
            barGO = new GameObject(VanityScrollbarName, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGO.transform.SetParent(panelParent, false);
        }

        var barRT = (RectTransform)barGO.transform;
        barRT.anchorMin = BarAnchorMin;
        barRT.anchorMax = BarAnchorMax;
        barRT.pivot = BarPivot;
        barRT.offsetMin = BarOffsetMin;
        barRT.offsetMax = BarOffsetMax;

        var bar = barGO.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;

        var bg = bar.GetComponent<Image>();
        if (bg) bg.enabled = true;

        var handle = bar.transform.Find("Sliding Area/Handle")?.GetComponent<Image>();
        if (handle) handle.enabled = true;
        else
        {
            var h = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var hRT = (RectTransform)h.transform;
            hRT.SetParent(barRT, false);
            AnchorFill(hRT);
            bar.targetGraphic = h.GetComponent<Image>();
            bar.handleRect = hRT;
        }

        return bar;
    }

    private static void BuildToggleButton(InventoryGui gui)
    {
        var src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src) return;

        var clone = CloneButton(src, gui.m_crafting, VanityToggleButtonName, ToggleBtnAnchorMin, ToggleBtnAnchorMax, ToggleBtnPivot, ToggleBtnPos, ToggleBtnSize);
        var btn = clone.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            _visible = !_visible;
            SetVisible(_visible);
        });

        var label = clone.GetComponentInChildren<TMP_Text>();
        if (label) label.text = Localization.instance.Localize("$azuepi_vanity");

        _toggleBtn = btn;
    }

    private static void BuildResetButton(InventoryGui gui)
    {
        var src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src || !_panel) return;

        var clone = CloneButton(src, _panel, ResetAllVanityButtonName, ResetBtnAnchorMin, ResetBtnAnchorMax, ResetBtnPivot, ResetBtnPos, ResetBtnSize);
        var btn = clone.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ResetAllVanities);

        var label = clone.GetComponentInChildren<TMP_Text>();
        if (label) label.text = Localization.instance.Localize("$azuepi_reset_vanity");

        _resetVanitiesBtn = btn;
    }

    private static RectTransform AddHeader(string label)
    {
        var go = new GameObject($"Header_{label}", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(_content, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);

        var txt = go.AddComponent<TextMeshProUGUI>();
        ApplyHeaderStyle(txt, label, isExpanded: false);

        var headerButton = go.AddComponent<Button>();
        headerButton.onClick.AddListener(() =>
        {
            int myIndex = rt.GetSiblingIndex();
            if (myIndex + 1 >= _content.childCount) return;

            var next = _content.GetChild(myIndex + 1);
            bool newActive = !next.gameObject.activeSelf;
            next.gameObject.SetActive(newActive);

            ApplyHeaderStyle(txt, label, isExpanded: newActive);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        });

        return rt;
    }

    private static void ApplyHeaderStyle(TextMeshProUGUI txt, string label, bool isExpanded)
    {
        if (_fontSample)
        {
            txt.font = _fontSample.font;
            txt.fontSharedMaterial = _fontSample.fontSharedMaterial;
            txt.fontSize = _fontSample.fontSize + 2f;
            txt.color = _fontSample.color;
            txt.enableWordWrapping = false;
            txt.alignment = TextAlignmentOptions.Left;
        }
        else
        {
            txt.fontSize = 20f;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Left;
        }

        var tri = isExpanded ? DownTriangle : UpTriangle;
        txt.text = $"{label} {HeaderTriangleSizeTag}{tri}</size>";
    }

    private static GridLayoutGroup AddGrid(string headerLabel)
    {
        var go = new GameObject($"{headerLabel}_Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        var rt = (RectTransform)go.transform;
        rt.SetParent(_content, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);

        var le = go.GetComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.flexibleHeight = 0;

        var grid = go.GetComponent<GridLayoutGroup>();
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.cellSize = CellSize;
        grid.spacing = Spacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.padding = new RectOffset((int)Padding.x, (int)Padding.x, (int)Padding.y, (int)Padding.y);

        go.SetActive(false);
        return grid;
    }

    private static void CreateNoneCell(GameObject slotPrefab, GridLayoutGroup grid, VisSlot slot)
    {
        var go = Object.Instantiate(slotPrefab, grid.transform);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = grid.cellSize.x;
        le.minHeight = le.preferredHeight = grid.cellSize.y;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        var icon = go.transform.Find("icon").GetComponent<Image>();
        icon.sprite = null;
        icon.enabled = false;

        DisableChild(go.transform, "amount");
        DisableChild(go.transform, "equiped");
        DisableChild(go.transform, "queued");
        DisableChild(go.transform, "noteleport");
        DisableChild(go.transform, "foodicon");
        DisableChild(go.transform, "durability");
        DisableChild(go.transform, "quality");
        DisableChild(go.transform, "binding");
        DisableChildrenContaining(go.transform, "JC_");

        var labelGo = new GameObject("NoneLabel", typeof(RectTransform), typeof(TMP_Text));
        var labelRT = (RectTransform)labelGo.transform;
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = new Vector2(0.5f, 0.5f);
        labelRT.pivot = new Vector2(0.5f, 0.5f);
        labelRT.anchoredPosition = Vector2.zero;

        var text = labelGo.AddComponent<TextMeshProUGUI>();
        if (_fontSample)
        {
            text.font = _fontSample.font;
            text.fontSharedMaterial = _fontSample.fontSharedMaterial;
            text.fontSize = _fontSample.fontSize - 2f;
            text.color = _fontSample.color;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            text.fontSize = 16f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.text = Localization.instance?.Localize("$menu_none") ?? "None";

        var selectedBadge = FindOrCreateSelectedBadge(go.transform);

        var btn = go.GetComponent<Button>();
        btn.onClick = new Button.ButtonClickedEvent();

        var cell = go.GetComponent<VanityCell>() ?? go.AddComponent<VanityCell>();
        cell.Item = null;
        cell.Icon = icon;
        cell.Slot = slot;
        cell.SelectedBadge = selectedBadge;
        cell.IsNone = true;

        GetOrCreateSlotList(slot).Add(cell);

        var tooltipForNone = go.GetComponent<UITooltip>() ?? go.AddComponent<UITooltip>();
        tooltipForNone.m_topic = Localization.instance?.Localize("$menu_none") ?? "None";
        tooltipForNone.m_text = "";
    }

    private static void CreateCell(GameObject slotPrefab, GridLayoutGroup grid, ItemDrop.ItemData data, VisSlot slot)
    {
        var go = Object.Instantiate(slotPrefab, grid.transform);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = grid.cellSize.x;
        le.minHeight = le.preferredHeight = grid.cellSize.y;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        var icon = go.transform.Find("icon").GetComponent<Image>();
        icon.sprite = data.m_shared.m_icons[0];
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = Color.white;

        DisableChild(go.transform, "amount");
        DisableChild(go.transform, "equiped");
        DisableChild(go.transform, "queued");
        DisableChild(go.transform, "noteleport");
        DisableChild(go.transform, "foodicon");
        DisableChild(go.transform, "durability");
        DisableChild(go.transform, "quality");
        DisableChild(go.transform, "binding");
        DisableChildrenContaining(go.transform, "JC_");

        var selectedBadge = FindOrCreateSelectedBadge(go.transform);

        var btn = go.GetComponent<Button>();
        btn.onClick = new Button.ButtonClickedEvent();

        var cell = go.GetComponent<VanityCell>() ?? go.AddComponent<VanityCell>();
        cell.Item = data;
        cell.Icon = icon;
        cell.Slot = slot;
        cell.SelectedBadge = selectedBadge;

        GetOrCreateSlotList(slot).Add(cell);
    }

    private static VisSlot MapItemTypeToVisSlot(ItemDrop.ItemData.ItemType t) => t switch
    {
        ItemDrop.ItemData.ItemType.Helmet => VisSlot.Helmet,
        ItemDrop.ItemData.ItemType.Chest => VisSlot.Chest,
        ItemDrop.ItemData.ItemType.Legs => VisSlot.Legs,
        ItemDrop.ItemData.ItemType.Shoulder => VisSlot.Shoulder,
        ItemDrop.ItemData.ItemType.Utility => VisSlot.Utility,
        _ => throw new NotSupportedException($"Vanity not supported for {t}")
    };

    private static void AnchorFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Transform CloneButton(Transform src, Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        var clone = Object.Instantiate(src, parent);
        clone.name = name;
        clone.SetAsLastSibling();

        var rt = (RectTransform)clone;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        return clone;
    }

    private static void DisableChild(Transform root, string childName)
    {
        var t = root.Find(childName);
        if (t) t.gameObject.SetActive(false);
    }

    private static void DisableChildrenContaining(Transform root, string contains)
    {
        for (int i = 0; i < root.childCount; ++i)
        {
            var t = root.GetChild(i);
            if (t.name.Contains(contains))
                t.gameObject.SetActive(false);
        }
    }

    private static GameObject FindOrCreateSelectedBadge(Transform root)
    {
        var t = root.Find("selected") ?? root.Find("Selected");
        if (t) return t.gameObject;

        var go = new GameObject("Selected", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        AnchorFill(rt);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1, 1, 1, 0.18f);
        go.SetActive(false);
        return go;
    }

    private static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; --i)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    private static List<VanityCell> GetOrCreateSlotList(VisSlot slot)
    {
        if (!_cellsBySlot.TryGetValue(slot, out var list))
        {
            list = new List<VanityCell>();
            _cellsBySlot[slot] = list;
        }

        return list;
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
static class Vanity_OnShow
{
    static void Postfix(InventoryGui __instance)
    {
        VanityPanelController.EnsureBuilt(__instance);
        VanityPanelController.RefreshGrid();
    }
}

public class VanityCell : MonoBehaviour
{
    public ItemDrop.ItemData Item;
    public Image Icon;
    public VisSlot Slot;
    public GameObject SelectedBadge;
    public bool IsNone = false;
    private UIInputHandler _input;

    private void Awake()
    {
        var tooltip = gameObject.GetComponent<UITooltip>();
        if (tooltip != null)
        {
            tooltip.m_topic = Item?.m_shared?.m_name ?? "";
            tooltip.m_text = "";
        }

        _input = gameObject.GetComponentInChildren<UIInputHandler>();
        if (_input == null) return;
        _input.m_onRightDown += OnRightClick;
        _input.m_onLeftDown += OnLeftClick;
    }

    private void OnDestroy()
    {
        if (_input == null) return;
        _input.m_onRightDown -= OnRightClick;
        _input.m_onLeftDown -= OnLeftClick;
    }

    private void Update()
    {
        if (IsNone) return;
        if (!Icon || Item?.m_shared == null || Player.m_localPlayer == null) return;

        bool known = Player.m_localPlayer.IsKnownMaterial(Item.m_shared.m_name);
        Icon.color = known ? Color.white : Color.black;
    }

    public void OnRightClick(UIInputHandler _)
    {
        var ve = Player.m_localPlayer?.m_visEquipment;
        var previewVe = AzuEPICharacterPanel.playerPreviewComp?.m_visEquipment;
        if (!ve) return;

        if (IsNone)
        {
            VanityAPI.SetHidden(ve, Slot, false);
            if (previewVe) VanityAPI.SetHidden(previewVe, Slot, false);
            VanityPanelController.UpdateSelectedVisuals(Slot);
            VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
            return;
        }

        if (Item?.m_dropPrefab == null) return;
        VanityAPI.ClearVanity(ve, Slot);
        if (previewVe) VanityAPI.ClearVanity(previewVe, Slot);
        VanityPanelController.UpdateSelectedVisuals(Slot);
        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }

    public void OnLeftClick(UIInputHandler _)
    {
        var ve = Player.m_localPlayer?.m_visEquipment;
        var previewVe = AzuEPICharacterPanel.playerPreviewComp?.m_visEquipment;
        if (!ve || !previewVe) return;

        if (IsNone)
        {
            VanityAPI.SetHidden(ve, Slot, true);
            VanityAPI.SetHidden(previewVe, Slot, true);
            VanityPanelController.UpdateSelectedVisuals(Slot);
            VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
            return;
        }

        if (Item?.m_shared == null || Item.m_dropPrefab == null) return;
        if (!Player.m_localPlayer.IsKnownMaterial(Item.m_shared.m_name)) return;

        string prefab = Item.m_dropPrefab.name;
        switch (Slot)
        {
            case VisSlot.Helmet:
                VanityAPI.SetVanity(ve, VisSlot.Helmet, prefab);
                VanityAPI.SetVanity(previewVe, VisSlot.Helmet, prefab);
                break;
            case VisSlot.Chest:
                VanityAPI.SetVanity(ve, VisSlot.Chest, prefab);
                VanityAPI.SetVanity(previewVe, VisSlot.Chest, prefab);
                break;
            case VisSlot.Legs:
                VanityAPI.SetVanity(ve, VisSlot.Legs, prefab);
                VanityAPI.SetVanity(previewVe, VisSlot.Legs, prefab);
                break;
            case VisSlot.Shoulder:
                VanityAPI.SetVanity(ve, VisSlot.Shoulder, prefab, variant: 0);
                VanityAPI.SetVanity(previewVe, VisSlot.Shoulder, prefab, variant: 0);
                break;
            case VisSlot.Utility:
                VanityAPI.SetVanity(ve, VisSlot.Utility, prefab);
                VanityAPI.SetVanity(previewVe, VisSlot.Utility, prefab);
                break;
        }

        VanityPanelController.UpdateSelectedVisuals(Slot);
        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }
}