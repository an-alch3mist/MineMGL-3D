using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// STANDALONE PROTOTYPE — Phase A Full Integration (end-to-end cross-system)
/// Zero dependencies. Drop on empty scene → Press Play.
///
/// Tests EVERY cross-system event chain in Phase A:
///
///   FLOW 1: Interaction → Shop Open
///     Player walks to InteractableComputer → E → raycast hits → IInteractable.ShouldUseInteractionWheel()=false
///     → auto-execute first option → InteractableComputer.Interact() fires RaiseOpenShopView
///     → ShopUI subscribes → SetActive(true) → OnEnable fires RaiseMenuStateChanged(true)
///     → PlayerController pauses movement + cursor unlocks
///
///   FLOW 2: MoneyBridge Wiring (timing)
///     MoneyBridge.Start() fires RaiseMoneyProviderReady(IShopMoney)
///     → ShopUI.OnEnable (isFirstEnable) subscribes → stores IShopMoney reference
///     → ShopUI passes IShopMoney to Orchestrator.Init()
///     → EconomyManager.Start() fires RaiseMoneyChanged(400) → moneyUI updates HUD
///
///   FLOW 3: Shop Browse → Cart → Purchase
///     Player clicks category tab → SelectCategoryView → RepopulateShopItems
///     → clicks "Add to Cart" → TryAddNewCartItem → cart row created
///     → +/- qty buttons → IncreaseCartItemQty → RefreshAll (total + afford color)
///     → clicks Purchase → CanAffordCartItems check → IShopMoney.AddMoney(-cost)
///     → RaiseMoneyChanged → moneyUI updates → items spawn at ShopSpawnPoint
///     → RaiseCloseShopView → ShopUI.SetActive(false) → OnDisable fires RaiseMenuStateChanged(false)
///     → PlayerController resumes + cursor relocks
///
///   FLOW 4: Interaction Wheel (multi-option)
///     Player walks to multi-option interactable → E → raycast → ShouldUseInteractionWheel()=true
///     → RaiseOpenInteractionView(interactable) → InteractionWheelUI subscribes → SetActive(true)
///     → RaiseMenuStateChanged(true) → builds option buttons → player clicks one
///     → interactable.Interact(option) → RaiseCloseInteractionView → wheel closes
///     → RaiseMenuStateChanged(false) → cursor relocks
///
///   FLOW 5: Category Unlock
///     U key → RaiseUnlockedCategory(premium) → ShopUIOrchestrator subscribes
///     → UnlockEntireCategory → hidden tab becomes visible → items become purchasable
///
///   FLOW 6: UIManager.CloseAllSubManager
///     Opening shop while wheel is open → CloseAllSubManager → wheel closes first → shop opens
///     Opening wheel while shop is open → CloseAllSubManager → shop closes first → wheel opens
///
/// Controls:
///   WASD = move, Mouse = look
///   E = interact (raycast)
///   ESC = close any open menu
///   U = unlock Premium category
///   G = gift $100 (external income)
///   I = print full state (money, menu, events log)
///   L = print event log (all events fired this session)
/// </summary>
public class Proto_PhaseA_Integration : MonoBehaviour
{
    // ═══ INLINE EVENT BUS (mirrors GameEvents) ═══
    static Action onOpenShopView;
    static Action onCloseShopView;
    static Action<PAInteractable> onOpenInteractionView;
    static Action onCloseInteractionView;
    static Action<bool> onMenuStateChanged;
    static Action<float> onMoneyChanged;
    static Action<PACategory> onUnlockedCategory;
    static Action<PAShopMoney> onMoneyProviderReady;

    static List<string> eventLog = new List<string>();
    static void LogEvent(string name)
    {
        string entry = $"[{Time.frameCount}] {name}";
        eventLog.Add(entry);
        Debug.Log($"<color=cyan>[Event] {entry}</color>");
    }

    // ═══ CORE STATE ═══
    Camera cam;
    CharacterController cc;
    float xRot, yRot;
    bool isAnyMenuOpen;

    // ═══ ECONOMY (EconomyManager) ═══
    float currMoney = 400f;
    PAShopMoney moneyProvider;

    // ═══ SHOP DATA ═══
    List<PACategory> categories = new List<PACategory>();
    PADataService dataService;

    // ═══ UI REFERENCES ═══
    Canvas canvas;
    // → shop UI panel
    GameObject shopPanel;
    GameObject categoryContainer, itemContainer, cartContainer;
    UnityEngine.UI.Text totalPriceText, moneyHudText, statusHudText;
    UnityEngine.UI.Button purchaseBtn;
    // → interaction wheel panel
    GameObject wheelPanel, wheelContent;
    UnityEngine.UI.Text wheelTitle;
    // → tracking
    PACategory selectedCategory;
    Dictionary<PACategory, GameObject> catTabs = new Dictionary<PACategory, GameObject>();
    List<GameObject> itemRows = new List<GameObject>();
    Dictionary<PADataService.CartItem, GameObject> cartRows = new Dictionary<PADataService.CartItem, GameObject>();
    List<GameObject> wheelButtons = new List<GameObject>();
    // → world
    List<PAInteractable> interactables = new List<PAInteractable>();
    List<Vector3> spawnPoints = new List<Vector3>();

    // ═══ COLORS ═══
    Color selectedTabCol = new Color(0.3f, 0.6f, 1f);
    Color normalTabCol = new Color(0.22f, 0.22f, 0.25f);
    Color canAffordCol = new Color(0.2f, 0.8f, 0.2f);
    Color cantAffordCol = new Color(0.8f, 0.2f, 0.2f);
    Color canBuyCol = new Color(0.3f, 0.7f, 0.3f);
    Color cantBuyCol = new Color(0.5f, 0.2f, 0.2f);

    // ═══════════════════════════════════════════
    //  START — wires everything in correct order
    // ═══════════════════════════════════════════
    void Start()
    {
        // → clear static state (domain reload safety)
        onOpenShopView = null; onCloseShopView = null;
        onOpenInteractionView = null; onCloseInteractionView = null;
        onMenuStateChanged = null; onMoneyChanged = null;
        onUnlockedCategory = null; onMoneyProviderReady = null;
        eventLog.Clear();

        // → camera + controller
        cam = Camera.main;
        transform.position = new Vector3(0f, 0f, -3f);
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.center = new Vector3(0f, 0.9f, 0f);
        cam.transform.SetParent(transform);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

        // → ground + spawn points
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = new Vector3(5, 1, 5);
        ground.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.25f);
        for (int i = 0; i < 3; i++)
        {
            var sp = new Vector3(-3 + i * 3, 0.5f, -6f);
            spawnPoints.Add(sp);
            var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m.transform.position = sp; m.transform.localScale = Vector3.one * 0.2f;
            m.GetComponent<Renderer>().material.color = Color.green;
            Destroy(m.GetComponent<Collider>());
        }

        // → build data
        BuildCategoryData();
        dataService = new PADataService();
        moneyProvider = new PAShopMoney(this);

        // → build UI
        BuildCanvas();
        BuildShopPanel();
        BuildWheelPanel();
        BuildHUD();

        // ─── SUBSCRIPTION PHASE (mirrors real OnEnable/isFirstEnable) ───

        // → PlayerController subscribes to MenuStateChanged
        onMenuStateChanged += (open) =>
        {
            isAnyMenuOpen = open;
            LogEvent($"MenuStateChanged → isAnyMenuOpen={open}");
        };

        // → ShopUI subscribes to open/close (isFirstEnable pattern)
        onMoneyProviderReady += (m) =>
        {
            LogEvent($"MoneyProviderReady received → IShopMoney wired");
        };

        onOpenShopView += () =>
        {
            LogEvent("OpenShopView → CloseAll + ShopUI.SetActive(true)");
            CloseAll();
            shopPanel.SetActive(true);
            dataService.BuildCategories(categories, currMoney);
            RebuildCategoryView();
            RefreshShop();
            RaiseMenuStateChanged(true);
        };
        onCloseShopView += () =>
        {
            LogEvent("CloseShopView → ShopUI.SetActive(false)");
            shopPanel.SetActive(false);
            RaiseMenuStateChanged(false);
        };

        // → InteractionWheelUI subscribes to open/close
        onOpenInteractionView += (interactable) =>
        {
            LogEvent($"OpenInteractionView → {interactable.objectName}");
            CloseAll();
            var options = interactable.GetOptions();
            if (!interactable.ShouldUseWheel())
            {
                LogEvent($"  Single-option shortcut → {options[0].name}");
                interactable.Interact(options[0]);
                return;
            }
            OpenWheel(interactable, options);
            RaiseMenuStateChanged(true);
        };
        onCloseInteractionView += () =>
        {
            LogEvent("CloseInteractionView → wheel closed");
            wheelPanel.SetActive(false);
            RaiseMenuStateChanged(false);
        };

        // → moneyUI subscribes to MoneyChanged
        onMoneyChanged += (money) =>
        {
            LogEvent($"MoneyChanged → ${money:F2}");
            moneyHudText.text = $"${money:F2}";
        };

        // → ShopUIOrchestrator subscribes to UnlockedCategory
        onUnlockedCategory += (cat) =>
        {
            LogEvent($"UnlockedCategory → {cat.name}");
            dataService.UnlockEntireCategory(cat);
            if (shopPanel.activeSelf) RebuildCategoryView();
        };

        // → ShopUI subscribes to purchaseButton
        purchaseBtn.onClick.AddListener(PurchaseAll);

        // ─── BRIDGE FIRES (MoneyBridge.Start pattern) ───
        shopPanel.SetActive(false);
        wheelPanel.SetActive(false);

        LogEvent("MoneyBridge.Start → firing MoneyProviderReady");
        if (onMoneyProviderReady != null) onMoneyProviderReady(moneyProvider);

        // → EconomyManager.Start fires initial MoneyChanged
        LogEvent("EconomyManager.Start → firing MoneyChanged(400)");
        RaiseMoneyChanged();

        // → spawn interactables
        SpawnWorld();

        Debug.Log("[Integration] WASD=move E=interact ESC=close U=unlock G=gift$100 I=info L=eventlog");
    }

    // ═══ UPDATE ═══
    void Update()
    {
        // → cursor lock (PlayerController pattern)
        Cursor.lockState = isAnyMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isAnyMenuOpen;

        if (!isAnyMenuOpen)
        {
            HandleLook();
            HandleMovement();
        }

        // → E = interact or close
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isAnyMenuOpen) { CloseAll(); RaiseMenuStateChanged(false); }
            else TryInteract();
        }

        // → ESC = close
        if (Input.GetKeyDown(KeyCode.Escape) && isAnyMenuOpen)
        {
            CloseAll();
            RaiseMenuStateChanged(false);
        }

        // → U = unlock premium
        if (Input.GetKeyDown(KeyCode.U))
        {
            var premium = categories.Find(c => c.name == "Premium");
            if (premium != null) RaiseUnlockedCategory(premium);
        }

        // → G = gift money
        if (Input.GetKeyDown(KeyCode.G))
        {
            currMoney += 100f;
            dataService.SetMoney(currMoney);
            RaiseMoneyChanged();
            if (shopPanel.activeSelf) RefreshShop();
        }

        // → I = info
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"[State] Money=${currMoney:F2} | MenuOpen={isAnyMenuOpen} | ShopActive={shopPanel.activeSelf} | WheelActive={wheelPanel.activeSelf} | Cart={dataService.GetCartItems().Count} | Events={eventLog.Count}");
        }

        // → L = event log
        if (Input.GetKeyDown(KeyCode.L))
        {
            string log = "[Event Log]\n";
            foreach (var e in eventLog) log += "  " + e + "\n";
            Debug.Log(log);
        }

        UpdateHUD();
    }

    // ═══ PLAYER MOVEMENT ═══
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

    // ═══ INTERACTION (SimplePlayerInteraction) ═══
    void TryInteract()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, 6f)) return;
        var ia = hit.collider.GetComponent<PAInteractable>();
        if (ia == null) { Debug.Log($"[Interact] Hit {hit.collider.name} — no interactable"); return; }
        LogEvent($"Player pressed E → raycast hit {ia.objectName}");
        RaiseOpenInteractionView(ia);
    }

    // ═══ EVENT RAISE METHODS (GameEvents pattern) ═══
    void RaiseOpenShopView() { LogEvent("RAISE OpenShopView"); onOpenShopView?.Invoke(); }
    void RaiseCloseShopView() { LogEvent("RAISE CloseShopView"); onCloseShopView?.Invoke(); }
    void RaiseOpenInteractionView(PAInteractable ia) { LogEvent($"RAISE OpenInteractionView({ia.objectName})"); onOpenInteractionView?.Invoke(ia); }
    void RaiseCloseInteractionView() { LogEvent("RAISE CloseInteractionView"); onCloseInteractionView?.Invoke(); }
    void RaiseMenuStateChanged(bool open) { LogEvent($"RAISE MenuStateChanged({open})"); onMenuStateChanged?.Invoke(open); }
    void RaiseMoneyChanged() { LogEvent($"RAISE MoneyChanged({currMoney:F2})"); onMoneyChanged?.Invoke(currMoney); }
    void RaiseUnlockedCategory(PACategory cat) { LogEvent($"RAISE UnlockedCategory({cat.name})"); onUnlockedCategory?.Invoke(cat); }

    // ═══ CLOSE ALL (UIManager.CloseAllSubManager) ═══
    void CloseAll()
    {
        if (shopPanel.activeSelf) { shopPanel.SetActive(false); LogEvent("CloseAll → shop closed"); }
        if (wheelPanel.activeSelf) { wheelPanel.SetActive(false); LogEvent("CloseAll → wheel closed"); }
    }

    // ═══ SHOP UI ═══

    void RebuildCategoryView()
    {
        foreach (var kvp in catTabs) Destroy(kvp.Value);
        catTabs.Clear();
        float y = 0.88f;
        PACategory firstVisible = null;
        foreach (var cat in categories)
        {
            if (dataService.ShouldCategoryBeHidden(cat)) continue;
            var tab = MakeButton($"Tab_{cat.name}", categoryContainer.transform, 0.05f, y - 0.08f, 0.95f, y, cat.name, normalTabCol, null);
            catTabs[cat] = tab;
            var cc = cat;
            tab.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectCategory(cc));
            if (firstVisible == null) firstVisible = cat;
            y -= 0.1f;
        }
        if (firstVisible != null) SelectCategory(firstVisible);
    }

    void SelectCategory(PACategory cat)
    {
        selectedCategory = cat;
        foreach (var kvp in catTabs)
            kvp.Value.GetComponent<UnityEngine.UI.Image>().color = kvp.Key == cat ? selectedTabCol : normalTabCol;
        RepopulateItems();
    }

    void RepopulateItems()
    {
        foreach (var r in itemRows) Destroy(r);
        itemRows.Clear();
        float y = 0.95f;
        foreach (var w in dataService.GetItems(selectedCategory))
        {
            var row = MakePanel($"Item_{w.def.name}", itemContainer.transform, 0.02f, y - 0.12f, 0.98f, y, new Color(0.15f, 0.15f, 0.2f, 0.85f));
            MakePanel("Sw", row.transform, 0.02f, 0.1f, 0.1f, 0.9f, w.def.color);
            MakeText("N", row.transform, 0.12f, 0.5f, 0.55f, 0.95f, w.def.name, 15, TextAnchor.MiddleLeft, Color.white);
            MakeText("D", row.transform, 0.12f, 0.05f, 0.55f, 0.48f, w.def.descr, 11, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f));
            MakeText("P", row.transform, 0.56f, 0.1f, 0.72f, 0.9f, $"${w.def.price:F0}", 15, TextAnchor.MiddleCenter, Color.yellow);
            bool canBuy = !w.isLocked;
            var btn = MakeButton("Add", row.transform, 0.74f, 0.15f, 0.97f, 0.85f, canBuy ? "Add" : "Locked", canBuy ? canBuyCol : cantBuyCol, null);
            if (canBuy) { var cw = w; btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => AddToCart(cw)); }
            else btn.GetComponent<UnityEngine.UI.Button>().interactable = false;
            if (w.IsNewlyUnlocked()) MakeText("New", row.transform, 0.88f, 0.75f, 1f, 1f, "NEW!", 10, TextAnchor.MiddleCenter, Color.cyan);
            itemRows.Add(row);
            y -= 0.13f;
        }
    }

    void AddToCart(PAWShopItem w)
    {
        var ci = dataService.TryAddCartItem(w);
        if (cartRows.ContainsKey(ci))
        {
            var qt = cartRows[ci].transform.Find("Q");
            if (qt != null) qt.GetComponent<UnityEngine.UI.Text>().text = ci.qty.ToString();
            RefreshShop(); return;
        }
        float rh = 0.1f, ry = 0.88f - cartRows.Count * (rh + 0.01f);
        var row = MakePanel($"C_{w.def.name}", cartContainer.transform, 0.02f, ry - rh, 0.98f, ry, new Color(0.18f, 0.18f, 0.22f, 0.85f));
        MakePanel("Sw", row.transform, 0.02f, 0.1f, 0.1f, 0.9f, w.def.color);
        MakeText("N", row.transform, 0.12f, 0.5f, 0.5f, 0.95f, w.def.name, 13, TextAnchor.MiddleLeft, Color.white);
        MakeText("Pr", row.transform, 0.12f, 0.05f, 0.5f, 0.45f, $"${w.def.price:F0}", 11, TextAnchor.MiddleLeft, Color.yellow);
        var qText = MakeText("Q", row.transform, 0.52f, 0.2f, 0.64f, 0.8f, ci.qty.ToString(), 15, TextAnchor.MiddleCenter, Color.white).GetComponent<UnityEngine.UI.Text>();
        var cci = ci;
        MakeButton("-", row.transform, 0.44f, 0.15f, 0.52f, 0.85f, "-", new Color(0.6f, 0.3f, 0.3f), () =>
        {
            dataService.ChangeQty(cci, -1);
            if (cci.qty <= 0) RemoveCartRow(cci); else qText.text = cci.qty.ToString();
            RefreshShop();
        });
        MakeButton("+", row.transform, 0.64f, 0.15f, 0.72f, 0.85f, "+", new Color(0.3f, 0.6f, 0.3f), () =>
        {
            dataService.ChangeQty(cci, +1);
            qText.text = cci.qty.ToString();
            RefreshShop();
        });
        MakeButton("X", row.transform, 0.82f, 0.15f, 0.97f, 0.85f, "X", new Color(0.7f, 0.2f, 0.2f), () =>
        {
            dataService.RemoveCartItem(cci);
            RemoveCartRow(cci);
            RefreshShop();
        });
        cartRows[ci] = row;
        RefreshShop();
    }

    void RemoveCartRow(PADataService.CartItem ci)
    {
        if (!cartRows.ContainsKey(ci)) return;
        Destroy(cartRows[ci]); cartRows.Remove(ci);
        int i = 0;
        foreach (var kvp in cartRows)
        {
            float ry = 0.88f - i * 0.11f;
            var r = kvp.Value.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.02f, ry - 0.1f); r.anchorMax = new Vector2(0.98f, ry);
            i++;
        }
    }

    void PurchaseAll()
    {
        float total = dataService.GetCartTotal();
        if (total <= 0 || currMoney < total) { Debug.Log("[Shop] Can't purchase!"); return; }
        var items = new List<PADataService.CartItem>(dataService.GetCartItems());
        foreach (var ci in items)
        {
            // → spawn at ShopSpawnPoint (random)
            Vector3 pt = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            for (int j = 0; j < ci.qty; j++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Purchased_{ci.wItem.def.name}";
                go.transform.localScale = Vector3.one * 0.35f;
                Vector3 off = UnityEngine.Random.insideUnitSphere; off.y = Mathf.Abs(off.y);
                go.transform.position = pt + off * 0.3f;
                go.GetComponent<Renderer>().material.color = ci.wItem.def.color;
                go.AddComponent<Rigidbody>().mass = 0.3f;
            }
            currMoney -= ci.wItem.def.price * ci.qty;
        }
        // → IShopMoney.AddMoney(-cost) already applied above
        dataService.ClearCart();
        foreach (var kvp in cartRows) Destroy(kvp.Value);
        cartRows.Clear();
        dataService.SetMoney(currMoney);
        RaiseMoneyChanged();
        RefreshShop();
        // → close shop after purchase (original behavior)
        RaiseCloseShopView();
        LogEvent($"Purchase complete! Spent ${total:F2}, remaining ${currMoney:F2}");
    }

    void RefreshShop()
    {
        float total = dataService.GetCartTotal();
        bool afford = currMoney >= total;
        totalPriceText.text = $"Total: ${total:F2}";
        totalPriceText.color = afford ? canAffordCol : cantAffordCol;
        purchaseBtn.interactable = afford && dataService.GetCartItems().Count > 0;
    }

    // ═══ INTERACTION WHEEL ═══

    void OpenWheel(PAInteractable ia, List<PAOption> options)
    {
        foreach (var b in wheelButtons) Destroy(b);
        wheelButtons.Clear();
        wheelTitle.text = ia.objectName;
        foreach (var opt in options)
        {
            var btn = MakeButton($"W_{opt.name}", wheelContent.transform, 0f, 0f, 1f, 1f, opt.name, opt.color, null);
            var co = opt; var ci = ia;
            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                ci.Interact(co);
                RaiseCloseInteractionView();
            });
            wheelButtons.Add(btn);
        }
        wheelPanel.SetActive(true);
    }

    // ═══ HUD ═══

    void UpdateHUD()
    {
        string menu = isAnyMenuOpen ? "<color=lime>MENU OPEN</color>" : "<color=grey>CLOSED</color>";
        string hint = "";
        if (!isAnyMenuOpen)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 6f))
            {
                var ia = hit.collider.GetComponent<PAInteractable>();
                if (ia != null) hint = $" | <color=yellow>[E] {ia.objectName}</color>";
            }
        }
        statusHudText.text = $"{menu}{hint} | Events: {eventLog.Count}";
    }

    // ═══ WORLD ═══

    void SpawnWorld()
    {
        // → Computer: single-option interactable → opens shop (the KEY cross-system flow)
        SpawnInteractable("Computer", new Vector3(-3f, 0.75f, 5f), new Color(0.2f, 0.4f, 0.8f),
            false, new List<PAOption> { new PAOption("Open Shop", PAActionType.openShop, new Color(0.3f, 0.5f, 0.9f)) });

        // → Vendor: 3 options (tests wheel)
        SpawnInteractable("Vendor", new Vector3(0f, 0.75f, 5f), new Color(0.1f, 0.7f, 0.3f),
            true, new List<PAOption>
            {
                new PAOption("Trade", PAActionType.custom, new Color(0.2f, 0.6f, 0.3f)),
                new PAOption("Talk", PAActionType.custom, new Color(0.3f, 0.3f, 0.7f)),
                new PAOption("Give Gift", PAActionType.custom, new Color(0.7f, 0.3f, 0.5f))
            });

        // → Second Computer: tests "open shop while already in wheel" → CloseAll flow
        SpawnInteractable("Terminal", new Vector3(3f, 0.75f, 5f), new Color(0.6f, 0.2f, 0.6f),
            false, new List<PAOption> { new PAOption("Open Shop", PAActionType.openShop, new Color(0.5f, 0.2f, 0.5f)) });

        // → Multi-option with 5 options (stress test wheel layout)
        SpawnInteractable("Workbench", new Vector3(6f, 0.75f, 5f), new Color(0.5f, 0.5f, 0.5f),
            true, new List<PAOption>
            {
                new PAOption("Craft", PAActionType.custom, new Color(0.4f, 0.6f, 0.2f)),
                new PAOption("Repair", PAActionType.custom, new Color(0.6f, 0.4f, 0.2f)),
                new PAOption("Upgrade", PAActionType.custom, new Color(0.2f, 0.4f, 0.7f)),
                new PAOption("Disassemble", PAActionType.custom, new Color(0.7f, 0.2f, 0.2f)),
                new PAOption("Inspect", PAActionType.custom, new Color(0.4f, 0.4f, 0.4f))
            });
    }

    void SpawnInteractable(string name, Vector3 pos, Color col, bool useWheel, List<PAOption> opts)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.position = pos;
        go.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
        go.GetComponent<Renderer>().material.color = col;
        var ia = go.AddComponent<PAInteractable>();
        ia.Init(name, useWheel, opts, this);
        interactables.Add(ia);
        // → marker
        var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        m.transform.position = pos + Vector3.up * 1.3f;
        m.transform.localScale = Vector3.one * 0.12f;
        m.GetComponent<Renderer>().material.color = Color.yellow;
        Destroy(m.GetComponent<Collider>());
    }

    // ═══ DATA ═══

    void BuildCategoryData()
    {
        categories.Add(new PACategory("Tools", false, new List<PAItemDef>
        {
            new PAItemDef("Pickaxe", "Basic mining tool", 50f, false, Color.grey),
            new PAItemDef("Shovel", "Digs dirt", 35f, false, new Color(0.5f, 0.35f, 0.15f)),
            new PAItemDef("Hammer", "Build & repair", 75f, false, new Color(0.6f, 0.6f, 0.6f)),
        }));
        categories.Add(new PACategory("Materials", false, new List<PAItemDef>
        {
            new PAItemDef("Wood", "Standard plank", 10f, false, new Color(0.6f, 0.4f, 0.2f)),
            new PAItemDef("Iron Bar", "Refined ingot", 25f, false, new Color(0.7f, 0.7f, 0.7f)),
            new PAItemDef("Gold Bar", "Precious metal", 100f, true, new Color(1f, 0.84f, 0f)),
        }));
        categories.Add(new PACategory("Premium", true, new List<PAItemDef>
        {
            new PAItemDef("TNT Box", "Explosive tool", 200f, true, new Color(0.8f, 0.1f, 0.1f)),
            new PAItemDef("Jet Pack", "Fly anywhere", 500f, true, new Color(0.2f, 0.2f, 0.8f)),
        }));
    }

    // ═══ UI BUILDERS ═══

    void BuildCanvas()
    {
        var go = new GameObject("IntegrationCanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var sc = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    void BuildShopPanel()
    {
        shopPanel = MakePanel("Shop", canvas.transform, 0.05f, 0.05f, 0.95f, 0.95f, new Color(0.08f, 0.08f, 0.12f, 0.95f));
        MakeText("Title", shopPanel.transform, 0f, 0.92f, 1f, 1f, "S H O P", 26, TextAnchor.MiddleCenter, Color.white);
        categoryContainer = MakePanel("Cats", shopPanel.transform, 0.02f, 0.08f, 0.18f, 0.9f, new Color(0.12f, 0.12f, 0.16f, 0.9f));
        itemContainer = MakePanel("Items", shopPanel.transform, 0.2f, 0.08f, 0.58f, 0.9f, new Color(0.1f, 0.1f, 0.14f, 0.9f));
        cartContainer = MakePanel("Cart", shopPanel.transform, 0.6f, 0.15f, 0.98f, 0.9f, new Color(0.12f, 0.12f, 0.16f, 0.9f));
        MakeText("CartT", cartContainer.transform, 0f, 0.92f, 1f, 1f, "CART", 18, TextAnchor.MiddleCenter, Color.white);
        var footer = MakePanel("Footer", shopPanel.transform, 0.6f, 0.05f, 0.98f, 0.14f, new Color(0.15f, 0.15f, 0.2f, 0.9f));
        totalPriceText = MakeText("Total", footer.transform, 0.02f, 0.5f, 0.5f, 1f, "$0.00", 16, TextAnchor.MiddleLeft, canAffordCol).GetComponent<UnityEngine.UI.Text>();
        var pbtn = MakeButton("Buy", footer.transform, 0.55f, 0.1f, 0.95f, 0.9f, "PURCHASE", new Color(0.2f, 0.6f, 0.2f), null);
        purchaseBtn = pbtn.GetComponent<UnityEngine.UI.Button>();
    }

    void BuildWheelPanel()
    {
        wheelPanel = MakePanel("Wheel", canvas.transform, 0.3f, 0.2f, 0.7f, 0.8f, new Color(0.1f, 0.1f, 0.15f, 0.92f));
        wheelTitle = MakeText("WTitle", wheelPanel.transform, 0f, 0.88f, 1f, 1f, "INTERACT", 22, TextAnchor.MiddleCenter, Color.white).GetComponent<UnityEngine.UI.Text>();
        wheelContent = MakePanel("WContent", wheelPanel.transform, 0.05f, 0.05f, 0.95f, 0.85f, new Color(0f, 0f, 0f, 0f));
        var vlg = wheelContent.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.spacing = 6f; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = true;
        vlg.padding = new RectOffset(8, 8, 8, 8);
    }

    void BuildHUD()
    {
        moneyHudText = MakeText("Money", canvas.transform, 0.75f, 0.93f, 0.99f, 1f, "$400.00", 22, TextAnchor.MiddleRight, Color.yellow).GetComponent<UnityEngine.UI.Text>();
        MakeText("MoneyL", canvas.transform, 0.6f, 0.93f, 0.75f, 1f, "Balance:", 16, TextAnchor.MiddleRight, new Color(0.6f, 0.6f, 0.6f));
        statusHudText = MakeText("Status", canvas.transform, 0.01f, 0.93f, 0.55f, 1f, "", 13, TextAnchor.MiddleLeft, Color.white).GetComponent<UnityEngine.UI.Text>();
        MakeText("Cross", canvas.transform, 0.49f, 0.48f, 0.51f, 0.52f, "+", 20, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
        MakeText("Help", canvas.transform, 0.01f, 0.01f, 0.5f, 0.06f, "E=interact  ESC=close  U=unlock  G=gift$100  I=info  L=eventlog", 11, TextAnchor.LowerLeft, new Color(0.5f, 0.5f, 0.5f));
    }

    // ═══ UI HELPERS ═══

    GameObject MakePanel(string n, Transform p, float x0, float y0, float x1, float y1, Color c)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
        go.AddComponent<UnityEngine.UI.Image>().color = c;
        return go;
    }
    GameObject MakeText(string n, Transform p, float x0, float y0, float x1, float y1, string txt, int sz, TextAnchor a, Color c)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = txt; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = sz; t.alignment = a; t.color = c;
        return go;
    }
    GameObject MakeButton(string n, Transform p, float x0, float y0, float x1, float y1, string lbl, Color bg, Action onClick)
    {
        var go = MakePanel(n, p, x0, y0, x1, y1, bg);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        MakeText("L", go.transform, 0, 0, 1, 1, lbl, 13, TextAnchor.MiddleCenter, Color.white);
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return go;
    }

    // → public accessors for PAShopMoney
    public float GetMoney() => currMoney;
    public void SetMoney(float m) => currMoney = m;

    // ═══ DATA TYPES ═══
    public enum PAActionType { openShop, custom }
    public class PAOption { public string name; public PAActionType type; public Color color; public PAOption(string n, PAActionType t, Color c) { name = n; type = t; color = c; } }
    public class PAItemDef { public string name, descr; public float price; public bool isDefaultLocked; public Color color; public PAItemDef(string n, string d, float p, bool l, Color c) { name = n; descr = d; price = p; isDefaultLocked = l; color = c; } }
    public class PACategory { public string name; public bool hideIfAllLocked; public List<PAItemDef> items; public PACategory(string n, bool h, List<PAItemDef> i) { name = n; hideIfAllLocked = h; items = i; } }
    public class PAWShopItem { public PAItemDef def; public bool isLocked; public int timesPurchased; public PAWShopItem(PAItemDef d) { def = d; isLocked = d.isDefaultLocked; } public bool IsNewlyUnlocked() => !isLocked && timesPurchased == 0 && def.isDefaultLocked; }
    public class PAShopMoney { Proto_PhaseA_Integration o; public PAShopMoney(Proto_PhaseA_Integration p) { o = p; } public float GetMoney() => o.GetMoney(); public void AddMoney(float d) => o.SetMoney(o.GetMoney() + d); public bool CanAfford(float p) => o.GetMoney() >= p; }
}

// ═══════════════════════════════════════════
// PAInteractable — mirrors IInteractable + InteractableComputer
// ═══════════════════════════════════════════
public class PAInteractable : MonoBehaviour
{
    public string objectName;
    bool useWheel;
    List<Proto_PhaseA_Integration.PAOption> options;
    Proto_PhaseA_Integration proto;

    public void Init(string n, bool w, List<Proto_PhaseA_Integration.PAOption> o, Proto_PhaseA_Integration p)
    { objectName = n; useWheel = w; options = o; proto = p; }

    public bool ShouldUseWheel() => useWheel;
    public List<Proto_PhaseA_Integration.PAOption> GetOptions() => options;

    public void Interact(Proto_PhaseA_Integration.PAOption opt)
    {
        if (opt.type == Proto_PhaseA_Integration.PAActionType.openShop)
        {
            Debug.Log($"<color=lime>[Interact] {objectName} → Open Shop!</color>");
            // → this is THE cross-system call: InteractableComputer.Interact fires GameEvents.RaiseOpenShopView
            // in real code: GameEvents.RaiseOpenShopView();
            // here we call the proto's raise method which triggers the full chain
            proto.SendMessage("RaiseOpenShopView", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.Log($"<color=lime>[Interact] {objectName} → '{opt.name}' executed!</color>");
        }
    }
}

// ═══════════════════════════════════════════
// PADataService — mirrors ShopDataService
// ═══════════════════════════════════════════
public class PADataService
{
    float money;
    Dictionary<Proto_PhaseA_Integration.PACategory, List<Proto_PhaseA_Integration.PAWShopItem>> doc = new Dictionary<Proto_PhaseA_Integration.PACategory, List<Proto_PhaseA_Integration.PAWShopItem>>();
    List<CartItem> cart = new List<CartItem>();
    public class CartItem { public Proto_PhaseA_Integration.PAWShopItem wItem; public int qty; }

    public void BuildCategories(List<Proto_PhaseA_Integration.PACategory> cats, float m)
    {
        money = m; doc.Clear();
        foreach (var c in cats)
        {
            var list = new List<Proto_PhaseA_Integration.PAWShopItem>();
            foreach (var d in c.items) list.Add(new Proto_PhaseA_Integration.PAWShopItem(d));
            doc[c] = list;
        }
    }
    public void SetMoney(float m) => money = m;
    public List<Proto_PhaseA_Integration.PAWShopItem> GetItems(Proto_PhaseA_Integration.PACategory c) => doc[c];
    public bool ShouldCategoryBeHidden(Proto_PhaseA_Integration.PACategory c)
    {
        if (!c.hideIfAllLocked) return false;
        foreach (var w in doc[c]) if (!w.isLocked) return false;
        return true;
    }
    public void UnlockEntireCategory(Proto_PhaseA_Integration.PACategory c) { foreach (var w in doc[c]) w.isLocked = false; }
    public CartItem TryAddCartItem(Proto_PhaseA_Integration.PAWShopItem w)
    {
        foreach (var ci in cart) if (ci.wItem == w) { ci.qty++; return ci; }
        var n = new CartItem { wItem = w, qty = 1 }; cart.Add(n); return n;
    }
    public void RemoveCartItem(CartItem ci) => cart.Remove(ci);
    public void ChangeQty(CartItem ci, int d) { ci.qty += d; if (ci.qty <= 0) cart.Remove(ci); }
    public void ClearCart() => cart.Clear();
    public List<CartItem> GetCartItems() => cart;
    public float GetCartTotal() { float s = 0; foreach (var ci in cart) s += ci.wItem.def.price * ci.qty; return s; }
}