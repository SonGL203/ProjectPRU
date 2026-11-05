using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Tooltip("Cac loai enemy (Enemy1, Enemy2...)")]
    public GameObject[] enemyPrefabs;

    [Header("Diem sinh ra (de trong thi tu lay mep trai/phai man hinh)")]
    public Transform[] leftPoints;
    public Transform[] rightPoints;
    public float edgeMargin = 2f;

    [Header("So luong")]
    public int startCount = 8;
    public int extraPerWeek = 1;
    public int maxCount = 20;

    [Header("May")]
    public float baseHealth = 26f;
    [Tooltip("Tang mau moi tuan. Dat 0 de giu nguyen 2 nhat / 4 nhat chem cuoc (xanh / tim)")]
    public float healthPerWeek = 8f;

    [System.Serializable]
    public struct HealthRule
    {
        public string prefabName;
        public float multiplier;
    }

    [Tooltip("He so mau theo ten prefab. Enemy1 (tim) x2 = 60 mau: chem 4 nhat / cowboy 15 dmg 4 phat. Enemy2 (xanh) x1 = 30 mau: 2 nhat")]
    public HealthRule[] healthRules =
    {
        new HealthRule { prefabName = "Enemy1", multiplier = 1.8f },
        new HealthRule { prefabName = "Enemy2", multiplier = 1f },
    };

    [Header("Thoi gian")]
    public float delayBeforeWave = 6f;
    public float spawnInterval = 0.5f;

    DayNightCycle subscribedCycle;

    void OnEnable()
    {
        if (GameManager.HasInstance) GameManager.Instance.OnNewDay += HandleNewDay;
        else Debug.LogWarning("WaveSpawner: scene chua co GameManager.");
    }

    void Start()
    {
        subscribedCycle = DayNightCycle.Instance;
        if (subscribedCycle) subscribedCycle.OnDusk += HandleDusk;
        else Debug.LogWarning("WaveSpawner: scene chua co DayNightCycle.");
    }

    void OnDisable()
    {
        if (GameManager.HasInstance) GameManager.Instance.OnNewDay -= HandleNewDay;
    }

    void OnDestroy()
    {
        if (subscribedCycle) subscribedCycle.OnDusk -= HandleDusk;
    }

    void HandleNewDay(WeekDay day, int week)
    {
        StopAllCoroutines();
        foreach (var e in Enemy.Alive.ToArray()) if (e) e.Vanish();
    }

    void HandleDusk()
    {
        var gm = GameManager.Instance;
        if (!gm || gm.today != gm.attackDay) return;

        StopAllCoroutines();
        int week = gm.week;
        int count = Mathf.Min(startCount + extraPerWeek * (week - 1), maxCount);
        float health = baseHealth + healthPerWeek * (week - 1);
        StartCoroutine(SpawnWave(count, health));
    }

    IEnumerator SpawnWave(int count, float health)
    {
        if (enemyPrefabs.Length == 0) yield break;

        yield return new WaitForSeconds(delayBeforeWave);
        for (int i = 0; i < count; i++)
        {
            bool left = i % 2 == 0;
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var enemy = Instantiate(prefab, SpawnPosition(left), Quaternion.identity);

            var e = enemy.GetComponent<Enemy>();
            if (!e) e = enemy.AddComponent<Enemy>();
            foreach (var rule in healthRules)
                if (rule.prefabName == prefab.name) e.healthMultiplier = rule.multiplier;
            e.SetMaxHealth(health);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    Vector3 SpawnPosition(bool left)
    {
        var points = left ? leftPoints : rightPoints;
        if (points != null && points.Length > 0)
            return points[Random.Range(0, points.Length)].position;

        var cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float x = cam.transform.position.x + (left ? -halfW - edgeMargin : halfW + edgeMargin);
        float y = cam.transform.position.y + Random.Range(-halfH, halfH);
        return new Vector3(x, y, 0f);
    }
}
