using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Sprites")]
    [Tooltip("Sprite cua so Inventory (Inventory_1 trong Inventory.png)")]
    public Sprite windowSprite;

    [Tooltip("Sprite vien sang khi hover vao slot")]
    public Sprite hoverSprite;

    [Tooltip("Sprite dong xu tien vang")]
    public Sprite coinSprite;

    [Header("Scale & Kich thuoc")]
    [Tooltip("Ti le phong to cua sprite pixel art (100x102 * scale)")]
    public float pixelScale = 5.5f;

    [Header("Trang thai")]
    public bool isOpen = false;

    // Runtime UI
    GameObject windowPanel;
    SlotUI[] gridSlots;

    // Tooltip Hover Floating Box (Bay theo chuot / hien ngay tren item)
    GameObject tooltipGO;
    RectTransform tooltipRT;
    TextMeshProUGUI tooltipTitleText;
    TextMeshProUGUI tooltipTypeText;
    TextMeshProUGUI tooltipDescText;

    // Tien vang trong kho
    TextMeshProUGUI goldAmountText;

    // Drag & Drop / Held Item
    Canvas rootCanvas;
    RectTransform heldItemRT;
    Image heldItemIcon;
    TextMeshProUGUI heldItemAmountText;

    ItemSlot heldSlot = new();
    int sourceSlotIndex = -1;
    bool isDragging = false;

    public bool IsHoldingItem => !heldSlot.IsEmpty;

    void Awake()
    {
        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();

        // Gioi han pixelScale hop ly de tranh loi
        if (pixelScale <= 0f)
        {
            pixelScale = 5.5f;
        }

        // Kiem tra xem ban da tao san GameObject InventoryWindow hay chua
        var existingSlots = GetComponentsInChildren<SlotUI>(true);
        if (existingSlots != null && existingSlots.Length >= 20)
        {
            windowPanel = gameObject; // Dung truc tiep GameObject nay
            gridSlots = existingSlots;
            goldAmountText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        else
        {
            // Reset Transform ve tam Canvas (Center) de khong bi lech toa do xuong duoi
            transform.localPosition = Vector3.zero;

            // Stretch RectTransform cha de anchor center hoat dong dung
            var myRT = GetComponent<RectTransform>();
            if (myRT != null)
            {
                myRT.anchorMin = Vector2.zero;
                myRT.anchorMax = Vector2.one;
                myRT.offsetMin = Vector2.zero;
                myRT.offsetMax = Vector2.zero;
                myRT.anchoredPosition = Vector2.zero;
            }

#if UNITY_EDITOR
            if (windowSprite == null)
            {
                var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Inventory.png");
                foreach (var s in sprites)
                {
                    if (s is Sprite sp && sp.name == "Inventory_2") { windowSprite = sp; break; }
                }
            }
            if (coinSprite == null)
            {
                coinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Inventory/Icon_Coin.png");
            }
#endif

            BuildWindow();
        }

        BuildHeldItemCursor();
        BuildTooltipFloating();
    }

    void BuildTooltipFloating()
    {
        tooltipGO = new GameObject("TooltipHoverBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        // Đặt cha trực tiếp lên rootCanvas hoặc GameObject này để hiển thị trên cùng
        Transform parentTransform = rootCanvas != null ? rootCanvas.transform : transform;
        tooltipGO.transform.SetParent(parentTransform, false);

        tooltipRT = tooltipGO.GetComponent<RectTransform>();
        tooltipRT.anchorMin = new Vector2(0f, 0f);
        tooltipRT.anchorMax = new Vector2(0f, 0f);
        tooltipRT.pivot     = new Vector2(0f, 0f); // Pivot góc dưới trái để bay phía trên con trỏ chuột
        tooltipRT.sizeDelta = new Vector2(290f, 135f);

        var bgImg = tooltipGO.GetComponent<Image>();
        bgImg.color = new Color(0.10f, 0.07f, 0.05f, 0.96f); // Nền gỗ nâu đậm pixel art sang trọng
        bgImg.raycastTarget = false;

        var outline = tooltipGO.GetComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.8f, 0.55f, 1f); // Viền vàng sáng rõ ràng
        outline.effectDistance = new Vector2(2f, -2f);

        // Tim font LiberationSans SDF mac dinh neu co
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // 1. Tiêu đề (Tên vật phẩm)
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(tooltipGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot     = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(12f, -10f);
        titleRT.sizeDelta = new Vector2(-24f, 26f);

        tooltipTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) tooltipTitleText.font = defaultFont;
        tooltipTitleText.fontSize = 18;
        tooltipTitleText.fontStyle = FontStyles.Bold;
        tooltipTitleText.color = new Color(1f, 0.95f, 0.6f);
        tooltipTitleText.raycastTarget = false;

        // 2. Phân loại / Thông số phụ
        var typeGO = new GameObject("Type", typeof(RectTransform), typeof(TextMeshProUGUI));
        typeGO.transform.SetParent(tooltipGO.transform, false);
        var typeRT = typeGO.GetComponent<RectTransform>();
        typeRT.anchorMin = new Vector2(0f, 1f);
        typeRT.anchorMax = new Vector2(1f, 1f);
        typeRT.pivot     = new Vector2(0f, 1f);
        typeRT.anchoredPosition = new Vector2(12f, -38f);
        typeRT.sizeDelta = new Vector2(-24f, 20f);

        tooltipTypeText = typeGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) tooltipTypeText.font = defaultFont;
        tooltipTypeText.fontSize = 14;
        tooltipTypeText.fontStyle = FontStyles.Bold;
        tooltipTypeText.color = new Color(0.55f, 0.95f, 0.55f);
        tooltipTypeText.raycastTarget = false;

        // 3. Mô tả chi tiết
        var descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(tooltipGO.transform, false);
        var descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 0f);
        descRT.anchorMax = new Vector2(1f, 1f);
        descRT.pivot     = new Vector2(0f, 0f);
        descRT.offsetMin = new Vector2(12f, 10f);
        descRT.offsetMax = new Vector2(-12f, -62f);

        tooltipDescText = descGO.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) tooltipDescText.font = defaultFont;
        tooltipDescText.fontSize = 14;
        tooltipDescText.color = new Color(1f, 1f, 1f); // Trắng sáng 100%
        tooltipDescText.raycastTarget = false;
        tooltipDescText.textWrappingMode = TextWrappingModes.Normal;

        tooltipGO.SetActive(false);
    }

    void Start()
    {
        if (windowPanel != null)
            windowPanel.SetActive(false);

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged += Refresh;
        }

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnChanged += UpdateGoldDisplay;
            UpdateGoldDisplay(PlayerWallet.Instance.gold);
        }
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged -= Refresh;
        }

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnChanged -= UpdateGoldDisplay;
        }
    }

    void UpdateGoldDisplay(int currentGold)
    {
        if (goldAmountText != null)
        {
            goldAmountText.text = currentGold.ToString("N0");
        }
    }

    void BuildWindow()
    {
        // 1. Panel nen cua so (Inventory_1: 100 x 102 pixels)
        float winW = 100f * pixelScale;
        float winH = 102f * pixelScale;

        windowPanel = new GameObject("InventoryWindow", typeof(RectTransform), typeof(Image));
        windowPanel.transform.SetParent(transform, false);

        var winRT = windowPanel.GetComponent<RectTransform>();
        winRT.anchorMin = new Vector2(0.5f, 0.5f);
        winRT.anchorMax = new Vector2(0.5f, 0.5f);
        winRT.pivot     = new Vector2(0.5f, 0.5f);
        winRT.sizeDelta = new Vector2(winW, winH);
        winRT.anchoredPosition = new Vector2(0f, 60f); // Nang nhe bang kho len tren de khong de len Hotbar co san ben duoi

        var winImg = windowPanel.GetComponent<Image>();
        winImg.sprite = windowSprite;
        winImg.type = Image.Type.Simple;

        // Nut X dong kho/tui do (o goc tren ben phai cua so kho)
        var closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(windowPanel.transform, false);
        var closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 1f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.pivot = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = new Vector2(-4f * pixelScale, -4f * pixelScale);
        closeBtnRT.sizeDelta = new Vector2(12f * pixelScale, 12f * pixelScale);

        var closeBtnImg = closeBtnGO.GetComponent<Image>();
        closeBtnImg.color = new Color(1f, 1f, 1f, 0f); // Trong suot vi tren sprite cua so da co san hinh nut X
        var closeBtn = closeBtnGO.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => Close());

        // 2. Tao 20 slot (5 cot x 4 hang) khop voi pixel cua Inventory_1
        // Col X trong sprite: 11, 27, 43, 59, 75 (w = 14)
        // Row Y trong sprite: 17, 33, 49, 65 (h = 14)
        int[] colXs = { 11, 27, 43, 59, 75 };
        int[] rowYs = { 17, 33, 49, 65 };
        float slotDim = 14f * pixelScale;

        gridSlots = new SlotUI[InventorySystem.GridSize];

        for (int row = 0; row < InventorySystem.GridRows; row++)
        {
            for (int col = 0; col < InventorySystem.GridCols; col++)
            {
                int indexInGrid = row * InventorySystem.GridCols + col;
                int globalIndex = InventorySystem.HotbarSize + indexInGrid; // 10..29

                var slotGO = new GameObject($"GSlot_{indexInGrid}", typeof(RectTransform), typeof(Image), typeof(SlotUI));
                slotGO.transform.SetParent(windowPanel.transform, false);

                var slotRT = slotGO.GetComponent<RectTransform>();
                slotRT.anchorMin = new Vector2(0f, 1f);
                slotRT.anchorMax = new Vector2(0f, 1f);
                slotRT.pivot     = new Vector2(0f, 1f);
                slotRT.sizeDelta = new Vector2(slotDim, slotDim);
                slotRT.anchoredPosition = new Vector2(colXs[col] * pixelScale, -rowYs[row] * pixelScale);

                // Image trong suot bat buoc phai co de nhan su kien chuot (Raycast Target)
                var slotBgImg = slotGO.GetComponent<Image>();
                slotBgImg.color = Color.clear;
                slotBgImg.raycastTarget = true;
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(slotGO.transform, false);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = new Vector2(4, 4);
                iconRT.offsetMax = new Vector2(-4, -4);
                var iconImg = iconGO.GetComponent<Image>();
                iconImg.raycastTarget = false;
                iconImg.preserveAspect = true;
                iconImg.enabled = false;

                // So luong
                var amountGO = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
                amountGO.transform.SetParent(slotGO.transform, false);
                var amountRT = amountGO.GetComponent<RectTransform>();
                amountRT.anchorMin = Vector2.zero;
                amountRT.anchorMax = Vector2.one;
                amountRT.offsetMin = new Vector2(2, 2);
                amountRT.offsetMax = new Vector2(-2, -2);
                var amountTmp = amountGO.GetComponent<TextMeshProUGUI>();
                amountTmp.fontSize = 13;
                amountTmp.fontStyle = FontStyles.Bold;
                amountTmp.color = Color.white;
                amountTmp.alignment = TextAlignmentOptions.BottomRight;
                amountTmp.raycastTarget = false;
                amountTmp.enabled = false;

                // Highlight overlay khi hover
                var hlOverlayGO = new GameObject("HoverOverlay", typeof(RectTransform), typeof(Image));
                hlOverlayGO.transform.SetParent(slotGO.transform, false);
                var hlOverlayRT = hlOverlayGO.GetComponent<RectTransform>();
                hlOverlayRT.anchorMin = Vector2.zero;
                hlOverlayRT.anchorMax = Vector2.one;
                hlOverlayRT.sizeDelta = Vector2.zero;
                var hlOverlayImg = hlOverlayGO.GetComponent<Image>();
                hlOverlayImg.color = new Color(1f, 1f, 1f, 0.2f);
                hlOverlayImg.raycastTarget = false;
                hlOverlayImg.enabled = false;

                // SlotUI
                var slotUI = slotGO.GetComponent<SlotUI>();
                slotUI.iconImage = iconImg;
                slotUI.amountText = amountTmp;
                slotUI.highlightOverlay = hlOverlayImg;
                slotUI.Init(globalIndex, false);

                gridSlots[indexInGrid] = slotUI;
            }
        }

        // 4. Widget hien thi Tien Vang o goc tren ben trai cua so kho
        var goldBadgeGO = new GameObject("GoldBadge", typeof(RectTransform));
        goldBadgeGO.transform.SetParent(windowPanel.transform, false);
        var goldBadgeRT = goldBadgeGO.GetComponent<RectTransform>();
        goldBadgeRT.anchorMin = new Vector2(0f, 1f);
        goldBadgeRT.anchorMax = new Vector2(0f, 1f);
        goldBadgeRT.pivot     = new Vector2(0f, 0.5f);
        // Can chinh ngang tam dong xu: y = -87.5f * pixelScale
        goldBadgeRT.anchoredPosition = new Vector2(27f * pixelScale, -87.5f * pixelScale);
        goldBadgeRT.sizeDelta        = new Vector2(35f * pixelScale, 14f * pixelScale);

        // Text so vang
        var goldTextGO = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
        goldTextGO.transform.SetParent(goldBadgeGO.transform, false);
        var goldTextRT = goldTextGO.GetComponent<RectTransform>();
        goldTextRT.anchorMin = Vector2.zero;
        goldTextRT.anchorMax = Vector2.one;
        goldTextRT.offsetMin = Vector2.zero;
        goldTextRT.offsetMax = Vector2.zero;

        goldAmountText = goldTextGO.GetComponent<TextMeshProUGUI>();
        goldAmountText.fontSize = 20;
        goldAmountText.fontStyle = FontStyles.Bold;
        goldAmountText.color = new Color(0.25f, 0.18f, 0.1f);
        goldAmountText.alignment = TextAlignmentOptions.MidlineLeft;
        goldAmountText.raycastTarget = false;
        goldAmountText.text = "0";

        // Che vien ngoc xanh bang mau nen chuan RGB (229, 214, 161)
        var coverGO = new GameObject("GemCover", typeof(RectTransform), typeof(Image));
        coverGO.transform.SetParent(windowPanel.transform, false);
        var coverRT = coverGO.GetComponent<RectTransform>();
        coverRT.anchorMin = new Vector2(0f, 1f);
        coverRT.anchorMax = new Vector2(0f, 1f);
        coverRT.pivot     = new Vector2(0.5f, 0.5f);
        coverRT.anchoredPosition = new Vector2(67.5f * pixelScale, -88f * pixelScale);
        coverRT.sizeDelta        = new Vector2(11f * pixelScale, 12f * pixelScale);
        var coverImg = coverGO.GetComponent<Image>();
        coverImg.color = new Color(229f / 255f, 214f / 255f, 161f / 255f, 1f);
        coverImg.raycastTarget = false;
    }

    void BuildHeldItemCursor()
    {
        var heldGO = new GameObject("HeldItemCursor", typeof(RectTransform), typeof(Image));
        heldGO.transform.SetParent(transform, false);
        heldItemRT = heldGO.GetComponent<RectTransform>();
        heldItemRT.sizeDelta = new Vector2(50f, 50f);
        heldItemRT.pivot     = new Vector2(0.5f, 0.5f);

        heldItemIcon = heldGO.GetComponent<Image>();
        heldItemIcon.raycastTarget = false;
        heldItemIcon.preserveAspect = true;
        heldItemIcon.enabled = false;

        var amtGO = new GameObject("HeldAmount", typeof(RectTransform), typeof(TextMeshProUGUI));
        amtGO.transform.SetParent(heldGO.transform, false);
        var amtRT = amtGO.GetComponent<RectTransform>();
        amtRT.anchorMin = Vector2.zero;
        amtRT.anchorMax = Vector2.one;
        amtRT.offsetMin = new Vector2(2, 2);
        amtRT.offsetMax = new Vector2(-2, -2);
        heldItemAmountText = amtGO.GetComponent<TextMeshProUGUI>();
        heldItemAmountText.fontSize = 14;
        heldItemAmountText.fontStyle = FontStyles.Bold;
        heldItemAmountText.alignment = TextAlignmentOptions.BottomRight;
        heldItemAmountText.color = Color.white;
        heldItemAmountText.raycastTarget = false;
        heldItemAmountText.enabled = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            bool inventoryKey = kb.eKey.wasPressedThisFrame && !CowFarm.CowFarmDairy.HandlesInteractKey;
            if (isOpen)
            {
                // Khi dang mo kho: Bam E, Esc, hoac X deu thoat/dong kho
                if (inventoryKey || kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
                {
                    Close();
                }
            }
            else
            {
                // Khi dang dong kho: Bam E hoac Tab de mo
                if (inventoryKey || kb.tabKey.wasPressedThisFrame)
                {
                    Open();
                }
            }
        }

        // Cap nhat vi tri item dang cam theo chuot
        var mouse = Mouse.current;
        Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : Vector2.zero;

        if (IsHoldingItem && heldItemRT != null)
        {
            UpdateHeldItemPosition(mousePos);
        }

        // Cap nhat vi tri tooltip noi theo con tro chuot
        if (tooltipGO != null && tooltipGO.activeSelf && tooltipRT != null)
        {
            UpdateTooltipPosition(mousePos);
        }
    }

    void UpdateTooltipPosition(Vector2 screenPos)
    {
        // Cach con tro chuot 15px de khong bi che
        Vector2 targetPos = screenPos + new Vector2(15f, 15f);

        // Giu cho tooltip khong bi tran ra khoi man hinh
        float maxX = Screen.width - tooltipRT.sizeDelta.x - 10f;
        float maxY = Screen.height - tooltipRT.sizeDelta.y - 10f;
        targetPos.x = Mathf.Clamp(targetPos.x, 10f, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, 10f, maxY);

        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            tooltipRT.position = targetPos;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tooltipRT.parent as RectTransform,
                targetPos,
                rootCanvas.worldCamera,
                out Vector2 localPoint);
            tooltipRT.localPosition = localPoint;
        }
    }

    public void Open()
    {
        isOpen = true;
        if (windowPanel != null) windowPanel.SetActive(true);
        Refresh();
        HideTooltip();
    }

    public void Close()
    {
        isOpen = false;
        if (windowPanel != null) windowPanel.SetActive(false);
        CancelHeld();
        HideTooltip();
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Refresh()
    {
        if (gridSlots == null || InventorySystem.Instance == null) return;

        for (int i = 0; i < InventorySystem.GridSize; i++)
        {
            var slotData = InventorySystem.Instance.GetGridSlot(i);
            gridSlots[i].SetData(slotData);
        }

        if (HotbarUI.Instance != null)
            HotbarUI.Instance.Refresh();

        // Cap nhat so vang hien tai trong vi
        if (goldAmountText != null && PlayerWallet.Instance != null)
        {
            goldAmountText.text = PlayerWallet.Instance.gold.ToString("N0");
        }
    }

    // --- Drag & Drop Mechanics ---
    public void StartDragFromSlot(int slotIndex, Vector2 screenPos)
    {
        var slot = InventorySystem.Instance.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return;

        sourceSlotIndex = slotIndex;
        heldSlot.item = slot.item;
        heldSlot.amount = slot.amount;
        slot.Clear(); // Tam thoi xoa khoi slot de tao hieu ung keo

        isDragging = true;
        ShowHeldVisual();
        UpdateHeldItemPosition(screenPos);
        InventorySystem.Instance.NotifyChanged();
    }

    public void UpdateDragPosition(Vector2 screenPos)
    {
        if (isDragging)
            UpdateHeldItemPosition(screenPos);
    }

    public void EndDrag(int targetSlotIndex)
    {
        if (!isDragging) return;
        isDragging = false;

        if (targetSlotIndex >= 0 && targetSlotIndex < InventorySystem.TotalSlots)
        {
            // Dat vao slot dich
            PlaceHeldItem(targetSlotIndex);
        }
        else
        {
            // Tha ra ngoai -> tra ve slot cu
            CancelHeld();
        }
    }

    // --- Click-to-Move Mechanics ---
    public void PickUpFull(int slotIndex)
    {
        var slot = InventorySystem.Instance.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return;

        sourceSlotIndex = slotIndex;
        heldSlot.item = slot.item;
        heldSlot.amount = slot.amount;
        slot.Clear();

        ShowHeldVisual();
        InventorySystem.Instance.NotifyChanged();
    }

    public void PickUpHalf(int slotIndex)
    {
        var slot = InventorySystem.Instance.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return;

        int half = Mathf.CeilToInt(slot.amount / 2f);
        sourceSlotIndex = slotIndex;
        heldSlot.item = slot.item;
        heldSlot.amount = half;

        slot.amount -= half;
        if (slot.amount <= 0) slot.Clear();

        ShowHeldVisual();
        InventorySystem.Instance.NotifyChanged();
    }

    public void DropOneToSlot(int targetIndex)
    {
        if (!IsHoldingItem) return;
        var target = InventorySystem.Instance.GetSlot(targetIndex);
        if (target == null) return;

        if (target.IsEmpty)
        {
            target.item = heldSlot.item;
            target.amount = 1;
            heldSlot.amount -= 1;
        }
        else if (target.item == heldSlot.item && target.item.IsStackable && target.amount < target.item.maxStack)
        {
            target.amount += 1;
            heldSlot.amount -= 1;
        }

        if (heldSlot.amount <= 0)
        {
            heldSlot.Clear();
            HideHeldVisual();
        }
        else
        {
            ShowHeldVisual();
        }

        InventorySystem.Instance.NotifyChanged();
    }

    public void PlaceHeldItem(int targetIndex)
    {
        if (!IsHoldingItem) return;
        var target = InventorySystem.Instance.GetSlot(targetIndex);
        if (target == null) return;

        // 1. Slot dich trong
        if (target.IsEmpty)
        {
            target.item = heldSlot.item;
            target.amount = heldSlot.amount;
            heldSlot.Clear();
            HideHeldVisual();
        }
        // 2. Cung loai item va stack duoc
        else if (target.item == heldSlot.item && target.item.IsStackable)
        {
            int space = target.item.maxStack - target.amount;
            int transfer = Mathf.Min(space, heldSlot.amount);
            target.amount += transfer;
            heldSlot.amount -= transfer;

            if (heldSlot.amount <= 0)
            {
                heldSlot.Clear();
                HideHeldVisual();
            }
            else
            {
                ShowHeldVisual();
            }
        }
        // 3. Khac loai -> Hoan doi
        else
        {
            var tempItem = target.item;
            var tempAmt = target.amount;

            target.item = heldSlot.item;
            target.amount = heldSlot.amount;

            heldSlot.item = tempItem;
            heldSlot.amount = tempAmt;
            ShowHeldVisual();
        }

        InventorySystem.Instance.NotifyChanged();
    }

    void CancelHeld()
    {
        if (!IsHoldingItem) return;

        if (sourceSlotIndex >= 0 && sourceSlotIndex < InventorySystem.TotalSlots)
        {
            var src = InventorySystem.Instance.GetSlot(sourceSlotIndex);
            if (src.IsEmpty)
            {
                src.item = heldSlot.item;
                src.amount = heldSlot.amount;
            }
            else
            {
                // Neu slot cu bi chiem roi thi nhet vao slot trong bat ky
                InventorySystem.Instance.AddItem(heldSlot.item, heldSlot.amount);
            }
        }
        else
        {
            InventorySystem.Instance.AddItem(heldSlot.item, heldSlot.amount);
        }

        heldSlot.Clear();
        sourceSlotIndex = -1;
        isDragging = false;
        HideHeldVisual();
        InventorySystem.Instance.NotifyChanged();
    }

    void ShowHeldVisual()
    {
        if (heldItemIcon != null && heldSlot.item != null)
        {
            heldItemIcon.sprite = heldSlot.item.icon;
            heldItemIcon.color = heldSlot.item.iconColor;
            heldItemIcon.enabled = true;

            bool showAmt = heldSlot.item.IsStackable && heldSlot.amount > 1;
            heldItemAmountText.text = showAmt ? heldSlot.amount.ToString() : "";
            heldItemAmountText.enabled = showAmt;
        }
    }

    void HideHeldVisual()
    {
        if (heldItemIcon != null)
            heldItemIcon.enabled = false;
        if (heldItemAmountText != null)
            heldItemAmountText.enabled = false;
    }

    void UpdateHeldItemPosition(Vector2 screenPos)
    {
        if (rootCanvas == null || heldItemRT == null) return;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            heldItemRT.position = screenPos;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                screenPos,
                rootCanvas.worldCamera,
                out Vector2 localPoint);
            heldItemRT.localPosition = localPoint;
        }
    }

    // --- Tooltip / Info Floating Box ---
    public void ShowTooltip(ItemData item)
    {
        if (item == null)
        {
            HideTooltip();
            return;
        }

        if (tooltipTitleText != null)
        {
            tooltipTitleText.text = item.itemName;
            tooltipTitleText.color = item.itemType switch
            {
                ItemType.Tool => new Color(0.35f, 0.65f, 1f),
                ItemType.Seed => new Color(0.4f, 0.95f, 0.4f),
                ItemType.Crop => new Color(1f, 0.75f, 0.3f),
                ItemType.FishFry => new Color(0.3f, 0.85f, 1f),
                ItemType.Fish => new Color(0.4f, 0.75f, 1f),
                ItemType.FishFood => new Color(1f, 0.85f, 0.35f),
                _ => new Color(1f, 0.9f, 0.5f)
            };
        }

        if (tooltipTypeText != null)
        {
            string typeStr = item.itemType switch
            {
                ItemType.Tool => $"[Công cụ: {item.toolType}]",
                ItemType.Seed => $"[Hạt giống] (Chín sau {item.growthDays} ngày)",
                ItemType.Crop => $"[Nông sản] (Giá bán: {item.sellPrice} vàng)",
                ItemType.FishFry => $"[Cá giống] (Thả xuống ao nuôi)",
                ItemType.Fish => $"[Thủy sản] (Giá bán: {item.sellPrice} vàng)",
                ItemType.FishFood => $"[Thức ăn cá] (Cho cá ăn mau lớn)",
                _ => "[Vật phẩm]"
            };
            tooltipTypeText.text = typeStr;
        }

        if (tooltipDescText != null)
        {
            string desc = item.description;
            if (item.itemType == ItemType.Seed && item.cropYield != null)
            {
                desc += $" (Thu hoạch: {item.cropYield.itemName})";
            }
            tooltipDescText.text = desc;
        }

        if (tooltipGO != null)
        {
            tooltipGO.SetActive(true);
            var mouse = Mouse.current;
            if (mouse != null)
            {
                UpdateTooltipPosition(mouse.position.ReadValue());
            }
        }
    }

    public void HideTooltip()
    {
        if (tooltipGO != null)
        {
            tooltipGO.SetActive(false);
        }
    }
}
