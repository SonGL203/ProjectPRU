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

        // [1] Mua Ca Chep Giong (15v)
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.CarpFryItem, 1, 15);
        }
        // [2] Mua Ca Vang Giong (25v)
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.GoldfishFryItem, 1, 25);
        }
        // [3] Mua Ca Hoi Giong (50v)
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.SalmonFryItem, 1, 50);
        }
        // [4] Mua Thuc An Ca x5 (25v)
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishFoodItem, 5, 25);
        }
        // [5] Mua Vot Bat Ca (30v)
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishNetItem, 1, 30);
        }
        // [6] Mua Be Ca Nhan Tao (100v)
        if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame)
        {
            BuyItem(FishItemsDatabase.FishPondItem, 1, 100);
        }
        // [X] Ban tat ca ca trong tui
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
            if (FishManager.Instance) FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, "Không đủ vàng!", Color.red);
            return;
        }

        if (InventorySystem.Instance == null) return;

        int leftover = InventorySystem.Instance.AddItem(item, count);
        if (leftover > 0)
        {
            if (FishManager.Instance) FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, "Túi đồ đã đầy!", Color.yellow);
            return;
        }

        wallet.TrySpend(cost);
        if (FishManager.Instance)
        {
            FishManager.Instance.SpawnSplash(transform.position, 3);
            FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, $"Đã mua {item.itemName} (-{cost}v)", Color.cyan);
        }
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
                int price = slot.item.sellPrice > 0 ? slot.item.sellPrice : 30;
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
            if (FishManager.Instance)
            {
                FishManager.Instance.SpawnSplash(transform.position, 5);
                FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, $"Đã bán {count} con cá! (+{totalGold}v)", Color.yellow);
            }
        }
        else
        {
            if (FishManager.Instance)
                FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 1f, "Không có cá nào để bán!", Color.white);
        }
    }

    void OnGUI()
    {
        if (!playerInside || Camera.main == null) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screen.z <= 0) return;

        float w = 320f;
        float h = 180f;
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
        GUI.Label(new Rect(x, y + 4, w, 22), "🏪 CỬA HÀNG THỦY SẢN", headerStyle);

        var itemStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.UpperLeft
        };
        itemStyle.normal.textColor = Color.white;

        string menuText = 
            "[Phím 1] Mua Cá Chép Giống: 15 vàng\n" +
            "[Phím 2] Mua Cá Vàng Giống: 25 vàng\n" +
            "[Phím 3] Mua Cá Hồi Giống: 50 vàng\n" +
            "[Phím 4] Mua Thức Ăn Cá (x5): 25 vàng\n" +
            "[Phím 5] Mua Vợt Bắt Cá: 30 vàng\n" +
            "[Phím 6] Mua Bể Nuôi Cá: 100 vàng\n" +
            "[Phím X] BÁN TẤT CẢ CÁ TRONG TÚI";

        GUI.Label(new Rect(x + 15, y + 28, w - 30, 140), menuText, itemStyle);
    }
}
