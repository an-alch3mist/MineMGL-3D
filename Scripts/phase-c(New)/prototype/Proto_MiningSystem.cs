using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — MiningSystem (end-to-end)
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY MiningSystem feature:
///   1. OreNode: health, TakeDamage, BreakNode, destroy on break
///   2. Random model selection: each node picks 1 of 3 child models on Start (hides others)
///   3. Weighted random drops: drop table with weights (iron=70, gold=20, diamond=10)
///   4. Drop count: Random.Range(minDrops, maxDrops+1)
///   5. Drop velocity: upward burst + lateral spread + angular
///   6. Particle burst at break point (primitive spheres, auto-destroy)
///   7. IDamageable interface pattern (raycast → GetComponent → TakeDamage)
///   8. ISaveLoadableStaticBreakable: GetPosition (truncated), DestroyFromLoading
///   9. Multiple resource types with different health/drop configs
///
/// Controls:
///   LMB = raycast from camera → hit node → TakeDamage(15)
///   H = hit ALL nodes once (tests bulk)
///   R = reset scene (respawn all nodes + clean drops)
///   I = print status (nodes remaining, drops on ground)
/// </summary>
public class Proto_MiningSystem : MonoBehaviour
{
    Camera cam;
    List<MNode> nodes = new List<MNode>();
    int dropCount;

    void Start()
    {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);
        // → spawn nodes
        SpawnNodes();
        Debug.Log("[Mining] LMB=hit, H=hit all, R=reset, I=info");
    }

    void Update()
    {
        // → mouse look
        float mx = Input.GetAxis("Mouse X") * 3f;
        float my = Input.GetAxis("Mouse Y") * 3f;
        cam.transform.Rotate(-my, mx, 0f, Space.Self);
        cam.transform.localEulerAngles = new Vector3(cam.transform.localEulerAngles.x, cam.transform.localEulerAngles.y, 0f);

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 50f))
            {
                var node = hit.collider.GetComponentInParent<MNode>();
                if (node != null) node.TakeDamage(15f, hit.point);
                else Debug.Log($"[Mining] Hit {hit.collider.name} — not a node");
            }
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            foreach (var n in nodes.ToArray())
                if (n != null) n.TakeDamage(15f, n.transform.position + Vector3.up * 0.5f);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var n in nodes) if (n != null) Destroy(n.gameObject);
            nodes.Clear();
            foreach (var d in FindObjectsByType<MDrop>(FindObjectsSortMode.None)) Destroy(d.gameObject);
            dropCount = 0;
            SpawnNodes();
            Debug.Log("[Mining] Reset done");
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            int alive = 0;
            foreach (var n in nodes) if (n != null) alive++;
            var drops = FindObjectsByType<MDrop>(FindObjectsSortMode.None);
            Debug.Log($"[Mining] Nodes: {alive}/{nodes.Count}, Drops on ground: {drops.Length}");
        }
    }

    void SpawnNodes()
    {
        // → 3 resource configs with different health, drops, and weighted drop tables
        var configs = new[]
        {
            new MNodeCfg("Iron", Color.grey, 30f, 2, 4, new[] {
                new MDropEntry("IronOre", Color.grey, 0.25f, 70f),
                new MDropEntry("CoalOre", Color.black, 0.2f, 30f) }),
            new MNodeCfg("Gold", new Color(1f, 0.84f, 0f), 45f, 1, 3, new[] {
                new MDropEntry("GoldOre", new Color(1f, 0.84f, 0f), 0.22f, 80f),
                new MDropEntry("GoldGem", Color.cyan, 0.15f, 20f) }),
            new MNodeCfg("Copper", new Color(0.72f, 0.45f, 0.2f), 25f, 3, 5, new[] {
                new MDropEntry("CopperOre", new Color(0.72f, 0.45f, 0.2f), 0.28f, 60f),
                new MDropEntry("CopperCrushed", new Color(0.5f, 0.3f, 0.1f), 0.18f, 30f),
                new MDropEntry("DiamondGem", new Color(0.6f, 0.95f, 1f), 0.12f, 10f) }),
        };
        for (int i = 0; i < 6; i++)
        {
            var cfg = configs[i % configs.Length];
            var go = new GameObject($"Node_{cfg.name}_{i}");
            go.transform.position = new Vector3(-5 + i * 2, 1f, 7f);
            // → 3 child models (random selection — OreNode._models pattern)
            var models = new GameObject[3];
            PrimitiveType[] shapes = { PrimitiveType.Sphere, PrimitiveType.Cube, PrimitiveType.Capsule };
            for (int m = 0; m < 3; m++)
            {
                models[m] = GameObject.CreatePrimitive(shapes[m]);
                models[m].name = $"Model_{m}";
                models[m].transform.SetParent(go.transform);
                models[m].transform.localPosition = Vector3.zero;
                models[m].transform.localScale = Vector3.one * 1.2f;
                models[m].GetComponent<Renderer>().material.color = cfg.color;
            }
            // → pick one random model, hide others (matches OreNode.Start)
            int pick = Random.Range(0, 3);
            for (int m = 0; m < 3; m++) models[m].SetActive(m == pick);

            var node = go.AddComponent<MNode>();
            node.Init(cfg, this);
            nodes.Add(node);
        }
    }

    public void OnDropSpawned() => dropCount++;

    // ═══ data structs ═══
    public class MDropEntry
    {
        public string name; public Color color; public float scale; public float weight;
        public MDropEntry(string n, Color c, float s, float w) { name = n; color = c; scale = s; weight = w; }
    }
    public class MNodeCfg
    {
        public string name; public Color color; public float health;
        public int minDrops, maxDrops; public MDropEntry[] drops;
        public MNodeCfg(string n, Color c, float h, int minD, int maxD, MDropEntry[] d)
        { name = n; color = c; health = h; minDrops = minD; maxDrops = maxD; drops = d; }
    }
}

// ═══════════════════════════════════════════
// MNode — breakable node with weighted drops + model selection
// ═══════════════════════════════════════════
public class MNode : MonoBehaviour
{
    Proto_MiningSystem.MNodeCfg cfg;
    Proto_MiningSystem proto;
    float health;

    public void Init(Proto_MiningSystem.MNodeCfg c, Proto_MiningSystem p)
    { cfg = c; proto = p; health = c.health; }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        health -= damage;
        Debug.Log($"[Mining] HIT {cfg.name}: health={health:F0}/{cfg.health}");
        if (health <= 0f) BreakNode(hitPoint);
    }

    void BreakNode(Vector3 hitPoint)
    {
        int count = Random.Range(cfg.minDrops, cfg.maxDrops + 1);
        Vector3 center = (transform.position + hitPoint) * 0.5f;
        // → weighted random drops
        for (int i = 0; i < count; i++)
        {
            var drop = WeightedRandom(cfg.drops);
            Vector3 pos = center + Random.insideUnitSphere * 0.15f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Drop_{drop.name}";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * drop.scale;
            go.GetComponent<Renderer>().material.color = drop.color;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearVelocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 4f), Random.Range(-1.5f, 1.5f));
            rb.angularVelocity = Random.insideUnitSphere * Random.Range(1f, 50f);
            go.AddComponent<MDrop>();
            proto.OnDropSpawned();
        }
        Debug.Log($"[Mining] BREAK {cfg.name}: {count} drops (weighted random)");
        // → particle burst
        for (int i = 0; i < 10; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.position = hitPoint + Random.insideUnitSphere * 0.3f;
            p.transform.localScale = Vector3.one * 0.06f;
            p.GetComponent<Renderer>().material.color = cfg.color * 0.6f;
            Destroy(p.GetComponent<Collider>());
            var prb = p.AddComponent<Rigidbody>();
            prb.linearVelocity = Random.insideUnitSphere * 4f;
            prb.useGravity = false;
            Object.Destroy(p, 0.35f);
        }
        // → ISaveLoadableStaticBreakable: log truncated position
        Vector3 tp = new Vector3(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y), Mathf.Floor(transform.position.z));
        Debug.Log($"[Mining] SAVE_POS: {tp} (truncated for persistence)");
        Object.Destroy(gameObject);
    }

    // → DestroyFromLoading (ISaveLoadableStaticBreakable)
    public void DestroyFromLoading()
    {
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        Object.Destroy(gameObject);
    }

    Proto_MiningSystem.MDropEntry WeightedRandom(Proto_MiningSystem.MDropEntry[] items)
    {
        float total = 0f;
        foreach (var e in items) total += e.weight;
        float roll = Random.value * total;
        float cum = 0f;
        foreach (var e in items) { cum += e.weight; if (roll <= cum) return e; }
        return items[items.Length - 1];
    }
}

public class MDrop : MonoBehaviour { }