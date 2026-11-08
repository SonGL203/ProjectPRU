using UnityEngine;
using UnityEditor;

public static class ConvertInventoryToGameObject
{
    [MenuItem("Tools/Convert Inventory To GameObjects")]
    public static void Convert()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Khong tim thay Canvas!");
            return;
        }

        // Xoa InventoryWindow / InventoryManager cu neu co de tao lai kich thuoc moi
        var oldWin = canvas.transform.Find("InventoryWindow");
        if (oldWin != null) Object.DestroyImmediate(oldWin.gameObject);
        var oldMgr = canvas.transform.Find("InventoryManager");
        if (oldMgr != null) Object.DestroyImmediate(oldMgr.gameObject);

        // pixelScale = 5.5f giup Kho va Hotbar to ro, chiem khoang 50-60% man hinh de nhin ro pixel va thao tac keo tha de dang
        float pixelScale = 5.5f;
        float winW = 100f * pixelScale;
        float winH = 102f * pixelScale;

        // Cap nhat luon scale cho Hotbar co san
        var hotbar = Object.FindFirstObjectByType<HotbarUI>();
        if (hotbar != null)
        {
            hotbar.pixelScale = pixelScale;
            hotbar.bottomOffset = 20f;
            hotbar.SendMessage("Build", SendMessageOptions.DontRequireReceiver);
            EditorUtility.SetDirty(hotbar);
        }

        // Load Sprites
        Sprite winSprite = null;
        var allSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Inventory.png");
        foreach (var s in allSprites)
        {
            if (s is Sprite sp && sp.name == "Inventory_2")
            {
                winSprite = sp;
                break;
            }
        }
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Inventory/Icon_Coin.png");

        // 1. Root InventoryWindow
        var winGO = new GameObject("InventoryWindow", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(InventoryUI));
        winGO.transform.SetParent(canvas.transform, false);

        var winRT = winGO.GetComponent<RectTransform>();
        winRT.anchorMin = new Vector2(0.5f, 0.5f);
        winRT.anchorMax = new Vector2(0.5f, 0.5f);
        winRT.pivot     = new Vector2(0.5f, 0.5f);
        winRT.sizeDelta = new Vector2(winW, winH);
        winRT.anchoredPosition = new Vector2(0f, 60f);

        var winImg = winGO.GetComponent<UnityEngine.UI.Image>();
        winImg.sprite = winSprite;
        winImg.type = UnityEngine.UI.Image.Type.Simple;

        var invUI = winGO.GetComponent<InventoryUI>();
        invUI.windowSprite = winSprite;
        invUI.coinSprite = coinSprite;
        invUI.pixelScale = pixelScale;

        // 2. Nut X Close Button
        var closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        closeBtnGO.transform.SetParent(winGO.transform, false);
        var closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.pivot     = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = new Vector2(-4f * pixelScale, -4f * pixelScale);
        closeBtnRT.sizeDelta = new Vector2(12f * pixelScale, 12f * pixelScale);
        var closeImg = closeBtnGO.GetComponent<UnityEngine.UI.Image>();
        closeImg.color = Color.clear;
        var btn = closeBtnGO.GetComponent<UnityEngine.UI.Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, invUI.Close);

        // 3. 20 Slots
        int[] colXs = { 11, 27, 43, 59, 75 };
        int[] rowYs = { 17, 33, 49, 65 };
        float slotDim = 14f * pixelScale;

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                int indexInGrid = row * 5 + col;
                int globalIndex = 10 + indexInGrid;

                var slotGO = new GameObject($"GSlot_{indexInGrid}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(SlotUI));
                slotGO.transform.SetParent(winGO.transform, false);

                var slotRT = slotGO.GetComponent<RectTransform>();
                slotRT.anchorMin = new Vector2(0f, 1f);
                slotRT.anchorMax = new Vector2(0f, 1f);
                slotRT.pivot     = new Vector2(0f, 1f);
                slotRT.sizeDelta = new Vector2(slotDim, slotDim);
                slotRT.anchoredPosition = new Vector2(colXs[col] * pixelScale, -rowYs[row] * pixelScale);

                var slotBg = slotGO.GetComponent<UnityEngine.UI.Image>();
                slotBg.color = Color.clear;
                slotBg.raycastTarget = true;

                // Icon
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                iconGO.transform.SetParent(slotGO.transform, false);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = new Vector2(4, 4);
                iconRT.offsetMax = new Vector2(-4, -4);
                var iconImg = iconGO.GetComponent<UnityEngine.UI.Image>();
                iconImg.raycastTarget = false;
                iconImg.preserveAspect = true;
                iconImg.enabled = false;

                // Amount Text
                var amtGO = new GameObject("Amount", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                amtGO.transform.SetParent(slotGO.transform, false);
                var amtRT = amtGO.GetComponent<RectTransform>();
                amtRT.anchorMin = Vector2.zero;
                amtRT.anchorMax = Vector2.one;
                amtRT.offsetMin = new Vector2(2, 2);
                amtRT.offsetMax = new Vector2(-2, -2);
                var amtText = amtGO.GetComponent<TMPro.TextMeshProUGUI>();
                amtText.fontSize = 13;
                amtText.fontStyle = TMPro.FontStyles.Bold;
                amtText.color = Color.white;
                amtText.alignment = TMPro.TextAlignmentOptions.BottomRight;
                amtText.raycastTarget = false;
                amtText.enabled = false;

                // Hover Highlight
                var hlGO = new GameObject("HoverOverlay", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                hlGO.transform.SetParent(slotGO.transform, false);
                var hlRT = hlGO.GetComponent<RectTransform>();
                hlRT.anchorMin = Vector2.zero;
                hlRT.anchorMax = Vector2.one;
                hlRT.sizeDelta = Vector2.zero;
                var hlImg = hlGO.GetComponent<UnityEngine.UI.Image>();
                hlImg.color = new Color(1f, 1f, 1f, 0.2f);
                hlImg.raycastTarget = false;
                hlImg.enabled = false;

                var slotUI = slotGO.GetComponent<SlotUI>();
                slotUI.iconImage = iconImg;
                slotUI.amountText = amtText;
                slotUI.highlightOverlay = hlImg;
                slotUI.Init(globalIndex, false);
            }
        }

        // 4. Gold Badge (So tien ngang hang tam chinh giua voi dong xu)
        // Dong xu pixel: x=13..23 (tam x=18), y=83..92 (tam y=87.5)
        var goldBadgeGO = new GameObject("GoldBadge", typeof(RectTransform));
        goldBadgeGO.transform.SetParent(winGO.transform, false);
        var goldRT = goldBadgeGO.GetComponent<RectTransform>();
        goldRT.anchorMin = new Vector2(0f, 1f);
        goldRT.anchorMax = new Vector2(0f, 1f);
        goldRT.pivot     = new Vector2(0f, 0.5f);
        // Dat pivot o (0, 0.5) va y = -87.5f * pixelScale de dong tam hoan hao voi dong xu
        goldRT.anchoredPosition = new Vector2(27f * pixelScale, -87.5f * pixelScale);
        goldRT.sizeDelta        = new Vector2(35f * pixelScale, 14f * pixelScale);

        // Text so vang
        var textGO = new GameObject("GoldText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textGO.transform.SetParent(goldBadgeGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var goldText = textGO.GetComponent<TMPro.TextMeshProUGUI>();
        goldText.fontSize = 20;
        goldText.fontStyle = TMPro.FontStyles.Bold;
        goldText.color = new Color(0.25f, 0.18f, 0.1f);
        goldText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        goldText.raycastTarget = false;
        goldText.text = "0";

        // 5. Che vien ngoc xanh (Dung ma mau RGB chinh xac 229, 214, 161 cua giay de dong nhat 100%)
        // Vien ngoc: x=63..72 (tam x=67.5), y=83..93 (tam y=88)
        var coverGO = new GameObject("GemCover", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        coverGO.transform.SetParent(winGO.transform, false);
        var coverRT = coverGO.GetComponent<RectTransform>();
        coverRT.anchorMin = new Vector2(0f, 1f);
        coverRT.anchorMax = new Vector2(0f, 1f);
        coverRT.pivot     = new Vector2(0.5f, 0.5f);
        coverRT.anchoredPosition = new Vector2(67.5f * pixelScale, -88f * pixelScale);
        coverRT.sizeDelta        = new Vector2(11f * pixelScale, 12f * pixelScale);
        var coverImg = coverGO.GetComponent<UnityEngine.UI.Image>();
        coverImg.color = new Color(229f / 255f, 214f / 255f, 161f / 255f, 1f); // Mau nen goc cua sprite
        coverImg.raycastTarget = false;

        // Giau kho luc khoi dau
        winGO.SetActive(false);

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(canvas.gameObject.scene);

        Selection.activeGameObject = winGO;
        Debug.Log("[Convert] Da tao thanh cong GameObject InventoryWindow trong Canvas!");
    }
}

