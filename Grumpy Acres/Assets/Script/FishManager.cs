using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class FishManager : MonoBehaviour
{
    public static FishManager Instance { get; private set; }

    [Header("Tilemap Nuoc")]
    public Tilemap waterTilemap;

    [Header("Thiet lap")]
    public float interactRange = 6f;
    public Sprite bubbleSprite;
    public Sprite pondSprite;

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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        FishItemsDatabase.Initialize();
        bubbleSprite = FishItemsDatabase.BubbleSprite;
        pondSprite = FishItemsDatabase.PondSprite;
    }

    void Start()
    {
        FindReferences();
    }

    void FindReferences()
    {
        if (!waterTilemap)
        {
            var waterGO = GameObject.Find("Water");
            if (waterGO) waterTilemap = waterGO.GetComponent<Tilemap>();
        }

        if (!playerTransform)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) playerTransform = p.transform;
            else
            {
                var pm = FindFirstObjectByType<PlayerMove>();
                if (pm) playerTransform = pm.transform;
            }
        }
    }

    void Update()
    {
        if (!waterTilemap || !playerTransform)
        {
            FindReferences();
        }

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
        for (int i = 0; i < FishAgent.AllFish.Count; i++)
        {
            var fish = FishAgent.AllFish[i];
            if (!fish) continue;
            if (Vector2.Distance(fish.transform.position, mouseWorld) < 0.6f)
            {
                hoveredFish = fish;
                break;
            }
        }
    }

    void HandleInteraction()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (FramePlacer.Active) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (Camera.main == null) return;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            worldPos.z = 0f;

            if (playerTransform != null && Vector2.Distance(playerTransform.position, worldPos) > interactRange)
                return;

            var activeSlot = InventorySystem.Instance != null ? InventorySystem.Instance.GetSelected() : null;
            var activeItem = activeSlot != null ? activeSlot.item : null;

            // 1. Check if clicked on a mature fish to harvest (either with FishNet or bare hands)
            if (TryHarvestClickedFish(worldPos, activeItem))
                return;

            // 2. Check if clicked on water or a fish pond
            bool isWaterTile = waterTilemap != null && waterTilemap.HasTile(waterTilemap.WorldToCell(worldPos));
            var pond = FishPond.FindPondAt(worldPos);

            if (isWaterTile || pond != null)
            {
                // Thả cá giống (FishFry)
                if (activeItem != null && (activeItem.itemType == ItemType.FishFry || activeItem.id.Contains("_fry")))
                {
                    var data = FishItemsDatabase.GetFishDataByItem(activeItem);
                    if (data != null)
                    {
                        if (pond != null && pond.IsFull)
                        {
                            ShowFloatingMessage(worldPos + Vector3.up * 0.4f, "Ao cá đã đầy!", Color.yellow);
                            return;
                        }

                        var newFish = SpawnFish(data, worldPos, pond != null ? null : waterTilemap);
                        if (pond != null && newFish != null)
                        {
                            pond.TryAddFish(newFish);
                        }

                        // Consume item
                        if (activeSlot != null)
                        {
                            activeSlot.amount--;
                            if (activeSlot.amount <= 0) activeSlot.Clear();
                            InventorySystem.Instance.NotifyChanged();
                        }

                        SpawnSplash(worldPos, 4);
                        ShowFloatingMessage(worldPos + Vector3.up * 0.4f, $"Đã thả {data.speciesName} giống!", new Color(0.2f, 0.8f, 1f));
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
                    ShowFloatingMessage(worldPos + Vector3.up * 0.4f, "Rải thức ăn cá! 🍞", new Color(1f, 0.85f, 0.3f));
                    return;
                }
            }

            // 3. Đặt Ao cá nhân tạo (Placeable Fish Pond) nếu đang cầm Item_FishPond
            if (activeItem != null && activeItem.id == "item_fish_pond")
            {
                PlaceFishPond(worldPos, activeSlot);
                return;
            }
        }
    }

    bool TryHarvestClickedFish(Vector3 clickPos, ItemData activeItem)
    {
        for (int i = 0; i < FishAgent.AllFish.Count; i++)
        {
            var fish = FishAgent.AllFish[i];
            if (!fish) continue;

            if (Vector2.Distance(fish.transform.position, clickPos) < 0.75f)
            {
                if (fish.IsMature)
                {
                    return fish.Harvest();
                }
                else
                {
                    int pct = Mathf.RoundToInt(fish.GrowthProgress * 100f);
                    ShowFloatingMessage(fish.transform.position + Vector3.up * 0.4f, $"Cá đang lớn ({pct}%)", Color.white);
                    return true;
                }
            }
        }
        return false;
    }

    public FishAgent SpawnFish(FishData data, Vector3 position, Tilemap waterMap)
    {
        var fishGO = new GameObject($"Fish_{data.speciesName}");
        var agent = fishGO.AddComponent<FishAgent>();
        agent.Init(data, position, waterMap);
        return agent;
    }

    public void SpawnFoodPellet(Vector3 position)
    {
        var foodGO = new GameObject("FishFoodPellet");
        foodGO.transform.position = position;
        var sr = foodGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.color = new Color(0.9f, 0.75f, 0.35f);
        if (FishItemsDatabase.BubbleSprite != null)
        {
            sr.sprite = FishItemsDatabase.BubbleSprite;
        }
        foodGO.transform.localScale = Vector3.one * 0.35f;
        foodGO.AddComponent<FishFoodPellet>();
    }

    public void PlaceFishPond(Vector3 position, ItemSlot slot)
    {
        var pondGO = new GameObject("CustomFishPond");
        pondGO.transform.position = position;
        pondGO.transform.localScale = Vector3.one * 1.5f;

        var sr = pondGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2; // Tren mat dat
        if (FishItemsDatabase.PondSprite != null)
        {
            sr.sprite = FishItemsDatabase.PondSprite;
        }

        var col = pondGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.5f, 1.8f);
        col.isTrigger = true;

        var pond = pondGO.AddComponent<FishPond>();
        pond.swimAreaSize = new Vector2(2.2f, 1.5f);

        if (slot != null)
        {
            slot.amount--;
            if (slot.amount <= 0) slot.Clear();
            InventorySystem.Instance.NotifyChanged();
        }

        SpawnSplash(position, 6);
        ShowFloatingMessage(position + Vector3.up * 0.5f, "Đã đặt Ao Cá! 🌊", new Color(0.3f, 0.9f, 1f));
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
            auto.seconds = UnityEngine.Random.Range(0.4f, 0.8f);
            AutoHide.Flash(auto);
            Destroy(pGO, 1f);
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
            if (ft.time <= 0f)
            {
                floatingTexts.RemoveAt(i);
            }
            else
            {
                floatingTexts[i] = ft;
            }
        }
    }

    void OnGUI()
    {
        if (Camera.main == null) return;

        // Draw floating text messages
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

            float w = 220f;
            float h = 26f;
            GUI.Label(new Rect(screen.x - w * 0.5f, Screen.height - screen.y - h, w, h), ft.text, textStyle);
        }

        // Draw hover info HUD
        if (hoveredFish != null && hoveredFish.fishData != null)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(hoveredFish.transform.position + Vector3.up * 0.6f);
            if (screen.z > 0)
            {
                float w = 200f;
                float h = 60f;
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
                    statusStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
                    GUI.Label(new Rect(x, y + 20, w, 18), "★ Đã trưởng thành (Click vớt cá)", statusStyle);
                }
                else
                {
                    int pct = Mathf.RoundToInt(hoveredFish.GrowthProgress * 100f);
                    statusStyle.normal.textColor = Color.white;
                    GUI.Label(new Rect(x, y + 20, w, 18), $"Sinh trưởng: {pct}%", statusStyle);
                }

                string foodStatus = hoveredFish.IsFed ? "No bụng (+250% tốc độ)" : "Đang đói (Cho ăn để lớn nhanh)";
                statusStyle.normal.textColor = hoveredFish.IsFed ? new Color(0.3f, 0.9f, 1f) : new Color(0.9f, 0.6f, 0.4f);
                GUI.Label(new Rect(x, y + 36, w, 18), foodStatus, statusStyle);
            }
        }
    }
}
