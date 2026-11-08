using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MarketTravel : MonoBehaviour
{
    static MarketTravel inst;
    static Vector3 returnPos, camPos;
    static float camSize;
    static Color camBg;
    static string currentMarketScene = "CowboyMarket";

    public static bool InMarket { get; private set; }
    public static bool Busy { get; private set; }

    static MarketTravel Runner()
    {
        if (!inst) inst = new GameObject("MarketTravel").AddComponent<MarketTravel>();
        return inst;
    }

    public static void Enter(Vector3 returnTo) => Enter("CowboyMarket", returnTo);

    public static void Enter(string sceneName, Vector3 returnTo)
    {
        if (Busy || InMarket) return;
        currentMarketScene = string.IsNullOrEmpty(sceneName) ? "CowboyMarket" : sceneName;
        Runner().StartCoroutine(EnterRoutine(currentMarketScene, returnTo));
    }

    public static void Exit()
    {
        if (Busy || !InMarket) return;
        Runner().StartCoroutine(ExitRoutine());
    }

    static IEnumerator EnterRoutine(string sceneName, Vector3 returnTo)
    {
        Busy = true;
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null) { Busy = false; yield break; }
        yield return op;

        MarketView view = null;
        var loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.isLoaded)
        {
            foreach (var rootGo in loadedScene.GetRootGameObjects())
            {
                view = rootGo.GetComponentInChildren<MarketView>(true);
                if (view) break;
            }
        }
        if (!view) view = FindFirstObjectByType<MarketView>();

        var cam = Camera.main;
        if (!view || !cam)
        {
            Debug.LogError($"Scene {sceneName} thieu MarketView hoac scene khong co Main Camera.");
            Busy = false;
            yield break;
        }

        returnPos = returnTo;
        camPos = cam.transform.position;
        camSize = cam.orthographicSize;
        camBg = cam.backgroundColor;

        Teleport(view.spawn ? view.spawn.position : view.transform.position);
        cam.transform.position = new Vector3(view.transform.position.x, view.transform.position.y, camPos.z);
        cam.orthographicSize = view.orthoSize;
        cam.backgroundColor = new Color(0.355f, 0.655f, 0.255f, 1f);

        // Dam bao nen co xanh phu kin khong bao gio lo khoang xanh duong
        if (!GameObject.Find("Background_Grass"))
        {
            var bgObj = new GameObject("Background_Grass");
            bgObj.transform.position = new Vector3(view.transform.position.x, view.transform.position.y, 0f);
            bgObj.transform.localScale = new Vector3(100f, 80f, 1f);
            var sr = bgObj.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(2, 2);
            var green = new Color(0.355f, 0.655f, 0.255f, 1f);
            tex.SetPixels(new Color[] { green, green, green, green });
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -100;
        }

        // Nap toan bo canh quan chi tiet 100% giong Farm vao Market
        MarketStandaloneSupport.SetupMarketAtRuntime();

        InMarket = true;
        Busy = false;
    }

    static IEnumerator ExitRoutine()
    {
        Busy = true;
        Teleport(returnPos);
        var cam = Camera.main;
        if (cam)
        {
            cam.transform.position = camPos;
            cam.orthographicSize = camSize;
            cam.backgroundColor = camBg;
        }

        var scene = SceneManager.GetSceneByName(currentMarketScene);
        if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);

        InMarket = false;
        Busy = false;
    }

    static void Teleport(Vector3 pos)
    {
        var player = FindFirstObjectByType<PlayerMove>();
        if (!player) return;
        player.transform.position = pos;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.position = pos;
            rb.linearVelocity = Vector2.zero;
        }
    }

    void OnDestroy()
    {
        InMarket = false;
        Busy = false;
        inst = null;
    }
}
