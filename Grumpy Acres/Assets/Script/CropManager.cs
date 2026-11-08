using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    [Header("Tilemaps lien quan")]
    public Tilemap soilTilemap;
    public Tilemap farmZoneTilemap;

    [Header("Prefab Cay trong")]
    [Tooltip("Prefab dai dien cho cay trong (CropPlant.prefab). Neu de trong se tu tao fallback")]
    public CropPlant cropPlantPrefab;

    [Header("Mau sac Dat")]
    public Color drySoilColor = new(0.8f, 0.6f, 0.45f);
    public Color wateredSoilColor = new(0.5f, 0.35f, 0.22f);

    [Header("Cai dat trong trot")]
    [Tooltip("Khoang cach toi da nguoi choi co the click thao tac")]
    public float interactRange = 6f;

    [Tooltip("Kich thuoc phong to hien thi cay trong o dat")]
    public float plantScale = 9f;

    [Tooltip("Do lech vi tri de goc cay nam gon ben trong o dat")]
    public Vector3 plantOffset = new(0f, 0.22f, 0f);

    [Tooltip("Mau cua moi o dat dang trong cay khi bi enemy pha")]
    public float plotMaxHealth = 30f;

    [Serializable]
    public class PlantedCrop
    {
        public Vector3Int cell;
        public ItemData seedData;
        public float currentGrowthTime;
        public bool isWatered;
        public int currentStage;
        public float health;
        public GameObject plantInstance;
        public SpriteRenderer spriteRenderer;
        public CropPlant cropPlantComponent;

        public bool IsMature => currentStage >= (seedData.growthStages != null ? seedData.growthStages.Length - 1 : 0);
    }

    readonly Dictionary<Vector3Int, PlantedCrop> plantedCrops = new();
    readonly HashSet<Vector3Int> wateredCells = new();

    Transform playerTransform;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (soilTilemap == null)
        {
            var soilGO = GameObject.Find("Soil");
            if (soilGO != null) soilTilemap = soilGO.GetComponent<Tilemap>();
        }

        if (farmZoneTilemap == null)
        {
            var fzGO = GameObject.Find("FarmZone");
            if (fzGO != null) farmZoneTilemap = fzGO.GetComponent<Tilemap>();
        }

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        UpdateCropGrowth();
        HandleMouseInteraction();
    }

    void UpdateCropGrowth()
    {
        float dt = Time.deltaTime;

        // Lay he so toc do lon theo mua (SeasonManager)
        float seasonMultiplier = SeasonManager.Instance != null ? SeasonManager.Instance.GetCurrentGrowthMultiplier() : 1.0f;

        foreach (var kvp in plantedCrops)
        {
            var crop = kvp.Value;
            if (crop.seedData == null || crop.seedData.growthStages == null || crop.seedData.growthStages.Length == 0) continue;
            if (crop.IsMature) continue;

            // Bat buoc phai tuoi nuoc thi cay moi bat dau count down lon!
            if (!crop.isWatered) continue;
            crop.currentGrowthTime += dt * seasonMultiplier;


            int totalStages = crop.seedData.growthStages.Length;
            float totalTime = Mathf.Max(10f, crop.seedData.growthSeconds);
            float timePerStage = totalTime / totalStages;

            int newStage = Mathf.Clamp((int)(crop.currentGrowthTime / timePerStage), 0, totalStages - 1);
            if (newStage != crop.currentStage)
            {
                crop.currentStage = newStage;
                UpdatePlantVisual(crop);
            }
        }
    }

    void HandleMouseInteraction()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (FramePlacer.Active) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (mouse.leftButton.isPressed)
        {
            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 worldPos = Camera.main != null ? Camera.main.ScreenToWorldPoint(mouseScreen) : Vector3.zero;
            worldPos.z = 0f;

            if (soilTilemap == null)
            {
                var soilGO = GameObject.Find("Soil");
                if (soilGO != null) soilTilemap = soilGO.GetComponent<Tilemap>();
                if (soilTilemap == null) return;
            }

            Vector3Int cell = soilTilemap.WorldToCell(worldPos);

            if (farmZoneTilemap == null)
            {
                var fzGO = GameObject.Find("FarmZone");
                if (fzGO != null) farmZoneTilemap = fzGO.GetComponent<Tilemap>();
            }

            // Chuan hoa vi tri cell dat trong FarmZone de khong bi lech/tran ra vung co xanh
            Vector3Int fzCell = farmZoneTilemap != null ? farmZoneTilemap.WorldToCell(worldPos) : cell;
            if (farmZoneTilemap != null && !farmZoneTilemap.HasTile(fzCell))
                return;

            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null)
            {
                if (Vector2.Distance(playerTransform.position, worldPos) > interactRange)
                    return;
            }

            var activeSlot = InventorySystem.Instance != null ? InventorySystem.Instance.GetSelected() : null;
            var activeItem = activeSlot != null ? activeSlot.item : null;

            if (mouse.leftButton.wasPressedThisFrame && plantedCrops.TryGetValue(cell, out var matureCrop) && matureCrop.IsMature)
            {
                HarvestCrop(cell, matureCrop);
                return;
            }

            if (soilTilemap.HasTile(cell))
            {
                if (activeItem != null && activeItem.itemType == ItemType.Tool && activeItem.toolType == ToolType.WateringCan)
                {
                    WaterSoil(cell);
                    return;
                }

                if (activeItem != null && activeItem.itemType == ItemType.Seed)
                {
                    PlantSeed(cell, activeItem, activeSlot);
                    return;
                }
            }
        }
    }

    public bool IsSoilTilled(Vector3Int cell)
    {
        if (soilTilemap == null || !soilTilemap.HasTile(cell)) return false;
        Color c = soilTilemap.GetColor(cell);
        // Neu dat con nguyen mau ban dau (Color.white hoac alpha == 0) thi la chua cuoc
        if (c == Color.white || c.a <= 0.05f) return false;
        return true;
    }

    public bool PlantSeed(Vector3Int cell, ItemData seedData, ItemSlot slot)
    {
        if (!soilTilemap.HasTile(cell)) return false;
        // Chi cho phep trong cay khi dat da duoc cuoc
        if (!IsSoilTilled(cell)) return false;
        if (plantedCrops.ContainsKey(cell)) return false;

        if (slot != null)
        {
            slot.amount--;
            if (slot.amount <= 0) slot.Clear();
            InventorySystem.Instance.NotifyChanged();
        }

        GameObject plantGO;
        SpriteRenderer sr;
        CropPlant cp = null;

        Vector3 cellCenter = soilTilemap.GetCellCenterWorld(cell);
        Vector3 spawnPos = cellCenter + plantOffset;

        if (cropPlantPrefab != null)
        {
            cp = Instantiate(cropPlantPrefab, spawnPos, Quaternion.identity, transform);
            cp.name = $"Crop_{cell.x}_{cell.y}";
            cp.transform.localScale = Vector3.one * plantScale;
            plantGO = cp.gameObject;
            sr = cp.spriteRenderer != null ? cp.spriteRenderer : cp.GetComponent<SpriteRenderer>();
        }
        else
        {
            plantGO = new GameObject($"Crop_{cell.x}_{cell.y}");
            plantGO.transform.SetParent(transform, false);
            plantGO.transform.position = spawnPos;
            plantGO.transform.localScale = Vector3.one * plantScale;

            sr = plantGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            cp = plantGO.AddComponent<CropPlant>();
            cp.spriteRenderer = sr;
        }

        var planted = new PlantedCrop
        {
            cell = cell,
            seedData = seedData,
            currentGrowthTime = 0f,
            isWatered = wateredCells.Contains(cell),
            currentStage = 0,
            health = plotMaxHealth,
            plantInstance = plantGO,
            spriteRenderer = sr,
            cropPlantComponent = cp
        };

        UpdatePlantVisual(planted);
        plantedCrops[cell] = planted;
        return true;
    }

    public bool WaterSoil(Vector3Int cell)
    {
        // 1. Bat buoc phai co dat da dao (tilled soil tile)
        if (!soilTilemap.HasTile(cell)) return false;

        // 2. Bat buoc phai co cay/hat giong da gieo o o dat nay moi cho phep tuoi nuoc
        if (!plantedCrops.TryGetValue(cell, out var crop)) return false;

        wateredCells.Add(cell);
        soilTilemap.SetTileFlags(cell, TileFlags.None);
        soilTilemap.SetColor(cell, wateredSoilColor);

        crop.isWatered = true;
        return true;
    }

    public void HarvestCrop(Vector3Int cell, PlantedCrop crop)
    {
        var yieldItem = crop.seedData.cropYield != null ? crop.seedData.cropYield : crop.seedData;

        int leftover = InventorySystem.Instance.AddItem(yieldItem, 1);
        if (leftover > 0)
        {
            Debug.Log("Inventory da day, khong the thu hoach!");
            return;
        }

        if (crop.plantInstance != null)
        {
            Destroy(crop.plantInstance);
        }
        plantedCrops.Remove(cell);

        wateredCells.Remove(cell);
        
        // Khi thu hoach: Tra dat ve trang thai ban dau chua cuoc (mau dat goc Color.white)
        if (soilTilemap.HasTile(cell))
        {
            soilTilemap.SetTileFlags(cell, TileFlags.None);
            soilTilemap.SetColor(cell, Color.white);
        }
    }

    public System.Collections.Generic.IEnumerable<Vector3Int> PlotCells => plantedCrops.Keys;

    public bool HasPlot(Vector3Int cell) => plantedCrops.ContainsKey(cell);

    public bool TryGetNearestPlot(Vector3 from, out Vector3Int cell, out Vector3 worldPos)
    {
        cell = default;
        worldPos = default;
        float best = float.MaxValue;
        bool found = false;

        foreach (var kvp in plantedCrops)
        {
            Vector3 pos = soilTilemap.GetCellCenterWorld(kvp.Key);
            float d = (pos - from).sqrMagnitude;
            if (d >= best) continue;
            best = d;
            cell = kvp.Key;
            worldPos = pos;
            found = true;
        }
        return found;
    }

    public void DamagePlot(Vector3Int cell, float amount)
    {
        if (!plantedCrops.TryGetValue(cell, out var crop)) return;
        crop.health -= amount;
        if (crop.health > 0f) return;

        if (crop.plantInstance != null) Destroy(crop.plantInstance);
        plantedCrops.Remove(cell);
        wateredCells.Remove(cell);
        if (soilTilemap.HasTile(cell))
        {
            soilTilemap.SetTileFlags(cell, TileFlags.None);
            soilTilemap.SetColor(cell, Color.white);
        }
    }

    public void DestroyAllFarmland()
    {
        foreach (var crop in plantedCrops.Values)
            if (crop.plantInstance != null) Destroy(crop.plantInstance);
        plantedCrops.Clear();
        wateredCells.Clear();
        if (soilTilemap != null) soilTilemap.ClearAllTiles();
    }

    void UpdatePlantVisual(PlantedCrop crop)
    {
        if (crop.spriteRenderer == null || crop.seedData == null || crop.seedData.growthStages == null) return;
        if (crop.currentStage >= 0 && crop.currentStage < crop.seedData.growthStages.Length)
        {
            var stageSprite = crop.seedData.growthStages[crop.currentStage];
            if (crop.cropPlantComponent != null)
            {
                crop.cropPlantComponent.SetStage(stageSprite, crop.IsMature);
            }
            else
            {
                crop.spriteRenderer.sprite = stageSprite;
            }
        }
    }
}
