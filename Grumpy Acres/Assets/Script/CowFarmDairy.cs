using UnityEngine;
using UnityEngine.InputSystem;

namespace CowFarm
{
    public class CowFarmDairy : MonoBehaviour
    {
        [SerializeField] Transform spawn, shop, trough;
        [SerializeField] Transform[] milkingStalls;
        [SerializeField] UnityEngine.UI.Text herdStatus, instructions;
        [SerializeField] UnityEngine.UI.Button buyHayButton, buyBucketButton, sellButton, feedButton, milkButton;
        [SerializeField] TextMesh[] cowLabels;
        [SerializeField] ItemData hay, emptyBucket, milk;
        [SerializeField, Min(0.1f)] float interactionRange = 1.8f;
        [SerializeField, Min(1f)] float baseMilkSeconds = 24f;

        static float[] progress;
        static InventorySystem owner;
        PlayerMove player;
        int nearbyCow = -1;
        string feedback;
        float feedbackUntil;

        public Transform Spawn => spawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHerd()
        {
            owner = null;
            progress = null;
        }

        void Update()
        {
            var inventory = InventorySystem.Instance;
            var wallet = PlayerWallet.Instance;
            if (!player) player = FindFirstObjectByType<PlayerMove>();
            if (!inventory || !wallet || !player) return;

            if (owner != inventory || progress == null || progress.Length != milkingStalls.Length)
            {
                owner = inventory;
                progress = new float[milkingStalls.Length];
                for (int i = 0; i < progress.Length; i++) progress[i] = -1f;
            }

            var seasons = SeasonManager.Instance;
            var season = seasons ? seasons.currentSeason : Season.Spring;
            float rate = season switch
            {
                Season.Spring => 1.2f,
                Season.Summer => 0.8f,
                Season.Autumn => 1f,
                Season.Winter => 0.6f,
                _ => 1f
            };
            for (int i = 0; i < progress.Length; i++)
                if (progress[i] >= 0f)
                    progress[i] = Mathf.Min(baseMilkSeconds, progress[i] + Time.deltaTime * rate);

            bool atShop = Near(shop);
            bool atTrough = Near(trough);
            nearbyCow = -1;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < milkingStalls.Length; i++)
            {
                float distance = (player.transform.position - milkingStalls[i].position).sqrMagnitude;
                if (!Near(milkingStalls[i]) || distance >= nearest) continue;
                nearbyCow = i;
                nearest = distance;
            }

            buyHayButton.interactable = atShop;
            buyBucketButton.interactable = atShop;
            sellButton.interactable = atShop;
            feedButton.interactable = atTrough && System.Array.IndexOf(progress, -1f) >= 0;
            milkButton.interactable = nearbyCow >= 0 && progress[nearbyCow] >= baseMilkSeconds;

            var keys = Keyboard.current;
            if (keys != null)
            {
                if (atShop && keys.bKey.wasPressedThisFrame) BuyBucket();
                if (atShop && keys.xKey.wasPressedThisFrame) SellMilk();
                if (keys.fKey.wasPressedThisFrame)
                {
                    if (atShop) BuyHay();
                    else if (atTrough && feedButton.interactable) FeedCow();
                    else if (milkButton.interactable) MilkCow();
                }
            }

            var herd = new System.Text.StringBuilder();
            for (int i = 0; i < progress.Length; i++)
            {
                string state = progress[i] < 0f ? "Đói" :
                    progress[i] >= baseMilkSeconds ? "Có sữa!" :
                    $"Còn {Mathf.CeilToInt((baseMilkSeconds - progress[i]) / rate)} giây";
                if (i > 0) herd.Append("   |   ");
                string label = $"Bò {i + 1}: {state}";
                herd.Append(progress[i] >= baseMilkSeconds ? $"<color=#B8FF8C>{label}</color>" : label);
                if (cowLabels[i].text != label) cowLabels[i].text = label;
                cowLabels[i].color = progress[i] >= baseMilkSeconds ? new Color(0.72f, 1f, 0.55f) : Color.white;
            }

            string status = herd.ToString();
            if (herdStatus.text != status) herdStatus.text = status;
            string hint = atShop ? $"Cỏ {hay.buyPrice}G · Xô {emptyBucket.buyPrice}G · Bán sữa +{milk.sellPrice}G." :
                atTrough ? "Cho một con bò đang đói ăn tại máng." :
                nearbyCow >= 0 ? "Khi bò có sữa, dùng một xô rỗng để vắt." :
                "Đến quầy, máng cỏ hoặc đứng cạnh bò để dùng nút.";
            string supplies = $"Cỏ: {Count(inventory, hay)}  ·  Xô: {Count(inventory, emptyBucket)}  ·  Sữa: {Count(inventory, milk)}";
            string message = Time.time < feedbackUntil ? feedback : hint;
            string text = $"{supplies}   |   {message}";
            if (instructions.text != text) instructions.text = text;
        }

        public void BuyHay()
        {
            if (Near(shop)) Buy(InventorySystem.Instance, PlayerWallet.Instance, hay);
        }

        public void BuyBucket()
        {
            if (Near(shop)) Buy(InventorySystem.Instance, PlayerWallet.Instance, emptyBucket);
        }

        public void SellMilk()
        {
            if (Near(shop))
                Exchange(InventorySystem.Instance, PlayerWallet.Instance, milk, emptyBucket, milk.sellPrice, "Đã bán sữa");
        }

        public void FeedCow()
        {
            if (!Near(trough) || progress == null) return;
            int hungry = System.Array.IndexOf(progress, -1f);
            if (hungry < 0) return;
            var inventory = InventorySystem.Instance;
            int slot = FindItem(inventory, hay);
            if (slot < 0 || !inventory.RemoveFromSlot(slot)) { Show("Bạn cần cỏ khô."); return; }
            progress[hungry] = 0f;
            Show($"Đã cho bò {hungry + 1} ăn.");
        }

        public void MilkCow()
        {
            if (nearbyCow < 0 || !Near(milkingStalls[nearbyCow]) || progress[nearbyCow] < baseMilkSeconds) return;
            if (Exchange(InventorySystem.Instance, PlayerWallet.Instance, emptyBucket, milk, 0, "Đã vắt sữa"))
                progress[nearbyCow] = -1f;
        }

        bool Near(Transform station) =>
            player && ((Vector2)(player.transform.position - station.position)).sqrMagnitude <= interactionRange * interactionRange;

        static int FindItem(InventorySystem inventory, ItemData item) =>
            System.Array.FindIndex(inventory.slots, slot => slot != null && !slot.IsEmpty && slot.item == item);

        static int Count(InventorySystem inventory, ItemData item)
        {
            int count = 0;
            foreach (var slot in inventory.slots)
                if (slot != null && !slot.IsEmpty && slot.item == item) count += slot.amount;
            return count;
        }

        static bool HasSpace(InventorySystem inventory, ItemData item) =>
            System.Array.Exists(inventory.slots, slot => slot.IsEmpty || (slot.item == item && slot.amount < item.maxStack));

        void Buy(InventorySystem inventory, PlayerWallet wallet, ItemData item)
        {
            if (!HasSpace(inventory, item)) { Show("Túi đồ đã đầy."); return; }
            if (!wallet.TrySpend(Mathf.Max(0, item.buyPrice))) { Show("Không đủ vàng."); return; }
            inventory.AddItem(item);
            Show("Đã mua " + item.itemName);
        }

        bool Exchange(InventorySystem inventory, PlayerWallet wallet, ItemData input, ItemData output, int gold, string message)
        {
            int index = FindItem(inventory, input);
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
