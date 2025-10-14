namespace AzuEPI.Game.PlayerPreview.Stats;

public class StatsUI
{
    public static RectTransform CreateUI(InventoryGui inventoryGui, RectTransform parentRect)
    {
        var statsRect = new GameObject("StatsUI", typeof(RectTransform)).GetComponent<RectTransform>();
        statsRect.SetParent(parentRect);
        statsRect.anchorMin = new Vector2(0, 0);
        statsRect.anchorMax = new Vector2(1, 0.15f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;
        statsRect.localScale = Vector3.one;

        var grid = new GameObject("StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<GridLayoutGroup>();
        var gridRect = grid.GetComponent<RectTransform>();
        gridRect.SetParent(statsRect);
        gridRect.anchorMin = new Vector2(0, 0);
        gridRect.anchorMax = new Vector2(1, 1);
        gridRect.offsetMin = new Vector2(10, 10);
        gridRect.offsetMax = new Vector2(-10, -10);
        gridRect.localScale = Vector3.one;

        var prefab = inventoryGui.m_recipeElementPrefab;
        var prefabRect = prefab.GetComponent<RectTransform>();
        var cellSize  = new Vector2(prefabRect.sizeDelta.x, prefabRect.sizeDelta.y);
        grid.cellSize = cellSize;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.spacing = new Vector2(3, 3);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 6; i++)
        {
            var statElement = GameObject.Instantiate(prefab, gridRect);
            statElement.name = "StatElement" + i;
            statElement.SetActive(true);
            var statRect = statElement.GetComponent<RectTransform>();
            var selected = statRect.Find("selected").gameObject;
            selected.SetActive(true);
            selected.GetComponent<Image>().color = new Color(0, 0, 0, .565f);
            var durability = statRect.Find("Durability");
            if (durability != null) GameObject.Destroy(durability.gameObject);
            var quality = statRect.Find("QualityLevel");
            if (quality != null) GameObject.Destroy(quality.gameObject);
        }

        return statsRect;
    }
}