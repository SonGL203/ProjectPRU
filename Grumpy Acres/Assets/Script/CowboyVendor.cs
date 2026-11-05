using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CowboyVendor : MonoBehaviour
{
    public string displayName = "Cowboy";
    public int tier;
    public int cost = 50;
    public Cowboy cowboyPrefab;

    bool playerInside;
    string message;
    float messageUntil;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

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
        if (kb != null && kb.fKey.wasPressedThisFrame) TryHire();
    }

    public bool TryHire()
    {
        if (CowboyRoster.Count >= GuardFrame.All.Count)
        {
            Say("Het cho! Hay xay them khung canh gac o nong trai.");
            return false;
        }

        var wallet = PlayerWallet.Instance;
        if (!wallet || !wallet.TrySpend(cost))
        {
            Say($"Khong du vang (can {cost}).");
            return false;
        }

        CowboyRoster.Add(tier);
        Say($"Da thue {displayName}! Ve nong trai, cowboy se tu vao khung.");
        return true;
    }

    void Say(string text)
    {
        message = text;
        messageUntil = Time.time + 2.5f;
    }

    [TextArea(2, 4)]
    public string recommend = "";

    static float EnemyHealth(string prefabName)
    {
        var sp = FindFirstObjectByType<WaveSpawner>();
        if (!sp) return 0f;
        int week = GameManager.HasInstance ? GameManager.Instance.week : 1;
        float mult = 1f;
        foreach (var r in sp.healthRules)
            if (r.prefabName == prefabName) mult = r.multiplier;
        return (sp.baseHealth + sp.healthPerWeek * (week - 1)) * mult;
    }

    void OnGUI()
    {
        string info = $"{displayName}  -  {cost} vang";
        if (cowboyPrefab)
        {
            float dmg = cowboyPrefab.bulletDamage, rate = 1f / cowboyPrefab.fireInterval;
            info += $"\nSat thuong {dmg:0} | {rate:0.0} phat/giay | Tam {cowboyPrefab.shootRange:0} | Mau {cowboyPrefab.maxHealth:0}";

            float green = EnemyHealth("Enemy2"), purple = EnemyHealth("Enemy1");
            if (green > 0f && purple > 0f)
            {
                int g = Mathf.CeilToInt(green / dmg), p = Mathf.CeilToInt(purple / dmg);
                info += $"\nHa Enemy xanh: {g} phat ({g * cowboyPrefab.fireInterval:0.0}s) | Enemy tim: {p} phat ({p * cowboyPrefab.fireInterval:0.0}s)";
            }
        }
        if (!string.IsNullOrEmpty(recommend)) info += "\nGoi y: " + recommend;

        if (playerInside)
        {
            int gold = PlayerWallet.Instance ? PlayerWallet.Instance.gold : 0;
            info += $"\n[F] Thue   (cho: {CowboyRoster.Count}/{GuardFrame.All.Count}, vang: {gold})";
            if (Time.time < messageUntil) info += "\n" + message;
        }

        WorldPrompt.Draw(transform.position + Vector3.up * 2.6f, info, 250f);
    }
}
