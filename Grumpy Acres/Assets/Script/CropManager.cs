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

    [Serializable]
    public class PlantedCrop
    {
        public Vector3Int cell;
        public ItemData seedData;
        public float currentGrowthTime;
        public bool isWatered;
        public int currentStage;
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

        // Tu dong tim Tilemap Soil neu chua gan
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

            // Cay lon cham hon neu chua tuoi nuoc (chi 20% toc do), tuoi nuoc thi lon 100% toc do
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

        // Khong tuong tac neu chuot dang tro tren giao dien UI (tranh click xuyen UI)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Click hoac giu chuot trai de rai hat / tuoi nuoc / thu hoach
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

            // Kiem tra khoang cach toi nguoi choi
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

            // Lay vat pham dang cam tren Hotbar
            var activeSlot = InventorySystem.Instance != null ? InventorySystem.Instance.GetSelected() : null;
            var activeItem = activeSlot != null ? activeSlot.item : null;

            // 1. Uu tien THU HOACH neu o do co cay da chin (chi can click chuot trai)
            if (mouse.leftButton.wasPressedThisFrame && plantedCrops.TryGetValue(cell, out var matureCrop) && matureCrop.IsMature)
            {
                HarvestCrop(cell, matureCrop);
                return;
            }

            // 2. Neu o dat da cuoc
            if (soilTilemap.HasTile(cell))
            {
                // A. Neu dang cam Binh tuoi nuoc (Watering Can) -> Tuoi nuoc
                if (activeItem != null && activeItem.itemType == ItemType.Tool && activeItem.toolType == ToolType.WateringCan)
                {
                    WaterSoil(cell);
                    return;
                }

                // B. Neu dang cam Hat giong (Seed) -> Rai hat
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
        if (plantedCrops.ContainsKey(cell)) return false; // Da co cay trong o nay

        // Tru 1 hat trong kho do
        if (slot != null)
        {
            slot.amount--;
            if (slot.amount <= 0) slot.Clear();
            InventorySystem.Instance.NotifyChanged();
        }

        // Tao doi tuong cay
        var plantGO = new GameObject($"Crop_{cell.x}_{cell.y}");
        plantGO.transform.SetParent(transform, false);

        Vector3 cellCenter = soilTilemap.GetCellCenterWorld(cell);
        plantGO.transform.position = cellCenter;
        plantGO.transform.localScale = Vector3.one * plantScale;

        var sr = plantGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5; // Hien tren lop mat dat

        var planted = new PlantedCrop
        {
            cell = cell,
            seedData = seedData,
            currentGrowthTime = 0f,
            isWatered = wateredCells.Contains(cell),
            currentStage = 0,
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

        // Cho nong san vao Inventory (AddItem tra ve so luong con thua neu khong nhet het)
        int leftover = InventorySystem.Instance.AddItem(yieldItem, 1);
        if (leftover > 0)
        {
            Debug.Log("Inventory da day, khong the thu hoach!");
            return;
        }

        // Xoa instance cay
        if (crop.plantInstance != null)
        {
            Destroy(crop.plantInstance);
        }
        plantedCrops.Remove(cell);

        // Reset lai mau dat kho neu can
        wateredCells.Remove(cell);
        if (soilTilemap.HasTile(cell))
        {
            soilTilemap.SetColor(cell, drySoilColor);
        }
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
