using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MarketStandaloneSupport : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CheckActiveScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckActiveScene(scene);
    }

    static void CheckActiveScene(Scene scene)
    {
        if (scene.name != "Market" && scene.name != "CowboyMarket") return;
        SetupMarketAtRuntime();
    }

    public static void SetupMarketAtRuntime()
    {
        // 1. Don dep blocker cu tren loi di
        CleanupOldBlockers();

        // 2. Chuan hoa Camera va mau nen co xanh
        EnsureCamera();

        // 3. Dam bao vi tien PlayerWallet
        EnsureWallet();

        // 4. Dam bao Player xuat hien day du de dieu khien di chuyen va tuong tac
        EnsurePlayer();

        // 5. Trang tri dep hon voi hoa, da canh va bui co (Co - Da - Hoa)
        EnsureDecorations();

        // 6. Don dep UI SeasonClockUI de man hinh Market luon thoang dang, khong che giua man hinh
        CleanupSeasonClockUI();

        // 7. Dam bao thanh item HotbarUI va he thong Inventory luon hien thi day du tren man hinh
        EnsureUI();

        // 8. Khu trung lap EventSystem, Hotbar, AudioListener
        CleanupDuplicates();
    }

    static void CleanupSeasonClockUI()
    {
        // Xoa bat ky UI SeasonClock / Panel Mua nao trong Market de man hinh hoan toan thoang dang
        var clocks = Object.FindObjectsByType<SeasonClockUI>(FindObjectsSortMode.None);
        foreach (var c in clocks)
        {
            if (c != null) Object.Destroy(c.gameObject);
        }

        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go == null) continue;
            string n = go.name;
            if (n == "SeasonClockUI" || n == "SeasonClock" || n == "SeasonClockPanel" || n == "SeasonText" || n == "DayText")
            {
                Object.Destroy(go);
            }
        }

        // Dam bao SeasonManager hoat dong ngam cho logic tro choi
        if (SeasonManager.Instance == null && Object.FindFirstObjectByType<SeasonManager>() == null)
        {
            var smGO = new GameObject("SeasonManager", typeof(SeasonManager));
            var sm = smGO.GetComponent<SeasonManager>();
            sm.currentSeason = Season.Spring;
            sm.daysPerSeason = 7;
            sm.currentDayInSeason = 1;
        }

        // Dam bao DayNightCycle hoat dong ngam cho anh sang
        if (DayNightCycle.Instance == null && Object.FindFirstObjectByType<DayNightCycle>() == null)
        {
            var dncGO = new GameObject("DayNightCycle", typeof(DayNightCycle));
            var dnc = dncGO.GetComponent<DayNightCycle>();
            dnc.globalLight = Object.FindFirstObjectByType<UnityEngine.Rendering.Universal.Light2D>();
        }
    }

    static void EnsureUI()
    {
        // 1. Dam bao co EventSystem de tuong tac chuot / ban phim
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length == 0)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        // 2. Dam bao he thong kho do InventorySystem ton tai
        if (InventorySystem.Instance == null && Object.FindFirstObjectByType<InventorySystem>() == null)
        {
            var invGO = new GameObject("InventorySystem", typeof(InventorySystem));
            Object.DontDestroyOnLoad(invGO);
        }

        // 3. Dam bao UI Canvas de hien thi thanh item
        var canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
        {
            canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 4. Dam bao thanh item HotbarUI o duoi man hinh
        var hotbars = Object.FindObjectsByType<HotbarUI>(FindObjectsSortMode.None);
        if (hotbars.Length == 0)
        {
            var hotbarGO = new GameObject("HotbarUI", typeof(RectTransform), typeof(HotbarUI));
            hotbarGO.transform.SetParent(canvasGO.transform, false);
        }

        // 5. Dam bao InventoryUI de mo kho
        var invUIs = Object.FindObjectsByType<InventoryUI>(FindObjectsSortMode.None);
        if (invUIs.Length == 0)
        {
            var invUIGO = new GameObject("InventoryUI", typeof(RectTransform), typeof(InventoryUI));
            invUIGO.transform.SetParent(canvasGO.transform, false);
        }
    }

    static void CleanupOldBlockers()
    {
        var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj == null) continue;
            string n = obj.name.ToLower();
            if (n.StartsWith("decor_")) continue; // Khong xoa vat pham trang tri!

            if (n.Contains("boulder") || n.StartsWith("sv_0_24") || n.Contains("blocker_rock"))
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
            cam.orthographic = true;
            cam.orthographicSize = 5.2f; // Zoom gan lai de nhin ro nhan vat va cac sap hang

            // Gan CameraFollow de camera roll theo nhan vat nhu ben nong trai
            var follow = cam.GetComponent<CameraFollow>();
            if (!follow) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.smoothSpeed = 12f;
            follow.clampToBounds = true;
            follow.minX = 7.5f;
            follow.maxX = 16.5f;
            follow.minY = 4.2f;
            follow.maxY = 7.8f;
            follow.SnapToTarget();

            var bgQuad = GameObject.Find("Background_Grass_Quad");
            if (!bgQuad)
            {
                bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bgQuad.name = "Background_Grass_Quad";
                bgQuad.transform.position = new Vector3(12f, 6f, 10f);
                bgQuad.transform.localScale = new Vector3(120f, 90f, 1f);

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
            wallet.gold = 300;
        }
        else if (PlayerWallet.Instance != null && PlayerWallet.Instance.gold <= 0)
        {
            PlayerWallet.Instance.Add(300);
        }
    }

    static void EnsurePlayer()
    {
        if (Object.FindFirstObjectByType<PlayerMove>() != null) return;

        // 1. Uu tien Instantiate tu Prefab trong Resources hoac Prefabs
        var playerPrefab = Resources.Load<GameObject>("Player");
        if (playerPrefab != null)
        {
            var p = Object.Instantiate(playerPrefab, new Vector3(11.385f, 2.475f, 0f), Quaternion.identity);
            p.name = "Player";
            p.tag = "Player";

            // Snap camera follow ngay vao vi tri player
            var cam = Camera.main;
            if (cam)
            {
                var follow = cam.GetComponent<CameraFollow>();
                if (follow)
                {
                    follow.target = p.transform;
                    follow.SnapToTarget();
                }
            }
            return;
        }

        // 2. Fallback neu chua load duoc prefab: tao day du component
        var playerGO = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(Animator), typeof(PlayerMove), typeof(PlayerHealth));
        playerGO.transform.position = new Vector3(11.385f, 2.475f, 0f);
        playerGO.tag = "Player";

        var rb = playerGO.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = playerGO.GetComponent<BoxCollider2D>();
        box.size = new Vector2(0.6f, 0.35f);
        box.offset = new Vector2(0f, -0.35f);

        var sr = playerGO.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        var camFallback = Camera.main;
        if (camFallback)
        {
            var follow = camFallback.GetComponent<CameraFollow>();
            if (follow)
            {
                follow.target = playerGO.transform;
                follow.SnapToTarget();
            }
        }
    }

    public static void EnsureDecorations()
    {
        if (GameObject.Find("Market_Decorations") != null) return;

        var root = new GameObject("Market_Decorations");
        var objectsParent = GameObject.Find("Objects");
        if (objectsParent != null) root.transform.SetParent(objectsParent.transform, false);

        // Load tat ca cac Sprite trang tri tu Resources va AssetDatabase
        var spriteMap = new Dictionary<string, Sprite>();
        string[] spriteNames = new string[] { "flower0", "flower1", "flower2", "flower3", "rock0", "rock1", "grass0", "grass1" };

        foreach (var sName in spriteNames)
        {
            var sp = Resources.Load<Sprite>($"Decorations/{sName}");
#if UNITY_EDITOR
            if (sp == null)
            {
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Tilemap/Generated/Sprites/{sName}.png");
            }
#endif
            if (sp != null) spriteMap[sName.ToLower()] = sp;
        }

        if (spriteMap.Count < spriteNames.Length)
        {
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var sp in allSprites)
            {
                if (sp == null) continue;
                string n = sp.name.ToLower();
                if (!spriteMap.ContainsKey(n)) spriteMap[n] = sp;
            }
        }

        // Danh sach toa do cac cum trang tri Co - Da - Hoa quanh ho va khu cho
        var items = new (string spriteKey, Vector3 pos, Vector3 scale)[]
        {
            // --- HOA (Flowers) ---
            // Cum hoa ben trai bo ao
            ("flower0", new Vector3(8.8f, 6.2f, 0f), new Vector3(1.4f, 1.4f, 1f)),
            ("flower1", new Vector3(8.2f, 5.5f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("flower2", new Vector3(7.3f, 6.0f, 0f), new Vector3(1.5f, 1.5f, 1f)),
            ("flower3", new Vector3(6.5f, 5.2f, 0f), new Vector3(1.4f, 1.4f, 1f)),
            ("flower0", new Vector3(4.0f, 6.5f, 0f), new Vector3(1.4f, 1.4f, 1f)),

            // Cum hoa ben phai bo ao
            ("flower2", new Vector3(14.2f, 6.5f, 0f), new Vector3(1.4f, 1.4f, 1f)),
            ("flower1", new Vector3(14.8f, 5.8f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("flower3", new Vector3(15.5f, 5.0f, 0f), new Vector3(1.4f, 1.4f, 1f)),
            ("flower1", new Vector3(16.3f, 5.3f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("flower0", new Vector3(17.1f, 4.9f, 0f), new Vector3(1.4f, 1.4f, 1f)),
            ("flower2", new Vector3(20.8f, 5.2f, 0f), new Vector3(1.4f, 1.4f, 1f)),

            // Hoa hai ben sap hang
            ("flower0", new Vector3(3.8f, 8.6f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("flower1", new Vector3(7.4f, 8.6f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("flower3", new Vector3(15.4f, 8.6f, 0f), new Vector3(1.3f, 1.3f, 1f)),

            // --- DA CANH (Ornamental Rocks & River Stones) ---
            // Da bo vien quanh 4 goc ho nuoc
            ("rock1", new Vector3(9.7f, 7.3f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock1", new Vector3(13.1f, 7.3f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock1", new Vector3(9.7f, 4.7f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock1", new Vector3(13.1f, 4.7f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock0", new Vector3(9.4f, 6.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("rock0", new Vector3(13.4f, 6.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),

            // Da tu nhien tren bai co tay va dong
            ("rock0", new Vector3(2.2f, 3.2f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock0", new Vector3(5.8f, 5.2f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("rock1", new Vector3(8.2f, 2.2f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock0", new Vector3(14.8f, 3.6f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("rock1", new Vector3(16.8f, 2.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("rock0", new Vector3(21.2f, 3.6f, 0f), new Vector3(1.3f, 1.3f, 1f)),

            // --- BUI CO (Grass Tufts & Clover) ---
            // Bai co tay
            ("grass0", new Vector3(1.8f, 5.0f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass1", new Vector3(2.5f, 2.0f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass0", new Vector3(5.0f, 3.5f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass1", new Vector3(6.2f, 2.5f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass0", new Vector3(8.4f, 5.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),

            // Quanh bo ao
            ("grass1", new Vector3(10.1f, 4.5f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("grass0", new Vector3(12.7f, 4.5f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("grass0", new Vector3(10.1f, 7.5f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("grass1", new Vector3(12.7f, 7.5f, 0f), new Vector3(1.2f, 1.2f, 1f)),

            // Bai co dong va vuon nha
            ("grass0", new Vector3(14.2f, 4.4f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass1", new Vector3(17.0f, 1.8f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass0", new Vector3(19.8f, 1.8f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass1", new Vector3(18.5f, 6.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("grass0", new Vector3(21.5f, 2.2f, 0f), new Vector3(1.3f, 1.3f, 1f)),
            ("grass1", new Vector3(21.8f, 6.8f, 0f), new Vector3(1.3f, 1.3f, 1f)),

            // Ven duong di va ven sap
            ("grass0", new Vector3(3.2f, 7.5f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("grass0", new Vector3(11.0f, 1.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),
            ("grass1", new Vector3(12.6f, 1.0f, 0f), new Vector3(1.2f, 1.2f, 1f)),
        };

        foreach (var item in items)
        {
            var go = new GameObject($"Decor_{item.spriteKey}");
            go.transform.SetParent(root.transform, false);
            go.transform.position = item.pos;
            go.transform.localScale = item.scale;

            var sr = go.AddComponent<SpriteRenderer>();
            if (spriteMap.TryGetValue(item.spriteKey.ToLower(), out var sprite) && sprite != null)
            {
                sr.sprite = sprite;
            }
            sr.sortingOrder = 100 + Mathf.RoundToInt((15f - item.pos.y) * 10f);
        }
    }

    static void CleanupDuplicates()
    {
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
                Object.Destroy(eventSystems[i].gameObject);
        }

        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length > 1)
        {
            for (int i = 1; i < listeners.Length; i++)
                Object.Destroy(listeners[i]);
        }

        var hotbars = Object.FindObjectsByType<HotbarUI>(FindObjectsSortMode.None);
        if (hotbars.Length > 1)
        {
            for (int i = 1; i < hotbars.Length; i++)
                Object.Destroy(hotbars[i].gameObject);
        }
    }
}
