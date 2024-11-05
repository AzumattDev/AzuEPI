using UnityEngine.EventSystems;

namespace AzuExtendedPlayerInventory.Moveable;

public class DragControl : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform dragRectTransform = new();

    private void Start()
    {
        dragRectTransform = GetComponent<RectTransform>();
        dragRectTransform.anchoredPosition = AzuExtendedPlayerInventoryPlugin.UIAnchor.Value;
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragRectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        AzuExtendedPlayerInventoryPlugin.UIAnchor.Value = dragRectTransform.anchoredPosition;
    }
}