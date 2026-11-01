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

    [Header("Scale & Kich thuoc")]
    [Tooltip("Ti le phong to cua sprite pixel art (100x102 * scale)")]
    public float pixelScale = 2.5f;

    [Header("Trang thai")]
    public bool isOpen = false;

    // Runtime UI
    GameObject windowPanel;
    SlotUI[] gridSlots;

    // Tooltip Info Box (vung duoi cung cua cua so)
    TextMeshProUGUI infoTitleText;
    TextMeshProUGUI infoTypeText;
    TextMeshProUGUI infoDescText;

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

        // Stretch RectTransform cha de anchor center hoat dong dung
        var myRT = GetComponent<RectTransform>();
        if (myRT != null)
        {
            myRT.anchorMin = Vector2.zero;
            myRT.anchorMax = Vector2.one;
            myRT.offsetMin = Vector2.zero;
            myRT.offsetMax = Vector2.zero;
        }

#if UNITY_EDITOR
        if (windowSprite == null)
        {
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Inventory.png");
            foreach (var s in sprites)
            {
                if (s is Sprite sp && sp.name == "Inventory_1") { windowSprite = sp; break; }
            }
        }
#endif

        BuildWindow();
        BuildHeldItemCursor();
    }

    void Start()
    {
        if (windowPanel != null)
            windowPanel.SetActive(false);

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged += Refresh;
        }
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged -= Refresh;
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
        winRT.anchoredPosition = new Vector2(0, 40f); // hoi lech len tren mot chut so voi tam

        var winImg = windowPanel.GetComponent<Image>();
        winImg.sprite = windowSprite;
        winImg.type = Image.Type.Simple;

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

                var slotGO = new GameObject($"GSlot_{indexInGrid}", typeof(RectTransform), typeof(SlotUI));
                slotGO.transform.SetParent(windowPanel.transform, false);

                var slotRT = slotGO.GetComponent<RectTransform>();
                slotRT.anchorMin = new Vector2(0f, 1f);
                slotRT.anchorMax = new Vector2(0f, 1f);
                slotRT.pivot     = new Vector2(0f, 1f);
                slotRT.sizeDelta = new Vector2(slotDim, slotDim);
                slotRT.anchoredPosition = new Vector2(colXs[col] * pixelScale, -rowYs[row] * pixelScale);

                // Slot khong can Image nen vi sprite Inventory_1 da ve san o luoi!
                // Chi them Icon
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

        // 3. Info / Tooltip Box o nua duoi cua so (y: 80 to 98)
        var infoBoxGO = new GameObject("InfoBox", typeof(RectTransform));
        infoBoxGO.transform.SetParent(windowPanel.transform, false);
        var infoRT = infoBoxGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0f, 1f);
        infoRT.anchorMax = new Vector2(0f, 1f);
        infoRT.pivot     = new Vector2(0f, 1f);
        infoRT.anchoredPosition = new Vector2(10f * pixelScale, -81f * pixelScale);
        infoRT.sizeDelta        = new Vector2(80f * pixelScale, 17f * pixelScale);

        // Ten vat pham
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(infoBoxGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot     = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(0, 0);
        titleRT.sizeDelta = new Vector2(0, 16);
        infoTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        infoTitleText.fontSize = 12;
        infoTitleText.fontStyle = FontStyles.Bold;
        infoTitleText.color = new Color(0.25f, 0.15f, 0.1f);
        infoTitleText.raycastTarget = false;
        infoTitleText.text = "";

        // Loai vat pham
        var typeGO = new GameObject("Type", typeof(RectTransform), typeof(TextMeshProUGUI));
        typeGO.transform.SetParent(infoBoxGO.transform, false);
        var typeRT = typeGO.GetComponent<RectTransform>();
        typeRT.anchorMin = new Vector2(0f, 1f);
        typeRT.anchorMax = new Vector2(1f, 1f);
        typeRT.pivot     = new Vector2(0f, 1f);
        typeRT.anchoredPosition = new Vector2(0, -15);
        typeRT.sizeDelta = new Vector2(0, 14);
        infoTypeText = typeGO.GetComponent<TextMeshProUGUI>();
        infoTypeText.fontSize = 10;
        infoTypeText.color = new Color(0.4f, 0.25f, 0.15f);
        infoTypeText.raycastTarget = false;
        infoTypeText.text = "";

        // Mo ta vat pham
        var descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(infoBoxGO.transform, false);
        var descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 1f);
        descRT.anchorMax = new Vector2(1f, 1f);
        descRT.pivot     = new Vector2(0f, 1f);
        descRT.anchoredPosition = new Vector2(0, -29);
        descRT.sizeDelta = new Vector2(0, 32);
        infoDescText = descGO.GetComponent<TextMeshProUGUI>();
        infoDescText.fontSize = 10;
        infoDescText.color = new Color(0.35f, 0.2f, 0.15f);
        infoDescText.raycastTarget = false;
        infoDescText.overflowMode = TextOverflowModes.Ellipsis;
        infoDescText.text = "";
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
            if (kb.eKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        // Cap nhat vi tri item dang cam theo chuot
        if (IsHoldingItem && heldItemRT != null)
        {
            var mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
            UpdateHeldItemPosition(mousePos);
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        windowPanel.SetActive(isOpen);

        if (isOpen)
        {
            Refresh();
            HideTooltip();
        }
        else
        {
            // Neu dong tui ma dang cam vat pham tren chuot -> tra lai slot cu
            CancelHeld();
            HideTooltip();
        }
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

    // --- Tooltip / Info Box ---
    public void ShowTooltip(ItemData item)
    {
        if (item == null)
        {
            HideTooltip();
            return;
        }

        if (infoTitleText != null)
        {
            infoTitleText.text = item.itemName;
            // Mau theo loai vat pham
            infoTitleText.color = item.itemType switch
            {
                ItemType.Tool => new Color(0.1f, 0.35f, 0.75f),
                ItemType.Seed => new Color(0.15f, 0.55f, 0.15f),
                ItemType.Crop => new Color(0.75f, 0.35f, 0.05f),
                _ => new Color(0.25f, 0.15f, 0.1f)
            };
        }

        if (infoTypeText != null)
        {
            string typeStr = item.itemType switch
            {
                ItemType.Tool => $"[Cong cu: {item.toolType}]",
                ItemType.Seed => $"[Hat giong]  (Lon len sau {item.growthDays} ngay)",
                ItemType.Crop => $"[Nong san]  (Gia ban: {item.sellPrice} vang)",
                _ => "[Vat pham]"
            };
            infoTypeText.text = typeStr;
        }

        if (infoDescText != null)
        {
            string desc = item.description;
            if (item.itemType == ItemType.Seed && item.cropYield != null)
            {
                desc += $" (Thu hoach: {item.cropYield.itemName})";
            }
            infoDescText.text = desc;
        }
    }

    public void HideTooltip()
    {
        if (infoTitleText != null) infoTitleText.text = "";
        if (infoTypeText != null) infoTypeText.text = "";
        if (infoDescText != null) infoDescText.text = "";
    }
}
