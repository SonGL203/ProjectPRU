using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class InventoryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        // 1. Kiem tra EventSystem - luon dam bao duy nhat 1 EventSystem, xoa sach trung lap
        var allES = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allES.Length > 1)
        {
            for (int i = 1; i < allES.Length; i++)
            {
                if (allES[i] != null) Object.Destroy(allES[i].gameObject);
            }
        }
        else if (allES.Length == 0)
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

        // 3. Xoa tat ca HotbarUI trung lap - LUON CHI CO DUY NHAT 1 THANH CONG CU
        var allHotbars = Object.FindObjectsByType<HotbarUI>(FindObjectsSortMode.None);
        for (int i = 1; i < allHotbars.Length; i++)
        {
            if (allHotbars[i] == null) continue;
            // Xoa ca canvas cha neu canvas do la InventoryCanvas duoc sinh tu dong
            var parentCanvas = allHotbars[i].GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.gameObject.name == "InventoryCanvas")
                Object.Destroy(parentCanvas.gameObject);
            else
                Object.Destroy(allHotbars[i].gameObject);
        }

        // 4. Dam bao co Canvas (chi tao neu chua co Canvas nao)
        var existingCanvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO;
        if (existingCanvas == null)
        {
            canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.DontDestroyOnLoad(canvasGO);
        }
        else
        {
            canvasGO = existingCanvas.gameObject;
        }

        // 5. SeasonClockUI luon phai co
        if (Object.FindFirstObjectByType<SeasonClockUI>() == null)
        {
            var clockGO = new GameObject("SeasonClockUI", typeof(RectTransform), typeof(SeasonClockUI));
            clockGO.transform.SetParent(canvasGO.transform, false);
        }

        // Neu da co HotbarUI thi khong tao them Hotbar/Inventory
        if (Object.FindFirstObjectByType<HotbarUI>() != null)
        {
            return;
        }

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var hotbarGO = new GameObject("HotbarUI", typeof(RectTransform), typeof(HotbarUI));
        hotbarGO.transform.SetParent(canvasGO.transform, false);

        if (Object.FindFirstObjectByType<InventoryUI>() == null)
        {
            var invUIGO = new GameObject("InventoryUI", typeof(RectTransform), typeof(InventoryUI));
            invUIGO.transform.SetParent(canvasGO.transform, false);
        }
    }
}
