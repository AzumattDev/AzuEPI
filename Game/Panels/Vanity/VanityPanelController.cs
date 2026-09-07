using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Game.Panels.Vanity;

internal static class VanityPanelController
{
    public const string VanityPanelName = $"{Prefix}VanityPanel";
    public const string VanityScrollRootName = "ScrollRoot";
    public const string VanityViewportName = "Viewport";
    public const string VanityContentName = "Content";
    public const string VanityScrollbarName = "Scrollbar";
    public const string VanityToggleButtonName = $"{Prefix}VanityToggleButton";
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
    private static readonly List<VanityCell> _allCells = [];
    private static Image _gamepadSelectionOverlay;

    private static readonly Dictionary<VisSlot, List<VanityCell>> _cellsBySlot = new();

    private static readonly Dictionary<VisSlot, TextMeshProUGUI> _headersBySlot = new();
    private static readonly Dictionary<TextMeshProUGUI, (string baseLabel, VisSlot slot)> _headerInfo = new();
    private static readonly Dictionary<VisSlot, GameObject> _resetButtonsBySlot = new();
    private static readonly Dictionary<VisSlot, RectTransform> _headerRectsBySlot = new();

    private static readonly List<GameObject> _reusableGameObjectList = new(256);
    private static readonly Dictionary<string, ItemDrop> _reusableDropsDict = new(256);
    private static readonly List<ItemDrop.ItemData> _reusableVanityItems = new(128);

    private static Player _cachedPlayer = null;
    private static bool _hasCompleteBuild = false;

    public static void EnsureBuilt(InventoryGui gui)
    {
        if (!gui) return;

        if (!_panel)
        {
            BuildPanel(gui);
        }

        if (!_scroll) BuildScrollTree();
        if (!_toggleBtn) BuildVanityToggleButton(gui);
        if (!_resetVanitiesBtn) BuildResetButton(gui);

        _fontSample = gui.m_craftButton?.GetComponentInChildren<TMP_Text>();

        RefreshGridIfNeeded();

        SetVisible(_visible);
    }

    public static void InvalidateCache()
    {
        _cachedPlayer = null;
        _hasCompleteBuild = false;
        VanityLookup.InvalidateCache();
    }

    private static bool NeedsRebuild()
    {
        Player? player = Player.m_localPlayer;
        ObjectDB? odb = ObjectDB.instance;

        if (!player || !odb) return false;

        if (_cachedPlayer != player)
            return true;

        if (_allCells.Count == 0)
            return true;

        if (!_hasCompleteBuild && _allCells.Count < 15)
            return true;

        return false;
    }

    internal static void UpdateUnknownVisibility()
    {
        Player? player = Player.m_localPlayer;
        if (!player) return;

        bool hide = HideUnknownVanityItems.Value.isOn();
        bool changed = false;

        foreach (VanityCell cell in _allCells)
        {
            if (!cell || cell.IsNone || cell.Item?.m_shared == null) continue;

            bool show = !hide || player.IsKnownMaterial(cell.Item.m_shared.m_name);
            if (cell.gameObject.activeSelf == show) continue;

            cell.gameObject.SetActive(show);
            changed = true;
        }

        if (changed && _content) LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    public static void RefreshGridIfNeeded()
    {
        if (!NeedsRebuild()) return;

        RefreshGrid();
    }

    public static bool IsVisible()
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

            UpdateUnknownVisibility();

            foreach (VisSlot slot in _cellsBySlot.Keys)
                UpdateSelectedVisuals(slot);

            if (ZInput.IsGamepadActive())
            {
                ExpandAllSections();
                UpdateEquippedBorders();
                SelectFirstCell();
            }
        }
        else
        {
            _selectedCell = null;

            foreach (VanityCell cell in _allCells)
            {
                if (cell && cell.SelectedBadge)
                    cell.SelectedBadge.SetActive(false);
                if (cell && cell.EquippedBorder)
                    cell.EquippedBorder.SetActive(false);
            }

            SlotOverlays.HideAllVanityOverlays();
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
                    if (txt && _headerInfo.TryGetValue(txt, out (string baseLabel, VisSlot slot) info))
                    {
                        ApplyHeaderStyle(txt, info.baseLabel, info.slot, isExpanded: true);
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

        _cachedPlayer = player;

        _cellsBySlot.Clear();
        _headersBySlot.Clear();
        _headerInfo.Clear();
        _resetButtonsBySlot.Clear();
        _headerRectsBySlot.Clear();
        _allCells.Clear();
        _selectedCell = null;
        PanelUtilities.ClearChildren(_content);

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
#if DEBUG
        int rejectedNoAttach = 0;
#endif
        foreach (ItemDrop? drop in _reusableDropsDict.Values)
        {
            ItemDrop.ItemData? d = drop.m_itemData;
            GameObject prefab = d?.m_dropPrefab ? d.m_dropPrefab : drop.gameObject;

            if (d is { m_shared.m_icons.Length: > 0 } && prefab &&
                (prefab.HasChildWithNameThatContains("attach") || prefab.HasChildWithNameThatContains("log")) &&
                string.IsNullOrWhiteSpace(d.m_shared.m_dlc) &&
                d.m_shared.m_itemType is ItemDrop.ItemData.ItemType.Helmet or ItemDrop.ItemData.ItemType.Chest or ItemDrop.ItemData.ItemType.Legs or ItemDrop.ItemData.ItemType.Shoulder or ItemDrop.ItemData.ItemType.Utility)
            {
                if (d.m_dropPrefab == null) d.m_dropPrefab = drop.gameObject;
                _reusableVanityItems.Add(d);
            }
#if DEBUG
            else if (d is { m_shared.m_icons.Length: > 0 } && prefab &&
                     string.IsNullOrWhiteSpace(d.m_shared.m_dlc) &&
                     d.m_shared.m_itemType is ItemDrop.ItemData.ItemType.Helmet or ItemDrop.ItemData.ItemType.Chest or ItemDrop.ItemData.ItemType.Legs or ItemDrop.ItemData.ItemType.Shoulder or ItemDrop.ItemData.ItemType.Utility &&
                     !(prefab.HasChildWithNameThatContains("attach") || prefab.HasChildWithNameThatContains("log")))
            {
                rejectedNoAttach++;
                AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Vanity: Rejected '{d.m_shared.m_name}' - no attach/log child in prefab '{prefab.name}'");
            }
#endif
        }
#if DEBUG
        AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Vanity panel: Found {_reusableVanityItems.Count} valid items, {rejectedNoAttach} rejected for missing attach/log");
#endif

        if (_reusableVanityItems.Count >= 15)
            _hasCompleteBuild = true;

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
                    ItemDrop.ItemData.ItemType.Helmet => "$azu_epi_helmet",
                    ItemDrop.ItemData.ItemType.Chest => "$azu_epi_chest",
                    ItemDrop.ItemData.ItemType.Legs => "$azu_epi_legs",
                    ItemDrop.ItemData.ItemType.Shoulder => "$azu_epi_shoulder",
                    ItemDrop.ItemData.ItemType.Utility => "$item_utility",
                    _ => itemType.ToString(),
				};

                string? headerLabel = Localization.instance.Localize(headerLocKey);
                VisSlot slot = MapItemTypeToVisSlot(itemType);
                AddHeader(headerLabel, slot);
                currentGrid = AddGrid(headerLabel);

                CreateNoneCell(slotPrefab, currentGrid, MapItemTypeToVisSlot(itemType));
            }

            if (currentGrid != null)
            {
                int variants = data.m_shared.m_variants;
                if (variants <= 1)
                {
                    CreateCell(slotPrefab, currentGrid, data, MapItemTypeToVisSlot(itemType), 0);
                }
                else
                {
                    for (int v = 0; v < variants && v < data.m_shared.m_icons.Length; v++)
                        CreateCell(slotPrefab, currentGrid, data, MapItemTypeToVisSlot(itemType), v);
                }
            }
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
        else if (ZInput.GetButtonDown("JoyLTrigger"))
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

        List<VanityCell> gridCells = [];
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

        if (_selectedCell != null && _selectedCell.SelectedBadge != null)
        {
            _selectedCell.SelectedBadge.SetActive(false);
        }

        _selectedCell = cell;

        if (_selectedCell.SelectedBadge != null)
        {
            _selectedCell.SelectedBadge.SetActive(true);
        }

        UpdateEquippedBorders();

        Transform grid = cell.transform.parent;
        if (!grid.gameObject.activeSelf)
        {
            grid.gameObject.SetActive(true);
            int headerIndex = grid.GetSiblingIndex() - 1;
            if (headerIndex >= 0)
            {
                Transform header = _content.GetChild(headerIndex);
                TextMeshProUGUI txt = header.GetComponent<TextMeshProUGUI>();
                if (txt && _headerInfo.TryGetValue(txt, out (string baseLabel, VisSlot slot) info))
                {
                    ApplyHeaderStyle(txt, info.baseLabel, info.slot, isExpanded: true);
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
        UpdateHeaderLabel(slot);

        // When gamepad is active, don't update selected badges automatically
        if (ZInput.IsGamepadActive() && _visible)
        {
            UpdateEquippedBorders();
            return;
        }

        VisEquipment? ve = Player.m_localPlayer?.m_visEquipment;
        if (!ve) return;

        VanityState vs = VanitySlots.GetState(ve, slot);

        if (_cellsBySlot.TryGetValue(slot, out List<VanityCell>? list))
        {
            foreach (VanityCell? cell in list)
            {
                bool isSelected = cell.IsNone
                    ? vs.IsHidden
                    : (vs.HasVanity &&
                       cell.Item?.m_dropPrefab &&
                       cell.Item.m_dropPrefab.name.GetStableHashCode() == vs.Hash &&
                       cell.Variant == vs.Variant);

                if (cell.SelectedBadge) cell.SelectedBadge.SetActive(isSelected);
            }
        }
    }

    private static void UpdateEquippedBorders()
    {
        VisEquipment? ve = Player.m_localPlayer?.m_visEquipment;
        if (!ve) return;

        foreach (VisSlot slot in _cellsBySlot.Keys)
        {
            VanityState vs = VanitySlots.GetState(ve, slot);
            List<VanityCell>? cellList = GetOrCreateSlotList(slot);
            if (cellList == null) continue;

            foreach (VanityCell? cell in cellList)
            {
                if (!cell || !cell.EquippedBorder) continue;

                bool isEquipped = cell.IsNone
                    ? vs.IsHidden
                    : (vs.HasVanity &&
                       cell.Item?.m_dropPrefab &&
                       cell.Item.m_dropPrefab.name.GetStableHashCode() == vs.Hash &&
                       cell.Variant == vs.Variant);

                cell.EquippedBorder.SetActive(isEquipped);
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
        _panel = PanelUtilities.BuildPanel(gui, VanityPanelName);
        _panel.GetOrAddComponent<Localize>();
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
        PanelUtilities.AnchorFill(rootRT);
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
        return PanelUtilities.BuildViewport(parent, VanityViewportName);
    }

    private static void BuildContentStack(RectTransform parent)
    {
        _content = PanelUtilities.BuildVerticalContent(parent, VanityContentName, new RectOffset(12, 12, 12, 12), 10f);
        _stack = _content.GetComponent<VerticalLayoutGroup>();
        _fitter = _content.GetComponent<ContentSizeFitter>();
    }

    private static Scrollbar BuildScrollbar(Transform panelParent)
    {
        return PanelUtilities.BuildScrollbar(panelParent, new PanelUtilities.ScrollbarConfig
        {
            Name = VanityScrollbarName,
            AnchorMin = BarAnchorMin,
            AnchorMax = BarAnchorMax,
            Pivot = BarPivot,
            OffsetMin = BarOffsetMin,
            OffsetMax = BarOffsetMax,
		});
    }

    private static void BuildVanityToggleButton(InventoryGui gui)
    {
        PanelUtilities.ButtonConfig config = new(
            name: VanityToggleButtonName,
            anchorMin: ToggleBtnAnchorMin,
            anchorMax: ToggleBtnAnchorMax,
            pivot: ToggleBtnPivot,
            anchoredPosition: ToggleBtnPos,
            size: ToggleBtnSize,
            gamepadKey: PanelUtilities.KeyCodeToZInputKey(VanityToggleGamepadKey.Value),
            gamepadKeyCode: VanityToggleGamepadKey.Value,
            label: "👕",
            labelFontSize: ToggleButtonFontSize,
            onClick: () =>
            {
                _visible = !_visible;
                SetVisible(_visible);
                if (PersonalLoadoutGui.IsVisible()) PersonalLoadoutGui.Hide();
                if (StatsPanelController.IsVisible()) StatsPanelController.Hide();
                PanelUtilities.HideCraftingElements(_visible);
            }
        );

        (VanityButtonGo, _toggleBtn) = PanelUtilities.BuildToggleButton(gui, ToggleButtonParentGlg, config);
        VanityButtonGo.gameObject.SetActive(VanityOption.Value.isOn());
    }

    private static void BuildResetButton(InventoryGui gui)
    {
        Transform? src = gui.m_takeAllButton?.transform ?? gui.m_craftButton?.transform;
        if (!src || !_panel) return;

        Transform clone = PanelUtilities.CloneButton(src, _panel, ResetAllVanityButtonName, ResetBtnAnchorMin, ResetBtnAnchorMax, ResetBtnPivot, ResetBtnPos, ResetBtnSize);

        PanelUtilities.BindGamePad(clone, PanelUtilities.KeyCodeToZInputKey(KeyCode.JoystickButton15), KeyCode.None, gui);

        Button? btn = clone.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ResetAllVanities);

        TMP_Text? label = clone.GetComponentInChildren<TMP_Text>();
        if (label) label.text = Localization.instance.Localize("$azuepi_reset_vanity");

        _resetVanitiesBtn = btn;
    }

    private static RectTransform AddHeader(string label, VisSlot slot)
    {
        GameObject containerGo = new($"HeaderRow_{label}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform containerRt = (RectTransform)containerGo.transform;
        containerRt.SetParent(_content, false);

        HorizontalLayoutGroup hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        LayoutElement containerLe = containerGo.GetComponent<LayoutElement>();
        containerLe.minHeight = 26f;
        containerLe.preferredHeight = 26f;
        containerLe.flexibleWidth = 1;

        CreateSlotResetButton(containerRt, slot);

        GameObject textGo = new($"Header_{label}", typeof(RectTransform), typeof(LayoutElement));
        RectTransform textRt = (RectTransform)textGo.transform;
        textRt.SetParent(containerRt, false);

        LayoutElement textLe = textGo.GetComponent<LayoutElement>();
        textLe.flexibleWidth = 1;
        textLe.minHeight = 24f;
        textLe.preferredHeight = 24f;

        TextMeshProUGUI? txt = textGo.AddComponent<TextMeshProUGUI>();
        ApplyHeaderStyle(txt, label, slot, isExpanded: false);

        _headersBySlot[slot] = txt;
        _headerInfo[txt] = (label, slot);
        _headerRectsBySlot[slot] = containerRt;

        Button? headerButton = textGo.AddComponent<Button>();
        headerButton.onClick.AddListener(() =>
        {
            int myIndex = containerRt.GetSiblingIndex();
            if (myIndex + 1 >= _content.childCount) return;

            Transform? next = _content.GetChild(myIndex + 1);
            bool newActive = !next.gameObject.activeSelf;
            next.gameObject.SetActive(newActive);

            ApplyHeaderStyle(txt, label, slot, isExpanded: newActive);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        });

        return containerRt;
    }

    private static void CreateSlotResetButton(RectTransform parent, VisSlot slot)
    {
        InventoryGui? gui = InventoryGui.instance;
        Transform? src = gui?.m_takeAllButton?.transform ?? gui?.m_craftButton?.transform;
        if (!src) return;

        Transform clone = Object.Instantiate(src, parent);
        clone.name = $"ResetBtn_{slot}";

        RectTransform btnRt = (RectTransform)clone;

        LayoutElement le = clone.gameObject.GetComponent<LayoutElement>() ?? clone.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = false;
        le.minWidth = 50f;
        le.preferredWidth = 50f;
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        UIGamePad? gp = clone.GetComponent<UIGamePad>();
        if (gp)
        {
            if (gp.m_hint) gp.m_hint.gameObject.SetActive(false);
            Object.Destroy(gp);
        }

        Button? btn = clone.GetComponent<Button>();
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ResetSlotVanity(slot));
        }

        TMP_Text? label = clone.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.text = "X";
            label.fontSize = 14f;
        }

        _resetButtonsBySlot[slot] = clone.gameObject;

        clone.gameObject.SetActive(false);
    }

    private static void ResetSlotVanity(VisSlot slot)
    {
        Player? player = Player.m_localPlayer;
        if (!player) return;
        VisEquipment? ve = player.m_visEquipment;
        if (!ve) return;

        VanityAPI.ClearVanity(ve, slot);
        UpdateSelectedVisuals(slot);

        VECloneSync.ResetStamp();
        VECloneSync.MirrorFrom(player, AzuEPICharacterPanel.playerPreviewComp);
    }

    private static void UpdateResetButtonVisibility(VisSlot slot)
    {
        if (!_resetButtonsBySlot.TryGetValue(slot, out GameObject? btn) || !btn) return;

        Player? player = Player.m_localPlayer;
        if (!player)
        {
            btn.SetActive(false);
            return;
        }

        VisEquipment? ve = player.m_visEquipment;
        if (!ve)
        {
            btn.SetActive(false);
            return;
        }

        VanityState vs = VanitySlots.GetState(ve, slot);
        bool hasVanityOrHidden = vs.HasVanity || vs.IsHidden;
        btn.SetActive(hasVanityOrHidden);
    }

    private static void UpdateAllResetButtonVisibility()
    {
        foreach (VisSlot slot in _resetButtonsBySlot.Keys)
            UpdateResetButtonVisibility(slot);
    }

    private static void ApplyHeaderStyle(TextMeshProUGUI txt, string label, VisSlot slot, bool isExpanded)
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
        string vanityItemName = GetCurrentVanityItemName(slot);
        if (!string.IsNullOrEmpty(vanityItemName))
            txt.text = $"{label}: {vanityItemName} {HeaderTriangleSizeTag}{tri}</size>";
        else
            txt.text = $"{label} {HeaderTriangleSizeTag}{tri}</size>";
    }

    private static string GetCurrentVanityItemName(VisSlot slot)
    {
        Player? player = Player.m_localPlayer;
        if (!player) return null;

        VisEquipment? ve = player.m_visEquipment;
        if (!ve) return null;

        VanityState vs = VanitySlots.GetState(ve, slot);

        if (vs.IsHidden)
            return Localization.instance?.Localize("$azuepi_hidden") ?? "Hidden";

        if (!vs.HasVanity || vs.Hash == 0)
            return null;

        if (VanityLookup.TryGetByHash(vs.Hash, out _, out ItemDrop itemDrop) && itemDrop?.m_itemData?.m_shared != null)
            return Localization.instance?.Localize(itemDrop.m_itemData.m_shared.m_name) ?? itemDrop.m_itemData.m_shared.m_name;

        return null;
    }

    private static void UpdateHeaderLabel(VisSlot slot)
    {
        if (!_headersBySlot.TryGetValue(slot, out TextMeshProUGUI txt) || !txt) return;
        if (!_headerInfo.TryGetValue(txt, out (string baseLabel, VisSlot slot) info)) return;

        bool isExpanded = txt.text.Contains(DownTriangle);
        ApplyHeaderStyle(txt, info.baseLabel, slot, isExpanded);

        UpdateResetButtonVisibility(slot);
    }

    public static void UpdateAllHeaderLabels()
    {
        foreach (KeyValuePair<TextMeshProUGUI, (string baseLabel, VisSlot slot)> kvp in _headerInfo)
        {
            if (!kvp.Key) continue;

            bool isExpanded = kvp.Key.text.Contains(DownTriangle);

            ApplyHeaderStyle(kvp.Key, kvp.Value.baseLabel, kvp.Value.slot, isExpanded);
        }
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

        PanelUtilities.DisableChild(go.transform, "amount");
        PanelUtilities.DisableChild(go.transform, "equiped");
        PanelUtilities.DisableChild(go.transform, "queued");
        PanelUtilities.DisableChild(go.transform, "noteleport");
        PanelUtilities.DisableChild(go.transform, "foodicon");
        PanelUtilities.DisableChild(go.transform, "durability");
        PanelUtilities.DisableChild(go.transform, "quality");
        PanelUtilities.DisableChild(go.transform, "binding");
        PanelUtilities.DisableChildrenContaining(go.transform, "JC_");

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
        GameObject equippedBorder = FindOrCreateEquippedBadge(go.transform);

        Button? btn = go.GetComponent<Button>();
        btn.onClick = new Button.ButtonClickedEvent();

        VanityCell? cell = go.GetComponent<VanityCell>() ?? go.AddComponent<VanityCell>();
        cell.Item = null;
        cell.Icon = icon;
        cell.Slot = slot;
        cell.SelectedBadge = selectedBadge;
        cell.EquippedBorder = equippedBorder;
        cell.IsNone = true;

        GetOrCreateSlotList(slot).Add(cell);
        _allCells.Add(cell);

        UITooltip? tooltipForNone = go.GetComponent<UITooltip>() ?? go.AddComponent<UITooltip>();
        tooltipForNone.m_topic = Localization.instance?.Localize("$menu_none") ?? "None";
        tooltipForNone.m_text = "";
    }

    private static void CreateCell(GameObject slotPrefab, GridLayoutGroup grid, ItemDrop.ItemData data, VisSlot slot, int variant = 0)
    {
        Sprite[]? icons = data.m_shared.m_icons;
        if (icons == null || variant >= icons.Length || icons[variant] == null) return;

        GameObject? go = Object.Instantiate(slotPrefab, grid.transform);
        RectTransform rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;

        LayoutElement? le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = grid.cellSize.x;
        le.minHeight = le.preferredHeight = grid.cellSize.y;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        Image? icon = go.transform.Find("icon").GetComponent<Image>();
        icon.sprite = icons[variant];
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = Color.white;

        PanelUtilities.DisableChild(go.transform, "amount");
        PanelUtilities.DisableChild(go.transform, "equiped");
        PanelUtilities.DisableChild(go.transform, "queued");
        PanelUtilities.DisableChild(go.transform, "noteleport");
        PanelUtilities.DisableChild(go.transform, "foodicon");
        PanelUtilities.DisableChild(go.transform, "durability");
        PanelUtilities.DisableChild(go.transform, "quality");
        PanelUtilities.DisableChild(go.transform, "binding");
        PanelUtilities.DisableChildrenContaining(go.transform, "JC_");

        GameObject selectedBadge = FindOrCreateSelectedBadge(go.transform);
        GameObject equippedBorder = FindOrCreateEquippedBadge(go.transform);

        Button? btn = go.GetComponent<Button>();
        btn.onClick = new Button.ButtonClickedEvent();

        VanityCell? cell = go.GetComponent<VanityCell>() ?? go.AddComponent<VanityCell>();
        cell.Item = data;
        cell.Icon = icon;
        cell.Slot = slot;
        cell.Variant = variant;
        cell.SelectedBadge = selectedBadge;
        cell.EquippedBorder = equippedBorder;

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
        _ => throw new NotSupportedException($"Vanity not supported for {t}"),
	};

    private static GameObject FindOrCreateSelectedBadge(Transform root)
    {
        Transform? t = root.Find("selected") ?? root.Find("Selected");
        if (t) return t.gameObject;

        GameObject go = new("Selected", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        PanelUtilities.AnchorFill(rt);

        Image? img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1, 1, 1, 0.18f);
        go.SetActive(false);
        return go;
    }

    private static GameObject FindOrCreateEquippedBadge(Transform root)
    {
        Transform? t = root.Find("equiped") ?? root.Find("equiped_jc_disabled");
        if (t) return t.gameObject;

        GameObject go = new("equiped", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        PanelUtilities.AnchorFill(rt);

        Image? img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1, 1, 1, 0.18f);
        go.SetActive(false);
        return go;
    }

    private static List<VanityCell> GetOrCreateSlotList(VisSlot slot)
    {
        if (!_cellsBySlot.TryGetValue(slot, out List<VanityCell>? list))
        {
            list = [];
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
    }
}

public class VanityCell : MonoBehaviour
{
    public ItemDrop.ItemData Item;
    public Image Icon;
    public VisSlot Slot;
    public int Variant;
    public GameObject SelectedBadge;
    public GameObject EquippedBorder;
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
        VanityAPI.SetVanity(ve, Slot, prefab, variant: Variant);

        VanityPanelController.UpdateSelectedVisuals(Slot);
        // Reset stamp so MirrorFrom actually runs (vanity changes don't affect the stamp)
        VECloneSync.ResetStamp();
        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
    }
}
