using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float respawnDelay = 3f;

    [Header("Thanh mau")]
    public Vector2 barOffset = new(0f, 1.1f);
    public Vector2 barSize = new(1.2f, 0.14f);

    [Header("Gioi han ke tan cong")]
    [Tooltip("So enemy toi da duoc danh player cung luc. Con du chuyen sang muc tieu khac")]
    public int maxAttackers = 2;

    public float Health { get; private set; }
    public bool IsDead { get; private set; }

    readonly System.Collections.Generic.HashSet<Enemy> attackers = new();

    public bool TryClaim(Enemy e)
    {
        if (attackers.Contains(e)) return true;
        attackers.RemoveWhere(x => !x || x.IsDead);
        if (attackers.Count >= maxAttackers) return false;
        attackers.Add(e);
        return true;
    }

    public void Release(Enemy e) => attackers.Remove(e);

    PlayerMove move;
    Rigidbody2D rb;
    Vector3 spawnPos;
    Transform fill;
    SpriteRenderer[] barRenderers;
    float respawnTimer;

    void Awake()
    {
        move = GetComponent<PlayerMove>();
        rb = GetComponent<Rigidbody2D>();
        Health = maxHealth;
        spawnPos = transform.position;
        BuildBar();
    }

    void Update()
    {
        if (!IsDead) return;
        respawnTimer -= Time.deltaTime;
        if (respawnTimer <= 0f) Respawn();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0f, Health - amount);
        RefreshBar();
        if (Health > 0f) return;

        IsDead = true;
        respawnTimer = respawnDelay;
        if (move) move.enabled = false;
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    void Respawn()
    {
        if (GameManager.HasInstance) GameManager.Instance.PlayerDefeated();

        IsDead = false;
        Health = maxHealth;
        transform.position = spawnPos;
        if (move) move.enabled = true;
        RefreshBar();
    }

    void BuildBar()
    {
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        var root = new GameObject("HealthBar").transform;
        root.SetParent(transform, false);
        var s = transform.lossyScale;
        root.localScale = new Vector3(1f / Mathf.Max(s.x, 0.0001f), 1f / Mathf.Max(s.y, 0.0001f), 1f);
        root.localPosition = new Vector3(
            (-barSize.x * 0.5f + barOffset.x) / Mathf.Max(s.x, 0.0001f),
            barOffset.y / Mathf.Max(s.y, 0.0001f), 0f);

        var back = MakeBar(root, "Back", sprite, new Color(0.15f, 0.15f, 0.15f, 0.9f), 20);
        back.localScale = new Vector3(barSize.x, barSize.y, 1f);

        fill = MakeBar(root, "Fill", sprite, new Color(0.85f, 0.15f, 0.15f, 1f), 21);
        fill.localScale = new Vector3(barSize.x, barSize.y, 1f);

        barRenderers = root.GetComponentsInChildren<SpriteRenderer>();
    }

    static Transform MakeBar(Transform parent, string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return go.transform;
    }

    void RefreshBar()
    {
        if (!fill) return;
        float t = Mathf.Clamp01(Health / maxHealth);
        fill.localScale = new Vector3(barSize.x * t, barSize.y, 1f);
        foreach (var r in barRenderers)
            if (r.transform == fill) r.color = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(0.3f, 0.85f, 0.3f), t);
    }
}
