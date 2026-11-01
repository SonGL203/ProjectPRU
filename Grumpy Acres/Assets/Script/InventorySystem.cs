using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemSlot
{
    public ItemData item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public ItemSlot()
    {
        item = null;
        amount = 0;
    }

    public ItemSlot(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }

    public void CopyFrom(ItemSlot other)
    {
        if (other == null || other.IsEmpty)
        {
            Clear();
        }
        else
        {
            item = other.item;
            amount = other.amount;
        }
    }
}

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    // Sprite Action_panel_1 co 10 o (phim 1-9 va phim 0)
    public const int HotbarSize = 10;
    public const int GridCols   = 5;
    public const int GridRows   = 4;
    public const int GridSize   = GridCols * GridRows; // 20
    public const int TotalSlots = HotbarSize + GridSize; // 10 + 20 = 30

    [Header("Danh sach slot (0-9: Hotbar, 10-29: Inventory Grid)")]
    public ItemSlot[] slots = new ItemSlot[TotalSlots];

    [Header("Hotbar dang chon (0-9)")]
    public int selectedHotbar = 0;

    [Header("Khoi tao vat pham (De trong neu muon bat dau khong co item)")]
    public List<StartingItem> startingItems = new();

    [Serializable]
    public struct StartingItem
    {
        public ItemData item;
        [Min(1)] public int amount;
    }

    public event Action OnChanged;
    public event Action<int> OnHotbarSelected;

    public void NotifyChanged() => OnChanged?.Invoke();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khoi tao cac slot trong
        if (slots == null || slots.Length != TotalSlots)
            slots = new ItemSlot[TotalSlots];

        for (int i = 0; i < TotalSlots; i++)
        {
            if (slots[i] == null)
                slots[i] = new ItemSlot();
        }
    }

    void Start()
    {
        // Chi them vat pham neu nguoi dung chu dong them vao danh sach startingItems trong Inspector
        if (IsInventoryEmpty() && startingItems != null && startingItems.Count > 0)
        {
            foreach (var s in startingItems)
            {
                if (s.item != null)
                    AddItem(s.item, s.amount);
            }
        }
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        // Phim F1 de nhan full do test (Cuoc, Binh tuoi, Riu, Cuoc chim, Hat giong)
        if (kb.f1Key.wasPressedThisFrame)
        {
            GiveAllTestItems();
        }
    }

    public void GiveAllTestItems()
    {
        // Sap xep uu tien ro rang:
        // Slot 1: Cuoc (Tool_Hoe)
        // Slot 2: Binh tuoi (Tool_WateringCan)
        // Slot 3: Hat Ca chua (Seed_Tomato)
        // Slot 4: Hat Ca rot (Seed_Carrot)
        // Slot 5: Hat Lua mi (Seed_Wheat)
        ClearAll();

        AddItemByName("Tool_Hoe", 1);
        AddItemByName("Tool_WateringCan", 1);
        AddItemByName("Seed_Tomato", 10);
        AddItemByName("Seed_Carrot", 10);
        AddItemByName("Seed_Wheat", 10);
        AddItemByName("Tool_Axe", 1);
        AddItemByName("Tool_Pickaxe", 1);

        NotifyChanged();
        SelectHotbar(0); // Tu dong chon slot dau tien la Cuoc Dat
        Debug.Log("<color=green>[Test]</color> Da nap do test theo thu tu: 1-Cuoc, 2-Binh tuoi, 3-Hat Ca chua, 4-Hat Ca rot, 5-Hat Lua mi!");
    }

    void AddItemByName(string assetName, int count)
    {
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets($"{assetName} t:ItemData");
        foreach (var g in guids)
        {
            var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var it = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(p);
            if (it != null && it.name == assetName)
            {
                AddItem(it, count);
                return;
            }
        }
#endif
    }

    public void ClearAll()
    {
        for (int i = 0; i < TotalSlots; i++)
        {
            if (slots[i] != null) slots[i].Clear();
        }
        NotifyChanged();
    }

    public bool IsInventoryEmpty()
    {
        for (int i = 0; i < TotalSlots; i++)
        {
            if (slots[i] != null && !slots[i].IsEmpty) return false;
        }
        return true;
    }

    // --- Truy xuat slot ---
    public ItemSlot GetSlot(int index)
    {
        if (index < 0 || index >= TotalSlots) return null;
        return slots[index];
    }

    public ItemSlot GetHotbarSlot(int index) => GetSlot(index);
    public ItemSlot GetGridSlot(int index)   => GetSlot(HotbarSize + index);
    public ItemSlot GetSelected()            => GetSlot(selectedHotbar);

    public bool IsHotbarIndex(int slotIndex) => slotIndex >= 0 && slotIndex < HotbarSize;
    public bool IsGridIndex(int slotIndex)   => slotIndex >= HotbarSize && slotIndex < TotalSlots;

    public void SelectHotbar(int index)
    {
        selectedHotbar = (index % HotbarSize + HotbarSize) % HotbarSize;
        OnHotbarSelected?.Invoke(selectedHotbar);
    }

    // --- Them vat pham ---
    public int AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return amount;

        // 1. Stack vao slot da co item cung loai
        if (item.IsStackable)
        {
            for (int i = 0; i < TotalSlots && amount > 0; i++)
            {
                var s = slots[i];
                if (s.item == item && s.amount < item.maxStack)
                {
                    int canAdd = item.maxStack - s.amount;
                    int adding = Mathf.Min(canAdd, amount);
                    s.amount += adding;
                    amount   -= adding;
                }
            }
        }

        // 2. Them vao slot trong
        for (int i = 0; i < TotalSlots && amount > 0; i++)
        {
            var s = slots[i];
            if (s.IsEmpty)
            {
                int adding = Mathf.Min(item.maxStack, amount);
                s.item   = item;
                s.amount = adding;
                amount  -= adding;
            }
        }

        NotifyChanged();
        return amount; // Phan du neu full
    }

    // --- Xoa vat pham khoi slot ---
    public bool RemoveFromSlot(int slotIndex, int amount = 1)
    {
        var s = GetSlot(slotIndex);
        if (s == null || s.IsEmpty || s.amount < amount) return false;

        s.amount -= amount;
        if (s.amount <= 0) s.Clear();

        NotifyChanged();
        return true;
    }

    // --- Hoan doi hoac hop nhat giua 2 slot (phuc vu sap xep Hotbar & Inventory) ---
    public void SwapOrMerge(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        var from = GetSlot(fromIndex);
        var to   = GetSlot(toIndex);
        if (from == null || to == null) return;
        if (from.IsEmpty && to.IsEmpty) return;

        // 1. Target trong -> chuyen toan bo
        if (to.IsEmpty)
        {
            to.CopyFrom(from);
            from.Clear();
        }
        // 2. Cung loai item va stack duoc -> gop stack
        else if (from.item == to.item && to.item.IsStackable)
        {
            int space = to.item.maxStack - to.amount;
            if (space > 0)
            {
                int transfer = Mathf.Min(space, from.amount);
                to.amount += transfer;
                from.amount -= transfer;
                if (from.amount <= 0) from.Clear();
            }
            else
            {
                var temp = new ItemSlot(to.item, to.amount);
                to.CopyFrom(from);
                from.CopyFrom(temp);
            }
        }
        // 3. Khac loai -> hoan doi (Swap)
        else
        {
            var temp = new ItemSlot(to.item, to.amount);
            to.CopyFrom(from);
            from.CopyFrom(temp);
        }

        NotifyChanged();
    }

    // --- Tach nua stack (Right-click split) ---
    public void SplitStack(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        var from = GetSlot(fromIndex);
        var to   = GetSlot(toIndex);
        if (from == null || from.IsEmpty || to == null) return;

        if (to.IsEmpty)
        {
            int half = Mathf.CeilToInt(from.amount / 2f);
            to.item = from.item;
            to.amount = half;
            from.amount -= half;
            if (from.amount <= 0) from.Clear();
            NotifyChanged();
        }
        else if (to.item == from.item && to.item.IsStackable && to.amount < to.item.maxStack)
        {
            to.amount += 1;
            from.amount -= 1;
            if (from.amount <= 0) from.Clear();
            NotifyChanged();
        }
    }

    // --- Chuyen nhanh giua Hotbar va Grid (Shift + Click) ---
    public bool QuickTransfer(int fromIndex)
    {
        var from = GetSlot(fromIndex);
        if (from == null || from.IsEmpty) return false;

        // Tu Hotbar -> chuyen vao Grid
        if (IsHotbarIndex(fromIndex))
        {
            for (int i = HotbarSize; i < TotalSlots; i++)
            {
                var target = slots[i];
                if (target.IsEmpty || (target.item == from.item && target.item.IsStackable && target.amount < target.item.maxStack))
                {
                    SwapOrMerge(fromIndex, i);
                    return true;
                }
            }
        }
        // Tu Grid -> chuyen vao Hotbar
        else if (IsGridIndex(fromIndex))
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                var target = slots[i];
                if (target.IsEmpty || (target.item == from.item && target.item.IsStackable && target.amount < target.item.maxStack))
                {
                    SwapOrMerge(fromIndex, i);
                    return true;
                }
            }
        }

        return false;
    }
}
