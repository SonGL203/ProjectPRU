using UnityEngine;
using UnityEngine.UI;

public enum TradeKind
{
    Item,      // vat pham asset (hat giong...)
    FishItem,  // ca giong / ca truong thanh tao luc chay game (FishItemsDatabase)
    Cow        // bo cua chuong bo
}

public enum FishSlot
{
    CarpFry,
    CarpAdult,
    GoldfishFry,
    GoldfishAdult,
    SalmonFry,
    SalmonAdult,
    FishFood
}

// Mot dong mua / ban trong bang cua hang cho. Chu va nut da duoc dat san trong scene.
public class MarketTradeRow : MonoBehaviour
{
    public TradeKind kind = TradeKind.Item;
    public ItemData item;
    public FishSlot fish;
    public bool canBuy = true;
    public bool canSell = true;

    [Tooltip("Chi dung cho bo")]
    public int cowBuyPrice = 300;
    public int cowSellPrice = 150;

    public Text info;
    public Image icon;
    [Tooltip("Hinh hien cho dong bo")]
    public Sprite cowIcon;
    public MarketStall stall;

    ItemData fishItem;

    ItemData Resolve()
    {
        if (kind == TradeKind.Item) return item;
        if (kind == TradeKind.Cow) return null;
        if (fishItem) return fishItem;

        FishItemsDatabase.Initialize();
        switch (fish)
        {
            case FishSlot.CarpFry: fishItem = FishItemsDatabase.CarpFryItem; break;
            case FishSlot.CarpAdult: fishItem = FishItemsDatabase.CarpItem; break;
            case FishSlot.GoldfishFry: fishItem = FishItemsDatabase.GoldfishFryItem; break;
            case FishSlot.GoldfishAdult: fishItem = FishItemsDatabase.GoldfishItem; break;
            case FishSlot.SalmonFry: fishItem = FishItemsDatabase.SalmonFryItem; break;
            case FishSlot.SalmonAdult: fishItem = FishItemsDatabase.SalmonItem; break;
            case FishSlot.FishFood: fishItem = FishItemsDatabase.FishFoodItem; break;
        }
        return fishItem;
    }

    FishData Species()
    {
        FishItemsDatabase.Initialize();
        switch (fish)
        {
            case FishSlot.CarpFry: return FishItemsDatabase.CarpData;
            case FishSlot.GoldfishFry: return FishItemsDatabase.GoldfishData;
            case FishSlot.SalmonFry: return FishItemsDatabase.SalmonData;
        }
        return null;
    }

    int BuyPrice(ItemData data)
    {
        if (kind == TradeKind.Cow) return cowBuyPrice;
        return data ? Mathf.Max(0, data.buyPrice) : 0;
    }

    int SellPrice(ItemData data)
    {
        if (kind == TradeKind.Cow) return cowSellPrice;
        return data ? Mathf.Max(0, data.sellPrice) : 0;
    }

    int Owned(ItemData data)
    {
        if (kind == TradeKind.Cow) return CowHerd.Owned;
        var inventory = InventorySystem.Instance;
        if (!inventory || !data) return 0;
        int total = 0;
        foreach (var slot in inventory.slots)
        {
            if (slot != null && !slot.IsEmpty && slot.item == data) total += slot.amount;
        }
        return total;
    }

    public void Refresh()
    {
        var data = Resolve();
        if (icon)
        {
            Sprite picture = kind == TradeKind.Cow ? cowIcon : (data ? data.icon : null);
            if (icon.sprite != picture) icon.sprite = picture;
            if (icon.enabled != (picture != null)) icon.enabled = picture != null;
        }
        if (!info) return;
        string text;
        if (kind == TradeKind.Cow)
        {
            text = "Bò sữa   Mua " + cowBuyPrice + "G   ";
            if (canSell) text += "Bán " + cowSellPrice + "G   ";
            text += "Có: " + CowHerd.Owned + "/" + CowHerd.Capacity;
        }
        else if (!data)
        {
            text = "(chưa có dữ liệu)";
        }
        else
        {
            text = data.itemName + "   ";
            if (canBuy) text += "Mua" + (data.itemType == ItemType.FishFry ? " (thả hồ) " : " ") + BuyPrice(data) + "G   ";
            if (canSell) text += "Bán " + SellPrice(data) + "G   ";
            text += "Có: " + Owned(data);
        }
        if (info.text != text) info.text = text;
    }

    public void Buy()
    {
        var wallet = PlayerWallet.Instance;
        var inventory = InventorySystem.Instance;
        if (!canBuy || !wallet || !inventory) return;

        if (kind == TradeKind.Cow)
        {
            if (CowHerd.Owned >= CowHerd.Capacity) { Say("Chuồng bò đã đầy."); return; }
            if (!wallet.TrySpend(cowBuyPrice)) { Say("Không đủ vàng."); return; }
            CowHerd.Owned++;
            Say("Đã mua 1 bò (-" + cowBuyPrice + "G). Hãy ra chuồng bò chăm sóc.");
            return;
        }

        var data = Resolve();
        if (!data) { Say("Chưa có dữ liệu vật phẩm."); return; }
        int price = BuyPrice(data);

        if (data.itemType == ItemType.FishFry)
        {
            var pond = WaterTilemapFishPond.Instance;
            var species = Species();
            if (pond && species)
            {
                if (pond.CurrentFishCount >= pond.maxFishCapacity) { Say("Hồ cá đã đầy."); return; }
                if (!wallet.TrySpend(price)) { Say("Không đủ vàng."); return; }
                Vector3 position = pond.GetRandomWaterPosition();
                pond.AddFish(species, position);
                pond.SpawnSplash(position, 5);
                pond.SpawnRipple(position);
                Say("Đã mua & thả " + species.speciesName + " xuống hồ (-" + price + "G)");
                return;
            }
        }

        if (!HasSpace(inventory, data)) { Say("Túi đồ đã đầy."); return; }
        if (!wallet.TrySpend(price)) { Say("Không đủ vàng."); return; }
        inventory.AddItem(data, 1);
        Say("Đã mua " + data.itemName + " (-" + price + "G)");
    }

    public void Sell()
    {
        SellAmount(1);
    }

    public void SellAll()
    {
        SellAmount(int.MaxValue);
    }

    void SellAmount(int wanted)
    {
        var wallet = PlayerWallet.Instance;
        var inventory = InventorySystem.Instance;
        if (!canSell || !wallet || !inventory) return;

        if (kind == TradeKind.Cow)
        {
            if (CowHerd.Owned <= 0) { Say("Bạn không có bò để bán."); return; }
            CowHerd.Owned--;
            wallet.Add(cowSellPrice);
            Say("Đã bán 1 bò (+" + cowSellPrice + "G)");
            return;
        }

        var data = Resolve();
        if (!data) { Say("Chưa có dữ liệu vật phẩm."); return; }
        int count = Mathf.Min(wanted, Owned(data));
        if (count <= 0) { Say("Bạn không có " + data.itemName + "."); return; }

        int remaining = count;
        foreach (var slot in inventory.slots)
        {
            if (remaining <= 0) break;
            if (slot == null || slot.IsEmpty || slot.item != data) continue;
            int take = Mathf.Min(remaining, slot.amount);
            slot.amount -= take;
            remaining -= take;
            if (slot.amount <= 0) slot.Clear();
        }
        inventory.NotifyChanged();

        int income = count * SellPrice(data);
        wallet.Add(income);
        Say("Đã bán " + count + " " + data.itemName + " (+" + income + "G)");
    }

    static bool HasSpace(InventorySystem inventory, ItemData data)
    {
        foreach (var slot in inventory.slots)
        {
            if (slot == null) continue;
            if (slot.IsEmpty) return true;
            if (slot.item == data && data.IsStackable && slot.amount < data.maxStack) return true;
        }
        return false;
    }

    void Say(string message)
    {
        if (stall) stall.Say(message);
    }
}
