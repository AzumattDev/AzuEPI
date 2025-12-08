using System.Diagnostics.CodeAnalysis;

namespace AzuEPI.Game.Loadout;

public class UIDragger : EventTrigger
{
    public delegate void UIDroppedHandler(object source, Vector3 newLocalPosition);

    public event UIDroppedHandler OnUIDropped = null!;

    private bool _isDragging;

    public void Update()
    {
        if (!_isDragging) return;
        transform.position = Input.mousePosition;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || !Input.GetKey(KeyCode.LeftControl)) return;
        _isDragging = true;
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    public override void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        _isDragging = false;
        OnUIDropped?.Invoke(this, new Vector3(transform.localPosition.x, transform.localPosition.y, -1f));
    }
}