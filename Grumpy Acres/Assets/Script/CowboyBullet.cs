using UnityEngine;

public class CowboyBullet : MonoBehaviour
{
    public float speed = 12f;
    public float hitRadius = 0.3f;
    public float lifetime = 2f;

    float damage;
    Enemy target;
    Vector2 dir;

    public void Init(Enemy target, float damage)
    {
        this.target = target;
        this.damage = damage;
        dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (target && !target.IsDead)
            dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        if (!target || target.IsDead) return;
        if (((Vector2)target.transform.position - (Vector2)transform.position).sqrMagnitude <= hitRadius * hitRadius)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
