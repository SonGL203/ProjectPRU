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
            ShowToast("CHƯA GÁN DỮ LIỆU HẠT GIỐNG!", new Color(1f, 0.4f, 0.4f));
            return false;
        }

        int unitPrice = Price;
        int totalCost = unitPrice * count;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < totalCost)
        {
            ShowToast($"✕ KHÔNG ĐỦ VÀNG! Cần {totalCost}v (Ví có: {(wallet ? wallet.gold : 0)}v)", new Color(1f, 0.35f, 0.35f));
            if (msgNoGold) AutoHide.Flash(msgNoGold);
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            ShowToast("HỆ THỐNG TÚI ĐỒ CHƯA SẴN SÀNG!", Color.yellow);
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
                ShowToast($"TÚI CHẬT! Chỉ mua được {added} {seedItem.itemName} (-{partialCost}v)", Color.yellow);
                if (msgBought) AutoHide.Flash(msgBought);
                return true;
            }
            ShowToast("✕ TÚI ĐỒ ĐÃ ĐẦY! Hãy dọn bớt ô trống.", new Color(1f, 0.85f, 0.25f));
            if (msgBagFull) AutoHide.Flash(msgBagFull);
            return false;
        }

        wallet.TrySpend(totalCost);
        ShowToast($"✓ ĐÃ MUA x{count} {seedItem.itemName.ToUpper()}! (-{totalCost} VÀNG)", new Color(0.35f, 1f, 0.5f));
        if (msgBought) AutoHide.Flash(msgBought);
        return true;
    }

    void ShowToast(string msg, Color col)
    {
        toastMessage = msg;
        toastColor = col;
        toastTimer = 3.0f;

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

    static void DrawPanel(Rect r, Color bg, Color border, float borderWidth = 3f)
    {
        // Bong do dam phia sau (Drop Shadow)
        DrawSolidRect(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.6f));
        // Vien vien hop thoai
        DrawSolidRect(r, border);
        // Nen ben trong
        DrawSolidRect(new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f), bg);
    }

    static void DrawShadowLabel(Rect r, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 2f)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, style);
        GUI.Label(new Rect(r.x - 1f, r.y + offset, r.width, r.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(r, text, style);
        style.normal.textColor = old;
    }

    void OnGUI()
    {
        if (Camera.main == null) return;

        // Ty le phong to tu dong theo do phan giai man hinh de chu luon to, ro net
        float scale = Mathf.Clamp(Screen.height / 900f, 1.25f, 2.5f);

        // 1. KHI PLAYER O XA: Hien thi bien hieu World-Space tren noc sap to va ro
        if (!playerInside)
        {
            Vector3 signWorld = transform.position + Vector3.up * 1.85f;
            Vector3 signScreen = Camera.main.WorldToScreenPoint(signWorld);
            if (signScreen.z > 0)
            {
                float sw = Mathf.Round(220f * scale);
                float sh = Mathf.Round(36f * scale);
                float sx = signScreen.x - sw * 0.5f;
                float sy = Screen.height - signScreen.y - sh * 0.5f;

                DrawPanel(new Rect(sx, sy, sw, sh), new Color(0.08f, 0.11f, 0.16f, 0.95f), new Color(1.0f, 0.82f, 0.22f, 0.95f), 2f * scale);

                var signStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(15 * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                string simpleName = seedItem ? seedItem.itemName : stallName;
                DrawShadowLabel(new Rect(sx, sy, sw, sh), $"🌱 {simpleName.ToUpper()} ({Price}v)", signStyle, new Color(1f, 0.94f, 0.45f), Color.black, 2f);
            }
            return;
        }

        // 2. KHI PLAYER DUNG VAO QUAY: Hien thi hop thoai RPG sieu to, cuc ky ro net o giua tren man hinh
        float w = Mathf.Round(640f * scale);
        float h = Mathf.Round(250f * scale);
        float x = Mathf.Round((Screen.width - w) * 0.5f);
        float y = Mathf.Round(24f * scale);

        // Khung card RPG toi sang trong voi vien vang dam
        Color bgCard = new Color(0.07f, 0.09f, 0.13f, 0.98f);
        Color borderGold = new Color(1.0f, 0.82f, 0.22f, 1.0f);
        float bw = Mathf.Round(3.5f * scale);
        DrawPanel(new Rect(x, y, w, h), bgCard, borderGold, bw);

        // Header Banner tren cung
        float headerH = Mathf.Round(48f * scale);
        DrawSolidRect(new Rect(x + bw, y + bw, w - bw * 2f, headerH), new Color(0.14f, 0.19f, 0.27f, 1.0f));
        DrawSolidRect(new Rect(x + bw, y + bw + headerH, w - bw * 2f, Mathf.Round(3f * scale)), new Color(1.0f, 0.82f, 0.22f, 0.85f));

        string itemName = seedItem ? seedItem.itemName : stallName;
        int unitPrice = Price;
        var wallet = PlayerWallet.Instance;
        int currentGold = wallet ? wallet.gold : 0;

        // Tieu de sap hang (Sieu to & Ruc ro)
        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(23 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawShadowLabel(new Rect(x, y + 4f * scale, w, headerH), $"🌱 SẠP HẠT GIỐNG: {itemName.ToUpper()}", headerStyle, new Color(1f, 0.92f, 0.35f), Color.black, 2.5f);

        // Hang 1: Gia ban va so vang hien co trong vi
        var infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(18 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        float contentY = y + headerH + 12f * scale;
        float padX = 24f * scale;

        DrawShadowLabel(new Rect(x + padX, contentY, (w - padX * 2f) * 0.55f, 30f * scale),
            $"💰 Giá: {unitPrice} Vàng / Hạt", infoStyle, new Color(1f, 0.95f, 0.7f), Color.black, 2f);

        var walletStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(18 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        DrawShadowLabel(new Rect(x + padX + (w - padX * 2f) * 0.45f, contentY, (w - padX * 2f) * 0.55f, 30f * scale),
            $"🪙 Ví bạn: {currentGold} Vàng", walletStyle, new Color(0.4f, 0.95f, 1f), Color.black, 2f);

        // Hang 2: Phim bam Mua 1 Hat
        var actionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(17 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        float row1Y = contentY + 36f * scale;
        float rowH = 36f * scale;
        DrawSolidRect(new Rect(x + padX, row1Y, w - padX * 2f, rowH), new Color(0.13f, 0.17f, 0.23f, 0.95f));
        DrawSolidRect(new Rect(x + padX, row1Y, 4f * scale, rowH), new Color(0.4f, 0.85f, 1f, 1f));
        DrawShadowLabel(new Rect(x + padX + 14f * scale, row1Y + 3f * scale, w - padX * 2f, rowH),
            $"[ F ] hoặc [ 1 ]  :  MUA 1 HẠT  ({unitPrice} Vàng)", actionStyle, Color.white, Color.black, 2f);

        // Hang 3: Phim bam Mua 5 Hat
        float row2Y = row1Y + rowH + 8f * scale;
        DrawSolidRect(new Rect(x + padX, row2Y, w - padX * 2f, rowH), new Color(0.13f, 0.17f, 0.23f, 0.95f));
        DrawSolidRect(new Rect(x + padX, row2Y, 4f * scale, rowH), new Color(1.0f, 0.82f, 0.22f, 1f));
        DrawShadowLabel(new Rect(x + padX + 14f * scale, row2Y + 3f * scale, w - padX * 2f, rowH),
            $"[ SHIFT + F ] hoặc [ 5 ]  :  MUA 5 HẠT  ({unitPrice * 5} Vàng)", actionStyle, new Color(1f, 0.95f, 0.5f), Color.black, 2f);

        // Hang 4: Thong bao ket qua giao dich (Toast)
        if (toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
        {
            float toastY = row2Y + rowH + 8f * scale;
            var toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(18 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            DrawSolidRect(new Rect(x + padX, toastY, w - padX * 2f, 32f * scale), new Color(0.04f, 0.05f, 0.08f, 0.95f));
            DrawSolidRect(new Rect(x + padX, toastY, w - padX * 2f, 2f * scale), toastColor);
            DrawShadowLabel(new Rect(x + padX, toastY + 2f * scale, w - padX * 2f, 30f * scale), toastMessage, toastStyle, toastColor, Color.black, 2f);
        }
    }
}
