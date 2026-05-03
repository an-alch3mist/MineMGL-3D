using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — ShopUISystem (end-to-end)
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Covers EVERY ShopUISystem feature:
///   1. SO_ShopCategory data (inline): categoryName, hideIfAllItemsLocked, list of item defs
///   2. SO_ShopItemDef data (inline): name, descr, price, isDefaultLocked, maxStackable, prefab color
///   3. WShopItem wrapper: isLockedCurr, timesPurchased, IsNewlyUnlocked()
///   4. ShopDataService: BuildCategories, GetWShopItems, TryAddNewCartItem, RemoveCartItem,
///      AlterCartItemQty, IncreaseCartItemQty, ClearCartItems, GetCartTotalPrice, CanAffordCartItems,
///      shouldCategoryBeHiddenInView, UnlockWShopItem, UnlockEntireCategory
///   5. ShopUI: OnEnable/OnDisable lifecycle, isFirstEnable pattern, toggle via events
///   6. ShopUIOrchestrator: BuildCategoryView, RepopulateShopItems, CreateCartItemFields,
///      SelectCategoryView (tab highlight), OrchestratePurchaseButton, RefreshAllRequired
///   7. Field_ShopCategory: tab button with selected/normal color
///   8. Field_ShopItem: name, descr, price, add-to-cart button, locked state coloring
///   9. Field_ShopCartItem: name, qty +/- buttons, remove button, price display
///  10. Cart total price display with can-afford color (green/red)
///  11. Purchase button: disabled when cart empty or can't afford
///  12. SpawnAtPoint: purchased items spawn as cubes at random spawn points with offset
///  13. ShopSpawnPoint: static GetRandomSpawnPoint from all spawn points in scene
///  14. Category tab hiding when all items locked (hideIfAllItemsLocked)
///  15. Unlock category event → unhide tab + unlock all items
///  16. Menu state changed event pattern (open/close → cursor lock toggle)
///  17. IShopMoney interface usage (money provider injected, not direct singleton)
///
/// Controls:
///   E = open shop
///   ESC = close shop
///   U = unlock "Premium" category (tests unlock flow)
///   M = add $500 money (tests money refresh)
///   I = print snapshot (categories, cart, money)
/// </summary>
public class Proto_ShopSystem : MonoBehaviour
{
    Camera cam;
    bool shopOpen;
    float money = 500f;

    // → data
    List<PSCategory> categories = new List<PSCategory>();
    PSDataService dataService;

    // → UI roots
    Canvas canvas;
    GameObject shopPanel;
    GameObject categoryPanel, itemPanel, cartPanel, footerPanel;
    UnityEngine.UI.Text totalPriceText, moneyHudText, statusText;
    UnityEngine.UI.Button purchaseBtn;
    UnityEngine.UI.Text purchaseBtnText;

    // → tracking
    PSCategory selectedCategory;
    Dictionary<PSCategory, GameObject> categoryTabs = new Dictionary<PSCategory, GameObject>();
    List<GameObject> itemRows = new List<GameObject>();
    Dictionary<PSDataService.CartItem, GameObject> cartRows = new Dictionary<PSDataService.CartItem, GameObject>();

    // → spawn points
    List<Vector3> spawnPoints = new List<Vector3>();

    Color canAffordColor = new Color(0.2f, 0.8f, 0.2f);
    Color cannotAffordColor = new Color(0.8f, 0.2f, 0.2f);
    Color selectedTabColor = new Color(0.3f, 0.6f, 1f);
    Color normalTabColor = new Color(0.25f, 0.25f, 0.25f);
    Color canBuyColor = new Color(0.3f, 0.7f, 0.3f);
    Color cannotBuyColor = new Color(0.5f, 0.2f, 0.2f);

    void Start()
    {
        cam = Camera.main;
        cam.transform.position = new Vector3(0f, 5f, -10f);
        cam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        // → ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3, 1, 3);
        ground.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.25f);

        // → spawn points (ShopSpawnPoint pattern)
        for (int i = 0; i < 3; i++)
        {
            var sp = new Vector3(-3 + i * 3, 0.5f, 3f);
            spawnPoints.Add(sp);
            // → visual marker
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = sp;
            marker.transform.localScale = Vector3.one * 0.3f;
            marker.GetComponent<Renderer>().material.color = Color.green;
            Destroy(marker.GetComponent<Collider>());
        }

        // → build categories + items data
        BuildData();
        dataService = new PSDataService();
        dataService.BuildCategories(categories, money);

        // → build UI
        BuildCanvas();
        BuildShopUI();
        BuildMoneyHUD();
        BuildStatusHUD();

        shopPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[Shop] E=open, ESC=close, U=unlock premium, M=add $500, I=info");
    }

    void Update()
    {
        Cursor.lockState = shopOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shopOpen;

        if (Input.GetKeyDown(KeyCode.E) && !shopOpen) OpenShop();
        if (Input.GetKeyDown(KeyCode.Escape) && shopOpen) CloseShop();

        // → U = unlock "Premium" category
        if (Input.GetKeyDown(KeyCode.U))
        {
            var premium = categories.Find(c => c.name == "Premium");
            if (premium != null)
            {
                dataService.UnlockEntireCategory(premium);
                Debug.Log("[Shop] Unlocked Premium category!");
                if (shopOpen) RebuildCategoryView();
            }
        }

        // → M = add money
        if (Input.GetKeyDown(KeyCode.M))
        {
            money += 500f;
            dataService.SetMoney(money);
            UpdateMoneyHUD();
            if (shopOpen) RefreshAll();
            Debug.Log($"[Shop] Added $500. Balance: ${money:F2}");
        }

        // → I = info
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log(dataService.GetSnapshot(money));
        }

        UpdateStatusHUD();
    }

    void OpenShop()
    {
        shopOpen = true;
        shopPanel.SetActive(true);
        RebuildCategoryView();
        RefreshAll();
        Debug.Log("[Shop] MenuStateChanged → open (cursor unlocked)");
    }

    void CloseShop()
    {
        shopOpen = false;
        shopPanel.SetActive(false);
        Debug.Log("[Shop] MenuStateChanged → closed (cursor locked)");
    }

    // ═══ DATA ═══

    void BuildData()
    {
        // → Category: Tools (always visible, no locked items)
        var tools = new PSCategory("Tools", false, new List<PSItemDef>
        {
            new PSItemDef("Pickaxe", "Basic mining pickaxe", 50f, false, Color.grey, 1),
            new PSItemDef("Shovel", "Digs dirt fast", 35f, false, new Color(0.5f, 0.35f, 0.15f), 1),
            new PSItemDef("Hammer", "Build and repair", 75f, false, new Color(0.6f, 0.6f, 0.6f), 1),
        });

        // → Category: Materials (visible, some locked)
        var materials = new PSCategory("Materials", false, new List<PSItemDef>
        {
            new PSItemDef("Wood Plank", "Standard building material", 10f, false, new Color(0.6f, 0.4f, 0.2f), 99),
            new PSItemDef("Iron Bar", "Refined iron ingot", 25f, false, new Color(0.7f, 0.7f, 0.7f), 50),
            new PSItemDef("Gold Bar", "Precious metal", 100f, true, new Color(1f, 0.84f, 0f), 20),
            new PSItemDef("Diamond", "Rare gemstone", 250f, true, new Color(0.6f, 0.95f, 1f), 10),
        });

        // → Category: Premium (hidden until unlocked — hideIfAllItemsLocked=true)
        var premium = new PSCategory("Premium", true, new List<PSItemDef>
        {
            new PSItemDef("TNT Box", "Explosive mining tool", 200f, true, new Color(0.8f, 0.1f, 0.1f), 5),
            new PSItemDef("Jet Pack", "Fly anywhere", 500f, true, new Color(0.2f, 0.2f, 0.8f), 1),
            new PSItemDef("Auto Miner", "Mines ore automatically", 350f, true, new Color(0.9f, 0.5f, 0f), 3),
        });

        categories.Add(tools);
        categories.Add(materials);
        categories.Add(premium);
    }

    // ═══ UI BUILDING ═══

    void BuildCanvas()
    {
        var canvasGo = new GameObject("ShopCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    void BuildShopUI()
    {
        // → Shop Panel (main container)
        shopPanel = MakePanel("ShopPanel", canvas.transform, 0.05f, 0.05f, 0.95f, 0.95f, new Color(0.08f, 0.08f, 0.12f, 0.95f));

        // → Title bar
        var titleGo = MakeText("Title", shopPanel.transform, 0f, 0.92f, 1f, 1f, "S H O P", 28, TextAnchor.MiddleCenter, Color.white);

        // → Category panel (left side tabs)
        categoryPanel = MakePanel("Categories", shopPanel.transform, 0.02f, 0.08f, 0.18f, 0.9f, new Color(0.12f, 0.12f, 0.16f, 0.9f));

        // → Item panel (center)
        itemPanel = MakePanel("Items", shopPanel.transform, 0.2f, 0.08f, 0.58f, 0.9f, new Color(0.1f, 0.1f, 0.14f, 0.9f));

        // → Cart panel (right side)
        cartPanel = MakePanel("Cart", shopPanel.transform, 0.6f, 0.15f, 0.98f, 0.9f, new Color(0.12f, 0.12f, 0.16f, 0.9f));
        MakeText("CartTitle", cartPanel.transform, 0f, 0.92f, 1f, 1f, "CART", 20, TextAnchor.MiddleCenter, Color.white);

        // → Footer (total + purchase button)
        footerPanel = MakePanel("Footer", shopPanel.transform, 0.6f, 0.05f, 0.98f, 0.14f, new Color(0.15f, 0.15f, 0.2f, 0.9f));

        // → Total price text
        var totalGo = MakeText("Total", footerPanel.transform, 0.02f, 0.5f, 0.5f, 1f, "$0.00", 18, TextAnchor.MiddleLeft, canAffordColor);
        totalPriceText = totalGo.GetComponent<UnityEngine.UI.Text>();

        // → Purchase button
        var purchGo = MakeButton("PurchaseBtn", footerPanel.transform, 0.55f, 0.1f, 0.95f, 0.9f, "PURCHASE", new Color(0.2f, 0.6f, 0.2f), () =>
        {
            PurchaseAllCartItems();
        });
        purchaseBtn = purchGo.GetComponent<UnityEngine.UI.Button>();
        purchaseBtnText = purchGo.GetComponentInChildren<UnityEngine.UI.Text>();
    }

    void RebuildCategoryView()
    {
        // → destroy old tabs
        foreach (var kvp in categoryTabs) Destroy(kvp.Value);
        categoryTabs.Clear();

        float y = 0.88f;
        float tabHeight = 0.08f;
        PSCategory firstVisible = null;

        foreach (var cat in categories)
        {
            if (dataService.ShouldCategoryBeHidden(cat)) continue;

            var tabGo = MakeButton($"Tab_{cat.name}", categoryPanel.transform,
                0.05f, y - tabHeight, 0.95f, y, cat.name, normalTabColor, null);
            categoryTabs[cat] = tabGo;

            var capturedCat = cat;
            tabGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectCategory(capturedCat));

            if (firstVisible == null) firstVisible = cat;
            y -= tabHeight + 0.02f;
        }

        if (firstVisible != null) SelectCategory(firstVisible);
    }

    void SelectCategory(PSCategory cat)
    {
        selectedCategory = cat;
        // → highlight selected tab
        foreach (var kvp in categoryTabs)
        {
            var img = kvp.Value.GetComponent<UnityEngine.UI.Image>();
            img.color = (kvp.Key == cat) ? selectedTabColor : normalTabColor;
        }
        RepopulateItems();
    }

    void RepopulateItems()
    {
        // → destroy old item rows
        foreach (var r in itemRows) Destroy(r);
        itemRows.Clear();

        var wItems = dataService.GetWShopItems(selectedCategory);
        float y = 0.95f;
        float rowH = 0.12f;

        foreach (var wItem in wItems)
        {
            var rowGo = MakePanel($"Item_{wItem.def.name}", itemPanel.transform,
                0.02f, y - rowH, 0.98f, y, new Color(0.15f, 0.15f, 0.2f, 0.85f));

            // → color swatch (icon placeholder)
            var swatch = MakePanel("Swatch", rowGo.transform, 0.02f, 0.1f, 0.1f, 0.9f, wItem.def.color);

            // → name
            MakeText("Name", rowGo.transform, 0.12f, 0.55f, 0.55f, 0.95f, wItem.def.name, 16, TextAnchor.MiddleLeft, Color.white);

            // → descr
            MakeText("Descr", rowGo.transform, 0.12f, 0.05f, 0.55f, 0.5f, wItem.def.descr, 12, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.7f));

            // → price
            MakeText("Price", rowGo.transform, 0.56f, 0.1f, 0.72f, 0.9f, $"${wItem.def.price:F0}", 16, TextAnchor.MiddleCenter, Color.yellow);

            // → add to cart button
            bool canBuy = !wItem.isLockedCurr;
            string btnLabel = canBuy ? "Add" : "Locked";
            Color btnCol = canBuy ? canBuyColor : cannotBuyColor;
            var btnGo = MakeButton("AddBtn", rowGo.transform, 0.74f, 0.15f, 0.97f, 0.85f, btnLabel, btnCol, null);

            if (canBuy)
            {
                var capturedWItem = wItem;
                btnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    AddToCart(capturedWItem);
                });
            }
            else
            {
                btnGo.GetComponent<UnityEngine.UI.Button>().interactable = false;
            }

            // → newly unlocked indicator
            if (wItem.IsNewlyUnlocked())
            {
                MakeText("New", rowGo.transform, 0.9f, 0.75f, 1f, 1f, "NEW!", 11, TextAnchor.MiddleCenter, Color.cyan);
            }

            itemRows.Add(rowGo);
            y -= rowH + 0.01f;
        }
    }

    void AddToCart(PSWShopItem wItem)
    {
        var cartItem = dataService.TryAddNewCartItem(wItem);

        if (cartRows.ContainsKey(cartItem))
        {
            // → already exists, just update qty
            UpdateCartRowQty(cartItem);
            RefreshAll();
            return;
        }

        // → create new cart row
        float rowH = 0.1f;
        float y = 0.88f - cartRows.Count * (rowH + 0.01f);
        var rowGo = MakePanel($"Cart_{wItem.def.name}", cartPanel.transform,
            0.02f, y - rowH, 0.98f, y, new Color(0.18f, 0.18f, 0.22f, 0.85f));

        // → swatch
        MakePanel("Swatch", rowGo.transform, 0.02f, 0.1f, 0.12f, 0.9f, wItem.def.color);

        // → name
        MakeText("Name", rowGo.transform, 0.14f, 0.5f, 0.5f, 0.95f, wItem.def.name, 14, TextAnchor.MiddleLeft, Color.white);

        // → price
        MakeText("Price", rowGo.transform, 0.14f, 0.05f, 0.5f, 0.48f, $"${wItem.def.price:F0}", 12, TextAnchor.MiddleLeft, Color.yellow);

        // → qty text
        var qtyTextGo = MakeText("Qty", rowGo.transform, 0.52f, 0.2f, 0.64f, 0.8f, cartItem.qty.ToString(), 16, TextAnchor.MiddleCenter, Color.white);
        var qtyText = qtyTextGo.GetComponent<UnityEngine.UI.Text>();

        // → sub button (-)
        var capturedCartItem = cartItem;
        MakeButton("Sub", rowGo.transform, 0.44f, 0.15f, 0.52f, 0.85f, "-", new Color(0.6f, 0.3f, 0.3f), () =>
        {
            dataService.IncreaseCartItemQty(capturedCartItem, -1);
            if (capturedCartItem.qty <= 0)
            {
                RemoveCartRow(capturedCartItem);
            }
            else
            {
                qtyText.text = capturedCartItem.qty.ToString();
            }
            RefreshAll();
        });

        // → add button (+)
        MakeButton("Add", rowGo.transform, 0.64f, 0.15f, 0.72f, 0.85f, "+", new Color(0.3f, 0.6f, 0.3f), () =>
        {
            dataService.IncreaseCartItemQty(capturedCartItem, +1);
            qtyText.text = capturedCartItem.qty.ToString();
            RefreshAll();
        });

        // → remove button (X)
        MakeButton("Rem", rowGo.transform, 0.82f, 0.15f, 0.97f, 0.85f, "X", new Color(0.7f, 0.2f, 0.2f), () =>
        {
            dataService.RemoveCartItem(capturedCartItem);
            RemoveCartRow(capturedCartItem);
            RefreshAll();
        });

        cartRows[cartItem] = rowGo;
        RefreshAll();
    }

    void UpdateCartRowQty(PSDataService.CartItem cartItem)
    {
        if (!cartRows.ContainsKey(cartItem)) return;
        var row = cartRows[cartItem];
        var qtyT = row.transform.Find("Qty");
        if (qtyT != null) qtyT.GetComponent<UnityEngine.UI.Text>().text = cartItem.qty.ToString();
    }

    void RemoveCartRow(PSDataService.CartItem cartItem)
    {
        if (!cartRows.ContainsKey(cartItem)) return;
        Destroy(cartRows[cartItem]);
        cartRows.Remove(cartItem);
        RebuildCartLayout();
    }

    void RebuildCartLayout()
    {
        float rowH = 0.1f;
        int i = 0;
        foreach (var kvp in cartRows)
        {
            float y = 0.88f - i * (rowH + 0.01f);
            var rect = kvp.Value.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, y - rowH);
            rect.anchorMax = new Vector2(0.98f, y);
            i++;
        }
    }

    void PurchaseAllCartItems()
    {
        float totalCost = dataService.GetCartTotalPrice();
        if (totalCost <= 0f) return;
        if (money < totalCost)
        {
            Debug.Log($"[Shop] Cannot afford! Need ${totalCost:F2}, have ${money:F2}");
            return;
        }

        // → spawn purchased items
        var items = new List<PSDataService.CartItem>(dataService.GetCartItems());
        foreach (var ci in items)
        {
            float cost = ci.wShopItem.def.price * ci.qty;
            SpawnAtPoint(ci.wShopItem.def.color, ci.wShopItem.def.name, ci.qty);
            money -= cost;
        }

        // → clear cart
        dataService.ClearCartItems();
        foreach (var kvp in cartRows) Destroy(kvp.Value);
        cartRows.Clear();
        dataService.SetMoney(money);

        UpdateMoneyHUD();
        RefreshAll();
        CloseShop();
        Debug.Log($"[Shop] Purchased! Remaining: ${money:F2}");
    }

    void SpawnAtPoint(Color color, string itemName, int qty)
    {
        Vector3 point = spawnPoints[Random.Range(0, spawnPoints.Count)];
        for (int i = 0; i < qty; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Purchased_{itemName}";
            go.transform.localScale = Vector3.one * 0.4f;
            Vector3 offset = Random.insideUnitSphere; offset.y = Mathf.Abs(offset.y);
            go.transform.position = point + offset * 0.3f;
            go.GetComponent<Renderer>().material.color = color;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
        }
    }

    void RefreshAll()
    {
        float total = dataService.GetCartTotalPrice();
        totalPriceText.text = $"Total: ${total:F2}";
        bool canAfford = money >= total;
        totalPriceText.color = canAfford ? canAffordColor : cannotAffordColor;
        purchaseBtn.interactable = canAfford && dataService.GetCartItems().Count > 0;
        purchaseBtnText.color = purchaseBtn.interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }

    // ═══ HUD ═══

    void BuildMoneyHUD()
    {
        var hudGo = MakeText("MoneyHUD", canvas.transform, 0.8f, 0.93f, 1f, 1f,
            $"${money:F2}", 22, TextAnchor.MiddleRight, Color.yellow);
        moneyHudText = hudGo.GetComponent<UnityEngine.UI.Text>();
    }

    void UpdateMoneyHUD()
    {
        moneyHudText.text = $"${money:F2}";
    }

    void BuildStatusHUD()
    {
        var go = MakeText("Status", canvas.transform, 0f, 0.93f, 0.5f, 1f,
            "", 14, TextAnchor.MiddleLeft, Color.white);
        statusText = go.GetComponent<UnityEngine.UI.Text>();
    }

    void UpdateStatusHUD()
    {
        string s = shopOpen ? "<color=lime>SHOP OPEN</color>" : "<color=red>SHOP CLOSED</color>";
        statusText.text = $"  {s} | Cart: {dataService.GetCartItems().Count} items";
    }

    // ═══ UI HELPERS ═══

    GameObject MakePanel(string name, Transform parent, float xMin, float yMin, float xMax, float yMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = color;
        return go;
    }

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

    GameObject MakeButton(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
        string label, Color bgColor, System.Action onClick)
    {
        var go = MakePanel(name, parent, xMin, yMin, xMax, yMax, bgColor);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = colors;

        MakeText("Label", go.transform, 0f, 0f, 1f, 1f, label, 14, TextAnchor.MiddleCenter, Color.white);

        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return go;
    }

    // ═══ DATA CLASSES ═══

    public class PSItemDef
    {
        public string name, descr;
        public float price;
        public bool isDefaultLocked;
        public Color color;
        public int maxStackable;
        public PSItemDef(string n, string d, float p, bool locked, Color c, int ms)
        { name = n; descr = d; price = p; isDefaultLocked = locked; color = c; maxStackable = ms; }
    }

    public class PSCategory
    {
        public string name;
        public bool hideIfAllItemsLocked;
        public List<PSItemDef> items;
        public PSCategory(string n, bool hide, List<PSItemDef> i)
        { name = n; hideIfAllItemsLocked = hide; items = i; }
    }

    public class PSWShopItem
    {
        public PSItemDef def;
        public bool isLockedCurr;
        public int timesPurchased;
        public PSWShopItem(PSItemDef d) { def = d; isLockedCurr = d.isDefaultLocked; timesPurchased = 0; }
        public bool IsNewlyUnlocked() => !isLockedCurr && timesPurchased == 0 && def.isDefaultLocked;
    }
}

// ═══════════════════════════════════════════
// PSDataService — pure C# data service (mirrors ShopDataService)
// ═══════════════════════════════════════════
public class PSDataService
{
    float money;
    List<Proto_ShopSystem.PSCategory> categories = new List<Proto_ShopSystem.PSCategory>();
    Dictionary<Proto_ShopSystem.PSCategory, List<Proto_ShopSystem.PSWShopItem>> doc = new Dictionary<Proto_ShopSystem.PSCategory, List<Proto_ShopSystem.PSWShopItem>>();
    List<CartItem> cart = new List<CartItem>();

    public class CartItem
    {
        public Proto_ShopSystem.PSWShopItem wShopItem;
        public int qty;
    }

    public void BuildCategories(List<Proto_ShopSystem.PSCategory> cats, float startMoney)
    {
        money = startMoney;
        categories = cats;
        doc.Clear();
        foreach (var cat in cats)
        {
            var list = new List<Proto_ShopSystem.PSWShopItem>();
            foreach (var def in cat.items) list.Add(new Proto_ShopSystem.PSWShopItem(def));
            doc[cat] = list;
        }
    }

    public void SetMoney(float m) => money = m;
    public List<Proto_ShopSystem.PSWShopItem> GetWShopItems(Proto_ShopSystem.PSCategory cat) => doc[cat];

    public bool ShouldCategoryBeHidden(Proto_ShopSystem.PSCategory cat)
    {
        if (!cat.hideIfAllItemsLocked) return false;
        var items = doc[cat];
        foreach (var w in items) if (!w.isLockedCurr) return false;
        return true;
    }

    public void UnlockWShopItem(Proto_ShopSystem.PSItemDef def)
    {
        foreach (var kvp in doc)
            foreach (var w in kvp.Value)
                if (w.def == def) { w.isLockedCurr = false; return; }
    }

    public void UnlockEntireCategory(Proto_ShopSystem.PSCategory cat)
    {
        foreach (var w in doc[cat]) w.isLockedCurr = false;
    }

    public CartItem TryAddNewCartItem(Proto_ShopSystem.PSWShopItem wItem)
    {
        foreach (var ci in cart)
            if (ci.wShopItem == wItem) { ci.qty++; return ci; }
        var newCi = new CartItem { wShopItem = wItem, qty = 1 };
        cart.Add(newCi);
        return newCi;
    }

    public void RemoveCartItem(CartItem ci) => cart.Remove(ci);

    public void IncreaseCartItemQty(CartItem ci, int delta)
    {
        ci.qty += delta;
        if (ci.qty <= 0) cart.Remove(ci);
    }

    public void AlterCartItemQty(CartItem ci, int newQty)
    {
        ci.qty = newQty;
        if (ci.qty <= 0) cart.Remove(ci);
    }

    public void ClearCartItems() => cart.Clear();
    public List<CartItem> GetCartItems() => cart;

    public float GetCartTotalPrice()
    {
        float sum = 0f;
        foreach (var ci in cart) sum += ci.wShopItem.def.price * ci.qty;
        return sum;
    }

    public bool CanAffordCartItems() => money >= GetCartTotalPrice();

    public string GetSnapshot(float externalMoney)
    {
        string s = $"[Shop Snapshot] Money=${externalMoney:F2}\n";
        foreach (var cat in categories)
        {
            s += $"  Category: {cat.name} (hidden={ShouldCategoryBeHidden(cat)})\n";
            foreach (var w in doc[cat])
                s += $"    {w.def.name}: locked={w.isLockedCurr}, purchased={w.timesPurchased}\n";
        }
        s += $"  Cart ({cart.Count} items):\n";
        foreach (var ci in cart)
            s += $"    {ci.wShopItem.def.name} x{ci.qty} = ${ci.wShopItem.def.price * ci.qty:F2}\n";
        s += $"  Total: ${GetCartTotalPrice():F2}, CanAfford: {CanAffordCartItems()}";
        return s;
    }
}