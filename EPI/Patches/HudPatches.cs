using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class HudPatches
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    private static class HudAwakePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
                return;

            API.HudAwake(__instance);

            Transform transform = Object.Instantiate(__instance.m_rootObject.transform.Find("HotKeyBar"), __instance.m_rootObject.transform, true);
            transform.name = ExtendedPlayerInventory.QABName;
            transform.GetComponent<RectTransform>().localPosition = Vector3.zero;

            API.HudAwakeComplete(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    private static class HudUpdatePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || Player.m_localPlayer == null)
                return;

            API.HudUpdate(__instance);

            float scaleFactor = GameObject.Find("LoadingGUI").GetComponent<CanvasScaler>().scaleFactor;
            Vector3 mousePosition = Input.mousePosition;

            ExtendedPlayerInventory.SetElementPositions();
            if (ExtendedPlayerInventory.lastMousePos == Vector3.zero)
                ExtendedPlayerInventory.lastMousePos = mousePosition;

            Transform hudrootTransform = Hud.instance.transform.Find("hudroot");
            Transform quickAccessBarTransform = hudrootTransform.Find(ExtendedPlayerInventory.QABName);

            if (AzuExtendedPlayerInventoryPlugin.QuickslotDragKeys.Value.IsPressed() && quickAccessBarTransform != null)
            {
                RectTransform quickAccessBarRect = quickAccessBarTransform.GetComponent<RectTransform>();
                Vector2 anchoredPosition = quickAccessBarRect.anchoredPosition;
                Vector2 sizeDelta = quickAccessBarRect.sizeDelta;
                float quickAccessScale = AzuExtendedPlayerInventoryPlugin.QuickAccessScale.Value;

                Rect rect = new(anchoredPosition.x * scaleFactor, anchoredPosition.y * scaleFactor + Screen.height - sizeDelta.y * scaleFactor * quickAccessScale, (float)(sizeDelta.x * scaleFactor * quickAccessScale * 0.375), sizeDelta.y * scaleFactor * quickAccessScale);

                if (rect.Contains(ExtendedPlayerInventory.lastMousePos) && ExtendedPlayerInventory.currentlyDragging is "" or ExtendedPlayerInventory.QABName)
                {
                    float deltaX = (mousePosition.x - ExtendedPlayerInventory.lastMousePos.x) / scaleFactor;
                    float deltaY = (mousePosition.y - ExtendedPlayerInventory.lastMousePos.y) / scaleFactor;

                    AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value += deltaX;
                    AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value += deltaY;
                    ExtendedPlayerInventory.currentlyDragging = ExtendedPlayerInventory.QABName;
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