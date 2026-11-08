using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FishAgent : MonoBehaviour
{
    public static readonly List<FishAgent> AllFish = new();

    [Header("Dữ liệu Cá")]
    public FishData fishData;

    [Header("Sinh trưởng & Đói bụng")]
    public float currentGrowthTime;
    public float hungerTimer; // Đếm ngược giây đói (Mặc định 5 phút = 300s)
    public bool isMature;

    [Header("Bơi lội")]
    public Vector3 targetPos;
    public float changeTargetCooldown;
    public Bounds movementBounds;
    public bool hasCustomBounds;

    // Components
    SpriteRenderer shadowRenderer;
    Transform visualTransform;
    Tilemap waterTilemap;

    // Visual indicators
    GameObject hungerIndicator;
    GameObject matureSparkleIndicator;

    float rippleTimer = 0f;

    public float GrowthProgress
    {
        get
        {
            if (fishData == null || fishData.BaseGrowthSeconds <= 0f) return 1f;
            return Mathf.Clamp01(currentGrowthTime / fishData.BaseGrowthSeconds);
        }
    }

    public bool IsMature => isMature || GrowthProgress >= 1f;
    public bool IsHungry => hungerTimer <= 0f;
    public bool IsFed => !IsHungry;

    void Awake()
    {
        AllFish.Add(this);

        visualTransform = transform.Find("Visual");
        if (!visualTransform)
        {
            var visGO = new GameObject("Visual");
            visGO.transform.SetParent(transform, false);
            visualTransform = visGO.transform;
        }

        shadowRenderer = visualTransform.GetComponent<SpriteRenderer>();
        if (!shadowRenderer)
        {
            shadowRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sortingOrder = 3; // Nằm ngay trên nền nước (Water Tilemap = 2)
        }

        var col = GetComponent<CircleCollider2D>();
        if (!col)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }

        CreateIndicators();
    }

    void OnDestroy()
    {
        AllFish.Remove(this);
    }

    void CreateIndicators()
    {
        // 1. Hunger bubble indicator (Fx_HungerBubble)
        hungerIndicator = new GameObject("HungerIndicator");
        hungerIndicator.transform.SetParent(transform, false);
        hungerIndicator.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var hSr = hungerIndicator.AddComponent<SpriteRenderer>();
        hSr.sortingOrder = 8;
        if (FishItemsDatabase.HungerBubbleSprite != null)
            hSr.sprite = FishItemsDatabase.HungerBubbleSprite;
        hungerIndicator.SetActive(false);

        // 2. Mature harvest sparkle indicator
        matureSparkleIndicator = new GameObject("MatureSparkle");
        matureSparkleIndicator.transform.SetParent(transform, false);
        matureSparkleIndicator.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        var mSr = matureSparkleIndicator.AddComponent<SpriteRenderer>();
        mSr.sortingOrder = 8;
        mSr.color = new Color(1f, 0.95f, 0.3f, 0.9f);
        if (FishItemsDatabase.BubbleSprite != null)
            mSr.sprite = FishItemsDatabase.BubbleSprite;
        matureSparkleIndicator.SetActive(false);
    }

    public void Init(FishData data, Vector3 startPos, Tilemap waterMap = null)
    {
        fishData = data;
        transform.position = startPos;
        targetPos = startPos;
        if (waterMap != null) waterTilemap = waterMap;

        currentGrowthTime = 0f;
        // Bắt đầu với no bụng đầy đủ theo cài đặt loài cá (Mặc định 5 phút = 300s)
        hungerTimer = (fishData != null) ? fishData.HungerSeconds : 300f;
        isMature = false;

        UpdateVisuals();
    }

    void Start()
    {
        if (!waterTilemap)
        {
            if (WaterTilemapFishPond.Instance != null && WaterTilemapFishPond.Instance.waterTilemap != null)
                waterTilemap = WaterTilemapFishPond.Instance.waterTilemap;
            else
            {
                var wGO = GameObject.Find("Water");
                if (wGO) waterTilemap = wGO.GetComponent<Tilemap>();
            }
        }

        targetPos = transform.position;
        UpdateVisuals();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        UpdateGrowth(dt);
        UpdateSwimming(dt);
        UpdateVisualAnimation(dt);
    }

    void UpdateGrowth(float dt)
    {
        if (fishData == null) return;

        // Đếm ngược thời gian đói (cứ 5 phút là đói)
        if (hungerTimer > 0f)
        {
            hungerTimer = Mathf.Max(0f, hungerTimer - dt);
        }

        // Tăng trưởng: Nếu no bụng thì lớn nhanh, nếu đói thì ngừng lớn
        if (!isMature)
        {
            if (!IsHungry)
            {
                currentGrowthTime += dt * fishData.fedGrowthSpeedMultiplier;
                if (currentGrowthTime >= fishData.BaseGrowthSeconds)
                {
                    currentGrowthTime = fishData.BaseGrowthSeconds;
                    isMature = true;
                    OnMatured();
                }
            }
        }

        // Cập nhật hiển thị icon đói
        if (hungerIndicator)
        {
            bool showHunger = IsHungry;
            if (hungerIndicator.activeSelf != showHunger)
                hungerIndicator.SetActive(showHunger);

            if (showHunger)
            {
                // Nhấp nhô nhẹ trên mặt hồ
                float bob = Mathf.Sin(Time.time * 4f) * 0.08f;
                hungerIndicator.transform.localPosition = new Vector3(0f, 0.6f + bob, 0f);
            }
        }

        // Cập nhật hiệu ứng cá trưởng thành
        if (matureSparkleIndicator)
        {
            if (IsMature)
            {
                if (!matureSparkleIndicator.activeSelf) matureSparkleIndicator.SetActive(true);
                float pulse = 0.6f + Mathf.PingPong(Time.time * 2f, 0.3f);
                matureSparkleIndicator.transform.localScale = Vector3.one * pulse;
            }
            else if (matureSparkleIndicator.activeSelf)
            {
                matureSparkleIndicator.SetActive(false);
            }
        }
    }

    void OnMatured()
    {
        if (WaterTilemapFishPond.Instance != null)
        {
            WaterTilemapFishPond.Instance.SpawnSplash(transform.position, 5);
            WaterTilemapFishPond.Instance.SpawnRipple(transform.position);
            WaterTilemapFishPond.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.5f, $"★ {fishData.speciesName} ĐÃ LỚN! (Bán được {fishData.adultSellPrice}v)", Color.yellow);
        }
    }

    void UpdateSwimming(float dt)
    {
        float speed = fishData != null ? fishData.swimSpeed : 1.2f;
        if (IsHungry) speed *= 0.65f; // Cá đói bơi lờ đờ hơn

        // Tìm thức ăn cho cá gần nhất
        var food = FishFoodPellet.FindNearest(transform.position, 4.5f);
        if (food != null)
        {
            targetPos = food.transform.position;
            speed *= 1.5f;

            if (Vector2.Distance(transform.position, food.transform.position) < 0.35f)
            {
                EatFood(food);
            }
        }
        else
        {
            changeTargetCooldown -= dt;
            if (changeTargetCooldown <= 0f || Vector2.Distance(transform.position, targetPos) < 0.2f)
            {
                PickNewWanderTarget();
            }
        }

        // Di chuyển bóng mờ về phía đích
        Vector3 moveDir = (targetPos - transform.position);
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 step = moveDir.normalized * (speed * dt);
            Vector3 nextPos = transform.position + step;

            if (IsPositionValid(nextPos))
            {
                transform.position = nextPos;
            }
            else
            {
                PickNewWanderTarget();
            }

            // Lật hướng bơi theo chiều di chuyển
            if (Mathf.Abs(moveDir.x) > 0.03f && visualTransform != null)
            {
                bool flip = moveDir.x > 0f;
                visualTransform.localScale = new Vector3(
                    flip ? -Mathf.Abs(visualTransform.localScale.x) : Mathf.Abs(visualTransform.localScale.x),
                    visualTransform.localScale.y,
                    1f);
            }

            // Thỉnh thoảng tạo vòng sóng nước khi bóng cá bơi
            rippleTimer += dt;
            if (rippleTimer > 2.5f)
            {
                rippleTimer = 0f;
                if (WaterTilemapFishPond.Instance != null)
                {
                    WaterTilemapFishPond.Instance.SpawnRipple(transform.position);
                }
            }
        }
    }

    public void EatFood(FishFoodPellet food)
    {
        if (food != null) Destroy(food.gameObject);

        // Reset thời gian no bụng về 5 phút (hoặc theo loài cá)
        hungerTimer = fishData != null ? fishData.HungerSeconds : 300f;

        if (WaterTilemapFishPond.Instance != null)
        {
            WaterTilemapFishPond.Instance.SpawnSplash(transform.position, 3);
            WaterTilemapFishPond.Instance.SpawnRipple(transform.position);
            int minutes = fishData != null ? Mathf.RoundToInt(fishData.hungerIntervalMinutes) : 5;
            WaterTilemapFishPond.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.4f, $"No bụng ({minutes} phút)! ✨", new Color(0.2f, 0.95f, 0.4f));
        }
    }

    void PickNewWanderTarget()
    {
        changeTargetCooldown = UnityEngine.Random.Range(2.5f, 5.0f);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 randOffset = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(1.2f, 3.8f);
            Vector3 test = transform.position + new Vector3(randOffset.x, randOffset.y, 0f);

            if (IsPositionValid(test))
            {
                targetPos = test;
                return;
            }
        }
    }

    bool IsPositionValid(Vector3 pos)
    {
        if (hasCustomBounds)
        {
            return movementBounds.Contains(pos);
        }

        if (waterTilemap != null)
        {
            Vector3Int cell = waterTilemap.WorldToCell(pos);
            return waterTilemap.HasTile(cell);
        }

        return true;
    }

    void UpdateVisualAnimation(float dt)
    {
        if (!visualTransform) return;

        // Vẫy đuôi cá hình sin (Wiggle animation)
        float wiggle = Mathf.Sin(Time.time * 7f) * 7.5f;
        visualTransform.localRotation = Quaternion.Euler(0f, 0f, wiggle);

        // Kích thước bóng mờ tăng dần theo độ lớn (từ 0.55f lên 1.25f)
        float baseScale = Mathf.Lerp(0.55f, 1.25f, GrowthProgress);

        // Hiệu ứng bơi lặn nông sâu (dao động kích thước nhẹ)
        float depthBreath = 1f + Mathf.Sin(Time.time * 3f) * 0.04f;
        float curXSign = Mathf.Sign(visualTransform.localScale.x);
        visualTransform.localScale = new Vector3(curXSign * baseScale * depthBreath, baseScale, 1f);
    }

    public void UpdateVisuals()
    {
        if (!shadowRenderer || fishData == null) return;

        // Gán sprite bóng mờ (Fish_Shadow)
        if (FishItemsDatabase.ShadowSprite != null)
        {
            shadowRenderer.sprite = FishItemsDatabase.ShadowSprite;
        }
        else if (fishData.shadowSprite != null)
        {
            shadowRenderer.sprite = fishData.shadowSprite;
        }

        // Màu bóng mờ dưới hồ (nước xanh thẫm bán trong suốt)
        shadowRenderer.color = fishData.shadowColor;
    }

    public bool Harvest()
    {
        if (!IsMature) return false;

        var yieldItem = (fishData != null && fishData.adultItem != null) ? fishData.adultItem : null;
        if (yieldItem != null && InventorySystem.Instance != null)
        {
            int leftover = InventorySystem.Instance.AddItem(yieldItem, 1);
            if (leftover > 0)
            {
                if (WaterTilemapFishPond.Instance != null)
                    WaterTilemapFishPond.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.5f, "Túi đồ đã đầy!", Color.red);
                return false;
            }
        }

        if (WaterTilemapFishPond.Instance != null)
        {
            WaterTilemapFishPond.Instance.SpawnSplash(transform.position, 6);
            WaterTilemapFishPond.Instance.SpawnRipple(transform.position);
            string name = fishData != null ? fishData.speciesName : "Cá";
            int sellPrice = yieldItem != null ? yieldItem.sellPrice : 45;
            WaterTilemapFishPond.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.5f, $"+1 {name} (Giá bán: {sellPrice}v)", new Color(0.2f, 1f, 0.8f));
        }

        Destroy(gameObject);
        return true;
    }
}
