using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Tilemap))]
public class WaterTilemapFishPond : MonoBehaviour
{
    public static WaterTilemapFishPond Instance { get; private set; }

    [Header("Tilemap")]
    public Tilemap waterTilemap;

    [Header("Cài đặt Hồ Cá")]
    public float interactRange = 7f;
    public int maxFishCapacity = 20;

    [Header("Hiệu ứng Nước")]
    public Sprite bubbleSprite;
    public Sprite rippleSprite;
    public Sprite shadowSprite;
    public Sprite hungerBubbleSprite;

    readonly List<Vector3Int> waterCells = new();
    readonly List<FishAgent> fishInPond = new();

    Transform playerTransform;
    FishAgent hoveredFish;

    // Floating text queue
    struct FloatingText
    {
        public string text;
        public Vector3 worldPos;
        public Color color;
        public float time;
    }
    readonly List<FloatingText> floatingTexts = new();

    public int CurrentFishCount
    {
        get
        {
            fishInPond.RemoveAll(f => f == null);
            return fishInPond.Count;
        }
    }

    void Awake()
    {
        Instance = this;
        waterTilemap = GetComponent<Tilemap>();
        if (!waterTilemap) waterTilemap = GetComponentInChildren<Tilemap>();

        FishItemsDatabase.Initialize();
        bubbleSprite = FishItemsDatabase.BubbleSprite;
        shadowSprite = FishItemsDatabase.ShadowSprite;
        hungerBubbleSprite = FishItemsDatabase.HungerBubbleSprite;
        rippleSprite = FishItemsDatabase.RippleSprite;

        ScanWaterCells();
    }

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        var p = GameObject.FindWithTag("Player");
        if (p) playerTransform = p.transform;
        else
        {
            var pm = FindFirstObjectByType<PlayerMove>();
            if (pm) playerTransform = pm.transform;
        }
    }

    public void ScanWaterCells()
    {
        waterCells.Clear();
        if (!waterTilemap) return;

        BoundsInt bounds = waterTilemap.cellBounds;
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (waterTilemap.HasTile(cell))
                {
                    waterCells.Add(cell);
                }
            }
        }
    }

    public bool IsWater(Vector3 worldPos)
    {
        if (!waterTilemap) return false;
        Vector3Int cell = waterTilemap.WorldToCell(worldPos);
        return waterTilemap.HasTile(cell);
    }

    public Vector3 GetRandomWaterPosition()
    {
        if (waterCells.Count == 0) ScanWaterCells();
        if (waterCells.Count == 0) return transform.position;

        var cell = waterCells[UnityEngine.Random.Range(0, waterCells.Count)];
        Vector3 center = waterTilemap.GetCellCenterWorld(cell);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.35f;
        return center + new Vector3(offset.x, offset.y, 0f);
    }

    void Update()
    {
        if (!playerTransform) FindPlayer();

        HandleHover();
        HandleInteraction();
        UpdateFloatingTexts();
    }

    void HandleHover()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null)
        {
            hoveredFish = null;
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        mouseWorld.z = 0f;

        hoveredFish = null;
        for (int i = 0; i < fishInPond.Count; i++)
        {
            var fish = fishInPond[i];
            if (!fish) continue;
            if (Vector2.Distance(fish.transform.position, mouseWorld) < 0.8f)
            {
                hoveredFish = fish;
                break;
            }
        }
    }

    void HandleInteraction()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;
        if (FramePlacer.Active) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            worldPos.z = 0f;

            if (playerTransform != null && Vector2.Distance(playerTransform.position, worldPos) > interactRange)
                return;

            var activeSlot = InventorySystem.Instance != null ? InventorySystem.Instance.GetSelected() : null;
            var activeItem = activeSlot != null ? activeSlot.item : null;

            // 1. Click vào cá trưởng thành để thu hoạch
            if (TryHarvestClickedFish(worldPos))
                return;

            // 2. Click vào vùng nước để thả cá hoặc cho ăn
            if (IsWater(worldPos))
            {
                // Thả cá giống (FishFry) tạo bóng mờ dưới hồ
                if (activeItem != null && (activeItem.itemType == ItemType.FishFry || activeItem.id.Contains("_fry")))
                {
                    var data = FishItemsDatabase.GetFishDataByItem(activeItem);
                    if (data != null)
                    {
                        if (CurrentFishCount >= maxFishCapacity)
                        {
                            ShowFloatingMessage(worldPos + Vector3.up * 0.4f, "Hồ cá đã đầy!", Color.yellow);
                            return;
                        }

                        // Tạo bóng mờ cá dưới hồ
                        AddFish(data, worldPos);

                        // Trừ 1 cá giống khỏi túi đồ
                        if (activeSlot != null)
                        {
                            activeSlot.amount--;
                            if (activeSlot.amount <= 0) activeSlot.Clear();
                            InventorySystem.Instance.NotifyChanged();
                        }

                        SpawnSplash(worldPos, 4);
                        SpawnRipple(worldPos);
                        ShowFloatingMessage(worldPos + Vector3.up * 0.4f, $"Đã thả {data.speciesName} vào hồ! 🐟", new Color(0.2f, 0.85f, 1f));
                        return;
                    }
                }

                // Cho cá ăn (FishFood)
                if (activeItem != null && (activeItem.itemType == ItemType.FishFood || activeItem.id == "fish_food"))
                {
                    SpawnFoodPellet(worldPos);

                    if (activeSlot != null)
                    {
                        activeSlot.amount--;
                        if (activeSlot.amount <= 0) activeSlot.Clear();
                        InventorySystem.Instance.NotifyChanged();
                    }

                    SpawnSplash(worldPos, 3);
                    SpawnRipple(worldPos);
                    ShowFloatingMessage(worldPos + Vector3.up * 0.4f, "Rải thức ăn cá! 🍞", new Color(1f, 0.85f, 0.3f));
                    return;
                }
            }
        }
    }

    public FishAgent AddFish(FishData data, Vector3 spawnPos)
    {
        if (data == null) return null;

        var fishGO = new GameObject($"FishShadow_{data.speciesName}");
        fishGO.transform.position = spawnPos;
        fishGO.transform.SetParent(transform, true);

        var agent = fishGO.AddComponent<FishAgent>();
        agent.Init(data, spawnPos, waterTilemap);

        fishInPond.Add(agent);
        return agent;
    }

    bool TryHarvestClickedFish(Vector3 clickPos)
    {
        for (int i = 0; i < fishInPond.Count; i++)
        {
            var fish = fishInPond[i];
            if (!fish) continue;

            if (Vector2.Distance(fish.transform.position, clickPos) < 0.85f)
            {
                if (fish.IsMature)
                {
                    return fish.Harvest();
                }
                else
                {
                    int pct = Mathf.RoundToInt(fish.GrowthProgress * 100f);
                    string status = fish.IsHungry ? " (ĐANG ĐÓI!)" : "";
                    ShowFloatingMessage(fish.transform.position + Vector3.up * 0.4f, $"Cá đang lớn ({pct}%){status}", Color.white);
                    return true;
                }
            }
        }
        return false;
    }

    public void SpawnFoodPellet(Vector3 position)
    {
        var foodGO = new GameObject("FishFoodPellet");
        foodGO.transform.position = position;
        var sr = foodGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.color = new Color(0.95f, 0.8f, 0.35f);
        if (bubbleSprite != null) sr.sprite = bubbleSprite;
        foodGO.transform.localScale = Vector3.one * 0.35f;
        foodGO.AddComponent<FishFoodPellet>();
    }

    public void SpawnRipple(Vector3 pos)
    {
        if (!rippleSprite) return;
        var rGO = new GameObject("WaterRipple");
        rGO.transform.position = pos;
        var sr = rGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;
        sr.sprite = rippleSprite;
        sr.color = new Color(0.7f, 0.9f, 1f, 0.6f);
        rGO.transform.localScale = Vector3.one * 0.4f;

        var auto = rGO.AddComponent<AutoHide>();
        auto.seconds = 0.8f;
        AutoHide.Flash(auto);
        Destroy(rGO, 0.85f);
    }

    public void SpawnSplash(Vector3 pos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var pGO = new GameObject("SplashParticle");
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.3f;
            pGO.transform.position = pos + new Vector3(offset.x, offset.y, 0f);

            var sr = pGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            sr.color = new Color(0.6f, 0.85f, 1f, 0.8f);
            if (bubbleSprite) sr.sprite = bubbleSprite;
            pGO.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.2f, 0.45f);

            var auto = pGO.AddComponent<AutoHide>();
            auto.seconds = UnityEngine.Random.Range(0.4f, 0.7f);
            AutoHide.Flash(auto);
            Destroy(pGO, 0.8f);
        }
    }

    public void ShowFloatingMessage(Vector3 pos, string msg, Color color)
    {
        floatingTexts.Add(new FloatingText
        {
            text = msg,
            worldPos = pos,
            color = color,
            time = 1.6f
        });
    }

    void UpdateFloatingTexts()
    {
        float dt = Time.deltaTime;
        for (int i = floatingTexts.Count - 1; i >= 0; i--)
        {
            var ft = floatingTexts[i];
            ft.time -= dt;
            ft.worldPos += Vector3.up * (0.6f * dt);
            if (ft.time <= 0f) floatingTexts.RemoveAt(i);
            else floatingTexts[i] = ft;
        }
    }

    void OnGUI()
    {
        if (Camera.main == null) return;

        // Floating texts
        var textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < floatingTexts.Count; i++)
        {
            var ft = floatingTexts[i];
            Vector3 screen = Camera.main.WorldToScreenPoint(ft.worldPos);
            if (screen.z < 0) continue;

            float alpha = Mathf.Clamp01(ft.time);
            textStyle.normal.textColor = new Color(ft.color.r, ft.color.g, ft.color.b, alpha);

            float w = 240f;
            float h = 26f;
            GUI.Label(new Rect(screen.x - w * 0.5f, Screen.height - screen.y - h, w, h), ft.text, textStyle);
        }

        // Hover tooltip on fish shadow
        if (hoveredFish != null && hoveredFish.fishData != null)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(hoveredFish.transform.position + Vector3.up * 0.65f);
            if (screen.z > 0)
            {
                float w = 220f;
                float h = 64f;
                float x = screen.x - w * 0.5f;
                float y = Screen.height - screen.y - h;

                GUI.Box(new Rect(x, y, w, h), "");

                var titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                titleStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(x, y + 2, w, 20), hoveredFish.fishData.speciesName, titleStyle);

                var statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter
                };

                if (hoveredFish.IsMature)
                {
                    statusStyle.normal.textColor = new Color(0.2f, 1f, 0.5f);
                    GUI.Label(new Rect(x, y + 20, w, 18), $"★ Đã lớn! (Bán được {hoveredFish.fishData.adultSellPrice} vàng)", statusStyle);
                }
                else
                {
                    int pct = Mathf.RoundToInt(hoveredFish.GrowthProgress * 100f);
                    statusStyle.normal.textColor = Color.white;
                    GUI.Label(new Rect(x, y + 20, w, 18), $"Đang lớn: {pct}%", statusStyle);
                }

                if (hoveredFish.IsHungry)
                {
                    statusStyle.normal.textColor = new Color(1f, 0.4f, 0.3f);
                    GUI.Label(new Rect(x, y + 38, w, 18), "⚠ Đang ĐÓI! (Cần rải thức ăn)", statusStyle);
                }
                else
                {
                    int minLeft = Mathf.CeilToInt(hoveredFish.hungerTimer / 60f);
                    statusStyle.normal.textColor = new Color(0.3f, 0.9f, 1f);
                    GUI.Label(new Rect(x, y + 38, w, 18), $"No bụng (còn {minLeft} phút)", statusStyle);
                }
            }
        }
    }
}
