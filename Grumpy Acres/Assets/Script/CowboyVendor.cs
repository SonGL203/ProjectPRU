using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CowboyVendor : MonoBehaviour
{
    public int tier;
    public int cost = 50;
    public Cowboy cowboyPrefab;

    [Header("Giao dien (thiet ke san trong scene, code chi bat / tat)")]
    public InfoPanel panel;
    [Tooltip("Nhan ten + gia tren quay: an khi player cham vao, hien lai khi roi di. De trong = tu tim object NameLabel gan nhat")]
    public GameObject nameLabel;
    [Tooltip("Thong bao: het cho trong chong canh")]
    public AutoHide msgNoSlot;
    [Tooltip("Thong bao: khong du vang")]
    public AutoHide msgNoGold;
    [Tooltip("Thong bao: thue thanh cong")]
    public AutoHide msgHired;

    bool playerInside;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

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
        if (kb != null && kb.fKey.wasPressedThisFrame)
        {
            Debug.Log("[CowboyVendor] Bam F tai quay tier " + tier);
            TryHire();
        }
    }

    public bool TryHire()
    {
        if (CowboyRoster.Count >= GuardFrame.All.Count)
        {
            AutoHide.Flash(msgNoSlot);
            return false;
        }

        var wallet = PlayerWallet.Instance;
        if (!wallet || !wallet.TrySpend(cost))
        {
            AutoHide.Flash(msgNoGold);
            return false;
        }

        CowboyRoster.Add(tier);
        AutoHide.Flash(msgHired);
        return true;
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
        if (msgNoSlot) msgNoSlot.gameObject.SetActive(false);
        if (msgNoGold) msgNoGold.gameObject.SetActive(false);
        if (msgHired) msgHired.gameObject.SetActive(false);
    }
}
