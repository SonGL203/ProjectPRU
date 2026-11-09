using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupMarketStalls : MonoBehaviour
{
    [MenuItem("Tools/Farm/Cai dat cho Market va Sap hat giong")]
    public static void Setup()
    {
        var marketScene = EditorSceneManager.OpenScene("Assets/Scenes/Market.unity", OpenSceneMode.Single);

        MarketStandaloneSupport.SetupMarketAtRuntime();

        EditorSceneManager.MarkSceneDirty(marketScene);
        EditorSceneManager.SaveScene(marketScene);

        Debug.Log(">>> [SetupMarketStalls] Da cai dat thanh cong 3 sap ban hat giong (Ca chua, Ca rot, Lua mi) trong scene Market!");
    }
}
