namespace AzuEPI.Game.Compatibility;

[HarmonyPatch(typeof(RectTransformUtility), nameof(RectTransformUtility.RectangleContainsScreenPoint), typeof(RectTransform), typeof(Vector2))]
public static class BackpacksCompatBlockInteractInput
{
    private static void Postfix(RectTransform rect, Vector2 screenPoint, ref bool __result)
    {
        if (__result || !InventoryGui.instance)
            return;

        bool isPlayerGrid = rect == InventoryGui.instance.m_playerGrid.m_gridRoot;
        bool isCraftingPanel = InventoryGui.instance.m_crafting != null && InventoryGui.instance.m_crafting.TryGetComponent<RectTransform>(out RectTransform? craftingRT) && rect == craftingRT;

        if (!isPlayerGrid && !isCraftingPanel)
            return;

        Player player = Player.m_localPlayer;
        if (!player) return;

        Inventory inventory = player.GetInventory();
        if (inventory == null) return;

        int baseIndex = Layout.GetBaseSlotIndex(inventory);
        List<Model.Slot?> slots = InventoryGuiPatches.UpdateInventory_Patch.slots;

        for (int i = 0; i < slots.Count; ++i)
        {
            int elementIndex = baseIndex + i;
            if (elementIndex >= InventoryGui.instance.m_playerGrid.m_elements.Count)
                break;

            InventoryGrid.Element element = InventoryGui.instance.m_playerGrid.m_elements[elementIndex];
            if (element?.m_go == null) continue;
            RectTransform elementRT = element.m_go.transform as RectTransform;
            if (elementRT == null) continue;
            Vector3[] corners = new Vector3[4];
            elementRT.GetWorldCorners(corners);

            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

            if (!(screenPoint.x >= minX) || !(screenPoint.x <= maxX) || !(screenPoint.y >= minY) || !(screenPoint.y <= maxY)) continue;
            __result = true;
            return;
        }
    }
}