using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SeedVendor : MonoBehaviour
{
    [Header("Mat hang hat giong")]
    public ItemData seedItem;
    [Tooltip("Gia ban tuy chinh (de 0 = tu dong lay buyPrice tu seedItem)")]
    public int customPrice = 0;

    [Header("Ten quay hang")]
    public string stallName = "Sap Hat Giong";

    [Header("Giao dien tuy chon (neu co trong scene)")]
    public InfoPanel panel;
    public GameObject nameLabel;
    public AutoHide msgBought;
    public AutoHide msgNoGold;
    public AutoHide msgBagFull;

    bool playerInside;
    float toastTimer = 0f;
    string toastMessage = "";
    Color toastColor = Color.white;

    public int Price => (customPrice > 0) ? customPrice : (seedItem != null && seedItem.buyPrice > 0 ? seedItem.buyPrice : 10);

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        if (panel) panel.Hide();
        if (nameLabel) nameLabel.SetActive(true);
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
        if (toastTimer > 0f) toastTimer -= Time.deltaTime;

        if (!playerInside) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

        // Bam F de mua 1 hat, Shift + F de mua 5 hat
        if (kb.fKey.wasPressedThisFrame)
        {
            if (shift) BuySeeds(5);
            else BuySeeds(1);
        }
        // Hoac bam phim 1 mua 1 hat, phim 5 mua 5 hat
        else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            BuySeeds(1);
        }
        else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
        {
            BuySeeds(5);
        }
    }

    public bool BuySeeds(int count)
    {
        if (seedItem == null)
        {
            ShowToast("Chua gan du lieu hat giong!", Color.red);
            return false;
        }

        int unitPrice = Price;
        int totalCost = unitPrice * count;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < totalCost)
        {
            ShowToast($"Khong du vang! Can {totalCost}v (co {(wallet ? wallet.gold : 0)}v)", new Color(1f, 0.35f, 0.35f));
            if (msgNoGold) AutoHide.Flash(msgNoGold);
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            ShowToast("He thong tui do chua san sang!", Color.yellow);
            return false;
        }

        int leftover = InventorySystem.Instance.AddItem(seedItem, count);
        if (leftover > 0)
        {
            int added = count - leftover;
            if (added > 0)
            {
                int partialCost = unitPrice * added;
                wallet.TrySpend(partialCost);
                ShowToast($"Tui chat! Chi mua duoc {added} {seedItem.itemName} (-{partialCost}v)", Color.yellow);
                if (msgBought) AutoHide.Flash(msgBought);
                return true;
            }
            ShowToast("Tui do da day! Hay don bot do.", new Color(1f, 0.85f, 0.2f));
            if (msgBagFull) AutoHide.Flash(msgBagFull);
            return false;
        }

        wallet.TrySpend(totalCost);
        ShowToast($"Da mua x{count} {seedItem.itemName}! (-{totalCost} vang)", new Color(0.35f, 1f, 0.5f));
        if (msgBought) AutoHide.Flash(msgBought);
        return true;
    }

    void ShowToast(string msg, Color col)
    {
        toastMessage = msg;
        toastColor = col;
        toastTimer = 2.5f;

        if (WaterTilemapFishPond.Instance != null)
            WaterTilemapFishPond.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1.5f, msg, col);
    }

    void HideMessages()
    {
        if (msgBought) msgBought.gameObject.SetActive(false);
        if (msgNoGold) msgNoGold.gameObject.SetActive(false);
        if (msgBagFull) msgBagFull.gameObject.SetActive(false);
        toastTimer = 0f;
    }

    void OnGUI()
    {
        if (!playerInside || Camera.main == null) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.8f);
        if (screen.z <= 0) return;

        float w = 320f;
        float h = 135f;
        float x = Mathf.Clamp(screen.x - w * 0.5f, 10f, Screen.width - w - 10f);
        float y = Mathf.Clamp(Screen.height - screen.y - h * 0.5f, 10f, Screen.height - h - 10f);

        GUI.Box(new Rect(x, y, w, h), "");

        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(1f, 0.9f, 0.2f);

        string itemName = seedItem ? seedItem.itemName : stallName;
        int unitPrice = Price;
        var wallet = PlayerWallet.Instance;
        int currentGold = wallet ? wallet.gold : 0;

        GUI.Label(new Rect(x, y + 6, w, 22), $"🌱 {itemName.ToUpper()}", headerStyle);

        var bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.UpperLeft
        };
        bodyStyle.normal.textColor = Color.white;

        string info = $"• Gia ban: {unitPrice} vang / hat  (Vi: {currentGold} vang)\n" +
                      $"• [F] hoac [1]: Mua 1 hat ({unitPrice} vang)\n" +
                      $"• [Shift + F] hoac [5]: Mua 5 hat ({unitPrice * 5} vang)";
        GUI.Label(new Rect(x + 14, y + 32, w - 28, 60), info, bodyStyle);

        if (toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
        {
            var toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            toastStyle.normal.textColor = toastColor;
            GUI.Label(new Rect(x + 8, y + 96, w - 16, 30), toastMessage, toastStyle);
        }
    }
}
