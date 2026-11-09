using UnityEngine;

// Gan len Main Camera:
// - Chan nguoi choi khong chay ra ngoai vung CameraBoundsArea (ban do).
// - Giu camera khong lo ra ngoai chinh vung do (vung lon nhat chua nguoi choi).
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Camera))]
public class CameraBounds : MonoBehaviour
{
    [SerializeField, Min(0f)] float playerMargin = 0.4f;

    Camera cam;
    PlayerMove player;
    Rigidbody2D playerBody;
    Rect lastPlayerRect;
    bool hasLastPlayerRect;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!cam) return;
        if (!player)
        {
            player = FindFirstObjectByType<PlayerMove>();
            if (!player) return;
            playerBody = player.GetComponent<Rigidbody2D>();
            hasLastPlayerRect = false;
        }

        Rect limit;
        if (!KeepPlayerInside(out limit)) return;
        KeepCameraInside(limit);
    }

    // Tra ve vung gioi han dang ap dung (vung lon nhat chua nguoi choi, hoac vung gan nhat ho vua o)
    bool KeepPlayerInside(out Rect limit)
    {
        Vector2 position = player.transform.position;

        // Gioi han nguoi choi = vung lon nhat dang chua nguoi choi (de khong bi nhot trong vung con)
        bool found = false;
        limit = new Rect();
        float largest = -1f;
        foreach (var area in CameraBoundsArea.All)
        {
            Rect rect = area.GetRect();
            if (!rect.Contains(position)) continue;
            float size = rect.width * rect.height;
            if (size <= largest) continue;
            limit = rect;
            largest = size;
            found = true;
        }

        if (found)
        {
            lastPlayerRect = limit;
            hasLastPlayerRect = true;
        }
        else if (hasLastPlayerRect)
        {
            limit = lastPlayerRect;
        }
        else
        {
            return false;
        }

        float minX = limit.xMin + playerMargin;
        float maxX = limit.xMax - playerMargin;
        float minY = limit.yMin + playerMargin;
        float maxY = limit.yMax - playerMargin;
        if (minX > maxX) minX = maxX = limit.center.x;
        if (minY > maxY) minY = maxY = limit.center.y;

        Vector2 clamped = new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
        if (clamped == position) return true;

        player.transform.position = new Vector3(clamped.x, clamped.y, player.transform.position.z);
        if (playerBody)
        {
            playerBody.position = clamped;
            Vector2 velocity = playerBody.linearVelocity;
            if (!Mathf.Approximately(clamped.x, position.x)) velocity.x = 0f;
            if (!Mathf.Approximately(clamped.y, position.y)) velocity.y = 0f;
            playerBody.linearVelocity = velocity;
        }
        return true;
    }

    void KeepCameraInside(Rect best)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 view = transform.position;
        view.x = best.width <= halfWidth * 2f ? best.center.x : Mathf.Clamp(view.x, best.xMin + halfWidth, best.xMax - halfWidth);
        view.y = best.height <= halfHeight * 2f ? best.center.y : Mathf.Clamp(view.y, best.yMin + halfHeight, best.yMax - halfHeight);
        transform.position = view;
    }
}
