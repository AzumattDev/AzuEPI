namespace AzuEPI.Core.Utilities.Extensions;

public static class GameObjectHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject CreateWithComponents(string name, params Type[] components)
    {
        return new GameObject(name, components);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject CreateUIObject(string name, params Type[] additionalComponents)
    {
        Type[] allComponents = new Type[additionalComponents.Length + 1];
        allComponents[0] = typeof(RectTransform);
        Array.Copy(additionalComponents, 0, allComponents, 1, additionalComponents.Length);
        return new GameObject(name, allComponents);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject WithActive(this GameObject go, bool active)
    {
        go.SetActive(active);
        return go;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject WithParent(this GameObject go, Transform parent, bool worldPositionStays = true)
    {
        go.transform.SetParent(parent, worldPositionStays);
        return go;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject WithSiblingIndex(this GameObject go, int index)
    {
        go.transform.SetSiblingIndex(index);
        return go;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SafeSetActive(this GameObject go, bool active)
    {
        if (go) go.SetActive(active);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SafeSetActive(this Transform transform, bool active)
    {
        if (transform) transform.gameObject.SetActive(active);
    }

    public static void SetLayerForEntireHierarchy(this GameObject gameObject, int layer, int depth = 0)
    {
        if (depth >= 50)
        {
            return;
        }

        gameObject.layer = layer;

        foreach (Transform child in gameObject.transform)
        {
            SetLayerForEntireHierarchy(child.gameObject, layer, depth + 1);
        }
    }

    public static bool HasChildWithNameThatContains(this GameObject gameObject, string name)
    {
        List<Transform> children = gameObject.GetAllChildTransforms();

        return children.Any(child => child.name.Contains(name));
    }

    public static List<Transform> GetAllChildTransforms(this GameObject gameObject)
    {
        return _GetAllChildTransforms(gameObject, true);
    }

    private static List<Transform> _GetAllChildTransforms(GameObject gameObject, bool isRoot = false, List<Transform>? transforms = null)
    {
        transforms = transforms ?? [];

        if (!isRoot)
        {
            transforms.Add(gameObject.transform);
        }

        for (int i = 0; i < gameObject.transform.childCount; ++i)
        {
            _GetAllChildTransforms(gameObject.transform.GetChild(i).gameObject, transforms: transforms);
        }

        return transforms;
    }
}