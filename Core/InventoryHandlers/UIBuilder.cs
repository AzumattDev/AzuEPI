using AzuEPI.Game.Loadout;
using AzuEPI.Game.PlayerPreview;
using AzuEPI.Game.Vanity;

namespace AzuEPI.Core.InventoryHandlers;

public class UIBuilder
{
    public const string Prefix = "AzuEPI_";
    public const string QabName = $"{Prefix}QuickAccessBar";
    public const string AzuEquipmentBkgName = $"{Prefix}EquipmentBkg";
    public const string AzuPlayerBkgName = $"{Prefix}PlayerBkg";
    public const string DropAllButtonName = $"{Prefix}DropAllButton";
    public const string ToggleButtonsGlgName = $"{Prefix}ToggleButtonsGlg";
    public const string RuntimePanelName = $"{Prefix}RuntimePanel";
    public const string PlayerPreviewName = $"{Prefix}PlayerPreview";
    public const string PlayerPreviewImageName = $"{Prefix}PlayerPreviewImg";
    public const string CharacterName = $"{Prefix}CharacterName";

    public const string MinimalUiguid = "Azumatt.MinimalUI";

    internal static RectTransform _epiPreviewRect;
    public static GameObject PreviewParent = null!;
    public static GameObject PlayerPreviewImage = null!;
    public static Transform CharName = null!;
    public static GameObject GlgGo = null!;
    public static RectTransform GlgRt = null!;

    public const int columns = 2;
    public const int gapTiles = 4;
    public const float padding = 0.6f;
    public const float extraTiles = columns + gapTiles + padding;
    public const float totalWidth = 570f;

    public static void RebuildUI()
    {
        if (!InventoryGui.instance) return;

        InventoryGui.instance.m_player.Find(AzuEquipmentBkgName).SafeSetActive(OldLayout.Value.isOn());
        Layout.AzuPlayerBkg.SafeSetActive(OldLayout.Value.isOff());
        PreviewParent.SafeSetActive(OldLayout.Value.isOff());
        PlayerPreviewImage.SafeSetActive(OldLayout.Value.isOff());
        CharName.SafeSetActive(OldLayout.Value.isOff());
        GUICache.ButtonGridLayoutGroup.constraintCount = OldLayout.Value.isOff() ? 2 : QuickSlotsAmount.Value < 1 ? 1 : 2;
        GUICache.ButtonGridLayoutGroup.childAlignment = OldLayout.Value.isOff() ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        GUICache.ButtonGridLayoutGroup.cellSize = OldLayout.Value.isOff() ? new Vector2(118f, 32f) : new Vector2(170f, 32f);
    }

    public static void BuildEquipmentBkg(InventoryGui invGui, RectTransform bkgRect)
    {
        Transform transform = Object.Instantiate(bkgRect.transform, invGui.m_player);
        transform.SetAsFirstSibling();
        transform.name = AzuEquipmentBkgName;

        Vector2 maxAnchor = Layout.GetEquipmentBackAnchorMax();
        if (Chainloader.PluginInfos.TryGetValue(MinimalUiguid, out PluginInfo? pluginInfo) && pluginInfo is not null)
            maxAnchor.x += 0.03f;

        RectTransform equipBkgRT = transform.GetComponent<RectTransform>();
        equipBkgRT.WithAnchors(new Vector2(1f, 0f), maxAnchor);

        if (OldLayout.Value.isOn() && VanityOption.Value.isOff() && LoadoutOption.Value.isOff())
        {
            equipBkgRT.offsetMin = new Vector2(0f, (Layout.tileSize - 10));
        }
        else
        {
            equipBkgRT.offsetMin = new Vector2(-10,-10);
        }

        InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().WithAnchorMax(maxAnchor);

        InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;
        transform.gameObject.SetActive(OldLayout.Value.isOn());
    }

    public static void BuildToggleButtonGlg(InventoryGui invGui)
    {
        GlgGo = GameObjectHelper.CreateUIObject(ToggleButtonsGlgName, typeof(GridLayoutGroup))
            .WithParent(OldLayout.Value.isOff() ? invGui.m_crafting.transform : invGui.m_player.transform, false);

        GlgRt = (RectTransform)GlgGo.transform;
        GlgRt.WithAnchors(new Vector2(0f, 1f), new Vector2(0f, 1f))
            .WithPivot(new Vector2(0.5f, 1f))
            .WithAnchoredPosition(OldLayout.Value.isOff() ? Layout.ToggleButtonsHlgAnchoredPos : Layout.ToggleButtonsHlgAnchoredPosOldVert)
            .WithSizeDelta(new Vector2(270f, 32f));

        GridLayoutGroup? glg = GlgGo.GetComponent<GridLayoutGroup>();
        glg.childAlignment = TextAnchor.MiddleCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.spacing = new Vector2(35f, 5f);
        glg.cellSize = new Vector2(118f, 32f);

        GUICache.ButtonGridLayoutGroup = glg;
        VanityPanelController.ToggleButtonParentGlg = GlgRt;
        PersonalLoadoutGui.ToggleButtonParentGlg = GlgRt;
    }

    public static void EnsureVanityPanelBuilt(InventoryGui invGui)
    {
        VanityPanelController.EnsureBuilt(invGui);
    }

    public static void BuildLoadoutToggles(InventoryGui invGui)
    {
        PersonalLoadoutGui.BuildLoadoutToggleButton(invGui);
    }

    public static void CreateExtendedCraftingPanel(InventoryGui invGui, RectTransform selectedFrame)
    {
        Layout.AzuPlayerBkg = Object.Instantiate(invGui.m_crafting.Find("Bkg"), invGui.m_crafting);
        Layout.AzuPlayerBkg.SetSiblingIndex(selectedFrame.GetSiblingIndex() + 2);
        Layout.AzuPlayerBkg.name = AzuPlayerBkgName;
        Layout.AzuPlayerBkg.GetComponent<RectTransform>().WithAnchorMin(Layout.PlayerBkgAnchorMin);
        Layout.AzuPlayerBkg.gameObject.SetActive(OldLayout.Value.isOff());
    }

    public static void CreateRuntimePanel()
    {
        if (AzuEPICharacterPanel.instance == null)
            new GameObject(RuntimePanelName).AddComponent<AzuEPICharacterPanel>();
    }

    public static void CreateAzuEpiPreview(InventoryGui invGui, out RectTransform previewParentRT)
    {
        PreviewParent = GameObjectHelper.CreateUIObject(PlayerPreviewName, typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(PlayerRotationController))
            .WithParent(invGui.m_crafting, false)
            .WithActive(OldLayout.Value.isOff());

        previewParentRT = (RectTransform)PreviewParent.transform;
        previewParentRT.WithAnchors(Layout.PreviewAnchorMin, Layout.PreviewAnchorMax)
            .WithSizeDelta(Layout.PreviewSizeDelta)
            .WithAnchoredPosition(Layout.PreviewAnchoredPos);

        PreviewParent.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.565f);
    }

    public static void CreatePlayerPreviewImage(RectTransform previewParentRT)
    {
        PlayerPreviewImage = GameObjectHelper.CreateUIObject(PlayerPreviewImageName, typeof(CanvasRenderer), typeof(RawImage))
            .WithParent(previewParentRT, false)
            .WithActive(OldLayout.Value.isOff());

        RectTransform rt = (RectTransform)PlayerPreviewImage.transform;
        rt.WithSizeDelta(Layout.PlayerPreviewImageSize)
            .WithAnchoredPosition(Vector2.zero);

        RawImage? raw = PlayerPreviewImage.GetComponent<RawImage>();
        raw.raycastTarget = false;
        raw.color = Color.white;

        AzuEPICharacterPanel.instance.render = rt;
        AzuEPICharacterPanel.instance.renderRawImage = raw;
        _epiPreviewRect = rt;
    }

    public static void SetupPreviewPanel()
    {
        PlayerPreviewManager.Initialize();
        PlayerPreviewManager.Instance.CreatePreviewCamera();
        PlayerPreviewManager.Instance.CreatePreviewLights();
        PlayerPreviewManager.Instance.UpdateRenderTexture();
    }

    public static void CreateCharacterName(InventoryGui invGui, RectTransform previewParentRT)
    {
        CharName = Object.Instantiate(invGui.m_info.transform.Find("TitlePanel"), previewParentRT);
        CharName.name = CharacterName;
        CharName.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault()!.text = global::Game.instance.GetPlayerProfile().GetName();
        foreach (Transform child in CharName)
            if (child.name.Contains("BraidLine"))
                Object.Destroy(child.gameObject);
    }

    public static void BuildDropAllButton(InventoryGui invGui)
    {
        if (invGui.m_player.Find(DropAllButtonName)) return;

        Transform dropAllButtonPrefab = invGui.m_takeAllButton.transform;
        Transform dropAllButtonTransform = Object.Instantiate(dropAllButtonPrefab, invGui.m_player);
        dropAllButtonTransform.name = DropAllButtonName;

        RectTransform? rectTransform = dropAllButtonTransform.GetComponent<RectTransform>();
        rectTransform.SetAsFirstSibling();
        rectTransform.WithAnchors(Layout.DropAllAnchorMin, Layout.DropAllAnchorMax)
            .WithPivot(Layout.DropAllPivot)
            .WithAnchoredPosition(DropAllButtonPosition.Value)
            .WithSizeDelta(Layout.DropAllSize);

        rectTransform.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$azuepi_dropall");

        Button? buttonComp = dropAllButtonTransform.GetComponent<Button>();
        buttonComp.onClick.RemoveAllListeners();
        buttonComp.onClick.AddListener(() => Console.instance.TryRunCommand("azuepi.dropall"));

        dropAllButtonTransform.gameObject.SetActive(MakeDropAllButton.Value.isOn());
    }
}