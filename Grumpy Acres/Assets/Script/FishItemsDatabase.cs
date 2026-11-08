using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class FishItemsDatabase
{
    public static FishData CarpData;
    public static FishData GoldfishData;
    public static FishData SalmonData;

    public static ItemData CarpFryItem;
    public static ItemData CarpItem;
    public static ItemData GoldfishFryItem;
    public static ItemData GoldfishItem;
    public static ItemData SalmonFryItem;
    public static ItemData SalmonItem;

    public static ItemData FishFoodItem;
    public static ItemData FishNetItem;
    public static ItemData FishPondItem;

    public static Sprite CarpSprite;
    public static Sprite CarpFrySprite;
    public static Sprite GoldfishSprite;
    public static Sprite GoldfishFrySprite;
    public static Sprite SalmonSprite;
    public static Sprite SalmonFrySprite;
    public static Sprite FishFoodSprite;
    public static Sprite FishNetSprite;
    public static Sprite BubbleSprite;
    public static Sprite PondSprite;

    static bool initialized = false;

    public static void Initialize()
    {
        if (initialized) return;
        LoadSprites();
        BuildItems();
        initialized = true;
    }

    static void LoadSprites()
    {
#if UNITY_EDITOR
        CarpSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fish_Carp.png");
        CarpFrySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fish_Carp_Fry.png");
        GoldfishSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fish_Goldfish.png");
        GoldfishFrySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fish_Goldfish_Fry.png");
        SalmonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fish_Salmon.png");
        SalmonFrySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fish_Salmon_Fry.png");
        FishFoodSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Item_FishFood.png");
        FishNetSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Tool_FishNet.png");
        BubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Fish/Fx_WaterBubble.png");

        var pondSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Tilemap/Generated/Sprites/pond.png");
        foreach (var s in pondSprites)
        {
            if (s is Sprite sp) { PondSprite = sp; break; }
        }
#endif
    }

    static void BuildItems()
    {
        // 1. Carp
        CarpItem = ScriptableObject.CreateInstance<ItemData>();
        CarpItem.name = "Fish_Carp";
        CarpItem.id = "fish_carp";
        CarpItem.itemName = "Cá Chép";
        CarpItem.itemType = ItemType.Fish;
        CarpItem.maxStack = 99;
        CarpItem.buyPrice = 50;
        CarpItem.sellPrice = 45;
        CarpItem.icon = CarpSprite;
        CarpItem.description = "Cá chép béo tròn, thịt ngọt thơm. Bán được giá cao.";

        CarpFryItem = ScriptableObject.CreateInstance<ItemData>();
        CarpFryItem.name = "Fish_Carp_Fry";
        CarpFryItem.id = "fish_carp_fry";
        CarpFryItem.itemName = "Cá Chép Giống";
        CarpFryItem.itemType = ItemType.FishFry;
        CarpFryItem.maxStack = 99;
        CarpFryItem.buyPrice = 15;
        CarpFryItem.sellPrice = 8;
        CarpFryItem.icon = CarpFrySprite;
        CarpFryItem.fishYield = CarpItem;
        CarpFryItem.growthSeconds = 25f;
        CarpFryItem.description = "Cá chép con. Thả vào ao nước và cho ăn để cá lớn nhanh.";

        CarpData = ScriptableObject.CreateInstance<FishData>();
        CarpData.name = "Data_Carp";
        CarpData.speciesId = "fish_carp";
        CarpData.speciesName = "Cá Chép";
        CarpData.fryItem = CarpFryItem;
        CarpData.adultItem = CarpItem;
        CarpData.baseGrowthSeconds = 25f;
        CarpData.fedGrowthSpeedMultiplier = 2.5f;
        CarpData.fullStomachDuration = 20f;
        CarpData.swimSpeed = 1.3f;
        CarpData.frySprite = CarpFrySprite;
        CarpData.adultSprite = CarpSprite;
        CarpData.tint = Color.white;
        CarpData.fryBuyPrice = 15;
        CarpData.adultSellPrice = 45;

        // 2. Goldfish
        GoldfishItem = ScriptableObject.CreateInstance<ItemData>();
        GoldfishItem.name = "Fish_Goldfish";
        GoldfishItem.id = "fish_goldfish";
        GoldfishItem.itemName = "Cá Vàng";
        GoldfishItem.itemType = ItemType.Fish;
        GoldfishItem.maxStack = 99;
        GoldfishItem.buyPrice = 90;
        GoldfishItem.sellPrice = 80;
        GoldfishItem.icon = GoldfishSprite;
        GoldfishItem.description = "Cá vàng vây dài rực rỡ, tượng trưng cho tài lộc.";

        GoldfishFryItem = ScriptableObject.CreateInstance<ItemData>();
        GoldfishFryItem.name = "Fish_Goldfish_Fry";
        GoldfishFryItem.id = "fish_goldfish_fry";
        GoldfishFryItem.itemName = "Cá Vàng Giống";
        GoldfishFryItem.itemType = ItemType.FishFry;
        GoldfishFryItem.maxStack = 99;
        GoldfishFryItem.buyPrice = 25;
        GoldfishFryItem.sellPrice = 12;
        GoldfishFryItem.icon = GoldfishFrySprite;
        GoldfishFryItem.fishYield = GoldfishItem;
        GoldfishFryItem.growthSeconds = 35f;
        GoldfishFryItem.description = "Cá vàng con bơi lội duyên dáng. Thả xuống ao để nuôi.";

        GoldfishData = ScriptableObject.CreateInstance<FishData>();
        GoldfishData.name = "Data_Goldfish";
        GoldfishData.speciesId = "fish_goldfish";
        GoldfishData.speciesName = "Cá Vàng";
        GoldfishData.fryItem = GoldfishFryItem;
        GoldfishData.adultItem = GoldfishItem;
        GoldfishData.baseGrowthSeconds = 35f;
        GoldfishData.fedGrowthSpeedMultiplier = 2.5f;
        GoldfishData.fullStomachDuration = 22f;
        GoldfishData.swimSpeed = 1.1f;
        GoldfishData.frySprite = GoldfishFrySprite;
        GoldfishData.adultSprite = GoldfishSprite;
        GoldfishData.tint = Color.white;
        GoldfishData.fryBuyPrice = 25;
        GoldfishData.adultSellPrice = 80;

        // 3. Salmon
        SalmonItem = ScriptableObject.CreateInstance<ItemData>();
        SalmonItem.name = "Fish_Salmon";
        SalmonItem.id = "fish_salmon";
        SalmonItem.itemName = "Cá Hồi";
        SalmonItem.itemType = ItemType.Fish;
        SalmonItem.maxStack = 99;
        SalmonItem.buyPrice = 200;
        SalmonItem.sellPrice = 180;
        SalmonItem.icon = SalmonSprite;
        SalmonItem.description = "Cá hồi thượng hạng béo ngậy. Loài cá quý giá nhất.";

        SalmonFryItem = ScriptableObject.CreateInstance<ItemData>();
        SalmonFryItem.name = "Fish_Salmon_Fry";
        SalmonFryItem.id = "fish_salmon_fry";
        SalmonFryItem.itemName = "Cá Hồi Giống";
        SalmonFryItem.itemType = ItemType.FishFry;
        SalmonFryItem.maxStack = 99;
        SalmonFryItem.buyPrice = 50;
        SalmonFryItem.sellPrice = 25;
        SalmonFryItem.icon = SalmonFrySprite;
        SalmonFryItem.fishYield = SalmonItem;
        SalmonFryItem.growthSeconds = 50f;
        SalmonFryItem.description = "Cá hồi giống bơi khỏe. Nuôi lớn đem lại lợi nhuận khổng lồ.";

        SalmonData = ScriptableObject.CreateInstance<FishData>();
        SalmonData.name = "Data_Salmon";
        SalmonData.speciesId = "fish_salmon";
        SalmonData.speciesName = "Cá Hồi";
        SalmonData.fryItem = SalmonFryItem;
        SalmonData.adultItem = SalmonItem;
        SalmonData.baseGrowthSeconds = 50f;
        SalmonData.fedGrowthSpeedMultiplier = 2.5f;
        SalmonData.fullStomachDuration = 25f;
        SalmonData.swimSpeed = 1.5f;
        SalmonData.frySprite = SalmonFrySprite;
        SalmonData.adultSprite = SalmonSprite;
        SalmonData.tint = Color.white;
        SalmonData.fryBuyPrice = 50;
        SalmonData.adultSellPrice = 180;

        // 4. Fish Food
        FishFoodItem = ScriptableObject.CreateInstance<ItemData>();
        FishFoodItem.name = "Item_FishFood";
        FishFoodItem.id = "fish_food";
        FishFoodItem.itemName = "Thức Ăn Cho Cá";
        FishFoodItem.itemType = ItemType.FishFood;
        FishFoodItem.maxStack = 99;
        FishFoodItem.buyPrice = 5;
        FishFoodItem.sellPrice = 2;
        FishFoodItem.icon = FishFoodSprite;
        FishFoodItem.description = "Thức ăn dinh dưỡng dạng hạt. Rải xuống ao giúp cá lớn nhanh gấp 2.5 lần.";

        // 5. Fish Net
        FishNetItem = ScriptableObject.CreateInstance<ItemData>();
        FishNetItem.name = "Tool_FishNet";
        FishNetItem.id = "tool_fishnet";
        FishNetItem.itemName = "Vợt Bắt Cá";
        FishNetItem.itemType = ItemType.Tool;
        FishNetItem.toolType = ToolType.FishNet;
        FishNetItem.maxStack = 1;
        FishNetItem.buyPrice = 30;
        FishNetItem.sellPrice = 15;
        FishNetItem.icon = FishNetSprite;
        FishNetItem.description = "Vợt lưới thủ công dùng để vớt cá trưởng thành từ ao.";

        // 6. Fish Pond (Placeable)
        FishPondItem = ScriptableObject.CreateInstance<ItemData>();
        FishPondItem.name = "Item_FishPond";
        FishPondItem.id = "item_fish_pond";
        FishPondItem.itemName = "Bể Nuôi Cá";
        FishPondItem.itemType = ItemType.Placeable;
        FishPondItem.maxStack = 10;
        FishPondItem.buyPrice = 100;
        FishPondItem.sellPrice = 50;
        FishPondItem.icon = PondSprite;
        FishPondItem.description = "Ao/Bể nuôi cá đặt trên mặt đất. Cho phép nuôi cá ngay tại nông trại.";
    }

    public static FishData GetFishDataByItem(ItemData item)
    {
        if (item == null) return null;
        Initialize();
        if (item == CarpFryItem || item.id == "fish_carp_fry") return CarpData;
        if (item == GoldfishFryItem || item.id == "fish_goldfish_fry") return GoldfishData;
        if (item == SalmonFryItem || item.id == "fish_salmon_fry") return SalmonData;
        return null;
    }
}
