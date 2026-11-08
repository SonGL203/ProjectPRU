using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class InventoryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        // 1. Kiem tra EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Object.DontDestroyOnLoad(esGO);
        }

        // 2. Kiem tra InventorySystem
        if (InventorySystem.Instance == null)
        {
            var invGO = new GameObject("InventorySystem", typeof(InventorySystem));
            Object.DontDestroyOnLoad(invGO);
        }

        // 2.0 Kiem tra SeasonManager
        if (SeasonManager.Instance == null && Object.FindFirstObjectByType<SeasonManager>() == null)
        {
            var seasonGO = new GameObject("SeasonManager", typeof(SeasonManager));
            Object.DontDestroyOnLoad(seasonGO);
        }

        // 2.1 Kiem tra CropManager
        if (CropManager.Instance == null && Object.FindFirstObjectByType<CropManager>() == null)
        {
            var cropGO = new GameObject("CropManager", typeof(CropManager));
            Object.DontDestroyOnLoad(cropGO);
        }

        // 2.2 Kiem tra FishManager
        if (FishManager.Instance == null && Object.FindFirstObjectByType<FishManager>() == null)
        {
            var fishMgrGO = new GameObject("FishManager", typeof(FishManager));
            Object.DontDestroyOnLoad(fishMgrGO);
        }

        // 2.3 Kiem tra FishVendor (dat gan bo ao toa do x=29.5, y=18.5)
        if (Object.FindFirstObjectByType<FishVendor>() == null)
        {
            var waterGO = GameObject.Find("Water");
            if (waterGO != null)
            {
                var vendorGO = new GameObject("FishVendor", typeof(BoxCollider2D), typeof(FishVendor));
                vendorGO.transform.position = new Vector3(29.5f, 18.5f, 0f);
            }
        }

        // 2.4 Kiem tra HouseStorageTrigger tren ngoi nha
        var houseGO = GameObject.Find("house (1)");
        if (houseGO != null)
        {
            if (houseGO.GetComponent<Collider2D>() == null)
            {
                var box = houseGO.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(2f, 1.8f);
            }
            if (houseGO.GetComponent<HouseStorageTrigger>() == null)
            {
                houseGO.AddComponent<HouseStorageTrigger>();
            }
        }

        // 3. Kiem tra UI Canvas
        var existingHotbar = Object.FindFirstObjectByType<HotbarUI>();
        var existingInvUI  = Object.FindFirstObjectByType<InventoryUI>();

        if (existingHotbar == null || existingInvUI == null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGO;
            if (canvas == null)
            {
                canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Object.DontDestroyOnLoad(canvasGO);
            }
            else
            {
                canvasGO = canvas.gameObject;
            }

            // Dam bao Canvas Scaler luon dung ScaleWithScreenSize
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (existingHotbar == null)
            {
                var hotbarGO = new GameObject("HotbarUI", typeof(RectTransform), typeof(HotbarUI));
                hotbarGO.transform.SetParent(canvasGO.transform, false);
            }

            if (existingInvUI == null)
            {
                var invUIGO = new GameObject("InventoryUI", typeof(RectTransform), typeof(InventoryUI));
                invUIGO.transform.SetParent(canvasGO.transform, false);
            }

            // Kiem tra SeasonClockUI
            if (Object.FindFirstObjectByType<SeasonClockUI>() == null)
            {
                var clockGO = new GameObject("SeasonClockUI", typeof(RectTransform), typeof(SeasonClockUI));
                clockGO.transform.SetParent(canvasGO.transform, false);
            }
        }
        else
        {
            // Neu da co san Canvas trong scene, dam bao co SeasonClockUI
            if (Object.FindFirstObjectByType<SeasonClockUI>() == null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    var clockGO = new GameObject("SeasonClockUI", typeof(RectTransform), typeof(SeasonClockUI));
                    clockGO.transform.SetParent(canvas.transform, false);
                }
            }
        }
    }
}

