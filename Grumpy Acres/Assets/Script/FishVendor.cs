using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FishVendor : MonoBehaviour
{
    public string vendorName = "Ngư Dân Ba Bể";
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

        // [1] Mua Cá Chép Giống vào túi (15v)
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.CarpFryItem, 1, 15);
        }
        // [2] Mua Cá Vàng Giống vào túi (25v)
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.GoldfishFryItem, 1, 25);
        }
        // [3] Mua Cá Hồi Giống vào túi (50v)
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.SalmonFryItem, 1, 50);
        }
        // [4] Mua Thức Ăn Cá x5 (25v)
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishFoodItem, 5, 25);
        }
        // [5] Mua Vợt Bắt Cá (30v)
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishNetItem, 1, 30);
        }
        // [6] Mua Bể Nuôi Cá Nhân Tạo (100v)
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishPondItem, 1, 100);
        }
        // [7] Mua & Thả ngay Cá Chép vào Hồ nước (15v)
        if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame)
        {
            BuyAndReleaseDirectly(FishItemsDatabase.CarpData, 15);
        }
        // [8] Mua & Thả ngay Cá Vàng vào Hồ nước (25v)
        if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame)
        {
            BuyAndReleaseDirectly(FishItemsDatabase.GoldfishData, 25);
        }
        // [9] Mua & Thả ngay Cá Hồi vào Hồ nước (50v)
        if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame)
        {
            BuyAndReleaseDirectly(FishItemsDatabase.SalmonData, 50);
        }
        // [X] Bán toàn bộ cá thương phẩm trong túi
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
            ShowMsg("Không đủ vàng!", Color.red);
            return;
        }

        if (InventorySystem.Instance == null) return;

        int leftover = InventorySystem.Instance.AddItem(item, count);
        if (leftover > 0)
        {
            ShowMsg("Túi đồ đã đầy!", Color.yellow);
            return;
        }

        wallet.TrySpend(cost);
        SpawnEffect(transform.position);
        ShowMsg($"Đã mua {item.itemName} (-{cost}v)", Color.cyan);
    }

    void BuyAndReleaseDirectly(FishData fishData, int cost)
    {
        if (fishData == null) return;

        var wallet = PlayerWallet.Instance;
        if (!wallet || wallet.gold < cost)
        {
            ShowMsg("Không đủ vàng!", Color.red);
            return;
        }

        var pond = WaterTilemapFishPond.Instance;
        if (!pond) pond = FindFirstObjectByType<WaterTilemapFishPond>();

        if (!pond)
        {
            ShowMsg("Không tìm thấy hồ nước!", Color.red);
            return;
        }

        if (pond.CurrentFishCount >= pond.maxFishCapacity)
        {
            ShowMsg("Hồ cá đã đầy!", Color.yellow);
            return;
        }

        wallet.TrySpend(cost);

        Vector3 spawnPos = pond.GetRandomWaterPosition();
        pond.AddFish(fishData, spawnPos);
        pond.SpawnSplash(spawnPos, 5);
        pond.SpawnRipple(spawnPos);

        ShowMsg($"Đã mua & thả bóng mờ {fishData.speciesName} vào hồ! (-{cost}v)", new Color(0.2f, 1f, 0.8f));
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
            ShowMsg($"Đã bán {count} con cá! (+{totalGold}v)", Color.yellow);
        }
        else
        {
            ShowMsg("Không có cá thương phẩm nào để bán!", Color.white);
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

    void OnGUI()
    {
        if (!playerInside || Camera.main == null) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screen.z <= 0) return;

        float w = 340f;
        float h = 220f;
        float x = Mathf.Clamp(screen.x - w * 0.5f, 10f, Screen.width - w - 10f);
        float y = Mathf.Clamp(Screen.height - screen.y - h * 0.5f, 10f, Screen.height - h - 10f);

        GUI.Box(new Rect(x, y, w, h), "");

        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(0.2f, 0.9f, 1f);
        GUI.Label(new Rect(x, y + 4, w, 22), "🏪 CỬA HÀNG THỦY SẢN & HỒ CÁ", headerStyle);

        var itemStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft
        };
        itemStyle.normal.textColor = Color.white;

        string menuText = 
            "--- MUA VÀO TÚI ĐỒ (Click ao để thả) ---\n" +
            "[1] Cá Chép Giống: 15v  |  [4] Thức Ăn Cá x5: 25v\n" +
            "[2] Cá Vàng Giống: 25v  |  [5] Vợt Bắt Cá: 30v\n" +
            "[3] Cá Hồi Giống: 50v   |  [6] Bể Cá Nhân Tạo: 100v\n" +
            "--- MUA & THẢ THẲNG VÀO HỒ (Tạo bóng mờ) ---\n" +
            "[7] Thả Cá Chép (15v) - Đói sau 5 phút\n" +
            "[8] Thả Cá Vàng (25v) - Đói sau 4 phút\n" +
            "[9] Thả Cá Hồi (50v)  - Đói sau 6.5 phút\n" +
            "------------------------------------------\n" +
            "[Phím X] BÁN TẤT CẢ CÁ TRƯỞNG THÀNH TRONG TÚI";

        GUI.Label(new Rect(x + 12, y + 26, w - 24, 185), menuText, itemStyle);
    }
}
