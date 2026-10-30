using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class GrumpyTilemapBuilder
{
    const string SheetPath = "Assets/image/Serene_Village_16x16.png";
    const string MapPath = "Assets/Tilemap/FarmMap.txt";
    const string TileDir = "Assets/Tilemap/Tiles";
    const int Px = 16;

    // (col, row) tu goc tren-trai cua sheet, moi o 16px
    static readonly (int c, int r)[] GrassCells = { (3, 0), (4, 0), (5, 0), (3, 0), (4, 0), (5, 0), (4, 1), (4, 2) };

    // TL, T, TR, L, C, R, BL, B, BR
    static readonly (int c, int r)[] DirtCells = { (5, 2), (7, 3), (3, 2), (5, 3), (10, 2), (3, 3), (5, 1), (7, 1), (3, 1) };
    static readonly (int c, int r)[] PondCells = { (11, 0), (12, 0), (13, 0), (11, 1), (12, 1), (13, 1), (11, 2), (12, 2), (13, 2) };

    // ngang, doc, TL, TR, BL, BR
    static readonly (int c, int r)[] FenceCells = { (5, 15), (4, 16), (4, 15), (6, 15), (4, 17), (6, 17) };

    // ky tu map -> (col, row, rong, cao, chan duong)
    static readonly Dictionary<char, (int c, int r, int w, int h, bool solid)> Objects = new()
    {
        ['T'] = (9, 13, 2, 2, true),  // tree
        ['H'] = (0, 21, 3, 4, true),  // house (home base)
        ['m'] = (7, 16, 1, 2, true),  // mailbox
        ['n'] = (8, 13, 1, 1, true),  // sign
        ['r'] = (1, 15, 1, 1, true),  // rock
        ['R'] = (0, 15, 1, 1, true),  // mossy rock
        ['b'] = (7, 12, 1, 1, true),  // bush
        ['*'] = (2, 13, 1, 1, false), // flower
    };

    [MenuItem("Grumpy Acres/Tilemap/Build All")]
    public static void BuildAll()
    {
        SliceSheet();
        BuildMap();
    }

    [MenuItem("Grumpy Acres/Tilemap/1. Slice Serene Village 16x16")]
    public static void SliceSheet()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Px;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(SheetPath));
        var rects = new List<SpriteRect>();
        var ids = new List<SpriteNameFileIdPair>();
        for (int r = 0; r < tex.height / Px; r++)
        for (int c = 0; c < tex.width / Px; c++)
        {
            int x = c * Px, y = tex.height - (r + 1) * Px;
            if (IsEmpty(tex, x, y)) continue;
            var sr = new SpriteRect
            {
                name = SpriteName(c, r),
                rect = new Rect(x, y, Px, Px),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            };
            rects.Add(sr);
            ids.Add(new SpriteNameFileIdPair(sr.name, sr.spriteID));
        }
        Object.DestroyImmediate(tex);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();
        dp.SetSpriteRects(rects.ToArray());
        dp.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(ids);
        dp.Apply();
        importer.SaveAndReimport();
        Debug.Log($"[Tilemap] Sliced {rects.Count} sprites from {SheetPath}");
    }

    [MenuItem("Grumpy Acres/Tilemap/2. Build Farm Map In Open Scene")]
    public static void BuildMap()
    {
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
        if (!sprites.ContainsKey(SpriteName(4, 0)))
        {
            Debug.LogError("[Tilemap] Chua slice sheet");
            return;
        }
        Sprite S((int c, int r) cell) => sprites[SpriteName(cell.c, cell.r)];

        objectTiles.Clear();
        var grass = RandomTile("Grass", GrassCells.Select(S).ToArray());
        var path = EdgeTile("Path", DirtCells.Select(S).ToArray(), Tile.ColliderType.None);
        var soil = EdgeTile("Soil", DirtCells.Select(S).ToArray(), Tile.ColliderType.None);
        var water = EdgeTile("Water", PondCells.Select(S).ToArray(), Tile.ColliderType.Grid);
        var fence = FenceTile(FenceCells.Select(S).ToArray());

        string[] map = File.ReadAllLines(MapPath).Where(l => l.Length > 0).ToArray();
        int h = map.Length, w = map[0].Length;
        Vector3Int Cell(int x, int y) => new(x, h - 1 - y, 0);

        var old = GameObject.Find("FarmGrid");
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject("FarmGrid", typeof(Grid));
        var farm = root.AddComponent<FarmTilemap>();
        farm.ground = Layer(root, "Ground", 0, false);
        farm.path = Layer(root, "Path", 1, false);
        farm.soil = Layer(root, "Soil", 1, false);
        farm.water = Layer(root, "Water", 2, true);
        farm.decor = Layer(root, "Decor", 3, false);
        farm.obstacles = Layer(root, "Obstacles", 4, true);

        var spawns = new List<Transform>();
        var spawnRoot = new GameObject("EnemySpawns").transform;
        spawnRoot.SetParent(root.transform);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            char ch = map[y][x];
            var cell = Cell(x, y);
            farm.ground.SetTile(cell, grass);
            switch (ch)
            {
                case '=': farm.path.SetTile(cell, path); break;
                case 's': farm.soil.SetTile(cell, soil); break;
                case 'w': farm.water.SetTile(cell, water); break;
                case 'f': farm.obstacles.SetTile(cell, fence); break;
                case 'E':
                    farm.path.SetTile(cell, path);
                    spawns.Add(Marker($"Spawn_{spawns.Count}", farm.ground.GetCellCenterWorld(cell), spawnRoot));
                    break;
                case 'P':
                    farm.path.SetTile(cell, path);
                    Marker("PlayerSpawn", farm.ground.GetCellCenterWorld(cell), root.transform);
                    break;
            }
            if (!Objects.TryGetValue(ch, out var o)) continue;
            var target = o.solid ? farm.obstacles : farm.decor;
            for (int j = 0; j < o.h; j++)
            for (int i = 0; i < o.w; i++)
                target.SetTile(Cell(x + i, y + j), SingleTile(S((o.c + i, o.r + j)), o.solid));
            if (ch == 'H')
            {
                var center = (farm.ground.GetCellCenterWorld(Cell(x, y)) + farm.ground.GetCellCenterWorld(Cell(x + o.w - 1, y + o.h - 1))) / 2f;
                farm.homeBase = Marker("HomeBase", center, root.transform);
            }
        }
        farm.enemySpawns = spawns.ToArray();

        var cam = Camera.main;
        if (cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = h / 2f;
            cam.transform.position = new Vector3(w / 2f, h / 2f, -10f);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"[Tilemap] Built {w}x{h} farm map with {spawns.Count} enemy spawns");
    }

    static Tilemap Layer(GameObject root, string name, int order, bool solid)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(root.transform);
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        if (solid)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            go.AddComponent<CompositeCollider2D>();
        }
        return go.GetComponent<Tilemap>();
    }

    static Transform Marker(string name, Vector3 pos, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent);
        t.position = pos;
        return t;
    }

    static readonly Dictionary<Sprite, Tile> objectTiles = new();

    static Tile SingleTile(Sprite sprite, bool solid)
    {
        if (objectTiles.TryGetValue(sprite, out var cached)) return cached;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        return objectTiles[sprite] = Save(tile, $"{TileDir}/Objects/{sprite.name}.asset");
    }

    static RuleTile RandomTile(string name, Sprite[] sprites)
    {
        var tile = ScriptableObject.CreateInstance<RuleTile>();
        tile.m_DefaultSprite = sprites[0];
        var rule = new RuleTile.TilingRule
        {
            m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
            m_Sprites = sprites,
            m_PerlinScale = 0.9f,
            m_Neighbors = new List<int>(),
            m_NeighborPositions = new List<Vector3Int>(),
        };
        tile.m_TilingRules = new List<RuleTile.TilingRule> { rule };
        return Save(tile, $"{TileDir}/{name}.asset");
    }

    static RuleTile EdgeTile(string name, Sprite[] s, Tile.ColliderType collider)
    {
        var tile = ScriptableObject.CreateInstance<RuleTile>();
        tile.m_DefaultSprite = s[4];
        tile.m_DefaultColliderType = collider;
        tile.m_TilingRules = new List<RuleTile.TilingRule>
        {
            Rule(s[0], collider, (Up, No), (Left, No)),
            Rule(s[2], collider, (Up, No), (Right, No)),
            Rule(s[6], collider, (Down, No), (Left, No)),
            Rule(s[8], collider, (Down, No), (Right, No)),
            Rule(s[1], collider, (Up, No)),
            Rule(s[7], collider, (Down, No)),
            Rule(s[3], collider, (Left, No)),
            Rule(s[5], collider, (Right, No)),
        };
        return Save(tile, $"{TileDir}/{name}.asset");
    }

    static RuleTile FenceTile(Sprite[] s)
    {
        var grid = Tile.ColliderType.Grid;
        var tile = ScriptableObject.CreateInstance<RuleTile>();
        tile.m_DefaultSprite = s[0];
        tile.m_DefaultColliderType = grid;
        tile.m_TilingRules = new List<RuleTile.TilingRule>
        {
            Rule(s[2], grid, (Down, Yes), (Right, Yes), (Up, No), (Left, No)),
            Rule(s[3], grid, (Down, Yes), (Left, Yes), (Up, No), (Right, No)),
            Rule(s[4], grid, (Up, Yes), (Right, Yes), (Down, No), (Left, No)),
            Rule(s[5], grid, (Up, Yes), (Left, Yes), (Down, No), (Right, No)),
            Rule(s[1], grid, (Up, Yes), (Left, No), (Right, No)),
            Rule(s[1], grid, (Down, Yes), (Left, No), (Right, No)),
        };
        return Save(tile, $"{TileDir}/Fence.asset");
    }

    static readonly Vector3Int Up = Vector3Int.up, Down = Vector3Int.down, Left = Vector3Int.left, Right = Vector3Int.right;
    const int Yes = RuleTile.TilingRuleOutput.Neighbor.This;
    const int No = RuleTile.TilingRuleOutput.Neighbor.NotThis;

    static RuleTile.TilingRule Rule(Sprite sprite, Tile.ColliderType collider, params (Vector3Int pos, int match)[] neighbors) => new()
    {
        m_Sprites = new[] { sprite },
        m_ColliderType = collider,
        m_NeighborPositions = neighbors.Select(n => n.pos).ToList(),
        m_Neighbors = neighbors.Select(n => n.match).ToList(),
    };

    // ghi de asset cu de scene khong mat tham chieu
    static T Save<T>(T asset, string path) where T : Object
    {
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!existing)
        {
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        EditorUtility.CopySerialized(asset, existing);
        Object.DestroyImmediate(asset);
        return existing;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    static string SpriteName(int c, int r) => $"sv_{c}_{r}";

    static bool IsEmpty(Texture2D tex, int x, int y)
    {
        foreach (var p in tex.GetPixels(x, y, Px, Px))
            if (p.a > 0.01f) return false;
        return true;
    }
}
