#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tool tam: tao bien "Market" (sang scene Market) bang cach nhan doi bien Cowboy Market. Xong thi xoa file nay.
public static class MarketSignTool
{
    static readonly Vector3 SignPos = new(24f, 20.5f, 0f);

    [MenuItem("Tools/Farm/Them bien Market (tam)")]
    public static void Build()
    {
        MarketPortal src = null;
        foreach (var p in Object.FindObjectsByType<MarketPortal>(FindObjectsSortMode.None))
            if (p.toMarket) { src = p; break; }
        if (!src)
        {
            Debug.LogError("Khong thay bien Cowboy Market. Hay mo SampleScene truoc.");
            return;
        }

        foreach (var n in new[] { "MarketPortal_Sell", "PortalLabel_Market" })
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t && t.name == n) Object.DestroyImmediate(t.gameObject);

        var parent = src.transform.parent;

        var sign = Object.Instantiate(src.gameObject, parent);
        sign.name = "MarketPortal_Sell";
        for (int i = sign.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(sign.transform.GetChild(i).gameObject);
        Object.DestroyImmediate(sign.GetComponent<MarketPortal>());
        sign.transform.position = SignPos;
        var st = sign.AddComponent<SceneTransition>();
        st.targetScene = "Market";
        st.targetSpawn = "";

        if (src.labelObj)
        {
            var label = Object.Instantiate(src.labelObj.gameObject, parent);
            label.name = "PortalLabel_Market";
            label.transform.position = src.labelObj.transform.position + (SignPos - src.transform.position);
            foreach (var tm in label.GetComponentsInChildren<TextMesh>(true)) tm.text = "Market";
            label.SetActive(true);
        }

        EditorSceneManager.MarkSceneDirty(sign.scene);
        EditorSceneManager.SaveScene(sign.scene);
        Selection.activeGameObject = sign;
        if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("Da tao MarketPortal_Sell tai " + SignPos + ". Co the xoa MarketSignTool.cs.");
    }
}
#endif
