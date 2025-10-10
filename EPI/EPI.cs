using AzuEPI.InventoryHandlers;
using AzuEPI.Slots;
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

    public static void SetElementPositions()
    {
        Transform transform = Hud.instance.transform.Find("hudroot");
        if (!(transform.Find(QABName)?.GetComponent<RectTransform>() != null))
            return;
        if (QuickAccessX.Value == 9999.0)
            QuickAccessX.Value = transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.x - 32f;
        if (QuickAccessY.Value == 9999.0)
            QuickAccessY.Value = transform.Find("healthpanel").GetComponent<RectTransform>().anchoredPosition.y - 870f;
        transform.Find(QABName).GetComponent<RectTransform>().anchoredPosition = new Vector2(QuickAccessX.Value, QuickAccessY.Value);
        transform.Find(QABName).GetComponent<RectTransform>().localScale = new Vector3(QuickAccessScale.Value, QuickAccessScale.Value, 1f);
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.RPC_TakeAllRespons))]
internal static class ContainerRPCRequestTakeAllPatch
{
    private static void Postfix(Container __instance, ref bool granted)
    {
        if (granted) InventoryHealth.InventoryFix();
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
internal static class MoveAllToPatch
{
    private static void Postfix(Inventory __instance, Inventory fromInventory)
    {
        if (__instance.IsPlayerInventory()) InventoryHealth.InventoryFix();
    }
}