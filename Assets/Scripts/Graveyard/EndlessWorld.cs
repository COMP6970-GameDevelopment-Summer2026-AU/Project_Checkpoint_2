// EndlessWorld.cs — makes the graveyard endless. The ground is one large static
// plane (built by the world builder); this streamer keeps the area around the
// player populated with decoration, harvestable resources, and ghosts. As the
// player crosses into new grid cells, fresh content spawns ahead and far-away
// content is removed, so the yard never runs out and never fills memory.

using System.Collections.Generic;
using UnityEngine;

public class EndlessWorld : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Streaming grid")]
    public float cellSize = 22f;
    public int viewRadius = 3;          // cells kept around the player

    [Header("Content models (assigned by the builder)")]
    public Material colormap;
    public GameObject[] woodModels;
    public GameObject[] stoneModels;
    public GameObject[] pumpkinModels;
    public GameObject[] decorModels;
    public GameObject[] coffinModels;
    public GameObject ghostModel;

    [Header("Density per cell")]
    public int decorPerCell = 4;
    public int woodPerCell = 2;
    public int stonePerCell = 2;
    public int pumpkinPerCell = 1;
    [Range(0f, 1f)] public float ghostChance = 0.55f;
    [Range(0f, 1f)] public float coffinChance = 0.35f;
    [Range(0f, 1f)] public float hazardChance = 0.28f;

    readonly Dictionary<Vector2Int, List<GameObject>> cells = new Dictionary<Vector2Int, List<GameObject>>();
    Vector2Int center;
    bool started;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null) return;
        center = CellOf(player.position);
        Refresh(true);
        started = true;
    }

    void Update()
    {
        if (!started || player == null) return;
        var c = CellOf(player.position);
        if (c != center) { center = c; Refresh(false); }
    }

    Vector2Int CellOf(Vector3 p) =>
        new Vector2Int(Mathf.FloorToInt(p.x / cellSize), Mathf.FloorToInt(p.z / cellSize));

    void Refresh(bool immediate)
    {
        // Spawn any missing cells within view.
        for (int dx = -viewRadius; dx <= viewRadius; dx++)
            for (int dz = -viewRadius; dz <= viewRadius; dz++)
            {
                var cell = new Vector2Int(center.x + dx, center.y + dz);
                if (!cells.ContainsKey(cell)) Populate(cell);
            }

        // Remove cells that fell outside view (+1 buffer).
        var toRemove = new List<Vector2Int>();
        foreach (var kv in cells)
            if (Mathf.Abs(kv.Key.x - center.x) > viewRadius + 1 ||
                Mathf.Abs(kv.Key.y - center.y) > viewRadius + 1)
                toRemove.Add(kv.Key);

        foreach (var key in toRemove)
        {
            foreach (var go in cells[key]) if (go) Destroy(go);
            cells.Remove(key);
        }
    }

    void Populate(Vector2Int cell)
    {
        var list = new List<GameObject>();
        var rng = new System.Random(cell.x * 73856093 ^ cell.y * 19349663);

        // Keep the very first cell (spawn area) clearer.
        bool spawnCell = cell == Vector2Int.zero;

        int decor = spawnCell ? decorPerCell / 2 : decorPerCell;
        for (int i = 0; i < decor; i++)
            AddDecor(cell, list, rng);

        if (!spawnCell)
        {
            for (int i = 0; i < woodPerCell; i++)    AddResource(cell, list, rng, woodModels,    Harvestable.ResourceType.Wood, 3);
            for (int i = 0; i < stonePerCell; i++)   AddResource(cell, list, rng, stoneModels,   Harvestable.ResourceType.Stone, 3);
            for (int i = 0; i < pumpkinPerCell; i++) AddResource(cell, list, rng, pumpkinModels, Harvestable.ResourceType.Pumpkin, 1);

            if (ghostModel != null && rng.NextDouble() < ghostChance)
                AddGhost(cell, list, rng);
            if (rng.NextDouble() < coffinChance) AddCoffin(cell, list, rng);
            if (rng.NextDouble() < hazardChance) AddHazard(cell, list, rng);
        }

        cells[cell] = list;
    }

    Vector3 RandomPos(Vector2Int cell, System.Random rng)
    {
        float x = cell.x * cellSize + (float)rng.NextDouble() * cellSize;
        float z = cell.y * cellSize + (float)rng.NextDouble() * cellSize;
        return new Vector3(x, 0f, z);
    }

    GameObject Instantiate(GameObject model, Vector3 pos, System.Random rng, List<GameObject> list)
    {
        if (model == null) return null;          // don't spawn placeholder cubes
        GameObject go = Object.Instantiate(model);
        go.transform.SetParent(transform);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        ApplyColormap(go);
        list.Add(go);
        return go;
    }

    void AddDecor(Vector2Int cell, List<GameObject> list, System.Random rng)
    {
        if (decorModels == null || decorModels.Length == 0) return;
        var model = decorModels[rng.Next(decorModels.Length)];
        var go = Instantiate(model, RandomPos(cell, rng), rng, list);
        if (go == null) return;

        // Add a flickering flame light to fire/lantern props (visual polish).
        if (model != null)
        {
            string n = model.name.ToLowerInvariant();
            if (n.Contains("fire") || n.Contains("lantern") || n.Contains("candle") ||
                n.Contains("lightpost") || n.Contains("torch"))
            {
                var lgo = new GameObject("Flame");
                lgo.transform.SetParent(go.transform, false);
                lgo.transform.localPosition = Vector3.up * 1.2f;
                lgo.AddComponent<Light>();
                lgo.AddComponent<FlickerLight>();
            }
        }
    }

    void AddCoffin(Vector2Int cell, List<GameObject> list, System.Random rng)
    {
        var model = (coffinModels != null && coffinModels.Length > 0)
            ? coffinModels[rng.Next(coffinModels.Length)] : null;
        var go = Instantiate(model, RandomPos(cell, rng), rng, list);
        if (go == null) return;

        var b = GetBounds(go);
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size = new Vector3(
            Mathf.Max(0.6f, b.size.x / Mathf.Max(0.01f, go.transform.lossyScale.x)),
            Mathf.Max(0.6f, b.size.y / Mathf.Max(0.01f, go.transform.lossyScale.y)),
            Mathf.Max(0.6f, b.size.z / Mathf.Max(0.01f, go.transform.lossyScale.z)));

        go.AddComponent<Openable>();
    }

    void AddHazard(Vector2Int cell, List<GameObject> list, System.Random rng)
    {
        var go = new GameObject("HazardMist");
        go.transform.SetParent(transform);
        go.transform.position = RandomPos(cell, rng);
        go.AddComponent<HazardZone>();
        list.Add(go);
    }

    void AddResource(Vector2Int cell, List<GameObject> list, System.Random rng,
                     GameObject[] models, Harvestable.ResourceType type, int hits)
    {
        if (models == null || models.Length == 0) return;
        var model = models[rng.Next(models.Length)];
        var go = Instantiate(model, RandomPos(cell, rng), rng, list);
        if (go == null) return;

        var b = GetBounds(go);
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size = new Vector3(
            Mathf.Max(0.5f, b.size.x / Mathf.Max(0.01f, go.transform.lossyScale.x)),
            Mathf.Max(0.5f, b.size.y / Mathf.Max(0.01f, go.transform.lossyScale.y)),
            Mathf.Max(0.5f, b.size.z / Mathf.Max(0.01f, go.transform.lossyScale.z)));

        var h = go.AddComponent<Harvestable>();
        h.type = type;
        h.hitsToHarvest = hits;
        h.yieldAmount = 1;
    }

    void AddGhost(Vector2Int cell, List<GameObject> list, System.Random rng)
    {
        var pos = RandomPos(cell, rng) + Vector3.up * 1.2f;
        var go = Instantiate(ghostModel, pos, rng, list);
        if (go == null) return;
        go.AddComponent<GhostWander>();
    }

    void ApplyColormap(GameObject go)
    {
        if (colormap == null) return;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = colormap;
            r.sharedMaterials = mats;
        }
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
