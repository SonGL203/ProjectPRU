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

    [Header("Mau sac Dat")]
    public Color drySoilColor = new(0.8f, 0.6f, 0.45f);
    public Color wateredSoilColor = new(0.5f, 0.35f, 0.22f);

    [Header("Cai dat trong trot")]
    [Tooltip("Khoang cach toi da nguoi choi co the click thao tac")]
    public float interactRange = 6f;

    [Tooltip("Kich thuoc phong to hien thi cay trong o dat")]
    public float plantScale = 4f;

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

        foreach (var kvp in plantedCrops)
        {
            var crop = kvp.Value;
            if (crop.seedData == null || crop.seedData.growthStages == null || crop.seedData.growthStages.Length == 0) continue;
            if (crop.IsMature) continue;

            float growthRate = crop.isWatered ? 1.0f : 0.2f;
            crop.currentGrowthTime += dt * growthRate;

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

    public bool PlantSeed(Vector3Int cell, ItemData seedData, ItemSlot slot)
    {
        if (!soilTilemap.HasTile(cell)) return false;
        if (plantedCrops.ContainsKey(cell)) return false;

        if (slot != null)
        {
            slot.amount--;
            if (slot.amount <= 0) slot.Clear();
            InventorySystem.Instance.NotifyChanged();
        }

        var plantGO = new GameObject($"Crop_{cell.x}_{cell.y}");
        plantGO.transform.SetParent(transform, false);

        Vector3 cellCenter = soilTilemap.GetCellCenterWorld(cell);
        plantGO.transform.position = cellCenter;
        plantGO.transform.localScale = Vector3.one * plantScale;

        var sr = plantGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        var planted = new PlantedCrop
        {
            cell = cell,
            seedData = seedData,
            currentGrowthTime = 0f,
            isWatered = wateredCells.Contains(cell),
            currentStage = 0,
            health = plotMaxHealth,
            plantInstance = plantGO,
            spriteRenderer = sr
        };

        UpdatePlantVisual(planted);
        plantedCrops[cell] = planted;
        return true;
    }

    public bool WaterSoil(Vector3Int cell)
    {
        if (!soilTilemap.HasTile(cell)) return false;

        wateredCells.Add(cell);
        soilTilemap.SetTileFlags(cell, TileFlags.None);
        soilTilemap.SetColor(cell, wateredSoilColor);

        if (plantedCrops.TryGetValue(cell, out var crop))
        {
            crop.isWatered = true;
        }

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
        if (soilTilemap.HasTile(cell))
        {
            soilTilemap.SetColor(cell, drySoilColor);
        }
    }

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
        soilTilemap.SetTileFlags(cell, TileFlags.None);
        soilTilemap.SetColor(cell, drySoilColor);
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
            crop.spriteRenderer.sprite = crop.seedData.growthStages[crop.currentStage];
        }
    }
}
