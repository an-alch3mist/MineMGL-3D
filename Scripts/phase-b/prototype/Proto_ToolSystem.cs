using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — Tool System (drag onto any GO, press Play)
/// Creates: player (capsule+camera), ore nodes (colored spheres), loose cubes (physics).
/// Hotbar: 1=Pickaxe, 2=Magnet, 3=Hammer. Each tool auto-created and equipped.
///
/// Controls:
///   WASD/Space — move/jump
///   Mouse — look
///   1 — equip Pickaxe (hold LMB: swing → 0.2s delay → raycast → damage node or push rigidbody)
///   2 — equip Magnet (hold RMB: pull nearby cubes via SpringJoint; LMB: push all; R: drop gently; Q: cycle mode)
///   3 — equip Hammer (LMB: raycast → log "hit building" placeholder)
///   G — unequip current tool
///
/// Covers: tool switching, pickaxe swing+delay+raycast+damage, magnet pull/push/drop/cycle,
///         hammer raycast, equip/unequip, ViewModelContainer parenting.
/// Zero external deps. Zero interfaces.
/// </summary>
public class Proto_ToolSystem : MonoBehaviour
{
    #region config
    [Header("Player")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float lookSens = 2f;
    [Header("Pickaxe")]
    [SerializeField] float pickaxeRange = 3f;
    [SerializeField] float pickaxeDamage = 25f;
    [SerializeField] float pickaxeCooldown = 0.8f;
    [Header("Magnet")]
    [SerializeField] float magnetRadius = 3f;
    [SerializeField] float pushForce = 5f;
    #endregion

    #region runtime
    GameObject player;
    Camera cam;
    CharacterController cc;
    float xRot, yVel;
    Transform viewModelContainer;

    // tools (visual cubes parented to viewModelContainer)
    GameObject toolPickaxe, toolMagnet, toolHammer;
    int activeTool = -1; // 0=pickaxe, 1=magnet, 2=hammer
    float lastSwingTime = -1f;

    // magnet
    List<Rigidbody> heldBodies = new List<Rigidbody>();
    List<SpringJoint> magnetJoints = new List<SpringJoint>();
    List<GameObject> magnetAnchors = new List<GameObject>();
    Transform magnetPullOrigin;

    // ore nodes
    class OreNode { public GameObject go; public float health; public Color color; }
    List<OreNode> nodes = new List<OreNode>();

    // physics cubes
    List<GameObject> looseCubes = new List<GameObject>();
    #endregion

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CreateWorld();
        CreatePlayer();
        CreateTools();
        SwitchTool(0); // start with pickaxe
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleToolSwitch();
        HandleToolInput();
        MagnetFixedLogic();
    }

    #region world
    void CreateWorld()
    {
        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 5f;
        floor.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.3f);

        // ore nodes (spheres with health)
        Color[] colors = { Color.gray, Color.yellow, new Color(1f, 0.5f, 0f), Color.black };
        string[] names = { "Iron", "Gold", "Copper", "Coal" };
        for (int i = 0; i < 4; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"OreNode_{names[i]}";
            go.transform.position = new Vector3(-3 + i * 2, 0.5f, 5);
            go.GetComponent<Renderer>().material.color = colors[i];
            nodes.Add(new OreNode { go = go, health = 100f, color = colors[i] });
        }

        // loose physics cubes (for magnet)
        for (int i = 0; i < 8; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"LooseCube_{i}";
            cube.transform.position = new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(2f, 8f));
            cube.transform.localScale = Vector3.one * 0.3f;
            cube.GetComponent<Renderer>().material.color = Color.Lerp(Color.red, Color.blue, i / 8f);
            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            looseCubes.Add(cube);
        }
    }
    #endregion

    #region player
    void CreatePlayer()
    {
        player = new GameObject("Proto_Player");
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

        magnetPullOrigin = new GameObject("MagnetPullOrigin").transform;
        magnetPullOrigin.parent = camGO.transform;
        magnetPullOrigin.localPosition = new Vector3(0, 0, 1.5f);
    }
    #endregion

    #region tools creation
    void CreateTools()
    {
        // pickaxe — elongated cyan cube
        toolPickaxe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        toolPickaxe.name = "Pickaxe";
        toolPickaxe.transform.localScale = new Vector3(0.08f, 0.08f, 0.4f);
        toolPickaxe.GetComponent<Renderer>().material.color = Color.cyan;
        toolPickaxe.GetComponent<Collider>().enabled = false;
        SetupToolParenting(toolPickaxe);

        // magnet — magenta sphere
        toolMagnet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        toolMagnet.name = "Magnet";
        toolMagnet.transform.localScale = Vector3.one * 0.15f;
        toolMagnet.GetComponent<Renderer>().material.color = Color.magenta;
        toolMagnet.GetComponent<Collider>().enabled = false;
        SetupToolParenting(toolMagnet);

        // hammer — yellow cube
        toolHammer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        toolHammer.name = "Hammer";
        toolHammer.transform.localScale = new Vector3(0.12f, 0.12f, 0.3f);
        toolHammer.GetComponent<Renderer>().material.color = Color.yellow;
        toolHammer.GetComponent<Collider>().enabled = false;
        SetupToolParenting(toolHammer);
    }
    void SetupToolParenting(GameObject tool)
    {
        tool.transform.parent = viewModelContainer;
        tool.transform.localPosition = Vector3.zero;
        tool.transform.localRotation = Quaternion.identity;
        tool.SetActive(false);
    }
    #endregion

    #region movement + look
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = player.transform.right * h + player.transform.forward * v;
        move.y = 0; move = move.normalized * moveSpeed;
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) yVel = 7f;
        yVel += Physics.gravity.y * Time.deltaTime;
        move.y = yVel;
        cc.Move(move * Time.deltaTime);
    }
    void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * lookSens;
        float my = Input.GetAxis("Mouse Y") * lookSens;
        xRot -= my; xRot = Mathf.Clamp(xRot, -89f, 89f);
        player.transform.Rotate(Vector3.up * mx);
        cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);
    }
    #endregion

    #region tool switch
    void HandleToolSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTool(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTool(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTool(2);
        if (Input.GetKeyDown(KeyCode.G)) { MagnetDropAll(0.5f); SwitchTool(-1); }
    }
    void SwitchTool(int idx)
    {
        toolPickaxe.SetActive(false);
        toolMagnet.SetActive(false);
        toolHammer.SetActive(false);
        activeTool = idx;
        if (idx == 0) toolPickaxe.SetActive(true);
        if (idx == 1) toolMagnet.SetActive(true);
        if (idx == 2) toolHammer.SetActive(true);
    }
    #endregion

    #region tool input
    void HandleToolInput()
    {
        if (activeTool == 0) PickaxeInput();
        if (activeTool == 1) MagnetInput();
        if (activeTool == 2) HammerInput();
    }

    // === PICKAXE ===
    void PickaxeInput()
    {
        if (Input.GetMouseButton(0) && Time.time - lastSwingTime >= pickaxeCooldown)
        {
            lastSwingTime = Time.time;
            // swing visual: quick rotation punch
            StartCoroutine(SwingAnimation());
            StartCoroutine(PickaxeHit(0.2f));
        }
    }
    IEnumerator SwingAnimation()
    {
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float angle = Mathf.Sin(t / 0.3f * Mathf.PI) * 45f;
            toolPickaxe.transform.localRotation = Quaternion.Euler(-angle, 0, 0);
            yield return null;
        }
        toolPickaxe.transform.localRotation = Quaternion.identity;
    }
    IEnumerator PickaxeHit(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, pickaxeRange))
            yield break;

        // check ore node
        foreach (var node in nodes)
        {
            if (node.go == hit.collider.gameObject)
            {
                node.health -= pickaxeDamage;
                // flash white
                var rend = node.go.GetComponent<Renderer>();
                rend.material.color = Color.white;
                StartCoroutine(FlashBack(rend, node.color, 0.1f));
                Debug.Log($"[Proto_Tool] Hit {node.go.name}, health={node.health}");
                if (node.health <= 0) BreakNode(node);
                yield break;
            }
        }
        // push rigidbody
        var rb = hit.collider.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForceAtPosition(cam.transform.forward * 5f, hit.point, ForceMode.Impulse);
            Debug.Log($"[Proto_Tool] Pushed {hit.collider.name}");
        }
    }
    IEnumerator FlashBack(Renderer rend, Color original, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rend != null) rend.material.color = original;
    }
    void BreakNode(OreNode node)
    {
        Debug.Log($"[Proto_Tool] BROKE {node.go.name}!");
        // spawn 3 small cubes
        for (int i = 0; i < 3; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.transform.position = node.go.transform.position + Random.insideUnitSphere * 0.3f;
            piece.transform.localScale = Vector3.one * 0.2f;
            piece.GetComponent<Renderer>().material.color = node.color;
            var rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearVelocity = Random.insideUnitSphere * 3f + Vector3.up * 2f;
            looseCubes.Add(piece);
        }
        nodes.Remove(node);
        Destroy(node.go);
    }

    // === MAGNET ===
    void MagnetInput()
    {
        if (Input.GetMouseButton(1)) MagnetPull();
        if (Input.GetMouseButtonDown(0)) MagnetDropAll(pushForce);
        if (Input.GetKeyDown(KeyCode.R)) MagnetDropAll(0.5f);
    }
    void MagnetPull()
    {
        Collider[] cols = Physics.OverlapSphere(magnetPullOrigin.position, magnetRadius);
        foreach (var col in cols)
        {
            var rb = col.attachedRigidbody;
            if (rb == null || heldBodies.Contains(rb)) continue;
            if (rb.gameObject.GetComponent<CharacterController>()) continue; // skip player

            var anchor = new GameObject("MagnetAnchor");
            anchor.transform.position = magnetPullOrigin.position;
            anchor.transform.parent = magnetPullOrigin;
            var anchorRb = anchor.AddComponent<Rigidbody>();
            anchorRb.isKinematic = true;
            var sj = anchor.AddComponent<SpringJoint>();
            sj.connectedBody = rb;
            sj.autoConfigureConnectedAnchor = false;
            sj.connectedAnchor = Vector3.zero;
            sj.spring = 100f; sj.damper = 25f; sj.maxDistance = 0.01f;
            sj.breakForce = 120f;
            rb.linearDamping = 3f;
            heldBodies.Add(rb); magnetJoints.Add(sj); magnetAnchors.Add(anchor);
        }
    }
    void MagnetDropAll(float force)
    {
        for (int i = 0; i < magnetAnchors.Count; i++)
            if (magnetAnchors[i] != null) Destroy(magnetAnchors[i]);
        magnetJoints.Clear(); magnetAnchors.Clear();
        foreach (var rb in heldBodies)
        {
            if (rb == null) continue;
            rb.AddForce(cam.transform.forward * force, ForceMode.Impulse);
            rb.linearDamping = 0f;
        }
        heldBodies.Clear();
    }
    void MagnetFixedLogic()
    {
        // cleanup broken joints
        for (int i = magnetJoints.Count - 1; i >= 0; i--)
        {
            if (magnetJoints[i] == null || magnetJoints[i].connectedBody == null)
            {
                if (i < magnetAnchors.Count && magnetAnchors[i] != null) Destroy(magnetAnchors[i]);
                if (i < heldBodies.Count) { var rb = heldBodies[i]; if (rb) rb.linearDamping = 0f; heldBodies.RemoveAt(i); }
                magnetJoints.RemoveAt(i); magnetAnchors.RemoveAt(i);
            }
        }
    }

    // === HAMMER ===
    void HammerInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 3f))
                Debug.Log($"[Proto_Tool] Hammer hit: {hit.collider.name} (Phase D: would Take/Pack building)");
        }
    }
    #endregion

    void OnGUI()
    {
        string[] toolNames = { "Pickaxe", "Magnet", "Hammer" };
        string active = activeTool >= 0 ? toolNames[activeTool] : "None";
        GUI.Label(new Rect(10, 10, 400, 25), $"Tool: {active} | 1=Pick 2=Mag 3=Ham G=unequip | Held: {heldBodies.Count}");
        // node health bars
        int y = 35;
        foreach (var node in nodes)
        {
            GUI.Label(new Rect(10, y, 200, 20), $"{node.go.name}: {node.health}/100");
            y += 20;
        }
    }
}