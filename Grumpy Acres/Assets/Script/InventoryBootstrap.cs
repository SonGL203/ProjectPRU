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

        // 2.1 Kiem tra CropManager
        if (CropManager.Instance == null && Object.FindFirstObjectByType<CropManager>() == null)
        {
            var cropGO = new GameObject("CropManager", typeof(CropManager));
            Object.DontDestroyOnLoad(cropGO);
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
        }
    }
}

