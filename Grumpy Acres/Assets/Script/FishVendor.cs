using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FishVendor : MonoBehaviour
{
    public string vendorName = "NgÆ° DĂ¢n Ba Bá»ƒ";
    public Vector3 vendorPosition = new Vector3(29.5f, 18.5f, 0f);

    bool playerInside = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (!sr)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            if (FishItemsDatabase.FishNetSprite != null)
                sr.sprite = FishItemsDatabase.FishNetSprite;
        }
    }

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
        if (kb == null) return;

        FishItemsDatabase.Initialize();

        // [1] Mua CĂ¡ ChĂ©p Giá»‘ng vĂ o tĂºi (15v)
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.CarpFryItem, 1, 15);
        }
        // [2] Mua CĂ¡ VĂ ng Giá»‘ng vĂ o tĂºi (25v)
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.GoldfishFryItem, 1, 25);
        }
        // [3] Mua CĂ¡ Há»“i Giá»‘ng vĂ o tĂºi (50v)
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.SalmonFryItem, 1, 50);
        }
        // [4] Mua Thá»©c Ä‚n CĂ¡ x5 (25v)
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishFoodItem, 5, 25);
        }
        // [5] Mua Vá»£t Báº¯t CĂ¡ (30v)
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishNetItem, 1, 30);
        }
        // [6] Mua Bá»ƒ NuĂ´i CĂ¡ NhĂ¢n Táº¡o (100v)
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishPondItem, 1, 100);
        }
        // [7] Mua & Tháº£ ngay CĂ¡ ChĂ©p vĂ o Há»“ nÆ°á»›c (15v)
        if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame)
        {
            BuyAndReleaseDirectly(FishItemsDatabase.CarpData, 15);
        }
        // [8] Mua & Tháº£ ngay CĂ¡ VĂ ng vĂ o Há»“ nÆ°á»›c (25v)
        if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame)
        {
            BuyAndReleaseDirectly(FishItemsDatabase.GoldfishData, 25);
        }
        // [9] Mua & Tháº£ ngay CĂ¡ Há»“i vĂ o Há»“ nÆ°á»›c (50v)
        if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame)
        {
            BuyAndReleaseDirectly(FishItemsDatabase.SalmonData, 50);
        }
        // [X] BĂ¡n toĂ n bá»™ cĂ¡ thÆ°Æ¡ng pháº©m trong tĂºi
        if (kb.xKey.wasPressedThisFrame)
        {
            SellAllFish();
        }
    }

    void BuyItem(ItemData item, int count, int cost)
    {
        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < cost)
        {
            ShowMsg("KhĂ´ng Ä‘á»§ vĂ ng!", Color.red);
            return;
        }

        if (InventorySystem.Instance == null) return;

        int leftover = InventorySystem.Instance.AddItem(item, count);
        if (leftover > 0)
        {
            ShowMsg("TĂºi Ä‘á»“ Ä‘Ă£ Ä‘áº§y!", Color.yellow);
            return;
        }

        wallet.TrySpend(cost);
        SpawnEffect(transform.position);
        ShowMsg($"ÄĂ£ mua {item.itemName} (-{cost}v)", Color.cyan);
    }

    void BuyAndReleaseDirectly(FishData fishData, int cost)
    {
        if (fishData == null) return;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < cost)
        {
            ShowMsg("KhĂ´ng Ä‘á»§ vĂ ng!", Color.red);
            return;
        }

        var pond = WaterTilemapFishPond.Instance;
        if (!pond) pond = FindFirstObjectByType<WaterTilemapFishPond>();

        if (!pond)
        {
            ShowMsg("KhĂ´ng tĂ¬m tháº¥y há»“ nÆ°á»›c!", Color.red);
            return;
        }

        if (pond.CurrentFishCount >= pond.maxFishCapacity)
        {
            ShowMsg("Há»“ cĂ¡ Ä‘Ă£ Ä‘áº§y!", Color.yellow);
            return;
        }

        wallet.TrySpend(cost);

        Vector3 spawnPos = pond.GetRandomWaterPosition();
        pond.AddFish(fishData, spawnPos);
        pond.SpawnSplash(spawnPos, 5);
        pond.SpawnRipple(spawnPos);

        ShowMsg($"ÄĂ£ mua & tháº£ bĂ³ng má» {fishData.speciesName} vĂ o há»“! (-{cost}v)", new Color(0.2f, 1f, 0.8f));
    }

    void SellAllFish()
    {
        if (InventorySystem.Instance == null || PlayerWallet.Instance == null) return;

        int totalGold = 0;
        int count = 0;

        for (int i = 0; i < InventorySystem.TotalSlots; i++)
        {
            var slot = InventorySystem.Instance.GetSlot(i);
            if (slot == null || slot.IsEmpty || slot.item == null) continue;

            if (slot.item.itemType == ItemType.Fish || slot.item.id.StartsWith("fish_") && !slot.item.id.EndsWith("_fry"))
            {
                int price = slot.item.sellPrice > 0 ? slot.item.sellPrice : 45;
                int income = price * slot.amount;
                totalGold += income;
                count += slot.amount;
                slot.Clear();
            }
        }

        if (totalGold > 0)
        {
            PlayerWallet.Instance.Add(totalGold);
            InventorySystem.Instance.NotifyChanged();
            SpawnEffect(transform.position);
            ShowMsg($"ÄĂ£ bĂ¡n {count} con cĂ¡! (+{totalGold}v)", Color.yellow);
        }
        else
        {
            ShowMsg("KhĂ´ng cĂ³ cĂ¡ thÆ°Æ¡ng pháº©m nĂ o Ä‘á»ƒ bĂ¡n!", Color.white);
        }
    }

    void ShowMsg(string msg, Color col)
    {
        if (WaterTilemapFishPond.Instance != null)
            WaterTilemapFishPond.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, msg, col);
        else if (FishManager.Instance != null)
            FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, msg, col);
    }

    void SpawnEffect(Vector3 pos)
    {
        if (WaterTilemapFishPond.Instance != null)
            WaterTilemapFishPond.Instance.SpawnSplash(pos, 4);
    }

    static Texture2D _fishWhitePixel;
    static void DrawFishPanel(Rect r, Color bg, Color border, float borderWidth = 2f)
    {
        if (_fishWhitePixel == null)
        {
            _fishWhitePixel = new Texture2D(1, 1);
            _fishWhitePixel.SetPixel(0, 0, Color.white);
            _fishWhitePixel.Apply();
        }
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(r.x + 3f, r.y + 3f, r.width, r.height), _fishWhitePixel);
        GUI.color = border;
        GUI.DrawTexture(r, _fishWhitePixel);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f), _fishWhitePixel);
        GUI.color = old;
    }

    static void DrawFishShadowLabel(Rect r, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 1.5f)
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
        if (!playerInside || Camera.main == null) return;

        float scale = Mathf.Clamp(Screen.height / 900f, 1.25f, 2.5f);
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screen.z <= 0) return;

        float w = Mathf.Round(420f * scale);
        float h = Mathf.Round(260f * scale);
        float x = Mathf.Clamp(screen.x - w * 0.5f, 15f * scale, Screen.width - w - 15f * scale);
        float y = Mathf.Clamp(Screen.height - screen.y - h * 0.5f, 15f * scale, Screen.height - h - 15f * scale);

        DrawFishPanel(new Rect(x, y, w, h), new Color(0.08f, 0.12f, 0.16f, 0.96f), new Color(0.25f, 0.85f, 1.0f, 0.95f), 2f * scale);

        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(17 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawFishShadowLabel(new Rect(x, y + 6f * scale, w, 28f * scale), "🏪 CỬA HÀNG THỦY SẢN & HỒ CÁ", headerStyle, new Color(0.35f, 0.92f, 1f), Color.black, 2f);

        var itemStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(13 * scale),
            alignment = TextAnchor.UpperLeft
        };

        string menuText = 
            "--- MUA VÀO TÚI ĐỒ (Click ao để thả) ---\n" +
            "[1] Cá Chép Giống: 15v  |  [4] Thức Ăn Cá x5: 25v\n" +
            "[2] Cá Vàng Giống: 25v  |  [5] Vợt Bắt Cá: 30v\n" +
            "[3] Cá Hồi Giống: 50v   |  [6] Bể Cá Nhân Tạo: 100v\n\n" +
            "--- MUA & THẢ THẲNG VÀO HỒ (Tạo bóng mờ) ---\n" +
            "[7] Thả Cá Chép (15v) - Đói sau 5 phút\n" +
            "[8] Thả Cá Vàng (25v) - Đói sau 4 phút\n" +
            "[9] Thả Cá Hồi (50v)  - Đói sau 6.5 phút\n" +
            "------------------------------------------\n" +
            "[Phím X] BÁN TẤT CẢ CÁ TRƯỞNG THÀNH TRONG TÚI";

        DrawFishShadowLabel(new Rect(x + 16f * scale, y + 36f * scale, w - 32f * scale, h - 45f * scale), menuText, itemStyle, Color.white, Color.black, 1.5f);
    }
}

