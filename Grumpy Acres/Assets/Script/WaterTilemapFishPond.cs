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

    [Header("CĂ i Ä‘áº·t Há»“ CĂ¡")]
    public float interactRange = 7f;
    public int maxFishCapacity = 20;

    [Header("Hiá»‡u á»©ng NÆ°á»›c")]
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

            // 1. Click vĂ o cĂ¡ trÆ°á»Ÿng thĂ nh Ä‘á»ƒ thu hoáº¡ch
            if (TryHarvestClickedFish(worldPos))
                return;

            // 2. Click vĂ o vĂ¹ng nÆ°á»›c Ä‘á»ƒ tháº£ cĂ¡ hoáº·c cho Äƒn
            if (IsWater(worldPos))
            {
                // Tháº£ cĂ¡ giá»‘ng (FishFry) táº¡o bĂ³ng má» dÆ°á»›i há»“
                if (activeItem != null && (activeItem.itemType == ItemType.FishFry || activeItem.id.Contains("_fry")))
                {
                    var data = FishItemsDatabase.GetFishDataByItem(activeItem);
                    if (data != null)
                    {
                        if (CurrentFishCount >= maxFishCapacity)
                        {
                            ShowFloatingMessage(worldPos + Vector3.up * 0.4f, "Há»“ cĂ¡ Ä‘Ă£ Ä‘áº§y!", Color.yellow);
                            return;
                        }

                        // Táº¡o bĂ³ng má» cĂ¡ dÆ°á»›i há»“
                        AddFish(data, worldPos);

                        // Trá»« 1 cĂ¡ giá»‘ng khá»i tĂºi Ä‘á»“
                        if (activeSlot != null)
                        {
                            activeSlot.amount--;
                            if (activeSlot.amount <= 0) activeSlot.Clear();
                            InventorySystem.Instance.NotifyChanged();
                        }

                        SpawnSplash(worldPos, 4);
                        SpawnRipple(worldPos);
                        ShowFloatingMessage(worldPos + Vector3.up * 0.4f, $"ÄĂ£ tháº£ {data.speciesName} vĂ o há»“! đŸŸ", new Color(0.2f, 0.85f, 1f));
                        return;
                    }
                }

                // Cho cĂ¡ Äƒn (FishFood)
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
                    ShowFloatingMessage(worldPos + Vector3.up * 0.4f, "Ráº£i thá»©c Äƒn cĂ¡! đŸ", new Color(1f, 0.85f, 0.3f));
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
                    string status = fish.IsHungry ? " (ÄANG ÄĂ“I!)" : "";
                    ShowFloatingMessage(fish.transform.position + Vector3.up * 0.4f, $"CĂ¡ Ä‘ang lá»›n ({pct}%){status}", Color.white);
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

    static Texture2D _pondWhitePixel;
    static void DrawPondPanel(Rect r, Color bg, Color border, float borderWidth = 1.5f)
    {
        if (_pondWhitePixel == null)
        {
            _pondWhitePixel = new Texture2D(1, 1);
            _pondWhitePixel.SetPixel(0, 0, Color.white);
            _pondWhitePixel.Apply();
        }
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), _pondWhitePixel);
        GUI.color = border;
        GUI.DrawTexture(r, _pondWhitePixel);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(r.x + borderWidth, r.y + borderWidth, r.width - borderWidth * 2f, r.height - borderWidth * 2f), _pondWhitePixel);
        GUI.color = old;
    }

    static void DrawPondShadowLabel(Rect r, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 1.5f)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(r, text, style);
        style.normal.textColor = old;
    }

    void OnGUI()
    {
        if (Camera.main == null) return;
        float scale = Mathf.Clamp(Screen.height / 1080f, 1.0f, 2.2f);

        // Floating texts
        var textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < floatingTexts.Count; i++)
        {
            var ft = floatingTexts[i];
            Vector3 screen = Camera.main.WorldToScreenPoint(ft.worldPos);
            if (screen.z < 0) continue;

            float alpha = Mathf.Clamp01(ft.time);
            Color c = new Color(ft.color.r, ft.color.g, ft.color.b, alpha);

            float w = 280f * scale;
            float h = 30f * scale;
            DrawPondShadowLabel(new Rect(screen.x - w * 0.5f, Screen.height - screen.y - h, w, h), ft.text, textStyle, c, new Color(0, 0, 0, alpha), 1.5f);
        }

        // Hover tooltip on fish shadow
        if (hoveredFish != null && hoveredFish.fishData != null)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(hoveredFish.transform.position + Vector3.up * 0.65f);
            if (screen.z > 0)
            {
                float w = Mathf.Round(260f * scale);
                float h = Mathf.Round(80f * scale);
                float x = screen.x - w * 0.5f;
                float y = Screen.height - screen.y - h;

                DrawPondPanel(new Rect(x, y, w, h), new Color(0.10f, 0.14f, 0.18f, 0.95f), new Color(0.3f, 0.85f, 1.0f, 0.9f), 1.5f * scale);

                var titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(14 * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                DrawPondShadowLabel(new Rect(x, y + 4f * scale, w, 22f * scale), $"🐟 {hoveredFish.fishData.speciesName.ToUpper()}", titleStyle, new Color(1f, 0.92f, 0.35f), Color.black, 1.5f);

                var statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(12 * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                if (hoveredFish.IsMature)
                {
                    DrawPondShadowLabel(new Rect(x, y + 26f * scale, w, 22f * scale), $"★ Đã lớn! (Bán được {hoveredFish.fishData.adultSellPrice} vàng)", statusStyle, new Color(0.3f, 1f, 0.5f), Color.black, 1.5f);
                }
                else
                {
                    int pct = Mathf.RoundToInt(hoveredFish.GrowthProgress * 100f);
                    DrawPondShadowLabel(new Rect(x, y + 26f * scale, w, 22f * scale), $"Đang lớn: {pct}%", statusStyle, Color.white, Color.black, 1.5f);
                }

                if (hoveredFish.IsHungry)
                {
                    DrawPondShadowLabel(new Rect(x, y + 48f * scale, w, 22f * scale), "⚠ Đang ĐÓI! (Cần rải thức ăn)", statusStyle, new Color(1f, 0.4f, 0.3f), Color.black, 1.5f);
                }
                else
                {
                    int minLeft = Mathf.CeilToInt(hoveredFish.hungerTimer / 60f);
                    DrawPondShadowLabel(new Rect(x, y + 48f * scale, w, 22f * scale), $"No bụng (còn {minLeft} phút)", statusStyle, new Color(0.4f, 0.92f, 1f), Color.black, 1.5f);
                }
            }
        }
    }
}
