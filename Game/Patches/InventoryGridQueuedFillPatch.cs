namespace AzuEPI.Game.Patches;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
internal static class InventoryGridQueuedFillPatch
{
    private static readonly Dictionary<GameObject, Image> FillOverlays = new();

    [HarmonyPriority(Priority.Last)]
    private static void Postfix(InventoryGrid __instance, Player player)
    {
        if (!player) return;

        foreach (InventoryGrid.Element element in __instance.m_elements)
        {
            if (!element.m_queued) continue;

            ItemDrop.ItemData? item = element.m_used ? __instance.m_inventory?.GetItemAt(element.m_pos.x, element.m_pos.y) : null;
            UpdateFillOverlay(element.m_queued.gameObject, element.m_queued, player, item);
        }
    }

    public static void UpdateFillOverlay(GameObject queuedGo, Image? sourceImage, Player player, ItemDrop.ItemData? item)
    {
        if (queuedGo == null) return;
        sourceImage ??= queuedGo.GetComponent<Image>();
        if (sourceImage == null) return;

        Image fillOverlay = GetOrCreateFillOverlay(queuedGo, sourceImage);

        if (item == null)
        {
            fillOverlay.fillAmount = 0f;
            fillOverlay.enabled = false;
            return;
        }

        float progress = GetEquipProgress(player, item, out bool isActivelyEquipping, out bool isQueuedWaiting);

        if (isActivelyEquipping)
        {
            sourceImage.enabled = false;
            fillOverlay.fillAmount = progress;
            fillOverlay.enabled = true;
        }
        else if (isQueuedWaiting)
        {
            sourceImage.enabled = false;
            fillOverlay.fillAmount = 1f;
            fillOverlay.enabled = true;
        }
        else
        {
            fillOverlay.fillAmount = 0f;
            fillOverlay.enabled = false;
        }
    }

    private static Image GetOrCreateFillOverlay(GameObject queuedGo, Image sourceImage)
    {
        if (FillOverlays.TryGetValue(queuedGo, out Image? existing) && existing != null)
            return existing;

        GameObject overlayGo = new GameObject("FillOverlay", typeof(RectTransform), typeof(Image));
        overlayGo.transform.SetParent(queuedGo.transform.parent, false);

        RectTransform queuedRect = queuedGo.GetComponent<RectTransform>();
        RectTransform overlayRect = overlayGo.GetComponent<RectTransform>();
        overlayRect.anchorMin = queuedRect.anchorMin;
        overlayRect.anchorMax = queuedRect.anchorMax;
        overlayRect.anchoredPosition = queuedRect.anchoredPosition;
        overlayRect.sizeDelta = queuedRect.sizeDelta;
        overlayRect.pivot = queuedRect.pivot;

        overlayGo.transform.SetSiblingIndex(queuedGo.transform.GetSiblingIndex() + 1);

        Image fillImage = overlayGo.GetComponent<Image>();
        fillImage.sprite = sourceImage.sprite;
        fillImage.color = sourceImage.color;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 0f;
        fillImage.raycastTarget = false;
        fillImage.enabled = false;

        FillOverlays[queuedGo] = fillImage;
        return fillImage;
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

            if (i == 0)
            {
                isActivelyEquipping = true;
                return action.m_duration <= 0f ? 1f : Mathf.Clamp01(action.m_time / action.m_duration);
            }

            isQueuedWaiting = true;
            return 1f;
        }

        return 0f;
    }

    public static float GetEquipProgress(Player player, ItemDrop.ItemData item) => GetEquipProgress(player, item, out _, out _);

    public static void ClearCache()
    {
        foreach (Image? overlay in FillOverlays.Values)
        {
            if (overlay != null && overlay.gameObject != null)
                Object.Destroy(overlay.gameObject);
        }
        FillOverlays.Clear();
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
static class VanillaHotkeyBarFillPatch
{
    [HarmonyPriority(Priority.Last)]
    static void Postfix(HotkeyBar __instance, Player player)
    {
        if (__instance.name == QabName) return;
        if (!player) return;

        foreach (HotkeyBar.ElementData element in __instance.m_elements)
        {
            ItemDrop.ItemData? item = null;
            if (element.m_used)
            {
                int index = __instance.m_elements.IndexOf(element);
                foreach (ItemDrop.ItemData itemData in __instance.m_items)
                {
                    if (itemData.m_gridPos.x != index || itemData.m_gridPos.y != 0) continue;
                    item = itemData;
                    break;
                }
            }

            InventoryGridQueuedFillPatch.UpdateFillOverlay(element.m_queued, null, player, item);
        }
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
static class HudOnDestroyFillCachePatch
{
    static void Postfix() => InventoryGridQueuedFillPatch.ClearCache();
}