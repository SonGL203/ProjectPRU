#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class InventorySetupEditor
{
    const string ItemsPath = "Assets/Items";
    const string ToolsPath = "Assets/Items/Tools";
    const string SeedsPath = "Assets/Items/Seeds";
    const string CropsPath = "Assets/Items/Crops";

    [MenuItem("Tools/Farm/1. Create Sample Item Assets")]
    public static void CreateSampleItems()
    {
        EnsureFolder(ItemsPath);
        EnsureFolder(ToolsPath);
        EnsureFolder(SeedsPath);
        EnsureFolder(CropsPath);

        // Load Sprites
        var toolSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Character/Tools.png");
        var cropSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Plants/crops_all.png");

        Sprite FindSprite(Object[] array, string spriteName)
        {
            if (array == null) return null;
            foreach (var obj in array)
            {
                if (obj is Sprite s && s.name == spriteName) return s;
            }
            return null;
        }

        // 1. Tools
        var hoe = CreateOrGetItem($"{ToolsPath}/Tool_Hoe.asset");
        hoe.id = "tool_hoe";
        hoe.itemName = "Cuốc Đất";
        hoe.itemType = ItemType.Tool;
        hoe.toolType = ToolType.Hoe;
        hoe.maxStack = 1;
        hoe.description = "Dùng để cuốc xới đất trước khi gieo hạt giống.";
        hoe.icon = FindSprite(toolSprites, "Tools_24"); // Sprite chiec Cuoc (Hoe)
        EditorUtility.SetDirty(hoe);

        var can = CreateOrGetItem($"{ToolsPath}/Tool_WateringCan.asset");
        can.id = "tool_watering_can";
        can.itemName = "Bình Tưới Nước";
        can.itemType = ItemType.Tool;
        can.toolType = ToolType.WateringCan;
        can.maxStack = 1;
        can.description = "Dùng để tưới ẩm đất giúp cây trồng phát triển nhanh chóng.";
        can.icon = FindSprite(toolSprites, "Tools_0"); // Sprite Binh tuoi nuoc (Watering Can)
        EditorUtility.SetDirty(can);

        var axe = CreateOrGetItem($"{ToolsPath}/Tool_Axe.asset");
        axe.id = "tool_axe";
        axe.itemName = "Rìu";
        axe.itemType = ItemType.Tool;
        axe.toolType = ToolType.Axe;
        axe.maxStack = 1;
        axe.description = "Chặt cây và thu thập củi gỗ.";
        axe.icon = FindSprite(toolSprites, "Tools_16");
        EditorUtility.SetDirty(axe);

        var pickaxe = CreateOrGetItem($"{ToolsPath}/Tool_Pickaxe.asset");
        pickaxe.id = "tool_pickaxe";
        pickaxe.itemName = "Cuốc Chim";
        pickaxe.itemType = ItemType.Tool;
        pickaxe.toolType = ToolType.Pickaxe;
        pickaxe.maxStack = 1;
        pickaxe.description = "Đập đá và khai thác các quặng khoáng sản.";
        pickaxe.icon = FindSprite(toolSprites, "Tools_22");
        EditorUtility.SetDirty(pickaxe);

        // 2. Crops (Nong san thu hoach)
        var cropTomato = CreateOrGetItem($"{CropsPath}/Crop_Tomato.asset");
        cropTomato.id = "crop_tomato";
        cropTomato.itemName = "Cà Chua";
        cropTomato.itemType = ItemType.Crop;
        cropTomato.maxStack = 99;
        cropTomato.sellPrice = 25;
        cropTomato.description = "Quả cà chua đỏ mọng nước vừa mới thu hoạch.";
        cropTomato.icon = FindSprite(cropSprites, "crops_all_10");
        EditorUtility.SetDirty(cropTomato);

        var cropCarrot = CreateOrGetItem($"{CropsPath}/Crop_Carrot.asset");
        cropCarrot.id = "crop_carrot";
        cropCarrot.itemName = "Cà Rốt";
        cropCarrot.itemType = ItemType.Crop;
        cropCarrot.maxStack = 99;
        cropCarrot.sellPrice = 20;
        cropCarrot.description = "Củ cà rốt ngọt giòn, giàu vitamin.";
        cropCarrot.icon = FindSprite(cropSprites, "crops_all_20");
        EditorUtility.SetDirty(cropCarrot);

        var cropWheat = CreateOrGetItem($"{CropsPath}/Crop_Wheat.asset");
        cropWheat.id = "crop_wheat";
        cropWheat.itemName = "Bó Lúa Mì";
        cropWheat.itemType = ItemType.Crop;
        cropWheat.maxStack = 99;
        cropWheat.sellPrice = 15;
        cropWheat.description = "Bó lúa mì vàng óng có thể chế biến thành bột mì.";
        cropWheat.icon = FindSprite(cropSprites, "crops_all_47");
        EditorUtility.SetDirty(cropWheat);

        // 3. Seeds (Hat giong)
        var seedTomato = CreateOrGetItem($"{SeedsPath}/Seed_Tomato.asset");
        seedTomato.id = "seed_tomato";
        seedTomato.itemName = "Hạt Giống Cà Chua";
        seedTomato.itemType = ItemType.Seed;
        seedTomato.maxStack = 99;
        seedTomato.growthSeconds = 45f; // Thoi gian lon cham (45 giay)
        seedTomato.buyPrice = 15;
        seedTomato.cropYield = cropTomato;
        seedTomato.description = "Gieo lên ô đất đã cuốc. Tưới nước để lớn nhanh hơn.";
        seedTomato.icon = FindSprite(cropSprites, "crops_all_0");
        seedTomato.growthStages = new Sprite[]
        {
            FindSprite(cropSprites, "crops_all_1"),
            FindSprite(cropSprites, "crops_all_4"),
            FindSprite(cropSprites, "crops_all_6"),
            FindSprite(cropSprites, "crops_all_8"),
            FindSprite(cropSprites, "crops_all_9")
        };
        EditorUtility.SetDirty(seedTomato);

        var seedCarrot = CreateOrGetItem($"{SeedsPath}/Seed_Carrot.asset");
        seedCarrot.id = "seed_carrot";
        seedCarrot.itemName = "Hạt Giống Cà Rốt";
        seedCarrot.itemType = ItemType.Seed;
        seedCarrot.maxStack = 99;
        seedCarrot.growthSeconds = 35f; // 35 giay
        seedCarrot.buyPrice = 12;
        seedCarrot.cropYield = cropCarrot;
        seedCarrot.description = "Gieo lên ô đất đã cuốc. Tưới nước để lớn nhanh hơn.";
        seedCarrot.icon = FindSprite(cropSprites, "crops_all_11");
        seedCarrot.growthStages = new Sprite[]
        {
            FindSprite(cropSprites, "crops_all_12"),
            FindSprite(cropSprites, "crops_all_15"),
            FindSprite(cropSprites, "crops_all_16"),
            FindSprite(cropSprites, "crops_all_18"),
            FindSprite(cropSprites, "crops_all_19")
        };
        EditorUtility.SetDirty(seedCarrot);

        var seedWheat = CreateOrGetItem($"{SeedsPath}/Seed_Wheat.asset");
        seedWheat.id = "seed_wheat";
        seedWheat.itemName = "Hạt Giống Lúa Mì";
        seedWheat.itemType = ItemType.Seed;
        seedWheat.maxStack = 99;
        seedWheat.growthSeconds = 30f; // 30 giay
        seedWheat.buyPrice = 8;
        seedWheat.cropYield = cropWheat;
        seedWheat.description = "Gieo lên ô đất đã cuốc. Tưới nước để lớn nhanh hơn.";
        seedWheat.icon = FindSprite(cropSprites, "crops_all_37");
        seedWheat.growthStages = new Sprite[]
        {
            FindSprite(cropSprites, "crops_all_38"),
            FindSprite(cropSprites, "crops_all_40"),
            FindSprite(cropSprites, "crops_all_42"),
            FindSprite(cropSprites, "crops_all_44"),
            FindSprite(cropSprites, "crops_all_46")
        };
        EditorUtility.SetDirty(seedWheat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Da tao thanh cong tat ca cac ItemData: Cuoc, Binh tuoi, Riu, Cuoc chim, Hat giong va Nong san!");
    }

    [MenuItem("Tools/Farm/2. Setup Inventory & Hotbar UI in Scene")]
    public static void SetupSceneUI()
    {
        CreateSampleItems();

        // 1. Ensure EventSystem exists
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
        }

        // 2. Find or Create InventorySystem
        var invSystem = Object.FindFirstObjectByType<InventorySystem>();
        if (invSystem == null)
        {
            var invSystemGO = new GameObject("InventorySystem", typeof(InventorySystem));
            invSystem = invSystemGO.GetComponent<InventorySystem>();
            Undo.RegisterCreatedObjectUndo(invSystemGO, "Create InventorySystem");
        }

        // Start with empty inventory
        invSystem.startingItems.Clear();
        EditorUtility.SetDirty(invSystem);

        // 3. Find or Create UI Canvas
        var canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
        {
            canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        // Sprites for UI
        var invSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Inventory.png");
        var actionSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Action_panel.png");

        Sprite FindSprite(Object[] array, string spriteName)
        {
            if (array == null) return null;
            foreach (var obj in array)
            {
                if (obj is Sprite s && s.name == spriteName) return s;
            }
            return null;
        }

        var invWindowSprite = FindSprite(invSprites, "Inventory_1");
        var hotbarBarSprite = FindSprite(actionSprites, "Action_panel_1");
        var selectorSprite = FindSprite(actionSprites, "Action_panel_8");

        // 4. Hotbar UI
        var hotbarUI = canvasGO.GetComponentInChildren<HotbarUI>(true);
        if (hotbarUI == null)
        {
            var hotbarGO = new GameObject("HotbarUI", typeof(RectTransform), typeof(HotbarUI));
            hotbarGO.transform.SetParent(canvasGO.transform, false);
            hotbarUI = hotbarGO.GetComponent<HotbarUI>();
            Undo.RegisterCreatedObjectUndo(hotbarGO, "Create HotbarUI");
        }
        hotbarUI.barSprite = hotbarBarSprite;
        hotbarUI.highlightSprite = selectorSprite;
        EditorUtility.SetDirty(hotbarUI);

        // 5. Inventory UI
        var invUI = canvasGO.GetComponentInChildren<InventoryUI>(true);
        if (invUI == null)
        {
            var invUIGO = new GameObject("InventoryUI", typeof(RectTransform), typeof(InventoryUI));
            invUIGO.transform.SetParent(canvasGO.transform, false);
            invUI = invUIGO.GetComponent<InventoryUI>();
            Undo.RegisterCreatedObjectUndo(invUIGO, "Create InventoryUI");
        }
        invUI.windowSprite = invWindowSprite;
        EditorUtility.SetDirty(invUI);

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Da cai dat thanh cong UI Canvas, HotbarUI (9 slot 1-9) va InventoryUI (Grid 5x4 khop sprite) vao Scene!");
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    static ItemData CreateOrGetItem(string assetPath)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, assetPath);
        }
        return item;
    }
}
#endif

