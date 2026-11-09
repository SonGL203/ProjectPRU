#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tool tam, xoa file nay sau khi chay xong.
public static class GameOverPanelTool
{
    [MenuItem("Tools/Farm/Tao bang Game Over (tam)")]
    public static void Build()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        var canvasGo = GameObject.Find("Canvas");
        if (!gm || !canvasGo || !canvasGo.GetComponent<Canvas>())
        {
            Debug.LogError("Hay mo SampleScene (can co GameManager va Canvas) roi chay lai.");
            return;
        }

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t && t.name == "GameOverPanel") Object.DestroyImmediate(t.gameObject);
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var panel = NewRect("GameOverPanel", canvasGo.transform);
        Stretch(panel);
        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        var title = NewText("Title", panel, font, "GAME OVER", 72, new Color(0.9f, 0.2f, 0.2f), FontStyle.Bold);
        title.rectTransform.anchoredPosition = new Vector2(0f, 40f);

        var hint = NewText("Hint", panel, font, "Nhấn Enter để chơi lại", 28, Color.white, FontStyle.Normal);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -50f);

        panel.gameObject.transform.SetAsLastSibling();
        panel.gameObject.SetActive(false);

        gm.gameOverPanel = panel.gameObject;
        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        EditorSceneManager.SaveScene(gm.gameObject.scene);
        Selection.activeGameObject = panel.gameObject;
        Debug.Log("Da tao GameOverPanel va gan vao GameManager. Co the xoa GameOverPanelTool.cs.");
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    static Text NewText(string name, RectTransform parent, Font font, string text, int size, Color color, FontStyle style)
    {
        var r = NewRect(name, parent);
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(900f, 100f);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return t;
    }
}
#endif
