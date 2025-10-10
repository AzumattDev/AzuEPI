using AzuExtendedPlayerInventory;

namespace AzuEPI.Loadout;

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
static class PlayerSpawnedPatch
{
    static void Postfix(Player __instance)
    {
        try
        {
            GameObject rootPanel;
            GameObject ingameGui;
            Transform augaStoreScreen;

            rootPanel = StoreGui.instance.gameObject;

            if (rootPanel == null)
            {
                AzuExtendedPlayerInventoryLogger.LogError("StoreGui.instance is null");
                return;
            }

            var invGui = InventoryGui.instance;
            var backgroundParent = CreateBackground(invGui);

            CreateMainPanel(rootPanel, __instance, backgroundParent, out GameObject? newRootPanel, out PersonalLoadoutGui? itemsetGui);

            if (newRootPanel == null || itemsetGui == null)
            {
                AzuExtendedPlayerInventoryLogger.LogError("Failed to create main panel, second panel not created");
                return;
            }

            var craftBtn = invGui?.m_craftButton;
            var parent = newRootPanel.transform;

            /*var dd = AzuRuntimeDropdown.Create(parent, craftBtn, width: 240f, headerHeight: 38f, maxListHeight: 260f);
            dd.SetOptions(new[] { "Option A", "Option B", "Option C", "Very Long Option That Scrolls" });
            dd.OnValueChanged.AddListener((idx, text) =>
            {
                // Do your thing
                Debug.Log($"Selected {idx}: {text}");
            });
            
            // Position it
            var hdr = dd.GetHeaderButton();
            var hdrRT = (RectTransform)hdr.transform;
            hdrRT.anchoredPosition = new Vector2(20, -20);*/

            //CreateLoadoutContainer(newRootPanel);
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError("Exception: " + ex);
        }
    }

    private static void CreateMainPanel(GameObject rootPanel, Player player, Transform backgroundParent, out GameObject clonedRootPanel, out PersonalLoadoutGui gui)
    {
        GameObject newRootPanel = GameObject.Instantiate(rootPanel, backgroundParent, false);
        newRootPanel.name = "AzuEPILoadoutsRootPanel";
        newRootPanel.GetComponent<Canvas>().sortingOrder = 699;
        //Utils.FindChild(newRootPanel.transform, "border (1)").gameObject.GetComponent<Image>().sprite = player.m_inventory.GetBkg();
        clonedRootPanel = newRootPanel;

        RectTransform rt = newRootPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, 0);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        PersonalLoadoutGui itemsetGui = newRootPanel.AddComponent<PersonalLoadoutGui>();
        if (itemsetGui == null)
        {
            gui = null;
            return;
        }

        gui = itemsetGui;

        PersonalLoadoutGui.m_rootPanel = newRootPanel.gameObject;
        PersonalLoadoutGui.m_storeRootPanel = Utils.FindChild(newRootPanel.transform, "Store").gameObject;
        PersonalLoadoutGui.m_storeRootPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(166f, -50f);
        Utils.FindChild(newRootPanel.transform, "border (1)").gameObject.SetActive(false);
        itemsetGui.m_chooseButton = Utils.FindChild(newRootPanel.transform, "BuyButton").GetComponent<Button>();
        itemsetGui.m_chooseButton.transform.Find("Text").GetComponent<TMP_Text>().text = Localization.instance.Localize("$azu_epi_equipLoadout");

        itemsetGui.m_sellButton = Utils.FindChild(newRootPanel.transform, "SellButton").GetComponent<Button>();
        itemsetGui.m_sellButton.GetComponent<UITooltip>().m_text = Localization.instance.Localize("$azu_epi_equipSelected");

        itemsetGui.m_sellButton.transform.Find("Image").gameObject.SetActive(false);
        itemsetGui.m_sellButton.transform.Find("Image (1)").gameObject.SetActive(true);
        itemsetGui.m_sellButton.transform.parent.gameObject.SetActive(false);

        PersonalLoadoutGui.m_listRoot = Utils.FindChild(newRootPanel.transform, "ListRoot").GetComponent<RectTransform>();
        PersonalLoadoutGui.m_listElement = Utils.FindChild(newRootPanel.transform, "ItemElement").gameObject;

        itemsetGui.m_listScroll = Utils.FindChild(newRootPanel.transform, "ItemScroll").GetComponent<Scrollbar>();
        PersonalLoadoutGui.m_itemEnsureVisible = Utils.FindChild(newRootPanel.transform, "Items").GetComponent<ScrollRectEnsureVisible>();

        itemsetGui.m_coinText = newRootPanel.transform.Find("Store/coins/coins").GetComponent<TMP_Text>();
        itemsetGui.m_coinIcon = newRootPanel.transform.Find("Store/coins/coin icon").GetComponent<Image>();

        newRootPanel.transform.Find("Store/coins/coin icon").GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 5);
        newRootPanel.transform.Find("Store/coins").GetComponent<RectTransform>().anchoredPosition += new Vector2(35, 0);

        itemsetGui.m_topicText = Utils.FindChild(newRootPanel.transform, "topic").GetComponent<TMP_Text>();

        itemsetGui.m_buyEffects = newRootPanel.GetComponent<StoreGui>().m_buyEffects;
        itemsetGui.m_sellEffects = newRootPanel.GetComponent<StoreGui>().m_sellEffects;
        itemsetGui.m_hideDistance = newRootPanel.GetComponent<StoreGui>().m_hideDistance;
        PersonalLoadoutGui.m_itemSpacing = newRootPanel.GetComponent<StoreGui>().m_itemSpacing;
        PersonalLoadoutGui.m_coinPrefab = newRootPanel.GetComponent<StoreGui>().m_coinPrefab;
        PersonalLoadoutGui.m_itemlistBaseSize = newRootPanel.GetComponent<StoreGui>().m_itemlistBaseSize;

        GameObject.DestroyImmediate(newRootPanel.GetComponent<StoreGui>());
    }

    private static Transform CreateBackground(InventoryGui gui)
    {
        var crafting = gui.m_crafting;
        var srcBkg = crafting.Find("Bkg");
        if (!srcBkg) return null;

        var loadoutPanel = Object.Instantiate(srcBkg, crafting);
        loadoutPanel.name = "AzuEPILoadoutPanel";
        var rt = (RectTransform)loadoutPanel.transform;
        rt.anchorMin = srcBkg.GetComponent<RectTransform>().anchorMin;
        rt.anchorMax = srcBkg.GetComponent<RectTransform>().anchorMax;

        rt.SetAsLastSibling();

        var img = loadoutPanel.GetComponent<Image>();
        if (img)
        {
            img.raycastTarget = false;
            img.enabled = true;
        }
        loadoutPanel.gameObject.SetActive(false);
        return loadoutPanel;
    }

    private static void CreateLoadoutContainer(GameObject newRootPanel)
    {
        GameObject secondPanel = Object.Instantiate(newRootPanel, newRootPanel.transform);
        secondPanel.name = "AzuRapidLoadoutsSecondPanel";
        GameObject.DestroyImmediate(secondPanel.GetComponent<CanvasScaler>());
        GameObject.DestroyImmediate(secondPanel.GetComponent<GraphicRaycaster>());
        GameObject.DestroyImmediate(secondPanel.GetComponent<Canvas>());
        GameObject.DestroyImmediate(secondPanel.GetComponent<GuiScaler>());
        GameObject.DestroyImmediate(secondPanel.GetComponent<UIDragger>());
        PersonalLoadoutGuiDetails personalLoadoutGui = newRootPanel.AddComponent<PersonalLoadoutGuiDetails>();
        if (personalLoadoutGui == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("PurchasableLoadoutGui component not found on newRootPanel");
            return;
        }

        secondPanel.transform.localScale = Vector3.one;

        PersonalLoadoutGui.m_storeRootPanel = Utils.FindChild(secondPanel.transform, "Store").gameObject;
        personalLoadoutGui.m_chooseButton = Utils.FindChild(secondPanel.transform, "BuyButton").GetComponent<Button>();
        personalLoadoutGui.m_chooseButton.onClick.RemoveAllListeners();
        personalLoadoutGui.m_chooseButton.transform.Find("Text").GetComponent<TMP_Text>().text = Localization.instance.Localize("$azu_rl_emptyItemSet");

        personalLoadoutGui.m_sellButton = Utils.FindChild(newRootPanel.transform, "SellButton").GetComponent<Button>();
        personalLoadoutGui.m_sellButton.GetComponent<UITooltip>().m_text = Localization.instance.Localize("$azu_rl_equipSelected");

        personalLoadoutGui.m_sellButton.transform.Find("Image").gameObject.SetActive(false);

        personalLoadoutGui.m_sellButton.transform.Find("Image (1)").gameObject.SetActive(true);
        personalLoadoutGui.m_sellButton.transform.parent.gameObject.SetActive(false);

        RectTransform secondPanelRT = secondPanel.GetComponent<RectTransform>();
        secondPanelRT.localPosition = new Vector3(250, 0, 0);

        Object.Destroy(Utils.FindChild(secondPanel.transform, "SellPanel").gameObject);
        Object.Destroy(Utils.FindChild(secondPanel.transform, "border (1)").gameObject);
        Object.Destroy(Utils.FindChild(secondPanel.transform, "bkg").gameObject);

        PersonalLoadoutGui.m_listRoot = Utils.FindChild(secondPanel.transform, "ListRoot").GetComponent<RectTransform>();
        PersonalLoadoutGui.m_listElement = Utils.FindChild(secondPanel.transform, "ItemElement").gameObject;

        personalLoadoutGui.m_listScroll = Utils.FindChild(secondPanel.transform, "ItemScroll").GetComponent<Scrollbar>();
        PersonalLoadoutGui.m_itemEnsureVisible = Utils.FindChild(secondPanel.transform, "Items").GetComponent<ScrollRectEnsureVisible>();

        personalLoadoutGui.m_coinText = secondPanel.transform.Find($"Store/coins/coins").GetComponent<TMP_Text>();
        personalLoadoutGui.m_coinIcon = secondPanel.transform.Find($"Store/coins/coin icon").GetComponent<Image>();

        secondPanel.transform.Find("Store/coins/coin icon").GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 5);
        secondPanel.transform.Find("Store/coins").GetComponent<RectTransform>().anchoredPosition += new Vector2(35, 0);

        personalLoadoutGui.m_topicText = Utils.FindChild(secondPanel.transform, "topic").GetComponent<TMP_Text>();

        /*personalLoadoutGui.m_buyEffects = itemsetGui.m_buyEffects;
        personalLoadoutGui.m_sellEffects = itemsetGui.m_sellEffects;
        personalLoadoutGui.m_hideDistance = itemsetGui.m_hideDistance;
        PersonalLoadoutGui.m_itemSpacing = PurchasableLoadoutGui.m_itemSpacing;
        PersonalLoadoutGui.m_coinPrefab = PurchasableLoadoutGui.m_coinPrefab;
        PersonalLoadoutGui.m_itemlistBaseSize = PurchasableLoadoutGui.m_itemlistBaseSize;*/

        Utils.FindChild(newRootPanel.transform, "border (1)").GetComponent<RectTransform>().anchorMax = new Vector2(2, 1);
    }
}