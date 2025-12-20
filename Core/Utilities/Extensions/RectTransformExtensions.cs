namespace AzuEPI.Core.Utilities.Extensions;

public static class RectTransformExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithAnchoredPosition(this RectTransform rt, Vector2 position)
    {
        rt.anchoredPosition = position;
        return rt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithAnchors(this RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        return rt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithAnchorMin(this RectTransform rt, Vector2 min)
    {
        rt.anchorMin = min;
        return rt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithAnchorMax(this RectTransform rt, Vector2 max)
    {
        rt.anchorMax = max;
        return rt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithPivot(this RectTransform rt, Vector2 pivot)
    {
        rt.pivot = pivot;
        return rt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithSizeDelta(this RectTransform rt, Vector2 sizeDelta)
    {
        rt.sizeDelta = sizeDelta;
        return rt;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithOffsetMin(this RectTransform rt, Vector2 offsetMin)
    {
        rt.offsetMin = offsetMin;
        return rt;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectTransform WithOffsetMax(this RectTransform rt, Vector2 offsetMax)
    {
        rt.offsetMax = offsetMax;
        return rt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrAddComponent<T>(this Component component) where T : Component
    {
        return component.gameObject.GetOrAddComponent<T>();
    }
}
