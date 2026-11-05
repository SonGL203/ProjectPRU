#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public static class SellMapsBuilder
{
    const float Cell = 0.99f;
    const string TilesPath = "Assets/Tilemap/Tiles";
    const string GenPath = "Assets/Tilemap/Generated";
    const string SpritePath = "Assets/Tilemap/Generated/Sprites";

    static readonly string[] Market =
    {
        "TTTTTTTTTTTTTTTTTTTTTTTT",
        "TggggggggggggggggHHHHggT",
        "TgSSSggSSSggSSSggHHHHggT",
        "TgSNSggSNSggSNSggHHDHggT",
        "TppppppppppppppppppppppT",
        "TggfggggggpWWpggggggfggT",
        "TggggggggggpWWpggggggggT",
        "TgggggggggppppgggggggggT",
        "TgggggggggppppgggggggggT",
        "Tg$$ggggggppppggggggFFgT",
        "TgggggggggppppgggggggggT",
        "TgggggggggppppgggggggggT",
        "TTTTTTTTTTEEEETTTTTTTTTT",
    };

    class Layers
    {
        public Tilemap ground, path;
        public Transform objects;
        public GameObject markers;
    }

    [MenuItem("Tools/Farm/2. Build Sell Map (Market)")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(GenPath);
        AssetDatabase.Refresh();

        BuildMarket("Assets/Scenes/Market.unity");

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == "Assets/Scenes/Market.unity"))
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Market.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("Da tao scene Market, them vao Build Settings.");
    }

    static void BuildMarket(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rows = Market;
        int h = rows.Length, w = rows[0].Length;

        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 7f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;
        cam.transform.position = new Vector3(w * Cell / 2f, h * Cell / 2f, -10f);

        var light = new GameObject("Global Light 2D", typeof(Light2D));
        light.GetComponent<Light2D>().lightType = Light2D.LightType.Global;

        var gridGo = new GameObject("Grid", typeof(Grid));
        gridGo.GetComponent<Grid>().cellSize = new Vector3(Cell, Cell, 1f);

        var L = new Layers
        {
            ground = NewMap(gridGo, "Ground", 0),
            path = NewMap(gridGo, "Path", 1),
            objects = new GameObject("Objects").transform,
            markers = new GameObject("Markers"),
        };

        var sand = Solid("Sand", new Color(0.82f, 0.62f, 0.32f));
        var sandAlt = Solid("SandAlt", new Color(0.84f, 0.65f, 0.35f));
        var grass = Load("Grass");
        var rng = new System.Random(3);

        var cells = new Dictionary<char, List<Vector3Int>>();
        for (int r = 0; r < h; r++)
            for (int x = 0; x < w; x++)
            {
                char ch = rows[r][x];
                var cell = new Vector3Int(x, h - 1 - r, 0);
                L.ground.SetTile(cell, grass);
                if (ch == 'p' || ch == 'E') L.path.SetTile(cell, (x + r) % 2 == 0 ? sand : sandAlt);
                if (ch == 'T') Tree(L, cell, rng);
                if (ch == 'f') Prop(L, "Flower", "flower" + rng.Next(4), BottomCenter(cell), 1.5f, null);
                if (ch == 'F') Prop(L, "Fence", "fence", BottomCenter(cell), 2f, new Vector2(0.9f, 0.35f));
                if (!cells.ContainsKey(ch)) cells[ch] = new List<Vector3Int>();
                cells[ch].Add(cell);
            }

        foreach (var n in cells['N'])
            Prop(L, "Stall", "stall", new Vector3((n.x + 0.5f) * Cell, n.y * Cell, 0f), 1f, new Vector2(2.9f, 1.5f), new Vector2(0f, 0.75f));

        var door = cells['D'][0];
        Prop(L, "House", "house", new Vector3(19f * Cell, door.y * Cell + 0.06f, 0f), 1.95f, new Vector2(3.6f, 1.6f), new Vector2(0f, 0.8f));

        Prop(L, "Fountain", "pond", new Vector3(12f * Cell, 7f * Cell, 0f), 1.83f, new Vector2(2.1f, 2.1f), Vector2.zero, true);

        var bin = cells['$'];
        Prop(L, "ShippingBin", "bin", new Vector3((bin[0].x + 1f) * Cell, bin[0].y * Cell, 0f), 1f, new Vector2(1.8f, 0.8f), new Vector2(0f, 0.4f));

        MarketMarkers(cells, L);
        EditorSceneManager.SaveScene(scene, path);
    }

    static Tilemap NewMap(GameObject grid, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid.transform, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }

    static Vector3 BottomCenter(Vector3Int c) => new((c.x + 0.5f) * Cell, c.y * Cell, 0f);

    static void Tree(Layers L, Vector3Int cell, System.Random rng) =>
        Prop(L, "Tree", "tree" + rng.Next(4), BottomCenter(cell), 1.07f, new Vector2(0.6f, 0.35f));

    static void Prop(Layers L, string name, string sprite, Vector3 pos, float scale, Vector2? colSize,
        Vector2 colOffset = default, bool centerPivot = false)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(L.objects, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = Spr(sprite, centerPivot);
        sr.sortingOrder = centerPivot ? 5 : 2000 - Mathf.RoundToInt(pos.y * 100f);
        if (colSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colSize.Value / scale;
            box.offset = colOffset / scale;
        }
    }

    static void MarketMarkers(Dictionary<char, List<Vector3Int>> cells, Layers L)
    {
        Trigger<SceneTransition>(L, "Exit_To_Farm", cells['E'], 0, t => { t.targetScene = "SampleScene"; t.targetSpawn = ""; });
        foreach (var n in cells['N']) Trigger<SellZone>(L, "SellZone_Stall", new List<Vector3Int> { n }, 1, z => z.zoneName = "Sap cho");
        Trigger<SellZone>(L, "SellZone_Bin", cells['$'], 1, z => z.zoneName = "Thung ban");
        Spawn(L, "Spawn_FromFarm", new Vector3Int(11, 2, 0), "FromFarm", true);
    }

    static void Spawn(Layers L, string name, Vector3Int cell, string id, bool isDefault)
    {
        var go = new GameObject(name, typeof(SpawnPoint));
        go.transform.SetParent(L.markers.transform, false);
        go.transform.position = new Vector3((cell.x + 0.5f) * Cell, (cell.y + 0.5f) * Cell, 0f);
        var sp = go.GetComponent<SpawnPoint>();
        sp.id = id;
        sp.isDefault = isDefault;
    }

    static void Trigger<T>(Layers L, string name, List<Vector3Int> cells, int pad, System.Action<T> init) where T : Component
    {
        if (cells == null || cells.Count == 0) return;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in cells)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        var go = new GameObject(name, typeof(BoxCollider2D), typeof(T));
        go.transform.SetParent(L.markers.transform, false);
        go.transform.position = new Vector3((minX + maxX + 1) * Cell / 2f, (minY + maxY + 1) * Cell / 2f, 0f);
        var box = go.GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2((maxX - minX + 1 + 2 * pad) * Cell, (maxY - minY + 1 + 2 * pad) * Cell);
        init(go.GetComponent<T>());
    }

    static TileBase Load(string name) => AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesPath}/{name}.asset");

    static Sprite Spr(string name, bool centerPivot)
    {
        string p = $"{SpritePath}/{name}.png";
        AssetDatabase.ImportAsset(p);
        var imp = (TextureImporter)AssetImporter.GetAtPath(p);
        var align = centerPivot ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
        var ts = new TextureImporterSettings();
        imp.ReadTextureSettings(ts);
        if (imp.textureType != TextureImporterType.Sprite || ts.spriteAlignment != (int)align || imp.spritePixelsPerUnit != 100f)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.ReadTextureSettings(ts);
            ts.spriteAlignment = (int)align;
            imp.SetTextureSettings(ts);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    static TileBase Solid(string name, Color color)
    {
        string tilePath = $"{GenPath}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (existing) return existing;

        string pngPath = $"{GenPath}/{name}.png";
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = color;
        tex.SetPixels(px);
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(pngPath);

        var imp = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 4f / Cell;
        imp.filterMode = FilterMode.Point;
        imp.SaveAndReimport();

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        AssetDatabase.CreateAsset(tile, tilePath);
        return tile;
    }
}
#endif
