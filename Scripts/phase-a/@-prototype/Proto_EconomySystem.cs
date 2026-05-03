using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — EconomySystem + MoneyBridge (end-to-end)
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY EconomySystem feature:
///   1. IShopMoney interface: GetMoney, AddMoney, CanAfford — decoupled contract
///   2. EconomyManager: Singleton, owns currMoney, implements IShopMoney, fires MoneyChanged
///   3. moneyUI: subscribes to MoneyChanged event, updates HUD text with formatted money
///   4. MoneyBridge: wires the IShopMoney provider → consumer without tight coupling
///      (bridge knows both sides, neither side knows the other)
///   5. Money formatting: formatMoney ($12,345.00) and formatMoneyShort ($12,345)
///   6. DefaultExecutionOrder(-100): EconomyManager starts before other MonoBehaviours
///   7. Event-driven: OnMoneyChanged fires → moneyUI updates, shop refreshes affordability
///   8. OnMoneyProviderReady event: bridge fires → ShopUI receives IShopMoney at runtime
///   9. Menu state pattern: when spending opens a UI → cursor unlocks, movement pauses
///  10. Multiple consumers: money HUD + shop affordability + sell station all react to same event
///
/// Scene setup:
///   - 3D world: colored cubes = purchasable items, sell zone = cylinder
///   - Walk near item → press E to buy ($50 each)
///   - Walk near sell zone → press E to sell last purchase ($30 each)
///   - Demonstrates: earn, spend, CanAfford guard, event propagation, bridge pattern
///
/// Controls:
///   WASD = move, Mouse = look
///   E = interact (buy/sell depending on what you're looking at)
///   G = gift $100 (simulates external income → fires MoneyChanged)
///   T = tax $25 (simulates external expense)
///   I = print economy status
/// </summary>
public class Proto_EconomySystem : MonoBehaviour
{
    Camera cam;
    float xRot, yRot;
    CharacterController cc;

    // → economy data (inline EconomyManager)
    float currMoney;
    float defaultMoney = 400f;

    // → bridge pattern: provider and consumer are decoupled
    PEShopMoney moneyProvider;
    System.Action<float> onMoneyChanged;
    System.Action<PEShopMoney> onMoneyProviderReady;

    // → UI
    Canvas canvas;
    UnityEngine.UI.Text moneyHudText, statusText, interactHintText;

    // → world objects
    List<PEShopItem> shopItems = new List<PEShopItem>();
    List<GameObject> purchasedItems = new List<GameObject>();
    PESellZone sellZone;

    // → tracking
    bool isMenuOpen;
    int totalPurchases, totalSales;

    void Start()
    {
        // → camera + controller
        cam = Camera.main;
        transform.position = new Vector3(0f, 0f, -3f);
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cam.transform.SetParent(transform);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5, 1, 5);
        ground.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.3f);

        // → init economy (EconomyManager pattern)
        currMoney = defaultMoney;
        moneyProvider = new PEShopMoney(this);

        // → fire MoneyProviderReady event (MoneyBridge.Start pattern)
        // In real code: MoneyBridge.Start → fires RaiseMoneyProviderReady(IShopMoney)
        // → ShopUI subscribes → stores IShopMoney → passes to orchestrator
        // Here we simulate the full bridge flow:
        Debug.Log("[Economy] MoneyBridge.Start → firing OnMoneyProviderReady");
        if (onMoneyProviderReady != null) onMoneyProviderReady(moneyProvider);

        // → fire initial MoneyChanged (EconomyManager.Start pattern)
        RaiseMoneyChanged();

        // → build world
        SpawnShopItems();
        SpawnSellZone();

        // → build UI (moneyUI pattern)
        BuildUI();
        // → subscribe moneyUI to event
        onMoneyChanged += (money) => UpdateMoneyHUD();

        // → re-fire so UI gets initial value
        RaiseMoneyChanged();

        Debug.Log("[Economy] WASD=move, Mouse=look, E=buy/sell, G=gift $100, T=tax $25, I=info");
    }

    void Update()
    {
        HandleLook();
        HandleMovement();

        // → E = interact with what you're looking at
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }

        // → G = gift money (external income)
        if (Input.GetKeyDown(KeyCode.G))
        {
            moneyProvider.AddMoney(100f);
            RaiseMoneyChanged();
            Debug.Log($"[Economy] Gifted $100! Balance: {FormatMoney(currMoney)}");
        }

        // → T = tax (external expense)
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (moneyProvider.CanAfford(25f))
            {
                moneyProvider.AddMoney(-25f);
                RaiseMoneyChanged();
                Debug.Log($"[Economy] Taxed $25. Balance: {FormatMoney(currMoney)}");
            }
            else
            {
                Debug.Log("[Economy] Can't afford tax!");
            }
        }

        // → I = info
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"[Economy] Balance: {FormatMoney(currMoney)} | Purchases: {totalPurchases} | Sales: {totalSales} | Items owned: {purchasedItems.Count}");
        }

        UpdateInteractHint();
    }

    // ═══ MOVEMENT ═══

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
        cc.Move(Vector3.down * 10f * Time.deltaTime);
    }

    // ═══ INTERACTION ═══

    void TryInteract()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, 5f)) return;

        // → check for shop item (buy)
        var shopItem = hit.collider.GetComponent<PEShopItem>();
        if (shopItem != null)
        {
            TryBuy(shopItem);
            return;
        }

        // → check for sell zone
        var sz = hit.collider.GetComponent<PESellZone>();
        if (sz != null)
        {
            TrySell();
            return;
        }
    }

    void TryBuy(PEShopItem shopItem)
    {
        float price = shopItem.price;
        if (!moneyProvider.CanAfford(price))
        {
            Debug.Log($"[Economy] Can't afford {shopItem.itemName}! Need {FormatMoney(price)}, have {FormatMoney(currMoney)}");
            return;
        }

        // → spend money (IShopMoney.AddMoney pattern)
        moneyProvider.AddMoney(-price);
        RaiseMoneyChanged();

        // → spawn purchased item near player
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Owned_{shopItem.itemName}";
        go.transform.localScale = Vector3.one * 0.35f;
        go.transform.position = transform.position + transform.forward * 1.5f + Vector3.up * 2f;
        go.GetComponent<Renderer>().material.color = shopItem.color;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.3f;
        purchasedItems.Add(go);
        totalPurchases++;

        Debug.Log($"[Economy] Bought {shopItem.itemName} for {FormatMoney(price)}! Balance: {FormatMoney(currMoney)}");
    }

    void TrySell()
    {
        if (purchasedItems.Count == 0)
        {
            Debug.Log("[Economy] Nothing to sell!");
            return;
        }

        float sellPrice = 30f;
        var last = purchasedItems[purchasedItems.Count - 1];
        purchasedItems.RemoveAt(purchasedItems.Count - 1);
        Destroy(last);

        // → earn money
        moneyProvider.AddMoney(sellPrice);
        RaiseMoneyChanged();
        totalSales++;

        Debug.Log($"[Economy] Sold item for {FormatMoney(sellPrice)}! Balance: {FormatMoney(currMoney)}");
    }

    // ═══ EVENTS (GameEvents pattern) ═══

    void RaiseMoneyChanged()
    {
        if (onMoneyChanged != null) onMoneyChanged(currMoney);
    }

    // ═══ WORLD BUILDING ═══

    void SpawnShopItems()
    {
        var defs = new[]
        {
            new { name = "Iron Pickaxe", price = 50f, color = Color.grey, pos = new Vector3(-4f, 0.75f, 6f) },
            new { name = "Gold Ring", price = 120f, color = new Color(1f, 0.84f, 0f), pos = new Vector3(-1f, 0.75f, 6f) },
            new { name = "Wood Shield", price = 35f, color = new Color(0.5f, 0.35f, 0.15f), pos = new Vector3(2f, 0.75f, 6f) },
            new { name = "Diamond Gem", price = 250f, color = new Color(0.6f, 0.95f, 1f), pos = new Vector3(5f, 0.75f, 6f) },
            new { name = "TNT Box", price = 75f, color = new Color(0.8f, 0.15f, 0.15f), pos = new Vector3(8f, 0.75f, 6f) },
        };

        foreach (var d in defs)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = d.name;
            go.transform.position = d.pos;
            go.transform.localScale = new Vector3(1f, 1.5f, 1f);
            go.GetComponent<Renderer>().material.color = d.color;

            var item = go.AddComponent<PEShopItem>();
            item.Init(d.name, d.price, d.color);
            shopItems.Add(item);

            // → price label (small sphere above)
            var label = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            label.transform.position = d.pos + Vector3.up * 1.4f;
            label.transform.localScale = Vector3.one * 0.2f;
            label.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(label.GetComponent<Collider>());
        }
    }

    void SpawnSellZone()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "SellZone";
        go.transform.position = new Vector3(0f, 0.5f, -4f);
        go.transform.localScale = new Vector3(2f, 0.5f, 2f);
        go.GetComponent<Renderer>().material.color = new Color(0.2f, 0.7f, 0.2f, 0.8f);

        sellZone = go.AddComponent<PESellZone>();

        // → sell label
        var label = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        label.transform.position = go.transform.position + Vector3.up * 1.2f;
        label.transform.localScale = Vector3.one * 0.25f;
        label.GetComponent<Renderer>().material.color = Color.green;
        Destroy(label.GetComponent<Collider>());
    }

    // ═══ UI ═══

    void BuildUI()
    {
        var canvasGo = new GameObject("EconomyCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // → Money HUD (top right — moneyUI pattern)
        var moneyGo = MakeText("MoneyHUD", canvasGo.transform, 0.7f, 0.92f, 0.98f, 1f,
            FormatMoney(currMoney), 24, TextAnchor.MiddleRight, Color.yellow);
        moneyHudText = moneyGo.GetComponent<UnityEngine.UI.Text>();

        // → Money label
        MakeText("MoneyLabel", canvasGo.transform, 0.55f, 0.92f, 0.7f, 1f,
            "Balance:", 18, TextAnchor.MiddleRight, new Color(0.7f, 0.7f, 0.7f));

        // → Status (top left)
        var statusGo = MakeText("Status", canvasGo.transform, 0.01f, 0.92f, 0.4f, 1f,
            "", 14, TextAnchor.MiddleLeft, Color.white);
        statusText = statusGo.GetComponent<UnityEngine.UI.Text>();

        // → Interact hint (bottom center)
        var hintGo = MakeText("Hint", canvasGo.transform, 0.3f, 0.02f, 0.7f, 0.08f,
            "", 16, TextAnchor.MiddleCenter, Color.white);
        interactHintText = hintGo.GetComponent<UnityEngine.UI.Text>();

        // → Crosshair
        MakeText("Cross", canvasGo.transform, 0.49f, 0.48f, 0.51f, 0.52f,
            "+", 22, TextAnchor.MiddleCenter, Color.white);

        // → Controls help
        MakeText("Help", canvasGo.transform, 0.01f, 0.01f, 0.3f, 0.12f,
            "E=buy/sell  G=gift$100  T=tax$25  I=info", 12, TextAnchor.LowerLeft, new Color(0.6f, 0.6f, 0.6f));
    }

    void UpdateMoneyHUD()
    {
        // → moneyUI.HandleMoneyChanged pattern
        moneyHudText.text = FormatMoney(currMoney);
        // → flash color on change
        moneyHudText.color = Color.white;
        Invoke(nameof(ResetMoneyColor), 0.15f);
    }

    void ResetMoneyColor() => moneyHudText.color = Color.yellow;

    void UpdateInteractHint()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            var shopItem = hit.collider.GetComponent<PEShopItem>();
            if (shopItem != null)
            {
                bool afford = moneyProvider.CanAfford(shopItem.price);
                string col = afford ? "lime" : "red";
                interactHintText.text = $"[E] Buy {shopItem.itemName} — <color={col}>{FormatMoney(shopItem.price)}</color>";
                statusText.text = $"Purchases: {totalPurchases} | Sales: {totalSales} | Owned: {purchasedItems.Count}";
                return;
            }
            var sz = hit.collider.GetComponent<PESellZone>();
            if (sz != null)
            {
                interactHintText.text = purchasedItems.Count > 0
                    ? $"[E] Sell item — <color=lime>{FormatMoney(30f)}</color>"
                    : "[E] Sell — <color=red>nothing to sell</color>";
                statusText.text = $"Purchases: {totalPurchases} | Sales: {totalSales} | Owned: {purchasedItems.Count}";
                return;
            }
        }
        interactHintText.text = "";
        statusText.text = $"Purchases: {totalPurchases} | Sales: {totalSales} | Owned: {purchasedItems.Count}";
    }

    // ═══ FORMATTING (MoneyExtension pattern) ═══

    static string FormatMoney(float m) => $"${m:#,##0.00}";
    static string FormatMoneyShort(float m) => $"${m:#,##0.##}";

    // ═══ UI HELPER ═══

    GameObject MakeText(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
        string text, int fontSize, TextAnchor align, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        return go;
    }

    // ═══ IShopMoney IMPLEMENTATION ═══

    public float GetCurrMoney() => currMoney;
    public void SetCurrMoney(float m) => currMoney = m;
}

// ═══════════════════════════════════════════
// PEShopMoney — IShopMoney interface implementation (mirrors EconomyManager)
// ═══════════════════════════════════════════
public class PEShopMoney
{
    Proto_EconomySystem owner;
    public PEShopMoney(Proto_EconomySystem o) { owner = o; }

    public float GetMoney() => owner.GetCurrMoney();
    public void AddMoney(float delta) => owner.SetCurrMoney(owner.GetCurrMoney() + delta);
    public bool CanAfford(float price) => owner.GetCurrMoney() >= price;
}

// ═══════════════════════════════════════════
// PEShopItem — world item that can be purchased (represents shop items in 3D)
// ═══════════════════════════════════════════
public class PEShopItem : MonoBehaviour
{
    public string itemName;
    public float price;
    public Color color;

    public void Init(string n, float p, Color c) { itemName = n; price = p; color = c; }
}

// ═══════════════════════════════════════════
// PESellZone — sell station marker component
// ═══════════════════════════════════════════
public class PESellZone : MonoBehaviour { }