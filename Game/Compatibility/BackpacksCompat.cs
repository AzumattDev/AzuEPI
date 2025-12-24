namespace AzuEPI.Game.Compatibility;

[HarmonyPatch(typeof(RectTransformUtility), nameof(RectTransformUtility.RectangleContainsScreenPoint), typeof(RectTransform), typeof(Vector2))]
public static class BackpacksCompatBlockInteractInput
{
    private static void Postfix(RectTransform rect, Vector2 screenPoint, ref bool __result)
    {
        if (!InventoryGui.instance || rect != InventoryGui.instance.m_playerGrid.m_gridRoot)
            return;
        int fullheight = API.GetFullHeight(InventoryGui.instance.m_playerGrid.m_width);
        for (int i = 0; i < Math.Min(InventoryGuiPatches.UpdateInventory_Patch.slots.Count, InventoryGui.instance.m_playerGrid.m_elements.Count - fullheight); ++i)
            if (RectTransformUtility.RectangleContainsScreenPoint(InventoryGui.instance.m_playerGrid.m_elements[fullheight + i].m_go.transform as RectTransform, screenPoint))
            {
                __result = true;
                return;
            }
    }
}