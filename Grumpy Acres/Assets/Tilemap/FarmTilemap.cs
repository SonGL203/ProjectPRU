using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FarmTilemap : MonoBehaviour
{
    public Tilemap ground, path, soil, water, decor, obstacles;
    public Transform homeBase;
    public Transform[] enemySpawns;

    [Header("Disease")]
    public Color infectedTint = new(0.65f, 0.45f, 0.85f);
    public float spreadInterval = 5f;
    [Range(0f, 1f)] public float spreadChance = 0.25f;

    static readonly Vector3Int[] Dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    readonly HashSet<Vector3Int> infected = new();
    float spreadTimer;

    public Vector3Int WorldToCell(Vector3 world) => ground.WorldToCell(world);
    public Vector3 CellToWorld(Vector3Int cell) => ground.GetCellCenterWorld(cell);

    public bool IsFarmable(Vector3Int cell) => soil.HasTile(cell);
    public bool IsWalkable(Vector3Int cell) => ground.HasTile(cell) && !water.HasTile(cell) && !obstacles.HasTile(cell);
    public bool IsInfected(Vector3Int cell) => infected.Contains(cell);

    public void Infect(Vector3Int cell)
    {
        if (!IsFarmable(cell) || !infected.Add(cell)) return;
        soil.SetTileFlags(cell, TileFlags.None);
        soil.SetColor(cell, infectedTint);
    }

    public void Cure(Vector3Int cell)
    {
        if (infected.Remove(cell)) soil.SetColor(cell, Color.white);
    }

    void Update()
    {
        if (infected.Count == 0) return;
        spreadTimer += Time.deltaTime;
        if (spreadTimer < spreadInterval) return;
        spreadTimer = 0f;

        var next = new List<Vector3Int>();
        foreach (var cell in infected)
        foreach (var d in Dirs)
            if (IsFarmable(cell + d) && !infected.Contains(cell + d) && Random.value < spreadChance)
                next.Add(cell + d);
        foreach (var cell in next) Infect(cell);
    }
}
