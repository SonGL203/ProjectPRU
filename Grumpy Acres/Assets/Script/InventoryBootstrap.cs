using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class InventoryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        // 1. Kiem tra EventSystem (Khong dung DontDestroyOnLoad de tranh loi 2 EventSystems giua cac scene)
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
                Object.Destroy(eventSystems[i].gameObject);
        }
        else if (eventSystems.Length == 0)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
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

        // 2.2 Kiem tra WaterTilemapFishPond tren GameObject Water
        var waterObj = GameObject.Find("Water");
        if (waterObj != null && waterObj.GetComponent<WaterTilemapFishPond>() == null)
        {
            waterObj.AddComponent<WaterTilemapFishPond>();
        }

        // 2.2.1 Kiem tra FishManager
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

        // 3. Kiem tra UI Canvas va giu dung 1 HotbarUI duy nhat o duoi man hinh
        var existingHotbars = Object.FindObjectsByType<HotbarUI>(FindObjectsSortMode.None);
        if (existingHotbars.Length > 1)
        {
            for (int i = 1; i < existingHotbars.Length; i++)
                Object.Destroy(existingHotbars[i].gameObject);
        }

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
            }
            else
            {
                canvasGO = canvas.gameObject;
            }

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

            if (Object.FindFirstObjectByType<SeasonClockUI>() == null)
            {
                var clockGO = new GameObject("SeasonClockUI", typeof(RectTransform), typeof(SeasonClockUI));
                clockGO.transform.SetParent(canvasGO.transform, false);
            }
        }
        else
        {
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
