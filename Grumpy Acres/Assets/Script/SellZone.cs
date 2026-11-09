using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SellZone : MonoBehaviour
{
    public string zoneName = "Sap cho";

    public bool PlayerInside { get; private set; }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) PlayerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) PlayerInside = false;
    }
}
