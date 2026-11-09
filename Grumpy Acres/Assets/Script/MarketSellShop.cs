using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class SellSlotView
{
    public GameObject root;
    public Image icon;
    public Text label;
    public GameObject highlight;
}

// Cua hang thu mua trong Market: buoc vao cua la hien bang tui do, chon vat pham, nhap so luong roi bam Ban.
public class MarketSellShop : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] SellSlotView[] slotViews;
    [SerializeField] GameObject emptyText;
    [SerializeField] Image selectedIcon;
    [SerializeField] Text selectedName;
    [SerializeField] Text unitPrice;
    [SerializeField] Text totalText;
    [SerializeField] Text feedback;
    [SerializeField] InputField quantityField;
    [SerializeField] ItemData milk;
    [SerializeField] ItemData emptyBucket;
    [SerializeField] string defaultMessage = "Chọn vật phẩm muốn bán";

    bool touching;
    readonly List<ItemData> listed = new List<ItemData>();
    readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
    ItemData selected;
    int quantity = 1;
    bool open;
    bool wasInside;
    string message;
    float messageUntil;

    void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        bool inside = touching;

        var keys = Keyboard.current;
        bool inventoryOpen = InventoryUI.Instance && InventoryUI.Instance.isOpen;

        if (!inside)
        {
            open = false;
            wasInside = false;
        }
        else
        {
            if (!wasInside && !inventoryOpen)
            {
                open = true;
                selected = null;
                quantity = 1;
            }
            wasInside = true;
            if (inventoryOpen || (keys != null && keys.escapeKey.wasPressedThisFrame)) open = false;
        }

        if (panel && panel.activeSelf != open) panel.SetActive(open);
        if (open) RefreshView();
    }

    // Duoc MarketSellTrigger (gan tren ngoi nha) goi khi nguoi choi cham / roi khoi nha
    public void SetTouching(bool value)
    {
        touching = value;
    }

    bool Sellable(ItemData item)
    {
        if (item == null) return false;
        return item == milk || item.itemType == ItemType.Crop || item.itemType == ItemType.Fish;
    }

    void RefreshView()
    {
        var inventory = InventorySystem.Instance;
        listed.Clear();
        counts.Clear();
        if (inventory)
        {
            foreach (var slot in inventory.slots)
            {
                if (slot == null || slot.IsEmpty || !Sellable(slot.item)) continue;
                if (!counts.ContainsKey(slot.item))
                {
                    if (listed.Count >= slotViews.Length) continue;
                    listed.Add(slot.item);
                    counts[slot.item] = 0;
                }
                counts[slot.item] += slot.amount;
            }
        }

        if (selected && !counts.ContainsKey(selected)) selected = null;

        for (int i = 0; i < slotViews.Length; i++)
        {
            var view = slotViews[i];
            bool has = i < listed.Count;
            if (view.root.activeSelf != has) view.root.SetActive(has);
            if (!has) continue;
            var item = listed[i];
            view.icon.sprite = item.icon;
            view.icon.enabled = item.icon != null;
            string label = item.itemName + "\nx" + counts[item];
            if (view.label.text != label) view.label.text = label;
            bool on = item == selected;
            if (view.highlight.activeSelf != on) view.highlight.SetActive(on);
        }
        if (emptyText && emptyText.activeSelf != (listed.Count == 0)) emptyText.SetActive(listed.Count == 0);

        int owned = selected ? counts[selected] : 0;
        quantity = owned > 0 ? Mathf.Clamp(quantity, 1, owned) : 0;

        if (selectedIcon)
        {
            selectedIcon.sprite = selected ? selected.icon : null;
            selectedIcon.enabled = selected != null && selected.icon != null;
        }
        SetText(selectedName, selected ? selected.itemName : "(chưa chọn)");
        int price = selected ? Mathf.Max(0, selected.sellPrice) : 0;
        SetText(unitPrice, selected ? "Đơn giá: " + price + "G   Có: " + owned : "");
        SetText(totalText, "Thành tiền: " + (quantity * price) + "G");
        SetText(feedback, Time.time < messageUntil ? message : defaultMessage);

        if (quantityField && !quantityField.isFocused)
        {
            string typed = quantity.ToString();
            if (quantityField.text != typed) quantityField.SetTextWithoutNotify(typed);
        }
    }

    static void SetText(Text text, string value)
    {
        if (text && text.text != value) text.text = value;
    }

    public void Select(int index)
    {
        if (index < 0 || index >= listed.Count) return;
        selected = listed[index];
        quantity = 1;
    }

    public void OnQuantityEdited(string value)
    {
        int parsed;
        if (int.TryParse(value, out parsed)) quantity = Mathf.Max(1, parsed);
    }

    public void MoreQuantity()
    {
        if (selected) quantity++;
    }

    public void LessQuantity()
    {
        if (selected) quantity--;
    }

    public void MaxQuantity()
    {
        if (selected && counts.ContainsKey(selected)) quantity = counts[selected];
    }

    public void Close()
    {
        open = false;
    }

    public void Sell()
    {
        var inventory = InventorySystem.Instance;
        var wallet = PlayerWallet.Instance;
        if (!inventory || !wallet) return;
        if (!selected || !counts.ContainsKey(selected)) { Say("Hãy chọn vật phẩm trước."); return; }

        int count = Mathf.Clamp(quantity, 1, counts[selected]);
        var item = selected;
        int remaining = count;
        foreach (var slot in inventory.slots)
        {
            if (remaining <= 0) break;
            if (slot == null || slot.IsEmpty || slot.item != item) continue;
            int take = Mathf.Min(remaining, slot.amount);
            slot.amount -= take;
            remaining -= take;
            if (slot.amount <= 0) slot.Clear();
        }
        if (item == milk && emptyBucket) inventory.AddItem(emptyBucket, count);
        inventory.NotifyChanged();

        int income = count * Mathf.Max(0, item.sellPrice);
        wallet.Add(income);
        Say("Đã bán " + count + " " + item.itemName + " (+" + income + "G)");
    }

    void Say(string text)
    {
        message = text;
        messageUntil = Time.time + 2.5f;
    }
}
