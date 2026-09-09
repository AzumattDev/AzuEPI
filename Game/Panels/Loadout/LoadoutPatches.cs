using AzuEPI.Game.Compatibility.AdvBackpacks;
using AzuEPI.Game.Panels;

namespace AzuEPI.Game.Loadout;

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
static class PlayerSpawnedPatch
{
    static void Postfix(Player __instance)
    {
        try
        {
            GameObject rootPanel = StoreGui.instance.gameObject;

            if (rootPanel == null)
            {
                AzuExtendedPlayerInventoryLogger.LogError("StoreGui.instance is null");
                return;
            }

            InventoryGui? invGui = InventoryGui.instance;
            Transform backgroundParent = CreateBackground(invGui);

            CreateMainPanel(rootPanel, __instance, backgroundParent, out GameObject? newRootPanel, out PersonalLoadoutGui? itemsetGui);
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError("Exception: " + ex);
        }
    }

    private static void CreateMainPanel(GameObject rootPanel, Player player, Transform backgroundParent, out GameObject clonedRootPanel, out PersonalLoadoutGui gui)
    {
        GameObject newRootPanel = GameObject.Instantiate(rootPanel, backgroundParent, false);
        newRootPanel.name = $"{Prefix}LoadoutsRootPanel";
        newRootPanel.GetComponent<Canvas>().sortingOrder = 699;
        //Utils.FindChild(newRootPanel.transform, "border (1)").gameObject.GetComponent<Image>().sprite = player.m_inventory.GetBkg();
        clonedRootPanel = newRootPanel;

        RectTransform rt = newRootPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, 0);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = new Vector2(-135f, 0f);

        PersonalLoadoutGui itemsetGui = newRootPanel.AddComponent<PersonalLoadoutGui>();
        if (itemsetGui == null)
        {
            gui = null;
            return;
        }

        gui = itemsetGui;

        PersonalLoadoutGui.m_rootPanel = newRootPanel.gameObject;
        PersonalLoadoutGui.m_rootPanel.GetOrAddComponent<Localize>();
        PersonalLoadoutGui.m_storeRootPanel = Utils.FindChild(newRootPanel.transform, "Store").gameObject;
        PersonalLoadoutGui.m_storeRootPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(166f, -50f);

        Utils.FindChild(newRootPanel.transform, "border (1)").gameObject.SetActive(false);
        itemsetGui.m_chooseButton = Utils.FindChild(newRootPanel.transform, "BuyButton").GetComponent<Button>();
        itemsetGui.m_chooseButton.transform.Find("Text").GetComponent<TMP_Text>().text = Localization.instance.Localize("$azu_epi_equipLoadout");
        PanelUtilities.BindGamePad(itemsetGui.m_chooseButton.transform, "JoyButtonA", KeyCode.JoystickButton0);

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
        newRootPanel.transform.Find("Store/coins").SafeSetActive(false);

        itemsetGui.m_topicText = Utils.FindChild(newRootPanel.transform, "topic").GetComponent<TMP_Text>();

        itemsetGui.m_buyEffects = newRootPanel.GetComponent<StoreGui>().m_buyEffects;
        itemsetGui.m_sellEffects = newRootPanel.GetComponent<StoreGui>().m_sellEffects;
        itemsetGui.m_hideDistance = newRootPanel.GetComponent<StoreGui>().m_hideDistance;
        PersonalLoadoutGui.m_itemSpacing = newRootPanel.GetComponent<StoreGui>().m_itemSpacing;
        PersonalLoadoutGui.m_coinPrefab = newRootPanel.GetComponent<StoreGui>().m_coinPrefab;
        PersonalLoadoutGui.m_itemlistBaseSize = newRootPanel.GetComponent<StoreGui>().m_itemlistBaseSize;

        GameObject.DestroyImmediate(newRootPanel.GetComponent<StoreGui>());

        CreateLoadoutInventoryGrid(newRootPanel, player);
    }

    private static void CreateLoadoutInventoryGrid(GameObject panel, Player player)
    {
        GameObject gridRoot = new($"{Prefix}LoadoutGridContainer");
        gridRoot.transform.SetParent(panel.transform, false);

        RectTransform gridRT = gridRoot.AddComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(1, 0.5f);
        gridRT.anchorMax = new Vector2(1, 0.5f);
        gridRT.pivot = new Vector2(0, 0.5f);
        gridRT.anchoredPosition = new Vector2(-220f, 0f);
        gridRT.sizeDelta = new Vector2(400f, 400f);

        PersonalLoadoutGui.m_loadoutInventory = new Inventory("Loadout View", null, 4, 8);

        InventoryGrid templateGrid = InventoryGui.instance.m_playerGrid;

        PersonalLoadoutGui.m_loadoutGrid = gridRoot.AddComponent<InventoryGrid>();
        PersonalLoadoutGui.m_loadoutGrid.m_elementPrefab = templateGrid.m_elementPrefab;
        PersonalLoadoutGui.m_loadoutGrid.m_gridRoot = gridRoot.GetComponent<RectTransform>();
        PersonalLoadoutGui.m_loadoutGrid.m_elementSpace = 70f;
        PersonalLoadoutGui.m_loadoutGrid.m_width = 0;
        PersonalLoadoutGui.m_loadoutGrid.m_height = 0;
		PersonalLoadoutGui.m_loadoutGrid.CanDropDragOntoItem = _ => false;

        UIGroupHandler uiGroup = gridRoot.AddComponent<UIGroupHandler>();
        uiGroup.m_active = false;
        PersonalLoadoutGui.m_loadoutGrid.m_uiGroup = uiGroup;

        PersonalLoadoutGui.m_loadoutGrid.m_onSelected = OnLoadoutGridItemSelected;
        PersonalLoadoutGui.m_loadoutGrid.m_onRightClick = OnLoadoutGridItemRightClick;

        PersonalLoadoutGui.m_loadoutGridRoot = gridRoot;
    }

    private static void OnLoadoutGridItemSelected(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
    {
        Player player = Player.m_localPlayer;
        if (player == null) return;

        InventoryGui inventoryGui = InventoryGui.instance;
        if (inventoryGui == null) return;

        if (inventoryGui.m_dragGo != null && inventoryGui.m_dragItem != null)
        {
            ItemDrop.ItemData dragItem = inventoryGui.m_dragItem;
            Inventory dragInventory = inventoryGui.m_dragInventory;
            int dragAmount = inventoryGui.m_dragAmount;

            if (dragInventory == null) return;
            if (!dragItem.m_shared.m_teleportable) {player.Message(MessageHud.MessageType.Center, Localization.instance?.Localize("$msg_blocked $item_noteleport"));
                return;
            }

            if (dragItem.m_dropPrefab && (AdvBackpacksCompat.Backpacks.Contains(dragItem.m_dropPrefab.name) || RustyBagsCompat.Backpacks.Contains(dragItem.m_dropPrefab.name) || dragItem.m_dropPrefab.name == "bp_explorer" || dragItem.m_dropPrefab.name == "JC_Gem_Bag"))
            {
                player.Message(MessageHud.MessageType.Center, Localization.instance?.Localize("$msg_blocked $piece_armorstand_cantattach"));
                return;
            }
            if (!PersonalLoadoutGui.m_loadoutInventory.AddItem(dragItem, dragAmount, pos.x, pos.y)) return;
            if (dragItem.m_stack <= 0)
            {
                player.UnequipItem(dragItem);
                dragInventory.RemoveItem(dragItem);
            }

            UpdateLoadoutFromInventory();
            PersonalLoadoutGui.RefreshLoadoutInventory();
            inventoryGui.SetupDragItem(null, null, 0);
            return;
        }

        if (mod != InventoryGrid.Modifier.Move) return;
        if (item == null) return;
        if (!player.GetInventory().AddItem(item.Clone())) return;
        PersonalLoadoutGui.m_loadoutInventory.RemoveItem(item);
        UpdateLoadoutFromInventory();
        PersonalLoadoutGui.RefreshLoadoutInventory();
    }

    private static void OnLoadoutGridItemRightClick(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos)
    {
        if (item == null) return;
        Player player = Player.m_localPlayer;
        if (player == null || !player.GetInventory().AddItem(item.Clone())) return;
        PersonalLoadoutGui.m_loadoutInventory.RemoveItem(item);
        UpdateLoadoutFromInventory();
        PersonalLoadoutGui.RefreshLoadoutInventory();
    }

    private static void UpdateLoadoutFromInventory()
    {
        if (string.IsNullOrWhiteSpace(PersonalLoadoutGui.m_selectedItem))
            return;

        Player player = Player.m_localPlayer;
        if (player == null) return;

        string key = $"{PersonalLoadoutGui.LoadoutKey}{PersonalLoadoutGui.m_selectedItem}";

        List<ItemDrop.ItemData> items = PersonalLoadoutGui.m_loadoutInventory.GetAllItems();
        PersonalLoadout loadout = new(PersonalLoadoutGui.m_selectedItem, items);
        player.m_customData[key] = loadout.Serialize();

        PersonalLoadoutGui.FillList();
    }

    private static Transform CreateBackground(InventoryGui gui)
    {
        RectTransform? crafting = gui.m_crafting;
        Transform? srcBkg = crafting.Find("Bkg");
        if (!srcBkg) return null;

        Transform? loadoutPanel = Object.Instantiate(srcBkg, crafting);
        loadoutPanel.name = $"{Prefix}LoadoutPanel";
        RectTransform rt = (RectTransform)loadoutPanel.transform;
        rt.anchorMin = srcBkg.GetComponent<RectTransform>().anchorMin;
        rt.anchorMax = srcBkg.GetComponent<RectTransform>().anchorMax;

        rt.SetAsLastSibling();

        Image? img = loadoutPanel.GetComponent<Image>();
        if (img)
        {
            img.raycastTarget = false;
            img.enabled = true;
        }

        loadoutPanel.gameObject.SetActive(false);
        return loadoutPanel;
    }
}
