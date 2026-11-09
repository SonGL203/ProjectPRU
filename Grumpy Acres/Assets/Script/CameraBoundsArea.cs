using System.Collections.Generic;
using UnityEngine;

// Vung gioi han camera, dat trong scene. Keo BoxCollider2D cho vua ban do trong Scene view.
// Nhieu vung co the chong len nhau: camera dung vung nho nhat dang chua nguoi choi.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraBoundsArea : MonoBehaviour
{
    static readonly List<CameraBoundsArea> areas = new List<CameraBoundsArea>();

    BoxCollider2D box;

    public static List<CameraBoundsArea> All
    {
        get { return areas; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        areas.Clear();
    }

    void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnEnable()
    {
        areas.Add(this);
    }

    void OnDisable()
    {
        areas.Remove(this);
    }

    public Rect GetRect()
    {
        if (!box) box = GetComponent<BoxCollider2D>();
        Vector3 center = transform.TransformPoint(box.offset);
        Vector3 scale = transform.lossyScale;
        float width = box.size.x * Mathf.Abs(scale.x);
        float height = box.size.y * Mathf.Abs(scale.y);
        return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
    }

    void OnDrawGizmos()
    {
        Rect rect = GetRect();
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireCube(rect.center, rect.size);
    }
}
