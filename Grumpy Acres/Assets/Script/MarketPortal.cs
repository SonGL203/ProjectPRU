using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MarketPortal : MonoBehaviour
{
    public bool toMarket = true;
    [Tooltip("Cho player xuat hien khi tro ve nong trai (phai nam ngoai vung cua)")]
    public Transform returnPoint;

    [Tooltip("Chu hien tren cua")]
    public string label = "";

    [Tooltip("Nhan chu (object trong scene)")]
    public WorldLabel labelObj;
    [Tooltip("Bang canh bao khi con enemy, chua di cho duoc (tuy chon)")]
    public GameObject warningObj;

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

    void LateUpdate()
    {
        bool warn = toMarket && playerInside && Enemy.Alive.Count > 0;
        if (warningObj && warningObj.activeSelf != warn) warningObj.SetActive(warn);
        if (labelObj)
        {
            bool show = !warn && toMarket != MarketTravel.InMarket;
            if (labelObj.gameObject.activeSelf != show) labelObj.gameObject.SetActive(show);
        }
    }
}
