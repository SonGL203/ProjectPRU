using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneTransition : MonoBehaviour
{
    public static string NextSpawn;

    public string targetScene;
    public string targetSpawn;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerMove>() || string.IsNullOrEmpty(targetScene)) return;
        NextSpawn = targetSpawn;
        SceneManager.LoadScene(targetScene);
    }
}
