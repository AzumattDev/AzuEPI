using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AzuExtendedPlayerInventory.Moveable
{
    internal class MoveableChestInventory
    {
        public static ConfigEntry<float> ChestInventoryX = null!;
        public static ConfigEntry<float> ChestInventoryY = null!;
        public static ConfigEntry<KeyboardShortcut> ChestDragKeys = null!;
        public static ConfigEntry<KeyboardShortcut> ModKeyTwoChestMove = null!;
        private static Vector3 lastMousePos;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        private static class MoveableChestInventoryGuiUpdatePatch
        {
            private static void Postfix(InventoryGui __instance)
            {
                Vector3 mousePos = Input.mousePosition;
                if (!__instance.m_container.gameObject.activeSelf)
                {
                    lastMousePos = mousePos;
                    return;
                }


                if (ChestInventoryX.Value < 0)
                    ChestInventoryX.Value = __instance.m_container.anchorMin.x;
                if (ChestInventoryY.Value < 0)
                    ChestInventoryY.Value = __instance.m_container.anchorMin.y;

                __instance.m_container.anchorMin = new Vector2(ChestInventoryX.Value, ChestInventoryY.Value);
                __instance.m_container.anchorMax = new Vector2(ChestInventoryX.Value, ChestInventoryY.Value);


                if (lastMousePos == Vector3.zero)
                    lastMousePos = mousePos;


                PointerEventData eventData = new(EventSystem.current)
                {
                    position = lastMousePos,
                };

                if (!RectTransformUtility.RectangleContainsScreenPoint(__instance.m_containerGrid.m_gridRoot, Input.mousePosition) && ChestDragKeys.Value.IsPressed())
                {
                    List<RaycastResult> raycastResults = new();
                    EventSystem.current.RaycastAll(eventData, raycastResults);

                    foreach (RaycastResult rcr in raycastResults.Where(rcr =>
                                 rcr.gameObject.layer == LayerMask.NameToLayer("UI") &&
                                 rcr.gameObject.name == "Bkg" &&
                                 rcr.gameObject.transform.parent.name == "Container"))
                    {
                        ChestInventoryX.Value += (mousePos.x - lastMousePos.x) / Screen.width;
                        ChestInventoryY.Value += (mousePos.y - lastMousePos.y) / Screen.height;
                    }
                }

                lastMousePos = mousePos;
            }
        }
    }
}