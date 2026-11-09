using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Cong vao / ra scene "Market" (nap cong them, giu nguyen nguoi choi va tui do).
[RequireComponent(typeof(Collider2D))]
public class MarketMapPortal : MonoBehaviour
{
    [SerializeField] bool exit;
    [SerializeField] Transform returnPoint;

    const string MapScene = "Market";
    const string SpawnName = "Spawn_FromFarm";
    // Doi Market ra xa nong trai de hai ban do khong de len nhau
    static readonly Vector3 MapOffset = new Vector3(1000f, 0f, 0f);

    static MarketMapPortal entrance;
    static Vector3 playerReturn, cameraReturn;
    static float cameraSize;
    static bool busy;

    public static bool IsInMap
    {
        get { return entrance != null; }
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        entrance = null;
        busy = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerMove>();
        if (!player || busy || MarketTravel.Busy) return;

        if (exit)
        {
            if (entrance) entrance.StartCoroutine(entrance.Leave(player, gameObject.scene));
            return;
        }

        if (entrance || MarketTravel.InMarket || CowFarm.CowFarmPortal.IsInFarm) return;
        if (Enemy.Alive.Count > 0 || !Camera.main) return;
        if (SceneManager.GetSceneByName(MapScene).isLoaded || !Application.CanStreamedLevelBeLoaded(MapScene)) return;
        StartCoroutine(Enter(player));
    }

    IEnumerator Enter(PlayerMove player)
    {
        busy = true;
        var camera = Camera.main;
        cameraReturn = camera.transform.position;
        cameraSize = camera.orthographicSize;
        playerReturn = returnPoint ? returnPoint.position : transform.position + Vector3.down * 2.5f;

        yield return SceneManager.LoadSceneAsync(MapScene, LoadSceneMode.Additive);
        var scene = SceneManager.GetSceneByName(MapScene);

        Camera mapCamera = null;
        Transform spawn = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            root.transform.position += MapOffset;
        }
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!mapCamera) mapCamera = root.GetComponentInChildren<Camera>(true);
            if (!spawn) spawn = FindChild(root.transform, SpawnName);
        }

        if (!spawn || !camera || !player)
        {
            Debug.LogError("Scene Market thieu object " + SpawnName + ".");
            yield return SceneManager.UnloadSceneAsync(scene);
            busy = false;
            yield break;
        }

        Vector3 mapView = cameraReturn;
        float mapSize = cameraSize;
        if (mapCamera)
        {
            mapView = new Vector3(mapCamera.transform.position.x, mapCamera.transform.position.y, cameraReturn.z);
            mapSize = mapCamera.orthographicSize;
            mapCamera.enabled = false;
            var listener = mapCamera.GetComponent<AudioListener>();
            if (listener) listener.enabled = false;
        }

        entrance = this;
        Move(player, spawn.position);
        camera.transform.position = mapView;
        camera.orthographicSize = mapSize;
        busy = false;
    }

    IEnumerator Leave(PlayerMove player, Scene scene)
    {
        busy = true;
        Move(player, playerReturn);
        var camera = Camera.main;
        if (camera)
        {
            camera.transform.position = cameraReturn;
            camera.orthographicSize = cameraSize;
        }
        yield return SceneManager.UnloadSceneAsync(scene);
        entrance = null;
        busy = false;
    }

    static Transform FindChild(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindChild(parent.GetChild(i), childName);
            if (found) return found;
        }
        return null;
    }

    static void Move(PlayerMove player, Vector3 position)
    {
        player.transform.position = position;
        var body = player.GetComponent<Rigidbody2D>();
        if (body)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }
    }
}
