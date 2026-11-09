using UnityEngine;
using UnityEngine.InputSystem;

namespace CowFarm
{
    public class CowFarmDairy : MonoBehaviour
    {
        [SerializeField] Transform spawn, shop, trough, waterTrough;
        [SerializeField] Transform[] milkingStalls;
        [SerializeField] GameObject shopPanel;
        [SerializeField] UnityEngine.UI.Text instructions;
        [SerializeField] TextMesh[] cowLabels;
        [SerializeField] ItemData hay, water, emptyBucket, milk;
        [SerializeField, Min(0.1f)] float interactionRange = 1.8f;

        sealed class CowState
        {
            public bool Fed, Watered, Ready;
            public (int year, Season season, int day) CareDay;
        }

        static CowFarmDairy activeFarm;
        static CowState[] herdState;
        public static bool HandlesInteractKey => activeFarm && activeFarm.HasNearbyInteraction();

        (int year, Season season, int day) today;
        static InventorySystem owner;
        PlayerMove player;
        Transform[] cows;
        int nearbyCow = -1;
        bool shopOpen;
        string feedback;
        float feedbackUntil;

        public Transform Spawn => spawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHerd()
        {
            activeFarm = null;
            owner = null;
            herdState = null;
        }

        void Awake()
        {
            cows = new Transform[milkingStalls.Length];
            for (int i = 0; i < cows.Length; i++)
            {
                var motion = milkingStalls[i].GetComponentInChildren<CowFarmCowMotion>();
                cows[i] = motion ? motion.transform : milkingStalls[i];
            }
        }

        void OnEnable() => activeFarm = this;

        void OnDisable()
        {
            if (activeFarm == this) activeFarm = null;
        }

        bool HasNearbyInteraction()
        {
            if (Near(shop) || Near(trough) || Near(waterTrough)) return true;
            foreach (var cow in cows)
                if (Near(cow)) return true;
            return false;
        }

        void Update()
        {
            var inventory = InventorySystem.Instance;
            var wallet = PlayerWallet.Instance;
            if (!player) player = FindFirstObjectByType<PlayerMove>();
            var seasons = SeasonManager.Instance;
            if (!inventory || !wallet || !player || !seasons) return;

            today = (seasons.currentYear, seasons.currentSeason, seasons.currentDayInSeason);
            if (owner != inventory || herdState == null || herdState.Length != milkingStalls.Length)
            {
                owner = inventory;
                herdState = new CowState[milkingStalls.Length];
                for (int i = 0; i < herdState.Length; i++) herdState[i] = new CowState { CareDay = today };
            }

            foreach (var cow in herdState)
            {
                if (cow.CareDay == today) continue;
                cow.Ready |= cow.Fed && cow.Watered;
                cow.Fed = false;
                cow.Watered = false;
                cow.CareDay = today;
            }

            bool atShop = Near(shop);
            bool atTrough = Near(trough);
            bool atWater = Near(waterTrough);
            nearbyCow = -1;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < milkingStalls.Length; i++)
            {
                float distance = (player.transform.position - cows[i].position).sqrMagnitude;
                if (!Near(cows[i]) || distance >= nearest) continue;
                nearbyCow = i;
                nearest = distance;
            }

            var keys = Keyboard.current;
            bool inventoryOpen = InventoryUI.Instance && InventoryUI.Instance.isOpen;
            if (!atShop || inventoryOpen || (keys != null && keys.escapeKey.wasPressedThisFrame))
                shopOpen = false;

            if (!inventoryOpen && keys != null && keys.eKey.wasPressedThisFrame)
            {
                var selected = inventory.GetSelected();
                var item = selected != null ? selected.item : null;
                if (atShop) shopOpen = !shopOpen;
                else if ((atTrough || nearbyCow >= 0) && item == hay) FeedCow(atTrough ? -1 : nearbyCow);
                else if ((atWater || nearbyCow >= 0) && item == water) WaterCow(atWater ? -1 : nearbyCow);
                else if (nearbyCow >= 0 && item == emptyBucket) MilkCow();
                else if (atTrough || atWater || nearbyCow >= 0)
                    Show("Dùng phím số để chọn đúng vật phẩm.");
            }

            if (shopPanel.activeSelf != shopOpen) shopPanel.SetActive(shopOpen);

            for (int i = 0; i < herdState.Length; i++)
            {
                var cow = herdState[i];
                string state = cow.Ready ? "Có sữa!" :
                    cow.Fed && cow.Watered ? "Chờ ngày mới" :
                    !cow.Fed && !cow.Watered ? "Đói, khát" :
                    cow.Fed ? "Khát" : "Đói";
                cowLabels[i].transform.position = cows[i].position + Vector3.up * 0.95f;
                if (cowLabels[i].text != state) cowLabels[i].text = state;
                cowLabels[i].color = cow.Ready ? new Color(0.72f, 1f, 0.55f) : new Color(1f, 0.88f, 0.58f);
            }

            if (!shopOpen) return;
            string prices = $"Cỏ {hay.buyPrice}G · Nước {water.buyPrice}G · Xô {emptyBucket.buyPrice}G · Sữa +{milk.sellPrice}G";
            string message = Time.time < feedbackUntil ? feedback : prices;
            if (instructions.text != message) instructions.text = message;
        }

        public void BuyWater()
        {
            if (shopOpen && Near(shop)) Buy(InventorySystem.Instance, PlayerWallet.Instance, water);
        }

        public void BuyHay()
        {
            if (shopOpen && Near(shop)) Buy(InventorySystem.Instance, PlayerWallet.Instance, hay);
        }

        public void BuyBucket()
        {
            if (shopOpen && Near(shop)) Buy(InventorySystem.Instance, PlayerWallet.Instance, emptyBucket);
        }

        public void SellMilk()
        {
            if (shopOpen && Near(shop))
                Exchange(InventorySystem.Instance, PlayerWallet.Instance, milk, emptyBucket, milk.sellPrice, "Đã bán sữa");
        }

        void FeedCow(int cowIndex)
        {
            int hungry = cowIndex >= 0 ? cowIndex : System.Array.FindIndex(herdState, cow => !cow.Fed);
            if (hungry >= 0 && herdState[hungry].Fed) { Show("Bò này đã ăn hôm nay."); return; }
            if (hungry < 0) { Show("Hôm nay cả đàn đã ăn."); return; }
            var inventory = InventorySystem.Instance;
            int slot = inventory.selectedHotbar;
            if (!inventory.RemoveFromSlot(slot)) { Show("Bạn cần cỏ khô."); return; }
            herdState[hungry].Fed = true;
            Show($"Đã cho bò {hungry + 1} ăn.");
        }

        void WaterCow(int cowIndex)
        {
            int thirsty = cowIndex >= 0 ? cowIndex : System.Array.FindIndex(herdState, cow => !cow.Watered);
            if (thirsty >= 0 && herdState[thirsty].Watered) { Show("Bò này đã uống hôm nay."); return; }
            if (thirsty < 0) { Show("Hôm nay cả đàn đã uống nước."); return; }
            var inventory = InventorySystem.Instance;
            if (!inventory.RemoveFromSlot(inventory.selectedHotbar)) return;
            herdState[thirsty].Watered = true;
            Show($"Đã cho bò {thirsty + 1} uống nước.");
        }

        void MilkCow()
        {
            if (nearbyCow < 0 || !Near(cows[nearbyCow])) return;
            if (!herdState[nearbyCow].Ready) { Show("Bò này chưa có sữa hôm nay."); return; }
            if (Exchange(InventorySystem.Instance, PlayerWallet.Instance, emptyBucket, milk, 0, "Đã vắt sữa", InventorySystem.Instance.selectedHotbar))
                herdState[nearbyCow].Ready = false;
        }

        bool Near(Transform station) =>
            player && ((Vector2)(player.transform.position - station.position)).sqrMagnitude <= interactionRange * interactionRange;

        static int FindItem(InventorySystem inventory, ItemData item) =>
            System.Array.FindIndex(inventory.slots, slot => slot != null && !slot.IsEmpty && slot.item == item);

        static bool HasSpace(InventorySystem inventory, ItemData item) =>
            System.Array.Exists(inventory.slots, slot => slot.IsEmpty || (slot.item == item && slot.amount < item.maxStack));

        void Buy(InventorySystem inventory, PlayerWallet wallet, ItemData item)
        {
            if (!HasSpace(inventory, item)) { Show("Túi đồ đã đầy."); return; }
            if (!wallet.TrySpend(Mathf.Max(0, item.buyPrice))) { Show("Không đủ vàng."); return; }
            inventory.AddItem(item);
            Show("Đã mua " + item.itemName);
        }

        bool Exchange(InventorySystem inventory, PlayerWallet wallet, ItemData input, ItemData output, int gold, string message, int index = -1)
        {
            if (index < 0) index = FindItem(inventory, input);
            if (index < 0) { Show("Cần " + input.itemName); return false; }
            var slot = inventory.GetSlot(index);
            if (slot.amount == 1)
            {
                slot.item = output;
                inventory.NotifyChanged();
            }
            else
            {
                if (!HasSpace(inventory, output)) { Show("Túi đồ đã đầy."); return false; }
                slot.amount--;
                inventory.AddItem(output);
            }
            if (gold > 0) wallet.Add(gold);
            Show(message);
            return true;
        }

        void Show(string message)
        {
            feedback = message;
            feedbackUntil = Time.time + 2f;
        }
    }
}
