using UnityEngine;

public enum ItemType
{
    Tool,
    Seed,
    Crop,
    Misc,
    Placeable,
    FishFry,   // Ca giong tha xuong ao
    Fish,      // Ca truong thanh thu hoach
    FishFood   // Thuc an cho ca
}

public enum ToolType
{
    None,
    Hoe,
    WateringCan,
    Axe,
    Pickaxe,
    FishNet    // Vot bat ca
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Co ban")]
    public string id;
    public string itemName;
    public Sprite icon;
    [Tooltip("Nhan mau len icon (trang = giu nguyen). Dung de phan biet cung 1 sprite, vd chong go / sat")]
    public Color iconColor = Color.white;
    public ItemType itemType = ItemType.Misc;
    [TextArea(2, 4)]
    public string description;

    [Header("Xep chong (Stack)")]
    [Min(1)]
    public int maxStack = 99;

    [Header("Neu la Cong cu (Tool)")]
    public ToolType toolType = ToolType.None;

    [Header("Neu la Hat giong (Seed)")]
    [Tooltip("Nong san thu hoach duoc khi trong hat nay")]
    public ItemData cropYield;
    [Tooltip("Thoi gian truong thanh (giay thuc te)")]
    public float growthSeconds = 45f;
    [Tooltip("So ngay hien thi tren mo ta")]
    public int growthDays = 4;
    [Tooltip("Cac sprite cac giai doan phat trien tu mam den chin")]
    public Sprite[] growthStages;

    [Header("Neu la Ca (Fish / FishFry)")]
    [Tooltip("Thanh pham ca thu duoc khi lon (neu la ca giong)")]
    public ItemData fishYield;
    [Tooltip("Sprite ca con")]
    public Sprite frySprite;
    [Tooltip("Sprite ca truong thanh")]
    public Sprite adultSprite;

    [Header("Gia ca")]
    public int buyPrice = 10;
    public int sellPrice = 5;

    public bool IsStackable => maxStack > 1;
}
