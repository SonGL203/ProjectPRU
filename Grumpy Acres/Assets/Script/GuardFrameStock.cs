using UnityEngine;

public static class GuardFrameStock
{
    public static GameObject Prefab;

    static ItemData item;

    public static ItemData Item
    {
        get
        {
            if (item) return item;
            item = ScriptableObject.CreateInstance<ItemData>();
            item.name = "Item_GuardFrame";
            item.id = "guard_frame";
            item.itemName = "Khung canh gac";
            item.itemType = ItemType.Placeable;
            item.maxStack = 10;
            item.description = "Chon tren hotbar, click de dat khung canh gac o nong trai.";
            item.icon = FrameIcon();
            return item;
        }
    }

    static Sprite FrameIcon()
    {
        if (!Prefab)
        {
            var spot = Object.FindFirstObjectByType<TowerSpot>();
            if (spot) Prefab = spot.framePrefab;
        }
        if (!Prefab) return null;
        var front = Prefab.transform.Find("Front");
        var sr = front ? front.GetComponent<SpriteRenderer>() : Prefab.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    public static int Count
    {
        get
        {
            var inv = InventorySystem.Instance;
            if (!inv) return 0;
            int n = 0;
            foreach (var s in inv.slots)
                if (s != null && s.item == item && item) n += s.amount;
            return n;
        }
    }

    public static bool TryAdd()
    {
        var inv = InventorySystem.Instance;
        if (!inv) return false;
        return inv.AddItem(Item, 1) == 0;
    }

    public static bool IsSelected()
    {
        var inv = InventorySystem.Instance;
        if (!inv || !item) return false;
        var s = inv.GetSelected();
        return s != null && !s.IsEmpty && s.item == item;
    }

    public static bool UseSelected()
    {
        var inv = InventorySystem.Instance;
        return inv && IsSelected() && inv.RemoveFromSlot(inv.selectedHotbar, 1);
    }
}
