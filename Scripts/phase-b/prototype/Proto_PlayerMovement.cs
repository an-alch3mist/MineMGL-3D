using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — Player Movement System (drag onto any GO, press Play)
/// Creates: player capsule + camera, flat floor, obstacles (ramp, wall, platform).
///
/// Controls:
///   WASD — move
///   Space — jump
///   Shift — sprint (1.8x speed)
///   C — duck (halves height, slower speed)
///   Mouse — look (pitch clamped ±89°)
///   V — toggle noclip (fly through walls, no gravity)
///
/// Covers: WASD, jump, sprint, duck, slope sliding, gravity, noclip, camera bob, cursor lock.
/// Zero external deps. Zero GameEvents.
/// </summary>
public class Proto_PlayerMovement : MonoBehaviour
{
    #region config
    [Header("Movement")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintMultiplier = 1.8f;
    [SerializeField] float duckSpeed = 2.5f;
    [SerializeField] float jumpForce = 7f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float slopeLimit = 45f;

    [Header("Camera")]
    [SerializeField] float lookSens = 2f;
    [SerializeField] float bobFrequency = 8f;
    [SerializeField] float bobAmplitude = 0.03f;

    [Header("Duck")]
    [SerializeField] float standHeight = 2f;
    [SerializeField] float duckHeight = 1.2f;
    [SerializeField] float duckCamY = 0.9f;
    [SerializeField] float standCamY = 1.6f;
    #endregion

    #region runtime
    GameObject player;
    Camera cam;
    CharacterController cc;
    float xRot, yVel;
    bool isDucking, isNoclip;
    float bobTimer;
    Vector3 noclipVel;
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
        HandleLook();
        if (Input.GetKeyDown(KeyCode.V)) ToggleNoclip();
        if (isNoclip) HandleNoclip();
        else HandleNormalMovement();
        HandleDuck();
        HandleBob();
    }

    #region world creation
    void CreateWorld()
    {
        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 5f;
        floor.GetComponent<Renderer>().material.color = new Color(0.3f, 0.35f, 0.3f);

        // ramp (slope test)
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.transform.position = new Vector3(5, 0.5f, 5);
        ramp.transform.localScale = new Vector3(3, 0.2f, 5);
        ramp.transform.rotation = Quaternion.Euler(0, 0, -25f);
        ramp.GetComponent<Renderer>().material.color = new Color(0.5f, 0.4f, 0.3f);

        // wall
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(-5, 2, 0);
        wall.transform.localScale = new Vector3(0.3f, 4, 8);
        wall.GetComponent<Renderer>().material.color = Color.gray;

        // platform (jump test)
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.transform.position = new Vector3(0, 2, -5);
        platform.transform.localScale = new Vector3(3, 0.3f, 3);
        platform.GetComponent<Renderer>().material.color = new Color(0.4f, 0.5f, 0.6f);

        // step stairs
        for (int i = 0; i < 5; i++)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.transform.position = new Vector3(8, 0.25f + i * 0.5f, i * 0.6f);
            step.transform.localScale = new Vector3(2, 0.5f, 0.6f);
            step.GetComponent<Renderer>().material.color = Color.Lerp(Color.gray, Color.white, i / 5f);
        }
    }
    #endregion

    #region player creation
    void CreatePlayer()
    {
        player = new GameObject("Proto_Player");
        player.transform.position = new Vector3(0, 1, 0);
        cc = player.AddComponent<CharacterController>();
        cc.height = standHeight;
        cc.radius = 0.4f;
        cc.center = Vector3.up;
        cc.slopeLimit = slopeLimit;
        cc.stepOffset = 0.4f;

        var camGO = new GameObject("Camera");
        camGO.transform.parent = player.transform;
        camGO.transform.localPosition = new Vector3(0, standCamY, 0);
        cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
    }
    #endregion

    #region look
    void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * lookSens;
        float my = Input.GetAxis("Mouse Y") * lookSens;
        xRot -= my;
        xRot = Mathf.Clamp(xRot, -89f, 89f);
        player.transform.Rotate(Vector3.up * mx);
        cam.transform.localRotation = Quaternion.Euler(xRot, 0, 0);
    }
    #endregion

    #region normal movement
    void HandleNormalMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = player.transform.right * h + player.transform.forward * v;
        move.y = 0;

        float speed = walkSpeed;
        if (isDucking) speed = duckSpeed;
        else if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        if (cc.isGrounded)
        {
            yVel = -2f; // small downward to keep grounded
            if (Input.GetKeyDown(KeyCode.Space) && !isDucking)
                yVel = jumpForce;
        }
        yVel += gravity * Time.deltaTime;

        Vector3 finalMove = move.normalized * speed;
        finalMove.y = yVel;
        cc.Move(finalMove * Time.deltaTime);
    }
    #endregion

    #region duck
    void HandleDuck()
    {
        if (isNoclip) return;
        bool wantsDuck = Input.GetKey(KeyCode.C);
        if (wantsDuck && !isDucking)
        {
            isDucking = true;
            cc.height = duckHeight;
            cc.center = new Vector3(0, duckHeight / 2f, 0);
        }
        else if (!wantsDuck && isDucking)
        {
            // check ceiling before standing
            if (!Physics.Raycast(player.transform.position + Vector3.up * duckHeight, Vector3.up, standHeight - duckHeight + 0.1f))
            {
                isDucking = false;
                cc.height = standHeight;
                cc.center = Vector3.up;
            }
        }
        float targetY = isDucking ? duckCamY : standCamY;
        var camPos = cam.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetY, Time.deltaTime * 10f);
        cam.transform.localPosition = camPos;
    }
    #endregion

    #region noclip
    void ToggleNoclip()
    {
        isNoclip = !isNoclip;
        cc.enabled = !isNoclip;
        Debug.Log($"[Proto_Player] Noclip: {isNoclip}");
    }
    void HandleNoclip()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float ud = 0;
        if (Input.GetKey(KeyCode.Space)) ud = 1f;
        if (Input.GetKey(KeyCode.LeftControl)) ud = -1f;
        Vector3 dir = cam.transform.right * h + cam.transform.forward * v + Vector3.up * ud;
        float speed = Input.GetKey(KeyCode.LeftShift) ? walkSpeed * 3f : walkSpeed * 1.5f;
        player.transform.position += dir.normalized * speed * Time.deltaTime;
    }
    #endregion

    #region camera bob
    void HandleBob()
    {
        if (isNoclip || !cc.isGrounded) return;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool moving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        if (moving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobY = Mathf.Sin(bobTimer) * bobAmplitude;
            var pos = cam.transform.localPosition;
            pos.y += bobY;
            cam.transform.localPosition = pos;
        }
        else bobTimer = 0;
    }
    #endregion

    void OnGUI()
    {
        string mode = isNoclip ? "NOCLIP" : (isDucking ? "DUCK" : (Input.GetKey(KeyCode.LeftShift) ? "SPRINT" : "WALK"));
        string grounded = cc.enabled ? (cc.isGrounded ? "grounded" : "airborne") : "noclip";
        GUI.Label(new Rect(10, 10, 300, 25), $"[{mode}] {grounded} | V=noclip C=duck Shift=sprint");
    }
}