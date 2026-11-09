using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Muc tieu theo doi (Player)")]
    public Transform target;

    [Header("Toc do muot ma (Damping)")]
    [Tooltip("Toc do cuon theo nhan vat. 0 = di chuyen tuc thi 1:1")]
    public float smoothSpeed = 12f;

    [Header("Khoang cach Z")]
    public float cameraZ = -10f;

    [Header("Gioi han khung hinh (Tuy chon)")]
    public bool clampToBounds = true;
    public float minX = 7.5f;
    public float maxX = 16.5f;
    public float minY = 4.2f;
    public float maxY = 7.8f;

    void Start()
    {
        FindTarget();
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, cameraZ);

        if (clampToBounds)
        {
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        if (smoothSpeed <= 0f)
        {
            transform.position = desired;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smoothSpeed);
        }
    }

    public void SnapToTarget()
    {
        if (target == null) FindTarget();
        if (target == null) return;

        Vector3 pos = new Vector3(target.position.x, target.position.y, cameraZ);
        if (clampToBounds)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }
        transform.position = pos;
    }

    void FindTarget()
    {
        var player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
        {
            target = player.transform;
        }
    }
}
