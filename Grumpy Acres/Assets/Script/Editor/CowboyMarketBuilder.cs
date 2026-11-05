#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CowboyMarketBuilder
{
    const string ScenePath = "Assets/Scenes/CowboyMarket.unity";
    const string TilesPath = "Assets/Tilemap/Tiles";
    const string SpritePath = "Assets/Tilemap/Generated/Sprites";
    const int W = 24, H = 13;
    static readonly Vector3 WorldOffset = new(100f, 0f, 0f);

    struct VendorDef { public string name; public int tier; public int cost; public float x; public string recommend; }

    static readonly VendorDef[] Vendors =
    {
        new VendorDef { name = "Cowboy ao trang", tier = 0, cost = 50,  x = 5.5f,
            recommend = "Re, dan yeu. Hop diet Enemy xanh (yeu) khi chua nhieu vang; gap Enemy tim ban rat lau." },
        new VendorDef { name = "Cowboy ao vang", tier = 1, cost = 90,  x = 12f,
            recommend = "Can bang. Ha Enemy xanh 2 phat, Enemy tim 4 phat. Dung duoc ca hai loai." },
        new VendorDef { name = "Cowboy manh nhat", tier = 2, cost = 150, x = 18.5f,
            recommend = "Ban nhanh, dan manh, tam xa. Hop diet Enemy tim (trau) 3 phat/con va giu chot kho." },
    };

    [MenuItem("Tools/Farm/6. Build Cowboy Market")]
    public static void Build()
    {
        var cowboys = new Cowboy[3];
        for (int i = 0; i < 3; i++)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Cowboy{i + 1}.prefab");
            cowboys[i] = go ? go.GetComponent<Cowboy>() : null;
            if (cowboys[i] == null) { Debug.LogError("Thieu Assets/Prefabs/Cowboy1-3. Chay menu 4. Build Cowboys truoc."); return; }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        var root = new GameObject("MarketRoot");
        root.transform.position = WorldOffset;

        var gridGo = new GameObject("Grid", typeof(Grid));
        gridGo.transform.SetParent(root.transform, false);
        gridGo.GetComponent<Grid>().cellSize = new Vector3(1f, 1f, 1f);
        var ground = NewMap(gridGo, "Ground", 0);
        var path = NewMap(gridGo, "Path", 1);

        var grass = AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesPath}/Grass.asset");
        var sand = AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesPath}/Path.asset");
        var objects = new GameObject("Objects").transform;
        objects.SetParent(root.transform, false);
        var rng = new System.Random(5);

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                ground.SetTile(cell, grass);
                bool exit = y == 0 && x >= 10 && x <= 13;
                bool road = y == 4 || y == 5 || (x >= 10 && x <= 13 && y < 6);
                if (road || exit) path.SetTile(cell, sand);

                bool border = x == 0 || x == W - 1 || y == H - 1 || (y == 0 && !exit);
                if (border) Prop(objects, "Tree", "tree" + rng.Next(4), new Vector3(x + 0.5f, y, 0f), 1.08f, 6, new Vector2(0.6f, 0.35f));
            }

        foreach (var v in Vendors)
            MakeVendor(root.transform, objects, v, cowboys[v.tier]);

        MakeFrameVendor(root.transform, objects);

        var markers = new GameObject("Markers").transform;
        markers.SetParent(root.transform, false);

        var viewGo = new GameObject("MarketView", typeof(MarketView));
        viewGo.transform.SetParent(markers, false);
        viewGo.transform.localPosition = new Vector3(W / 2f, H / 2f, 0f);
        var spawn = new GameObject("Spawn").transform;
        spawn.SetParent(markers, false);
        spawn.localPosition = new Vector3(12f, 2.5f, 0f);
        var view = viewGo.GetComponent<MarketView>();
        view.spawn = spawn;
        view.orthoSize = 7.4f;

        var exitGo = new GameObject("Exit_To_Farm", typeof(BoxCollider2D), typeof(MarketPortal));
        exitGo.transform.SetParent(markers, false);
        exitGo.transform.localPosition = new Vector3(12f, 0.3f, 0f);
        var box = exitGo.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(4f, 1.2f);
        var portal = exitGo.GetComponent<MarketPortal>();
        portal.toMarket = false;
        portal.label = "Ve nong trai";

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);

        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        if (!list.Exists(s => s.path == "Assets/Scenes/SampleScene.unity")) list.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("Da tao scene Assets/Scenes/CowboyMarket.unity va them vao Build Settings.");
    }

    static void MakeVendor(Transform root, Transform objects, VendorDef v, Cowboy cowboyPrefab)
    {
        const float stallY = 8f;
        Prop(objects, "Stall", "stall", new Vector3(v.x, stallY, 0f), 1f, 3, new Vector2(2.9f, 1.5f), new Vector2(0f, 0.75f));

        var npc = new GameObject("Vendor_" + v.tier, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CowboyVendor));
        npc.transform.SetParent(root, false);
        npc.transform.localPosition = new Vector3(v.x, stallY + 0.5f, 0f);
        npc.transform.localScale = Vector3.one * 1.4f;

        var sr = npc.GetComponent<SpriteRenderer>();
        var src = cowboyPrefab.GetComponent<SpriteRenderer>();
        sr.sprite = src ? src.sprite : null;
        sr.sortingOrder = 4;

        var col = npc.GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3.2f, 3f) / 1.4f;
        col.offset = new Vector2(0f, -1.6f) / 1.4f;

        var vendor = npc.GetComponent<CowboyVendor>();
        vendor.displayName = v.name;
        vendor.tier = v.tier;
        vendor.cost = v.cost;
        vendor.recommend = v.recommend;
        vendor.cowboyPrefab = cowboyPrefab;
    }

    static void MakeFrameVendor(Transform root, Transform objects)
    {
        const float stallY = 8f, x = 21.5f;
        Prop(objects, "FrameStall", "stall", new Vector3(x, stallY, 0f), 1f, 3, new Vector2(2.9f, 1.5f), new Vector2(0f, 0.75f));

        var npc = new GameObject("Vendor_Frame", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(FrameVendor));
        npc.transform.SetParent(root, false);
        npc.transform.localPosition = new Vector3(x, stallY + 0.5f, 0f);
        npc.transform.localScale = Vector3.one * 0.5f;

        var sr = npc.GetComponent<SpriteRenderer>();
        sr.sprite = Spr("house");
        sr.sortingOrder = 4;

        var col = npc.GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3.2f, 3f) / 0.5f;
        col.offset = new Vector2(0f, -1.6f) / 0.5f;
    }

    static Tilemap NewMap(GameObject grid, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid.transform, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }

    static void Prop(Transform parent, string name, string sprite, Vector3 localPos, float scale, int order, Vector2? colSize, Vector2 colOffset = default)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = Spr(sprite);
        sr.sortingOrder = order;
        if (colSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colSize.Value / scale;
            box.offset = colOffset / scale;
        }
    }

    static Sprite Spr(string name)
    {
        string p = $"{SpritePath}/{name}.png";
        AssetDatabase.ImportAsset(p);
        var imp = (TextureImporter)AssetImporter.GetAtPath(p);
        var ts = new TextureImporterSettings();
        imp.ReadTextureSettings(ts);
        if (imp.textureType != TextureImporterType.Sprite || ts.spriteAlignment != (int)SpriteAlignment.BottomCenter || imp.spritePixelsPerUnit != 100f)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.ReadTextureSettings(ts);
            ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            imp.SetTextureSettings(ts);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }
}
#endif
