using AzuEPI.Game.Loadout;
using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Game.Vanity;

internal static class VanityPanelController
{
    public const string VanityPanelName = "VanityPanel";
    public const string VanityScrollRootName = "ScrollRoot";
    public const string VanityViewportName = "Viewport";
    public const string VanityContentName = "Content";
    public const string VanityScrollbarName = "Scrollbar";
    public const string VanityToggleButtonName = "AzuEPIVanityToggleButton";
    public const string ResetAllVanityButtonName = "ResetAllVanityButton";

    public static Transform VanityButtonGo = null!;

    private static readonly Vector2 CellSize = new(70, 70);
    private static readonly Vector2 Spacing = new(6, 6);
    private static readonly Vector2 Padding = new(12, 12);
    private const int Columns = 7;

    private static readonly Vector2 ScrollRootOffsetMin = new(10f, 10f);
    private static readonly Vector2 ScrollRootOffsetMax = new(-24f, -50f);

    internal static RectTransform ToggleButtonParentGlg = null!;
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

    private static VanityCell _selectedCell;
    private static readonly List<VanityCell> _allCells = new();
    private static Image _gamepadSelectionOverlay;

    private static readonly Dictionary<VisSlot, List<VanityCell>> _cellsBySlot = new();

    private static readonly List<GameObject> _reusableGameObjectList = new(256);
    private static readonly Dictionary<string, ItemDrop> _reusableDropsDict = new(256);
    private static readonly List<ItemDrop.ItemData> _reusableVanityItems = new(128);

    public static void EnsureBuilt(InventoryGui gui)
    {
        if (!gui) return;

        if (!_panel) BuildPanel(gui);
        if (!_scroll) BuildScrollTree();
        if (!_toggleBtn) BuildVanityToggleButton(gui);
        if (!_resetVanitiesBtn) BuildResetButton(gui);

        _fontSample = gui.m_craftButton?.GetComponentInChildren<TMP_Text>();

        RefreshGrid();
        SetVisible(_visible);
    }

    public static bool IsVanityPanelVisible()
    {
        return _visible;
    }

    public static void SetVisible(bool visible)
    {
        _visible = visible;
        if (_panel) _panel.gameObject.SetActive(visible);
        if (visible && _panel)
        {
            _panel.SetAsLastSibling();
            if (ZInput.IsGamepadActive())
            {
                ExpandAllSections();
                SelectFirstCell();
            }
        }
        else
        {
            _selectedCell = null;
            if (_gamepadSelectionOverlay) _gamepadSelectionOverlay.gameObject.SetActive(false);
        }
    }

    private static void ExpandAllSections()
    {
        if (!_content) return;

        for (int i = 0; i < _content.childCount; i++)
        {
            Transform child = _content.GetChild(i);
            GridLayoutGroup grid = child.GetComponent<GridLayoutGroup>();
            if (grid != null && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);

                if (i > 0)
                {
                    Transform header = _content.GetChild(i - 1);
                    TextMeshProUGUI txt = header.GetComponent<TextMeshProUGUI>();
                    if (txt)
                    {
                        string label = txt.text.Split(' ')[0];
                        ApplyHeaderStyle(txt, label, isExpanded: true);
                    }
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    public static void RefreshGrid()
    {
        if (!_content) return;

        Player? player = Player.m_localPlayer;
        ObjectDB? odb = ObjectDB.instance;
        if (!player || !odb) return;

        _cellsBySlot.Clear();
        _allCells.Clear();
        _selectedCell = null;
        ClearChildren(_content);

        _reusableGameObjectList.Clear();
        foreach (Recipe? recipe in odb.m_recipes)
        {
            if (recipe && recipe.m_item)
                _reusableGameObjectList.Add(recipe.m_item.gameObject);
        }

        if (odb.m_items != null)
        {
            foreach (GameObject? go in odb.m_items)
            {
                if (go) _reusableGameObjectList.Add(go);
            }
        }

        _reusableDropsDict.Clear();
        foreach (GameObject? go in _reusableGameObjectList)
        {
            ItemDrop[]? itemDrops = go.GetComponentsInChildren<ItemDrop>(true);
            foreach (var id in itemDrops)
            {
                if (id && id.m_itemData?.m_shared != null)
                {
                    string? name = id.m_itemData.m_shared.m_name;
                    if (!_reusableDropsDict.ContainsKey(name))
                        _reusableDropsDict[name] = id;
                }
            }
        }

        _reusableVanityItems.Clear();
        foreach (ItemDrop? drop in _reusableDropsDict.Values)
        {
            ItemDrop.ItemData? d = drop.m_itemData;
            if (d != null &&
                d.m_shared != null &&
                d.m_shared.m_icons != null &&
                d.m_shared.m_icons.Length > 0 &&
                (d.m_dropPrefab.HasChildWithNameThatContains("attach") || d.m_dropPrefab.HasChildWithNameThatContains("log")) &&
                string.IsNullOrWhiteSpace(d.m_shared.m_dlc) &&
                (d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet ||
                 d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
                 d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs ||
                 d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder ||
                 d.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility))
            {
                _reusableVanityItems.Add(d);
            }
        }

        _reusableVanityItems.Sort((a, b) =>
        {
            int typeCompare = a.m_shared.m_itemType.CompareTo(b.m_shared.m_itemType);
            return typeCompare != 0 ? typeCompare : string.Compare(a.m_shared.m_name, b.m_shared.m_name, StringComparison.Ordinal);
        });

        GameObject? slotPrefab = InventoryGui.instance.m_playerGrid.m_elementPrefab;
        ItemDrop.ItemData.ItemType currentType = (ItemDrop.ItemData.ItemType)(-1);
        GridLayoutGroup currentGrid = null;

        foreach (ItemDrop.ItemData? data in _reusableVanityItems)
        {
            ItemDrop.ItemData.ItemType itemType = data.m_shared.m_itemType;

            if (itemType != currentType)
            {
                currentType = itemType;

                string headerLocKey = itemType switch
                {
                    ItemDrop.ItemData.ItemType.Helmet => "$item_helmet",
                    ItemDrop.ItemData.ItemType.Chest => "$item_chest",
                    ItemDrop.ItemData.ItemType.Legs => "$item_legs",
                    ItemDrop.ItemData.ItemType.Shoulder => "$item_shoulder",
                    ItemDrop.ItemData.ItemType.Utility => "$item_utility",
                    _ => itemType.ToString()
                };

                string? headerLabel = Localization.instance.Localize(headerLocKey);
                AddHeader(headerLabel);
                currentGrid = AddGrid(headerLabel);

                CreateNoneCell(slotPrefab, currentGrid, MapItemTypeToVisSlot(itemType));
            }

            if (currentGrid != null)
                CreateCell(slotPrefab, currentGrid, data, MapItemTypeToVisSlot(itemType));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scroll.velocity = Vector2.zero;
        _scroll.normalizedPosition = new Vector2(0, 1);

        foreach (VisSlot slot in _cellsBySlot.Keys)
            UpdateSelectedVisuals(slot);
    }

    public static void UpdateGamepadNavigation()
    {
        if (!_visible || !ZInput.IsGamepadActive() || Console.IsVisible()) return;
        if (PersonalLoadoutGui.IsVisible()) return; // Don't handle input if loadout is open
        if (_allCells.Count == 0) return;

        if (_selectedCell == null || !_selectedCell.gameObject.activeInHierarchy)
        {
            SelectFirstCell();
            return;
        }

        bool left = ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft");
        bool right = ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight");
        bool up = ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp");
        bool down = ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown");

        if (left || right || up || down)
        {
            Navigate(left, right, up, down);
        }

        if (ZInput.GetButtonDown("JoyButtonA"))
        {
            _selectedCell?.OnLeftClick(null);
        }
        else if (ZInput.GetButtonDown("JoyButtonX"))
        {
            _selectedCell?.OnRightClick(null);
        }
        else if (ZInput.GetButtonDown("JoyLTrigger") || ZInput.GetButtonDown("JoyRTrigger"))
        {
            ToggleCurrentSection();
        }
    }

    private static void ToggleCurrentSection()
    {
        if (_selectedCell == null) return;

        Transform grid = _selectedCell.transform.parent;
        int gridIndex = grid.GetSiblingIndex();
        int headerIndex = gridIndex - 1;

        if (headerIndex < 0 || headerIndex >= _content.childCount) return;

        Transform header = _content.GetChild(headerIndex);
        Button headerButton = header.GetComponent<Button>();
        if (headerButton != null)
        {
            headerButton.onClick.Invoke();
        }
    }

    private static void Navigate(bool left, bool right, bool up, bool down)
    {
        if (_selectedCell == null) return;

        GridLayoutGroup parentGrid = _selectedCell.transform.parent.GetComponent<GridLayoutGroup>();
        if (!parentGrid) return;

        List<VanityCell> gridCells = new();
        foreach (Transform child in parentGrid.transform)
        {
            if (child.gameObject.activeSelf)
            {
                VanityCell cell = child.GetComponent<VanityCell>();
                if (cell) gridCells.Add(cell);
            }
        }

        int currentIndex = gridCells.IndexOf(_selectedCell);
        if (currentIndex < 0) return;

        int columns = parentGrid.constraintCount;
        int currentRow = currentIndex / columns;
        int currentCol = currentIndex % columns;

        VanityCell nextCell = null;

        if (left && currentCol > 0)
        {
            nextCell = gridCells[currentIndex - 1];
        }
        else if (right && currentCol < columns - 1 && currentIndex + 1 < gridCells.Count)
        {
            nextCell = gridCells[currentIndex + 1];
        }
        else if (up)
        {
            if (currentRow > 0)
            {
                int targetIndex = (currentRow - 1) * columns + currentCol;
                if (targetIndex < gridCells.Count)
                    nextCell = gridCells[targetIndex];
            }
            else
            {
                int currentSectionIndex = GetSectionIndexOfCell(_selectedCell);
                VanityCell prevSectionCell = FindLastCellInSection(currentSectionIndex - 1);
                if (prevSectionCell != null)
                    nextCell = prevSectionCell;
            }
        }
        else if (down)
        {
            int targetIndex = (currentRow + 1) * columns + currentCol;
            if (targetIndex < gridCells.Count)
            {
                nextCell = gridCells[targetIndex];
            }
            else
            {
                int currentSectionIndex = GetSectionIndexOfCell(_selectedCell);
                VanityCell nextSectionCell = FindFirstCellInSection(currentSectionIndex + 1);
                if (nextSectionCell != null)
                    nextCell = nextSectionCell;
            }
        }

        if (nextCell != null)
        {
            SelectCell(nextCell);
        }
    }

    private static int GetSectionIndexOfCell(VanityCell cell)
    {
        if (!cell) return -1;
        Transform grid = cell.transform.parent;
        return grid.GetSiblingIndex();
    }

    private static VanityCell FindFirstCellInSection(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= _content.childCount) return null;

        Transform section = _content.GetChild(sectionIndex);
        if (!section.gameObject.activeSelf) return FindFirstCellInSection(sectionIndex + 1);

        GridLayoutGroup grid = section.GetComponent<GridLayoutGroup>();
        if (!grid) return null;

        foreach (Transform child in grid.transform)
        {
            if (child.gameObject.activeSelf)
            {
                VanityCell cell = child.GetComponent<VanityCell>();
                if (cell) return cell;
            }
        }

        return FindFirstCellInSection(sectionIndex + 1);
    }

    private static VanityCell FindLastCellInSection(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= _content.childCount) return null;

        Transform section = _content.GetChild(sectionIndex);
        if (!section.gameObject.activeSelf) return FindLastCellInSection(sectionIndex - 1);

        GridLayoutGroup grid = section.GetComponent<GridLayoutGroup>();
        if (!grid) return null;

        VanityCell lastCell = null;
        foreach (Transform child in grid.transform)
        {
            if (child.gameObject.activeSelf)
            {
                VanityCell cell = child.GetComponent<VanityCell>();
                if (cell) lastCell = cell;
            }
        }

        return lastCell ?? FindLastCellInSection(sectionIndex - 1);
    }

    private static void SelectFirstCell()
    {
        foreach (VanityCell cell in _allCells)
        {
            if (cell && cell.gameObject.activeInHierarchy)
            {
                SelectCell(cell);
                return;
            }
        }
    }

    private static void SelectCell(VanityCell cell)
    {
        if (!cell) return;

        _selectedCell = cell;

        if (!_gamepadSelectionOverlay)
        {
            GameObject borderGo = new("GamepadSelection", typeof(RectTransform), typeof(Image), typeof(Outline));
            _gamepadSelectionOverlay = borderGo.GetComponent<Image>();
            _gamepadSelectionOverlay.color = Color.clear;
            _gamepadSelectionOverlay.raycastTarget = false;

            Outline outline = borderGo.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.8f, 0f, 1f);
            outline.effectDistance = new Vector2(3f, 3f);
            outline.useGraphicAlpha = false;

            RectTransform rt = (RectTransform)borderGo.transform;
            rt.SetParent(_content, true);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = CellSize;
        }

        RectTransform cellRT = (RectTransform)cell.transform;
        RectTransform borderRT = (RectTransform)_gamepadSelectionOverlay.transform;
        borderRT.position = cellRT.position;
        borderRT.SetAsLastSibling();
        _gamepadSelectionOverlay.gameObject.SetActive(true);

        Transform grid = cell.transform.parent;
        if (!grid.gameObject.activeSelf)
        {
            grid.gameObject.SetActive(true);
            int headerIndex = grid.GetSiblingIndex() - 1;
            if (headerIndex >= 0)
            {
                Transform header = _content.GetChild(headerIndex);
                TextMeshProUGUI txt = header.GetComponent<TextMeshProUGUI>();
                if (txt)
                {
                    string label = txt.text.Split(' ')[0];
                    ApplyHeaderStyle(txt, label, isExpanded: true);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        EnsureSelectionVisible();
    }

    private static void EnsureSelectionVisible()
    {
        if (!_selectedCell || !_scroll || !_viewport || !_content) return;

        RectTransform cellRT = (RectTransform)_selectedCell.transform;

        Vector3[] cellCorners = new Vector3[4];
        cellRT.GetWorldCorners(cellCorners);

        Vector3[] viewportCorners = new Vector3[4];
        _viewport.GetWorldCorners(viewportCorners);

        float cellTop = cellCorners[1].y;
        float cellBottom = cellCorners[0].y;
        float viewportTop = viewportCorners[1].y;
        float viewportBottom = viewportCorners[0].y;

        if (cellTop > viewportTop || cellBottom < viewportBottom)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 contentPos = _content.anchoredPosition;
            float viewportHeight = _viewport.rect.height;
            float contentHeight = _content.rect.height;

            float cellLocalY = -cellRT.anchoredPosition.y;
            float targetY = Mathf.Clamp(cellLocalY - viewportHeight / 2f, 0, Mathf.Max(0, contentHeight - viewportHeight));

            _content.anchoredPosition = new Vector2(contentPos.x, targetY);
        }
    }

    internal static void UpdateSelectedVisuals(VisSlot slot)
    {
        VisEquipment? ve = Player.m_localPlayer?.m_visEquipment;
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

        if (_cellsBySlot.TryGetValue(slot, out List<VanityCell>? list))
        {
            foreach (VanityCell? cell in list)
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
        Player? player = Player.m_localPlayer;
        if (!player) return;
        VisEquipment? ve = player.m_visEquipment;
        if (!ve) return;

        VanityAPI.ClearVanity(ve, VisSlot.Helmet);
        VanityAPI.ClearVanity(ve, VisSlot.Chest);
        VanityAPI.ClearVanity(ve, VisSlot.Legs);
        VanityAPI.ClearVanity(ve, VisSlot.Shoulder);
        VanityAPI.ClearVanity(ve, VisSlot.Utility);

        foreach (VisSlot slot in _cellsBySlot.Keys)
            UpdateSelectedVisuals(slot);

        VECloneSync.ResetStamp();
        VECloneSync.MirrorFrom(player, AzuEPICharacterPanel.playerPreviewComp);
    }

    private static void BuildPanel(InventoryGui gui)
    {
        RectTransform? crafting = gui.m_crafting;
        Transform? srcBkg = crafting.Find("Bkg");
        if (!srcBkg) return;

        Transform? vanity = Object.Instantiate(srcBkg, crafting);
        vanity.name = VanityPanelName;

        _panel = vanity.GetComponent<RectTransform>();
        RectTransform? srcRT = srcBkg.GetComponent<RectTransform>();
        _panel.anchorMin = srcRT.anchorMin;
        _panel.anchorMax = srcRT.anchorMax;
        _panel.SetAsLastSibling();

        Image? img = vanity.GetComponent<Image>();
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
            scrollRoot = new GameObject(VanityScrollRootName, typeof(RectTransform), typeof(ScrollRect));
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

        BuildContentStack(_viewport);

        _scroll.viewport = _viewport;
        _scroll.content = _content;

        _vbar = BuildScrollbar(_panel);
        _scroll.verticalScrollbar = _vbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    private static RectTransform BuildViewport(RectTransform parent)
    {
        GameObject go = new(VanityViewportName, typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        AnchorFill(rt);

        Image? img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        return rt;
    }

    private static void BuildContentStack(RectTransform parent)
    {
        GameObject go = new(VanityContentName,
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

    private static void BuildVanityToggleButton(InventoryGui gui)
    {
        Transform? src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src) return;

        VanityButtonGo = CloneButton(src, ToggleButtonParentGlg, VanityToggleButtonName, ToggleBtnAnchorMin, ToggleBtnAnchorMax, ToggleBtnPivot, ToggleBtnPos, ToggleBtnSize);
        Button? btn = VanityButtonGo.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            _visible = !_visible;
            SetVisible(_visible);
            if (PersonalLoadoutGui.IsVisible()) PersonalLoadoutGui.Hide();
            if (InventoryGui.instance)
            {
                var craftingPanel = InventoryGui.instance.m_crafting;
                craftingPanel.transform.Find("TabsButtons").SafeSetActive(!_visible);
                craftingPanel.transform.Find("RecipeList").SafeSetActive(!_visible);
                craftingPanel.transform.Find("Decription").SafeSetActive(!_visible);
            }
        });

        if (VanityButtonGo.TryGetComponent<UIGamePad>(out var gp))
        {
            if (ZInput.instance != null)
            {
                gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text = ZInput.instance.GetBoundKeyString("JoyLStick", true);
            }
            else
            {
                ZInput.Initialize();
                gp.m_hint.GetComponentInChildren<TextMeshProUGUI>(true).text = ZInput.instance.GetBoundKeyString("JoyLStick", true);
            }

            gp.m_zinputKey = "JoyLStick";
            gp.m_keyCode = KeyCode.JoystickButton8;
        }

        TMP_Text? label = VanityButtonGo.GetComponentInChildren<TMP_Text>();
        if (label) label.text = Localization.instance.Localize("$azuepi_vanity");

        _toggleBtn = btn;
        VanityButtonGo.gameObject.SetActive(VanityOption.Value.isOn());
    }

    private static void BuildResetButton(InventoryGui gui)
    {
        Transform? src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src || !_panel) return;

        Transform clone = CloneButton(src, _panel, ResetAllVanityButtonName, ResetBtnAnchorMin, ResetBtnAnchorMax, ResetBtnPivot, ResetBtnPos, ResetBtnSize);
        Button? btn = clone.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ResetAllVanities);

        TMP_Text? label = clone.GetComponentInChildren<TMP_Text>();
        if (label) label.text = Localization.instance.Localize("$azuepi_reset_vanity");

        _resetVanitiesBtn = btn;
    }

    private static RectTransform AddHeader(string label)
    {
        GameObject go = new($"Header_{label}", typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(_content, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);

        TextMeshProUGUI? txt = go.AddComponent<TextMeshProUGUI>();
        ApplyHeaderStyle(txt, label, isExpanded: false);

        Button? headerButton = go.AddComponent<Button>();
        headerButton.onClick.AddListener(() =>
        {
            int myIndex = rt.GetSiblingIndex();
            if (myIndex + 1 >= _content.childCount) return;

            Transform? next = _content.GetChild(myIndex + 1);
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

        string tri = isExpanded ? DownTriangle : UpTriangle;
        txt.text = $"{label} {HeaderTriangleSizeTag}{tri}</size>";
    }

    private static GridLayoutGroup AddGrid(string headerLabel)
    {
        GameObject go = new($"{headerLabel}_Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(_content, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);

        LayoutElement? le = go.GetComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.flexibleHeight = 0;

        GridLayoutGroup? grid = go.GetComponent<GridLayoutGroup>();
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
        GameObject? go = Object.Instantiate(slotPrefab, grid.transform);
        RectTransform rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;

        LayoutElement? le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = grid.cellSize.x;
        le.minHeight = le.preferredHeight = grid.cellSize.y;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        Image? icon = go.transform.Find("icon").GetComponent<Image>();
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

        GameObject labelGo = new("NoneLabel", typeof(RectTransform));
        RectTransform labelRT = (RectTransform)labelGo.transform;
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = new Vector2(0.5f, 0.5f);
        labelRT.pivot = new Vector2(0.5f, 0.5f);
        labelRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI? text = labelGo.AddComponent<TextMeshProUGUI>();
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

        GameObject selectedBadge = FindOrCreateSelectedBadge(go.transform);

        Button? btn = go.GetComponent<Button>();
        btn.onClick = new Button.ButtonClickedEvent();

        VanityCell? cell = go.GetComponent<VanityCell>() ?? go.AddComponent<VanityCell>();
        cell.Item = null;
        cell.Icon = icon;
        cell.Slot = slot;
        cell.SelectedBadge = selectedBadge;
        cell.IsNone = true;

        GetOrCreateSlotList(slot).Add(cell);
        _allCells.Add(cell);

        UITooltip? tooltipForNone = go.GetComponent<UITooltip>() ?? go.AddComponent<UITooltip>();
        tooltipForNone.m_topic = Localization.instance?.Localize("$menu_none") ?? "None";
        tooltipForNone.m_text = "";
    }

    private static void CreateCell(GameObject slotPrefab, GridLayoutGroup grid, ItemDrop.ItemData data, VisSlot slot)
    {
        GameObject? go = Object.Instantiate(slotPrefab, grid.transform);
        RectTransform rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;

        LayoutElement? le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = grid.cellSize.x;
        le.minHeight = le.preferredHeight = grid.cellSize.y;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        Image? icon = go.transform.Find("icon").GetComponent<Image>();
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

        GameObject selectedBadge = FindOrCreateSelectedBadge(go.transform);

        Button? btn = go.GetComponent<Button>();
        btn.onClick = new Button.ButtonClickedEvent();

        VanityCell? cell = go.GetComponent<VanityCell>() ?? go.AddComponent<VanityCell>();
        cell.Item = data;
        cell.Icon = icon;
        cell.Slot = slot;
        cell.SelectedBadge = selectedBadge;

        GetOrCreateSlotList(slot).Add(cell);
        _allCells.Add(cell);
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
        Transform? clone = Object.Instantiate(src, parent);
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

    private static void DisableChild(Transform root, string childName)
    {
        Transform? t = root.Find(childName);
        if (t) t.gameObject.SetActive(false);
    }

    private static void DisableChildrenContaining(Transform root, string contains)
    {
        int count = root.childCount;
        for (int i = 0; i < count; ++i)
        {
            Transform? t = root.GetChild(i);
            if (t.name.Contains(contains))
                t.gameObject.SetActive(false);
        }
    }

    private static GameObject FindOrCreateSelectedBadge(Transform root)
    {
        Transform? t = root.Find("selected") ?? root.Find("Selected");
        if (t) return t.gameObject;

        GameObject go = new("Selected", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        AnchorFill(rt);

        Image? img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1, 1, 1, 0.18f);
        go.SetActive(false);
        return go;
    }

    private static void ClearChildren(Transform t)
    {
        int count = t.childCount;
        for (int i = count - 1; i >= 0; --i)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    private static List<VanityCell> GetOrCreateSlotList(VisSlot slot)
    {
        if (!_cellsBySlot.TryGetValue(slot, out List<VanityCell>? list))
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

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
static class Vanity_GamepadUpdate
{
    static void Postfix()
    {
        VanityPanelController.UpdateGamepadNavigation();
    }
}

[HarmonyPatch(typeof(UnifiedPopup), nameof(UnifiedPopup.IsVisible))]
static class UnifiedPopupIsVisiblePatch
{
    static bool Prefix(ref bool __result)
    {
        if (!Player.m_localPlayer || !VanityPanelController.IsVanityPanelVisible()) return true;
        __result = true;
        return false;
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
    private bool? _lastKnownState = null;

    private void Awake()
    {
        UITooltip? tooltip = gameObject.GetComponent<UITooltip>();
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
        if (!Icon || Item?.m_shared == null) return;

        Player? player = Player.m_localPlayer;
        if (player == null) return;

        bool known = player.IsKnownMaterial(Item.m_shared.m_name);
        if (_lastKnownState.HasValue && _lastKnownState.Value == known) return;

        _lastKnownState = known;
        Icon.color = known ? Color.white : Color.black;
    }

    public void OnRightClick(UIInputHandler _)
    {
        VisEquipment? ve = Player.m_localPlayer?.m_visEquipment;
        if (!ve) return;

        if (IsNone)
        {
            VanityAPI.SetHidden(ve, Slot, false);
            VanityPanelController.UpdateSelectedVisuals(Slot);
            VECloneSync.ResetStamp();
            VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
            return;
        }

        if (Item?.m_dropPrefab == null) return;
        VanityAPI.ClearVanity(ve, Slot);
        VanityPanelController.UpdateSelectedVisuals(Slot);
        VECloneSync.ResetStamp();
        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }

    public void OnLeftClick(UIInputHandler _)
    {
        VisEquipment? ve = Player.m_localPlayer?.m_visEquipment;
        if (!ve) return;

        if (IsNone)
        {
            VanityAPI.SetHidden(ve, Slot, true);
            VanityPanelController.UpdateSelectedVisuals(Slot);
            VECloneSync.ResetStamp();
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
                break;
            case VisSlot.Chest:
                VanityAPI.SetVanity(ve, VisSlot.Chest, prefab);
                break;
            case VisSlot.Legs:
                VanityAPI.SetVanity(ve, VisSlot.Legs, prefab);
                break;
            case VisSlot.Shoulder:
                VanityAPI.SetVanity(ve, VisSlot.Shoulder, prefab, variant: 0);
                break;
            case VisSlot.Utility:
                VanityAPI.SetVanity(ve, VisSlot.Utility, prefab);
                break;
        }

        VanityPanelController.UpdateSelectedVisuals(Slot);
        // Reset stamp so MirrorFrom actually runs (vanity changes don't affect the stamp)
        VECloneSync.ResetStamp();
        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }
}