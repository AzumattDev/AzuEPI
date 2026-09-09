namespace AzuEPI.Game.Favoriting;

[HarmonyPatch(typeof(InventoryGrid))]
internal class InventoryGridButtonHandlingPatches
{
    private static bool Prepare() => !FavoritingMode.IsExternalFavoritingModLoaded();

    [HarmonyPatch(nameof(InventoryGrid.OnRightDown)), HarmonyPrefix]
    internal static bool OnRightClick(InventoryGrid __instance, UIInputHandler element)
    {
        return HandleClick(__instance, element, false);
    }

    [HarmonyPatch(nameof(InventoryGrid.OnLeftDown)), HarmonyPrefix]
    internal static bool OnLeftClick(InventoryGrid __instance, UIInputHandler clickHandler)
    {
        return HandleClick(__instance, clickHandler, true);
    }

    internal static bool HandleClick(InventoryGrid __instance, UIInputHandler clickHandler, bool isLeftClick)
    {
        Vector2i buttonPos = __instance.GetButtonPos(clickHandler.gameObject);

        return HandleClick(__instance, buttonPos, isLeftClick);
    }

    [HarmonyPatch(nameof(InventoryGrid.UpdateGamepad))]
    private static void Postfix(InventoryGrid __instance)
    {
        if (__instance != InventoryGui.instance.m_playerGrid)
        {
            return;
        }

        if (!__instance.m_uiGroup.IsActive)
        {
            return;
        }

        if (ZInput.GetButtonDown("JoyButtonA"))
        {
            HandleClick(__instance, __instance.m_selected, true);
        }
        else if (ZInput.GetButtonDown("JoyButtonX"))
        {
            HandleClick(__instance, __instance.m_selected, false);
        }
    }


    internal static bool HandleClick(InventoryGrid __instance, Vector2i buttonPos, bool isLeftClick)
    {
        if (InventoryGui.instance.m_playerGrid != __instance)
        {
            return true;
        }

        Player localPlayer = Player.m_localPlayer;

        if (localPlayer.IsTeleporting())
        {
            return true;
        }

        if (InventoryGui.instance.m_dragGo)
        {
            return true;
        }

        if (!FavoritingMode.IsInFavoritingMode())
        {
            return true;
        }

        if (buttonPos == new Vector2i(-1, -1))
        {
            return true;
        }

        switch (isLeftClick)
        {
            case false:
                UserConfig.GetPlayerConfig(localPlayer.GetPlayerID()).ToggleSlotFavoriting(buttonPos);
                break;
            default:
            {
                ItemDrop.ItemData itemAt = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);

                if (itemAt == null)
                {
                    return true;
                }

                UserConfig.GetPlayerConfig(localPlayer.GetPlayerID()).ToggleItemNameFavoriting(itemAt.m_shared);

                break;
            }
        }

        return false;
    }
}
