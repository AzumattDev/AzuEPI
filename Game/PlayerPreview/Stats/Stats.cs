namespace AzuEPI.Game.PlayerPreview.Stats;

public class StatsUI
{
    public static RectTransform CreateUI(InventoryGui inventoryGui, RectTransform parentRect)
    {
        RectTransform? statsRect = new GameObject("StatsUI", typeof(RectTransform)).GetComponent<RectTransform>();
        statsRect.SetParent(parentRect);
        statsRect.anchorMin = new Vector2(0, 0);
        statsRect.anchorMax = new Vector2(1, 0.15f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;
        statsRect.localScale = Vector3.one;

        GridLayoutGroup? grid = new GameObject("StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<GridLayoutGroup>();
        RectTransform? gridRect = grid.GetComponent<RectTransform>();
        gridRect.SetParent(statsRect);
        gridRect.anchorMin = new Vector2(0, 0);
        gridRect.anchorMax = new Vector2(1, 1);
        gridRect.offsetMin = new Vector2(10, 10);
        gridRect.offsetMax = new Vector2(-10, -10);
        gridRect.localScale = Vector3.one;

        GameObject? prefab = inventoryGui.m_recipeElementPrefab;
        RectTransform? prefabRect = prefab.GetComponent<RectTransform>();
        Vector2 cellSize  = new Vector2(prefabRect.sizeDelta.x, prefabRect.sizeDelta.y);
        grid.cellSize = cellSize;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.spacing = new Vector2(3, 3);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 6; i++)
        {
            GameObject? statElement = GameObject.Instantiate(prefab, gridRect);
            statElement.name = "StatElement" + i;
            statElement.SetActive(true);
            RectTransform? statRect = statElement.GetComponent<RectTransform>();
            GameObject selected = statRect.Find("selected").gameObject;
            selected.SetActive(true);
            selected.GetComponent<Image>().color = new Color(0, 0, 0, .565f);
            Transform? durability = statRect.Find("Durability");
            if (durability != null) GameObject.Destroy(durability.gameObject);
            Transform? quality = statRect.Find("QualityLevel");
            if (quality != null) GameObject.Destroy(quality.gameObject);
        }

        return statsRect;
    }
}