using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Structure : MonoBehaviour
{
    public static Structure Current { get; private set; }

    public float maxHealth = 200f;
    public float Health { get; private set; }

    void Awake()
    {
        Current = this;
        Health = maxHealth;
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health > 0f) return;

        Current = null;
        gameObject.SetActive(false);
        GameManager.ManagerGame.GameOver();
    }
}
