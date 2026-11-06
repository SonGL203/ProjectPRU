using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FramePlacer : MonoBehaviour
{
    public float placeRange = 8f;
    public Vector2 footprint = new(2.9f, 1.5f);
    public Vector2 footprintOffset = new(0f, 0.9f);

    static FramePlacer instance;
    static int lastActiveFrame = -1;
    GameObject ghost;
    SpriteRenderer[] ghostRenderers;
    string blocker;

    public static bool Active => instance && lastActiveFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance) return;
        var go = new GameObject("FramePlacer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FramePlacer>();
    }

    GameObject Prefab
    {
        get
        {
            if (!GuardFrameStock.Prefab) GuardFrameStock.Prefab = Resources.Load<GameObject>("GuardFrame");
            return GuardFrameStock.Prefab;
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        var player = FindFirstObjectByType<PlayerMove>();
        var cam = Camera.main;

        if (mouse == null || !player || !cam || GuardFrameStock.Selected() == null || !Prefab)
        {
            HideGhost();
            GameHud.ShowPlacing(false);
            return;
        }

        lastActiveFrame = Time.frameCount;
        GameHud.ShowPlacing(true);
        if (!ghost) CreateGhost();

        Vector3 w = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        var pos = new Vector3(Mathf.Round(w.x * 2f) / 2f, Mathf.Round(w.y * 2f) / 2f, 0f);
        bool ok = CanPlace(pos, player.transform.position);

        ghost.transform.position = pos;
        foreach (var r in ghostRenderers)
            r.color = ok ? new Color(0.5f, 1f, 0.5f, 0.6f) : new Color(1f, 0.4f, 0.4f, 0.6f);

        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (mouse.leftButton.wasPressedThisFrame && !overUi)
        {
            var kind = GuardFrameStock.Selected();
            if (ok && GuardFrameStock.UseSelected())
            {
                var placed = Instantiate(Prefab, pos, Quaternion.identity);
                var gf = placed.GetComponent<GuardFrame>();
                if (gf && kind != null) gf.ApplyKind(kind);
            }
            else if (!ok) { Debug.Log("Khong dat duoc khung: " + blocker); GameHud.Fail(); }
        }
    }

    bool CanPlace(Vector3 pos, Vector3 playerPos)
    {
        blocker = null;
        if (((Vector2)pos - (Vector2)playerPos).magnitude > placeRange) { blocker = "quá xa người chơi (> " + placeRange + ")"; return false; }

        var hits = Physics2D.OverlapBoxAll((Vector2)pos + footprintOffset, footprint, 0f);
        foreach (var h in hits)
        {
            if (h.isTrigger) continue;
            if (h.GetComponentInParent<PlayerMove>() || h.GetComponentInParent<Enemy>() || h.GetComponentInParent<Cowboy>()) continue;
            if (ghost && h.transform.IsChildOf(ghost.transform)) continue;
            blocker = "vướng: " + h.name;
            return false;
        }
        return true;
    }

    void CreateGhost()
    {
        ghost = Instantiate(Prefab);
        foreach (var c in ghost.GetComponentsInChildren<Collider2D>()) Destroy(c);
        foreach (var gf in ghost.GetComponentsInChildren<GuardFrame>()) Destroy(gf);
        ghostRenderers = ghost.GetComponentsInChildren<SpriteRenderer>();
    }

    void HideGhost()
    {
        if (ghost) Destroy(ghost);
        ghost = null;
    }

}
