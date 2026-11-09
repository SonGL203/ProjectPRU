using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MarketStandaloneSupport : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Market") return;

        SetupMarketAtRuntime();
    }

    public static void SetupMarketAtRuntime()
    {
        // 1. Loai bo moi vien da trong Market theo yeu cau cua user ("bo may cuc da di")
        RemoveAllRocks();

        // 2. Chuan hoa Camera va mau nen co xanh
        EnsureCamera();

        // 3. Dam bao vi tien PlayerWallet
        EnsureWallet();

        // 4. Dam bao Player xuat hien
        EnsurePlayer();

        // 5. Dam bao cac sap ban hat giong
        EnsureSeedStalls();

        // 6. Khu trung lap EventSystem, Hotbar, AudioListener
        CleanupDuplicates();
    }

    static void RemoveAllRocks()
    {
        var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj == null) continue;
            string n = obj.name.ToLower();
            if (n.Contains("rock") || n.Contains("boulder") || n.StartsWith("sv_0_24"))
            {
                Object.Destroy(obj);
            }
        }
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (!cam)
        {
            var camGO = GameObject.Find("Main Camera");
            if (camGO) cam = camGO.GetComponent<Camera>();
        }

        if (cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.355f, 0.655f, 0.255f, 1f);

            // Tao quad nen co phia sau de khong bao gio bi ho khoang den/xanh tren man hinh rong
            var bgQuad = GameObject.Find("Background_Grass_Quad");
            if (!bgQuad)
            {
                bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bgQuad.name = "Background_Grass_Quad";
                bgQuad.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);
                bgQuad.transform.localScale = new Vector3(80f, 60f, 1f);

                var collider = bgQuad.GetComponent<Collider>();
                if (collider) Object.Destroy(collider);

                var mr = bgQuad.GetComponent<MeshRenderer>();
                if (mr)
                {
                    mr.sortingOrder = -100;
                    var mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.355f, 0.655f, 0.255f, 1f);
                    mr.sharedMaterial = mat;
                }
            }
        }
    }

    static void EnsureWallet()
    {
        if (PlayerWallet.Instance == null && Object.FindFirstObjectByType<PlayerWallet>() == null)
        {
            var walletGO = new GameObject("PlayerWallet", typeof(PlayerWallet));
            var wallet = walletGO.GetComponent<PlayerWallet>();
            wallet.gold = 150;
            Object.DontDestroyOnLoad(walletGO);
        }
        else if (PlayerWallet.Instance != null && PlayerWallet.Instance.gold <= 0)
        {
            PlayerWallet.Instance.Add(150);
        }
    }

    static void EnsurePlayer()
    {
        if (Object.FindFirstObjectByType<PlayerMove>() != null) return;

        // Neu mo scene Market truc tiep ma chua qua Farm, tu dong tao Player
        var playerGO = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(Animator), typeof(PlayerMove), typeof(PlayerHealth));
        playerGO.transform.position = new Vector3(11.385f, 2.475f, 0f);

        var rb = playerGO.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = playerGO.GetComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.8f);
        box.offset = new Vector2(0f, -0.1f);

        var sr = playerGO.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Gan sprite player
        var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in sprites)
        {
            if (s.name == "Basic Charakter Spritesheet_0")
            {
                sr.sprite = s;
                break;
            }
        }

        // Gan animator controller neu tim thay
        var anim = playerGO.GetComponent<Animator>();
        var controllers = Resources.FindObjectsOfTypeAll<RuntimeAnimatorController>();
        foreach (var c in controllers)
        {
            if (c.name.Contains("PlayerController"))
            {
                anim.runtimeAnimatorController = c;
                break;
            }
        }
    }

    static void EnsureSeedStalls()
    {
        // Tim cac ItemData hat giong
        ItemData tomatoSeed = null;
        ItemData carrotSeed = null;
        ItemData wheatSeed = null;

#if UNITY_EDITOR
        tomatoSeed = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Seeds/Seed_Tomato.asset");
        carrotSeed = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Seeds/Seed_Carrot.asset");
        wheatSeed  = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Seeds/Seed_Wheat.asset");
#endif

        if (!tomatoSeed || !carrotSeed || !wheatSeed)
        {
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item.name.Contains("Tomato")) tomatoSeed = item;
                else if (item.name.Contains("Carrot")) carrotSeed = item;
                else if (item.name.Contains("Wheat")) wheatSeed = item;
            }
        }

        // Gan SeedVendor len 3 SellZone_Stall hoac Stall
        var sellZones = Object.FindObjectsByType<SellZone>(FindObjectsSortMode.None);
        foreach (var sz in sellZones)
        {
            if (sz.name != "SellZone_Stall") continue;

            float x = sz.transform.position.x;
            var vendor = sz.GetComponent<SeedVendor>();
            if (!vendor) vendor = sz.gameObject.AddComponent<SeedVendor>();

            if (x < 6f) // Sap Ca Chua (Ben trai, x ~ 3.465)
            {
                vendor.seedItem = tomatoSeed;
                vendor.customPrice = 15;
                vendor.stallName = "Sạp Hạt Giống Cà Chua";
            }
            else if (x < 11f) // Sap Ca Rot (O giua, x ~ 8.415)
            {
                vendor.seedItem = carrotSeed;
                vendor.customPrice = 12;
                vendor.stallName = "Sạp Hạt Giống Cà Rốt";
            }
            else // Sap Lua Mi (Ben phai, x ~ 13.365)
            {
                vendor.seedItem = wheatSeed;
                vendor.customPrice = 8;
                vendor.stallName = "Sạp Hạt Giống Lúa Mì";
            }
        }
    }

    static void CleanupDuplicates()
    {
        // 1 EventSystem duy nhat
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
                Object.Destroy(eventSystems[i].gameObject);
        }

        // 1 AudioListener duy nhat
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length > 1)
        {
            for (int i = 1; i < listeners.Length; i++)
                Object.Destroy(listeners[i]);
        }

        // 1 HotbarUI duy nhat
        var hotbars = Object.FindObjectsByType<HotbarUI>(FindObjectsSortMode.None);
        if (hotbars.Length > 1)
        {
            for (int i = 1; i < hotbars.Length; i++)
                Object.Destroy(hotbars[i].gameObject);
        }
    }
}
