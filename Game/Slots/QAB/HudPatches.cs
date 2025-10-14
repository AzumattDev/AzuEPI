using AzuEPI.Core.InventoryHandlers;
using AzuEPI.EPI;

namespace AzuEPI.Slots.QAB;

public class HudPatches
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    private static class HudAwakePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AddEquipmentRow.Value.isOff())
                return;

            API.API.HudAwake(__instance);

            Transform transform = Object.Instantiate(__instance.m_rootObject.transform.Find("HotKeyBar"), __instance.m_rootObject.transform, true);
            transform.name = QabName;
            transform.GetComponent<RectTransform>().localPosition = Vector3.zero;

            API.API.HudAwakeComplete(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    private static class HudUpdatePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AddEquipmentRow.Value.isOff() || Player.m_localPlayer == null)
                return;

            API.API.HudUpdate(__instance);

            float scaleFactor = GuiScaler.m_largeGuiScale;
            Vector3 mousePosition = Input.mousePosition;

            QuickAccessBar.SetElementPositions();
            if (ExtendedPlayerInventory.lastMousePos == Vector3.zero)
                ExtendedPlayerInventory.lastMousePos = mousePosition;

            Transform hudrootTransform = Hud.instance.transform.Find("hudroot");
            Transform quickAccessBarTransform = hudrootTransform.Find(QabName);

            if (QuickslotDragKeys.Value.IsPressed() && quickAccessBarTransform != null)
            {
                RectTransform quickAccessBarRect = quickAccessBarTransform.GetComponent<RectTransform>();
                Vector2 anchoredPosition = quickAccessBarRect.anchoredPosition;
                Vector2 sizeDelta = quickAccessBarRect.sizeDelta;
                float quickAccessScale = QuickAccessScale.Value;

                Rect rect = new(anchoredPosition.x * scaleFactor, anchoredPosition.y * scaleFactor + Screen.height - sizeDelta.y * scaleFactor * quickAccessScale, (float)(sizeDelta.x * scaleFactor * quickAccessScale * 0.375), sizeDelta.y * scaleFactor * quickAccessScale);

                if (rect.Contains(ExtendedPlayerInventory.lastMousePos) && ExtendedPlayerInventory.currentlyDragging is "" or QabName)
                {
                    float deltaX = (mousePosition.x - ExtendedPlayerInventory.lastMousePos.x) / scaleFactor;
                    float deltaY = (mousePosition.y - ExtendedPlayerInventory.lastMousePos.y) / scaleFactor;

                    QuickAccessX.Value += deltaX;
                    QuickAccessY.Value += deltaY;
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

            API.API.HudUpdateComplete(__instance);
        }
    }
}