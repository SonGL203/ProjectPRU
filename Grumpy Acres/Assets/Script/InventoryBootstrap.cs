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

        // 2.1 Kiem tra CropManager
        if (CropManager.Instance == null && Object.FindFirstObjectByType<CropManager>() == null)
        {
            var cropGO = new GameObject("CropManager", typeof(CropManager));
            Object.DontDestroyOnLoad(cropGO);
        }

        // 3. Xoa tat ca HotbarUI trung lap - LUON CHI CO DUY NHAT 1 THANH CONG CU
        var allHotbars = Object.FindObjectsByType<HotbarUI>(FindObjectsSortMode.None);
        if (allHotbars.Length > 1)
        {
            for (int i = 1; i < allHotbars.Length; i++)
            {
                if (allHotbars[i] != null)
                {
                    // Xoa ca canvas cha neu canvas do la InventoryCanvas duoc sinh tu dong
                    var parentCanvas = allHotbars[i].GetComponentInParent<Canvas>();
                    if (parentCanvas != null && parentCanvas.gameObject.name == "InventoryCanvas")
                    {
                        Object.Destroy(parentCanvas.gameObject);
                    }
                    else
                    {
                        Object.Destroy(allHotbars[i].gameObject);
                    }
                }
            }
        }

        // 4. Neu da co HotbarUI thi ket thuc ngay, khong bao gio tao them Canvas hay Hotbar nua
        if (Object.FindFirstObjectByType<HotbarUI>() != null)
        {
            return;
        }

        // 5. Chi tao Canvas neu chua he co bat ky Canvas nao trong game
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

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (Object.FindFirstObjectByType<HotbarUI>() == null)
        {
            var hotbarGO = new GameObject("HotbarUI", typeof(RectTransform), typeof(HotbarUI));
            hotbarGO.transform.SetParent(canvasGO.transform, false);
        }

        if (Object.FindFirstObjectByType<InventoryUI>() == null)
        {
            var invUIGO = new GameObject("InventoryUI", typeof(RectTransform), typeof(InventoryUI));
            invUIGO.transform.SetParent(canvasGO.transform, false);
        }
    }
}
