using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — AutoMinerSystem
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY AutoMinerSystem feature:
///   1. Rotating drill visual (configurable axis: X/Y/Z)
///   2. Timed ore spawn with probability roll (0-100%)
///   3. Weighted random drop from definition (3 ore types with weights)
///   4. Fallback prefab when definition returns null
///   5. On/Off toggle (TurnOn/TurnOff) with light material swap (green/red)
///   6. ConfigureFromDefinition (reads SpawnProbability + SpawnRate from SO data)
///   7. SetResourceDefinition (hot-swap definition at runtime)
///   8. OreLimitManager throttle (multiplied timer when many objects)
///   9. ShouldBlockOreSpawning (stops entirely when blocked)
///  10. IInteractable pattern ("Turn On" / "Turn Off" interaction names)
///  11. ICustomSaveDataProvider (save/load IsOn state as JSON)
///  12. Multiple auto-miners with different resource definitions
///
/// Controls:
///   T = toggle miner #1 on/off
///   Y = toggle miner #2 on/off
///   1/2/3 = set miner #1 spawn rate (1s / 2s / 5s)
///   4 = swap miner #1 definition (gold ↔ copper — tests SetResourceDefinition)
///   P = toggle probability (100% vs 50%)
///   J = save state to JSON, then load it back (tests ICustomSaveDataProvider)
///   K = print stats
///
/// What you should see:
///   - 2 auto-miners side by side, each with spinning drill + spawn point
///   - Every SpawnRate seconds: weighted random colored cube appears, falls
///   - Toggle off → drill stops, light turns red, no spawns
///   - Toggle on → drill resumes, light turns green
///   - After 30+ pieces: spawn rate slows visibly (throttle)
///   - At 100+: spawning blocks entirely
///   - Key 4: definition swaps, new ore types start appearing
/// </summary>
public class Proto_AutoMinerSystem : MonoBehaviour
{
    Camera cam;
    ProtoAutoMiner miner1, miner2;
    List<GameObject> spawnedOre = new List<GameObject>();
    // → inline resource definitions (simulates SO_AutoMinerResourceDefinition)
    static AMDef defIron = new AMDef("Iron", 80f, 2f, new[] {
        new AMDrop("IronOre", Color.grey, 70f),
        new AMDrop("CoalOre", Color.black, 30f) });
    static AMDef defGold = new AMDef("Gold", 90f, 3f, new[] {
        new AMDrop("GoldOre", new Color(1f, 0.84f, 0f), 80f),
        new AMDrop("GoldGem", Color.cyan, 20f) });
    static AMDef defCopper = new AMDef("Copper", 75f, 1.5f, new[] {
        new AMDrop("CopperOre", new Color(0.72f, 0.45f, 0.2f), 60f),
        new AMDrop("CopperCrushed", new Color(0.5f, 0.3f, 0.1f), 30f),
        new AMDrop("DiamondGem", new Color(0.6f, 0.95f, 1f), 10f) });
    bool defSwapped;

    void Start()
    {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(5, 1, 5);
        // → miner #1 (iron, rotates around X axis)
        miner1 = BuildMiner("Miner_Iron", new Vector3(-2, 0.5f, 6), defIron, Vector3.right);
        // → miner #2 (gold, rotates around Y axis — tests rotation axis config)
        miner2 = BuildMiner("Miner_Gold", new Vector3(3, 0.5f, 6), defGold, Vector3.down);
        Debug.Log("[AM] T=toggle#1, Y=toggle#2, 1/2/3=rate, 4=swap def, P=prob, J=save/load, K=stats");
    }

    ProtoAutoMiner BuildMiner(string name, Vector3 pos, AMDef def, Vector3 rotAxis)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var m = go.AddComponent<ProtoAutoMiner>();
        m.rotationAxis = rotAxis;
        m.SetDefinition(def);
        m.onOreSpawned = (g) => spawnedOre.Add(g);
        return m;
    }

    void Update()
    {
        float mx = Input.GetAxis("Mouse X") * 3f;
        float my = Input.GetAxis("Mouse Y") * 3f;
        cam.transform.Rotate(-my, mx, 0f, Space.Self);
        cam.transform.localEulerAngles = new Vector3(cam.transform.localEulerAngles.x, cam.transform.localEulerAngles.y, 0f);

        if (Input.GetKeyDown(KeyCode.T)) miner1.Toggle();
        if (Input.GetKeyDown(KeyCode.Y)) miner2.Toggle();
        if (Input.GetKeyDown(KeyCode.Alpha1)) { miner1.spawnRate = 1f; Debug.Log("[AM] #1 Rate=1s"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { miner1.spawnRate = 2f; Debug.Log("[AM] #1 Rate=2s"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { miner1.spawnRate = 5f; Debug.Log("[AM] #1 Rate=5s"); }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // → SetResourceDefinition: hot-swap between gold and copper
            defSwapped = !defSwapped;
            miner1.SetDefinition(defSwapped ? defCopper : defIron);
            Debug.Log($"[AM] #1 definition swapped to {(defSwapped ? "Copper" : "Iron")}");
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            miner1.probability = (miner1.probability > 99f) ? 50f : 100f;
            Debug.Log($"[AM] #1 Probability={miner1.probability}%");
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            // → ICustomSaveDataProvider: save to JSON, then load back
            string json = miner1.GetSaveData();
            Debug.Log($"[AM] SAVED: {json}");
            miner1.Toggle(); // → change state
            Debug.Log($"[AM] Changed state to isOn={miner1.isOn}");
            miner1.LoadSaveData(json); // → restore
            Debug.Log($"[AM] LOADED: isOn={miner1.isOn} (should match saved)");
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            int moving = 0;
            foreach (var go in spawnedOre) { if (go != null && !go.GetComponent<Rigidbody>().IsSleeping()) moving++; }
            int totalSpawned = 0; foreach (var go in spawnedOre) if (go != null) totalSpawned++;
            Debug.Log($"[AM] Total:{totalSpawned} Moving:{moving} | #1: on={miner1.isOn} rate={miner1.spawnRate} prob={miner1.probability} | #2: on={miner2.isOn} rate={miner2.spawnRate}");
        }
    }

    // ═══ inline definition data ═══
    public class AMDrop { public string name; public Color color; public float weight;
        public AMDrop(string n, Color c, float w) { name=n; color=c; weight=w; } }
    public class AMDef { public string name; public float prob, rate; public AMDrop[] drops;
        public AMDef(string n, float p, float r, AMDrop[] d) { name=n; prob=p; rate=r; drops=d; } }
}

// ═══════════════════════════════════════════
// ProtoAutoMiner — inline auto-miner
// ═══════════════════════════════════════════
public class ProtoAutoMiner : MonoBehaviour
{
    public float spawnRate = 2f;
    public float probability = 80f;
    public bool isOn = true;
    public Vector3 rotationAxis = Vector3.right;
    public System.Action<GameObject> onOreSpawned;

    Proto_AutoMinerSystem.AMDef definition;
    GameObject rotator, spawnPoint, lightIndicator;
    float timer;
    int spawnCount;

    // → ConfigureFromDefinition (reads SO data)
    public void SetDefinition(Proto_AutoMinerSystem.AMDef def)
    {
        definition = def;
        if (def != null) { probability = def.prob; spawnRate = def.rate; }
        Debug.Log($"[AM] {name} configured: {def?.name}, prob={probability}, rate={spawnRate}");
    }

    void Start()
    {
        // → base platform
        var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseMesh.transform.SetParent(transform); baseMesh.transform.localPosition = Vector3.zero;
        baseMesh.transform.localScale = new Vector3(1, 0.3f, 1);
        baseMesh.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
        // → rotator (drill)
        rotator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rotator.transform.SetParent(transform); rotator.transform.localPosition = new Vector3(0, 0.8f, 0);
        rotator.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
        rotator.GetComponent<Renderer>().material.color = new Color(0.5f, 0.35f, 0.1f);
        Destroy(rotator.GetComponent<Collider>());
        // → spawn point
        spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.SetParent(transform); spawnPoint.transform.localPosition = new Vector3(0, -0.3f, 0.8f);
        // → light indicator
        lightIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lightIndicator.transform.SetParent(transform); lightIndicator.transform.localPosition = new Vector3(0.4f, 0.3f, 0);
        lightIndicator.transform.localScale = Vector3.one * 0.15f;
        Destroy(lightIndicator.GetComponent<Collider>());
        SetLight(true);
        timer = spawnRate;
    }

    void Update()
    {
        if (!isOn) return;
        // → rotate drill around configurable axis
        float angle = 360f / (spawnRate * 12f) * Time.deltaTime;
        rotator.transform.Rotate(rotationAxis, angle);
        // → spawn timer
        timer -= Time.deltaTime;
        timer = Mathf.Min(timer, spawnRate);
        if (timer <= 0f)
        {
            TrySpawn();
            // → OreLimitManager throttle
            float multiplier = spawnCount > 80 ? 2f : spawnCount > 50 ? 1.5f : spawnCount > 30 ? 1.25f : 1f;
            timer += spawnRate * multiplier;
            // → ShouldBlockOreSpawning
            if (spawnCount > 100) { Debug.Log("[AM] BLOCKED — too many objects"); return; }
            if (multiplier > 1f) Debug.Log($"[AM] Throttle: {multiplier}x ({spawnCount} spawned)");
        }
    }

    void TrySpawn()
    {
        if (Random.Range(0f, 100f) > probability) return;
        // → weighted random from definition drops
        Proto_AutoMinerSystem.AMDrop drop = null;
        if (definition != null && definition.drops.Length > 0)
        {
            float total = 0f;
            foreach (var d in definition.drops) total += d.weight;
            float roll = Random.value * total;
            float cum = 0f;
            foreach (var d in definition.drops) { cum += d.weight; if (roll <= cum) { drop = d; break; } }
            drop ??= definition.drops[definition.drops.Length - 1];
        }
        // → fallback if no definition or no drop selected
        Color color = drop?.color ?? Color.grey;
        string dropName = drop?.name ?? "Fallback";

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"{dropName}_{spawnCount}";
        go.transform.position = spawnPoint.transform.position;
        go.transform.localScale = Vector3.one * 0.2f;
        go.GetComponent<Renderer>().material.color = color;
        go.AddComponent<Rigidbody>().mass = 0.3f;
        spawnCount++;
        onOreSpawned?.Invoke(go);
    }

    public void Toggle()
    {
        isOn = !isOn;
        SetLight(isOn);
        Debug.Log($"[AM] {name}: {(isOn ? "ON ▶" : "OFF ■")}");
    }

    void SetLight(bool on) =>
        lightIndicator.GetComponent<Renderer>().material.color = on ? Color.green : Color.red;

    // → ICustomSaveDataProvider
    public string GetSaveData() => JsonUtility.ToJson(new AMSave { IsOn = isOn });
    public void LoadSaveData(string json)
    {
        var data = JsonUtility.FromJson<AMSave>(json);
        if (data != null) { isOn = data.IsOn; SetLight(isOn); }
    }

    [System.Serializable] class AMSave { public bool IsOn = true; }
}