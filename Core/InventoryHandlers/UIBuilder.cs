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
    public const string ToggleButtonsHlgName = $"{Prefix}ToggleButtonsHlg";
    public const string RuntimePanelName = $"{Prefix}RuntimePanel";
    public const string PlayerPreviewName = $"{Prefix}PlayerPreview";
    public const string PlayerPreviewImageName = $"{Prefix}PlayerPreviewImg";
    public const string CharacterName = $"{Prefix}CharacterName";

    public const string MinimalUiguid = "Azumatt.MinimalUI";

    internal static RectTransform _epiPreviewRect;
    public static GameObject PreviewParent = null!;
    public static GameObject PlayerPreviewImage = null!;
    public static Transform CharName = null!;
    public static GameObject HlgGo = null!;
    public static RectTransform HlgRt = null!;
    
    public const int columns = 2;
    public const int gapTiles = 4;
    public const float padding = 0.6f;
    public const float extraTiles = columns + gapTiles + padding;
    public const float totalWidth = 570f;

    public static void RebuildUI()
    {
        if (!InventoryGui.instance) return;
        Transform? equipmentBkgTransform = InventoryGui.instance.m_player.Find(AzuEquipmentBkgName);
        if (equipmentBkgTransform)
        {
            equipmentBkgTransform.gameObject.SetActive(OldLayout.Value.isOn());
        }

        if (Layout.AzuPlayerBkg)
        {
            Layout.AzuPlayerBkg.gameObject.SetActive(OldLayout.Value.isOff());
        }

        if (PreviewParent)
        {
            PreviewParent.SetActive(OldLayout.Value.isOff());
        }

        if (PlayerPreviewImage)
        {
            PlayerPreviewImage.SetActive(OldLayout.Value.isOff());
        }

        if (CharName)
        {
            CharName.gameObject.SetActive(OldLayout.Value.isOff());
        }
    }

    public static void BuildEquipmentBkg(InventoryGui invGui, RectTransform bkgRect)
    {
        Transform transform = Object.Instantiate(bkgRect.transform, invGui.m_player);
        transform.SetAsFirstSibling();
        transform.name = AzuEquipmentBkgName;
        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0.0f);
        Vector2 maxAnchor = Layout.GetEquipmentBackAnchorMax();
        if (Chainloader.PluginInfos.TryGetValue(MinimalUiguid, out var pluginInfo) && pluginInfo is not null) maxAnchor.x += 0.03f;

        rectTransform.anchorMax = maxAnchor;
        InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
        InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;
        transform.gameObject.SetActive(OldLayout.Value.isOn());
    }

    public static void BuildToggleButtonHlg(InventoryGui invGui)
    {
        HlgGo = new GameObject(ToggleButtonsHlgName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        HlgRt = (RectTransform)HlgGo.transform;
        HlgRt.SetParent(invGui.m_crafting.transform, false);
        HlgRt.anchorMin = new Vector2(0f, 1f);
        HlgRt.anchorMax = new Vector2(0f, 1f);
        HlgRt.pivot = new Vector2(0.5f, 1f);
        HlgRt.anchoredPosition = OldLayout.Value.isOff() ? Layout.ToggleButtonsHlgAnchoredPos : Layout.ToggleButtonsHlgAnchoredPosOld;
        HlgRt.sizeDelta = new Vector2(270f, 32f);
        var hlg = HlgGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 35f;

        VanityPanelController.ToggleButtonParentHlg = HlgRt;
        PersonalLoadoutGui.ToggleButtonParentHlg = HlgRt;
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
        var index = selectedFrame.GetSiblingIndex();
        Layout.AzuPlayerBkg.SetSiblingIndex(index + 2);
        Layout.AzuPlayerBkg.name = AzuPlayerBkgName;
        Layout.AzuPlayerBkg.GetComponent<RectTransform>().anchorMin = Layout.PlayerBkgAnchorMin;
        Layout.AzuPlayerBkg.gameObject.SetActive(OldLayout.Value.isOff());
    }

    public static void CreateRuntimePanel()
    {
        if (AzuEPICharacterPanel.instance == null)
            new GameObject(RuntimePanelName).AddComponent<AzuEPICharacterPanel>();
    }

    public static void CreateAzuEpiPreview(InventoryGui invGui, out RectTransform previewParentRT)
    {
        PreviewParent = new GameObject(PlayerPreviewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(PlayerRotationController));
        previewParentRT = (RectTransform)PreviewParent.transform;
        previewParentRT.SetParent(invGui.m_crafting, false);

        var img = PreviewParent.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.565f);
        previewParentRT.anchorMin = Layout.PreviewAnchorMin;
        previewParentRT.anchorMax = Layout.PreviewAnchorMax;
        previewParentRT.sizeDelta = Layout.PreviewSizeDelta;
        previewParentRT.anchoredPosition = Layout.PreviewAnchoredPos;
        PreviewParent.SetActive(OldLayout.Value.isOff());
    }

    public static void CreatePlayerPreviewImage(RectTransform previewParentRT)
    {
        PlayerPreviewImage = new GameObject(PlayerPreviewImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rt = (RectTransform)PlayerPreviewImage.transform;
        rt.SetParent(previewParentRT, false);

        rt.sizeDelta = Layout.PlayerPreviewImageSize;
        rt.anchoredPosition = Vector2.zero;

        var raw = PlayerPreviewImage.GetComponent<RawImage>();
        raw.raycastTarget = false;
        raw.color = Color.white;

        AzuEPICharacterPanel.instance.render = rt;
        AzuEPICharacterPanel.instance.renderRawImage = raw;

        _epiPreviewRect = rt;
        PlayerPreviewImage.SetActive(OldLayout.Value.isOff());
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
        Transform? dropallButton = invGui.m_player.Find(DropAllButtonName);

        if (dropallButton == null)
        {
            Transform dropAllButtonPrefab = invGui.m_takeAllButton.transform;
            RectTransform dropAllButtonTransform = Object.Instantiate(dropAllButtonPrefab, invGui.m_player).GetComponent<RectTransform>();
            dropAllButtonTransform.name = DropAllButtonName;
            dropAllButtonTransform.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$azuepi_dropall");
            var buttonComp = dropAllButtonTransform.GetComponent<Button>();
            buttonComp.onClick.RemoveAllListeners();
            buttonComp.onClick.AddListener(() => Console.instance.TryRunCommand("azuepi.dropall"));

            dropAllButtonTransform.SetAsFirstSibling();
            dropAllButtonTransform.anchorMin = Layout.DropAllAnchorMin;
            dropAllButtonTransform.anchorMax = Layout.DropAllAnchorMax;
            dropAllButtonTransform.pivot = Layout.DropAllPivot;
            dropAllButtonTransform.anchoredPosition = DropAllButtonPosition.Value;
            dropAllButtonTransform.sizeDelta = Layout.DropAllSize;

            dropAllButtonTransform.gameObject.SetActive(MakeDropAllButton.Value.isOn());
        }
    }
}