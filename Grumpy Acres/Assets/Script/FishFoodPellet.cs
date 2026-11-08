using System.Collections.Generic;
using UnityEngine;

public class FishFoodPellet : MonoBehaviour
{
    public static readonly List<FishFoodPellet> AllPellets = new();

    public float lifetime = 15f;
    public float sinkSpeed = 0.05f;

    float spawnTime;
    Vector3 startPos;
    SpriteRenderer sr;

    void Awake()
    {
        AllPellets.Add(this);
        spawnTime = Time.time;
        startPos = transform.position;

        sr = GetComponent<SpriteRenderer>();
        if (!sr)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 6;
        }
    }

    void OnDestroy()
    {
        AllPellets.Remove(this);
    }

    void Update()
    {
        float age = Time.time - spawnTime;
        if (age > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Bobbing & slow sink
        float bob = Mathf.Sin(Time.time * 4f) * 0.03f;
        transform.position = startPos + new Vector3(0f, -sinkSpeed * age + bob, 0f);

        // Fade out near end
        if (sr && age > lifetime - 2f)
        {
            float alpha = (lifetime - age) / 2f;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    public static FishFoodPellet FindNearest(Vector3 pos, float maxDist)
    {
        FishFoodPellet best = null;
        float bestSq = maxDist * maxDist;

        for (int i = 0; i < AllPellets.Count; i++)
        {
            var p = AllPellets[i];
            if (!p) continue;
            float sq = (p.transform.position - pos).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = p;
            }
        }
        return best;
    }
}
