using UnityEngine;

// Gan tren ngoi nha thu mua: nguoi choi cham vao nha (Rigidbody2D + BoxCollider2D) thi mo bang ban hang.
public class MarketSellTrigger : MonoBehaviour
{
    [SerializeField] MarketSellShop shop;

    void OnCollisionEnter2D(Collision2D collision)
    {
        Touch(collision.collider, true);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Touch(collision.collider, true);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        Touch(collision.collider, false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Touch(other, true);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Touch(other, true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Touch(other, false);
    }

    void Touch(Collider2D other, bool value)
    {
        if (shop && other.GetComponentInParent<PlayerMove>()) shop.SetTouching(value);
    }

    void OnDisable()
    {
        if (shop) shop.SetTouching(false);
    }
}
