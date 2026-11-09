using System.Collections.Generic;
using UnityEngine;

public class GuardFrame : MonoBehaviour
{
    public static readonly List<GuardFrame> All = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        All.Clear();
    }

    public float maxHealth = 80f;
    [Tooltip("Cho cowboy dung ben trong khung")]
    public Transform post;
    public SpriteRenderer back;
    public SpriteRenderer front;
    public Sprite brokenSprite;
    public Collider2D fence;
    [Tooltip("Prefab cowboy theo cap: 0 = trang, 1 = vang, 2 = manh nhat")]
    public Cowboy[] cowboyPrefabs;

    public float Health { get; private set; }
    public bool IsBroken { get; private set; }
    public bool Occupied
    {
        get { return occupant && !occupant.IsDead; }
    }
    public bool IsTargetable
    {
        get { return Occupied && !IsBroken; }
    }

    Cowboy occupant;
    Sprite intactSprite;

    public static GuardFrame Nearest(Vector3 from)
    {
        GuardFrame best = null;
        float bestSqr = float.MaxValue;
        foreach (var f in All)
        {
            if (!f.IsTargetable) continue;
            float d = (f.transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = f; }
        }
        return best;
    }

    public Vector3 ClosestPoint(Vector3 from)
    {
        return fence ? (Vector3)fence.ClosestPoint(from) : transform.position;
    }

    public void ApplyKind(GuardFrameStock.Kind kind)
    {
        maxHealth = kind.maxHealth;
        Health = maxHealth;
        if (back) back.color = kind.tint;
        if (front) front.color = kind.tint;
    }

    void Awake()
    {
        Health = maxHealth;
        if (back) intactSprite = back.sprite;
    }

    void OnEnable()
    {
        All.Add(this);
        GameManager.ManagerGame.OnNewDay += HandleNewDay;
    }

    void OnDisable()
    {
        All.Remove(this);
        if (GameManager.HasInstance) GameManager.ManagerGame.OnNewDay -= HandleNewDay;
    }

    void HandleNewDay(WeekDay day, int week)
    {
        Repair();
    }

    void Update()
    {
        if (!Occupied) TryReceiveCowboy();
    }

    void TryReceiveCowboy()
    {
        if (CowboyRoster.Count == 0 || cowboyPrefabs == null || cowboyPrefabs.Length == 0 || !post) return;
        var entry = CowboyRoster.TakeUnplaced();
        if (entry == null) return;

        var prefab = cowboyPrefabs[Mathf.Clamp(entry.tier, 0, cowboyPrefabs.Length - 1)];
        var c = Instantiate(prefab, post.position, Quaternion.identity);
        c.guardPost = post;
        c.frame = this;
        c.rosterId = entry.id;
        occupant = c;
    }

    public void TakeDamage(float amount)
    {
        if (IsBroken) return;
        Health -= amount;
        if (Health > 0f) return;

        IsBroken = true;
        if (front) front.enabled = false;
        if (back && brokenSprite) back.sprite = brokenSprite;
        if (fence) fence.enabled = false;
    }

    public void Repair()
    {
        Health = maxHealth;
        IsBroken = false;
        if (front) front.enabled = true;
        if (back && intactSprite) back.sprite = intactSprite;
        if (fence) fence.enabled = true;
    }
}
