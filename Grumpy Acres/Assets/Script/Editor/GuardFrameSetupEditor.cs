#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GuardFrameSetupEditor
{
    const string SpriteDir = "Assets/sprites/Tower";
    const string FramePrefabPath = "Assets/Prefabs/GuardFrame.prefab";

    static readonly Vector3[] FramePositions = { new(13f, 14f, 0f), new(27f, 14f, 0f) };
    static readonly Vector3[] SpotPositions = { new(8f, 12f, 0f), new(32f, 12f, 0f), new(20f, 20f, 0f) };
    static readonly Vector3 PortalPos = new(20f, 1.2f, 0f);

    [MenuItem("Tools/Farm/5. Setup Guard Frames")]
    public static void Setup()
    {
        var back = ImportSprite($"{SpriteDir}/FrameBack.png");
        var front = ImportSprite($"{SpriteDir}/FrameFront.png");
        var broken = ImportSprite($"{SpriteDir}/FrameBroken.png");
        var spotSprite = ImportSprite($"{SpriteDir}/TowerSpot.png");

        var cowboys = new Cowboy[3];
        for (int i = 0; i < 3; i++)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Cowboy{i + 1}.prefab");
            cowboys[i] = go ? go.GetComponent<Cowboy>() : null;
        }
        if (!back || !front || !broken || !spotSprite || cowboys[0] == null || cowboys[1] == null || cowboys[2] == null)
        {
            Debug.LogError("Thieu sprite khung rao hoac prefab Cowboy1-3. Chay menu 4. Build Cowboys truoc.");
            return;
        }

        var framePrefab = MakeFramePrefab(back, front, broken, cowboys);

        foreach (var c in Object.FindObjectsByType<Cowboy>(FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(c.gameObject);
        var old = GameObject.Find("Towers");
        if (old) Undo.DestroyObjectImmediate(old);
        var old2 = GameObject.Find("GuardFrames");
        if (old2) Undo.DestroyObjectImmediate(old2);

        var wallet = Object.FindFirstObjectByType<PlayerWallet>();
        if (!wallet)
        {
            var go = new GameObject("PlayerWallet");
            Undo.RegisterCreatedObjectUndo(go, "Setup wallet");
            wallet = go.AddComponent<PlayerWallet>();
        }
        wallet.gold = 150;

        var root = new GameObject("GuardFrames");
        Undo.RegisterCreatedObjectUndo(root, "Setup guard frames");

        for (int i = 0; i < FramePositions.Length; i++)
        {
            var f = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, root.transform);
            f.name = $"GuardFrame{i + 1}";
            f.transform.position = FramePositions[i];
        }

        for (int i = 0; i < SpotPositions.Length; i++)
            MakeSpot(root.transform, $"FrameSpot{i + 1}", SpotPositions[i], spotSprite, framePrefab);

        MakeMarketPortal(root.transform, spotSprite);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Da dat 2 khung canh gac, 3 nen dat xay them va cua Cowboy Market. Ctrl+S de luu scene. Chay tiep menu 6. Build Cowboy Market.");
    }

    static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) return null;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 16f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;

        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static GameObject MakeFramePrefab(Sprite back, Sprite front, Sprite broken, Cowboy[] cowboys)
    {
        var go = new GameObject("GuardFrame");

        var backGo = new GameObject("Back", typeof(SpriteRenderer));
        backGo.transform.SetParent(go.transform, false);
        var backSr = backGo.GetComponent<SpriteRenderer>();
        backSr.sprite = back;
        backSr.sortingOrder = 3;

        var frontGo = new GameObject("Front", typeof(SpriteRenderer));
        frontGo.transform.SetParent(go.transform, false);
        var frontSr = frontGo.GetComponent<SpriteRenderer>();
        frontSr.sprite = front;
        frontSr.sortingOrder = 6;

        var post = new GameObject("Post").transform;
        post.SetParent(go.transform, false);
        post.localPosition = new Vector3(0f, 1.6f, 0f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.9f, 1.5f);
        col.offset = new Vector2(0f, 0.9f);

        var gf = go.AddComponent<GuardFrame>();
        gf.post = post;
        gf.back = backSr;
        gf.front = frontSr;
        gf.brokenSprite = broken;
        gf.fence = col;
        gf.cowboyPrefabs = cowboys;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, FramePrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void MakeSpot(Transform parent, string name, Vector3 pos, Sprite sprite, GameObject framePrefab)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(3f, 2.5f);
        col.offset = new Vector2(0f, 0.8f);

        var spot = go.AddComponent<TowerSpot>();
        spot.framePrefab = framePrefab;
        spot.cost = 80;
    }

    static void MakeMarketPortal(Transform parent, Sprite sprite)
    {
        var go = new GameObject("CowboyMarketPortal");
        go.transform.SetParent(parent, false);
        go.transform.position = PortalPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.4f, 1.6f);
        col.offset = new Vector2(0f, 0.6f);

        var ret = new GameObject("ReturnPoint").transform;
        ret.SetParent(go.transform, false);
        ret.position = PortalPos + new Vector3(0f, 3f, 0f);

        var portal = go.AddComponent<MarketPortal>();
        portal.toMarket = true;
        portal.returnPoint = ret;
        portal.label = "Cowboy Market";
    }
}
#endif
