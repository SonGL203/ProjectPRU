using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class HouseStorageTrigger : MonoBehaviour
{
    [Tooltip("Khoang cach toi da nguoi choi co the click vao nha de mo kho")]
    public float interactDistance = 5f;

    Transform player;
    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // Tranh click xuyen qua UI dang mo
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null) return;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());

        // Kiem tra chuot co click trung vao ngoi nha hay khong
        if (col != null && col.OverlapPoint(mouseWorld))
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) player = p.transform;
            }

            // Neu o gan nha -> mo kho
            if (player == null || Vector2.Distance(player.position, transform.position) <= interactDistance)
            {
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.Toggle();
                }
            }
            else
            {
                Debug.Log("[HouseStorageTrigger] Ban o qua xa ngoi nha de mo kho!");
            }
        }
    }
}

