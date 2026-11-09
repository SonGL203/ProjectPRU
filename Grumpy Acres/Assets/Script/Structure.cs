using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour
{
    static readonly List<Structure> all = new();

    [Tooltip("Thu tu bi tan cong: 1 = Target 1, 2 = Target 2, ...")]
    public int order = 1;
    public float maxHealth = 200f;

    public float Health { get;  set; }
    public bool IsDestroyed { get;  set; }

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

    void Awake()
    {
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
        if (IsDestroyed) return;
        Health -= amount;
        if (Health > 0f) return;

        IsDestroyed = true;
        gameObject.SetActive(false);

        if (Current == null) GameManager.ManagerGame.GameOver();
    }
}
