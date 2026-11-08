using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFishData", menuName = "Aquaculture/Fish Data")]
public class FishData : ScriptableObject
{
    [Header("Thong tin loai ca")]
    public string speciesId = "fish_carp";
    public string speciesName = "Cá Chép";
    [TextArea(2, 3)]
    public string description = "Loài cá nước ngọt phổ biến, dễ nuôi, lớn nhanh.";

    [Header("Vat pham")]
    public ItemData fryItem;       // Cá giống
    public ItemData adultItem;     // Cá thương phẩm thu hoạch

    [Header("Sinh truong")]
    [Tooltip("Thoi gian truong thanh tinh bang giay (khi khong duoc cho an)")]
    public float baseGrowthSeconds = 30f;

    [Tooltip("He so tang toc khi duoc cho an no")]
    public float fedGrowthSpeedMultiplier = 2.5f;

    [Tooltip("Thoi gian no sau khi duoc cho an (giay)")]
    public float fullStomachDuration = 25f;

    [Tooltip("Co the sinh san khi truong thanh trong ao khong")]
    public bool canBreed = true;

    [Tooltip("Thoi gian giua cac lan sinh san (giay)")]
    public float breedCooldown = 40f;

    [Header("Boi loi & Hien thi")]
    public float swimSpeed = 1.3f;
    public Color tint = Color.white;
    public Sprite frySprite;
    public Sprite adultSprite;

    [Header("Gia")]
    public int fryBuyPrice = 15;
    public int adultSellPrice = 45;
}
