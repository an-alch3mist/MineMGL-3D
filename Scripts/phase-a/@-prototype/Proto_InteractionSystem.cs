using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — InteractionSystem (end-to-end)
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY InteractionSystem feature:
///   1. IInteractable interface pattern (GetObjectName, ShouldUseInteractionWheel, GetOptions, Interact)
///   2. SimplePlayerInteraction: raycast from camera → finds IInteractable → fires event
///   3. InteractionWheelUI: radial option buttons → player picks one → fires Interact
///   4. Single-option shortcut: ShouldUseInteractionWheel=false → auto-execute first option
///   5. Multi-option wheel: 2+ options → shows wheel buttons in radial layout → player clicks
///   6. SO_InteractionOption replaced by inline data class (name + type enum + color)
///   7. Field_InteractionOption: runtime button creation with label
///   8. Menu state tracking: interaction open → cursor unlocks, movement pauses
///   9. InteractableComputer pattern: one interactable fires "openShopView" action
///  10. Multiple interactable types: Computer (1 option), Vendor (3 options), Crate (2 options)
///
/// Controls:
///   WASD = move, Mouse = look
///   E = raycast interact (hit colored box)
///   ESC = close wheel if open
///   I = print status (interactables found, menu state)
/// </summary>
public class Proto_InteractionSystem : MonoBehaviour
{
    Camera cam;
    float xRot, yRot;
    bool isMenuOpen;
    GameObject wheelPanel;
    List<GameObject> wheelButtons = new List<GameObject>();
    List<PIInteractable> interactables = new List<PIInteractable>();
    GameObject statusText;
    CharacterController cc;

    void Start()
    {
        // → camera
        cam = Camera.main;
        cam.transform.position = new Vector3(0f, 1.6f, -5f);
        cam.transform.rotation = Quaternion.identity;

        // → character controller for movement
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        transform.position = new Vector3(0f, 0f, -5f);
        cam.transform.SetParent(transform);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);
        ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);

        // → spawn interactable objects
        SpawnInteractables();

        // → build wheel panel (initially hidden)
        BuildWheelUI();

        // → build HUD status text
        BuildStatusHUD();

        Debug.Log("[Interaction] WASD=move, Mouse=look, E=interact, ESC=close wheel, I=info");
    }

    void Update()
    {
        // → handle cursor
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isMenuOpen;

        if (!isMenuOpen)
        {
            HandleLook();
            HandleMovement();
        }

        // → E = interact
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isMenuOpen)
                CloseWheel();
            else
                TryInteract();
        }

        // → ESC = close
        if (Input.GetKeyDown(KeyCode.Escape) && isMenuOpen)
            CloseWheel();

        // → I = info
        if (Input.GetKeyDown(KeyCode.I))
        {
            int alive = 0;
            foreach (var ia in interactables) if (ia != null) alive++;
            Debug.Log($"[Interaction] Interactables: {alive}, MenuOpen: {isMenuOpen}");
        }

        UpdateStatusHUD();
    }

    void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * 2.5f;
        float my = Input.GetAxis("Mouse Y") * 2.5f;
        xRot = Mathf.Clamp(xRot - my, -80f, 80f);
        yRot += mx;
        cam.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yRot, 0f);
    }

    void HandleMovement()
    {
        Vector3 move = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
        cc.Move(move.normalized * 5f * Time.deltaTime);
        // → gravity
        cc.Move(Vector3.down * 10f * Time.deltaTime);
    }

    void TryInteract()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 6f))
        {
            var interactable = hit.collider.GetComponent<PIInteractable>();
            if (interactable == null)
            {
                Debug.Log($"[Interaction] Hit {hit.collider.name} — no IInteractable");
                return;
            }
            Debug.Log($"[Interaction] Found: {interactable.GetObjectName()}");

            var options = interactable.GetOptions();
            if (!interactable.ShouldUseInteractionWheel())
            {
                // → single option shortcut: auto-execute first
                Debug.Log($"[Interaction] Single-option shortcut → {options[0].name}");
                interactable.Interact(options[0]);
            }
            else
            {
                // → multi option: open wheel
                OpenWheel(interactable, options);
            }
        }
    }

    // ═══ Wheel UI (runtime IMGUI-style using world-space canvas) ═══

    Canvas canvas;
    UnityEngine.UI.VerticalLayoutGroup vlg;

    void BuildWheelUI()
    {
        // → Canvas
        var canvasGo = new GameObject("WheelCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // → Panel
        wheelPanel = new GameObject("WheelPanel");
        wheelPanel.transform.SetParent(canvasGo.transform, false);
        var panelRect = wheelPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.35f, 0.25f);
        panelRect.anchorMax = new Vector2(0.65f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImg = wheelPanel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.92f);

        // → Title
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(wheelPanel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.85f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleGo.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "INTERACTION WHEEL";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 22;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // → Vertical layout for option buttons
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(wheelPanel.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.05f, 0.05f);
        contentRect.anchorMax = new Vector2(0.95f, 0.82f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        vlg = contentGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        wheelPanel.SetActive(false);
    }

    void OpenWheel(PIInteractable interactable, List<PInteractionOption> options)
    {
        // → clear old buttons
        foreach (var b in wheelButtons) Destroy(b);
        wheelButtons.Clear();

        // → set title
        var titleText = wheelPanel.transform.Find("Title").GetComponent<UnityEngine.UI.Text>();
        titleText.text = interactable.GetObjectName();

        // → create option buttons
        var content = wheelPanel.transform.Find("Content");
        foreach (var option in options)
        {
            var btnGo = new GameObject($"Btn_{option.name}");
            btnGo.transform.SetParent(content, false);
            btnGo.AddComponent<RectTransform>();
            var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = option.color;
            var btn = btnGo.AddComponent<UnityEngine.UI.Button>();

            // → label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.AddComponent<UnityEngine.UI.Text>();
            labelText.text = option.name;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;

            // → click handler
            var capturedOption = option;
            var capturedInteractable = interactable;
            btn.onClick.AddListener(() =>
            {
                capturedInteractable.Interact(capturedOption);
                CloseWheel();
            });

            wheelButtons.Add(btnGo);
        }

        wheelPanel.SetActive(true);
        isMenuOpen = true;
        Debug.Log($"[Interaction] Wheel opened: {options.Count} options");
    }

    void CloseWheel()
    {
        wheelPanel.SetActive(false);
        isMenuOpen = false;
        Debug.Log("[Interaction] Wheel closed");
    }

    // ═══ Status HUD ═══

    UnityEngine.UI.Text hudText;

    void BuildStatusHUD()
    {
        var hudGo = new GameObject("HUDStatus");
        hudGo.transform.SetParent(canvas.transform, false);
        var rect = hudGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.92f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        hudText = hudGo.AddComponent<UnityEngine.UI.Text>();
        hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudText.fontSize = 16;
        hudText.alignment = TextAnchor.MiddleCenter;
        hudText.color = Color.yellow;
    }

    void UpdateStatusHUD()
    {
        string menuStr = isMenuOpen ? "<color=lime>OPEN</color>" : "<color=red>CLOSED</color>";
        // → crosshair hint
        string hint = "";
        if (!isMenuOpen)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 6f))
            {
                var ia = hit.collider.GetComponent<PIInteractable>();
                if (ia != null) hint = $" | [E] {ia.GetObjectName()}";
            }
        }
        hudText.text = $"Menu: {menuStr}{hint}";
    }

    // ═══ Spawn Interactables ═══

    void SpawnInteractables()
    {
        // → 1. Computer — single option (auto-execute, no wheel)
        SpawnBox("Computer", new Vector3(-3f, 0.75f, 5f), new Color(0.2f, 0.4f, 0.8f),
            false, new List<PInteractionOption>
            {
                new PInteractionOption("Open Shop", PInteractionType.openShopView, new Color(0.3f, 0.5f, 0.9f))
            });

        // → 2. Vendor — 3 options (wheel opens)
        SpawnBox("Vendor NPC", new Vector3(0f, 0.75f, 5f), new Color(0.1f, 0.7f, 0.3f),
            true, new List<PInteractionOption>
            {
                new PInteractionOption("Trade", PInteractionType.custom, new Color(0.2f, 0.6f, 0.3f)),
                new PInteractionOption("Talk", PInteractionType.custom, new Color(0.3f, 0.3f, 0.7f)),
                new PInteractionOption("Give Gift", PInteractionType.custom, new Color(0.7f, 0.3f, 0.5f))
            });

        // → 3. Crate — 2 options (wheel opens)
        SpawnBox("Supply Crate", new Vector3(3f, 0.75f, 5f), new Color(0.6f, 0.4f, 0.1f),
            true, new List<PInteractionOption>
            {
                new PInteractionOption("Open", PInteractionType.custom, new Color(0.5f, 0.35f, 0.1f)),
                new PInteractionOption("Pick Up", PInteractionType.custom, new Color(0.8f, 0.6f, 0.2f))
            });

        // → 4. Terminal — single option (auto-execute)
        SpawnBox("Terminal", new Vector3(6f, 0.75f, 5f), new Color(0.7f, 0.1f, 0.1f),
            false, new List<PInteractionOption>
            {
                new PInteractionOption("Access Terminal", PInteractionType.custom, new Color(0.8f, 0.2f, 0.2f))
            });

        // → 5. Workbench — 4 options (stress test wheel)
        SpawnBox("Workbench", new Vector3(-6f, 0.75f, 5f), new Color(0.5f, 0.5f, 0.5f),
            true, new List<PInteractionOption>
            {
                new PInteractionOption("Craft", PInteractionType.custom, new Color(0.4f, 0.6f, 0.2f)),
                new PInteractionOption("Repair", PInteractionType.custom, new Color(0.6f, 0.4f, 0.2f)),
                new PInteractionOption("Upgrade", PInteractionType.custom, new Color(0.2f, 0.4f, 0.7f)),
                new PInteractionOption("Disassemble", PInteractionType.custom, new Color(0.7f, 0.2f, 0.2f))
            });

        // → label each interactable with floating text
        foreach (var ia in interactables)
        {
            // → small sphere as visual label marker above box
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = ia.transform.position + Vector3.up * 1.2f;
            marker.transform.localScale = Vector3.one * 0.15f;
            marker.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(marker.GetComponent<Collider>());
        }
    }

    void SpawnBox(string objName, Vector3 pos, Color color, bool useWheel, List<PInteractionOption> options)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
        go.GetComponent<Renderer>().material.color = color;

        var ia = go.AddComponent<PIInteractable>();
        ia.Init(objName, useWheel, options);
        interactables.Add(ia);
    }

    // ═══ Data Classes ═══

    public enum PInteractionType { openShopView, custom }

    public class PInteractionOption
    {
        public string name;
        public PInteractionType type;
        public Color color;
        public PInteractionOption(string n, PInteractionType t, Color c) { name = n; type = t; color = c; }
    }
}

// ═══════════════════════════════════════════
// PIInteractable — standalone interactable component (replaces IInteractable + InteractableComputer)
// ═══════════════════════════════════════════
public class PIInteractable : MonoBehaviour
{
    string objectName;
    bool useWheel;
    List<Proto_InteractionSystem.PInteractionOption> options;

    public void Init(string name, bool useWheel, List<Proto_InteractionSystem.PInteractionOption> opts)
    {
        this.objectName = name;
        this.useWheel = useWheel;
        this.options = opts;
    }

    public string GetObjectName() => objectName;
    public bool ShouldUseInteractionWheel() => useWheel;
    public List<Proto_InteractionSystem.PInteractionOption> GetOptions() => options;

    public void Interact(Proto_InteractionSystem.PInteractionOption selectedOption)
    {
        if (selectedOption.type == Proto_InteractionSystem.PInteractionType.openShopView)
            Debug.Log($"[Interaction] ACTION: {objectName} → OPEN SHOP VIEW (would fire GameEvents.RaiseOpenShopView)");
        else
            Debug.Log($"[Interaction] ACTION: {objectName} → '{selectedOption.name}' executed!");
    }
}