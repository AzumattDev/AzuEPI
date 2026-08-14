namespace AzuEPI.Core.Utilities.Extensions;

public static class RectTransformExtensions
{
    extension(RectTransform rt)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithAnchoredPosition(Vector2 position)
        {
            rt.anchoredPosition = position;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithAnchors(Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithAnchorMin(Vector2 min)
        {
            rt.anchorMin = min;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithAnchorMax(Vector2 max)
        {
            rt.anchorMax = max;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithPivot(Vector2 pivot)
        {
            rt.pivot = pivot;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithSizeDelta(Vector2 sizeDelta)
        {
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithOffsetMin(Vector2 offsetMin)
        {
            rt.offsetMin = offsetMin;
            return rt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RectTransform WithOffsetMax(Vector2 offsetMax)
        {
            rt.offsetMax = offsetMax;
            return rt;
        }
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