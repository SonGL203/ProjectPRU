using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MarketTravel : MonoBehaviour
{
    public const string SceneName = "CowboyMarket";

    static MarketTravel inst;
    static Vector3 returnPos, camPos;
    static float camSize;

    public static bool InMarket { get; private set; }
    public static bool Busy { get; private set; }

    void Awake()
    {
        inst = this;
    }

    static MarketTravel Runner()
    {
        if (!inst) Debug.LogError("Scene chua co object nao gan component MarketTravel. Hay them no vao 1 object trong SampleScene.");
        return inst;
    }

    public static void Enter(Vector3 returnTo)
    {
        if (Busy || InMarket) return;
        var runner = Runner();
        if (!runner) return;
        runner.StartCoroutine(EnterRoutine(returnTo));
    }

    public static void Exit()
    {
        if (Busy || !InMarket) return;
        var runner = Runner();
        if (!runner) return;
        runner.StartCoroutine(ExitRoutine());
    }

    static IEnumerator EnterRoutine(Vector3 returnTo)
    {
        Busy = true;
        var op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        if (op == null) { Busy = false; yield break; }
        yield return op;

        var view = FindFirstObjectByType<MarketView>();
        var cam = Camera.main;
        if (!view || !cam)
        {
            Debug.LogError("Scene CowboyMarket thieu MarketView hoac scene khong co Main Camera.");
            Busy = false;
            yield break;
        }

        returnPos = returnTo;
        camPos = cam.transform.position;
        camSize = cam.orthographicSize;

        Teleport(view.spawn ? view.spawn.position : view.transform.position);
        cam.transform.position = new Vector3(view.transform.position.x, view.transform.position.y, camPos.z);
        cam.orthographicSize = view.orthoSize;

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
        }

        var scene = SceneManager.GetSceneByName(SceneName);
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
