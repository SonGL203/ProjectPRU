using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance { get; private set; }

    [Header("Sprites")]
    [Tooltip("Sprite thanh Hotbar (Action_panel_1 trong Action_panel.png)")]
    public Sprite barSprite;

    [Tooltip("Sprite khung chon slot dang active (Action_panel_8 trong Action_panel.png)")]
    public Sprite highlightSprite;

    [Header("Scale & Vi tri")]
    [Tooltip("Ti le pixel art dong bo voi Inventory (Mac dinh 2.5f)")]
    public float pixelScale = 2.5f;

    [Tooltip("Khoang cach tu day man hinh len")]
    public float bottomOffset = 10f;

    // Runtime
    GameObject barPanel;
    SlotUI[] slots;
    RectTransform highlightRT;
    Image highlightImg;

    void Awake()
    {
        Instance = this;

        // Stretch RectTransform cha de anchor bottom-center hoat dong dung
        var myRT = GetComponent<RectTransform>();
        if (myRT != null)
        {
            myRT.anchorMin = Vector2.zero;
            myRT.anchorMax = Vector2.one;
            myRT.offsetMin = Vector2.zero;
            myRT.offsetMax = Vector2.zero;
        }

#if UNITY_EDITOR
        if (barSprite == null)
        {
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Action_panel.png");
            foreach (var s in sprites)
            {
                if (s is Sprite sp && sp.name == "Action_panel_1") { barSprite = sp; break; }
            }
        }
        if (highlightSprite == null)
        {
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/sprites/Inventory/Action_panel.png");
            foreach (var s in sprites)
            {
                if (s is Sprite sp && sp.name == "Action_panel_8") { highlightSprite = sp; break; }
            }
        }
#endif

        Build();
    }

    void Build()
    {
        // Xoa cac thanh phan cu neu co
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Action_panel_1: 170 x 22 pixels
        float barW = 170f * pixelScale;
        float barH = 22f  * pixelScale;

        barPanel = new GameObject("HotbarBar", typeof(RectTransform), typeof(Image));
        barPanel.transform.SetParent(transform, false);

        var barRT = barPanel.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.5f, 0f);
        barRT.anchorMax = new Vector2(0.5f, 0f);
        barRT.pivot     = new Vector2(0.5f, 0f);
        barRT.sizeDelta = new Vector2(barW, barH);
        barRT.anchoredPosition = new Vector2(0, bottomOffset);

        var barImg = barPanel.GetComponent<Image>();
        barImg.sprite = barSprite;
        barImg.type = Image.Type.Simple;

        // Tao cac o slot tren thanh bar
        // Trong Action_panel_1 (170x22):
        // 10 o nam o cac toa do X: 6, 22, 38, 54, 70, 86, 102, 118, 134, 150 (pitch 16px, w=14, h=14)
        // Y tu dinh xuong: 4px
        int count = InventorySystem.HotbarSize;
        slots = new SlotUI[count];
        float slotDim = 14f * pixelScale;

        for (int i = 0; i < count; i++)
        {
            float slotX = (6f + i * 16f) * pixelScale;
            float slotY = -4f * pixelScale;

            var slotGO = new GameObject($"HSlot_{i + 1}", typeof(RectTransform), typeof(SlotUI));
            slotGO.transform.SetParent(barPanel.transform, false);

            var slotRT = slotGO.GetComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0f, 1f);
            slotRT.anchorMax = new Vector2(0f, 1f);
            slotRT.pivot     = new Vector2(0f, 1f);
            slotRT.sizeDelta = new Vector2(slotDim, slotDim);
            slotRT.anchoredPosition = new Vector2(slotX, slotY);

            // Icon vat pham
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(2, 2);
            iconRT.offsetMax = new Vector2(-2, -2);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconImg.enabled = false;

            // So luong (Bottom-Right)
            var amountGO = new GameObject("Amount", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            amountGO.transform.SetParent(slotGO.transform, false);
            var amountRT = amountGO.GetComponent<RectTransform>();
            amountRT.anchorMin = Vector2.zero;
            amountRT.anchorMax = Vector2.one;
            amountRT.offsetMin = new Vector2(1, 1);
            amountRT.offsetMax = new Vector2(-2, -1);
            var amountTmp = amountGO.GetComponent<TMPro.TextMeshProUGUI>();
            amountTmp.fontSize = 13;
            amountTmp.fontStyle = TMPro.FontStyles.Bold;
            amountTmp.color = Color.white;
            amountTmp.alignment = TMPro.TextAlignmentOptions.BottomRight;
            amountTmp.raycastTarget = false;
            amountTmp.enabled = false;

            // Overlay highlight khi hover
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

            var slotUI = slotGO.GetComponent<SlotUI>();
            slotUI.iconImage = iconImg;
            slotUI.amountText = amountTmp;
            slotUI.highlightOverlay = hlOverlayImg;
            slotUI.Init(i, true);

            slots[i] = slotUI;
        }

        // Khung selector active (Action_panel_8: 18 x 17 pixels)
        var hlGO = new GameObject("SelectorHighlight", typeof(RectTransform), typeof(Image));
        hlGO.transform.SetParent(barPanel.transform, false);
        highlightRT = hlGO.GetComponent<RectTransform>();
        highlightRT.anchorMin = new Vector2(0f, 1f);
        highlightRT.anchorMax = new Vector2(0f, 1f);
        highlightRT.pivot     = new Vector2(0f, 1f);
        highlightRT.sizeDelta = new Vector2(18f * pixelScale, 17f * pixelScale);

        highlightImg = hlGO.GetComponent<Image>();
        highlightImg.sprite = highlightSprite;
        highlightImg.type = Image.Type.Simple;
        highlightImg.raycastTarget = false;
    }

    void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged += Refresh;
            InventorySystem.Instance.OnHotbarSelected += OnSlotSelected;
            Refresh();
            OnSlotSelected(InventorySystem.Instance.selectedHotbar);
        }
    }

    void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged -= Refresh;
            InventorySystem.Instance.OnHotbarSelected -= OnSlotSelected;
        }
    }

    void Start()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnChanged += Refresh;
            InventorySystem.Instance.OnHotbarSelected += OnSlotSelected;
            Refresh();
            OnSlotSelected(InventorySystem.Instance.selectedHotbar);
        }
    }

    void Update()
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        var kb = Keyboard.current;
        if (kb != null)
        {
            // Phim 1-9
            Key[] keys = {
                Key.Digit1, Key.Digit2, Key.Digit3,
                Key.Digit4, Key.Digit5, Key.Digit6,
                Key.Digit7, Key.Digit8, Key.Digit9,
                Key.Digit0
            };
            for (int i = 0; i < keys.Length && i < InventorySystem.HotbarSize; i++)
            {
                if (kb[keys[i]].wasPressedThisFrame)
                {
                    inv.SelectHotbar(i);
                    break;
                }
            }

            // Numpad 1-9 va 0
            Key[] numpad = {
                Key.Numpad1, Key.Numpad2, Key.Numpad3,
                Key.Numpad4, Key.Numpad5, Key.Numpad6,
                Key.Numpad7, Key.Numpad8, Key.Numpad9,
                Key.Numpad0
            };
            for (int i = 0; i < numpad.Length && i < InventorySystem.HotbarSize; i++)
            {
                if (kb[numpad[i]].wasPressedThisFrame)
                {
                    inv.SelectHotbar(i);
                    break;
                }
            }
        }

        // Cuon chuot chuyen slot
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f)
                inv.SelectHotbar(inv.selectedHotbar - 1);
            else if (scroll < 0f)
                inv.SelectHotbar(inv.selectedHotbar + 1);
        }
    }

    public void Refresh()
    {
        if (slots == null || InventorySystem.Instance == null) return;
        for (int i = 0; i < InventorySystem.HotbarSize; i++)
        {
            var slotData = InventorySystem.Instance.GetHotbarSlot(i);
            slots[i].SetData(slotData);
        }
    }

    void OnSlotSelected(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        if (highlightRT != null)
        {
            // Toa do frame Action_panel_8 can lech -2px va +1px so voi slot 14x14
            float posX = (6f + index * 16f - 2f) * pixelScale;
            float posY = -(4f - 1f) * pixelScale;
            highlightRT.anchoredPosition = new Vector2(posX, posY);
        }
    }
}
