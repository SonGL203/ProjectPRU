using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketStandaloneSupport
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Market") return;

        // Neu mo truc tiep scene Market va bam Play
        SetupMarketAtRuntime();
    }

    public static void SetupMarketAtRuntime()
    {
        var root = GameObject.Find("MarketRoot");
        Transform parent = root ? root.transform : null;

        // 1. Dam bao Camera hoat dong neu mo truc tiep
        EnsureCamera(parent);

        // 2. Dam bao co Player (neu choi truc tiep tu scene Market)
        EnsurePlayer(parent);

        // 3. Dam bao co nen co xanh Background_Grass
        EnsureBackgroundGrass(parent);

        // 4. Dam bao rung cay day dac bao quanh (chong lo khoang xanh 4 huong)
        EnsureDenseForest(parent);

        // 5. Dam bao canh quan hai hoa, em diu: BO HET DA XUNG DOT, chi giu bui cay xanh, hoa nho xinh va hang rao
        EnsureHarmoniousDecor(parent);
    }

    static void EnsureCamera(Transform parent)
    {
        var cam = Camera.main;
        if (!cam)
        {
            var camObj = new GameObject("MarketCamera");
            if (parent) camObj.transform.SetParent(parent, false);
            camObj.transform.localPosition = new Vector3(8.415f, 5.85f, -10f);

            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.355f, 0.655f, 0.255f, 1f);
        }
        else
        {
            cam.backgroundColor = new Color(0.355f, 0.655f, 0.255f, 1f);
        }

        // Luon dam bao chi co dung 1 AudioListener trong toan bo game
        var allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (allListeners.Length > 1)
        {
            for (int i = 1; i < allListeners.Length; i++)
            {
                if (allListeners[i] != null) Object.Destroy(allListeners[i]);
            }
        }
        else if (allListeners.Length == 0)
        {
            var camTarget = Camera.main;
            if (camTarget) camTarget.gameObject.AddComponent<AudioListener>();
        }
    }

    static void EnsurePlayer(Transform parent)
    {
        var existingPlayer = Object.FindFirstObjectByType<PlayerMove>();
        if (existingPlayer) return;

        // Tao InventorySystem neu chua co
        if (!InventorySystem.Instance)
        {
            var invObj = new GameObject("InventorySystem");
            invObj.AddComponent<InventorySystem>();
        }

        // Tao Player tai vi tri cong cho (x = 11.88, y = 2.5)
        var playerObj = new GameObject("Player");
        if (parent) playerObj.transform.SetParent(parent, false);
        playerObj.transform.localPosition = new Vector3(11.88f, 2.5f, 0f);
        playerObj.tag = "Player";

        var sr = playerObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 2000;

#if UNITY_EDITOR
        var playerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Character/Basic Charakter Spritesheet.png");
        if (playerSprite) sr.sprite = playerSprite;

        var anim = playerObj.AddComponent<Animator>();
        var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Player/PlayerController.controller");
        if (ctrl) anim.runtimeAnimatorController = ctrl;
#endif

        var rb = playerObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = playerObj.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.offset = new Vector2(0f, -0.1f);

        playerObj.AddComponent<PlayerMove>();

        var wallet = playerObj.AddComponent<PlayerWallet>();
        wallet.gold = 200;

        Debug.Log("<color=green>[MarketStandaloneSupport] Da tu dong tao Player (co san 200 vang) tai Market!</color>");
    }

    static void EnsureBackgroundGrass(Transform parent)
    {
        if (GameObject.Find("Background_Grass")) return;

        var bgObj = new GameObject("Background_Grass");
        if (parent) bgObj.transform.SetParent(parent, false);
        bgObj.transform.localPosition = new Vector3(8.5f, 6.0f, 0f);
        bgObj.transform.localScale = new Vector3(100f, 80f, 1f);

        var sr = bgObj.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(2, 2);
        var green = new Color(0.355f, 0.655f, 0.255f, 1f);
        tex.SetPixels(new Color[] { green, green, green, green });
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -100;
    }

    static void EnsureDenseForest(Transform parent)
    {
        if (GameObject.Find("DenseForest_Borders")) return;

#if UNITY_EDITOR
        var forestObj = new GameObject("DenseForest_Borders");
        if (parent) forestObj.transform.SetParent(parent, false);

        var treeSprites = new Sprite[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree0.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree1.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree2.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree3.png")
        };

        // Phia Nam
        float[] southY = { -0.7f, -1.5f, -2.3f, -3.1f };
        foreach (float y in southY)
        {
            for (float x = -3.8f; x <= 9.8f; x += 0.88f)
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
            for (float x = 13.8f; x <= 21.0f; x += 0.88f)
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
        }

        // Phia Bac
        float[] northY = { 12.8f, 13.8f, 14.8f, 15.8f };
        foreach (float y in northY)
        {
            for (float x = -3.8f; x <= 21.0f; x += 0.95f)
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), false);
        }

        // Phia Tay
        float[] westX = { -0.7f, -1.6f, -2.5f, -3.4f };
        foreach (float x in westX)
        {
            for (float y = -3.2f; y <= 15.0f; y += 1.0f)
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
        }

        // Phia Dong
        float[] eastX = { 17.5f, 18.6f, 19.8f, 21.0f };
        foreach (float x in eastX)
        {
            for (float y = -3.2f; y <= 15.0f; y += 1.0f)
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
        }
#endif
    }

    public static void EnsureHarmoniousDecor(Transform parent)
    {
        // 1. Don sach decor cu
        var oldDecor = GameObject.Find("DetailedFarmDecor");
        if (oldDecor) Object.Destroy(oldDecor);

        // 2. Xoa sach 100% tat ca cac cuc da xung dot trong scene
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go != null && (go.name.StartsWith("Rock") || go.name.Contains("_Rock") || go.name == "Rock"))
            {
                Object.Destroy(go);
            }
        }

#if UNITY_EDITOR
        var decorObj = new GameObject("DetailedFarmDecor");
        if (parent) decorObj.transform.SetParent(parent, false);

        var allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/Serene_Village_16x16.png");
        var spriteMap = new Dictionary<string, Sprite>();
        foreach (var obj in allSprites)
        {
            if (obj is Sprite sp) spriteMap[sp.name] = sp;
        }

        // Bui cay tron xanh (sv_2_13) dep nhe nhang, tu nhien, mem mai
        spriteMap.TryGetValue("sv_2_13", out var roundBush);

        // Hoa dại nho xinh (flower0..3)
        var flowerSprites = new Sprite[]
        {
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/flower0.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/flower1.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/flower2.png"),
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/flower3.png")
        };

        // --- A. BUI CAY TRON XANH: Bố trí theo cụm tao nha, khong gian thoang dang ---
        var bushData = new (Vector3 pos, float scale)[]
        {
            // Cum 3 bui cay bo trai (tao goc cong vien nghi ngoi)
            (new Vector3(2.0f, 6.0f, 0f), 1.25f),
            (new Vector3(2.8f, 6.6f, 0f), 1.05f),
            (new Vector3(1.6f, 7.0f, 0f), 1.35f),

            // 2 bui cay canh ghe go
            (new Vector3(4.8f, 3.2f, 0f), 1.15f),
            (new Vector3(2.0f, 3.2f, 0f), 1.20f),

            // Cac bui cay diem xuyet goc bo ho nuoc
            (new Vector3(6.2f, 6.8f, 0f), 1.15f),
            (new Vector3(6.2f, 4.2f, 0f), 1.10f),
            (new Vector3(10.5f, 4.2f, 0f), 1.15f),

            // Cum bui cay bo phai doc theo hang cay phia Dong
            (new Vector3(16.5f, 5.5f, 0f), 1.30f),
            (new Vector3(15.8f, 6.2f, 0f), 1.10f),
            (new Vector3(16.8f, 6.8f, 0f), 1.25f),

            // Bui cay canh hang rao phai
            (new Vector3(14.0f, 2.5f, 0f), 1.15f),
            (new Vector3(16.5f, 2.6f, 0f), 1.20f),

            // Bui cay canh nha go
            (new Vector3(15.2f, 7.8f, 0f), 1.25f)
        };

        for (int i = 0; i < bushData.Length; i++)
        {
            var bGo = new GameObject($"Bush_{i + 1}");
            bGo.transform.SetParent(decorObj.transform, false);
            bGo.transform.localPosition = bushData[i].pos;
            bGo.transform.localScale = new Vector3(bushData[i].scale, bushData[i].scale, 1f);

            var bSr = bGo.AddComponent<SpriteRenderer>();
            bSr.sprite = roundBush ?? flowerSprites[0];
            bSr.sortingLayerName = "Default";
            bSr.sortingOrder = 130;
        }

        // --- B. HOA TIEU BIEU NHO XINH (Scale 0.85f): Diem xuyen nhe nhang tren tham co ---
        Vector3[] flowerPositions = new Vector3[]
        {
            new(2.5f, 5.0f, 0f),
            new(4.2f, 5.8f, 0f),
            new(3.2f, 4.2f, 0f),
            new(5.0f, 2.6f, 0f),
            new(6.2f, 5.4f, 0f),
            new(10.5f, 5.6f, 0f),
            new(14.2f, 4.8f, 0f),
            new(15.5f, 5.6f, 0f),
            new(16.2f, 4.0f, 0f),
            new(14.8f, 3.2f, 0f)
        };

        for (int i = 0; i < flowerPositions.Length; i++)
        {
            var fGo = new GameObject($"Flower_{i + 1}");
            fGo.transform.SetParent(decorObj.transform, false);
            fGo.transform.localPosition = flowerPositions[i];
            fGo.transform.localScale = new Vector3(0.85f, 0.85f, 1f); // Nho xinh, thanh thoat

            var fSr = fGo.AddComponent<SpriteRenderer>();
            fSr.sprite = flowerSprites[i % flowerSprites.Length];
            fSr.sortingLayerName = "Default";
            fSr.sortingOrder = 120;
        }

        // --- C. HANG RAO GO ben trai canh ghe ngoi ---
        var fenceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/fence.png");
        if (fenceSprite)
        {
            var fenceGo = new GameObject("Fence_Left");
            fenceGo.transform.SetParent(decorObj.transform, false);
            fenceGo.transform.localPosition = new Vector3(3.8f, 3.2f, 0f);
            var sr = fenceGo.AddComponent<SpriteRenderer>();
            sr.sprite = fenceSprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 1700;

            var col = fenceGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 0.2f);
        }
#endif
    }

    static void CreateTree(Transform parent, Sprite[] sprites, Vector3 pos, bool addCollider)
    {
        var tree = new GameObject("Tree_Outer");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = pos;

        int idx = Mathf.Abs((int)(pos.x * 7 + pos.y * 11)) % sprites.Length;
        var sr = tree.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[idx] ?? sprites[0];
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 2000 - (int)(pos.y * 10);

        float s = 1.08f;
        tree.transform.localScale = new Vector3(s, s, 1f);

        if (addCollider)
        {
            var col = tree.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.5f);
            col.offset = new Vector2(0f, -0.3f);
        }
    }
}
