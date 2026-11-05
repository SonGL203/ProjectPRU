using UnityEngine;

public static class WorldPrompt
{
    static GUIStyle style;

    public static void Draw(Vector3 worldPos, string text, float width = 320f)
    {
        var cam = Camera.main;
        if (!cam) return;

        if (style == null)
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };

        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z < 0f) return;
        float h = style.CalcHeight(new GUIContent(text), width);
        var rect = new Rect(sp.x - width * 0.5f, Screen.height - sp.y - h * 0.5f, width, h);

        GUI.color = Color.black;
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
        GUI.color = Color.white;
        GUI.Label(rect, text, style);
    }
}
