using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MarketPortal : MonoBehaviour
{
    public bool toMarket = true;
    [Tooltip("Cho player xuat hien khi tro ve nong trai (phai nam ngoai vung cua)")]
    public Transform returnPoint;

    [Tooltip("Chu hien tren cua")]
    public string label = "";

    bool playerInside;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) playerInside = false;
    }

    void Update()
    {
        if (!playerInside || MarketTravel.Busy) return;

        if (!toMarket) { MarketTravel.Exit(); return; }
        if (MarketTravel.InMarket || Enemy.Alive.Count > 0) return;

        var back = returnPoint ? returnPoint.position : transform.position + Vector3.down * 2f;
        MarketTravel.Enter(back);
    }

    void OnGUI()
    {
        if (toMarket && playerInside && Enemy.Alive.Count > 0)
        {
            WorldPrompt.Draw(transform.position + Vector3.up * 1.6f, "Dang co enemy, chua di cho duoc!");
            return;
        }
        if (!string.IsNullOrEmpty(label) && toMarket != MarketTravel.InMarket)
            WorldPrompt.Draw(transform.position + Vector3.up * 0.8f, label);
    }
}
