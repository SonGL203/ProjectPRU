using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FishPond : MonoBehaviour
{
    public static readonly List<FishPond> AllPonds = new();

    public int maxCapacity = 6;
    public string pondName = "Ao Nuôi Cá";
    public Vector2 swimAreaSize = new Vector2(2.4f, 1.6f);

    readonly List<FishAgent> pondFish = new();

    public int CurrentFishCount
    {
        get
        {
            pondFish.RemoveAll(f => f == null);
            return pondFish.Count;
        }
    }

    public bool IsFull => CurrentFishCount >= maxCapacity;

    void Awake()
    {
        AllPonds.Add(this);
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnDestroy()
    {
        AllPonds.Remove(this);
    }

    public bool TryAddFish(FishAgent fish)
    {
        if (IsFull) return false;
        pondFish.Add(fish);
        fish.hasCustomBounds = true;
        fish.movementBounds = new Bounds(transform.position, new Vector3(swimAreaSize.x, swimAreaSize.y, 1f));
        return true;
    }

    public static FishPond FindPondAt(Vector3 worldPos)
    {
        for (int i = 0; i < AllPonds.Count; i++)
        {
            var p = AllPonds[i];
            if (!p) continue;
            var col = p.GetComponent<BoxCollider2D>();
            if (col && col.bounds.Contains(worldPos))
                return p;
        }
        return null;
    }
}
