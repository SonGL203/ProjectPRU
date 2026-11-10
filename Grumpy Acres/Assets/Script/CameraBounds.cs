using UnityEngine;

// Gan len Main Camera:
// - Chan nguoi choi khong chay ra ngoai vung CameraBoundsArea (ban do).
// - Camera bam theo nguoi choi moi frame (LateUpdate), bi gioi han trong vung nho nhat dang chua nguoi choi.
//   Neu vung nho hon man hinh thi camera can giua vung do.
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Camera))]
public class CameraBounds : MonoBehaviour
{
    [SerializeField, Min(0f)] float playerMargin = 0.4f;
    [Tooltip("Bat: camera khong lo ra ngoai ban do (sat mep thi nguoi choi lech khoi giua man hinh). Tat: camera luon o giua nguoi choi, sat mep se thay phan trong ben ngoai")]
    [SerializeField] bool keepInsideMap = true;

    Camera cam;
    PlayerMove player;
    Rigidbody2D playerBody;
    Rect lastPlayerRect;
    Rect lastCameraRect;
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
        Rect cameraArea;
        if (!KeepPlayerInside(out limit, out cameraArea)) return;

        FocusCamera(cameraArea, limit);
    }

    // limit: vung lon nhat chua nguoi choi, dung de chan nguoi choi (khong bi nhot trong vung con)
    // cameraArea: vung nho nhat chua nguoi choi, dung de gioi han camera (ban do hien tai)
    bool KeepPlayerInside(out Rect limit, out Rect cameraArea)
    {
        Vector2 position = player.transform.position;

        bool found = false;
        limit = new Rect();
        cameraArea = new Rect();
        float largest = -1f;
        float smallest = float.MaxValue;
        foreach (var area in CameraBoundsArea.All)
        {
            Rect rect = area.GetRect();
            if (!rect.Contains(position)) continue;
            float size = rect.width * rect.height;
            if (size > largest)
            {
                limit = rect;
                largest = size;
            }
            if (size < smallest)
            {
                cameraArea = rect;
                smallest = size;
            }
            found = true;
        }

        if (found)
        {
            lastPlayerRect = limit;
            lastCameraRect = cameraArea;
            hasLastPlayerRect = true;
        }
        else if (hasLastPlayerRect)
        {
            limit = lastPlayerRect;
            cameraArea = lastCameraRect;
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

    void FocusCamera(Rect area, Rect map)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 focus = player.transform.position;
        Vector3 view = transform.position;

        if (keepInsideMap)
        {
            view.x = area.width <= halfWidth * 2f ? ClampAxis(area.center.x, map.xMin + halfWidth, map.xMax - halfWidth, map.center.x) : Mathf.Clamp(focus.x, area.xMin + halfWidth, area.xMax - halfWidth);
            view.y = area.height <= halfHeight * 2f ? ClampAxis(area.center.y, map.yMin + halfHeight, map.yMax - halfHeight, map.center.y) : Mathf.Clamp(focus.y, area.yMin + halfHeight, area.yMax - halfHeight);
        }
        else
        {
            view.x = area.width <= halfWidth * 2f ? area.center.x : focus.x;
            view.y = area.height <= halfHeight * 2f ? area.center.y : focus.y;
        }
        transform.position = view;
    }

    // Vung nho hon man hinh: dat camera giua vung, nhung khong de lo ra ngoai ban do lon
    static float ClampAxis(float wanted, float min, float max, float fallback)
    {
        if (min > max) return fallback;
        return Mathf.Clamp(wanted, min, max);
    }
}
