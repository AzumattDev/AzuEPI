using AzuExtendedPlayerInventory;
using AzuExtendedPlayerInventory.EPI.Patches;

namespace AzuEPI.EPI;

internal class ExtendedPlayerInventory
{
    public const string QABName = "QuickAccessBar";
    public const string AzuBkgName = "AzuEquipmentBkg";
    public const string DropAllButtonName = "AzuDropAllButton";
    public const string MinimalUiguid = "Azumatt.MinimalUI";

    private static readonly GameObject _elementPrefab = null!;

    internal static ItemDrop.ItemData?[] equipItems = new ItemDrop.ItemData[5];

    public static Vector3 lastMousePos;
    public static string currentlyDragging = null!;
    internal static readonly int Visible = Animator.StringToHash("visible");
    public static List<HotkeyBar> HotkeyBars { get; set; } = null!;

    public static int SelectedHotkeyBarIndex { get; set; } = -1;

    public static Vector2 LastSlotPosition { get; set; }

    public static void SetSlotText(string value, Transform transform, bool center = true)
    {
        Transform transform1 = transform.Find("binding");
        if (!transform1)
            transform1 = Object.Instantiate(_elementPrefab.transform.Find("binding"), transform);
        var textComp = transform1.GetComponent<TMP_Text>();
        textComp.enabled = true;
        textComp.overflowMode = TextOverflowModes.Overflow;
        textComp.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        textComp.fontSizeMin = 10f;
        textComp.fontSizeMax = 18f;
        textComp.enableAutoSizing = true;
        textComp.text = value;
        if (!center)
            return;
        transform1.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 17f);
        transform1.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, -10f);
    }
    
    private static int LinearIndexIntoEpiBlock(Inventory inv, int x, int y)
    {
        int width = inv.GetWidth();
        int addedRows = API.GetAddedRows(width);
        int adjustedHeight = inv.GetHeight() - addedRows; // vanilla rows only
        if (y < adjustedHeight) return -1;

        int baseLinear = adjustedHeight * width;
        int linear = y * width + x;
        return linear - baseLinear;
    }

    internal static bool IsEquipmentSlotFree(Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        var addedRows = API.GetAddedRows(inventory.GetWidth());
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is InventoryGuiPatches.EquipmentSlot slot && slot.Valid(item) && !slot.Occupied);
        return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
    }
    internal static bool IsEquipmentSlotFree(Inventory inventory, out int which)
    {
        var addedRows = API.GetAddedRows(inventory.GetWidth());
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is InventoryGuiPatches.EquipmentSlot { EquipmentSlot: not null, Occupied: false });
        return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
    }

    internal static bool IsQuickSlotFree(Inventory inventory, out int which)
    {
        var addedRows = API.GetAddedRows(inventory.GetWidth());
        which = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s is { IsQuickSlot: true, EquipmentSlot: null, Occupied: false });
        return which >= 0 && inventory.GetItemAt(which, inventory.GetHeight() - addedRows) == null;
    }

    internal static bool IsAtEquipmentSlot(Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        var inventoryRows = inventory.GetHeight() - API.GetAddedRows(inventory.GetWidth());
        if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.isOff() || item.m_gridPos.y < inventoryRows || (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length)
        {
            which = -1;
            return false;
        }

        which = (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x;
        return true;
    }

    internal static bool IsAtQuickSlot(Inventory inventory, ItemDrop.ItemData item, out int which)
    {
        var inventoryRows = inventory.GetHeight() - API.GetAddedRows(inventory.GetWidth());
        if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.isOff() || item.m_gridPos.y < inventoryRows || (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x < InventoryGuiPatches.UpdateInventory_Patch.slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length)
        {
            which = -1;
            return false;
        }

        which = (item.m_gridPos.y - inventoryRows) * inventory.GetWidth() + item.m_gridPos.x;
        return true;
    }

    internal static bool IsEquipmentCell(Inventory inv, int x, int y, out int whichSlot)
    {
        whichSlot = -1;
        int li = LinearIndexIntoEpiBlock(inv, x, y);
        if (li < 0) return false;
        if (li >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count) return false;
        if (InventoryGuiPatches.UpdateInventory_Patch.slots[li] is not InventoryGuiPatches.EquipmentSlot) return false;
        whichSlot = li;
        return true;
    }

    internal static bool IsQuickCell(Inventory inv, int x, int y)
    {
        int li = LinearIndexIntoEpiBlock(inv, x, y);
        if (li < 0) return false;
        if (li >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count) return false;
        InventoryGuiPatches.Slot? s = InventoryGuiPatches.UpdateInventory_Patch.slots[li];
        return s is not InventoryGuiPatches.EquipmentSlot && s.IsQuickSlot;
    }

    internal static bool IsHiddenCell(Inventory inv, int x, int y)
    {
        int li = LinearIndexIntoEpiBlock(inv, x, y);
        return li >= 0 && li >= InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
    }
    
    internal static IEnumerable<Vector2i> EnumerateQuickCells(Inventory inv)
    {
        int width = inv.GetWidth();
        int addedRows = API.GetAddedRows(width);
        int adjustedHeight = inv.GetHeight() - addedRows;

        int firstLinear = adjustedHeight * width;
        int total = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;

        int quickCount = AzuExtendedPlayerInventoryPlugin.Hotkeys.Length;
        int quickStart = total - quickCount;
        for (int i = quickStart; i < total; i++)
        {
            int li = firstLinear + i;
            yield return new Vector2i(li % width, li / width);
        }
    }
    
    internal static bool TryFindEmptyQuickCell(Inventory inv, out Vector2i pos)
    {
        foreach (var p in EnumerateQuickCells(inv))
        {
            if (inv.GetItemAt(p.x, p.y) != null) continue;
            pos = p;
            return true;
        }

        pos = new Vector2i(-1, -1);
        return false;
    }

    internal static bool ShouldGuard(Inventory inv)
    {
        return Player.m_localPlayer && inv == Player.m_localPlayer.GetInventory() && AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.isOn();
    }

    public static void SetElementPositions()
    {
        Transform transform = Hud.instance.transform.Find("hudroot");
        if (!(transform.Find(QABName)?.GetComponent<RectTransform>() != null))
            return;
        if (AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value == 9999.0)
            AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value = transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x - 32f;
        if (AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value == 9999.0)
            AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value = transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y - 870f;
        transform.Find(QABName).GetComponent<RectTransform>().anchoredPosition = new Vector2(AzuExtendedPlayerInventoryPlugin.QuickAccessX.Value, AzuExtendedPlayerInventoryPlugin.QuickAccessY.Value);
        transform.Find(QABName).GetComponent<RectTransform>().localScale = new Vector3(AzuExtendedPlayerInventoryPlugin.QuickAccessScale.Value, AzuExtendedPlayerInventoryPlugin.QuickAccessScale.Value, 1f);
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.RPC_TakeAllRespons))]
internal static class ContainerRPCRequestTakeAllPatch
{
    private static void Postfix(Container __instance, ref bool granted)
    {
        if (Player.m_localPlayer == null)
            return;
        if (granted) Utilities.Utilities.InventoryFix();
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
internal static class MoveAllToPatch
{
    private static void Postfix(Inventory __instance, Inventory fromInventory)
    {
        if (Player.m_localPlayer == null)
            return;
        if (__instance == Player.m_localPlayer.GetInventory()) Utilities.Utilities.InventoryFix();
    }
}