using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour
{
    static readonly List<Structure> all = new();

    [Tooltip("Thu tu bi tan cong: 1 = Target 1, 2 = Target 2, ...")]
    public int order = 1;
    public float maxHealth = 200f;

    public float Health { get; private set; }
    public bool IsDestroyed { get; private set; }

    public static Structure Current
    {
        get
        {
            Structure best = null;
            foreach (var s in all)
                if (!s.IsDestroyed && (!best || s.order < best.order)) best = s;
            return best;
        }
    }

    static readonly List<Structure> everyone = new();

    public static void RestoreAll()
    {
        foreach (var s in everyone)
        {
            if (!s) continue;
            s.Health = s.maxHealth;
            s.IsDestroyed = false;
            s.gameObject.SetActive(true);
        }
    }

    void Awake()
    {
        Health = maxHealth;
        everyone.Add(this);
    }

    void OnDestroy() => everyone.Remove(this);
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    public void TakeDamage(float amount)
    {
        if (IsDestroyed) return;
        Health -= amount;
        if (Health > 0f) return;

        IsDestroyed = true;
        gameObject.SetActive(false);
    }
}
