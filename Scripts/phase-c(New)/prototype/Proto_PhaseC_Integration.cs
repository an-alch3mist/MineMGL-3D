using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE INTEGRATION PROTOTYPE — Phase C (all systems wired together)
/// Zero dependencies. Empty scene → add one GO → add this script → Play.
///
/// Wires ALL cross-system event chains from FLOW.md using inline static Action fields.
/// Every event raise logs with frame number. FPS controller (WASD + mouse).
///
/// ALL FLOWS TESTED:
///   1. Mining flow: hit node → TakeDamage → BreakNode → OnSpawnOreRequested → pool spawns
///      → OnCreateParticleRequested → particles → OnOreNodeBroken → OnOreMined → OnStaticBreakableBroken
///   2. Sell flow: ore enters seller trigger → SellAfterDelay → 2s → OnOreSold → money adds
///   3. AutoMiner flow: timer → probability → weighted random → pool spawn → throttle
///   4. Limit flow: count moving → OnOreLimitChanged → warning UI show/hide
///   5. Pool lifecycle: spawn (Instantiate or dequeue) → use → ReturnToPool (full reset) → reuse
///   6. Crush flow: C key → TryConvertToCrushed → 2 smaller pieces → original returns to pool
///   7. Polish flow: P key → AddPolish → at 100% CompletePolishing → material swap
///   8. Cross-system: OnOreSold → EconomyManager adds money (mocked) + QuestManager tracks (mocked)
///   9. Cleanup flow: ore falls below y=-100 → OreManager round-robin detects → ReturnToPool
///
/// EDGE CASES:
///   - Double-sell prevention: ore enters 2 sellers → only first sells
///   - Non-rigidbody in seller → ignored
///   - BaseSellableItem fallback → instant destroy
///   - Domain reload: static fields reset via [RuntimeInitializeOnLoadMethod]
///   - OreLimitManager block → AutoMiner stops spawning
///   - Pool warmup vs reuse (first N = Instantiate, after R = Dequeue)
///
/// Controls:
///   WASD = move, Mouse = look, Space = jump
///   LMB = hit nearest node (15 damage)
///   N = spawn ore manually
///   M = spawn 30 ore (bulk — triggers limit)
///   C = crush nearest ore → 2 smaller pieces
///   P = polish nearest ore (+25%)
///   T = toggle auto-miner on/off
///   R = return ALL ore to pool
///   F = drop nearest ore to y=-120 (tests cleanup)
///   K = print full status (nodes, ore, pool, money, limit, event counts)
///   L = dump full event log (every event with frame number)
/// </summary>
public partial class Proto_PhaseC_Integration : MonoBehaviour
{
    #region inline event bus (mirrors GameEvents from FLOW.md)
    // → domain reload cleanup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnSpawnOreRequested = null; OnCreateParticleRequested = null;
        OnOreNodeBroken = null; OnOreMined = null; OnStaticBreakableBroken = null;
        OnOreSold = null; OnOreLimitChanged = null;
        eventLog.Clear(); IPiece.All.Clear();
    }

    static event Action<Color, Vector3, Vector3, Vector3> OnSpawnOreRequested;
    static event Action<Vector3> OnCreateParticleRequested;
    static event Action<Vector3, string> OnOreNodeBroken;
    static event Action<string, Vector3> OnOreMined;
    static event Action<Vector3> OnStaticBreakableBroken;
    static event Action<float, string> OnOreSold;
    static event Action<string> OnOreLimitChanged;

    static List<string> eventLog = new List<string>();
    static void Log(string evt, string detail)
    {
        string entry = $"[F{Time.frameCount}] {evt}: {detail}";
        eventLog.Add(entry);
        Debug.Log(entry);
    }
    #endregion

    #region state
    Camera cam;
    CharacterController cc;
    float xRot;
    float money;
    int sellCount;
    List<INode> nodes = new List<INode>();
    IPool pool;
    IAutoMiner autoMiner;
    ILimiter limiter;
    IWarning warning;
    ICleanup cleanup;
    ISeller seller1, seller2;
    List<Vector3> savedBrokenPositions = new List<Vector3>();
    Dictionary<string, int> eventCounts = new Dictionary<string, int>();
    #endregion

    #region setup
    void Start()
    {
        // → FPS controller
        cam = Camera.main;
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.3f;
        transform.position = new Vector3(0, 1.1f, 0);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(8, 1, 8);
        ground.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.3f);

        // → pool
        pool = new GameObject("[Pool]").AddComponent<IPool>();

        // → subscribe events (simulates all system subscriptions)
        // OrePiecePoolManager subscribes to spawn requests
        OnSpawnOreRequested += (color, pos, vel, angVel) =>
        {
            Count("OnSpawnOreRequested");
            var piece = pool.Spawn(pos, color);
            piece.GetRb().linearVelocity = vel;
            piece.GetRb().angularVelocity = angVel;
        };
        // ParticleManager subscribes
        OnCreateParticleRequested += (pos) =>
        {
            Count("OnCreateParticleRequested");
            for (int i = 0; i < 8; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.transform.position = pos + UnityEngine.Random.insideUnitSphere * 0.3f;
                p.transform.localScale = Vector3.one * 0.06f;
                p.GetComponent<Renderer>().material.color = Color.yellow * 0.7f;
                Destroy(p.GetComponent<Collider>());
                p.AddComponent<Rigidbody>().useGravity = false;
                p.GetComponent<Rigidbody>().linearVelocity = UnityEngine.Random.insideUnitSphere * 4f;
                Destroy(p, 0.35f);
            }
        };
        // Phase D: BuildingManager subscribes (mock)
        OnOreNodeBroken += (pos, type) => { Count("OnOreNodeBroken"); Log("OreNodeBroken", $"{type} at {pos:F1}"); };
        // Phase F: QuestManager subscribes (mock)
        OnOreMined += (type, pos) => { Count("OnOreMined"); Log("OreMined", $"{type}"); };
        // Phase G: SavingLoadingManager subscribes (mock)
        OnStaticBreakableBroken += (pos) => { Count("OnStaticBreakableBroken"); savedBrokenPositions.Add(pos); Log("StaticBreakableBroken", $"saved pos {pos:F0}"); };
        // EconomyManager subscribes
        OnOreSold += (value, type) => { Count("OnOreSold"); money += value; sellCount++; Log("OreSold", $"${value:F2} ({type}), total=${money:F2}"); };
        // PhysicsLimitUIWarning subscribes
        OnOreLimitChanged += (state) => { Count("OnOreLimitChanged"); warning.SetState(state); Log("OreLimitChanged", state); };

        // → spawn nodes (6 along the back wall)
        var nodeConfigs = new[] {
            ("Iron", Color.grey, 30f, 2, 4), ("Gold", new Color(1,0.84f,0), 45f, 1, 3),
            ("Copper", new Color(0.72f,0.45f,0.2f), 25f, 3, 5), ("Iron", Color.grey, 30f, 2, 4),
            ("Gold", new Color(1,0.84f,0), 45f, 1, 3), ("Coal", Color.black, 20f, 2, 6) };
        for (int i = 0; i < 6; i++)
        {
            var (name, color, hp, minD, maxD) = nodeConfigs[i];
            var node = SpawnNode(name, color, hp, minD, maxD, new Vector3(-5 + i * 2, 1.2f, 10));
            nodes.Add(node);
        }

        // → auto-miner
        autoMiner = new GameObject("AutoMiner").AddComponent<IAutoMiner>();
        autoMiner.transform.position = new Vector3(8, 0.5f, 8);
        autoMiner.pool = pool; autoMiner.limiter = null; // set below

        // → limiter + warning
        limiter = new GameObject("[Limiter]").AddComponent<ILimiter>();
        limiter.threshold = 25;
        warning = new GameObject("[Warning]").AddComponent<IWarning>();
        autoMiner.limiter = limiter;

        // → cleanup (round-robin)
        cleanup = new GameObject("[Cleanup]").AddComponent<ICleanup>();
        cleanup.pool = pool;

        // → 2 seller machines (tests double-sell prevention)
        seller1 = SpawnSeller("Seller_1", new Vector3(-6, 0.5f, 5));
        seller2 = SpawnSeller("Seller_2", new Vector3(-6, 0.5f, 8));

        Debug.Log("═══ PHASE C INTEGRATION ═══");
        Debug.Log("WASD=move, Mouse=look, Space=jump");
        Debug.Log("LMB=hit node, N=spawn ore, M=bulk 30, C=crush, P=polish");
        Debug.Log("T=toggle miner, R=return all, F=drop to abyss, K=status, L=event log");
    }
    #endregion

    #region FPS controller + input
    void Update()
    {
        // → mouse look
        float mx = Input.GetAxis("Mouse X") * 4f;
        float my = Input.GetAxis("Mouse Y") * 4f;
        xRot = Mathf.Clamp(xRot - my, -80f, 80f);
        transform.Rotate(0, mx, 0);
        cam.transform.localEulerAngles = new Vector3(xRot, transform.eulerAngles.y, 0);
        cam.transform.position = transform.position + Vector3.up * 0.8f;
        cam.transform.rotation = Quaternion.Euler(xRot, transform.eulerAngles.y, 0);

        // → WASD movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        move.y = cc.isGrounded ? -0.1f : -9.8f * Time.deltaTime;
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) move.y = 5f;
        cc.Move(move * 5f * Time.deltaTime);

        // ═══ input ═══
        if (Input.GetMouseButtonDown(0)) HitNearestNode();
        if (Input.GetKeyDown(KeyCode.N)) pool.Spawn(cam.transform.position + cam.transform.forward * 2f, RandomOreColor());
        if (Input.GetKeyDown(KeyCode.M)) { for (int i = 0; i < 30; i++) pool.Spawn(cam.transform.position + cam.transform.forward * 2f + UnityEngine.Random.insideUnitSphere, RandomOreColor()); Debug.Log("[INT] Bulk spawned 30"); }
        if (Input.GetKeyDown(KeyCode.C)) CrushNearest();
        if (Input.GetKeyDown(KeyCode.P)) PolishNearest();
        if (Input.GetKeyDown(KeyCode.T)) autoMiner.Toggle();
        if (Input.GetKeyDown(KeyCode.R)) { int c = IPiece.All.Count; while (IPiece.All.Count > 0) pool.ReturnToPool(IPiece.All[0]); Debug.Log($"[INT] Returned {c} to pool"); }
        if (Input.GetKeyDown(KeyCode.F)) { var ore = FindNearest(); if (ore != null) { ore.transform.position = new Vector3(0, -120f, 0); Debug.Log("[INT] Dropped to abyss"); } }
        if (Input.GetKeyDown(KeyCode.K)) PrintStatus();
        if (Input.GetKeyDown(KeyCode.L)) DumpLog();
    }
    #endregion

    #region mining flow
    void HitNearestNode()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 50f))
        {
            var node = hit.collider.GetComponentInParent<INode>();
            if (node != null)
            {
                node.TakeDamage(15f, hit.point);
                if (node.IsBroken())
                {
                    // → BreakNode: fire all events in sequence
                    int drops = UnityEngine.Random.Range(node.minDrops, node.maxDrops + 1);
                    Vector3 center = (node.transform.position + hit.point) * 0.5f;
                    for (int i = 0; i < drops; i++)
                    {
                        Vector3 pos = center + UnityEngine.Random.insideUnitSphere * 0.15f;
                        Vector3 vel = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(2f, 4f), UnityEngine.Random.Range(-1.5f, 1.5f));
                        Vector3 angVel = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(1f, 50f);
                        Log("SpawnOreRequested", $"drop {i + 1}/{drops}");
                        OnSpawnOreRequested?.Invoke(node.color, pos, vel, angVel);
                    }
                    Log("CreateParticleRequested", $"at {hit.point:F1}");
                    OnCreateParticleRequested?.Invoke(hit.point);
                    OnOreNodeBroken?.Invoke(node.transform.position, node.typeName);
                    OnOreMined?.Invoke(node.typeName, node.transform.position);
                    Vector3 tp = new Vector3(Mathf.Floor(node.transform.position.x), Mathf.Floor(node.transform.position.y), Mathf.Floor(node.transform.position.z));
                    OnStaticBreakableBroken?.Invoke(tp);
                    nodes.Remove(node);
                    Destroy(node.gameObject);
                }
            }
        }
    }
    #endregion

    #region ore operations
    void CrushNearest()
    {
        var ore = FindNearest();
        if (ore == null) return;
        Vector3 pos = ore.transform.position;
        Color c = ore.color * 0.7f;
        pool.ReturnToPool(ore);
        for (int i = 0; i < 2; i++)
        {
            var crushed = pool.Spawn(pos + UnityEngine.Random.insideUnitSphere * 0.2f, c);
            crushed.transform.localScale *= 0.6f;
            crushed.isCrushed = true;
        }
        Log("Crush", $"→ 2 smaller pieces at {pos:F1}");
    }

    void PolishNearest()
    {
        var ore = FindNearest();
        if (ore == null) return;
        ore.polishPct = Mathf.Min(1f, ore.polishPct + 0.25f);
        if (ore.polishPct >= 1f)
        {
            ore.GetComponent<Renderer>().material.color = Color.white;
            ore.isPolished = true;
            Log("Polish", "100% → material swapped to white (polished)");
        }
        else Log("Polish", $"{ore.polishPct * 100:F0}%");
    }

    IPiece FindNearest()
    {
        IPiece best = null; float bd = float.MaxValue;
        foreach (var o in IPiece.All)
        { float d = Vector3.Distance(o.transform.position, cam.transform.position); if (d < bd) { bd = d; best = o; } }
        if (best == null) Debug.Log("[INT] No active ore");
        return best;
    }
    #endregion

    #region helpers
    INode SpawnNode(string name, Color color, float hp, int minD, int maxD, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"Node_{name}"; go.transform.position = pos;
        go.transform.localScale = Vector3.one * 1.3f;
        go.GetComponent<Renderer>().material.color = color;
        var node = go.AddComponent<INode>();
        node.Init(name, color, hp, minD, maxD);
        return node;
    }

    ISeller SpawnSeller(string name, Vector3 pos)
    {
        var root = new GameObject(name); root.transform.position = pos;
        var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.transform.SetParent(root.transform); surface.transform.localPosition = Vector3.zero;
        surface.transform.localScale = new Vector3(2.5f, 0.3f, 2.5f);
        surface.GetComponent<Renderer>().material.color = new Color(0, 0.5f, 0);
        var trigGo = new GameObject("Trigger");
        trigGo.transform.SetParent(root.transform); trigGo.transform.localPosition = new Vector3(0, 0.5f, 0);
        var col = trigGo.AddComponent<BoxCollider>(); col.size = new Vector3(2.2f, 1f, 2.2f); col.isTrigger = true;
        var seller = trigGo.AddComponent<ISeller>();
        seller.pool = pool;
        seller.onSold = (value, type) =>
        {
            Log("SellerTrigger", $"ore entered → SellAfterDelay");
            OnOreSold?.Invoke(value, type);
        };
        return seller;
    }

    Color RandomOreColor()
    {
        Color[] c = { Color.grey, new Color(1, 0.84f, 0), new Color(0.72f, 0.45f, 0.2f), Color.black };
        return c[UnityEngine.Random.Range(0, c.Length)];
    }

    void Count(string evt)
    {
        if (!eventCounts.ContainsKey(evt)) eventCounts[evt] = 0;
        eventCounts[evt]++;
    }

    void PrintStatus()
    {
        Debug.Log($"═══ STATUS ═══");
        int alive = 0; foreach (var n in nodes) if (n != null) alive++;
        Debug.Log($"Nodes: {alive}/{nodes.Count}");
        Debug.Log($"Ore active: {IPiece.All.Count}, pooled: {pool.GetPooledCount()}");
        Debug.Log($"Money: ${money:F2} ({sellCount} sales)");
        Debug.Log($"Saved broken positions: {savedBrokenPositions.Count}");
        Debug.Log($"AutoMiner: {(autoMiner.isOn ? "ON" : "OFF")}, spawned: {autoMiner.spawnCount}");
        Debug.Log($"Limit state: {limiter.currentState} (threshold={limiter.threshold})");
        string evtStr = ""; foreach (var kv in eventCounts) evtStr += $"{kv.Key}={kv.Value} ";
        Debug.Log($"Event counts: {evtStr}");
    }

    void DumpLog()
    {
        Debug.Log($"═══ EVENT LOG ({eventLog.Count} entries) ═══");
        foreach (var entry in eventLog) Debug.Log(entry);
        Debug.Log($"═══ END LOG ═══");
    }
    #endregion
}

// ═══════════════════════════════════════════════════════════════
// INode — breakable node
// ═══════════════════════════════════════════════════════════════
public class INode : MonoBehaviour
{
    public string typeName; public Color color;
    public float health, maxHealth; public int minDrops, maxDrops;
    public void Init(string n, Color c, float hp, int minD, int maxD)
    { typeName = n; color = c; health = hp; maxHealth = hp; minDrops = minD; maxDrops = maxD; }
    public void TakeDamage(float dmg, Vector3 hitPoint)
    {
        health -= dmg;
        Debug.Log($"[INT] HIT {typeName}: {health:F0}/{maxHealth}");
    }
    public bool IsBroken() => health <= 0f;
}

// ═══════════════════════════════════════════════════════════════
// IPiece — poolable ore piece
// ═══════════════════════════════════════════════════════════════
public class IPiece : MonoBehaviour
{
    public static List<IPiece> All = new List<IPiece>();
    public Color color; public float sellValue; public float polishPct;
    public bool isPolished, isCrushed, isMarked;
    Rigidbody rb;
    void Awake() { rb = GetComponent<Rigidbody>(); }
    void OnEnable() { All.Add(this); isMarked = false; polishPct = 0f; isPolished = false; isCrushed = false; }
    void OnDisable() { All.Remove(this); }
    public Rigidbody GetRb() => rb;

    public void SellAfterDelay(IPool pool, Action<float, string> onSold)
    {
        if (isMarked) { Debug.Log("[INT] Double-sell PREVENTED"); return; }
        isMarked = true;
        GetComponent<Renderer>().material.color = Color.red;
        StartCoroutine(DoSell(pool, onSold));
    }
    IEnumerator DoSell(IPool pool, Action<float, string> onSold)
    {
        yield return new WaitForSeconds(2f);
        if (this == null || !isActiveAndEnabled) yield break;
        onSold?.Invoke(sellValue, isCrushed ? "crushed" : isPolished ? "polished" : "raw");
        pool.ReturnToPool(this);
    }
}

// ═══════════════════════════════════════════════════════════════
// IPool — keyed object pool
// ═══════════════════════════════════════════════════════════════
public class IPool : MonoBehaviour
{
    Queue<IPiece> pool = new Queue<IPiece>();
    int instantiateCount, dequeueCount;

    public IPiece Spawn(Vector3 pos, Color color)
    {
        IPiece ore;
        if (pool.Count > 0)
        {
            ore = pool.Dequeue(); ore.transform.position = pos;
            ore.gameObject.SetActive(true); dequeueCount++;
        }
        else
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (0.2f + UnityEngine.Random.Range(-0.04f, 0.04f));
            go.AddComponent<Rigidbody>().mass = 0.3f;
            ore = go.AddComponent<IPiece>(); instantiateCount++;
        }
        ore.color = color;
        ore.GetComponent<Renderer>().material.color = color;
        ore.sellValue = UnityEngine.Random.Range(0.9f, 1.1f);
        ore.GetRb().linearVelocity = Vector3.zero;
        ore.GetRb().angularVelocity = Vector3.zero;
        return ore;
    }

    public void ReturnToPool(IPiece ore)
    {
        if (ore == null || !ore.gameObject.activeSelf) return;
        ore.gameObject.SetActive(false);
        ore.GetRb().linearVelocity = Vector3.zero;
        ore.GetRb().angularVelocity = Vector3.zero;
        ore.GetRb().Sleep();
        ore.GetRb().linearDamping = 0.2f;
        ore.transform.SetParent(transform);
        pool.Enqueue(ore);
    }
    public int GetPooledCount() => pool.Count;
    public string GetStats() => $"instantiated={instantiateCount}, dequeued={dequeueCount}";
}

// ═══════════════════════════════════════════════════════════════
// ISeller — trigger-based seller (dual collider)
// ═══════════════════════════════════════════════════════════════
public class ISeller : MonoBehaviour
{
    public IPool pool;
    public Action<float, string> onSold;
    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) { Debug.Log($"[INT] Seller: ignored {other.name} (no rb)"); return; }
        var ore = other.GetComponent<IPiece>();
        if (ore != null) { ore.SellAfterDelay(pool, onSold); return; }
        // → generic sellable fallback (instant destroy)
        if (other.GetComponent<IGenericSellable>() is IGenericSellable gs)
        {
            onSold?.Invoke(gs.value, "generic");
            Destroy(gs.gameObject);
        }
    }
}

public class IGenericSellable : MonoBehaviour { public float value = 5f; }

// ═══════════════════════════════════════════════════════════════
// IAutoMiner — timed spawner with throttle
// ═══════════════════════════════════════════════════════════════
public class IAutoMiner : MonoBehaviour
{
    public IPool pool;
    public ILimiter limiter;
    public bool isOn = true;
    public int spawnCount;
    float timer = 2f, spawnRate = 2f, probability = 80f;
    GameObject rotator, light;
    static readonly Color[] dropColors = { Color.grey, new Color(1, 0.84f, 0), new Color(0.72f, 0.45f, 0.2f) };

    void Start()
    {
        var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseMesh.transform.SetParent(transform); baseMesh.transform.localPosition = Vector3.zero;
        baseMesh.transform.localScale = new Vector3(1, 0.3f, 1);
        baseMesh.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
        rotator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rotator.transform.SetParent(transform); rotator.transform.localPosition = new Vector3(0, 0.8f, 0);
        rotator.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
        Destroy(rotator.GetComponent<Collider>());
        light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        light.transform.SetParent(transform); light.transform.localPosition = new Vector3(0.4f, 0.3f, 0);
        light.transform.localScale = Vector3.one * 0.15f; Destroy(light.GetComponent<Collider>());
        SetLight(true);
    }

    void Update()
    {
        if (!isOn) return;
        rotator.transform.Rotate(Vector3.right, 360f / (spawnRate * 12f) * Time.deltaTime);
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // → ShouldBlockOreSpawning
            if (limiter != null && limiter.currentState == "BLOCKED") { timer = spawnRate; return; }
            if (UnityEngine.Random.Range(0f, 100f) <= probability)
            {
                pool.Spawn(transform.position + Vector3.down * 0.3f + Vector3.forward * 0.8f,
                    dropColors[UnityEngine.Random.Range(0, dropColors.Length)]);
                spawnCount++;
            }
            float mult = limiter != null ? limiter.GetMultiplier() : 1f;
            timer += spawnRate * mult;
        }
    }

    public void Toggle()
    {
        isOn = !isOn; SetLight(isOn);
        Debug.Log($"[INT] AutoMiner {(isOn ? "ON" : "OFF")}");
    }
    void SetLight(bool on) => light.GetComponent<Renderer>().material.color = on ? Color.green : Color.red;
}

// ═══════════════════════════════════════════════════════════════
// ILimiter — physics limit checker
// ═══════════════════════════════════════════════════════════════
public class ILimiter : MonoBehaviour
{
    public int threshold = 25;
    public string currentState = "REGULAR";
    float timer;
    public float GetMultiplier()
    {
        if (currentState == "SLIGHTLY") return 1.25f;
        if (currentState == "HIGHLY") return 1.5f;
        if (currentState == "BLOCKED") return 2f;
        return 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < 5f) return;
        timer = 0f;
        int moving = 0;
        foreach (var o in IPiece.All)
            if (o != null && !o.GetRb().IsSleeping()) { moving++; if (moving > threshold + 10) break; }
        string prev = currentState;
        currentState = moving > threshold + 10 ? "BLOCKED" :
                       moving > threshold + 5 ? "HIGHLY" :
                       moving > threshold ? "SLIGHTLY" : "REGULAR";
        if (currentState != prev)
            Proto_PhaseC_Integration.RaiseLimitChanged(currentState);
    }
}

// ═══════════════════════════════════════════════════════════════
// IWarning — visual limit warning
// ═══════════════════════════════════════════════════════════════
public class IWarning : MonoBehaviour
{
    GameObject soft, hard;
    void Awake()
    {
        soft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        soft.transform.position = new Vector3(0, 5, 5); soft.transform.localScale = new Vector3(3, 0.3f, 0.3f);
        soft.GetComponent<Renderer>().material.color = Color.yellow; Destroy(soft.GetComponent<Collider>()); soft.SetActive(false);
        hard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hard.transform.position = new Vector3(0, 5.5f, 5); hard.transform.localScale = new Vector3(3, 0.3f, 0.3f);
        hard.GetComponent<Renderer>().material.color = Color.red; Destroy(hard.GetComponent<Collider>()); hard.SetActive(false);
    }
    public void SetState(string state)
    {
        soft.SetActive(state == "SLIGHTLY" || state == "HIGHLY");
        hard.SetActive(state == "BLOCKED");
    }
}

// ═══════════════════════════════════════════════════════════════
// ICleanup — round-robin ore cleanup
// ═══════════════════════════════════════════════════════════════
public class ICleanup : MonoBehaviour
{
    public IPool pool;
    int idx;
    void Update()
    {
        if (IPiece.All.Count == 0) { idx = 0; return; }
        if (idx >= IPiece.All.Count) idx = 0;
        var ore = IPiece.All[idx];
        if (ore == null) { IPiece.All.RemoveAt(idx); return; }
        if (ore.transform.position.y < -100f || float.IsNaN(ore.transform.position.x))
        {
            Debug.Log("[INT] Cleanup: ore out of bounds → pool");
            pool.ReturnToPool(ore);
        }
        idx++;
    }
}

// partial to expose event raise for limiter
public partial class Proto_PhaseC_Integration
{
    public static void RaiseLimitChanged(string state)
    {
        OnOreLimitChanged?.Invoke(state);
    }
}