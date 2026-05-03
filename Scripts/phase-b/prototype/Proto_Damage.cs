using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — Damage System (drag onto any GO, press Play)
/// Creates: player (capsule+camera), damageable cubes with health bars, breakable cluster sphere.
///
/// Controls:
///   WASD/Space — move/jump
///   Mouse — look
///   Hold LMB — swing pickaxe → raycast → TakeDamage on damageable objects
///   Objects flash white on hit, health bar depletes, break into pieces at 0 HP
///   Cluster sphere: breaks into 3-5 smaller spheres on death (DamageableOrePiece)
///
/// Covers: IDamageable contract, health tracking, visual feedback (flash), break into pieces,
///         cluster breaking (DamageableOrePiece), particle burst on break, debris cleanup.
/// Zero external deps. Zero interfaces (simulated inline).
/// </summary>
public class Proto_Damage : MonoBehaviour
{
    #region config
    [SerializeField] float swingRange = 3f;
    [SerializeField] float swingDamage = 30f;
    [SerializeField] float swingCooldown = 0.6f;
    [SerializeField] float debrisDespawnTime = 8f;
    #endregion

    #region runtime
    GameObject player;
    Camera cam;
    CharacterController cc;
    float xRot, yVel;
    float lastSwingTime = -1f;

    class DamageableObj
    {
        public GameObject go;
        public float health;
        public float maxHealth;
        public Color color;
        public bool isCluster; // breaks into smaller pieces
        public GameObject healthBar;
        public Transform healthBarFill;
    }
    List<DamageableObj> damageables = new List<DamageableObj>();
    #endregion

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CreateWorld();
        CreatePlayer();
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleSwing();
        UpdateHealthBars();
    }

    #region world
    void CreateWorld()
    {
        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 5f;
        floor.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);

        // regular damageable cubes (ore nodes)
        CreateDamageable("IronNode", PrimitiveType.Cube, new Vector3(-3, 0.5f, 5), Color.gray, 100f, false);
        CreateDamageable("GoldNode", PrimitiveType.Cube, new Vector3(0, 0.5f, 5), Color.yellow, 80f, false);
        CreateDamageable("CopperNode", PrimitiveType.Cube, new Vector3(3, 0.5f, 5), new Color(1f, 0.5f, 0f), 120f, false);

        // cluster sphere (breaks into smaller pieces like DamageableOrePiece)
        CreateDamageable("Cluster_Large", PrimitiveType.Sphere, new Vector3(0, 0.8f, 8), new Color(0.6f, 0.3f, 0.8f), 60f, true);

        // small test target
        CreateDamageable("WeakTarget", PrimitiveType.Cube, new Vector3(-3, 0.3f, 8), Color.red, 30f, false);
    }

    void CreateDamageable(string name, PrimitiveType type, Vector3 pos, Color color, float health, bool isCluster)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<Renderer>().material.color = color;
        if (isCluster) go.transform.localScale = Vector3.one * 1.2f;

        // health bar (world space canvas alternative: simple scaled cube above)
        var barBG = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barBG.name = $"{name}_HealthBar";
        barBG.transform.position = pos + Vector3.up * 1.2f;
        barBG.transform.localScale = new Vector3(1f, 0.08f, 0.08f);
        barBG.GetComponent<Renderer>().material.color = Color.black;
        barBG.GetComponent<Collider>().enabled = false;

        var barFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barFill.name = $"{name}_HealthFill";
        barFill.transform.parent = barBG.transform;
        barFill.transform.localPosition = Vector3.zero;
        barFill.transform.localScale = Vector3.one;
        barFill.GetComponent<Renderer>().material.color = Color.green;
        barFill.GetComponent<Collider>().enabled = false;

        damageables.Add(new DamageableObj
        {
            go = go, health = health, maxHealth = health, color = color,
            isCluster = isCluster, healthBar = barBG, healthBarFill = barFill.transform
        });
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

        // simple pickaxe visual
        var pick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pick.name = "PickaxeVisual";
        pick.transform.parent = camGO.transform;
        pick.transform.localPosition = new Vector3(0.4f, -0.3f, 0.8f);
        pick.transform.localScale = new Vector3(0.06f, 0.06f, 0.35f);
        pick.GetComponent<Renderer>().material.color = Color.cyan;
        pick.GetComponent<Collider>().enabled = false;
    }
    #endregion

    #region movement + look
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = player.transform.right * h + player.transform.forward * v;
        move.y = 0; move = move.normalized * 5f;
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) yVel = 7f;
        yVel += Physics.gravity.y * Time.deltaTime;
        move.y = yVel;
        cc.Move(move * Time.deltaTime);
    }
    void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * 2f;
        float my = Input.GetAxis("Mouse Y") * 2f;
        xRot -= my; xRot = Mathf.Clamp(xRot, -89f, 89f);
        player.transform.Rotate(Vector3.up * mx);
        cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);
    }
    #endregion

    #region swing + damage
    void HandleSwing()
    {
        if (!Input.GetMouseButton(0)) return;
        if (Time.time - lastSwingTime < swingCooldown) return;
        lastSwingTime = Time.time;

        StartCoroutine(SwingAndHit());
    }

    IEnumerator SwingAndHit()
    {
        // visual: quick punch (rotate pickaxe)
        var pick = cam.transform.Find("PickaxeVisual");
        if (pick != null)
        {
            float t = 0;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float angle = Mathf.Sin(t / 0.2f * Mathf.PI) * 40f;
                pick.localRotation = Quaternion.Euler(-angle, 0, 0);
                yield return null;
            }
            pick.localRotation = Quaternion.identity;
        }

        // delayed raycast (like real ToolPickaxe 0.2s delay)
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, swingRange))
            yield break;

        // find damageable
        DamageableObj found = null;
        foreach (var d in damageables)
        {
            if (d.go == hit.collider.gameObject) { found = d; break; }
        }
        if (found == null)
        {
            // push rigidbody if any
            var rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForceAtPosition(cam.transform.forward * 3f, hit.point, ForceMode.Impulse);
            yield break;
        }

        // apply damage
        found.health -= swingDamage;
        Debug.Log($"[Proto_Damage] Hit {found.go.name} for {swingDamage}, health={found.health}/{found.maxHealth}");

        // flash white
        var rend = found.go.GetComponent<Renderer>();
        Color originalColor = found.color;
        rend.material.color = Color.white;
        StartCoroutine(FlashBack(rend, originalColor, 0.1f));

        // spawn hit particles (small cubes burst)
        SpawnHitParticles(hit.point, hit.normal, found.color);

        // break at 0
        if (found.health <= 0)
        {
            if (found.isCluster)
                BreakCluster(found, hit.point);
            else
                BreakNode(found, hit.point);
        }
    }

    IEnumerator FlashBack(Renderer rend, Color color, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rend != null) rend.material.color = color;
    }

    void SpawnHitParticles(Vector3 point, Vector3 normal, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.position = point;
            p.transform.localScale = Vector3.one * 0.05f;
            p.GetComponent<Renderer>().material.color = color;
            p.GetComponent<Collider>().enabled = false;
            var rb = p.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            rb.useGravity = true;
            rb.linearVelocity = (normal + Random.insideUnitSphere).normalized * Random.Range(2f, 4f);
            Destroy(p, 1.5f);
        }
    }

    void BreakNode(DamageableObj node, Vector3 hitPoint)
    {
        Debug.Log($"[Proto_Damage] BROKE {node.go.name}!");
        int dropCount = Random.Range(2, 5);
        for (int i = 0; i < dropCount; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.transform.position = node.go.transform.position + Random.insideUnitSphere * 0.3f;
            piece.transform.localScale = Vector3.one * 0.25f;
            piece.GetComponent<Renderer>().material.color = node.color;
            var rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.linearVelocity = (Random.insideUnitSphere + Vector3.up).normalized * Random.Range(2f, 5f);
            Destroy(piece, debrisDespawnTime);
        }
        // burst particles
        SpawnHitParticles(node.go.transform.position, Vector3.up, node.color);
        // cleanup
        Destroy(node.healthBar);
        Destroy(node.go);
        damageables.Remove(node);
    }

    void BreakCluster(DamageableObj cluster, Vector3 hitPoint)
    {
        Debug.Log($"[Proto_Damage] CLUSTER BREAK {cluster.go.name}! Spawning sub-pieces...");
        int subCount = Random.Range(3, 6);
        for (int i = 0; i < subCount; i++)
        {
            // sub-pieces are smaller damageables
            Color subColor = Color.Lerp(cluster.color, Color.white, Random.Range(0f, 0.3f));
            var sub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sub.name = $"ClusterPiece_{i}";
            sub.transform.position = cluster.go.transform.position + Random.insideUnitSphere * 0.5f;
            sub.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);
            sub.GetComponent<Renderer>().material.color = subColor;
            var rb = sub.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearVelocity = (Random.insideUnitSphere + Vector3.up).normalized * Random.Range(3f, 6f);

            // make sub-piece damageable too (no cluster flag — breaks into debris)
            var barBG = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barBG.transform.position = sub.transform.position + Vector3.up * 0.8f;
            barBG.transform.localScale = new Vector3(0.6f, 0.05f, 0.05f);
            barBG.GetComponent<Renderer>().material.color = Color.black;
            barBG.GetComponent<Collider>().enabled = false;
            var barFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barFill.transform.parent = barBG.transform;
            barFill.transform.localPosition = Vector3.zero;
            barFill.transform.localScale = Vector3.one;
            barFill.GetComponent<Renderer>().material.color = Color.green;
            barFill.GetComponent<Collider>().enabled = false;

            damageables.Add(new DamageableObj
            {
                go = sub, health = 40f, maxHealth = 40f, color = subColor,
                isCluster = false, healthBar = barBG, healthBarFill = barFill.transform
            });
        }
        // burst particles
        for (int i = 0; i < 10; i++)
            SpawnHitParticles(cluster.go.transform.position, Random.insideUnitSphere, cluster.color);
        // cleanup original
        Destroy(cluster.healthBar);
        Destroy(cluster.go);
        damageables.Remove(cluster);
    }
    #endregion

    #region health bars
    void UpdateHealthBars()
    {
        foreach (var d in damageables)
        {
            if (d.healthBar == null || d.go == null) continue;
            // position above object
            d.healthBar.transform.position = d.go.transform.position + Vector3.up * (d.go.transform.localScale.y + 0.5f);
            // face camera
            d.healthBar.transform.LookAt(cam.transform);
            // fill scale
            float pct = Mathf.Clamp01(d.health / d.maxHealth);
            d.healthBarFill.localScale = new Vector3(pct, 1, 1);
            d.healthBarFill.localPosition = new Vector3((pct - 1f) * 0.5f, 0, 0);
            // color: green → yellow → red
            var fillRend = d.healthBarFill.GetComponent<Renderer>();
            if (fillRend != null)
                fillRend.material.color = Color.Lerp(Color.red, Color.green, pct);
        }
    }
    #endregion

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 25), $"Hold LMB to swing pickaxe | {damageables.Count} targets remaining");
    }
}