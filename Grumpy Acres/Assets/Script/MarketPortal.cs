using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MarketPortal : MonoBehaviour
{
    public bool toMarket = true;
    [Tooltip("Ten scene cho can den (de trong = Market)")]
    public string targetMarketScene = "Market";

    [Tooltip("Bat buoc phai diet het quai vat tren nong trai moi duoc vao cho (mac dinh: false de di cho tu do)")]
    public bool requireEnemiesCleared = false;

    [Tooltip("Cho player xuat hien khi tro ve nong trai (phai nam ngoai vung cua)")]
    public Transform returnPoint;

    [Tooltip("Chu hien tren cua")]
    public string label = "";

    [Tooltip("Nhan chu (object trong scene)")]
    public WorldLabel labelObj;
    [Tooltip("Bang canh bao khi con enemy, chua di cho duoc (tuy chon)")]
    public GameObject warningObj;

    bool playerInside;
    static float lastTravelTime;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) playerInside = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>()) playerInside = false;
    }

    void Update()
    {
        if (!playerInside || MarketTravel.Busy) return;
        if (Time.time - lastTravelTime < 1.0f) return;

        if (!toMarket)
        {
            lastTravelTime = Time.time;
            if (MarketTravel.InMarket)
            {
                MarketTravel.Exit();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
            return;
        }

        if (MarketTravel.InMarket) return;
        if (requireEnemiesCleared && Enemy.Alive.Count > 0) return;

        lastTravelTime = Time.time;
        var back = returnPoint ? returnPoint.position : transform.position + Vector3.down * 3f;
        string sceneToLoad = string.IsNullOrEmpty(targetMarketScene) ? "Market" : targetMarketScene;
        MarketTravel.Enter(sceneToLoad, back);
    }

    void LateUpdate()
    {
        bool warn = toMarket && playerInside && requireEnemiesCleared && Enemy.Alive.Count > 0;
        if (warningObj && warningObj.activeSelf != warn) warningObj.SetActive(warn);
        if (labelObj)
        {
            bool show = !warn && toMarket != MarketTravel.InMarket;
            if (labelObj.gameObject.activeSelf != show) labelObj.gameObject.SetActive(show);
        }
    }
}
