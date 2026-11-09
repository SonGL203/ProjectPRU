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

    static Texture2D _whitePixel;

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
            ShowToast("Chưa gán dữ liệu hạt giống!", new Color(1f, 0.4f, 0.4f));
            return false;
        }

        int unitPrice = Price;
        int totalCost = unitPrice * count;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < totalCost)
        {
            ShowToast($"✕ Không đủ vàng! Cần {totalCost} vàng (Ví có: {(wallet ? wallet.gold : 0)}v)", new Color(1f, 0.35f, 0.35f));
            if (msgNoGold) AutoHide.Flash(msgNoGold);
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            ShowToast("Hệ thống túi đồ chưa sẵn sàng!", Color.yellow);
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
                ShowToast($"Túi chật! Chỉ mua được {added} {seedItem.itemName} (-{partialCost}v)", Color.yellow);
                if (msgBought) AutoHide.Flash(msgBought);
                return true;
            }
            ShowToast("✕ Túi đồ đã đầy! Hãy dọn bớt ô trống.", new Color(1f, 0.85f, 0.25f));
            if (msgBagFull) AutoHide.Flash(msgBagFull);
            return false;
        }

        wallet.TrySpend(totalCost);
        ShowToast($"✓ Đã mua x{count} {seedItem.itemName}! (-{totalCost} vàng)", new Color(0.35f, 1f, 0.5f));
        if (msgBought) AutoHide.Flash(msgBought);
        return true;
    }

    void ShowToast(string msg, Color col)
    {
        toastMessage = msg;
        toastColor = col;
        toastTimer = 2.8f;

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

    static void DrawSolidRect(Rect r, Color c)
    {
        if (_whitePixel == null)
        {
            _whitePixel = new Texture2D(1, 1);
            _whitePixel.SetPixel(0, 0, Color.white);
            _whitePixel.Apply();
        }
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, _whitePixel);
        GUI.color = old;
    }

    static void DrawPanel(Rect r, Color bg, Color border, float borderWidth = 2f)
    {
        // Shadow offset
        DrawSolidRect(new Rect(r.x + 3f, r.y + 3f, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));
        // Outer border
        DrawSolidRect(r, border);
        // Inner body
        DrawSolidRect(new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f), bg);
    }

    static void DrawShadowLabel(Rect r, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 2f)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(r, text, style);
        style.normal.textColor = old;
    }

    void OnGUI()
    {
        if (Camera.main == null) return;

        // Ty le phong to tu dong theo do phan giai man hinh (QHD 1440p = ~1.33x, 4K = ~2.0x)
        float scale = Mathf.Clamp(Screen.height / 1080f, 1.0f, 2.2f);

        // 1. Neu player khong o trong quay, hien thi bien hieu nho World-Space tren noc sap
        if (!playerInside)
        {
            Vector3 signWorld = transform.position + Vector3.up * 1.8f;
            Vector3 signScreen = Camera.main.WorldToScreenPoint(signWorld);
            if (signScreen.z > 0)
            {
                float sw = Mathf.Round(180f * scale);
                float sh = Mathf.Round(30f * scale);
                float sx = signScreen.x - sw * 0.5f;
                float sy = Screen.height - signScreen.y - sh * 0.5f;

                DrawPanel(new Rect(sx, sy, sw, sh), new Color(0.10f, 0.13f, 0.17f, 0.90f), new Color(0.95f, 0.75f, 0.25f, 0.85f), 1.5f * scale);

                var signStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(13 * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                string simpleName = seedItem ? seedItem.itemName : stallName;
                DrawShadowLabel(new Rect(sx, sy, sw, sh), $"🌱 {simpleName} ({Price}v)", signStyle, new Color(1f, 0.92f, 0.4f), Color.black, 1.5f);
            }
            return;
        }

        // 2. Player dang dung o quay: Hien thi hop thoai chi tiet, phong to ro net
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.9f);
        if (screen.z <= 0) return;

        float w = Mathf.Round(480f * scale);
        float h = Mathf.Round(200f * scale);
        float x = Mathf.Clamp(screen.x - w * 0.5f, 15f * scale, Screen.width - w - 15f * scale);
        float y = Mathf.Clamp(Screen.height - screen.y - h * 0.5f, 15f * scale, Screen.height - h - 15f * scale);

        // Khung nen toi sang trong voi vien vang
        Color bgCard = new Color(0.08f, 0.11f, 0.15f, 0.96f);
        Color borderGold = new Color(0.96f, 0.78f, 0.22f, 1.0f);
        float bw = Mathf.Round(2.5f * scale);
        DrawPanel(new Rect(x, y, w, h), bgCard, borderGold, bw);

        // Thanh tieu de o tren cung
        float headerH = Mathf.Round(38f * scale);
        DrawSolidRect(new Rect(x + bw, y + bw, w - bw * 2f, headerH), new Color(0.15f, 0.20f, 0.27f, 0.98f));
        DrawSolidRect(new Rect(x + bw, y + bw + headerH, w - bw * 2f, Mathf.Round(2f * scale)), new Color(0.96f, 0.78f, 0.22f, 0.6f));

        string itemName = seedItem ? seedItem.itemName : stallName;
        int unitPrice = Price;
        var wallet = PlayerWallet.Instance;
        int currentGold = wallet ? wallet.gold : 0;

        // Tieu de
        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(19 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(x, y + 4f * scale, w, headerH), $"🌱 SẠP HẠT GIỐNG: {itemName.ToUpper()}", headerStyle, new Color(1f, 0.88f, 0.25f), Color.black, 2f);

        // Hang 1: Gia ban va so vang trong vi
        var infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        float contentY = y + headerH + 10f * scale;
        float padX = 20f * scale;

        DrawShadowLabel(new Rect(x + padX, contentY, (w - padX * 2f) * 0.55f, 26f * scale),
            $"💰 Giá: {unitPrice} Vàng / hạt", infoStyle, new Color(1f, 0.95f, 0.7f), Color.black, 1.5f);

        var walletStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        DrawShadowLabel(new Rect(x + padX + (w - padX * 2f) * 0.5f, contentY, (w - padX * 2f) * 0.5f, 26f * scale),
            $"🪙 Ví bạn: {currentGold} Vàng", walletStyle, new Color(0.4f, 0.95f, 1f), Color.black, 1.5f);

        // Hang 2 & 3: Phim bam huong dan mua hat
        var actionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        float row1Y = contentY + 30f * scale;
        DrawSolidRect(new Rect(x + padX, row1Y, w - padX * 2f, 28f * scale), new Color(0.14f, 0.18f, 0.23f, 0.85f));
        DrawShadowLabel(new Rect(x + padX + 10f * scale, row1Y + 2f * scale, w - padX * 2f, 24f * scale),
            $"[ F ] hoặc [ 1 ]  :  Mua 1 Hạt  ({unitPrice} Vàng)", actionStyle, Color.white, Color.black, 1.5f);

        float row2Y = row1Y + 32f * scale;
        DrawSolidRect(new Rect(x + padX, row2Y, w - padX * 2f, 28f * scale), new Color(0.14f, 0.18f, 0.23f, 0.85f));
        DrawShadowLabel(new Rect(x + padX + 10f * scale, row2Y + 2f * scale, w - padX * 2f, 24f * scale),
            $"[ Shift + F ] hoặc [ 5 ]  :  Mua 5 Hạt  ({unitPrice * 5} Vàng)", actionStyle, new Color(1f, 0.95f, 0.5f), Color.black, 1.5f);

        // Hang thong bao ket qua giao dich (Toast)
        if (toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
        {
            float toastY = row2Y + 34f * scale;
            var toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            DrawSolidRect(new Rect(x + padX, toastY, w - padX * 2f, 26f * scale), new Color(0f, 0f, 0f, 0.7f));
            DrawShadowLabel(new Rect(x + padX, toastY, w - padX * 2f, 26f * scale), toastMessage, toastStyle, toastColor, Color.black, 1.5f);
        }
    }
}
