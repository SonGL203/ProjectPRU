using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Thanh phan UI")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI keyNumberText; // Hien thi "1", "2"... tren hotbar
    public Image highlightOverlay;

    [Header("Thong tin slot")]
    public int slotIndex = -1;
    public bool isHotbarSlot = false;

    ItemSlot currentSlot;

    public void Init(int index, bool isHotbar, string keyLabel = null)
    {
        slotIndex = index;
        isHotbarSlot = isHotbar;

        if (keyNumberText != null)
        {
            if (!string.IsNullOrEmpty(keyLabel))
            {
                keyNumberText.text = keyLabel;
                keyNumberText.enabled = true;
            }
            else
            {
                keyNumberText.enabled = false;
            }
        }

        if (highlightOverlay != null)
            highlightOverlay.enabled = false;
    }

    public void SetData(ItemSlot slot)
    {
        currentSlot = slot;

        if (slot == null || slot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (amountText != null)
            {
                amountText.text = "";
                amountText.enabled = false;
            }
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = slot.item.icon;
                iconImage.enabled = slot.item.icon != null;
            }

            if (amountText != null)
            {
                bool showAmount = slot.item.IsStackable && slot.amount > 1;
                amountText.text = showAmount ? slot.amount.ToString() : "";
                amountText.enabled = showAmount;
            }
        }
    }

    // --- Hover Tooltip / Highlight ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightOverlay != null)
            highlightOverlay.enabled = true;

        if (InventoryUI.Instance != null && currentSlot != null && !currentSlot.IsEmpty)
        {
            InventoryUI.Instance.ShowTooltip(currentSlot.item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightOverlay != null)
            highlightOverlay.enabled = false;

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.HideTooltip();
        }
    }

    // --- Click: Quick Transfer / Click-to-Move ---
    public void OnPointerClick(PointerEventData eventData)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return;

        // Shift + Click -> Chuyen nhanh giua Hotbar va Grid
        bool isShift = UnityEngine.InputSystem.Keyboard.current != null &&
                       (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed ||
                        UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed);

        if (isShift)
        {
            inv.QuickTransfer(slotIndex);
            return;
        }

        // Right-Click: Tach nua stack hoac tha 1 item
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsHoldingItem)
            {
                // Tha 1 item vao slot nay
                InventoryUI.Instance.DropOneToSlot(slotIndex);
            }
            else if (currentSlot != null && !currentSlot.IsEmpty)
            {
                // Cam nua stack vao chuot
                if (InventoryUI.Instance != null)
                    InventoryUI.Instance.PickUpHalf(slotIndex);
            }
            return;
        }

        // Left-Click: Cam len hoac Dat xuong / Hoan doi
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (InventoryUI.Instance != null)
            {
                if (InventoryUI.Instance.IsHoldingItem)
                {
                    InventoryUI.Instance.PlaceHeldItem(slotIndex);
                }
                else if (currentSlot != null && !currentSlot.IsEmpty)
                {
                    InventoryUI.Instance.PickUpFull(slotIndex);
                }
            }
        }
    }

    // --- Drag & Drop ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.IsEmpty) return;
        if (InventoryUI.Instance != null && !InventoryUI.Instance.IsHoldingItem)
        {
            InventoryUI.Instance.StartDragFromSlot(slotIndex, eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.UpdateDragPosition(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUI.Instance != null)
        {
            // Tim slot ben duoi vi tri tha chuot
            GameObject targetObj = eventData.pointerCurrentRaycast.gameObject;
            SlotUI targetSlot = targetObj != null ? targetObj.GetComponentInParent<SlotUI>() : null;

            InventoryUI.Instance.EndDrag(targetSlot != null ? targetSlot.slotIndex : -1);
        }
    }
}
