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
    public ItemData fryItem;       // Cá giống (khi mua thả xuống hồ)
    public ItemData adultItem;     // Cá thương phẩm (khi lớn vớt bán lấy vàng)

    [Header("Sinh truong & Doi bung")]
    [Tooltip("Thoi gian truong thanh tinh bang phut (mac dinh)")]
    public float growthDurationMinutes = 5.0f;

    [Tooltip("Cu bao nhieu phut thi ca bi doi bung (mac dinh 5 phut, tuy loai ca co the it hoac nhieu hon)")]
    public float hungerIntervalMinutes = 5.0f;

    [Tooltip("He so tang toc sinh truong khi duoc cho an no")]
    public float fedGrowthSpeedMultiplier = 2.0f;

    [Tooltip("Co the sinh san khi truong thanh trong ao khong")]
    public bool canBreed = true;

    [Tooltip("Thoi gian hoi sinh san (giay)")]
    public float breedCooldown = 120f;

    [Header("Boi loi & Bong mo duoi ho")]
    public float swimSpeed = 1.2f;
    public Color shadowColor = new Color(0.08f, 0.2f, 0.35f, 0.65f);
    public Sprite shadowSprite;
    public Sprite frySprite;
    public Sprite adultSprite;

    [Header("Gia ca")]
    public int fryBuyPrice = 15;
    public int adultSellPrice = 45;

    public float BaseGrowthSeconds => Mathf.Max(10f, growthDurationMinutes * 60f);
    public float HungerSeconds => Mathf.Max(5f, hungerIntervalMinutes * 60f);
}
