using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance { get; private set; }

    public float cellSize = 0.5f;
    public float agentRadius = 0.15f;

    [Tooltip("Tu lay kich thuoc tu cac Tilemap trong scene")]
    public bool autoBounds = true;
    public Vector2 center;
    public Vector2 size = new(60f, 40f);

    [Tooltip("Quet lai vat can moi bao nhieu giay (cong trinh bi pha thi duong mo ra)")]
    public float rebuildInterval = 2f;

    bool[] blocked;
    int w, h;
    Vector2 origin;
    float rebuildTimer;
    readonly Collider2D[] hits = new Collider2D[16];
    ContactFilter2D filter;

    static readonly Vector2Int[] Dirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    void Awake()
    {
        Instance = this;
        filter = new ContactFilter2D { useTriggers = false };
        if (autoBounds) FitToTilemaps();
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        rebuildTimer -= Time.deltaTime;
        if (rebuildTimer > 0f) return;
        rebuildTimer = rebuildInterval;
        Build();
    }

    void FitToTilemaps()
    {
        bool any = false;
        Bounds b = default;
        foreach (var r in FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
        {
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!any) return;
        center = b.center;
        size = b.size + Vector3.one * 4f;
    }

    void Build()
    {
        w = Mathf.CeilToInt(size.x / cellSize);
        h = Mathf.CeilToInt(size.y / cellSize);
        origin = center - size * 0.5f;
        if (blocked == null || blocked.Length != w * h) blocked = new bool[w * h];

        float radius = cellSize * 0.5f + agentRadius;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int n = Physics2D.OverlapCircle(CellCenter(x, y), radius, filter, hits);
                bool block = false;
                for (int i = 0; i < n && !block; i++)
                {
                    var c = hits[i];
                    block = !c.GetComponentInParent<Enemy>() && !c.GetComponentInParent<PlayerMove>();
                }
                blocked[y * w + x] = block;
            }
    }

    Vector2 CellCenter(int x, int y) => origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

    Vector2Int ToCell(Vector2 p) => new(
        Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, w - 1),
        Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / cellSize), 0, h - 1));

    bool Blocked(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || blocked[y * w + x];

    Vector2Int NearestFree(Vector2Int c)
    {
        if (!Blocked(c.x, c.y)) return c;
        for (int r = 1; r < 20; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    if (!Blocked(c.x + dx, c.y + dy)) return new Vector2Int(c.x + dx, c.y + dy);
                }
        return c;
    }

    public bool FindPath(Vector2 from, Vector2 to, List<Vector2> result)
    {
        result.Clear();
        if (blocked == null) return false;

        var start = NearestFree(ToCell(from));
        var goal = NearestFree(ToCell(to));
        int startId = start.y * w + start.x, goalId = goal.y * w + goal.x;

        var g = new float[w * h];
        var parent = new int[w * h];
        var closed = new bool[w * h];
        for (int i = 0; i < g.Length; i++) { g[i] = float.MaxValue; parent[i] = -1; }

        var open = new SortedSet<(float f, int id)>();
        g[startId] = 0f;
        open.Add((Heuristic(start, goal), startId));

        while (open.Count > 0)
        {
            var cur = open.Min;
            open.Remove(cur);
            int id = cur.id;
            if (closed[id]) continue;
            closed[id] = true;
            if (id == goalId) break;

            int cx = id % w, cy = id / w;
            foreach (var d in Dirs)
            {
                int nx = cx + d.x, ny = cy + d.y;
                if (Blocked(nx, ny)) continue;
                bool diagonal = d.x != 0 && d.y != 0;
                if (diagonal && (Blocked(cx + d.x, cy) || Blocked(cx, cy + d.y))) continue;

                int nid = ny * w + nx;
                if (closed[nid]) continue;
                float ng = g[id] + (diagonal ? 1.414f : 1f);
                if (ng >= g[nid]) continue;
                g[nid] = ng;
                parent[nid] = id;
                open.Add((ng + Heuristic(new Vector2Int(nx, ny), goal), nid));
            }
        }

        if (!closed[goalId]) return false;

        var cells = new List<Vector2>();
        for (int id = goalId; id != -1; id = parent[id])
            cells.Add(CellCenter(id % w, id / w));
        cells.Reverse();

        Smooth(cells, result);
        return true;
    }

    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
    }

    void Smooth(List<Vector2> cells, List<Vector2> result)
    {
        int i = 0;
        while (i < cells.Count - 1)
        {
            int far = i + 1;
            for (int j = cells.Count - 1; j > i + 1; j--)
                if (LineClear(cells[i], cells[j])) { far = j; break; }
            result.Add(cells[far]);
            i = far;
        }
    }

    bool LineClear(Vector2 a, Vector2 b)
    {
        float dist = Vector2.Distance(a, b);
        int steps = Mathf.CeilToInt(dist / (cellSize * 0.5f));
        for (int s = 1; s < steps; s++)
        {
            var c = ToCell(Vector2.Lerp(a, b, s / (float)steps));
            if (Blocked(c.x, c.y)) return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (blocked == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (blocked[y * w + x])
                    Gizmos.DrawCube(CellCenter(x, y), Vector3.one * cellSize * 0.9f);
    }
}
