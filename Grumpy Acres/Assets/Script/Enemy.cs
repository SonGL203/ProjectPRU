using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public float baseHealth = 30f;
    [Tooltip("Nhan voi mau tu WaveSpawner. 1 = binh thuong, 1.5 = trau hon (cowboy ban 15 dmg: 30hp = 2 phat, 45hp = 3 phat)")]
    public float healthMultiplier = 1f;
    public float speed = 1.5f;
    public float deathDelay = 1.2f;

    [Header("Pha cong trinh")]
    public float attackRange = 1.2f;
    public float attackDamage = 10f;
    public float attackInterval = 1f;
    [Tooltip("Player vao trong tam nay thi enemy bo muc tieu khac, quay sang danh player")]
    public float playerAggroRange = 1.5f;

    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public bool IsDead { get; private set; }

    public static readonly List<Enemy> Alive = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Alive.Clear();

    [Tooltip("Moi enemy dang danh cung muc tieu se day muc tieu do 'xa' them bay nhieu don vi khi chon, de enemy dan trai ra")]
    public float loadPenalty = 4f;
    object targetKey;
    float retargetTimer;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Transform player;
    PlayerHealth playerHealth;
    float attackTimer;
    float repathTimer;
    int pathIndex;
    readonly List<Vector2> path = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        MaxHealth = Health = baseHealth * healthMultiplier;
    }

    void OnEnable() => Alive.Add(this);
    void OnDisable()
    {
        Alive.Remove(this);
        if (playerHealth) playerHealth.Release(this);
    }

    void Start()
    {
        var p = FindFirstObjectByType<PlayerMove>();
        if (p)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    public void SetMaxHealth(float value)
    {
        MaxHealth = Health = value * healthMultiplier;
    }

    void FixedUpdate()
    {
        if (IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector3Int plotCell = default;
        Vector3 targetPos = default;
        bool inRange = playerHealth && !playerHealth.IsDead &&
                       ((Vector2)player.position - (Vector2)transform.position).magnitude <= playerAggroRange;
        bool hitPlayer = inRange && playerHealth.TryClaim(this);
        if (!hitPlayer && playerHealth) playerHealth.Release(this);
        retargetTimer -= Time.fixedDeltaTime;
        if (!hitPlayer && (retargetTimer <= 0f || !KeyValid(targetKey)))
        {
            retargetTimer = 1f;
            PickTarget();
        }
        var crops = CropManager.Instance;
        var cowboy = hitPlayer ? null : targetKey as Cowboy;
        var frame = hitPlayer ? null : targetKey as GuardFrame;
        bool hasPlot = !hitPlayer && targetKey is Vector3Int && crops && crops.soilTilemap;
        if (hasPlot)
        {
            plotCell = (Vector3Int)targetKey;
            targetPos = crops.soilTilemap.GetCellCenterWorld(plotCell);
        }
        var structure = (hitPlayer || cowboy || frame || hasPlot) ? null : Structure.Current;

        if (hitPlayer) targetPos = player.position;
        else if (cowboy) targetPos = cowboy.transform.position;
        else if (frame) targetPos = frame.ClosestPoint(transform.position);
        else if (!hasPlot)
        {
            if (structure)
            {
                var col = structure.GetComponent<Collider2D>();
                targetPos = col ? col.ClosestPoint(transform.position) : structure.transform.position;
            }
            else if (player) targetPos = player.position;
            else
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        bool attacking = hitPlayer || cowboy || frame || hasPlot || structure;
        Vector2 to = targetPos - transform.position;
        float stopAt = attacking ? attackRange : 0.8f;
        bool moving = to.magnitude > stopAt;

        Vector2 dir = moving ? (Steer(targetPos) - (Vector2)transform.position).normalized : Vector2.zero;
        if (sr && Mathf.Abs(dir.x) > 0.01f) sr.flipX = dir.x < 0f;
        rb.linearVelocity = dir * speed;
        if (anim) anim.SetFloat("Speed", moving ? 1f : 0f);

        attackTimer -= Time.fixedDeltaTime;
        if (!moving && attacking && attackTimer <= 0f)
        {
            attackTimer = attackInterval;
            if (anim) anim.SetTrigger("Attack");
            if (hitPlayer) playerHealth.TakeDamage(attackDamage);
            else if (cowboy) cowboy.TakeDamage(attackDamage);
            else if (frame) frame.TakeDamage(attackDamage);
            else if (hasPlot) CropManager.Instance.DamagePlot(plotCell, attackDamage);
            else structure.TakeDamage(attackDamage);
        }
    }

    static bool KeyValid(object key)
    {
        switch (key)
        {
            case Cowboy c: return c && c.Targetable;
            case GuardFrame f: return f && f.IsTargetable;
            case Vector3Int cell: return CropManager.Instance && CropManager.Instance.HasPlot(cell);
            default: return false;
        }
    }

    int Load(object key)
    {
        int n = 0;
        foreach (var e in Alive)
            if (e != this && !e.IsDead && key.Equals(e.targetKey)) n++;
        return n;
    }

    void PickTarget()
    {
        object best = null;
        float bestScore = float.MaxValue;
        Vector2 me = transform.position;

        void Consider(object key, Vector2 pos)
        {
            float s = (pos - me).magnitude + Load(key) * loadPenalty - (key.Equals(targetKey) ? 2f : 0f);
            if (s < bestScore) { bestScore = s; best = key; }
        }

        foreach (var c in Cowboy.Active)
            if (c && c.Targetable) Consider(c, c.transform.position);

        if (best == null)
            foreach (var f in GuardFrame.All)
                if (f && f.IsTargetable) Consider(f, f.transform.position);

        var crops = CropManager.Instance;
        if (best == null && crops && crops.soilTilemap)
            foreach (var cell in crops.PlotCells)
                Consider(cell, crops.soilTilemap.GetCellCenterWorld(cell));

        targetKey = best;
    }

    Vector2 Steer(Vector2 goal)
    {
        var pf = Pathfinder.Instance;
        if (!pf) return goal;

        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = 0.4f + Random.value * 0.2f;
            if (!pf.FindPath(transform.position, goal, path)) path.Clear();
            pathIndex = 0;
        }

        Vector2 pos = transform.position;
        while (pathIndex < path.Count && (path[pathIndex] - pos).sqrMagnitude < 0.04f) pathIndex++;
        return pathIndex < path.Count ? path[pathIndex] : goal;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health -= amount;
        if (Health <= 0f) { Die(); return; }
        if (anim) anim.SetTrigger("Hurt");
    }

    public void Vanish()
    {
        if (!IsDead) Die();
    }

    void Die()
    {
        IsDead = true;
        Alive.Remove(this);
        if (playerHealth) playerHealth.Release(this);
        rb.linearVelocity = Vector2.zero;
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        if (anim) anim.SetTrigger("Die");
        Destroy(gameObject, deathDelay);
    }
}
