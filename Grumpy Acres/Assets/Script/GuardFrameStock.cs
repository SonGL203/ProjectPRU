using UnityEngine;

public enum FrameKind { Wood, Iron }

public static class GuardFrameStock
{
    public class Kind
    {
        public string id, displayName, material;
        public float maxHealth;
        public int cost;
        public Color tint;
        public ItemData item;
    }

    public static GameObject Prefab;

    public static readonly Kind[] Kinds =
    {
        new Kind { id = "guard_frame_wood", displayName = "Chòi canh gỗ", material = "gỗ",
                   maxHealth = 80f, cost = 80, tint = Color.white },
        new Kind { id = "guard_frame_iron", displayName = "Chòi canh sắt", material = "sắt",
                   maxHealth = 220f, cost = 180, tint = new Color(0.62f, 0.72f, 0.9f) },
    };

    public static Kind Get(FrameKind k)
    {
        return Kinds[(int)k];
    }

    public static ItemData Item(FrameKind k)
    {
        var kind = Get(k);
        if (kind.item)
        {
            kind.item.iconColor = kind.tint;
            return kind.item;
        }
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.name = "Item_" + kind.id;
        item.id = kind.id;
        item.itemName = kind.displayName;
        item.itemType = ItemType.Placeable;
        item.maxStack = 10;
        item.description = $"Chọn trên hotbar, click để đặt {kind.displayName.ToLower()} ở nông trại (máu {kind.maxHealth:0}).";
        item.icon = FrameIcon();
        item.iconColor = kind.tint;
        kind.item = item;
        return item;
    }

    static Sprite FrameIcon()
    {
        if (!Prefab) Prefab = Resources.Load<GameObject>("GuardFrame");
        if (!Prefab) return null;
        var front = Prefab.transform.Find("Front");
        var sr = front ? front.GetComponent<SpriteRenderer>() : Prefab.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    public static int Count(FrameKind k)
    {
        var inv = InventorySystem.Instance;
        var item = Get(k).item;
        if (!inv || !item) return 0;
        int n = 0;
        foreach (var s in inv.slots)
            if (s != null && s.item == item) n += s.amount;
        return n;
    }

    public static bool TryAdd(FrameKind k)
    {
        var inv = InventorySystem.Instance;
        if (!inv) return false;
        return inv.AddItem(Item(k), 1) == 0;
    }

    // Loai khung dang chon tren hotbar, null neu khong chon khung nao.
    public static Kind Selected()
    {
        var inv = InventorySystem.Instance;
        if (!inv) return null;
        var s = inv.GetSelected();
        if (s == null || s.IsEmpty) return null;
        foreach (var k in Kinds)
            if (k.item && s.item == k.item) return k;
        return null;
    }

    public static bool UseSelected()
    {
        var inv = InventorySystem.Instance;
        return inv && Selected() != null && inv.RemoveFromSlot(inv.selectedHotbar, 1);
    }
}
