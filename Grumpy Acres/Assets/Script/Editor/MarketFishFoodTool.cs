#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tool tam: them dong "Thuc An Cho Ca" vao quay ca va doi phim mo quay tu E sang F. Chay xong tu xoa file nay.
public static class MarketFishFoodTool
{
    const string MarketScenePath = "Assets/Scenes/Market.unity";
    const string ToolPath = "Assets/Script/Editor/MarketFishFoodTool.cs";

    [MenuItem("Tools/Farm/Them thuc an ca va phim F Market (tam)")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Hay thoat Play mode truoc.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var market = EditorSceneManager.OpenScene(MarketScenePath, OpenSceneMode.Single);

        // Doi chu huong dan E -> F
        foreach (var tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tm.name == "StallHint") tm.text = "[F] Mua hàng";
        }
        foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.name == "Footer" && text.text.StartsWith("E / Esc")) text.text = "F / Esc: đóng cửa hàng";
        }

        // Them dong thuc an ca vao bang quay ca
        MarketTradeRow last = null;
        bool hasFood = false;
        foreach (var row in Object.FindObjectsByType<MarketTradeRow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (row.kind != TradeKind.FishItem) continue;
            if (row.fish == FishSlot.FishFood) hasFood = true;
            if (!last || row.transform.GetSiblingIndex() > last.transform.GetSiblingIndex()) last = row;
        }

        if (!last)
        {
            Debug.LogError("Khong thay quay ca (chua co dong ca nao).");
            return;
        }

        if (!hasFood)
        {
            var panel = (RectTransform)last.transform.parent;
            var clone = Object.Instantiate(last.gameObject, panel);
            int count = panel.GetComponentsInChildren<MarketTradeRow>(true).Length;
            clone.name = "Row_" + (count - 1);
            var cloneRect = (RectTransform)clone.transform;
            var lastRect = (RectTransform)last.transform;
            cloneRect.anchoredPosition = lastRect.anchoredPosition + new Vector2(0f, -60f);

            var cloneRow = clone.GetComponent<MarketTradeRow>();
            cloneRow.fish = FishSlot.FishFood;
            cloneRow.kind = TradeKind.FishItem;
            cloneRow.canBuy = true;
            cloneRow.canSell = false;
            cloneRow.stall = last.stall;
            EditorUtility.SetDirty(cloneRow);

            float height = 170f + count * 60f + 50f;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);
            var footer = panel.Find("Footer");
            if (footer)
            {
                var footerRect = (RectTransform)footer;
                footerRect.anchoredPosition = new Vector2(footerRect.anchoredPosition.x, -(height - 40f));
            }
        }

        EditorSceneManager.MarkSceneDirty(market);
        EditorSceneManager.SaveScene(market);
        Debug.Log("Da them thuc an ca va doi phim sang F. Tool tu xoa.");
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(ToolPath);
    }
}
#endif
