#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SetupMarketStalls
{
    static SetupMarketStalls()
    {
        EditorApplication.update += CheckAndRun;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update += CheckAndRun;
            }
        };
    }

    static void CheckAndRun()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= CheckAndRun;

        try
        {
            string marketPath = "Assets/Scenes/Market.unity";
            if (System.IO.File.Exists(marketPath))
            {
                string content = System.IO.File.ReadAllText(marketPath);
                if (!content.Contains("DetailedFarmDecor") || !content.Contains("DenseForest_Borders") || !content.Contains("Background_Grass"))
                {
                    Debug.Log("<color=cyan>[SetupMarketStalls] Phat hien Market.unity chua co canh quan chi tiet, dang tu dong cai dat...</color>");
                    Setup();
                    return;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SetupMarketStalls] Check error: " + ex.Message);
        }
    }

    [MenuItem("Tools/Farm/Cai dat cho Market va Sap hat giong %&m")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SetupMarketStalls] Vui long tat che do Play truoc khi cai dat scene!");
            return;
        }

        EditorSceneManager.SaveOpenScenes();
        SetupMarketScene();
        SetupSampleScene();
        Debug.Log("<color=green>[SetupMarketStalls] Hoan tat cai dat Market: Canh quan chi tiet 100% giong Farm (Da, Bui cay, Hoa, Co, Rung bao quanh, Hang rao)!</color>");
    }

    static void SetupMarketScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Market.unity", OpenSceneMode.Single);

        var rootObj = GameObject.Find("MarketRoot");
        if (!rootObj) rootObj = new GameObject("MarketRoot");
        rootObj.transform.position = new Vector3(200f, 0f, 0f);

        var oldCam = GameObject.Find("Main Camera");
        if (oldCam) Object.DestroyImmediate(oldCam);

        var camObj = GameObject.Find("MarketCamera");
        if (!camObj) camObj = new GameObject("MarketCamera");
        camObj.transform.SetParent(rootObj.transform, false);
        camObj.transform.localPosition = new Vector3(8.415f, 5.85f, -10f);

        var cam = camObj.GetComponent<Camera>();
        if (!cam) cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.75f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.355f, 0.655f, 0.255f, 1f);

        var listener = camObj.GetComponent<AudioListener>();
        if (!listener) listener = camObj.AddComponent<AudioListener>();

        var standaloneHelper = camObj.GetComponent<StandaloneMarketCamera>();
        if (!standaloneHelper) standaloneHelper = camObj.AddComponent<StandaloneMarketCamera>();

        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go == rootObj) continue;
            if (go.transform.position.x >= 190f)
                go.transform.SetParent(rootObj.transform, true);
            else
                go.transform.SetParent(rootObj.transform, false);
        }

        var uiWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/ui_white.png");

        // 1. Nen co xanh vo tan (Background_Grass) xoa sach 100% khoang xanh duong
        SetupBackgroundGrass(rootObj.transform, uiWhite);

        // 2. MarketView & Spawn: Can chinh khung hinh dep mat bao tron toan bo khu cho
        var viewObj = GameObject.Find("MarketView");
        if (!viewObj)
        {
            viewObj = new GameObject("MarketView");
            viewObj.transform.SetParent(rootObj.transform, false);
        }
        else viewObj.transform.SetParent(rootObj.transform, false);
        viewObj.transform.localPosition = new Vector3(8.415f, 5.85f, 0f);

        var view = viewObj.GetComponent<MarketView>();
        if (!view) view = viewObj.AddComponent<MarketView>();
        view.orthoSize = 5.75f;

        var spawnObj = GameObject.Find("MarketSpawn");
        if (!spawnObj)
        {
            spawnObj = new GameObject("MarketSpawn");
            spawnObj.transform.SetParent(viewObj.transform, false);
        }
        else spawnObj.transform.SetParent(viewObj.transform, false);
        spawnObj.transform.localPosition = new Vector3(11.88f - 8.415f, 2.0f - 5.85f, 0f);
        view.spawn = spawnObj.transform;

        // 3. Exit_To_Farm tai con duong phia Nam
        var exitObj = GameObject.Find("Exit_To_Farm");
        if (!exitObj)
        {
            exitObj = new GameObject("Exit_To_Farm");
            exitObj.transform.SetParent(rootObj.transform, false);
        }
        else exitObj.transform.SetParent(rootObj.transform, false);
        exitObj.transform.localPosition = new Vector3(11.88f, 0.495f, 0f);

        var st = exitObj.GetComponent<SceneTransition>();
        if (st) Object.DestroyImmediate(st);

        var portal = exitObj.GetComponent<MarketPortal>();
        if (!portal) portal = exitObj.AddComponent<MarketPortal>();
        portal.toMarket = false;
        portal.targetMarketScene = "Market";
        portal.requireEnemiesCleared = false;

        var exitCol = exitObj.GetComponent<BoxCollider2D>();
        if (!exitCol) exitCol = exitObj.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector2(3.5f, 1.2f);
        exitCol.offset = Vector2.zero;

        // Bien chi duong PortalLabel: "← Về nông trại" chuan CowboyMarket
        var oldPortalLabel = rootObj.transform.Find("PortalLabel_Exit");
        if (oldPortalLabel) Object.DestroyImmediate(oldPortalLabel.gameObject);

        var portalLabelObj = new GameObject("PortalLabel_Exit");
        portalLabelObj.transform.SetParent(rootObj.transform, false);
        portalLabelObj.transform.localPosition = new Vector3(11.88f, 1.8f, 0f);
        portalLabelObj.AddComponent<WorldLabel>();

        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var fontMat = font ? font.material : null;

        var plShadow = new GameObject("Shadow");
        plShadow.transform.SetParent(portalLabelObj.transform, false);
        plShadow.transform.localPosition = new Vector3(0.02f, -0.02f, 0f);
        var plShadowTm = plShadow.AddComponent<TextMesh>();
        if (font) plShadowTm.font = font;
        if (fontMat) plShadow.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        plShadowTm.text = "← Về nông trại";
        plShadowTm.fontSize = 58;
        plShadowTm.characterSize = 0.045f;
        plShadowTm.anchor = TextAnchor.MiddleCenter;
        plShadowTm.alignment = TextAlignment.Center;
        plShadowTm.color = new Color(0f, 0f, 0f, 0.9f);
        plShadowTm.fontStyle = FontStyle.Bold;
        var plShadowMr = plShadow.GetComponent<MeshRenderer>();
        if (plShadowMr) { plShadowMr.sortingLayerName = "Default"; plShadowMr.sortingOrder = 3200; }

        var plText = new GameObject("Text");
        plText.transform.SetParent(portalLabelObj.transform, false);
        plText.transform.localPosition = Vector3.zero;
        var plTextTm = plText.AddComponent<TextMesh>();
        if (font) plTextTm.font = font;
        if (fontMat) plText.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        plTextTm.text = "← Về nông trại";
        plTextTm.fontSize = 58;
        plTextTm.characterSize = 0.045f;
        plTextTm.anchor = TextAnchor.MiddleCenter;
        plTextTm.alignment = TextAlignment.Center;
        plTextTm.color = new Color(1f, 0.9f, 0.5f, 1f);
        plTextTm.fontStyle = FontStyle.Bold;
        var plTextMr = plText.GetComponent<MeshRenderer>();
        if (plTextMr) { plTextMr.sortingLayerName = "Default"; plTextMr.sortingOrder = 3201; }

        // 4. Sprites va Items
        var seedTomato = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Seeds/Seed_Tomato.asset");
        var seedCarrot = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Seeds/Seed_Carrot.asset");
        var seedWheat  = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Seeds/Seed_Wheat.asset");

        // 3 sap ban hat giong tai vi tri counter hop ly (localPos.y = 8.2f)
        BuildStall(rootObj.transform, "Sap_CaChua", new Vector3(3.465f, 8.2f, 0f),
            seedTomato, "Hạt Cà Chua", "Cây trồng nhiều vụ · Lớn sau 4 ngày\nThu hoạch: Cà chua (Bán 25 vàng / quả)\nLợi nhuận: +15 vàng mỗi vụ thu hoạch", uiWhite, font, fontMat);

        BuildStall(rootObj.transform, "Sap_CaRot", new Vector3(8.415f, 8.2f, 0f),
            seedCarrot, "Hạt Cà Rốt", "Nông sản tươi giòn · Lớn sau 3 ngày\nThu hoạch: Cà rốt (Bán 20 vàng / củ)\nLợi nhuận: +10 vàng mỗi củ bán ra", uiWhite, font, fontMat);

        BuildStall(rootObj.transform, "Sap_LuaMi", new Vector3(13.365f, 8.2f, 0f),
            seedWheat, "Hạt Lúa Mì", "Cực kỳ dễ trồng · Lớn nhanh 2 ngày\nThu hoạch: Lúa mì (Bán 18 vàng / bó)\nThích hợp thu hồi vốn nhanh", uiWhite, font, fontMat);

        // 5. Rung cay day dac bao quanh khu cho (chong lo khoang xanh o 4 huong)
        SetupDenseForestBorders(rootObj.transform);

        // 6. Trang tri canh quan chi tiet nhu Farm (Da, Bui cay tron, Hoa, Co tu nhien)
        SetupDetailedFarmLikeDecor(rootObj.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SetupBackgroundGrass(Transform root, Sprite uiWhite)
    {
        var oldBg = root.Find("Background_Grass");
        if (oldBg) Object.DestroyImmediate(oldBg.gameObject);

        var bgObj = new GameObject("Background_Grass");
        bgObj.transform.SetParent(root, false);
        bgObj.transform.localPosition = new Vector3(8.5f, 6.0f, 0f);
        bgObj.transform.localScale = new Vector3(80f, 60f, 1f);

        var sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = uiWhite;
        sr.color = new Color(0.355f, 0.655f, 0.255f, 1f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -100;
    }

    static void SetupDenseForestBorders(Transform root)
    {
        var oldForest = root.Find("DenseForest_Borders");
        if (oldForest) Object.DestroyImmediate(oldForest.gameObject);

        var forestObj = new GameObject("DenseForest_Borders");
        forestObj.transform.SetParent(root, false);

        var treeSprites = new Sprite[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree0.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree1.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree2.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilemap/Generated/Sprites/tree3.png")
        };

        // Cay rung day dac phia Nam (duoi duong cay cu, bao trum Y tu -0.8 den -3.5)
        float[] southY = { -0.7f, -1.5f, -2.3f, -3.1f };
        foreach (float y in southY)
        {
            for (float x = -3.8f; x <= 9.8f; x += 0.88f)
            {
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
            }
            for (float x = 13.8f; x <= 21.0f; x += 0.88f)
            {
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
            }
        }

        // Cay rung day dac phia Bac (sau cac sap hang va nha go, Y tu 12.8 den 16)
        float[] northY = { 12.8f, 13.8f, 14.8f, 15.8f };
        foreach (float y in northY)
        {
            for (float x = -3.8f; x <= 21.0f; x += 0.95f)
            {
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), false);
            }
        }

        // Cay rung day dac phia Tay (x tu -0.8 den -3.8)
        float[] westX = { -0.7f, -1.6f, -2.5f, -3.4f };
        foreach (float x in westX)
        {
            for (float y = -3.2f; y <= 15.0f; y += 1.0f)
            {
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
            }
        }

        // Cay rung day dac phia Dong (x tu 17.5 den 21)
        float[] eastX = { 17.5f, 18.6f, 19.8f, 21.0f };
        foreach (float x in eastX)
        {
            for (float y = -3.2f; y <= 15.0f; y += 1.0f)
            {
                CreateTree(forestObj.transform, treeSprites, new Vector3(x, y, 0f), true);
            }
        }
    }

    static void CreateTree(Transform parent, Sprite[] sprites, Vector3 pos, bool addCollider)
    {
        var tree = new GameObject("Tree_Outer");
        tree.transform.SetParent(parent, false);
        float rX = ((pos.x * 13.37f) % 0.15f);
        float rY = ((pos.y * 17.43f) % 0.15f);
        tree.transform.localPosition = new Vector3(pos.x + rX, pos.y + rY, 0f);

        int idx = Mathf.Abs((int)(pos.x * 7 + pos.y * 11)) % sprites.Length;
        var sr = tree.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[idx] ?? sprites[0];
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 2000 - (int)(pos.y * 10);

        float s = 1.05f + Mathf.Abs(((pos.x + pos.y) * 3.1f) % 0.1f);
        tree.transform.localScale = new Vector3(s, s, 1f);

        if (addCollider)
        {
            var col = tree.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.5f);
            col.offset = new Vector2(0f, -0.3f);
        }
    }

    static void SetupDetailedFarmLikeDecor(Transform root)
    {
        MarketStandaloneSupport.EnsureHarmoniousDecor(root);
    }

    static void BuildStall(Transform root, string name, Vector3 localPos, ItemData seed, string title, string desc, Sprite uiWhite, Font font, Material fontMat)
    {
        var stallObj = GameObject.Find(name);
        if (!stallObj)
        {
            stallObj = new GameObject(name);
            stallObj.transform.SetParent(root, false);
        }
        else stallObj.transform.SetParent(root, false);
        stallObj.transform.localPosition = localPos;

        var col = stallObj.GetComponent<BoxCollider2D>();
        if (!col) col = stallObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3.0f, 2.4f);
        col.offset = new Vector2(0f, -0.4f);

        var vendor = stallObj.GetComponent<SeedVendor>();
        if (!vendor) vendor = stallObj.AddComponent<SeedVendor>();
        vendor.seed = seed;
        vendor.amount = 1;
        vendor.customPrice = 10;

        // 1. NameLabel (Hien thi khi player dung xa, tren mai che sap hang)
        var oldLabel = stallObj.transform.Find("NameLabel");
        if (oldLabel) Object.DestroyImmediate(oldLabel.gameObject);

        var nameLabelObj = new GameObject("NameLabel");
        nameLabelObj.transform.SetParent(stallObj.transform, false);
        nameLabelObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        // Tam bien go sang trong (Plate)
        var plateObj = new GameObject("Plate");
        plateObj.transform.SetParent(nameLabelObj.transform, false);
        plateObj.transform.localPosition = Vector3.zero;
        plateObj.transform.localScale = new Vector3(4.1f, 0.72f, 1f);
        var plateSr = plateObj.AddComponent<SpriteRenderer>();
        plateSr.sprite = uiWhite;
        plateSr.color = new Color(0.12f, 0.09f, 0.06f, 0.88f);
        plateSr.sortingLayerName = "Default";
        plateSr.sortingOrder = 3200;

        // Vien vang duoi tam bien
        var plateBorder = new GameObject("PlateBorder");
        plateBorder.transform.SetParent(nameLabelObj.transform, false);
        plateBorder.transform.localPosition = new Vector3(0f, -0.34f, 0f);
        plateBorder.transform.localScale = new Vector3(4.0f, 0.025f, 1f);
        var pbSr = plateBorder.AddComponent<SpriteRenderer>();
        pbSr.sprite = uiWhite;
        pbSr.color = new Color(0.85f, 0.68f, 0.25f, 0.9f);
        pbSr.sortingLayerName = "Default";
        pbSr.sortingOrder = 3201;

        // Icon hat giong nho ben trai bien
        if (seed && seed.icon)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(nameLabelObj.transform, false);
            iconObj.transform.localPosition = new Vector3(-1.65f, 0f, 0f);
            iconObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            var iconSr = iconObj.AddComponent<SpriteRenderer>();
            iconSr.sprite = seed.icon;
            iconSr.sortingLayerName = "Default";
            iconSr.sortingOrder = 3202;
        }

        // Shadow text
        var shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(nameLabelObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.18f, -0.02f, 0f);
        var shadowTm = shadowObj.AddComponent<TextMesh>();
        if (font) shadowTm.font = font;
        if (fontMat) shadowObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        shadowTm.text = $"{title}  –  10 vàng";
        shadowTm.fontSize = 55;
        shadowTm.characterSize = 0.033f;
        shadowTm.anchor = TextAnchor.MiddleCenter;
        shadowTm.alignment = TextAlignment.Center;
        shadowTm.color = new Color(0f, 0f, 0f, 0.95f);
        shadowTm.fontStyle = FontStyle.Bold;
        var shadowMr = shadowObj.GetComponent<MeshRenderer>();
        if (shadowMr) { shadowMr.sortingLayerName = "Default"; shadowMr.sortingOrder = 3202; }

        // Main text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(nameLabelObj.transform, false);
        textObj.transform.localPosition = new Vector3(0.16f, 0f, 0f);
        var mainTm = textObj.AddComponent<TextMesh>();
        if (font) mainTm.font = font;
        if (fontMat) textObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        mainTm.text = $"{title}  –  10 vàng";
        mainTm.fontSize = 55;
        mainTm.characterSize = 0.033f;
        mainTm.anchor = TextAnchor.MiddleCenter;
        mainTm.alignment = TextAlignment.Center;
        mainTm.color = new Color(1f, 0.88f, 0.45f, 1f);
        mainTm.fontStyle = FontStyle.Bold;
        var textMr = textObj.GetComponent<MeshRenderer>();
        if (textMr) { textMr.sortingLayerName = "Default"; textMr.sortingOrder = 3203; }

        vendor.nameLabel = nameLabelObj;

        // 2. InfoPanel (Bang thong tin RPG hien len ngay truoc mat khi player lai gan quay)
        string panelName = $"InfoPanel_{name}";
        var oldPanel = root.Find(panelName);
        if (oldPanel) Object.DestroyImmediate(oldPanel.gameObject);

        var panelObj = new GameObject(panelName);
        panelObj.transform.SetParent(root, false);
        panelObj.transform.localPosition = new Vector3(localPos.x, 8.85f, 0f);

        var infoPanel = panelObj.AddComponent<InfoPanel>();

        // Background
        var bgObj = new GameObject("BG");
        bgObj.transform.SetParent(panelObj.transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(4.4f, 2.85f, 1f);
        var bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = uiWhite;
        bgSr.color = new Color(0.07f, 0.08f, 0.12f, 0.95f);
        bgSr.sortingLayerName = "Default";
        bgSr.sortingOrder = 3300;

        // Accent gold line
        var accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(panelObj.transform, false);
        accentObj.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        accentObj.transform.localScale = new Vector3(4.1f, 0.035f, 1f);
        var accentSr = accentObj.AddComponent<SpriteRenderer>();
        accentSr.sprite = uiWhite;
        accentSr.color = new Color(0.95f, 0.75f, 0.25f, 1f);
        accentSr.sortingLayerName = "Default";
        accentSr.sortingOrder = 3301;

        // O vuong chua Icon hat giong
        var iconFrameObj = new GameObject("IconFrame");
        iconFrameObj.transform.SetParent(panelObj.transform, false);
        iconFrameObj.transform.localPosition = new Vector3(-1.58f, 0.18f, 0f);
        iconFrameObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        var ifSr = iconFrameObj.AddComponent<SpriteRenderer>();
        ifSr.sprite = uiWhite;
        ifSr.color = new Color(0.15f, 0.18f, 0.25f, 0.9f);
        ifSr.sortingLayerName = "Default";
        ifSr.sortingOrder = 3301;

        if (seed && seed.icon)
        {
            var pIconObj = new GameObject("Icon");
            pIconObj.transform.SetParent(panelObj.transform, false);
            pIconObj.transform.localPosition = new Vector3(-1.58f, 0.18f, 0f);
            pIconObj.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            var pIconSr = pIconObj.AddComponent<SpriteRenderer>();
            pIconSr.sprite = seed.icon;
            pIconSr.sortingLayerName = "Default";
            pIconSr.sortingOrder = 3302;
        }

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        titleObj.transform.localPosition = new Vector3(-0.95f, 1.15f, 0f);
        var titleTm = titleObj.AddComponent<TextMesh>();
        if (font) titleTm.font = font;
        if (fontMat) titleObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        titleTm.text = $"{title}  –  10 vàng";
        titleTm.fontSize = 54;
        titleTm.characterSize = 0.033f;
        titleTm.anchor = TextAnchor.UpperLeft;
        titleTm.alignment = TextAlignment.Left;
        titleTm.color = new Color(1f, 0.88f, 0.42f, 1f);
        titleTm.fontStyle = FontStyle.Bold;
        var titleMr = titleObj.GetComponent<MeshRenderer>();
        if (titleMr) { titleMr.sortingLayerName = "Default"; titleMr.sortingOrder = 3302; }

        // Body
        var bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panelObj.transform, false);
        bodyObj.transform.localPosition = new Vector3(-0.95f, 0.68f, 0f);
        var bodyTm = bodyObj.AddComponent<TextMesh>();
        if (font) bodyTm.font = font;
        if (fontMat) bodyObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        bodyTm.text = desc;
        bodyTm.fontSize = 42;
        bodyTm.characterSize = 0.026f;
        bodyTm.anchor = TextAnchor.UpperLeft;
        bodyTm.alignment = TextAlignment.Left;
        bodyTm.color = new Color(0.88f, 0.92f, 0.96f, 1f);
        bodyTm.lineSpacing = 1.15f;
        var bodyMr = bodyObj.GetComponent<MeshRenderer>();
        if (bodyMr) { bodyMr.sortingLayerName = "Default"; bodyMr.sortingOrder = 3302; }

        // Footer with LiveText
        var footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(panelObj.transform, false);
        footerObj.transform.localPosition = new Vector3(-1.95f, -0.92f, 0f);
        var footerTm = footerObj.AddComponent<TextMesh>();
        if (font) footerTm.font = font;
        if (fontMat) footerObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        footerTm.text = "[F] Mua x1 (10v) · [Shift+F] x5 · Vàng: 0";
        footerTm.fontSize = 42;
        footerTm.characterSize = 0.027f;
        footerTm.anchor = TextAnchor.UpperLeft;
        footerTm.alignment = TextAlignment.Left;
        footerTm.color = new Color(0.45f, 0.95f, 0.55f, 1f);
        footerTm.fontStyle = FontStyle.Bold;
        var liveText = footerObj.AddComponent<LiveText>();
        liveText.format = "[F] Mua x1 (10v) · [Shift+F] x5 · Vàng: {0}";
        var footerMr = footerObj.GetComponent<MeshRenderer>();
        if (footerMr) { footerMr.sortingLayerName = "Default"; footerMr.sortingOrder = 3302; }

        // Thong bao phan hoi nhanh (AutoHide)
        var msgBoughtObj = new GameObject("MsgBought");
        msgBoughtObj.transform.SetParent(panelObj.transform, false);
        msgBoughtObj.transform.localPosition = new Vector3(-1.95f, -0.52f, 0f);
        var mbTm = msgBoughtObj.AddComponent<TextMesh>();
        if (font) mbTm.font = font;
        if (fontMat) msgBoughtObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        mbTm.text = "✓ Đã mua thành công!";
        mbTm.fontSize = 42;
        mbTm.characterSize = 0.027f;
        mbTm.color = new Color(0.3f, 1f, 0.45f, 1f);
        mbTm.fontStyle = FontStyle.Bold;
        var mbMr = msgBoughtObj.GetComponent<MeshRenderer>();
        if (mbMr) { mbMr.sortingLayerName = "Default"; mbMr.sortingOrder = 3303; }
        var mbAh = msgBoughtObj.AddComponent<AutoHide>();
        mbAh.seconds = 1.5f;
        msgBoughtObj.SetActive(false);

        var msgNoGoldObj = new GameObject("MsgNoGold");
        msgNoGoldObj.transform.SetParent(panelObj.transform, false);
        msgNoGoldObj.transform.localPosition = new Vector3(-1.95f, -0.52f, 0f);
        var mgTm = msgNoGoldObj.AddComponent<TextMesh>();
        if (font) mgTm.font = font;
        if (fontMat) msgNoGoldObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        mgTm.text = "✗ Không đủ vàng!";
        mgTm.fontSize = 42;
        mgTm.characterSize = 0.027f;
        mgTm.color = new Color(1f, 0.35f, 0.35f, 1f);
        mgTm.fontStyle = FontStyle.Bold;
        var mgMr = msgNoGoldObj.GetComponent<MeshRenderer>();
        if (mgMr) { mgMr.sortingLayerName = "Default"; mgMr.sortingOrder = 3303; }
        var mgAh = msgNoGoldObj.AddComponent<AutoHide>();
        mgAh.seconds = 1.5f;
        msgNoGoldObj.SetActive(false);

        var msgBagFullObj = new GameObject("MsgBagFull");
        msgBagFullObj.transform.SetParent(panelObj.transform, false);
        msgBagFullObj.transform.localPosition = new Vector3(-1.95f, -0.52f, 0f);
        var mfTm = msgBagFullObj.AddComponent<TextMesh>();
        if (font) mfTm.font = font;
        if (fontMat) msgBagFullObj.GetComponent<MeshRenderer>().sharedMaterial = fontMat;
        mfTm.text = "✗ Túi đồ đã đầy!";
        mfTm.fontSize = 42;
        mfTm.characterSize = 0.027f;
        mfTm.color = new Color(1f, 0.65f, 0.2f, 1f);
        mfTm.fontStyle = FontStyle.Bold;
        var mfMr = msgBagFullObj.GetComponent<MeshRenderer>();
        if (mfMr) { mfMr.sortingLayerName = "Default"; mfMr.sortingOrder = 3303; }
        var mfAh = msgBagFullObj.AddComponent<AutoHide>();
        mfAh.seconds = 1.5f;
        msgBagFullObj.SetActive(false);

        panelObj.SetActive(false);
        vendor.panel = infoPanel;
        vendor.msgBought = mbAh;
        vendor.msgNoGold = mgAh;
        vendor.msgBagFull = mfAh;
    }

    static void SetupSampleScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

        var portalObj = GameObject.Find("MarketPortal_Sell");
        if (portalObj)
        {
            var st = portalObj.GetComponent<SceneTransition>();
            if (st) Object.DestroyImmediate(st);

            var portal = portalObj.GetComponent<MarketPortal>();
            if (!portal) portal = portalObj.AddComponent<MarketPortal>();
            portal.toMarket = true;
            portal.targetMarketScene = "Market";
            portal.requireEnemiesCleared = false;

            var col = portalObj.GetComponent<BoxCollider2D>();
            if (!col) col = portalObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.5f, 2f);
            col.offset = Vector2.zero;

            var returnObj = portalObj.transform.Find("ReturnPoint");
            if (!returnObj)
            {
                var rGo = new GameObject("ReturnPoint");
                rGo.transform.SetParent(portalObj.transform, false);
                rGo.transform.localPosition = new Vector3(0f, -2.5f, 0f);
                returnObj = rGo.transform;
            }
            portal.returnPoint = returnObj;
        }

        // Nang do cao Hotbar trong Canvas
        var hotbar = Object.FindFirstObjectByType<HotbarUI>();
        if (hotbar)
        {
            hotbar.bottomOffset = 26f;
            EditorUtility.SetDirty(hotbar);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
