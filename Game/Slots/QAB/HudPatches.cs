using AzuEPI.EPI;

namespace AzuEPI.Game.Slots.QAB;

public class HudPatches
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    private static class HudAwakePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AddEquipmentRow.Value.isOff())
                return;

            API.HudAwake(__instance);

            Transform? vanillaHotkeyBar = __instance.m_rootObject.transform.Find("HotKeyBar");
            if (vanillaHotkeyBar)
            {
                vanillaHotkeyBar.TryGetComponent(out RectTransform rect);
                if (rect)
                {
                    Transform transform = Object.Instantiate(rect, __instance.m_rootObject.transform, true);
                    transform.name = QabName;
                    transform.localPosition = Vector3.zero;
                    transform.SetSiblingIndex(rect.GetSiblingIndex() + 1);
                }
            }

            API.HudAwakeComplete(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    private static class HudUpdatePatch
    {
        private static Transform _cachedHudrootTransform;
        private static Transform _cachedQuickAccessBarTransform;
        private static RectTransform _cachedQuickAccessBarRect;

        private static void Postfix(Hud __instance)
        {
            if (AddEquipmentRow.Value.isOff() || Player.m_localPlayer == null)
                return;

            API.HudUpdate(__instance);

            float scaleFactor = GuiScaler.m_largeGuiScale;
            Vector3 mousePosition = Input.mousePosition;

            QuickAccessBar.SetElementPositions();
            if (ExtendedPlayerInventory.lastMousePos == Vector3.zero)
                ExtendedPlayerInventory.lastMousePos = mousePosition;

            if (_cachedHudrootTransform == null)
                _cachedHudrootTransform = Hud.instance.transform.Find("hudroot");
            if (_cachedQuickAccessBarTransform == null)
                _cachedQuickAccessBarTransform = _cachedHudrootTransform.Find(QabName);
            if (_cachedQuickAccessBarRect == null && _cachedQuickAccessBarTransform != null)
                _cachedQuickAccessBarRect = _cachedQuickAccessBarTransform.GetComponent<RectTransform>();

            if (InventoryGui.IsVisible()) return;
            
            if (QuickslotDragKeys.Value.IsPressed() && _cachedQuickAccessBarTransform != null)
            {
                RectTransform quickAccessBarRect = _cachedQuickAccessBarRect;
                Vector2 anchoredPosition = quickAccessBarRect.anchoredPosition;
                Vector2 sizeDelta = quickAccessBarRect.sizeDelta;
                float quickAccessScale = QuickAccessScale.Value;

                Rect rect = new(anchoredPosition.x * scaleFactor, anchoredPosition.y * scaleFactor + Screen.height - sizeDelta.y * scaleFactor * quickAccessScale, (float)(sizeDelta.x * scaleFactor * quickAccessScale * 0.375), sizeDelta.y * scaleFactor * quickAccessScale);

                if (rect.Contains(ExtendedPlayerInventory.lastMousePos) && ExtendedPlayerInventory.currentlyDragging is "" or QabName)
                {
                    float deltaX = (mousePosition.x - ExtendedPlayerInventory.lastMousePos.x) / scaleFactor;
                    float deltaY = (mousePosition.y - ExtendedPlayerInventory.lastMousePos.y) / scaleFactor;

                    QuickAccessLocation.Value = new Vector2(QuickAccessLocation.Value.x + deltaX, QuickAccessLocation.Value.y + deltaY);
                    ExtendedPlayerInventory.currentlyDragging = QabName;
                }
                else
                {
                    ExtendedPlayerInventory.currentlyDragging = "";
                }
            }
            else
            {
                ExtendedPlayerInventory.currentlyDragging = "";
            }

            ExtendedPlayerInventory.lastMousePos = mousePosition;

            API.HudUpdateComplete(__instance);
        }
    }
}