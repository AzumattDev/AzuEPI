namespace AzuEPI.Game.Patches;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
internal static class InventoryGridQueuedFillPatch
{
    private static readonly HashSet<Image> ConfiguredImages = [];

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(InventoryGrid __instance, Player player)
    {
        if (!player) return;

        foreach (InventoryGrid.Element element in __instance.m_elements)
        {
            if (!element.m_queued) continue;

            ConfigureQueuedImageForFill(element.m_queued);

            if (!element.m_used)
            {
                element.m_queued.fillAmount = 0f;
                continue;
            }

            ItemDrop.ItemData? item = __instance.m_inventory?.GetItemAt(element.m_pos.x, element.m_pos.y);
            if (item == null)
            {
                element.m_queued.fillAmount = 0f;
                continue;
            }

            float progress = GetEquipProgress(player, item, out bool isActivelyEquipping, out bool isQueuedWaiting);

            if (isActivelyEquipping)
            {
                element.m_queued.fillAmount = progress;
                element.m_queued.enabled = true;
            }
            else if (isQueuedWaiting)
            {
                element.m_queued.fillAmount = 1f;
                element.m_queued.enabled = true;
            }
            else
            {
                element.m_queued.fillAmount = 0f;
            }
        }
    }

    private static void ConfigureQueuedImageForFill(Image image)
    {
        if (ConfiguredImages.Contains(image)) return;

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Vertical;
        image.fillOrigin = (int)Image.OriginVertical.Bottom;
        image.fillAmount = 0f;

        ConfiguredImages.Add(image);
    }

    public static float GetEquipProgress(Player player, ItemDrop.ItemData item, out bool isActivelyEquipping, out bool isQueuedWaiting)
    {
        isActivelyEquipping = false;
        isQueuedWaiting = false;

        if (item == null || player == null) return 0f;

        for (int i = 0; i < player.m_actionQueue.Count; i++)
        {
            Player.MinorActionData action = player.m_actionQueue[i];
            if (action.m_item != item) continue;
            if (action.m_type != Player.MinorActionData.ActionType.Equip &&
                action.m_type != Player.MinorActionData.ActionType.Unequip)
                continue;

            if (i == 0 && action.m_time > 0f)
            {
                isActivelyEquipping = true;
                return action.m_duration <= 0f ? 1f : Mathf.Clamp01(action.m_time / action.m_duration);
            }

            isQueuedWaiting = true;
            return 1f;
        }

        return 0f;
    }

    public static float GetEquipProgress(Player player, ItemDrop.ItemData item)
    {
        return GetEquipProgress(player, item, out _, out _);
    }

    public static void ClearCache()
    {
        ConfiguredImages.Clear();
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
internal static class InventoryGuiOnDestroyQueuedFillPatch
{
    private static void Postfix()
    {
        InventoryGridQueuedFillPatch.ClearCache();
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
static class HudAwakePatch
{
    public static List<GameObject> actionBarChildren = [];

    static void Postfix(Hud __instance)
    {
        if (actionBarChildren.Count > 0)
        {
            actionBarChildren.Clear();
        }

        Transform[]? children = __instance.m_actionBarRoot.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            actionBarChildren.Add(child.gameObject);
        }
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateActionProgress))]
static class HudUpdateActionProgressPatch
{
    static void Prefix(Hud __instance)
    {
        Player? player = Player.m_localPlayer;
        if (!player || player.m_actionQueue.Count <= 0) return;
        if (HudAwakePatch.actionBarChildren.Count <= 0) return;
        
        if (player.m_actionQueue[0].m_type is Player.MinorActionData.ActionType.Equip or Player.MinorActionData.ActionType.Unequip)
        {
            foreach (GameObject actionBarChild in HudAwakePatch.actionBarChildren)
            {
                actionBarChild.SafeSetActive(false);
            }
        }
        else
        {
            foreach (GameObject actionBarChild in HudAwakePatch.actionBarChildren)
            {
                actionBarChild.SafeSetActive(true);
            }
        }
    }
}

[HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
internal static class HotkeyBarQueuedFillPatch
{
    private const string FillOverlayName = "EPI_QueuedFillOverlay";
    private static readonly Dictionary<GameObject, Image> FillOverlayByQueued = new();

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(HotkeyBar __instance, Player player)
    {
        if (!player) return;

        if (__instance.name == QabName) return;
        
        List<ItemDrop.ItemData> boundItems = [];
        player.GetInventory().GetBoundItems(boundItems);

        foreach (HotkeyBar.ElementData element in __instance.m_elements)
        {
            if (element.m_queued == null) continue;

            // Get or create the fill overlay (separate from original queued image to avoid MaterialMan conflicts)
            Image fillOverlay = EnsureFillOverlay(element.m_queued);

            if (!element.m_used)
            {
                fillOverlay.fillAmount = 0f;
                fillOverlay.enabled = false;
                continue;
            }

            ItemDrop.ItemData? item = null;
            foreach (ItemDrop.ItemData boundItem in boundItems)
            {
                int elementIndex = __instance.m_elements.IndexOf(element);
                if (boundItem.m_gridPos.x != elementIndex) continue;
                item = boundItem;
                break;
            }

            if (item == null)
            {
                fillOverlay.fillAmount = 0f;
                fillOverlay.enabled = false;
                continue;
            }

            float progress = InventoryGridQueuedFillPatch.GetEquipProgress(player, item, out bool isActivelyEquipping, out bool isQueuedWaiting);

            if (isActivelyEquipping)
            {
                fillOverlay.fillAmount = progress;
                fillOverlay.enabled = true;
                // Hide vanilla queued indicator since we're showing progress
                element.m_queued.SetActive(false);
            }
            else if (isQueuedWaiting)
            {
                // Item is queued but waiting - let vanilla handle the solid indicator
                fillOverlay.fillAmount = 0f;
                fillOverlay.enabled = false;
                element.m_queued.SetActive(true);
            }
            else
            {
                fillOverlay.fillAmount = 0f;
                fillOverlay.enabled = false;
                element.m_queued.SetActive(false);
            }
        }
    }

    private static Image EnsureFillOverlay(GameObject queuedGo)
    {
        if (FillOverlayByQueued.TryGetValue(queuedGo, out Image? existing) && existing)
            return existing;

        Image? originalImage = queuedGo.GetComponent<Image>();

        GameObject overlayGo = new(FillOverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform overlayRT = (RectTransform)overlayGo.transform;
        overlayRT.SetParent(queuedGo.transform, false);

        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        overlayRT.pivot = new Vector2(0.5f, 0.5f);

        Image fillImage = overlayGo.GetComponent<Image>();
        fillImage.raycastTarget = false;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 0f;
        fillImage.enabled = false;

        if (originalImage)
        {
            fillImage.sprite = originalImage.sprite;
            fillImage.color = originalImage.color;
        }

        FillOverlayByQueued[queuedGo] = fillImage;
        return fillImage;
    }

    public static void ClearCache()
    {
        FillOverlayByQueued.Clear();
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
internal static class HudOnDestroyQueuedFillPatch
{
    private static void Postfix()
    {
        HotkeyBarQueuedFillPatch.ClearCache();
    }
}