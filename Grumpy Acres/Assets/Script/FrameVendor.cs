using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FrameVendor : MonoBehaviour
{
    public string displayName = "Khung canh gac";
    public int cost = 80;

    bool playerInside;
    string message;
    float messageUntil;

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
        if (!playerInside) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !kb.fKey.wasPressedThisFrame) return;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < cost)
        {
            Say($"Khong du vang (can {cost}).");
            return;
        }
        if (!GuardFrameStock.TryAdd())
        {
            Say("Tui do day, khong mua them duoc.");
            return;
        }
        wallet.TrySpend(cost);
        Say("Da mua! Chon khung tren hotbar o nong trai, click de dat.");
    }

    void Say(string text)
    {
        message = text;
        messageUntil = Time.time + 2.5f;
    }

    void OnGUI()
    {
        string info = $"{displayName}  -  {cost} vang\nDat o bat ky cho nao tren nong trai (dang co: {GuardFrameStock.Count})";
        if (playerInside)
        {
            int gold = PlayerWallet.Instance ? PlayerWallet.Instance.gold : 0;
            info += $"\n[F] Mua   (vang: {gold})";
            if (Time.time < messageUntil) info += "\n" + message;
        }
        WorldPrompt.Draw(transform.position + Vector3.up * 2.6f, info, 280f);
    }
}
