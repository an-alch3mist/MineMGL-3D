using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — OreSystem (end-to-end)
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY OreSystem feature:
///   1. OrePiecePoolManager: keyed pools (type+variant), dequeue/instantiate, ReturnToPool (full reset)
///   2. OrePiece: random scale variation, RandomPriceMultiplier, static AllOrePieces tracking
///   3. OreManager: round-robin cleanup (remove null, delete fallen below y=-50)
///   4. OreLimitManager: 5s check, count non-sleeping, 4 threshold states, spawn multiplier
///   5. PhysicsLimitUIWarning: visual panel show/hide per state (soft yellow / hard red)
///   6. OreDataService: resource color lookups, formatted colored name strings
///   7. AddPolish → CompletePolishing (swap material at 100%)
///   8. TryConvertToCrushed → spawns 2 smaller pieces, deletes original
///   9. SellAfterDelay: tag double-sell prevention, 2s coroutine, value calculation
///  10. BaseBasket: trigger tracking which pieces are inside
///  11. Multiple ore types: iron/gold/copper with different colors, sizes, values
///
/// Controls:
///   N = spawn random ore from pool
///   M = spawn 20 (bulk — tests limit)
///   C = crush nearest ore (→ 2 smaller pieces)
///   P = polish nearest ore (+25% each press, at 100% material swaps to shiny)
///   S = sell nearest ore (2s delay → money)
///   K = print counts + data service snapshot
///   L = force limit check
///   R = return all to pool
///   F = drop nearest ore below y=-50 (tests cleanup)
/// </summary>
public class Proto_OreSystem : MonoBehaviour
{
    Camera cam;
    OPool pool;
    OCleanup cleanup;
    OLimiter limiter;
    OWarningUI warningUI;
    ODataSvc dataSvc;
    float totalMoney;

    void Start()
    {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(5, 1, 5);
        // → pool
        pool = new GameObject("[OPool]").AddComponent<OPool>();
        // → cleanup (round-robin)
        cleanup = new GameObject("[OCleanup]").AddComponent<OCleanup>();
        cleanup.pool = pool;
        // → limiter
        limiter = new GameObject("[OLimiter]").AddComponent<OLimiter>();
        limiter.threshold = 25;
        // → warning UI (world-space text)
        warningUI = new GameObject("[OWarningUI]").AddComponent<OWarningUI>();
        // → data service
        dataSvc = new ODataSvc();
        // → basket trigger (box on the side)
        var basketGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basketGo.name = "Basket";
        basketGo.transform.position = new Vector3(-4, 0.5f, 5);
        basketGo.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);
        basketGo.GetComponent<Renderer>().material.color = new Color(0.4f, 0.2f, 0f);
        basketGo.GetComponent<BoxCollider>().isTrigger = true;
        basketGo.AddComponent<OBasket>();
        // → physical inner wall
        var bWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bWall.transform.SetParent(basketGo.transform);
        bWall.transform.localPosition = new Vector3(0, -0.2f, 0);
        bWall.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
        bWall.GetComponent<Renderer>().material.color = new Color(0.35f, 0.18f, 0f);

        Debug.Log("[Ore] N=spawn M=bulk C=crush P=polish S=sell K=info L=limit R=return F=drop");
    }

    void Update()
    {
        float mx = Input.GetAxis("Mouse X") * 3f;
        float my = Input.GetAxis("Mouse Y") * 3f;
        cam.transform.Rotate(-my, mx, 0f, Space.Self);
        cam.transform.localEulerAngles = new Vector3(cam.transform.localEulerAngles.x, cam.transform.localEulerAngles.y, 0f);

        if (Input.GetKeyDown(KeyCode.N))
        {
            pool.Spawn(cam.transform.position + cam.transform.forward * 2f, Random.Range(0, 3));
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            for (int i = 0; i < 20; i++)
                pool.Spawn(cam.transform.position + cam.transform.forward * 2f + Random.insideUnitSphere, Random.Range(0, 3));
            Debug.Log("[Ore] Bulk spawned 20");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            var ore = FindNearest();
            if (ore != null)
            {
                // → crush: destroy original, spawn 2 smaller pieces (TryConvertToCrushed)
                Vector3 pos = ore.transform.position;
                int type = ore.oreType;
                pool.ReturnToPool(ore);
                for (int i = 0; i < 2; i++)
                {
                    var crushed = pool.Spawn(pos + Random.insideUnitSphere * 0.2f, type);
                    crushed.transform.localScale *= 0.6f;
                    crushed.GetComponent<Renderer>().material.color *= 0.7f;
                    crushed.isCrushed = true;
                }
                Debug.Log($"[Ore] CRUSHED → 2 smaller pieces at {pos}");
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            var ore = FindNearest();
            if (ore != null)
            {
                ore.polishPercent = Mathf.Min(1f, ore.polishPercent + 0.25f);
                if (ore.polishPercent >= 1f)
                {
                    // → CompletePolishing: swap material to shiny
                    ore.GetComponent<Renderer>().material.color = Color.white;
                    ore.isPolished = true;
                    Debug.Log("[Ore] POLISH COMPLETE → material swapped to shiny white");
                }
                else
                    Debug.Log($"[Ore] Polish: {ore.polishPercent * 100:F0}%");
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            var ore = FindNearest();
            if (ore != null)
            {
                if (ore.isMarked) { Debug.Log("[Ore] Already selling — double-sell prevented"); return; }
                ore.SellAfterDelay(pool, (v) => { totalMoney += v; Debug.Log($"[Ore] SOLD ${v:F2} — Total: ${totalMoney:F2}"); });
            }
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log($"[Ore] Active: {OPiece.All.Count}, Pooled: {pool.GetPooledCount()}, Money: ${totalMoney:F2}");
            Debug.Log($"[Ore] DataSvc: {dataSvc.GetColoredString(0)}, {dataSvc.GetColoredString(1)}, {dataSvc.GetColoredString(2)}");
            // → basket contents
            foreach (var b in FindObjectsByType<OBasket>(FindObjectsSortMode.None))
                Debug.Log($"[Ore] Basket contains: {b.GetCount()} pieces");
        }
        if (Input.GetKeyDown(KeyCode.L)) limiter.ForceCheck();
        if (Input.GetKeyDown(KeyCode.R))
        {
            int c = OPiece.All.Count;
            while (OPiece.All.Count > 0) pool.ReturnToPool(OPiece.All[0]);
            Debug.Log($"[Ore] Returned {c} to pool");
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            var ore = FindNearest();
            if (ore != null)
            {
                ore.transform.position = new Vector3(0, -60f, 0);
                Debug.Log("[Ore] Dropped below y=-50 — cleanup will catch it");
            }
        }
    }

    OPiece FindNearest()
    {
        OPiece best = null; float bd = float.MaxValue;
        foreach (var o in OPiece.All)
        { float d = Vector3.Distance(o.transform.position, cam.transform.position); if (d < bd) { bd = d; best = o; } }
        if (best == null) Debug.Log("[Ore] No active ore");
        return best;
    }
}

// ═══ OPiece — poolable ore with polish/crush/sell ═══
public class OPiece : MonoBehaviour
{
    public static List<OPiece> All = new List<OPiece>();
    public int oreType;
    public float sellValue;
    public float polishPercent;
    public bool isPolished;
    public bool isCrushed;
    public bool isMarked;
    public HashSet<OBasket> baskets = new HashSet<OBasket>();
    Rigidbody rb;
    void Awake() { rb = GetComponent<Rigidbody>(); }
    void OnEnable() { All.Add(this); isMarked = false; polishPercent = 0f; isPolished = false; isCrushed = false; baskets.Clear(); }
    void OnDisable() { All.Remove(this); }
    public Rigidbody GetRb() => rb;

    public void SellAfterDelay(OPool pool, System.Action<float> onSold)
    {
        if (isMarked) return;
        isMarked = true;
        GetComponent<Renderer>().material.color = Color.red;
        StartCoroutine(DoSell(pool, onSold));
    }
    IEnumerator DoSell(OPool pool, System.Action<float> onSold)
    {
        yield return new WaitForSeconds(2f);
        if (this == null || !isActiveAndEnabled) yield break;
        float val = Mathf.Round(sellValue * 100f) / 100f;
        onSold?.Invoke(val);
        pool.ReturnToPool(this);
    }
}

// ═══ OPool — keyed pool (type → queue) ═══
public class OPool : MonoBehaviour
{
    Dictionary<int, Queue<OPiece>> pools = new Dictionary<int, Queue<OPiece>>();
    static readonly Color[] colors = { Color.grey, new Color(1f, 0.84f, 0f), new Color(0.72f, 0.45f, 0.2f) };
    static readonly string[] names = { "Iron", "Gold", "Copper" };
    static readonly float[] values = { 1f, 3f, 1.5f };
    static readonly float[] scales = { 0.25f, 0.2f, 0.28f };

    public OPiece Spawn(Vector3 pos, int type)
    {
        if (!pools.ContainsKey(type)) pools[type] = new Queue<OPiece>();
        OPiece ore;
        if (pools[type].Count > 0)
        {
            ore = pools[type].Dequeue();
            ore.transform.position = pos;
            ore.gameObject.SetActive(true);
            Debug.Log($"[Ore] Dequeued {names[type]} from pool");
        }
        else
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"{names[type]} [Pooled]";
            go.transform.position = pos;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 0.3f;
            ore = go.AddComponent<OPiece>();
            Debug.Log($"[Ore] Instantiated new {names[type]}");
        }
        ore.oreType = type;
        // → random scale variation (OrePiece.Start pattern)
        float baseScale = scales[type];
        float sv = 0.05f;
        ore.transform.localScale = new Vector3(baseScale + Random.Range(-sv, sv), baseScale + Random.Range(-sv, sv), baseScale + Random.Range(-sv, sv));
        ore.GetComponent<Renderer>().material.color = colors[type];
        ore.sellValue = values[type] * Random.Range(0.9f, 1.1f);
        ore.GetRb().linearVelocity = Vector3.zero;
        ore.GetRb().angularVelocity = Vector3.zero;
        return ore;
    }

    public void ReturnToPool(OPiece ore)
    {
        if (ore == null || !ore.gameObject.activeSelf) return;
        int key = ore.oreType;
        if (!pools.ContainsKey(key)) pools[key] = new Queue<OPiece>();
        ore.gameObject.SetActive(false);
        ore.GetRb().linearVelocity = Vector3.zero;
        ore.GetRb().angularVelocity = Vector3.zero;
        ore.GetRb().Sleep();
        ore.GetRb().linearDamping = 0.2f;
        ore.GetRb().angularDamping = 0.05f;
        // → clear basket back-refs
        foreach (var b in ore.baskets) b.Remove(ore);
        ore.baskets.Clear();
        ore.transform.SetParent(transform);
        pools[key].Enqueue(ore);
    }

    public int GetPooledCount()
    {
        int c = 0; foreach (var q in pools.Values) c += q.Count; return c;
    }
}

// ═══ OCleanup — round-robin (OreManager.Update) ═══
public class OCleanup : MonoBehaviour
{
    public OPool pool;
    int idx;
    void Update()
    {
        if (OPiece.All.Count == 0) { idx = 0; return; }
        if (idx >= OPiece.All.Count) idx = 0;
        var ore = OPiece.All[idx];
        if (ore == null) { OPiece.All.RemoveAt(idx); return; }
        if (ore.transform.position.y < -50f)
        {
            Debug.Log($"[Ore] Cleanup: ore fell below y=-50, returning to pool");
            pool.ReturnToPool(ore);
        }
        idx++;
    }
}

// ═══ OLimiter — physics limit (OreLimitManager) ═══
public class OLimiter : MonoBehaviour
{
    public int threshold = 25;
    float timer;
    string currentState = "REGULAR";
    public float GetMultiplier()
    {
        if (currentState == "SLIGHTLY") return 1.25f;
        if (currentState == "HIGHLY") return 1.5f;
        if (currentState == "BLOCKED") return 2f;
        return 1f;
    }

    public void ForceCheck() => timer = 999f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < 5f) return;
        timer = 0f;
        int moving = 0;
        foreach (var ore in OPiece.All)
            if (!ore.GetRb().IsSleeping()) { moving++; if (moving > threshold + 10) break; }
        string prev = currentState;
        currentState = moving > threshold + 10 ? "BLOCKED" :
                       moving > threshold + 5 ? "HIGHLY" :
                       moving > threshold ? "SLIGHTLY" : "REGULAR";
        if (currentState != prev)
            Debug.Log($"[Ore] LIMIT: {currentState} ({moving} moving / {threshold} limit, multiplier={GetMultiplier()}x)");
        // → notify warning UI
        OWarningUI.SetState(currentState);
    }
}

// ═══ OWarningUI — PhysicsLimitUIWarning ═══
public class OWarningUI : MonoBehaviour
{
    static OWarningUI instance;
    GameObject softWarn, hardWarn;

    void Awake()
    {
        instance = this;
        // → create warning indicators (world-space cubes above camera)
        softWarn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        softWarn.name = "SoftWarning";
        softWarn.transform.position = new Vector3(0, 4, 3);
        softWarn.transform.localScale = new Vector3(2, 0.3f, 0.3f);
        softWarn.GetComponent<Renderer>().material.color = Color.yellow;
        Destroy(softWarn.GetComponent<Collider>());
        softWarn.SetActive(false);

        hardWarn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hardWarn.name = "HardWarning";
        hardWarn.transform.position = new Vector3(0, 4.5f, 3);
        hardWarn.transform.localScale = new Vector3(2, 0.3f, 0.3f);
        hardWarn.GetComponent<Renderer>().material.color = Color.red;
        Destroy(hardWarn.GetComponent<Collider>());
        hardWarn.SetActive(false);
    }

    public static void SetState(string state)
    {
        if (instance == null) return;
        switch (state)
        {
            case "REGULAR":
                instance.softWarn.SetActive(false);
                instance.hardWarn.SetActive(false);
                break;
            case "SLIGHTLY": case "HIGHLY":
                instance.softWarn.SetActive(true);
                instance.hardWarn.SetActive(false);
                break;
            case "BLOCKED":
                instance.softWarn.SetActive(false);
                instance.hardWarn.SetActive(true);
                break;
        }
    }
}

// ═══ ODataSvc — pure C# resource color lookups (OreDataService) ═══
public class ODataSvc
{
    static readonly string[] names = { "Iron", "Gold", "Copper" };
    static readonly string[] hexColors = { "B3B3B3", "FFD700", "B87333" };

    public string GetColoredString(int type) =>
        $"<color=#{hexColors[type % hexColors.Length]}>{names[type % names.Length]}</color>";
}

// ═══ OBasket — trigger tracking (BaseBasket) ═══
public class OBasket : MonoBehaviour
{
    List<OPiece> inside = new List<OPiece>();

    void OnTriggerEnter(Collider other)
    {
        var ore = other.GetComponent<OPiece>();
        if (ore != null && !inside.Contains(ore))
        {
            inside.Add(ore);
            ore.baskets.Add(this);
            Debug.Log($"[Ore] Basket: +1 ({inside.Count} inside)");
        }
    }
    void OnTriggerExit(Collider other)
    {
        var ore = other.GetComponent<OPiece>();
        if (ore != null)
        {
            inside.Remove(ore);
            ore.baskets.Remove(this);
            Debug.Log($"[Ore] Basket: -1 ({inside.Count} inside)");
        }
    }
    void OnDisable()
    {
        foreach (var o in inside) o.baskets.Remove(this);
        inside.Clear();
    }
    public int GetCount() => inside.Count;
    public void Remove(OPiece o) => inside.Remove(o);
}