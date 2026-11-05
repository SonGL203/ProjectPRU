using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TowerSpot : MonoBehaviour
{
    public GameObject framePrefab;
    public int cost = 80;

    public bool IsBuilt => built != null;

    GameObject built;
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
        if (IsBuilt || !playerInside) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame) TryBuild();
    }

    public bool TryBuild()
    {
        if (IsBuilt || !framePrefab) return false;
        var wallet = PlayerWallet.Instance;
        if (!wallet || !wallet.TrySpend(cost)) return false;

        built = Instantiate(framePrefab, transform.position, Quaternion.identity);
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        return true;
    }

    void OnGUI()
    {
        if (!playerInside || IsBuilt) return;
        int gold = PlayerWallet.Instance ? PlayerWallet.Instance.gold : 0;
        string t = gold >= cost ? $"[F] Xay khung canh gac ({cost} vang)" : $"Xay khung canh gac: can {cost} vang (co {gold})";
        WorldPrompt.Draw(transform.position + Vector3.up * 1.2f, t);
    }
}
