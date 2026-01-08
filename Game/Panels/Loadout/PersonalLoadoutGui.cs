using AzuEPI.Game.Panels;

namespace AzuEPI.Game.Loadout;

internal static class EpiSwapContext
{
    public static bool Active { get; private set; }

    private sealed class Scope : IDisposable
    {
        public void Dispose() => Active = false;
    }

    public static IDisposable Begin()
    {
        Active = true;
        return new Scope();
    }
}

public class PersonalLoadoutGui : MonoBehaviour, TextReceiver
{
    public static PersonalLoadoutGui? m_instance;
    public static GameObject m_rootPanel = null!;
    public static GameObject m_storeRootPanel = null!;
    public Button m_chooseButton = null!;

    public Button m_sellButton = null!;

    public static InventoryGrid m_loadoutGrid = null!;
    public static Inventory m_loadoutInventory = null!;
    public static GameObject m_loadoutGridRoot = null!;

    public static RectTransform m_listRoot = null!;
    public static GameObject m_listElement = null!;
    public Scrollbar m_listScroll = null!;
    public static ScrollRectEnsureVisible m_itemEnsureVisible = null!;
    public TMP_Text m_coinText = null!;
    public Image m_coinIcon = null!;
    public TMP_Text m_topicText = null!;
    public EffectList m_buyEffects = new();
    public EffectList m_sellEffects = new();
    public float m_hideDistance = 5f;
    public static float m_itemSpacing = 64f;
    public static ItemDrop m_coinPrefab = null!;
    public static List<GameObject> m_itemList = [];
    public static string? m_selectedItem;
    public static Player? m_LocalPlayerRef;
    public static float m_itemlistBaseSize;
    public List<ItemDrop.ItemData> m_tempItems = [];
    static List<Humanoid.ItemSet?> availableSets = [];

    public static bool PanelActive;
    public static PersonalLoadoutGui? instance => m_instance;
    public const string LoadoutKey = "AzuEPILoadout_";
    public const string LoadoutNameKey = "AzuEPILoadoutName_";
    public static Transform LoadoutsToggleButton = null!;
    private static Button _toggleBtn;
    internal static int tempInventorySize = 0;
    internal static RectTransform ToggleButtonParentGlg = null!;
    private static string? m_loadoutBeingRenamed;

    public void Awake()
    {
        m_instance = this;
        if (m_storeRootPanel != null)
            m_storeRootPanel.SetActive(false);
        if (m_listRoot != null)
            m_itemlistBaseSize = m_listRoot.rect.height;
        m_chooseButton.onClick = new Button.ButtonClickedEvent();
        m_chooseButton.onClick.AddListener(OnChooseLoadout);
        m_topicText.text = Localization.instance.Localize("$azu_epi_loadouts");
    }

    public void OnDestroy()
    {
        if (!(m_instance == this))
            return;
        m_instance = null;
    }

    public void HandleUIUpdates()
    {
        if (ShouldHide() || ShouldClose())
        {
            Hide();
        }
        else
        {
            UpdateUI();
        }
    }

    private bool ShouldHide()
    {
        Player localPlayer = Player.m_localPlayer;

        if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene())
            return true;

        return !InventoryGui.IsVisible() || Minimap.IsOpen();
    }

    private static bool ShouldClose()
    {
        Player localPlayer = Player.m_localPlayer;
        bool isUIBlocking = (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible() || Menu.IsVisible() || (TextViewer.instance != null && TextViewer.instance.IsVisible()) || localPlayer.InCutscene();

        if (!isUIBlocking || (!ZInput.GetButtonDown("JoyButtonB") && !Input.GetKeyDown(KeyCode.Escape) && !ZInput.GetButtonDown("Use"))) return false;
        ZInput.ResetButtonStatus("JoyButtonB");
        return true;
    }

    private void UpdateUI()
    {
        UpdateSwitchLoadoutButton();
        UpdateRecipeGamepadInput();
        m_coinText.text = GetNumberOfLoadouts().ToString();
    }

    public static void Show()
    {
        if (Player.m_localPlayer == null)
            return;
        if (IsVisible())
            return;
        m_LocalPlayerRef = Player.m_localPlayer;
        InventoryGui.instance.m_dropButton.gameObject.SetActive(false);

        m_rootPanel.transform.parent.gameObject.SetActive(true);
        m_rootPanel.SetActive(true);
        m_storeRootPanel.SetActive(true);
        if (m_loadoutGridRoot != null)
            m_loadoutGridRoot.SetActive(true);
        PanelActive = true;
        FillList();
        RefreshLoadoutInventory();
    }

    public static void Hide()
    {
        m_LocalPlayerRef = null;
        m_rootPanel.transform.parent.gameObject.SetActive(false);
        m_rootPanel.SetActive(false);
        m_storeRootPanel.SetActive(false);
        if (m_loadoutGridRoot != null)
            m_loadoutGridRoot.SetActive(false);
        PanelActive = false;
        InventoryGui.instance.m_dropButton.gameObject.SetActive(true);
    }

    public static void ToggleUI()
    {
        if (IsVisible())
        {
            Hide();
        }
        else
        {
            Show();
            VanityPanelController.SetVisible(false);
            StatsPanelController.Hide();
        }
    }

    public static bool IsVisible() => (m_instance && m_storeRootPanel.activeSelf) || PanelActive;

    public void OnChooseLoadout()
    {
        LoadSelectedLoadout();
    }

    public void LoadSelectedLoadout()
    {
        if (string.IsNullOrWhiteSpace(m_selectedItem))
            return;

        LoadLoadout(m_selectedItem!);

        m_buyEffects.Create(transform.position, Quaternion.identity);

        FillList();

        RefreshLoadoutInventory();
    }

    internal static void BuildLoadoutToggleButton(InventoryGui gui)
    {
        PanelUtilities.ButtonConfig config = new(
            name: $"{Prefix}LoadoutsToggleButton",
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            pivot: new Vector2(0f, 1f),
            anchoredPosition: new Vector2(-205f, -30f),
            size: new Vector2(120f, 32f),
            gamepadKey: PanelUtilities.KeyCodeToZInputKey(LoadoutToggleGamepadKey.Value),
            gamepadKeyCode: LoadoutToggleGamepadKey.Value,
            label: "🎯",
            labelFontSize: 20f,
            onClick: () =>
            {
                ToggleUI();
                if (InventoryGui.instance)
                {
                    bool vis = IsVisible();
                    PanelUtilities.HideCraftingElements(vis);
                }
            }
        );

        (LoadoutsToggleButton, _toggleBtn) = PanelUtilities.BuildToggleButton(gui, ToggleButtonParentGlg, config);
        LoadoutsToggleButton.gameObject.SetActive(LoadoutOption.Value.isOn());
    }

    public static int GetNumberOfLoadouts()
    {
        Player player = Player.m_localPlayer;
        if (player == null) return 0;
        return player.m_customData.Keys.Count(key => key.StartsWith(LoadoutKey));
    }

    public static List<string> GetAvailableLoadouts()
    {
        Player player = Player.m_localPlayer;
        if (player == null) return [];

        List<string> loadouts = [];
        foreach (string? key in player.m_customData.Keys)
        {
            if (key.StartsWith(LoadoutKey))
            {
                loadouts.Add(key.Replace(LoadoutKey, ""));
            }
        }

        return loadouts;
    }

    public static bool SaveLoadout(string loadoutName)
    {
        Player player = Player.m_localPlayer;
        if (player == null) return false;

        string key = $"{LoadoutKey}{loadoutName}";
        List<ItemDrop.ItemData> equippedItems = player.GetInventory().GetEquippedItems();
        AzuExtendedPlayerInventoryLogger.LogInfo($"SaveLoadout: Saving loadout '{loadoutName}' with {equippedItems.Count} items.");
        foreach (ItemDrop.ItemData? item in equippedItems)
        {
            AzuExtendedPlayerInventoryLogger.LogInfo($" - {item.m_shared.m_name} x{item.m_stack}");
        }

        PersonalLoadout loadout = new(loadoutName, equippedItems);
        player.m_customData[key] = loadout.Serialize();
        AzuExtendedPlayerInventoryLogger.LogInfo($"SaveLoadout: Key '{key}' created successfully.");
        return true;
    }

    private static void SaveEquippedItems(Player player, Inventory inventory)
    {
        List<ItemDrop.ItemData>? equippedItems = player.GetInventory().GetEquippedItems();
        if (equippedItems is { Count: > 0 })
        {
            for (int index = 0; index < equippedItems.Count; ++index)
            {
                ItemDrop.ItemData? item = equippedItems[index];
                inventory.AddItem(item.Clone());
            }
        }
    }

    public static void LoadLoadout(string loadoutName)
    {
        Player player = Player.m_localPlayer;
        if (player == null) return;

        string key = $"{LoadoutKey}{loadoutName}";
        if (!player.m_customData.TryGetValue(key, out string serializedData))
            return;

        string oldSerialized = serializedData;

        PersonalLoadout loadout = PersonalLoadout.Deserialize(loadoutName, serializedData);
        using (EpiSwapContext.Begin())
        {
            SaveLoadout(loadoutName);

            UnequipAndRemoveCurrentItems(player);
            if (!EquipItemsFromLoadout(player, loadout))
            {
                PersonalLoadout oldLoadout = PersonalLoadout.Deserialize(loadoutName, oldSerialized);
                EquipItemsFromLoadout(player, oldLoadout);
                player.m_customData[key] = oldSerialized;
            }

            player.m_visEquipment.UpdateVisuals();
        }
    }

    private static bool EquipItemsFromLoadout(Player player, PersonalLoadout loadout)
    {
        List<ItemDrop.ItemData> items = loadout.Items;
        int freeSlots = player.GetInventory().GetEmptySlots();
        bool canDo = freeSlots >= items.Count;
        if (canDo)
        {
            for (int index = 0; index < items.Count; ++index)
            {
                ItemDrop.ItemData item = items[index];
                if (item == null)
                {
                    continue;
                }

                if (item.m_equipped) item.m_equipped = false;

                string targetName = item.m_shared.m_name;
                long targetCrafterID = item.m_crafterID;
                string targetCrafterName = item.m_crafterName;
                float targetDurability = item.m_durability;
                int targetQuality = item.m_quality;

                AzuExtendedPlayerInventoryLogger.LogDebug("Attempting to add item: " + targetName);
                bool moved = player.GetInventory().AddItem(item);
                AzuExtendedPlayerInventoryLogger.LogDebug("Move result: " + moved);

                if (moved)
                {
                    AzuExtendedPlayerInventoryLogger.LogDebug("Equipping item: " + targetName);

                    ItemDrop.ItemData? actualItem = null;

                    if (player.GetInventory().ContainsItem(item))
                    {
                        actualItem = item;
                        AzuExtendedPlayerInventoryLogger.LogDebug("Using original item reference");
                    }
                    else
                    {
                        actualItem = player.GetInventory().GetAllItems().FirstOrDefault(i => i.m_shared.m_name == targetName && i.m_crafterID == targetCrafterID && i.m_crafterName == targetCrafterName && i.m_quality == targetQuality && Math.Abs(i.m_durability - targetDurability) < 0.01f && !i.m_equipped);
                    }

                    actualItem ??= player.GetInventory().GetAllItems().FirstOrDefault(i => i.m_shared.m_name == targetName && i.m_quality == targetQuality && !i.m_equipped);

                    if (actualItem != null)
                    {
                        if (AutoEquip.Value.isOff()) continue;
                        bool equipped = player.EquipItem(actualItem, false);
                        if (equipped)
                        {
                            AzuExtendedPlayerInventoryLogger.LogDebug($"Successfully equipped {actualItem.m_shared.m_name}");
                        }
                    }
                    else
                    {
                        AzuExtendedPlayerInventoryLogger.LogError($"Could not find {targetName} in inventory after adding!");
                    }
                }
                else
                {
                    AzuExtendedPlayerInventoryLogger.LogError($"Failed to add item {item.m_shared.m_name} to inventory during loadout restore!");
                }
            }

            return true;
        }

        player.Message(MessageHud.MessageType.Center, "Not enough room in inventory.");
        return false;
    }

    private static void UnequipAndRemoveCurrentItems(Player player)
    {
        List<ItemDrop.ItemData>? list = player.GetInventory().GetEquippedItems();
        for (int index = 0; index < list.Count; ++index)
        {
            ItemDrop.ItemData? item = list[index];
            player.UnequipItem(item);
            item.m_equipped = false;
            player.GetInventory().RemoveItem(item);
        }
    }

    private static void UnequipToBags(Player player)
    {
        List<ItemDrop.ItemData>? equipped = player.GetInventory().GetEquippedItems();
        foreach (ItemDrop.ItemData? it in equipped)
        {
            player.UnequipItem(it);
            if (!player.GetInventory().AddItem(it))
            {
                player.EquipItem(it);
                AzuExtendedPlayerInventoryLogger.LogWarning($"No space to unequip {it.m_shared.m_name}");
            }
        }
    }

    public static void FillList()
    {
        List<string> availableLoadouts = GetAvailableLoadouts();
        int index1 = GetSelectedItemIndex();

        foreach (GameObject obj in m_itemList)
            Destroy(obj);
        m_itemList.Clear();

        m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(m_itemlistBaseSize, availableLoadouts.Count * m_itemSpacing));

        for (int index2 = 0; index2 < availableLoadouts.Count; ++index2)
        {
            string loadoutName = availableLoadouts[index2];
            GameObject element = Instantiate(m_listElement, m_listRoot);
            element.SetActive(true);
            ((element.transform as RectTransform)!).anchoredPosition = new Vector2(0.0f, index2 * -m_itemSpacing);

            Image elementIcon = element.transform.Find("icon").GetComponent<Image>();

            if (Player.m_localPlayer.m_customData.TryGetValue($"{LoadoutKey}{loadoutName}", out string serializedData))
            {
                PersonalLoadout loadout = PersonalLoadout.Deserialize(loadoutName, serializedData);
                if (loadout.Items.Count > 0)
                {
                    ItemDrop.ItemData item = loadout.Items[0];
                    elementIcon.sprite = item.m_shared.m_icons[0];
                }
                else
                {
                    elementIcon.sprite = null;
                }

                bool flag = loadout.Items.Count > 0;
                elementIcon.color = flag ? Color.white : new Color(1f, 0.0f, 1f, 0.0f);

                TMP_Text elementName = element.transform.Find("name").GetComponent<TMP_Text>();
                string customName = GetLoadoutCustomName(loadoutName);
                elementName.text = Localization.instance.Localize(customName);
                elementName.color = flag ? Color.white : Color.grey;
                UITooltip elementTooltip = element.GetComponent<UITooltip>();
                elementTooltip.m_topic = customName;
                elementTooltip.m_text = flag ? $"$azu_epi_loadouts_load:\n{string.Join("\n", loadout.Items.Select(i => Localization.instance.Localize(i.m_shared.m_name)))}" : "$azu_epi_loadouts_empty";
                element.GetComponent<Button>().onClick.AddListener(() => OnSelectedLoadout(element));

                Button elementButton = element.GetComponent<Button>();
                if (elementButton != null)
                {
                    Transform renameButtonTransform = PanelUtilities.CloneButton(
                        src: instance.m_chooseButton.transform,
                        parent: element.transform,
                        name: "RenameButton",
                        anchorMin: new Vector2(1f, 0.5f),
                        anchorMax: new Vector2(1f, 0.5f),
                        pivot: new Vector2(1f, 0.5f),
                        anchoredPos: new Vector2(-10f, 0f),
                        size: new Vector2(30f, 30f)
                    );

                    Button renameBtn = renameButtonTransform.GetComponent<Button>();
                    renameBtn.onClick.RemoveAllListeners();

                    TMP_Text renameText = renameButtonTransform.GetComponentInChildren<TMP_Text>();
                    if (renameText != null)
                    {
                        renameText.text = "R";
                        renameText.fontSize = 18;
                        renameText.fontStyle = FontStyles.Bold;
                        renameText.alignment = TextAlignmentOptions.Center;
                        renameText.color = Color.white;
                    }

                    PanelUtilities.BindGamePad(renameButtonTransform, "", KeyCode.None);

                    UITooltip renameTooltip = renameButtonTransform.GetComponent<UITooltip>();
                    if (renameTooltip != null)
                    {
                        renameTooltip.m_text = "$azu_epi_loadout_rename_tooltip";
                        renameTooltip.m_topic = "";
                    }

                    string currentLoadoutId = loadoutName;
                    renameBtn.onClick.AddListener(() => RequestRenameLoadout(currentLoadoutId));
                }

                TMP_Text component4 = Utils.FindChild(element.transform, "price").GetComponent<TMP_Text>();
                Utils.FindChild(element.transform, "coin icon").GetComponent<Image>().enabled = false;
                string plural = loadout.Items.Count > 1 ? "items" : "item";
                component4.text = loadout.Items.Count >= 1 ? $"{loadout.Items.Count} {plural}" : "Empty";
                if (!flag)
                    component4.color = Color.grey;
                m_itemList.Add(element);
            }
        }

        if (index1 < 0)
            index1 = 0;
        SelectItem(index1, false);
    }

    public static void OnSelectedLoadout(GameObject button)
    {
        SelectItem(FindSelectedLoadout(button), false);
        RefreshLoadoutInventory();
    }

    public static void RefreshLoadoutInventory()
    {
        if (m_loadoutInventory == null || m_loadoutGrid == null || m_loadoutGrid.m_uiGroup == null)
            return;

        m_loadoutInventory.RemoveAll();

        if (string.IsNullOrWhiteSpace(m_selectedItem))
            return;

        Player player = Player.m_localPlayer;
        if (player == null) return;

        string key = $"{LoadoutKey}{m_selectedItem}";
        if (!player.m_customData.TryGetValue(key, out string serializedData))
            return;

        PersonalLoadout loadout = PersonalLoadout.Deserialize(m_selectedItem, serializedData);
        foreach (ItemDrop.ItemData item in loadout.Items)
        {
            if (item != null)
            {
                m_loadoutInventory.AddItem(item.Clone());
            }
        }

        m_loadoutGrid.UpdateInventory(m_loadoutInventory, null, null);
    }

    public static int FindSelectedLoadout(GameObject button)
    {
        for (int index = 0; index < m_itemList.Count; ++index)
        {
            if (m_itemList[index] == button)
                return index;
        }

        return -1;
    }

    public static void SelectItem(int index, bool center)
    {
        try
        {
            for (int index1 = 0; index1 < m_itemList.Count; ++index1)
            {
                bool flag = index1 == index;
                m_itemList[index1].transform.Find("selected").gameObject.SetActive(flag);
            }

            if (center && index >= 0)
                m_itemEnsureVisible.CenterOnItem(m_itemList[index].transform as RectTransform);
            m_selectedItem = index < 0 ? null : GetAvailableLoadouts()[index];
        }
        catch
        {
            // if this fails, just add a loadout to the player's custom data
            for (int i = 1; i < 11; ++i)
            {
                Player.m_localPlayer.m_customData[$"{LoadoutKey}{i}"] = "";
            }

            FillList();
        }
    }

    public static int GetSelectedItemIndex()
    {
        int selectedItemIndex = 0;
        List<string> availableLoadouts = GetAvailableLoadouts();
        for (int index = 0; index < availableLoadouts.Count; ++index)
        {
            if (availableLoadouts[index] == m_selectedItem)
                selectedItemIndex = index;
        }

        return selectedItemIndex;
    }

    public void UpdateSwitchLoadoutButton()
    {
        UITooltip component = m_chooseButton.GetComponent<UITooltip>();
        if (m_selectedItem != null)
        {
            Player player = Player.m_localPlayer;
            if (player == null || !player.m_customData.TryGetValue($"{LoadoutKey}{m_selectedItem}", out string serializedData)) return;
            PersonalLoadout selectedLoadout = PersonalLoadout.Deserialize(m_selectedItem, serializedData);
            bool isLoadoutEmpty = selectedLoadout.Items.Count == 0;

            bool flag2 = player.GetInventory().HaveEmptySlot() || isLoadoutEmpty;
            m_chooseButton.interactable = flag2;

            string buttonText = isLoadoutEmpty ? "$azu_epi_emptyLoadout" : "$azu_epi_swapLoadout";
            //m_chooseButton.transform.Find(AzuExtendedPlayerInventoryPlugin.HasAuga ? "Label" : "Text").GetComponent<TMP_Text>().text = Localization.instance.Localize(buttonText);
            m_chooseButton.transform.Find("Text").GetComponent<TMP_Text>().text = Localization.instance.Localize(buttonText);

            component.m_text = !flag2 ? Localization.instance.Localize("$inventory_full") : "";
        }
        else
        {
            m_chooseButton.interactable = false;
            component.m_text = "";
        }
    }

    public void UpdateRecipeGamepadInput()
    {
        if (m_itemList.Count <= 0)
            return;
        if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
            SelectItem(Mathf.Min(m_itemList.Count - 1, GetSelectedItemIndex() + 1), true);
        if (!ZInput.GetButtonDown("JoyLStickUp") && !ZInput.GetButtonDown("JoyDPadUp"))
            return;
        SelectItem(Mathf.Max(0, GetSelectedItemIndex() - 1), true);
    }

    public string GetText()
    {
        if (string.IsNullOrEmpty(m_loadoutBeingRenamed))
            return "";

        Player player = Player.m_localPlayer;
        if (player == null) return "";

        string nameKey = $"{LoadoutNameKey}{m_loadoutBeingRenamed}";
        if (player.m_customData.TryGetValue(nameKey, out string customName))
            return customName;

        return m_loadoutBeingRenamed;
    }

    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(m_loadoutBeingRenamed))
            return;

        Player player = Player.m_localPlayer;
        if (player == null) return;

        text = text.Trim();
        if (string.IsNullOrEmpty(text))
            text = m_loadoutBeingRenamed;

        if (text.Length > 20)
            text = text.Substring(0, 20);

        string nameKey = $"{LoadoutNameKey}{m_loadoutBeingRenamed}";
        player.m_customData[nameKey] = text;

        AzuExtendedPlayerInventoryLogger.LogInfo($"Renamed loadout '{m_loadoutBeingRenamed}' to '{text}'");

        m_loadoutBeingRenamed = null;

        FillList();
    }

    public static void RequestRenameLoadout(string loadoutId)
    {
        if (m_instance == null)
            return;

        m_loadoutBeingRenamed = loadoutId;
        TextInput.instance.RequestText((TextReceiver)m_instance, "$azu_epi_loadout_rename", 20);
    }

    public static string GetLoadoutCustomName(string loadoutId)
    {
        Player player = Player.m_localPlayer;
        if (player == null) return loadoutId;

        string nameKey = $"{LoadoutNameKey}{loadoutId}";
        if (player.m_customData.TryGetValue(nameKey, out string customName) && !string.IsNullOrEmpty(customName))
            return customName;

        return $"Loadout {loadoutId}";
    }
}

public static class InventorySerializer
{
    public static string SerializeInventory(Inventory inventory)
    {
        ZPackage pkg = new();
        inventory.Save(pkg);
        return pkg.GetBase64();
    }

    public static void DeserializeInventory(Inventory inventory, string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            ZPackage pkg = new(data);
            inventory.Load(pkg);
        }
    }
}

public class PersonalLoadout
{
    public string LoadoutName { get; set; }
    public List<ItemDrop.ItemData> Items { get; set; }

    public PersonalLoadout(string loadoutName, List<ItemDrop.ItemData> items)
    {
        LoadoutName = loadoutName;
        Items = items;
    }

    public string Serialize()
    {
        Inventory tempInventory = new("Loadout", null, 100, 100);
        foreach (ItemDrop.ItemData? item in Items)
        {
            tempInventory.AddItem(item.Clone());
        }

        return InventorySerializer.SerializeInventory(tempInventory);
    }

    public static PersonalLoadout Deserialize(string loadoutName, string data)
    {
        Inventory tempInventory = new("Loadout", null, 100, 100);
        InventorySerializer.DeserializeInventory(tempInventory, data);
        List<ItemDrop.ItemData> items = tempInventory.GetAllItems();
        return new PersonalLoadout(loadoutName, items);
    }
}