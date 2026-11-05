using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string id;
    public bool isDefault;

    void Start()
    {
        bool match = string.IsNullOrEmpty(SceneTransition.NextSpawn) ? isDefault : SceneTransition.NextSpawn == id;
        if (!match) return;
        var player = FindFirstObjectByType<PlayerMove>();
        if (player) player.transform.position = transform.position;
        SceneTransition.NextSpawn = null;
    }
}
