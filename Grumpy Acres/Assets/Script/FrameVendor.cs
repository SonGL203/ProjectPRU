using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FrameVendor : MonoBehaviour
{
    public FrameKind kind = FrameKind.Wood;

    [Header("Giao dien (thiet ke san trong scene, code chi bat / tat)")]
    public InfoPanel panel;
    [Tooltip("Nhan ten + gia tren quay: an khi player cham vao, hien lai khi roi di. De trong = tu tim object NameLabel gan nhat")]
    public GameObject nameLabel;
    [Tooltip("Thong bao: khong du vang")]
    public AutoHide msgNoGold;
    [Tooltip("Thong bao: tui do day")]
    public AutoHide msgBagFull;
    [Tooltip("Thong bao: mua thanh cong")]
    public AutoHide msgBought;

    bool playerInside;

    int Cost => GuardFrameStock.Get(kind).cost;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Start()
    {
        if (!nameLabel) nameLabel = FindNameLabel();
        if (nameLabel) nameLabel.SetActive(true);
        if (panel) panel.Hide();
        HideMessages();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerMove>()) return;
        playerInside = true;
        if (panel) panel.Show();
        if (nameLabel) nameLabel.SetActive(false);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerMove>()) return;
        playerInside = false;
        if (panel) panel.Hide();
        if (nameLabel) nameLabel.SetActive(true);
        HideMessages();
    }

    void Update()
    {
        if (!playerInside) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !kb.fKey.wasPressedThisFrame) return;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < Cost) { AutoHide.Flash(msgNoGold); return; }
        if (!GuardFrameStock.TryAdd(kind)) { AutoHide.Flash(msgBagFull); return; }

        wallet.TrySpend(Cost);
        AutoHide.Flash(msgBought);
    }

    GameObject FindNameLabel()
    {
        var parent = transform.parent;
        if (!parent) return null;
        foreach (Transform c in parent)
            if (c.name == "NameLabel" && Mathf.Abs(c.localPosition.x - transform.localPosition.x) < 0.5f)
                return c.gameObject;
        return null;
    }

    void HideMessages()
    {
        if (msgNoGold) msgNoGold.gameObject.SetActive(false);
        if (msgBagFull) msgBagFull.gameObject.SetActive(false);
        if (msgBought) msgBought.gameObject.SetActive(false);
    }
}
