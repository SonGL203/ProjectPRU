using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Quay hang trong Market: dung trong vung quay, nhan E de mo / dong bang mua ban.
[RequireComponent(typeof(SellZone))]
public class MarketStall : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Text feedback;
    [SerializeField] GameObject hint;
    [SerializeField] string defaultMessage = "Chọn giao dịch bên dưới";

    [SerializeField, Min(0.5f)] float interactRange = 3f;

    SellZone zone;
    PlayerMove player;
    MarketTradeRow[] rows;
    bool open;
    string message;
    float messageUntil;

    void Awake()
    {
        zone = GetComponent<SellZone>();
        rows = panel ? panel.GetComponentsInChildren<MarketTradeRow>(true) : new MarketTradeRow[0];
        if (panel) panel.SetActive(false);
        if (hint) hint.SetActive(false);
    }

    void Update()
    {
        if (!player) player = FindFirstObjectByType<PlayerMove>();
        bool inside = zone.PlayerInside;
        if (!inside && player)
        {
            Vector2 offset = player.transform.position - transform.position;
            inside = offset.sqrMagnitude <= interactRange * interactRange;
        }
        var keys = Keyboard.current;
        bool inventoryOpen = InventoryUI.Instance && InventoryUI.Instance.isOpen;

        if (!inside || inventoryOpen || (keys != null && keys.escapeKey.wasPressedThisFrame)) open = false;
        else if (keys != null && keys.fKey.wasPressedThisFrame) open = !open;

        if (panel && panel.activeSelf != open) panel.SetActive(open);
        if (hint)
        {
            bool showHint = inside && !open;
            if (hint.activeSelf != showHint) hint.SetActive(showHint);
        }
        if (!open) return;

        foreach (var row in rows) row.Refresh();
        if (feedback)
        {
            string text = Time.time < messageUntil ? message : defaultMessage;
            if (feedback.text != text) feedback.text = text;
        }
    }

    public void Say(string text)
    {
        message = text;
        messageUntil = Time.time + 2.5f;
    }
}
