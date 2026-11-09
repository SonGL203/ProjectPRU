using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class Cowboy : MonoBehaviour
{
    static readonly System.Collections.Generic.List<Cowboy> all = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
    }

    void Vanish()
    {
        gameObject.SetActive(false);
    }

    public static System.Collections.Generic.List<Cowboy> Active
    {
        get { return all; }
    }

    [Header("May")]
    public float maxHealth = 60f;
    public float deathDelay = 1f;
    public float Health { get; private set; }
    public bool IsDead { get; private set; }
    public bool Targetable
    {
        get { return !IsDead && !hidden && (!frame || frame.IsBroken); }
    }

    [HideInInspector] public GuardFrame frame;
    [HideInInspector] public int rosterId;

    [Header("Chien dau")]
    public float bulletDamage = 15f;
    public float fireInterval = 1f;
    [Tooltip("Tam ban tu vi tri canh gac")]
    public float shootRange = 6f;

    [Header("Canh gac")]
    public float walkSpeed = 1.5f;
    [Tooltip("Cho dung canh ban dem. GuardFrame se gan khi cowboy vao khung")]
    public Transform guardPost;
    public float edgeMargin = 1.5f;

    Animator anim;
    SpriteRenderer sr;
    SpriteRenderer[] renderers;
    Vector3 edgePos;
    bool wasNight;
    bool hidden;
    float fireTimer;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Health = maxHealth;
    }

    void OnEnable()
    {
        all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health -= amount;
        if (Health > 0f) return;

        IsDead = true;
        CowboyRoster.Remove(rosterId);
        Invoke(nameof(Vanish), deathDelay);
    }

    void Start()
    {
        wasNight = false;
        SetHidden(true);
    }

    void Update()
    {
        if (IsDead) return;
        fireTimer -= Time.deltaTime;
        if (!guardPost) return;

        bool isNight = !DayNightCycle.Instance || DayNightCycle.Instance.IsNight;
        if (isNight != wasNight)
        {
            wasNight = isNight;
            edgePos = EdgePoint();
            if (isNight)
            {
                transform.position = guardPost.position;
                SetHidden(false);
            }
        }

        if (hidden) return;

        Vector3 dest = isNight ? guardPost.position : edgePos;
        bool walking = ((Vector2)dest - (Vector2)transform.position).magnitude > 0.1f;

        if (walking)
        {
            Face(dest.x - transform.position.x);
            transform.position = Vector3.MoveTowards(transform.position, dest, walkSpeed * Time.deltaTime);
        }
        else if (!isNight)
        {
            SetHidden(true);
            return;
        }
        anim.SetBool("Walk", walking);

        if (walking || !isNight) return;

        TryShoot();
    }

    void TryShoot()
    {
        var target = FindTarget();
        if (!target) return;

        Face(target.transform.position.x - transform.position.x);
        if (fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            anim.SetTrigger("Shoot");
            target.TakeDamage(bulletDamage);
        }
    }

    Vector3 EdgePoint()
    {
        var cam = Camera.main;
        if (!cam) return guardPost.position;
        float halfW = cam.orthographicSize * cam.aspect;
        bool left = guardPost.position.x < cam.transform.position.x;
        float x = cam.transform.position.x + (left ? -halfW - edgeMargin : halfW + edgeMargin);
        return new Vector3(x, guardPost.position.y, 0f);
    }

    void SetHidden(bool value)
    {
        hidden = value;
        foreach (var r in renderers) r.enabled = !value;
    }

    Enemy FindTarget()
    {
        Enemy best = null;
        float bestDistance = shootRange;
        foreach (var e in Enemy.Alive)
        {
            if (!e || e.IsDead) continue;
            float d = ((Vector2)e.transform.position - (Vector2)transform.position).magnitude;
            if (d < bestDistance) { bestDistance = d; best = e; }
        }
        return best;
    }

    void Face(float dx)
    {
        if (Mathf.Abs(dx) > 0.01f) sr.flipX = dx < 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(guardPost ? guardPost.position : transform.position, shootRange);
    }
}
