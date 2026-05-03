using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — Highlight System (drag onto any GO, press Play)
/// Creates: player (capsule+camera), colored cubes. Look at a cube → it gets outlined.
/// Look away → outline removed.
///
/// Controls:
///   WASD/Space — move/jump
///   Mouse — look
///   Look at cube within range → outline appears (scaled wireframe overlay)
///   Look away → outline disappears
///
/// Covers: per-frame raycast, GetComponentInParent detection, apply/clear highlight,
///         wireframe overlay (simulated via second mesh + wireframe shader alternative: scaled clone),
///         different profiles per object type.
/// Zero external deps. Zero Highlight Plus package.
/// </summary>
public class Proto_Highlight : MonoBehaviour
{
    #region config
    [SerializeField] float highlightRange = 10f;
    [SerializeField] Color outlineColor = Color.cyan;
    [SerializeField] float outlineScale = 1.08f;
    #endregion

    #region runtime
    GameObject player;
    Camera cam;
    CharacterController cc;
    float xRot, yVel;

    // highlight state
    GameObject currentOutline;
    GameObject currentTarget;
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
        HandleHighlight();
    }

    #region world
    void CreateWorld()
    {
        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 5f;
        floor.GetComponent<Renderer>().material.color = new Color(0.25f, 0.3f, 0.25f);
        floor.tag = "Untagged"; // floor should NOT highlight

        // highlightable cubes with different "profiles" (colors)
        CreateHighlightable("Tool_Cyan", PrimitiveType.Cube, new Vector3(-3, 0.5f, 4), Color.cyan, Color.cyan);
        CreateHighlightable("Building_Green", PrimitiveType.Cube, new Vector3(0, 0.5f, 4), Color.green, Color.green);
        CreateHighlightable("Ore_Yellow", PrimitiveType.Sphere, new Vector3(3, 0.5f, 4), Color.yellow, Color.yellow);
        CreateHighlightable("Interactable_White", PrimitiveType.Cube, new Vector3(0, 0.5f, 7), Color.white, Color.white);

        // non-highlightable object (no HighlightTag)
        var plain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plain.name = "NonHighlightable";
        plain.transform.position = new Vector3(-3, 0.5f, 7);
        plain.GetComponent<Renderer>().material.color = Color.gray;
    }

    void CreateHighlightable(string name, PrimitiveType type, Vector3 pos, Color baseColor, Color hlColor)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<Renderer>().material.color = baseColor;
        // store highlight color in a simple component
        var tag = go.AddComponent<HighlightTag>();
        tag.highlightColor = hlColor;
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

    #region highlight logic
    void HandleHighlight()
    {
        // clear previous
        if (currentOutline != null)
        {
            Destroy(currentOutline);
            currentOutline = null;
            currentTarget = null;
        }

        // raycast
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, highlightRange))
            return;

        // check for HighlightTag (simulates IHighlightable.GetHighlightProfile)
        var tag = hit.collider.GetComponentInParent<HighlightTag>();
        if (tag == null) return;

        currentTarget = tag.gameObject;
        ApplyOutline(currentTarget, tag.highlightColor);
    }

    void ApplyOutline(GameObject target, Color color)
    {
        // create a scaled clone as outline (simulates Highlight Plus outline effect)
        var meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        currentOutline = new GameObject("Outline");
        currentOutline.transform.position = target.transform.position;
        currentOutline.transform.rotation = target.transform.rotation;
        currentOutline.transform.localScale = target.transform.lossyScale * outlineScale;

        var mf = currentOutline.AddComponent<MeshFilter>();
        mf.mesh = meshFilter.sharedMesh;
        var mr = currentOutline.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(color.r, color.g, color.b, 0.4f);
        mr.material = mat;

        // render on top
        mat.renderQueue = 3100;
    }
    #endregion

    void OnGUI()
    {
        string target = currentTarget != null ? currentTarget.name : "None";
        GUI.Label(new Rect(10, 10, 400, 25), $"Looking at: {target} | Look at colored shapes to highlight");
    }
}

/// <summary> Simple tag to mark objects as highlightable with a color profile. </summary>
public class HighlightTag : MonoBehaviour
{
    public Color highlightColor = Color.cyan;
}