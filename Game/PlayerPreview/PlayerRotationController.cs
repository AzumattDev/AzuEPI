namespace AzuEPI.Game.PlayerPreview;

public class PlayerRotationController : MonoBehaviour, IDragHandler, IEndDragHandler, IScrollHandler
{
    internal bool dragging;
    private Vector3 lastMousePosition;
    public float pitch;
    public float yaw;
    internal float zoom = -3;
    public static PlayerRotationController instance;

    private void Awake()
    {
        instance = this;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (AzuEPICharacterPanel.instance.render.rect.Contains((Vector2)transform.InverseTransformPoint(ZInput.mousePosition)))
        {
            if (!dragging)
            {
                lastMousePosition = Input.mousePosition;
                dragging = true;
            }

            pitch = Mathf.Clamp(pitch + (lastMousePosition - Input.mousePosition).y, -80, 80);
            yaw -= (lastMousePosition - Input.mousePosition).x;
            AzuEPICharacterPanel.instance.cam.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
            AzuEPICharacterPanel.instance.cam.transform.eulerAngles = new Vector3(AzuEPICharacterPanel.instance.cam.transform.eulerAngles.x, AzuEPICharacterPanel.instance.cam.transform.eulerAngles.y, 0);
            AzuEPICharacterPanel.instance.cam.transform.position = AzuEPICharacterPanel.instance.basePosition + AzuEPICharacterPanel.instance.cam.transform.forward * zoom;
            lastMousePosition = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (AzuEPICharacterPanel.instance.render.rect.Contains((Vector2)transform.InverseTransformPoint(ZInput.mousePosition)))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0)
            {
                zoom = Mathf.Clamp(zoom + scroll * GameCamera.instance.m_zoomSens, -10, -1);
                AzuEPICharacterPanel.instance.cam.transform.position = AzuEPICharacterPanel.instance.basePosition + AzuEPICharacterPanel.instance.cam.transform.forward * zoom;
            }
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
static class InventoryGuiHidePatch
{
    static void Postfix(InventoryGui __instance)
    {
        PlayerRotationController.instance.pitch = 0;
        PlayerRotationController.instance.yaw = 0;
        PlayerRotationController.instance.zoom = -3;
        PlayerRotationController.instance.dragging = false;
        if (AzuEPICharacterPanel.instance.cam)
        {
            AzuEPICharacterPanel.instance.cam.transform.rotation = Quaternion.Euler(0, 0, 0);
            AzuEPICharacterPanel.instance.cam.transform.position = AzuEPICharacterPanel.instance.basePosition + AzuEPICharacterPanel.instance.cam.transform.forward * PlayerRotationController.instance.zoom;
        }
    }
}