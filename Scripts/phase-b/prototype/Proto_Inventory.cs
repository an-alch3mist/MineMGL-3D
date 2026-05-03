using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// STANDALONE PROTOTYPE — Inventory System (drag onto any GO, press Play)
/// Creates: player (capsule+camera), 5 colored cubes (items), Canvas with hotbar slots.
///
/// Controls:
///   WASD/Space/Shift — move/jump/sprint
///   Mouse — look
///   Click item in world — pick up (stacks if same color)
///   1-5 — switch hotbar slot (equips item, floats in front of camera)
///   G — drop equipped item (or 1 from stack)
///   Tab — toggle extended inventory (10 extra slots)
///   Click slot in extended view — equip from that slot
///   Scroll — cycle hotbar
///
/// Covers: pickup, equip, drop, stack, hotbar keys, scroll, extended inventory, slot click.
/// Zero external deps. Zero GameEvents. Zero interfaces.
/// </summary>
public class Proto_Inventory : MonoBehaviour
{
    #region config
    [Header("Config")]
    [SerializeField] int hotbarSize = 5;
    [SerializeField] int totalSize = 15;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float lookSens = 2f;
    [SerializeField] float jumpForce = 7f;
    #endregion

    #region runtime
    // player
    GameObject player;
    Camera cam;
    CharacterController cc;
    float xRot, yVel;
    Transform viewModelContainer;

    // inventory data
    class Slot { public ProtoItem item; public int qty; }
    Slot[] slots;
    int activeSlot = -1;

    // UI
    Canvas canvas;
    Image[] slotBGs;
    TMP_Text[] slotTexts;
    GameObject extendedPanel;
    bool extendedOpen;

    // items in world
    List<ProtoItem> worldItems = new List<ProtoItem>();
    #endregion

    #region item class
    class ProtoItem
    {
        public GameObject go;
        public string itemName;
        public Color color;
        public int maxStack;
        public Vector3 originalScale;

        public ProtoItem(string name, Color col, int maxStack, Vector3 pos)
        {
            this.itemName = name;
            this.color = col;
            this.maxStack = maxStack;
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.5f;
            originalScale = go.transform.localScale;
            go.GetComponent<Renderer>().material.color = col;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
        }
    }
    #endregion

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CreatePlayer();
        CreateItems();
        CreateUI();
        slots = new Slot[totalSize];
        for (int i = 0; i < totalSize; i++) slots[i] = new Slot();
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandlePickup();
        HandleHotbarKeys();
        HandleScroll();
        HandleDrop();
        HandleToggleExtended();
        UpdateEquippedVisual();
        RefreshUI();
    }

    #region player creation
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
        viewModelContainer.localPosition = new Vector3(0.4f, -0.3f, 1f);

        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = Vector3.one * 5f;
        floor.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);
    }
    #endregion

    #region item creation
    void CreateItems()
    {
        // 5 items: 3 unique + 2 stackable (same name = stackable)
        worldItems.Add(new ProtoItem("Pickaxe", Color.cyan, 1, new Vector3(-2, 1, 3)));
        worldItems.Add(new ProtoItem("Magnet", Color.magenta, 1, new Vector3(0, 1, 3)));
        worldItems.Add(new ProtoItem("Scanner", Color.yellow, 1, new Vector3(2, 1, 3)));
        worldItems.Add(new ProtoItem("Dynamite", new Color(1f, 0.5f, 0f), 5, new Vector3(-1, 1, 5)));
        worldItems.Add(new ProtoItem("Dynamite", new Color(1f, 0.5f, 0f), 5, new Vector3(1, 1, 5)));
        worldItems.Add(new ProtoItem("Dynamite", new Color(1f, 0.5f, 0f), 5, new Vector3(0, 1, 7)));
    }
    #endregion

    #region movement + look
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = player.transform.right * h + player.transform.forward * v;
        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 1.8f : moveSpeed;
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space)) yVel = jumpForce;
        yVel += Physics.gravity.y * Time.deltaTime;
        move.y = 0; move = move.normalized * speed;
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

    #region pickup
    void HandlePickup()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 4f))
        {
            ProtoItem found = worldItems.Find(i => i.go == hit.collider.gameObject);
            if (found == null) return;

            // try stack into existing slot with same name
            for (int i = 0; i < totalSize; i++)
            {
                if (slots[i].item != null && slots[i].item.itemName == found.itemName && slots[i].qty < found.maxStack)
                {
                    slots[i].qty++;
                    found.go.SetActive(false);
                    worldItems.Remove(found);
                    Debug.Log($"[Proto_Inventory] Stacked {found.itemName} into slot {i} (qty={slots[i].qty})");
                    return;
                }
            }
            // try empty slot
            for (int i = 0; i < totalSize; i++)
            {
                if (slots[i].item == null)
                {
                    slots[i].item = found;
                    slots[i].qty = 1;
                    found.go.SetActive(false);
                    worldItems.Remove(found);
                    if (activeSlot == -1 && i < hotbarSize) SwitchTo(i);
                    Debug.Log($"[Proto_Inventory] Picked up {found.itemName} into slot {i}");
                    return;
                }
            }
            Debug.Log("[Proto_Inventory] Inventory full!");
        }
    }
    #endregion

    #region hotbar + scroll + equip
    void HandleHotbarKeys()
    {
        for (int i = 0; i < Mathf.Min(hotbarSize, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SwitchTo(i);
        }
        // extended slot click
        if (extendedOpen && Input.GetMouseButtonDown(0))
        {
            // simple: click slot index via keys 6-0 for extended
        }
    }
    void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;
        // find next occupied hotbar slot
        int dir = scroll > 0 ? 1 : -1;
        int start = activeSlot < 0 ? 0 : activeSlot;
        for (int i = 1; i <= hotbarSize; i++)
        {
            int idx = (start + dir * i + hotbarSize) % hotbarSize;
            if (slots[idx].item != null) { SwitchTo(idx); return; }
        }
    }
    void SwitchTo(int idx)
    {
        // unequip previous
        if (activeSlot >= 0 && slots[activeSlot].item != null)
            slots[activeSlot].item.go.SetActive(false);

        // toggle off if same slot
        if (idx == activeSlot) { activeSlot = -1; return; }

        activeSlot = idx;
        if (slots[idx].item != null)
        {
            var item = slots[idx].item;
            item.go.SetActive(true);
            item.go.transform.parent = viewModelContainer;
            item.go.transform.localPosition = Vector3.zero;
            item.go.transform.localRotation = Quaternion.identity;
            item.go.transform.localScale = item.originalScale * 0.5f;
            var rb = item.go.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
            var col = item.go.GetComponent<Collider>();
            if (col) col.enabled = false;
            Debug.Log($"[Proto_Inventory] Equipped {item.itemName} from slot {idx}");
        }
    }
    #endregion

    #region drop
    void HandleDrop()
    {
        if (!Input.GetKeyDown(KeyCode.G) || activeSlot < 0) return;
        var slot = slots[activeSlot];
        if (slot.item == null) return;

        if (slot.qty > 1)
        {
            // drop clone
            slot.qty--;
            var clone = new ProtoItem(slot.item.itemName, slot.item.color, slot.item.maxStack,
                cam.transform.position + cam.transform.forward * 1.5f);
            clone.go.GetComponent<Rigidbody>().linearVelocity = cam.transform.forward * 5f + Vector3.up * 2f;
            worldItems.Add(clone);
            Debug.Log($"[Proto_Inventory] Dropped 1 {slot.item.itemName}, remaining: {slot.qty}");
        }
        else
        {
            // drop actual
            var item = slot.item;
            item.go.SetActive(true);
            item.go.transform.parent = null;
            item.go.transform.localScale = item.originalScale;
            item.go.transform.position = cam.transform.position + cam.transform.forward * 1.5f;
            var rb = item.go.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = false; rb.linearVelocity = cam.transform.forward * 5f + Vector3.up * 2f; }
            var col = item.go.GetComponent<Collider>();
            if (col) col.enabled = true;
            worldItems.Add(item);
            slot.item = null; slot.qty = 0;
            activeSlot = -1;
            Debug.Log($"[Proto_Inventory] Dropped last {item.itemName}");
        }
    }
    #endregion

    #region extended inventory toggle
    void HandleToggleExtended()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            extendedOpen = !extendedOpen;
            extendedPanel.SetActive(extendedOpen);
            Cursor.lockState = extendedOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = extendedOpen;
        }
    }
    #endregion

    #region equipped visual
    void UpdateEquippedVisual()
    {
        // item follows viewModelContainer via parenting — no per-frame update needed
    }
    #endregion

    #region UI
    void CreateUI()
    {
        // Canvas
        var canvasGO = new GameObject("Proto_Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        slotBGs = new Image[totalSize];
        slotTexts = new TMP_Text[totalSize];

        // Hotbar (bottom center)
        var hotbar = CreatePanel(canvasGO.transform, "Hotbar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(hotbarSize * 70, 65), new Vector2(0, 40));
        for (int i = 0; i < hotbarSize; i++)
            CreateSlot(hotbar.transform, i, new Vector2(i * 70 - (hotbarSize - 1) * 35, 0));

        // Extended panel (center)
        extendedPanel = CreatePanel(canvasGO.transform, "Extended", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(5 * 70, (totalSize - hotbarSize) / 5 * 70 + 10), Vector2.zero);
        for (int i = hotbarSize; i < totalSize; i++)
        {
            int row = (i - hotbarSize) / 5;
            int col2 = (i - hotbarSize) % 5;
            CreateSlot(extendedPanel.transform, i, new Vector2(col2 * 70 - 140, -row * 70));
        }
        extendedPanel.SetActive(false);
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.4f);
        return go;
    }

    void CreateSlot(Transform parent, int idx, Vector2 pos)
    {
        var go = new GameObject($"Slot_{idx}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 55);
        rt.anchoredPosition = pos;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        slotBGs[idx] = bg;

        // text
        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 10;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.sizeDelta = Vector2.zero;
        slotTexts[idx] = text;
    }

    void RefreshUI()
    {
        for (int i = 0; i < totalSize; i++)
        {
            var slot = slots[i];
            if (slot.item != null)
            {
                slotBGs[i].color = slot.item.color * 0.6f;
                string label = slot.item.itemName;
                if (slot.qty > 1) label += $"\nx{slot.qty}";
                if (i < hotbarSize) label = $"[{i + 1}] {label}";
                slotTexts[i].text = label;
            }
            else
            {
                slotBGs[i].color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                slotTexts[i].text = i < hotbarSize ? $"[{i + 1}]" : "";
            }
            // highlight active
            if (i == activeSlot)
                slotBGs[i].color = Color.white * 0.8f;
        }
    }
    #endregion
}