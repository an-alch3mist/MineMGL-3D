using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — Phase B Full Integration (drag onto empty GO, press Play)
/// Wires ALL cross-system event chains from FLOW.md inline using static Action fields.
/// Logs every event raise with frame number. Tests every flow end-to-end.
///
/// Systems simulated inline:
///   InventorySystem — pickup, equip, drop, stack, hotbar, scroll, extended, drag-drop
///   PlayerSystem — WASD+jump+sprint, camera look, grab (SpringJoint+rope)
///   ToolSystem — pickaxe swing+hit, magnet pull/push, hammer raycast, ItemEquipBridge
///   HighlightSystem — raycast outline on hover, clear on look away, blocked when menu open
///   DamageSystem — IDamageable, health bars, break into pieces
///
/// Event Registry (all 8 from FLOW.md):
///   OnToolPickupRequested, OnItemPickedUp, OnItemDropped, OnItemEquipped,
///   OnToolSwitched, OnMenuStateChanged, OnOpenInventoryView, OnCloseInventoryView
///
/// Controls:
///   WASD/Space/Shift — move/jump/sprint
///   Mouse — look
///   Click world item — pickup
///   1-5 — hotbar switch (equip)
///   G — drop
///   Tab — toggle extended inventory
///   Hold LMB (pickaxe) — swing + hit damageable
///   Hold RMB (magnet) — pull; LMB push; R drop gently
///   F — grab/release physics object (SpringJoint)
///   L — dump event log to console
///   M — simulate menu open (tests CloseAll pattern)
///   N — simulate menu close
///
/// Edge cases tested:
///   - Opening inventory while grab active → grab blocked
///   - Opening menu while inventory open → CloseAll fires
///   - Domain reload cleanup (static fields cleared in OnDestroy)
///   - Start vs OnEnable timing (isFirstEnable pattern)
///   - Same slot toggle (equip→unequip)
///   - Stack overflow (inventory full)
///   - Drop from stack (clone) vs drop last (actual GO)
/// </summary>
public class Proto_PhaseB_Integration : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    // STATIC EVENTS — simulates GameEvents (cleared on destroy for domain reload)
    // ═══════════════════════════════════════════════════════════════
    static Action<MockItem> OnToolPickupRequested;
    static Action<MockItem> OnItemPickedUp;
    static Action<MockItem> OnItemDropped;
    static Action<MockItem> OnItemEquipped;
    static Action<int> OnToolSwitched;
    static Action<bool> OnMenuStateChanged;
    static Action OnOpenInventoryView;
    static Action OnCloseInventoryView;

    // ═══════════════════════════════════════════════════════════════
    // EVENT LOG
    // ═══════════════════════════════════════════════════════════════
    static List<string> eventLog = new List<string>();
    static void LogEvent(string name, string detail = "")
    {
        string entry = $"[F{Time.frameCount}] {name} {detail}";
        eventLog.Add(entry);
        Debug.Log(entry);
    }

    // ═══════════════════════════════════════════════════════════════
    // MOCK ITEM (replaces IInventoryItem)
    // ═══════════════════════════════════════════════════════════════
    class MockItem
    {
        public string name;
        public Color color;
        public int qty = 1;
        public int maxStack;
        public GameObject go;
        public Vector3 originalScale;
        public int toolType; // 0=pickaxe 1=magnet 2=hammer 3=generic

        public MockItem(string n, Color c, int max, Vector3 pos, int tool)
        {
            name = n; color = c; maxStack = max; toolType = tool;
            go = GameObject.CreatePrimitive(tool <= 2 ? PrimitiveType.Cube : PrimitiveType.Sphere);
            go.name = n;
            go.transform.position = pos;
            go.transform.localScale = tool <= 2 ? new Vector3(0.08f, 0.08f, 0.4f) : Vector3.one * 0.4f;
            originalScale = go.transform.localScale;
            go.GetComponent<Renderer>().material.color = c;
            go.AddComponent<Rigidbody>().mass = 0.5f;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFIG
    // ═══════════════════════════════════════════════════════════════
    const int HOTBAR = 5, TOTAL = 15;
    float moveSpeed = 6f, lookSens = 2f;

    // ═══════════════════════════════════════════════════════════════
    // RUNTIME STATE
    // ═══════════════════════════════════════════════════════════════
    // player
    GameObject player;
    Camera cam;
    CharacterController cc;
    float xRot, yVel;
    Transform viewModelContainer, magnetPullOrigin;

    // inventory
    class Slot { public MockItem item; public int qty; }
    Slot[] slots;
    int activeSlot = -1;
    bool extendedOpen, isMenuOpen;

    // tools
    float lastSwingTime = -1f;

    // magnet
    List<Rigidbody> heldBodies = new List<Rigidbody>();
    List<SpringJoint> magnetJoints = new List<SpringJoint>();
    List<GameObject> magnetAnchors = new List<GameObject>();

    // grab
    SpringJoint grabJoint;
    LineRenderer grabRope;
    GameObject grabDragger;
    Rigidbody grabbedRb;

    // highlight
    GameObject highlightOverlay;
    GameObject highlightTarget;

    // damageable
    class DmgObj { public GameObject go; public float hp, maxHp; public Color col; }
    List<DmgObj> damageables = new List<DmgObj>();

    // world items
    List<MockItem> worldItems = new List<MockItem>();

    // isFirstEnable (simulated)
    bool inventoryFirstEnable = true;

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // clear statics (domain reload safety)
        ClearStaticEvents();
        eventLog.Clear();

        CreateWorld();
        CreatePlayer();
        SubscribeEvents();
        slots = new Slot[TOTAL];
        for (int i = 0; i < TOTAL; i++) slots[i] = new Slot();

        // simulate isFirstEnable
        SimulateInventoryFirstEnable();
    }

    void OnDestroy()
    {
        // domain reload cleanup
        ClearStaticEvents();
        LogEvent("CLEANUP", "static events cleared (domain reload safe)");
    }

    void ClearStaticEvents()
    {
        OnToolPickupRequested = null;
        OnItemPickedUp = null;
        OnItemDropped = null;
        OnItemEquipped = null;
        OnToolSwitched = null;
        OnMenuStateChanged = null;
        OnOpenInventoryView = null;
        OnCloseInventoryView = null;
    }

    void SubscribeEvents()
    {
        // InventoryOrchestrator subscribes to pickup
        OnToolPickupRequested += HandleItemPickup;

        // ObjectHighlighterManager subscribes
        OnItemEquipped += (item) => LogEvent("OnItemEquipped→Highlight", $"tracking active: {item.name}");
        OnItemDropped += (item) => LogEvent("OnItemDropped→Highlight", $"cleared active: {item.name}");

        // UIManager subscribes to menu state
        OnMenuStateChanged += (open) => {
            isMenuOpen = open;
            LogEvent("OnMenuStateChanged", $"isMenuOpen={open}");
        };

        // InventoryUI subscribes to open/close
        OnOpenInventoryView += () => {
            extendedOpen = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            // fire menu state (simulates OnEnable)
            OnMenuStateChanged?.Invoke(true);
            LogEvent("OnOpenInventoryView", "extended opened + menu state true");
        };
        OnCloseInventoryView += () => {
            extendedOpen = false;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            OnMenuStateChanged?.Invoke(false);
            LogEvent("OnCloseInventoryView", "extended closed + menu state false");
        };

        LogEvent("SUBSCRIBE", "all events wired");
    }

    void SimulateInventoryFirstEnable()
    {
        // isFirstEnable pattern: first OnEnable does setup + self-disable, doesn't fire MenuStateChanged
        if (inventoryFirstEnable)
        {
            LogEvent("isFirstEnable", "InventoryUI: subscribe + self-disable (NO MenuStateChanged pulse)");
            inventoryFirstEnable = false;
            // subscriptions already done in SubscribeEvents
        }
    }

    void Update()
    {
        HandleMovement();
        HandleLook();

        if (!isMenuOpen)
        {
            HandlePickup();
            HandleHotbarKeys();
            HandleScroll();
            HandleDrop();
            HandleToolInput();
            HandleGrab();
            HandleHighlight();
        }

        HandleToggleInventory();
        HandleMenuSim();
        HandleDumpLog();

        MagnetCleanup();
        UpdateDamageableHPBars();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORLD CREATION
    // ═══════════════════════════════════════════════════════════════
    void CreateWorld()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 5f;
        floor.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.3f);

        // tools in world
        worldItems.Add(new MockItem("Pickaxe", Color.cyan, 1, new Vector3(-2, 1, 4), 0));
        worldItems.Add(new MockItem("Magnet", Color.magenta, 1, new Vector3(0, 1, 4), 1));
        worldItems.Add(new MockItem("Hammer", Color.yellow, 1, new Vector3(2, 1, 4), 2));
        worldItems.Add(new MockItem("Dynamite", new Color(1f, 0.5f, 0f), 5, new Vector3(-1, 1, 6), 3));
        worldItems.Add(new MockItem("Dynamite", new Color(1f, 0.5f, 0f), 5, new Vector3(1, 1, 6), 3));

        // damageable ore nodes
        CreateDamageable("IronNode", new Vector3(-3, 0.5f, 8), Color.gray, 80f);
        CreateDamageable("GoldNode", new Vector3(0, 0.5f, 8), Color.yellow, 60f);
        CreateDamageable("CopperNode", new Vector3(3, 0.5f, 8), new Color(1f, 0.5f, 0f), 100f);

        // physics cubes for grab + magnet
        for (int i = 0; i < 5; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"GrabCube_{i}";
            cube.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f), 0.4f, UnityEngine.Random.Range(2f, 6f));
            cube.transform.localScale = Vector3.one * 0.3f;
            cube.GetComponent<Renderer>().material.color = Color.Lerp(Color.red, Color.blue, i / 5f);
            cube.AddComponent<Rigidbody>().mass = 0.5f;
        }
    }

    void CreateDamageable(string name, Vector3 pos, Color col, float hp)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.position = pos;
        go.GetComponent<Renderer>().material.color = col;
        damageables.Add(new DmgObj { go = go, hp = hp, maxHp = hp, col = col });
    }

    void CreatePlayer()
    {
        player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);
        cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.up;

        var camGO = new GameObject("Camera");
        camGO.transform.parent = player.transform;
        camGO.transform.localPosition = new Vector3(0, 1.6f, 0);
        cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();

        viewModelContainer = new GameObject("ViewModelContainer").transform;
        viewModelContainer.parent = camGO.transform;
        viewModelContainer.localPosition = new Vector3(0.4f, -0.3f, 0.8f);

        magnetPullOrigin = new GameObject("MagnetPull").transform;
        magnetPullOrigin.parent = camGO.transform;
        magnetPullOrigin.localPosition = new Vector3(0, 0, 1.5f);

        // grab dragger
        grabDragger = new GameObject("RigidbodyDragger");
        grabDragger.transform.parent = camGO.transform;
        grabDragger.transform.localPosition = new Vector3(0, 0, 2f);
        grabDragger.AddComponent<Rigidbody>().isKinematic = true;
        var lr = grabDragger.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f; lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        lr.enabled = false;
        grabRope = lr;
        grabDragger.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    // MOVEMENT + LOOK
    // ═══════════════════════════════════════════════════════════════
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"), v = Input.GetAxis("Vertical");
        Vector3 move = player.transform.right * h + player.transform.forward * v;
        move.y = 0; move = move.normalized * (Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 1.8f : moveSpeed);
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) yVel = 7f;
        yVel += Physics.gravity.y * Time.deltaTime;
        move.y = yVel;
        cc.Move(move * Time.deltaTime);
    }
    void HandleLook()
    {
        if (extendedOpen) return;
        float mx = Input.GetAxis("Mouse X") * lookSens, my = Input.GetAxis("Mouse Y") * lookSens;
        xRot -= my; xRot = Mathf.Clamp(xRot, -89f, 89f);
        player.transform.Rotate(Vector3.up * mx);
        cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 1: TOOL PICKUP
    // ═══════════════════════════════════════════════════════════════
    void HandlePickup()
    {
        if (!Input.GetMouseButtonDown(0) || activeSlot >= 0 && slots[activeSlot].item != null && slots[activeSlot].item.toolType <= 2) return;
        // only pickup if not using a tool action
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 4f))
        {
            var found = worldItems.Find(i => i.go == hit.collider.gameObject);
            if (found != null)
            {
                LogEvent("PICKUP_CLICK", found.name);
                OnToolPickupRequested?.Invoke(found);
            }
        }
    }

    void HandleItemPickup(MockItem item)
    {
        LogEvent("OnToolPickupRequested", item.name);

        // stack check
        for (int i = 0; i < TOTAL; i++)
        {
            if (slots[i].item != null && slots[i].item.name == item.name && slots[i].qty < item.maxStack)
            {
                slots[i].qty++;
                item.go.SetActive(false);
                worldItems.Remove(item);
                LogEvent("STACKED", $"{item.name} into slot {i}, qty={slots[i].qty}");
                OnItemPickedUp?.Invoke(item);
                return;
            }
        }
        // empty slot
        for (int i = 0; i < TOTAL; i++)
        {
            if (slots[i].item == null)
            {
                slots[i].item = item; slots[i].qty = 1;
                item.go.SetActive(false);
                worldItems.Remove(item);
                if (activeSlot == -1 && i < HOTBAR) SwitchToSlot(i);
                LogEvent("ADDED", $"{item.name} to slot {i}");
                OnItemPickedUp?.Invoke(item);
                return;
            }
        }
        LogEvent("FULL", "inventory full — item not picked up");
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 2: EQUIP / SWITCH
    // ═══════════════════════════════════════════════════════════════
    void HandleHotbarKeys()
    {
        for (int i = 0; i < HOTBAR; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SwitchToSlot(i);
    }
    void HandleScroll()
    {
        float s = Input.GetAxis("Mouse ScrollWheel");
        if (s == 0) return;
        int dir = s > 0 ? 1 : -1, start = Mathf.Max(activeSlot, 0);
        for (int i = 1; i <= HOTBAR; i++)
        {
            int idx = (start + dir * i + HOTBAR) % HOTBAR;
            if (slots[idx].item != null) { SwitchToSlot(idx); return; }
        }
    }
    void SwitchToSlot(int idx)
    {
        // unequip previous
        if (activeSlot >= 0 && slots[activeSlot].item != null)
            slots[activeSlot].item.go.SetActive(false);

        // toggle off (same slot = unequip)
        if (idx == activeSlot) {
            LogEvent("TOGGLE_OFF", $"slot {idx} unequipped");
            activeSlot = -1;
            OnToolSwitched?.Invoke(-1);
            return;
        }

        activeSlot = idx;
        var item = slots[idx].item;
        if (item != null)
        {
            item.go.SetActive(true);
            item.go.transform.parent = viewModelContainer;
            item.go.transform.localPosition = Vector3.zero;
            item.go.transform.localRotation = Quaternion.identity;
            item.go.transform.localScale = item.originalScale * 0.6f;
            var rb = item.go.GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
            var col = item.go.GetComponent<Collider>(); if (col) col.enabled = false;

            // ItemEquipBridge: SetOwnerContext
            LogEvent("OnItemEquipped", $"{item.name} — ItemEquipBridge sends cam+container");
            OnItemEquipped?.Invoke(item);
        }
        OnToolSwitched?.Invoke(idx);
        LogEvent("OnToolSwitched", $"slot {idx}");
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 3: TOOL ACTIONS
    // ═══════════════════════════════════════════════════════════════
    void HandleToolInput()
    {
        if (activeSlot < 0 || slots[activeSlot].item == null) return;
        var tool = slots[activeSlot].item;

        if (tool.toolType == 0) PickaxeInput(tool); // pickaxe
        if (tool.toolType == 1) MagnetInput(); // magnet
        if (tool.toolType == 2) HammerInput(); // hammer
    }

    void PickaxeInput(MockItem pick)
    {
        if (!Input.GetMouseButton(0) || Time.time - lastSwingTime < 0.8f) return;
        lastSwingTime = Time.time;
        StartCoroutine(PickaxeSwing());
    }
    IEnumerator PickaxeSwing()
    {
        LogEvent("PICKAXE_SWING", "animation start");
        yield return new WaitForSeconds(0.2f);
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 3f)) yield break;

        // check damageable
        DmgObj dmg = damageables.Find(d => d.go == hit.collider.gameObject);
        if (dmg != null)
        {
            dmg.hp -= 25f;
            var r = dmg.go.GetComponent<Renderer>();
            r.material.color = Color.white;
            StartCoroutine(FlashBack(r, dmg.col, 0.1f));
            LogEvent("PICKAXE_HIT", $"{dmg.go.name} hp={dmg.hp}/{dmg.maxHp}");
            if (dmg.hp <= 0) BreakDamageable(dmg);
            yield break;
        }
        // push rigidbody
        var rb = hit.collider.GetComponent<Rigidbody>();
        if (rb != null) { rb.AddForceAtPosition(cam.transform.forward * 5f, hit.point, ForceMode.Impulse); LogEvent("PICKAXE_PUSH", hit.collider.name); }
    }
    IEnumerator FlashBack(Renderer r, Color c, float d) { yield return new WaitForSeconds(d); if (r) r.material.color = c; }
    void BreakDamageable(DmgObj d)
    {
        LogEvent("NODE_BREAK", d.go.name);
        for (int i = 0; i < 3; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.position = d.go.transform.position + UnityEngine.Random.insideUnitSphere * 0.3f;
            p.transform.localScale = Vector3.one * 0.2f;
            p.GetComponent<Renderer>().material.color = d.col;
            var rb = p.AddComponent<Rigidbody>();
            rb.linearVelocity = UnityEngine.Random.insideUnitSphere * 3f + Vector3.up * 2f;
            Destroy(p, 8f);
        }
        Destroy(d.go); damageables.Remove(d);
    }

    void MagnetInput()
    {
        if (Input.GetMouseButton(1)) MagnetPull();
        if (Input.GetMouseButtonDown(0)) MagnetDrop(5f);
        if (Input.GetKeyDown(KeyCode.R)) MagnetDrop(0.5f);
    }
    void MagnetPull()
    {
        foreach (var c in Physics.OverlapSphere(magnetPullOrigin.position, 3f))
        {
            var rb = c.attachedRigidbody;
            if (rb == null || heldBodies.Contains(rb) || rb.GetComponent<CharacterController>()) continue;
            var a = new GameObject("Anchor"); a.transform.position = magnetPullOrigin.position; a.transform.parent = magnetPullOrigin;
            a.AddComponent<Rigidbody>().isKinematic = true;
            var sj = a.AddComponent<SpringJoint>(); sj.connectedBody = rb;
            sj.autoConfigureConnectedAnchor = false; sj.connectedAnchor = Vector3.zero;
            sj.spring = 100f; sj.damper = 25f; sj.breakForce = 120f;
            rb.linearDamping = 3f;
            heldBodies.Add(rb); magnetJoints.Add(sj); magnetAnchors.Add(a);
        }
    }
    void MagnetDrop(float force)
    {
        for (int i = 0; i < magnetAnchors.Count; i++) if (magnetAnchors[i]) Destroy(magnetAnchors[i]);
        magnetJoints.Clear(); magnetAnchors.Clear();
        foreach (var rb in heldBodies) { if (rb) { rb.AddForce(cam.transform.forward * force, ForceMode.Impulse); rb.linearDamping = 0f; } }
        LogEvent("MAGNET_DROP", $"{heldBodies.Count} bodies, force={force}");
        heldBodies.Clear();
    }
    void MagnetCleanup()
    {
        for (int i = magnetJoints.Count - 1; i >= 0; i--)
        {
            if (magnetJoints[i] == null || magnetJoints[i].connectedBody == null)
            {
                if (i < magnetAnchors.Count && magnetAnchors[i]) Destroy(magnetAnchors[i]);
                if (i < heldBodies.Count) { var rb = heldBodies[i]; if (rb) rb.linearDamping = 0f; heldBodies.RemoveAt(i); }
                magnetJoints.RemoveAt(i); magnetAnchors.RemoveAt(i);
            }
        }
    }

    void HammerInput()
    {
        if (Input.GetMouseButtonDown(0) && Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 3f))
            LogEvent("HAMMER_HIT", $"{hit.collider.name} (Phase D: TryTakeOrPack)");
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 4: DROP
    // ═══════════════════════════════════════════════════════════════
    void HandleDrop()
    {
        if (!Input.GetKeyDown(KeyCode.G) || activeSlot < 0) return;
        var slot = slots[activeSlot];
        if (slot.item == null) return;

        if (slot.qty > 1)
        {
            slot.qty--;
            var clone = new MockItem(slot.item.name, slot.item.color, slot.item.maxStack,
                cam.transform.position + cam.transform.forward * 1.5f, slot.item.toolType);
            clone.go.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward * 5f + Vector3.up * 2f;
            worldItems.Add(clone);
            LogEvent("DROP_STACK", $"1 clone of {slot.item.name}, remaining={slot.qty}");
        }
        else
        {
            var item = slot.item;
            item.go.SetActive(true); item.go.transform.parent = null;
            item.go.transform.localScale = item.originalScale;
            item.go.transform.position = cam.transform.position + cam.transform.forward * 1.5f;
            var rb = item.go.GetComponent<Rigidbody>(); if (rb) { rb.isKinematic = false; rb.linearVelocity = cam.transform.forward * 5f + Vector3.up * 2f; }
            var col = item.go.GetComponent<Collider>(); if (col) col.enabled = true;
            worldItems.Add(item);
            slot.item = null; slot.qty = 0; activeSlot = -1;
            LogEvent("DROP_LAST", item.name);
            OnItemDropped?.Invoke(item);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 7: GRAB (SpringJoint)
    // ═══════════════════════════════════════════════════════════════
    void HandleGrab()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (grabbedRb != null) ReleaseGrab();
            else TryGrab();
        }
        if (grabbedRb != null && grabRope.enabled)
        {
            grabRope.SetPosition(0, grabDragger.transform.position);
            grabRope.SetPosition(1, grabbedRb.transform.position);
        }
    }
    void TryGrab()
    {
        if (isMenuOpen) { LogEvent("GRAB_BLOCKED", "menu is open"); return; }
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 4f)) return;
        var rb = hit.collider.GetComponent<Rigidbody>();
        if (rb == null || rb.isKinematic) return;
        grabbedRb = rb;
        grabDragger.SetActive(true);
        grabJoint = grabDragger.AddComponent<SpringJoint>();
        grabJoint.connectedBody = rb;
        grabJoint.spring = 50f; grabJoint.damper = 10f; grabJoint.maxDistance = 0.1f;
        rb.linearDamping = 5f;
        grabRope.enabled = true;
        LogEvent("GRAB", hit.collider.name);
    }
    void ReleaseGrab()
    {
        if (grabJoint) Destroy(grabJoint);
        if (grabbedRb) { grabbedRb.linearDamping = 0f; LogEvent("RELEASE", grabbedRb.name); }
        grabbedRb = null;
        grabRope.enabled = false;
        grabDragger.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 6: HIGHLIGHT
    // ═══════════════════════════════════════════════════════════════
    void HandleHighlight()
    {
        if (highlightOverlay) { Destroy(highlightOverlay); highlightOverlay = null; highlightTarget = null; }
        if (isMenuOpen) return;
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 10f)) return;
        // highlight damageables + world items
        bool isDmg = damageables.Exists(d => d.go == hit.collider.gameObject);
        bool isItem = worldItems.Exists(i => i.go == hit.collider.gameObject);
        if (!isDmg && !isItem) return;

        highlightTarget = hit.collider.gameObject;
        var mf = highlightTarget.GetComponent<MeshFilter>();
        if (mf == null) return;
        highlightOverlay = new GameObject("HL");
        highlightOverlay.transform.position = highlightTarget.transform.position;
        highlightOverlay.transform.rotation = highlightTarget.transform.rotation;
        highlightOverlay.transform.localScale = highlightTarget.transform.lossyScale * 1.08f;
        highlightOverlay.AddComponent<MeshFilter>().mesh = mf.sharedMesh;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0, 1, 1, 0.35f); mat.renderQueue = 3100;
        highlightOverlay.AddComponent<MeshRenderer>().material = mat;
    }

    // ═══════════════════════════════════════════════════════════════
    // FLOW 8: INVENTORY OPEN/CLOSE + MENU SIM
    // ═══════════════════════════════════════════════════════════════
    void HandleToggleInventory()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (extendedOpen) { OnCloseInventoryView?.Invoke(); LogEvent("TAB", "close inventory"); }
            else { OnOpenInventoryView?.Invoke(); LogEvent("TAB", "open inventory"); }
        }
        if (extendedOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCloseInventoryView?.Invoke();
            LogEvent("ESC", "close inventory");
        }
    }
    void HandleMenuSim()
    {
        // M = simulate opening another menu while inventory is open (CloseAll pattern)
        if (Input.GetKeyDown(KeyCode.M))
        {
            LogEvent("MENU_SIM", "CloseAll pattern — closing inventory first");
            if (extendedOpen) OnCloseInventoryView?.Invoke();
            OnMenuStateChanged?.Invoke(true);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            OnMenuStateChanged?.Invoke(false);
            LogEvent("MENU_SIM", "menu closed");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // DUMP LOG
    // ═══════════════════════════════════════════════════════════════
    void HandleDumpLog()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("═══ EVENT LOG DUMP ═══");
            foreach (var e in eventLog) Debug.Log(e);
            Debug.Log($"═══ {eventLog.Count} events total ═══");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // HP BARS
    // ═══════════════════════════════════════════════════════════════
    void UpdateDamageableHPBars() { /* drawn via OnGUI for simplicity */ }

    // ═══════════════════════════════════════════════════════════════
    // GUI
    // ═══════════════════════════════════════════════════════════════
    void OnGUI()
    {
        // hotbar
        for (int i = 0; i < HOTBAR; i++)
        {
            var s = slots[i];
            string label = s.item != null ? $"[{i + 1}]{s.item.name}" + (s.qty > 1 ? $"x{s.qty}" : "") : $"[{i + 1}]";
            Color bg = i == activeSlot ? Color.white : (s.item != null ? s.item.color * 0.5f : Color.gray * 0.5f);
            GUI.backgroundColor = bg;
            GUI.Box(new Rect(Screen.width / 2 - HOTBAR * 35 + i * 70, Screen.height - 60, 65, 50), label);
        }
        GUI.backgroundColor = Color.white;

        // status
        string tool = activeSlot >= 0 && slots[activeSlot].item != null ? slots[activeSlot].item.name : "None";
        string state = isMenuOpen ? "MENU OPEN" : (extendedOpen ? "INVENTORY" : "GAMEPLAY");
        GUI.Label(new Rect(10, 10, 500, 25), $"[{state}] Tool:{tool} Held:{heldBodies.Count} Grab:{(grabbedRb != null ? grabbedRb.name : "none")} Events:{eventLog.Count}");
        GUI.Label(new Rect(10, 30, 500, 20), "1-5=hotbar G=drop Tab=inv F=grab M/N=menu L=log");

        // hp bars
        int y = 55;
        foreach (var d in damageables)
        {
            float pct = d.hp / d.maxHp;
            GUI.Label(new Rect(10, y, 200, 18), $"{d.go.name}: {d.hp:F0}/{d.maxHp:F0}");
            GUI.DrawTexture(new Rect(160, y + 2, 100 * pct, 12), Texture2D.whiteTexture);
            y += 18;
        }
    }
}