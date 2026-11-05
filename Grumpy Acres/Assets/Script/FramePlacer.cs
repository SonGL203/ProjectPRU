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
    string hint;
    float hintUntil;

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
            if (!GuardFrameStock.Prefab)
            {
                var spot = FindFirstObjectByType<TowerSpot>();
                if (spot) GuardFrameStock.Prefab = spot.framePrefab;
            }
            return GuardFrameStock.Prefab;
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        var player = FindFirstObjectByType<PlayerMove>();
        var cam = Camera.main;

        if (mouse == null || !player || !cam || !GuardFrameStock.IsSelected() || !Prefab)
        {
            HideGhost();
            return;
        }

        lastActiveFrame = Time.frameCount;
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
            if (ok && GuardFrameStock.UseSelected()) Instantiate(Prefab, pos, Quaternion.identity);
            else if (!ok) Say("Khong dat duoc o day (qua xa hoac vuong vat can).");
        }
    }

    bool CanPlace(Vector3 pos, Vector3 playerPos)
    {
        if (((Vector2)pos - (Vector2)playerPos).magnitude > placeRange) return false;

        var hits = Physics2D.OverlapBoxAll((Vector2)pos + footprintOffset, footprint, 0f);
        foreach (var h in hits)
        {
            if (h.isTrigger) continue;
            if (h.GetComponentInParent<PlayerMove>() || h.GetComponentInParent<Enemy>() || h.GetComponentInParent<Cowboy>()) continue;
            if (ghost && h.transform.IsChildOf(ghost.transform)) continue;
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

    void Say(string t)
    {
        hint = t;
        hintUntil = Time.time + 2f;
    }

    void OnGUI()
    {
        if (!ghost) return;
        string text = "Click trai de dat khung canh gac (xanh = dat duoc)";
        if (Time.time < hintUntil) text += "\n" + hint;
        GUI.Label(new Rect(12, 32, 700, 48), text);
    }
}
