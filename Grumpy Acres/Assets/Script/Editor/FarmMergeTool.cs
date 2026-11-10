using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FarmMergeTool
{
    [MenuItem("Tools/Farm/Merge Cow Farm Into Main")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Hay thoat Play mode truoc khi chay tool.");
            return;
        }

        GameObject entrance = FindInScene(scene, "CowFarmEntrance");
        GameObject bounds = FindInScene(scene, "CameraBounds_CowFarm");
        GameObject cowFarm = FindInScene(scene, "Cow Farm");
        GameObject farmGrid = FindInScene(scene, "FarmGrid");

        if (entrance != null)
        {
            Undo.DestroyObjectImmediate(entrance);
            Debug.Log("Da xoa CowFarmEntrance");
        }
        else
        {
            Debug.LogWarning("Khong thay CowFarmEntrance");
        }

        if (bounds != null)
        {
            Undo.DestroyObjectImmediate(bounds);
            Debug.Log("Da xoa CameraBounds_CowFarm");
        }
        else
        {
            Debug.LogWarning("Khong thay CameraBounds_CowFarm");
        }

        if (cowFarm != null && farmGrid != null)
        {
            Undo.SetTransformParent(cowFarm.transform, farmGrid.transform, "Parent Cow Farm");
            Debug.Log("Da dua Cow Farm vao FarmGrid");
        }
        else
        {
            Debug.LogWarning("Khong thay Cow Farm hoac FarmGrid");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Xong. Da luu scene " + scene.name);
    }

    static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName) return t.gameObject;
            }
        }
        return null;
    }
}
