using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SeedVendor : MonoBehaviour
{
    [Header("Hat giong")]
    public ItemData seed;
    [Min(1)]
    public int amount = 1;
    [Tooltip("Gia mua: de -1 se tu dong lay theo gia buyPrice cua hat giong")]
    public int customPrice = -1;

    [Header("Giao dien (thiet ke san trong scene, code chi bat / tat)")]
    public InfoPanel panel;
    [Tooltip("Nhan ten + gia tren quay")]
    public GameObject nameLabel;
    [Tooltip("Thong bao: khong du vang")]
    public AutoHide msgNoGold;
    [Tooltip("Thong bao: tui do day")]
    public AutoHide msgBagFull;
    [Tooltip("Thong bao: mua thanh cong")]
    public AutoHide msgBought;

    bool playerInside;
    TextMesh labelTm;
    string defaultLabelText;
    float feedbackTimer;

    int UnitCost => customPrice >= 0 ? customPrice : (seed ? seed.buyPrice : 10);

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Start()
    {
        if (!nameLabel) nameLabel = FindNameLabel();
        if (nameLabel)
        {
            nameLabel.SetActive(true);
            labelTm = nameLabel.GetComponentInChildren<TextMesh>();
            if (labelTm) defaultLabelText = labelTm.text;
        }
        if (panel) panel.Hide();
        HideMessages();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerMove>()) return;
        playerInside = true;
        if (panel)
        {
            panel.Show();
            if (nameLabel) nameLabel.SetActive(false);
        }
        else if (nameLabel)
        {
            nameLabel.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerMove>()) return;
        playerInside = false;
        if (panel) panel.Hide();
        if (nameLabel)
        {
            nameLabel.SetActive(true);
            if (labelTm && !string.IsNullOrEmpty(defaultLabelText))
                labelTm.text = defaultLabelText;
        }
        HideMessages();
    }

    void Update()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f && labelTm && !string.IsNullOrEmpty(defaultLabelText))
                labelTm.text = defaultLabelText;
        }

        if (!playerInside || !seed) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !kb.fKey.wasPressedThisFrame) return;

        // Giu Shift de mua nhanh x5 goi
        int buyCount = amount;
        if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
            buyCount *= 5;

        int totalCost = UnitCost * buyCount;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < totalCost)
        {
            FlashFeedback(msgNoGold, "<color=red>Khong du vang!</color>");
            return;
        }

        var inv = InventorySystem.Instance;
        if (!inv) return;

        // Them vao tui, ham AddItem tra ve so luong khong nhat duoc neu tui day
        int remaining = inv.AddItem(seed, buyCount);
        if (remaining == buyCount)
        {
            FlashFeedback(msgBagFull, "<color=red>Tui do day!</color>");
            return;
        }

        int actualBought = buyCount - remaining;
        int actualCost = UnitCost * actualBought;

        wallet.TrySpend(actualCost);
        FlashFeedback(msgBought, $"<color=green>Da mua x{actualBought}!</color>");
    }

    void FlashFeedback(AutoHide msg, string fallbackText)
    {
        if (msg)
        {
            AutoHide.Flash(msg);
        }
        else if (labelTm)
        {
            labelTm.text = fallbackText;
            feedbackTimer = 1.2f;
        }
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
