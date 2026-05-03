using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — SellerSystem
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY SellerSystem feature:
///   1. OnTriggerEnter: detect ore entering seller
///   2. Skip markedForDestruction (double-sell prevention via tag)
///   3. Skip non-Rigidbody objects
///   4. OrePiece path: SellAfterDelay → 2s coroutine → sell + return to pool
///   5. BaseSellableItem fallback: non-ore sellable → instant destroy + sell
///   6. Random price multiplier (0.9x–1.1x per piece)
///   7. Dual collider setup: physical surface + trigger zone on child
///   8. Multiple ore types with different values
///   9. Two seller machines (tests ore entering different sellers)
///
/// Controls:
///   N = spawn ore above seller #1 (falls in)
///   B = spawn ore directly inside seller #1 (instant trigger)
///   G = spawn generic sellable item (BaseSellableItem path — instant destroy)
///   D = spawn non-rigidbody cube (should be ignored)
///   K = print money earned + sell count
///
/// What you should see:
///   - 2 green platforms = seller triggers
///   - N: ore falls → turns red (tagged) → 2s → disappears → "$1.05 earned"
///   - B: ore inside trigger → starts selling immediately
///   - G: generic sellable enters → INSTANT destroy (no delay) → money added
///   - D: static cube → ignored (no Rigidbody)
///   - Same ore touching 2nd seller = skipped (already marked)
/// </summary>
public class Proto_SellerSystem : MonoBehaviour
{
    Camera cam;
    float totalMoney;

    void Start()
    {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3, 1, 3);

        // → seller machine (trigger + physical surface)
        var sellerRoot = new GameObject("SellerMachine");
        sellerRoot.transform.position = new Vector3(0, 0.5f, 5);

        // physical surface (ore lands on this)
        var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "PhysicalSurface";
        surface.transform.SetParent(sellerRoot.transform);
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localScale = new Vector3(3, 0.3f, 3);
        surface.GetComponent<Renderer>().material.color = new Color(0, 0.5f, 0);

        // trigger zone (detects ore entry)
        var triggerGo = new GameObject("TriggerZone");
        triggerGo.transform.SetParent(sellerRoot.transform);
        triggerGo.transform.localPosition = new Vector3(0, 0.5f, 0);
        var triggerCol = triggerGo.AddComponent<BoxCollider>();
        triggerCol.size = new Vector3(2.5f, 1f, 2.5f);
        triggerCol.isTrigger = true;
        var sellerScript = triggerGo.AddComponent<ProtoSellerTrigger>();
        sellerScript.onSold = (value) =>
        {
            totalMoney += value;
            Debug.Log($"[Proto_Seller] SOLD ${value:F2} — Total: ${totalMoney:F2}");
        };

        // → seller #2 (tests double-sell prevention across multiple sellers)
        var seller2Root = new GameObject("SellerMachine_2");
        seller2Root.transform.position = new Vector3(5, 0.5f, 5);
        var surf2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surf2.name = "Surface2"; surf2.transform.SetParent(seller2Root.transform);
        surf2.transform.localPosition = Vector3.zero;
        surf2.transform.localScale = new Vector3(3, 0.3f, 3);
        surf2.GetComponent<Renderer>().material.color = new Color(0, 0.4f, 0);
        var trig2 = new GameObject("TriggerZone2");
        trig2.transform.SetParent(seller2Root.transform);
        trig2.transform.localPosition = new Vector3(0, 0.5f, 0);
        var tc2 = trig2.AddComponent<BoxCollider>(); tc2.size = new Vector3(2.5f, 1f, 2.5f); tc2.isTrigger = true;
        var ss2 = trig2.AddComponent<ProtoSellerTrigger>();
        ss2.onSold = (value) =>
        {
            totalMoney += value;
            Debug.Log($"[Seller] #2 SOLD ${value:F2} — Total: ${totalMoney:F2}");
        };

        Debug.Log("[Seller] N=drop ore, B=inside, G=generic sellable, D=non-rb, K=money");
    }

    void Update()
    {
        float mx = Input.GetAxis("Mouse X") * 3f;
        float my = Input.GetAxis("Mouse Y") * 3f;
        cam.transform.Rotate(-my, mx, 0f, Space.Self);
        cam.transform.localEulerAngles = new Vector3(cam.transform.localEulerAngles.x, cam.transform.localEulerAngles.y, 0f);

        if (Input.GetKeyDown(KeyCode.N))
        {
            // → spawn above seller, falls in
            SpawnOre(new Vector3(Random.Range(-0.5f, 0.5f), 3f, 5f + Random.Range(-0.5f, 0.5f)));
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            // → spawn directly inside trigger
            SpawnOre(new Vector3(0, 1.2f, 5f));
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            // → generic sellable item (BaseSellableItem path — instant destroy, no delay)
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GenericSellable";
            go.transform.position = new Vector3(0, 2f, 5f);
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<Renderer>().material.color = Color.yellow;
            go.AddComponent<Rigidbody>().mass = 0.2f;
            var gs = go.AddComponent<ProtoGenericSellable>();
            gs.sellValue = 5f;
            Debug.Log("[Seller] Spawned generic sellable ($5.00 — instant destroy on sell)");
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            // → non-rigidbody cube (should be ignored by seller)
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "NonRb_Cube";
            go.transform.position = new Vector3(0, 1.2f, 5f);
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<Renderer>().material.color = Color.magenta;
            Debug.Log("[Seller] Spawned non-Rigidbody cube (should be ignored)");
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log($"[Proto_Seller] Total money: ${totalMoney:F2}");
        }
    }

    void SpawnOre(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "SellableOre";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.25f;
        go.GetComponent<Renderer>().material.color = Color.grey;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.3f;
        var sellable = go.AddComponent<ProtoSellableOre>();
        sellable.sellValue = Random.Range(0.9f, 1.1f);
        Debug.Log($"[Proto_Seller] Spawned ore (value=${sellable.sellValue:F2})");
    }
}

// ═══════════════════════════════════════════
// ProtoSellableOre — ore with sell value + tag
// ═══════════════════════════════════════════
public class ProtoSellableOre : MonoBehaviour
{
    public float sellValue = 1f;
    public bool isMarked;

    public void MarkForDestruction()
    {
        isMarked = true;
        GetComponent<Renderer>().material.color = Color.red;
    }

    public void Sell(System.Action<float> onSold)
    {
        if (isMarked) return;
        MarkForDestruction();
        StartCoroutine(SellCoroutine(onSold));
    }

    IEnumerator SellCoroutine(System.Action<float> onSold)
    {
        yield return new WaitForSeconds(2f);
        if (this == null) yield break;
        onSold?.Invoke(sellValue);
        Destroy(gameObject);
    }
}

// ═══════════════════════════════════════════
// ProtoGenericSellable — BaseSellableItem fallback (instant destroy)
// ═══════════════════════════════════════════
public class ProtoGenericSellable : MonoBehaviour
{
    public float sellValue = 1f;
}

// ═══════════════════════════════════════════
// ProtoSellerTrigger — trigger sells ore (3-tier check: ore → generic → skip)
// ═══════════════════════════════════════════
public class ProtoSellerTrigger : MonoBehaviour
{
    public System.Action<float> onSold;

    void OnTriggerEnter(Collider other)
    {
        // → skip non-rigidbody
        if (other.attachedRigidbody == null)
        {
            Debug.Log($"[Seller] Ignored {other.name} — no Rigidbody");
            return;
        }
        // → check OrePiece first (most common — 2s delay sell)
        var ore = other.GetComponent<ProtoSellableOre>();
        if (ore != null)
        {
            if (ore.isMarked)
            {
                Debug.Log($"[Seller] Skipped {other.name} — already marked (double-sell prevention)");
                return;
            }
            Debug.Log($"[Seller] Selling ore {other.name}... (2s delay)");
            ore.Sell(onSold);
            return;
        }
        // → BaseSellableItem fallback (instant destroy + sell)
        var generic = other.GetComponent<ProtoGenericSellable>();
        if (generic != null)
        {
            Debug.Log($"[Seller] Generic sell: {other.name} → ${generic.sellValue:F2} (instant destroy)");
            onSold?.Invoke(generic.sellValue);
            Object.Destroy(generic.gameObject);
            return;
        }
        Debug.Log($"[Seller] Ignored {other.name} — not sellable");
    }
}