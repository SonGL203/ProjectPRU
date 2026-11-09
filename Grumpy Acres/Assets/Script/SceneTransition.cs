using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneTransition : MonoBehaviour
{
    public static string NextSpawn;

    public string targetScene;
    public string targetSpawn;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerMove>() || string.IsNullOrEmpty(targetScene)) return;
        NextSpawn = targetSpawn;
        SceneManager.sceneLoaded += OnSceneLoadedOnce;
        SceneManager.LoadScene(targetScene);
    }

    static void OnSceneLoadedOnce(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedOnce;

        var player = Object.FindFirstObjectByType<PlayerMove>();
        if (player == null) return;

        if (scene.name == "SampleScene")
        {
            // Vi tri an toan tro ve nong trai ngay phia duoi MarketPortal_Sell (24, 20.5)
            Vector3 returnPos = new Vector3(24f, 18.5f, 0f);
            player.transform.position = returnPos;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb) { rb.position = returnPos; rb.linearVelocity = Vector2.zero; }
        }
        else if (scene.name == "Market")
        {
            // Vi tri an toan khi vao cho (Spawn_FromFarm)
            Vector3 marketSpawn = new Vector3(11.385f, 2.475f, 0f);
            player.transform.position = marketSpawn;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb) { rb.position = marketSpawn; rb.linearVelocity = Vector2.zero; }
        }
    }
}
