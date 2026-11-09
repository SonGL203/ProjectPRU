using UnityEngine;

public class Cowboy : MonoBehaviour
{
    static readonly System.Collections.Generic.List<Cowboy> all = new();
    static readonly System.Collections.Generic.List<Cowboy> everyone = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
        everyone.Clear();
    }

    public static void RemoveAll()
    {
        foreach (var c in everyone.ToArray())
            if (c) Destroy(c.gameObject);
        everyone.Clear();
    }

    void OnDestroy()
    {
        everyone.Remove(this);
    }

    void Vanish()
    {
        gameObject.SetActive(false);
    }

    public static System.Collections.Generic.List<Cowboy> Active
    {
        get { return all; }
    }

    public static Cowboy Nearest(Vector3 from)
    {
        Cowboy best = null;
        float bestSqr = float.MaxValue;
        foreach (var c in all)
        {
            if (!c.Targetable) continue;
            float d = (c.transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c; }
        }
        return best;
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
    public CowboyBullet bulletPrefab;
    public Transform muzzle;

    [Header("Canh gac")]
    [Tooltip("Tam thoi: dung san tai cho dat trong scene, ban ca ngay lan dem, khong can khung rao")]
    public bool standAtPlace = true;
    public float walkSpeed = 1.5f;
    [Tooltip("Cho dung canh ban dem. GuardFrame se gan khi cowboy vao khung")]
    public Transform guardPost;
    public float edgeMargin = 1.5f;

    Animator anim;
    SpriteRenderer sr;
    SpriteRenderer[] renderers;
    Vector3 edgePos;
    bool wasOnDuty;
    bool hidden;
    float fireTimer;

    [Tooltip("true = canh gac ca ngay lan dem (vua mua xong la di vao khung); false = chi ban dem")]
    public bool alwaysOnDuty = false;

    bool OnDuty
    {
        get { return alwaysOnDuty || !DayNightCycle.Instance || DayNightCycle.Instance.IsNight; }
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Health = maxHealth;
        everyone.Add(this);
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
        if (Health > 0f) { SetTrigger("Hurt"); return; }

        IsDead = true;
        CowboyRoster.Remove(rosterId);
        SetTrigger("Die");
        Invoke(nameof(Vanish), deathDelay);
    }

    void Start()
    {
        if (standAtPlace) return;
        wasOnDuty = false;
        SetHidden(true);
    }

    void Update()
    {
        if (IsDead) return;
        fireTimer -= Time.deltaTime;

        if (standAtPlace)
        {
            TryShoot();
            return;
        }
        if (!guardPost) return;

        bool duty = OnDuty;
        if (duty != wasOnDuty)
        {
            wasOnDuty = duty;
            edgePos = EdgePoint();
            if (duty)
            {
                transform.position = guardPost.position;
                SetHidden(false);
            }
        }

        if (hidden) return;

        Vector3 dest = duty ? guardPost.position : edgePos;
        bool walking = ((Vector2)dest - (Vector2)transform.position).sqrMagnitude > 0.01f;

        if (walking)
        {
            Face(dest.x - transform.position.x);
            transform.position = Vector3.MoveTowards(transform.position, dest, walkSpeed * Time.deltaTime);
        }
        else if (!duty)
        {
            SetHidden(true);
            return;
        }
        SetBool("Walk", walking);

        if (walking || !duty) return;

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
            Shoot(target);
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
        float bestSqr = shootRange * shootRange;
        foreach (var e in Enemy.Alive)
        {
            if (!e || e.IsDead) continue;
            float d = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

    void Shoot(Enemy target)
    {
        SetTrigger("Shoot");

        if (!bulletPrefab) { target.TakeDamage(bulletDamage); return; }
        var from = muzzle ? muzzle.position : transform.position;
        var b = Instantiate(bulletPrefab, from, Quaternion.identity);
        b.Init(target, bulletDamage);
    }

    void Face(float dx)
    {
        if (sr && Mathf.Abs(dx) > 0.01f) sr.flipX = dx < 0f;
    }

    void SetBool(string name, bool v)
    {
        if (HasParam(name, AnimatorControllerParameterType.Bool)) anim.SetBool(name, v);
    }

    void SetTrigger(string name)
    {
        if (HasParam(name, AnimatorControllerParameterType.Trigger)) anim.SetTrigger(name);
    }

    bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (!anim || !anim.runtimeAnimatorController) return false;
        foreach (var p in anim.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(guardPost ? guardPost.position : transform.position, shootRange);
    }
}
