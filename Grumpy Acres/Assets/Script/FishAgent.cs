using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FishAgent : MonoBehaviour
{
    public static readonly List<FishAgent> AllFish = new();

    [Header("Du lieu ca")]
    public FishData fishData;

    [Header("Trang thai")]
    public float currentGrowthTime;
    public float hungerTimer; // > 0: dang no, = 0: dang doi
    public float breedTimer;
    public bool isMature;

    [Header("Boi loi")]
    public Vector3 targetPos;
    public float changeTargetCooldown;
    public Bounds movementBounds;
    public bool hasCustomBounds;

    // Components
    SpriteRenderer spriteRenderer;
    Transform visualTransform;
    Tilemap waterTilemap;

    // Visual effects
    GameObject readyIndicator;
    GameObject fedIndicator;
    float fedIndicatorTimer;

    public float GrowthProgress
    {
        get
        {
            if (fishData == null || fishData.baseGrowthSeconds <= 0f) return 1f;
            return Mathf.Clamp01(currentGrowthTime / fishData.baseGrowthSeconds);
        }
    }

    public bool IsMature => GrowthProgress >= 1f;
    public bool IsFed => hungerTimer > 0f;

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

        spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
        {
            spriteRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 4; // Bơi duoi mat nuoc/tren nen nuoc
        }

        // Add 2D collider for mouse picking
        var col = GetComponent<CircleCollider2D>();
        if (!col)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }

        CreateIndicators();
    }

    void Start()
    {
        if (FishManager.Instance != null && FishManager.Instance.waterTilemap != null)
        {
            waterTilemap = FishManager.Instance.waterTilemap;
        }
        else
        {
            var waterGO = GameObject.Find("Water");
            if (waterGO) waterTilemap = waterGO.GetComponent<Tilemap>();
        }

        targetPos = transform.position;
        breedTimer = fishData != null ? fishData.breedCooldown : 40f;
        UpdateVisuals();
    }

    void OnDestroy()
    {
        AllFish.Remove(this);
    }

    void CreateIndicators()
    {
        // Ready to harvest sparkle indicator
        readyIndicator = new GameObject("ReadyIndicator");
        readyIndicator.transform.SetParent(transform, false);
        readyIndicator.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var readySr = readyIndicator.AddComponent<SpriteRenderer>();
        readySr.sortingOrder = 8;
        readySr.color = new Color(1f, 0.95f, 0.2f, 0.9f);

        // Load bubble or star sprite
        if (FishManager.Instance != null && FishManager.Instance.bubbleSprite != null)
        {
            readySr.sprite = FishManager.Instance.bubbleSprite;
        }
        readyIndicator.SetActive(false);

        // Fed heart/happy bubble indicator
        fedIndicator = new GameObject("FedIndicator");
        fedIndicator.transform.SetParent(transform, false);
        fedIndicator.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        var fedSr = fedIndicator.AddComponent<SpriteRenderer>();
        fedSr.sortingOrder = 8;
        fedSr.color = new Color(0.3f, 1f, 0.5f, 0.9f);
        if (FishManager.Instance != null && FishManager.Instance.bubbleSprite != null)
        {
            fedSr.sprite = FishManager.Instance.bubbleSprite;
        }
        fedIndicator.SetActive(false);
    }

    public void Init(FishData data, Vector3 startPosition, Tilemap waterMap = null)
    {
        fishData = data;
        transform.position = startPosition;
        targetPos = startPosition;
        if (waterMap != null) waterTilemap = waterMap;

        currentGrowthTime = 0f;
        hungerTimer = 10f; // Vừa thả xuống ao còn no 10s
        isMature = false;

        UpdateVisuals();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        UpdateGrowth(dt);
        UpdateSwimming(dt);
        UpdateBreeding(dt);
        UpdateVisualAnimation(dt);
    }

    void UpdateGrowth(float dt)
    {
        if (fishData == null) return;

        // Hunger countdown
        if (hungerTimer > 0f)
        {
            hungerTimer = Mathf.Max(0f, hungerTimer - dt);
        }

        if (!isMature)
        {
            float rate = IsFed ? fishData.fedGrowthSpeedMultiplier : 0.4f;
            currentGrowthTime += dt * rate;

            if (currentGrowthTime >= fishData.baseGrowthSeconds)
            {
                currentGrowthTime = fishData.baseGrowthSeconds;
                isMature = true;
                OnMatured();
            }

            UpdateVisuals();
        }

        // Handle fed indicator flash
        if (fedIndicatorTimer > 0f)
        {
            fedIndicatorTimer -= dt;
            if (fedIndicator)
            {
                fedIndicator.SetActive(true);
                float scale = 0.5f + Mathf.Sin(fedIndicatorTimer * 10f) * 0.1f;
                fedIndicator.transform.localScale = Vector3.one * scale;
            }
        }
        else if (fedIndicator && fedIndicator.activeSelf)
        {
            fedIndicator.SetActive(false);
        }

        // Ready indicator pulse
        if (readyIndicator)
        {
            if (isMature)
            {
                readyIndicator.SetActive(true);
                float pulse = 0.6f + Mathf.PingPong(Time.time * 2f, 0.25f);
                readyIndicator.transform.localScale = Vector3.one * pulse;
            }
            else
            {
                readyIndicator.SetActive(false);
            }
        }
    }

    void OnMatured()
    {
        UpdateVisuals();
        if (FishManager.Instance != null)
        {
            FishManager.Instance.SpawnSplash(transform.position, 4);
        }
    }

    void UpdateSwimming(float dt)
    {
        float speed = fishData != null ? fishData.swimSpeed : 1.2f;

        // Check if there is fish food nearby
        var food = FishFoodPellet.FindNearest(transform.position, 4.5f);
        if (food != null)
        {
            targetPos = food.transform.position;
            speed *= 1.4f; // Boi nhanh toi moi

            // Check if reached food
            if (Vector2.Distance(transform.position, food.transform.position) < 0.35f)
            {
                EatFood(food);
            }
        }
        else
        {
            // Normal wander
            changeTargetCooldown -= dt;
            if (changeTargetCooldown <= 0f || Vector2.Distance(transform.position, targetPos) < 0.2f)
            {
                PickNewWanderTarget();
            }
        }

        // Move towards targetPos
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

            // Flip sprite according to swim direction
            if (Mathf.Abs(moveDir.x) > 0.05f && visualTransform != null)
            {
                bool flip = moveDir.x > 0f; // Quay phai hay trai
                visualTransform.localScale = new Vector3(flip ? -Mathf.Abs(visualTransform.localScale.x) : Mathf.Abs(visualTransform.localScale.x), visualTransform.localScale.y, 1f);
            }
        }
    }

    public void EatFood(FishFoodPellet food)
    {
        if (food != null)
        {
            Destroy(food.gameObject);
        }

        hungerTimer = fishData != null ? fishData.fullStomachDuration : 25f;
        fedIndicatorTimer = 2.5f;

        if (FishManager.Instance != null)
        {
            FishManager.Instance.SpawnSplash(transform.position, 3);
            FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.4f, "No bụng! ✨", new Color(0.2f, 0.9f, 0.4f));
        }
    }

    void PickNewWanderTarget()
    {
        changeTargetCooldown = UnityEngine.Random.Range(2f, 4.5f);

        // Try random positions within radius
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 randOffset = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(1.2f, 3.5f);
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

    void UpdateBreeding(float dt)
    {
        if (!isMature || !IsFed || fishData == null || !fishData.canBreed) return;

        breedTimer -= dt;
        if (breedTimer <= 0f)
        {
            breedTimer = fishData.breedCooldown;

            // Search for another mature fed fish of same species nearby
            for (int i = 0; i < AllFish.Count; i++)
            {
                var other = AllFish[i];
                if (other == this || !other || other.fishData != fishData) continue;
                if (!other.isMature || !other.IsFed) continue;

                if (Vector2.Distance(transform.position, other.transform.position) < 1.5f)
                {
                    // Breed new fry!
                    Vector3 babyPos = (transform.position + other.transform.position) * 0.5f;
                    if (FishManager.Instance != null)
                    {
                        FishManager.Instance.SpawnFish(fishData, babyPos, waterTilemap);
                        FishManager.Instance.ShowFloatingMessage(babyPos + Vector3.up * 0.5f, "❤️ Cá sinh sản!", new Color(1f, 0.4f, 0.7f));
                    }
                    break;
                }
            }
        }
    }

    void UpdateVisualAnimation(float dt)
    {
        if (!visualTransform) return;

        // Fish swimming waggle motion (sine wave rotation)
        float wiggle = Mathf.Sin(Time.time * 8f) * 6f;
        visualTransform.localRotation = Quaternion.Euler(0f, 0f, wiggle);

        // Gentle breathing scale
        float baseScale = Mathf.Lerp(0.55f, 1.15f, GrowthProgress);
        float breathe = 1f + Mathf.Sin(Time.time * 4f) * 0.04f;
        float curXSign = Mathf.Sign(visualTransform.localScale.x);
        visualTransform.localScale = new Vector3(curXSign * baseScale * breathe, baseScale, 1f);
    }

    public void UpdateVisuals()
    {
        if (!spriteRenderer || fishData == null) return;

        if (isMature && fishData.adultSprite != null)
        {
            spriteRenderer.sprite = fishData.adultSprite;
        }
        else if (fishData.frySprite != null)
        {
            spriteRenderer.sprite = fishData.frySprite;
        }

        spriteRenderer.color = fishData.tint;
    }

    public bool Harvest()
    {
        if (!isMature) return false;

        var yieldItem = (fishData != null && fishData.adultItem != null) ? fishData.adultItem : null;
        if (yieldItem != null && InventorySystem.Instance != null)
        {
            int leftover = InventorySystem.Instance.AddItem(yieldItem, 1);
            if (leftover > 0)
            {
                if (FishManager.Instance != null)
                    FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.5f, "Túi đồ đã đầy!", Color.red);
                return false;
            }
        }

        if (FishManager.Instance != null)
        {
            FishManager.Instance.SpawnSplash(transform.position, 6);
            string name = fishData != null ? fishData.speciesName : "Cá";
            int sellPrice = yieldItem != null ? yieldItem.sellPrice : 30;
            FishManager.Instance.ShowFloatingMessage(transform.position + Vector3.up * 0.5f, $"+1 {name} ({sellPrice}v)", new Color(0.2f, 1f, 0.8f));
        }

        Destroy(gameObject);
        return true;
    }
}
