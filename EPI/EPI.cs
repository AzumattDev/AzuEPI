namespace AzuEPI.EPI;

internal class ExtendedPlayerInventory
{
    internal static readonly GameObject _elementPrefab = null!;

    internal static ItemDrop.ItemData?[] equipItems = new ItemDrop.ItemData[15];

    public static Vector3 lastMousePos;
    public static string currentlyDragging = null!;
    internal static readonly int Visible = Animator.StringToHash("visible");
    public static List<HotkeyBar> HotkeyBars { get; set; } = null!;

    public static int SelectedHotkeyBarIndex { get; set; } = -1;

    public static Vector2 LastSlotPosition { get; set; }
}