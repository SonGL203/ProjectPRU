#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class WaveSetupEditor
{
    [MenuItem("Tools/Farm/4. Setup Wave System (scene dang mo)")]
    public static void Setup()
    {
        var gm = Ensure<GameManager>("GameManager");

        var day = Ensure<DayNightCycle>("DayNight");
        if (!day.globalLight) day.globalLight = Object.FindFirstObjectByType<Light2D>();
        if (!day.globalLight) Debug.LogWarning("Chua co Global Light 2D, hay tao roi keo vao DayNight.");

        var spawner = Ensure<WaveSpawner>("WaveSpawner");
        var e1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy1.prefab");
        var e2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy2.prefab");
        if (e1 && e2) spawner.enemyPrefabs = new[] { e1, e2 };
        else Debug.LogWarning("Chua co Assets/Prefabs/Enemy1, Enemy2. Chay menu 3. Build Enemy Animations truoc.");

        Ensure<Pathfinder>("Pathfinder");
        EnsureHouseStructure();

        if (!Object.FindFirstObjectByType<CropManager>())
            Debug.LogWarning("Scene chua co CropManager nen enemy se khong thay o dat co cay.");

        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        Debug.Log("Da dat xong GameManager, DayNight, WaveSpawner, Pathfinder, Structure. Bam Ctrl+S de luu scene.");
    }

    static T Ensure<T>(string name) where T : Component
    {
        var existing = Object.FindFirstObjectByType<T>();
        if (existing) return existing;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Setup " + name);
        return go.AddComponent<T>();
    }

    static void EnsureHouseStructure()
    {
        if (Object.FindFirstObjectByType<Structure>()) return;

        GameObject house = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.GetComponent<UnityEngine.Tilemaps.Tilemap>() || t.GetComponent<Grid>()) continue;
            if (t.name.ToLower().Contains("house")) { house = t.gameObject; break; }
        }
        if (!house) house = GameObject.Find("HomeBase");
        if (!house)
        {
            Debug.LogWarning("Khong tim thay object ngoi nha. Hay tu gan Structure vao ngoi nha.");
            return;
        }

        Undo.AddComponent<Structure>(house);
        var s = house.GetComponent<Structure>();
        s.order = 1;
        s.maxHealth = 300f;
        Debug.Log($"Da gan Structure vao '{house.name}'.");
    }
}
#endif
