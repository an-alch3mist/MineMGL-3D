# GOAL

> Every script is typed by hand. This file defines how.

---

## What

Rebuild **MINEMGL** from `Scripts/Assembly-CSharp/` (~200 scripts).
**100% main-source behavior** + cleaner architecture + independently testable systems.

## Source of Truth

| What | Where |
|------|-------|
| Logic & behavior | `Scripts/Assembly-CSharp/*.cs` |
| Architecture | `learn/ARCHITECTURE.md` |
| Roadmap | `learn/PhaseMap.md` |
| Reasoning log | `learn/surfer.md` |
| User's actual code | `learn/handTyped(latest)/` — ground truth for coding style |

---

## Core Principle

**Every script's purpose fits in one sentence. If it doesn't, split it until it does.**
**Every script performs ONLY the service it was assigned — purely, nothing else.**

Every file must have a `→ "I do X"` one-liner. That one-liner is the script's contract. If the script does anything outside that sentence, it's doing too much — move it to the script whose sentence covers it.

```
SO_ShopItemDef         → "I define what a shop item IS"
WShopItem              → "I track what happened to one item this session"
ShopDataService        → "I manage all shop data + cart as a collection"
Field_ShopItem         → "I display one item row"
ShopUIOrchestrator     → "I wire UI fields to data and handle actions"
ShopUI                 → "I open and close the shop panel"
BgUI                   → "I show/hide blur when menus change"
UIManager              → "I report menu state + close all panels when needed"
EconomyManager         → "I own money"
GameEvents             → "I deliver messages between systems"
Singleton              → "I ensure one instance"
```

End goal: **main source (100%) behavior** with this architecture — consistent, decoupled, independently testable via vertical slice tests.

---

## Folder Structure

Numbered for Unity project panel sorting. One-liner purpose per folder.

```
Scripts/phase-All/                      → "shared scripts that grow across phases — never duplicated"
├── 0-Core/
│   ├── Singleton.cs                    → "generic singleton base — first instance wins"
│   └── GameEvents.cs                   → "core events shared across ALL phases (OnCloseAllSubManagers, LogSubscribersCount)"
├── 1-Managers/
│   ├── UIManager.cs                    → "reports menu state + closes all panels + routes keyboard with priority"
│   └── EconomyManager.cs               → "owns money"
├── 2-Data/
│   └── Enums/
│       └── GlobalEnumsAll.cs           → "TagType enum — Unity tags as enum, grows across phases"
├── 4-Utils/
│   └── UtilsPhaseAll.cs                → "shared extensions (HasTag, SetTag) — auto-available via C# extension methods"
└── 6-Shaders/
    └── ShaderGuide.md                  → "ALL shaders, materials, URP settings across all phases — Shader Graph setup, property values, Renderer Features, layers"

Scripts/phase-X/
│
├── 0-Core/                     → "everything depends on me, I have zero game logic"
│
├── 1-Managers/                 → "I'm a singleton that owns one domain's state"
│   └── SubManager/             → "I open/close one UI panel and nothing else"
│
├── 2-Data/                     → "everything inside me is independent — testable via new instance, no scene needed"
│   ├── SO_*.cs  (root)         → "I'm a config blueprint — only fields, no methods"
│   ├── Field_*.cs (root)       → "I display UI — SetData/SetState only, no onClick, no logic"
│   ├── Interface/              → "I'm a contract — no implementation"
│   ├── DataWrapper/            → "I wrap an SO with mutable session state (W prefix)"
│   ├── DataService/            → "I manage a collection — build, query, add, remove, snapshot"
│   ├── Enums/                  → "one GlobalEnumsX.cs per phase — all enums including TagType (no raw string tags)"
│   └── Entities/               → "I'm a grouped small [Serializable] class — entries, save data, structs"
│
├── 3-MonoBehaviours/           → "I have game logic and live in the scene"
│   └── Orchestrator/           → "I wire Field_ prefabs — Instantiate, AddListener, Destroy (only special pattern)"
│
├── 4-Utils/                    → "I'm static — no MonoBehaviour, no state, just helpers and LOG"
│   ├── UtilsPhaseX.cs          → "I'm static extensions + helpers grouped by region (Shop, Economy, Player...)"
│   └── PhaseXLOG.cs            → "I format data collections to JSON for logging — per-collection targeting via .ToNSJson()"
│
└── 5-Tests/                    → "I test one system independently — no other systems needed"
```

### Key Principles

- **2-Data/ is fully independent.** Every class inside can be tested by creating a `new` instance. No scene, no MonoBehaviour, no GameObjects needed. `DEBUG_Check` proves this.
- **Most of 2-Data/ is pure C#** — no Unity dependency. DataWrapper, DataService, Enum, Entities are all plain C# classes (Vector3/Vector2 are fine — they're structs, not Unity components). Only `SO_` (inherits ScriptableObject) and `Field_` (inherits MonoBehaviour) have Unity dependency.
- **3-MonoBehaviours/ has one special subfolder: Orchestrator/.** Everything else is just "MonoBehaviour with game logic." Orchestrator is the only distinct pattern — it wires Field_ instances. Machines, tools, conveyors, buildings are all regular MonoBehaviours grouped by domain when needed (not by pattern).
- **1-Managers/SubManager/ owns its own lifecycle.** UIManager never calls SetActive on SubManagers. They self-init in Start(), disable themselves, listen for GameEvents to re-enable.

### Why This Structure

**Separation by responsibility, not by domain.** Most Unity projects do `Scripts/Shop/`, `Scripts/Player/` — grouping by topic. This structure groups by **what the file does**: `2-Data/` = no logic, `3-MonoBehaviours/` = has logic. When you ask "where does this go?" the answer is about the file's **nature**, not its **topic**.

**What's strong:**
- `2-Data/` independence — testable via `new`, no scene needed. Rare in Unity projects.
- Numbered folders — Unity project panel shows dependency order visually.
- Orchestrator as the only special pattern — no over-categorization.
- Natural MVP: Field_ = View, DataService = Model, Orchestrator = Presenter, ShopUI = Controller.

**What to watch:**
- `3-MonoBehaviours/` will get crowded by Phase E (~80+ files). Add domain subfolders when it gets noisy (`Machine/`, `Tool/`, `Conveyor/`).
- DataService/ grows across phase boundaries — `ShopDataService` (Phase A) + `QuestDataService` (Phase F) coexist. That's fine.

**End goal: 100% of main source functionality** with this architecture to keep it consistent, decoupled, minimal public API, and independently testable via vertical slice tests.

### Where Does It Go?

```
Is it a foundation utility?            → 0-Core/
Is it a singleton owning domain state? → 1-Managers/
Does it only open/close a UI panel?    → 1-Managers/SubManager/
Is it an SO with only fields?          → 2-Data/SO_*.cs
Is it a display-only prefab handle?    → 2-Data/Field_*.cs
Is it a contract with no implementation?→ 2-Data/Interface/
Is it a runtime wrapper around an SO?  → 2-Data/DataWrapper/
Does it manage a collection?           → 2-Data/DataService/
Is it an enum?                         → 2-Data/Enums/GlobalEnumsX.cs
Is it a small serializable struct/class?→ 2-Data/Entities/
Does it wire Field_ + AddListener?     → 3-MonoBehaviours/Orchestrator/
Does it have game logic in the scene?  → 3-MonoBehaviours/
Is it a static helper or LOG formatter?→ 4-Utils/
Is it a test?                          → 5-Tests/
```

### Manager vs MonoBehaviour — The `Singleton<T>` Rule

**If a script extends `Singleton<T>`, it goes in `1-Managers/`.** No exceptions. This takes precedence over PhaseMap.md listings.

The deciding question: **"Does this script own shared state that other scripts query?"**

| Goes in `1-Managers/` | Goes in `3-MonoBehaviours/` |
|---|---|
| Extends `Singleton<T>` | Does NOT extend `Singleton<T>` |
| Other scripts read its state (`.Ins.GetX()`) | Only manages its own GO |
| One instance in the scene | Multiple instances allowed |
| EconomyManager, UIManager, OreManager, OrePiecePoolManager, OreLimitManager, ParticleManager | OreNode, OrePiece, AutoMiner, SellerMachine, PhysicsLimitUIWarning |

**Common mistake:** PhaseMap.md may list a singleton under `3-MonoBehaviours/` because the file list was written before folder rules were finalized. **Always cross-check against this rule.** GOAL.md is the authority — PhaseMap is the roadmap.

---

## Naming

| Kind | Convention | Example |
|------|-----------|---------|
| ScriptableObject | `SO_` prefix | `SO_ShopCategory`, `SO_ShopItemDef` |
| Display-only prefab handle | `Field_` prefix | `Field_ShopItem`, `Field_ShopCartItem` |
| DataWrapper | `W` prefix | `WShopItem`, `WQuest` |
| Interface | `I` prefix | `IInteractable` |
| Dictionary lookups | `DOC__` prefix | `DOC__category_wShopItem`, `DOC__CartItem__Field` |
| List/collection fields | `ALL_CAPS` | `CATEGORY`, `ITEM_DEF`, `CARTITEM` |
| `[SerializeField]` fields | `_` prefix | `[SerializeField] float _defaultMoney` |
| Private non-serialized | `camelCase` | `money`, `isAnyMenuOpen`, `xRot` |
| Event handlers | `Handle...` | `HandleMoneyChanged` |
| GameEvents | `On...` + `Raise...` side by side | `OnMoneyChanged` / `RaiseMoneyChanged()` |
| Singleton access | `.Ins` | `Singleton<EconomyManager>.Ins` |
| Enum values | `camelCase` | `TagType.grabbable`, `OreLimitState.slightlyLimited`, `PieceType.ore` |

**Prefix = no logic.** `SO_` = pure data. `Field_` = display only. `W` = session wrapper. No prefix = has logic.

**TagType enum — no raw string tags.** Unity tags go in `TagType` enum (inside `phase-All/2-Data/Enums/GlobalEnumsAll.cs`). Never use `CompareTag("Grabbable")` or `tag = "string"`. Use extension methods in `phase-All/4-Utils/UtilsPhaseAll.cs` — they're C# extensions so you just call them directly on the object, no class name needed:
- `collider.HasTag(TagType.grabbable)` — replaces `CompareTag("Grabbable")`
- `gameObject.SetTag(TagType.markedForDestruction)` — replaces `tag = "MarkedForDestruction"`
- New tags added to the enum as phases grow (e.g. Phase D: `conveyorBelt`)

---

## Script Structure

`#region` blocks. Every class, method, interface, and enum gets a **beginner-friendly summary + inline flow markers**.

**All summaries are reference-only** — you read them while typing to understand the flow. You do NOT type them. When hand-typing, keep your own one-liner (e.g. `/// recycles ore objects to avoid GC spikes`).

### Class Summary — conversation-style, first person "I"

```csharp
/// <summary>
/// I recycle ore objects so we never call Instantiate/Destroy after the initial pool warmup.
/// When someone needs an ore piece, they ask me via SpawnPooledOre — I check my dictionary
/// of queues (keyed by ResourceType + PieceType + IsPolished). If a recycled piece exists,
/// I dequeue it and reposition it. If the queue is empty, I create a brand new one.
/// When ore is done (sold, fell out of world, etc.), OrePiece calls Delete which comes back
/// to me — I reset everything (velocity, drag, tags) and enqueue it for next time.
///
/// Who uses me: OreNode (spawn drops), AutoMiner (timer spawn), OrePiece.Delete (return).
/// Events I fire: none. Events I subscribe to: none.
/// </summary>
public class OrePiecePoolManager : Singleton<OrePiecePoolManager>
```

### Method Summary — 2-line English explaining what happens inside + `// →` inline steps

**Public methods** — slightly more comprehensive (external callers need to understand the full effect):
```csharp
/// <summary> Takes an ore piece that's done being used (sold, fell, etc.) and puts it back
/// in the pool for reuse — deactivates it, resets all physics and state, re-tags as Grabbable,
/// and enqueues it so the next SpawnPooledOre can reuse it instead of creating a new one. </summary>
public void ReturnToPool(OrePiece piece)
{
    // → find or create queue for this ore type key
    ...
    // → deactivate GO + zero out velocity, angular velocity, put Rb to sleep, restore standard drag
    ...
    // → reset all runtime state: position, rotation, baskets, sieve%, magnet ref, tag back to Grabbable
    ...
    // → parent under pool root + enqueue for next SpawnPooledOre call
    ...
}
```

**Private methods** — same pattern, describes what actually happens (not just the method name):
```csharp
/// <summary> Picks a random drop count between min and max, spawns that many ore pieces via
/// the pool with random velocity (upward + lateral spread), plays break particles at the hit
/// point, fires OnOreMined for quest tracking, then destroys this node permanently. </summary>
void BreakNode(Vector3 hitPosition)
{
    // → pick random drop count between _minDrops and _maxDrops
    ...
    // → for each drop: weighted random prefab from _possibleDrops, spawn via pool, apply random velocity + angular
    ...
    // → spawn break particle burst at hit point
    ...
    // → fire OnOreMined so quest system (Phase F) can track mining progress
    ...
    // → destroy this node GO permanently (Phase G saves broken position)
    ...
}
```

### Interface Summary — what implementors must do

```csharp
/// <summary> Any object that can take damage at a world position. OreNode implements this
/// to lose health when the pickaxe hits it. DamageableOrePiece implements this to break
/// into cluster pieces when hit hard enough by collision. ToolPickaxe calls TakeDamage
/// on whatever IDamageable the raycast hits. </summary>
public interface IDamageable
{
    void TakeDamage(float damage, Vector3 position);
}
```

### Enum Summary — what each value means in context

```csharp
/// <summary> Ore limit throttle state — OreLimitManager checks every 15s how many ore pieces
/// are actively moving. Regular = normal spawning. SlightlyLimited = AutoMiner 25% slower.
/// HighlyLimited = 50% slower. Blocked = AutoMiner stops entirely. PhysicsLimitUIWarning
/// subscribes to OnOreLimitChanged and shows/hides the warning panel based on this. </summary>
public enum OreLimitState
{
    Regular = 0,
    SlightlyLimited = 1,
    HighlyLimited = 2,
    Blocked = 3
}
```

### Unity Lifecycle Methods — ALSO get summaries

Start, Update, Awake, OnEnable, OnDisable, OnCollisionEnter, OnTriggerEnter, OnDestroy, FixedUpdate — **every lifecycle method gets a 2-liner summary** explaining what it does in context. Don't just say "called by Unity" — explain what this specific script does in this hook.

```csharp
/// <summary> Every frame: updates cursor lock state, checks ground, routes input to movement
/// or noclip, applies gravity+slope, and respawns if fallen below y=-200. </summary>
private void Update()

/// <summary> When something collides with me, checks if the impact velocity exceeds the minimum
/// threshold and if enough time has passed since the last play. If both pass, plays impact sound. </summary>
private void OnCollisionEnter(Collision collision)
```

### Inline Flow Markers — `// →` inside method body

Every logical step inside a method gets a `// →` comment describing **what actually happens at that point** — not just the method name being called, but what it does. If a method call does 5 things, list those 5 things.

```csharp
/// <summary> Called when ore enters the SellerMachine trigger. Detaches from magnet if held,
/// tags as MarkedForDestruction so other triggers ignore it, then starts a 2-second coroutine
/// that sells the ore (adds money, fires OnOreSold, returns piece to pool). </summary>
public void SellAfterDelay(float delay = 2f)
{
    // → detach from magnet so it stops pulling this piece
    if (CurrentMagnetTool != null) CurrentMagnetTool.DetachBody(Rb);
    // → tag so other triggers (other sellers, conveyors) skip this piece
    gameObject.tag = "MarkedForDestruction";
    // → after 2s: add money to EconomyManager, fire OnOreSold for quest tracking, return to pool
    StartCoroutine(DelayThenSell(delay));
}
```

Nested types (`CartItem`) go inside the class that owns them.

---

## Minimal Methods

**Top priority.** Every script typed by hand — fewer methods = less to type, less to break.

- **Public API:** only expose what another script absolutely needs to call
- **Private methods:** only create when logic is reused or genuinely needs isolation — otherwise inline it
- **Don't over-fragment.** If a 5-line block is used once, keep it inline.

```
ShopUIOrchestrator:  2 public (Init, BuildCategoryView)
                     5 private (only what's needed)
ShopUI:              1 public (GetDataServiceForTest)
                     0 private (lifecycle only)
EconomyManager:      3 public (GetMoney, AddMoney, CanAfford)
SimplePlayerController: 0 public, 3 private handlers
```

---

## Class Responsibilities

### `SO_` — Pure Data (2-Data/)
Only fields. **Zero methods.** One file per SO.
If the original source has helper methods on an SO (e.g. `GetOrePrefab()`, `GetPrimaryResourceType()`), move them to the **consumer** that reads the SO (the MonoBehaviour, DataService, or Orchestrator). The SO exposes its fields as `public` — the consumer reads them and does the logic.
This keeps SO_ as pure config blueprints that the inspector can edit without any code coupling.

### `Field_` — Display Only (2-Data/)
Inspector refs + display setters: `SetData(...)`, `SetButtonInteractable(...)`, `SetPrice(...)`, `SetQty(...)`.
Sets `.text`, `.color`, `.sprite`, `.interactable` — nothing else.
**No** onClick wiring. **No** business logic. **No** singleton access.

### `W` — DataWrapper (2-Data/DataWrapper/)
Plain C# wrapping SO + mutable session state.
`WShopItem` wraps `SO_ShopItemDef` + `isLockedCurr` + `timesPurchased`.
**SO = what it IS. W = what happened to it.**

### DataService (2-Data/DataService/)
One service per domain. Plain C#. Manages ALL collections for that domain.
`ShopDataService` — builds W wrappers, category queries, cart operations, afford check, snapshot.
Nested types live here (`CartItem` inside `ShopDataService`).
Has `GetSnapShotForTest(header)` that combines all PhaseXLOG calls into one formatted output.

**When to create a DataService:**
- Operations are pure logic (add, remove, query, sort, total) — no Unity API needed
- Testable via `new` instance in `DEBUG_Check` — zero scene, zero GameObjects
- Examples: InventoryDataService (slots, switch, stack), ShopDataService (categories, cart, afford), OreDataService (resource descriptions, color formatting)

**Actively look for DataService opportunities in every phase.** If a Manager or MonoBehaviour has pure C# logic that could be extracted into a testable class, extract it. The question: "Can I test this logic by calling `new DataService()` with no scene?" If yes → it's a DataService.

Common candidates that should be DataServices:
- **Collection management** — any `List<T>` / `Dictionary<K,V>` with Build/Get/Add/Remove operations
- **Lookup/query** — resource descriptions by type, quest progress by ID, save file headers by path
- **Formatting** — colored resource strings, money formatting, quest requirement text
- **Validation** — placement checks (pure math), afford checks, requirement checks

**When NOT to create a DataService — keep in MonoBehaviour:**
- Operations need Unity physics (SpringJoint, Rigidbody, AddForce, OverlapSphere)
- Operations need Unity lifecycle (Update, FixedUpdate, Destroy, Instantiate)
- Data is transient frame state (velocity, bob counter) not a persistent collection
- Example: ToolMagnet manages `List<Rigidbody>` + SpringJoints — every operation needs Unity. Not a DataService.

### Managers (1-Managers/)
Singleton MonoBehaviours. One domain, one manager.
`EconomyManager` — owns money via `GetMoney()`, `AddMoney()`, `CanAfford()`.
`UIManager` — reports `IsInAnyMenu()`. Nothing else.

### SubManagers (1-Managers/SubManager/)
One panel's lifecycle. **Open/close + init only.**
`ShopUI` — builds DataService on Start, inits Orchestrator, subscribes toggle event. Zero business logic.
`BgUI` — blur on/off via `GameEvents.OnMenuStateChanged`.

### Orchestrator (3-MonoBehaviours/Orchestrator/)
Wires `Field_` prefab instances. All `AddListener` calls live here.
Reads from DataService. Refreshes on events only — never `Update()`.
`ShopUIOrchestrator` — BuildCategoryView, SelectCategoryView, RepopulateShopItemsView, createAndOrchestrateCartItemField, PurchaseCart.

### MonoBehaviours (3-MonoBehaviours/)
World objects, systems, player. Each has minimal public API (ideally zero).

### Utils (4-Utils/)
`UtilsPhaseX` — static extensions + helpers grouped by `#region`. One per phase.
`PhaseXLOG` — snapshot formatters targeting specific collections.

**important: Actively extract reusable logic into UtilsPhaseX.** If a method appears in 2+ scripts, or if a MonoBehaviour has a pure static helper that doesn't need `this`, it belongs in Utils. Examples:
- `UtilsPhaseA.formatMoney()` — used by ShopOrchestrator + MoneyHUD
- `UtilsPhaseB.IgnoreAllCollisions()` — used by PlayerGrab + ToolMagnet
- `UtilsPhaseC.WeightedRandom<T>()` — used by OreNode + AutoMiner + future OrePiece sieving
- `UtilsPhaseC.PickOrePrefab()` — reads SO fields + filters gems + weighted select

**Rule:** If logic was on an SO_ (moved per "SO_ = pure data" rule) and it's reusable, put it in UtilsPhaseX, not in a single consumer. If only one consumer uses it, inline it in that consumer.

`PhaseALOG` — snapshot formatters **targeting specific collections separately**:
```
LIST_CATEGORY__TO__JSON(CATEGORY)           → snapshot of category list
DOC_CATEGORY_ITEM__TO__JSON(DOC)            → snapshot of category→items dictionary
LIST_CARTITEM__TO__JSON(CARTITEM)           → snapshot of cart items
```
All serialization via `.Select()` + anonymous types + `.ToNSJson(pretify: true)` from `SPACE_UTIL`.
PhaseXLOG methods are called by DataService's `GetSnapShotForTest()` — single entry point for test logging.

### Tests (5-Tests/)
**Two levels of testing:**
1. **Data-level** (`DEBUG_Check`) — tests anything in `2-Data/` by creating a plain C# instance. Zero dependency. No UI, no scene, no MonoBehaviour needed. DataService, DataWrapper, Entities — all testable this way.
2. **UI-level** (`ShopUITest`) — tests full UI flow with keyboard shortcuts listed in inspector README field.

Each test is independent. Lists prerequisites in summary or inspector field.

---

## Decoupling

- **`GameEvents`** — static event bus. Event + Raise grouped by domain with `// when X >>` `// << when X` comment blocks.
  Each Raise calls `LogSubscribersCount()`: `[GameEvents] OnX raised for -> N subscribers`
- **Every `.Raise...()` call must have a `// purpose:` one-liner** explaining why it's fired and who listens:
  ```csharp
  // purpose: cursor lock/unlock for player controller
  GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
  
  // purpose: MoneyOrchestrator updates HUD text
  GameEvents.RaiseMoneyChanged(money);
  ```
- **Interfaces** — for abstraction (`IInteractable`)
- **Singleton reads** — only for queries (`Singleton<EconomyManager>.Ins.GetMoney()`), never cross-system commands
  **Exception:** `Singleton<UIManager>.Ins.CloseAllSubManager()` is acceptable — UIManager is the centralized panel controller, closing all panels is its job.
- **Never** let Script A directly call into unrelated Script B
- **Event-driven refresh** — never poll in `Update()`. Refresh only when state changes.

### Cross-Phase File Changes

When a new phase needs to add to an existing file:

- **`GameEvents` (static class)** → use `partial class`. Each phase adds its own events in its own `0-Core/GameEvents.cs`. No modification to earlier phase's file. Phase A's GameEvents must have `partial` keyword.
  ```
  phase-a/0-Core/GameEvents.cs    → OnMoneyChanged, OnMenuStateChanged, OnOpenShopView, OnCloseShopView, OnOpenInteractionView, OnCloseInteractionView, OnUnlockedCategory
  phase-a-1/0-Core/GameEvents.cs  → OnElevatorLanded, OnGamePaused, OnGameUnpaused
  phase-b/0-Core/GameEvents.cs    → OnToolSwitched, OnItemPickedUp, OnItemDropped
  ```
- **MonoBehaviours with `[SerializeField]`** (UIManager, InteractionSystem) → must be **directly modified** because inspector fields can't be added via partial across files. Document in GUIDE.md exactly what to change and why.
- **Rule:** prefer `partial` extend over direct modify. Only modify when `[SerializeField]` or inheritance requires it.
- **Every phase GUIDE.md** lists modifications in a table: `| File | Change | Why |`

---

## No Defensive Null Checks

Let it crash. The crash is traceable.
Inspector refs, core singletons, `Field_` components — your responsibility to wire.
Don't hide bugs behind `?.` or `if (x != null)`.

**Exception:** `?.Invoke()` on GameEvents (events can have zero subscribers).

---

## Unity Lifecycle Order

```
Scene loads, GO is active:

  Awake()           ← once, first. Singleton registration, GetComponent caching.
      ↓
  OnEnable()        ← fires immediately after Awake (same frame). 
      ↓               Fires BEFORE Start. Fires every time GO re-enables.
  Start()           ← once, after ALL Awake + OnEnable across scene.
      ↓               Safe to read other singletons. Build data, init, subscribe.
  Update()          ← every frame
  LateUpdate()      ← every frame, after all Update()
      ↓
  OnDisable()       ← fires when GO disabled (SetActive false) or destroyed
  OnDestroy()       ← fires when GO destroyed (scene unload or Destroy())

Re-enable (SetActive true):
  OnEnable()        ← fires again (Start does NOT re-run)
      ↓
  Update() resumes

Key facts:
  - Awake + OnEnable fire together BEFORE Start
  - Start runs ONCE — never again even if re-enabled
  - OnEnable fires EVERY enable (first time + every re-enable)
  - Destroy() auto-cleans onClick/UnityEvent listeners
  - SetActive(false) does NOT clean listeners

CRITICAL: execution order of Awake/OnEnable is NOT guaranteed.
  - Single component: Awake → OnEnable → Start (guaranteed for THAT component)
  - Multiple components on SAME GO: ComponentA.Awake may or may not run before ComponentB.Awake
  - ACROSS GOs: completely unpredictable order
  - So any component's OnEnable could fire BEFORE another component's Awake — even on the same GO
  - Only Start() is guaranteed to run AFTER ALL Awake+OnEnable across the entire scene
  - But Start() only runs ONCE — can't use it for re-enable logic
  - This is why isFirstEnable exists — safe regardless of execution order
```

### Why `isFirstEnable` — Not Awake/Start

**Problem:** Awake + OnEnable execution order across GOs is not guaranteed. If SubManager A's OnEnable fires before Manager B's Awake, referencing B crashes. Start() runs after all Awake — but Start only runs once, so you can't use it for re-enable subscribe/announce logic.

**Solution:** `isFirstEnable` flag in OnEnable. First call: do setup (subscribe, build, self-disable, return early). Subsequent calls: announce menu state. This way:
- Setup happens in OnEnable (guaranteed to fire)
- No dependency on Awake order of other GOs (singletons register in their own Awake)
- Re-enable works naturally (isFirstEnable is false, goes straight to announce)
- No Start() needed for subscriptions — everything lives in OnEnable

**This replaces the Awake+Start pattern for SubManagers.** Managers (singletons) still use Awake for registration.

### SubManager Pattern — `isFirstEnable` (user's actual pattern)

The user's hand-typed SubManagers use `isFirstEnable` flag:

```csharp
bool isFirstEnable = true;
private void OnEnable()
{
    if (isFirstEnable)
    {
        // subscribe to open/close events, build data, init orchestrator
        GameEvents.OnOpenShopView += () => this.gameObject.SetActive(true);
        GameEvents.OnCloseShopView += () => this.gameObject.SetActive(false);
        this.gameObject.SetActive(false); // self-disable after setup
        isFirstEnable = false;
        return; // skip RaiseMenuStateChanged on first enable
    }
    GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
}
private void Update()
{
    if (INPUT.K.InstantDown(KeyCode.Escape) || INPUT.K.InstantDown(KeyCode.E))
        this.gameObject.toggle(value: false);
}
private void OnDisable()
{
    GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false);
}
```

**Why `isFirstEnable`:** avoids the first-frame OnEnable→RaiseMenuStateChanged(true) pulse. First enable does setup + self-disable. Subsequent enables announce menu state.

### Usage Table

| Hook | Use For | Pair With |
|------|---------|-----------|
| `Awake()` | Singleton registration, `GetComponent` caching | — |
| `Start()` | Build data, init orchestrator, subscribe events, disable self | `OnDestroy()` |
| `OnEnable()` | `RaiseMenuStateChanged(true)`, event subs for toggled panels | `OnDisable()` |
| `OnDisable()` | `RaiseMenuStateChanged(false)`, unsub events | — |
| `Destroy()` | Auto-cleans onClick/UnityEvent listeners | — |
| `SetActive(false)` | Does NOT clean listeners | — |

---

## C# Features (Unity 6000.3+ / .NET 2.0+)

**Allowed — use actively to reduce verbosity:**
- `$""` — string interpolation everywhere
- `?.` — null-conditional on any reference: `obj?.Method()`, `spawned?.Rb`, `_viewModel?.SetActive(!hide)`
- `??` — null-coalescing: `result ?? fallback`, `dict.TryGetValue(key, out var v) ? v : new()`
- `=>` — expression-bodied methods/properties: `public float GetSellValue() => BaseSellValue * multiplier;`
- Ternary `? :` — inline conditionals: `isDucking ? _duckSpeed : _walkSpeed`
- LINQ + extensions — `.Select()`, `.Where()`, `.Any()`, `.All()`, `.First()`, `.FirstOrDefault()`, `.Last()`, `.Count()`, `.OrderBy()`, `.OrderByDescending()`, `.GroupBy()`, `.Distinct()`, `.Skip()`, `.Take()`, `.Aggregate()`, `.ToDictionary()`, `.ToList()`, `.FindAll()`, `.FindIndex()`, `.Exists()`, `.TrueForAll()`
- Custom extensions — `.sum()`, `.find()`, `.map()`, `.GetOrCreate()` from SPACE_UTIL + IEnumerableUtilsPhaseAll
- `var` — for obvious types: `var queue = pools.GetOrCreate(key);`

**Reduce verbosity wherever possible:**
- `if (x != null) x.DoThing()` → `x?.DoThing()` when it's a single call
- `if (!dict.TryGetValue(key, out var v)) { v = new(); dict[key] = v; }` → `dict.GetOrCreate(key)`
- Manual `foreach` accumulation → `.sum(selector)` extension
- Keep `if (x == null) return;` guard clauses when the block does multiple things — `?.` can't replace those

**Not allowed:** `async/await` (use coroutines), `Span<T>`, `Memory<T>`, `ValueTuple` deconstruction

---

## LEGO Independence

Each system works **standalone first**, connects later via GameEvents.
A script must **never** require an unfinished system to compile or run.

```
Type DataService → test with DEBUG_Check (no UI, no scene)
Type ShopUI + Orchestrator → test with ShopUITest (no player, no interaction)
Type InteractionSystem → test with InteractionTest (no shop, no player)
Type SimplePlayerController → test with PlayerControllerTest (no shop, no interaction)
Connect via GameEvents → full phase works
```

---

## Vertical Slice Tests

**Two levels per system:**
1. **Data-level** — test DataService logic without UI (`DEBUG_Check` — plain `new` instance, zero scene)
2. **UI-level** — test full system flow with keyboard shortcuts (`ShopUITest`, `PlayerGrabTest`, etc.)

**Each test in GUIDE.md must have ALL of these:**
- **Internal prerequisites** — which scripts must be typed first to compile
- **External prerequisites** — exact scene setup: which GOs, which components, what to assign in inspector
- **NOT required** — explicitly list which OTHER systems are NOT needed (proves LEGO independence)
- **Controls** — keyboard shortcuts for manual testing
- **Checklist** — pass/fail items to verify

**Each test is a standalone scene.** You should be able to create a fresh empty scene, follow the external prerequisites, and the test works without any other system present. If a test requires another system to run, it's not independent — fix the architecture.

**Each Test script (.cs) must contain:**
- **Summary comment** with: prerequisites, NOT required, "How to test" step-by-step (what to do + what to expect), controls
- **`// purpose:` one-liner** on every `Raise` call and `+=` subscription inside the test
- **Console logging** via GameEvents subscription — proves the system fired the right events without manual visual inspection
- **Minimal code** — the test script is a bootstrap. The actual system handles its own input/logic in its own `Update()`. The test just: locks cursor, provides sim keys (M/N for menu), subscribes to events for logging.
- **No duplicate logic** — never re-implement the system's behavior inside the test. If the test needs to trigger something, fire a GameEvent (e.g. `RaiseToolPickupRequested`) rather than calling methods directly.

**Manual Test Guides (`5-Tests/Manual/*.md`):**
When a system requires visual/hands-on verification (UI flows, 3D animations, physics, visual effects, audio), create a `.md` file in `5-Tests/Manual/` with:

- **Prerequisites** — exactly which singletons, prefabs, and test scripts must exist in the scene
- **Setup Guide** — beginner-level step-by-step Unity Editor instructions:
  - Every GO to create, what to name it, where to parent it
  - Every component to add, with exact `[SerializeField]` values and wiring (which field → which GO)
  - Prefab hierarchy: every child GO with its components, RectTransform anchors/sizes, Image colors, raycastTarget flags
  - Inspector values: defaults, ranges, layer masks, tags
  - Wiring tables: `| Field | Drag From |` format for complex components
  - Final hierarchy tree showing parent-child relationships
- **How It Works (System Flow)** — before the DO/EXPECT test steps, explain the system's **end-to-end data flow** in conversation-style plain English. This section is the heart of the manual test — it teaches the reader the full architecture of the system being tested, not just how to click buttons. Break it into labelled paragraphs per major action (e.g. "Scene loads:", "Tool pickup:", "Drag-drop:", "Open/close:"). Each paragraph traces the full path:
  - Which script method is called first
  - Which GameEvent fires (and which scripts are subscribed)
  - Which GOs become `SetActive(true/false)` and why
  - Which component fields change (e.g. `_background.color = _selectedColor`)
  - Which Unity lifecycle callbacks trigger as a result (e.g. `OnEnable → RaiseMenuStateChanged(true)`)
  - What the player sees on screen as the end result

  **Example — Inventory drag-drop (from InventoryUITest.md):**

  > **Drag-drop:** When you click-and-drag a slot, `UIEventRelay.OnBeginDrag` fires → orchestrator stores `dragFromIndex`, calls `FIELD_SLOT[i].SetDragVisible(false)` which **hides the `HideWhenDragged` child GO** (`SetActive(false)`) — the slot *looks empty*. The `DragGhostIcon` GO **activates** with the dragged item's sprite. Every frame during drag, `OnDrag` moves the ghost to cursor position. On drop over another slot, `UIEventRelay.OnDrop` fires → `dataService.Swap(from, to)` exchanges the two slots in data → `RefreshAllSlots()` updates all displays. `OnEndDrag` restores the source slot visual and **deactivates** the ghost.

  **Example — Mining flow (from MiningFlowTest.md):**

  > **Breaking:** `BreakNode` picks a random drop count between `_minDrops` and `_maxDrops`. For each drop, `UtilsPhaseC.WeightedRandom(_possibleDrops, d => d.Weight)` selects an `OrePiece` prefab. Then `Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(prefab, pos)` either **dequeues a recycled piece** from the pool or **Instantiates a new one**. Each spawned piece gets random velocity — they **fly out and tumble**. `ParticleManager.CreateParticle(BreakOreNodeParticlePrefab, hitPosition)` spawns a burst. `GameEvents.RaiseOreMined(resourceType, position)` fires for quest tracking. Finally `Destroy(gameObject)` removes the node.

  **Example — Grab rope (from GrabRopeTest.md):**

  > **Grab:** `PlayerGrab.TryGrab()` raycasts from `_cam`. If the hit tag is `"Grabbable"`, calls `GrabObject(hit)`. Inside: the **RigidbodyDragger child GO activates** (`SetActive(true)`), a `SpringJoint` is **added at runtime** to the dragger and connected to the cube's `Rigidbody`. `UtilsPhaseB.IgnoreAllCollisions(cube, player, true)` prevents clipping. The cube's `linearDamping` increases (feels heavy). The **LineRenderer enables** with `positionCount = 2`.

  This teaches the reader WHY things happen, not just WHAT they see. Keep it **bold** for GO state changes, `code` for method/field names, *italics* for important context.
- **Manual Test Flow** — numbered DO/EXPECT steps:
  - Each step: one player action (press key, wait, drag, click)
  - Each EXPECT: exactly what the screen/console should show — **bold** for visual changes, `code` for console messages
  - **Also explain WHAT HAPPENS BEHIND THE SCENES** for each step:
    - Which script method runs
    - Which GameEvent fires (and who receives it)
    - Which GOs become active/inactive (`SetActive(true/false)`)
    - Which component fields change (e.g. `_background.color = _selectedColor`)
    - Which Unity callbacks trigger (e.g. `OnEnable → RaiseMenuStateChanged(true)`)
  - Cover: initial state → primary action → edge cases → error conditions
- **Summary Checklist** — pass/fail items at the end

**The Manual/*.md must be comprehensive enough for a beginner** who has never used Unity to follow AND must teach the internal flow so the reader understands the full data path. It's not just "click → see result" — it's "click → this script fires → this event reaches this subscriber → this GO activates → you see this on screen." The reader should understand the architecture by reading the manual test.

These are purely hand-tested — no script automates them. The `.md` is the test. One file per system.

**When building any phase, always ask:** "Are there any systems in this phase that need visual/hands-on verification?" If yes, create a `Manual/*.md` for each. Analyse the main source for UI panels, animations, physics visuals, and any complex inspector setup that a `.cs` test can't verify. See PhaseMap.md + StructureMap.md for the full list of required manual tests per phase.

----

## Hand-Typing Order

1. `#region` blocks (empty skeleton)
2. Inspector Fields (the data shape)
3. Private fields (state)
4. Unity Lifecycle (Start, OnEnable, OnDisable)
5. Public API (only what's needed — least possible)
6. Private API (only what's needed — least possible)
7. **Compile → Test → Next script**

---

## MANDATORY: Beginner-Level Documentation

**The following files MUST be written for someone with almost zero knowledge** — both zero knowledge of this codebase AND zero knowledge of Unity scene setup. This is non-negotiable. If a beginner can't follow it from scratch, it's not done.

| File | Audience Assumption |
|------|-------------------|
| `GUIDE.md` | Never seen this codebase. Needs: what each script does in one sentence, exact typing order, exact scene setup with every GO/component/wiring, full test steps with DO/EXPECT |
| `FLOW.md` | Understands basic coding but not this architecture. Needs: conversation-style prose explaining data flow, ASCII system maps, event registry |
| `5-Tests/*.cs` | Never used these test scripts. Needs: summary comment with prerequisites, NOT required, "How to test for that specific system" steps, controls, what console output to expect |
| `5-Tests/Manual/*.md` | A Fully Detailed Comprehensive Guide For Never used Unity Editor. Needs: every GO to create (name, parent), every component to add, every `[SerializeField]` to wire (`\| Field \| Drag From \|` tables), prefab hierarchies with RectTransform values, numbered DO/EXPECT steps, edge cases, summary checklist |

**"Beginner-level" means:**
- No "see GUIDE.md for details" shortcuts in Manual tests — each `.md` is **self-contained**
- No assumed knowledge of which singletons exist or what they do — list every one needed
- No assumed knowledge of Unity concepts — explain "IsTrigger = true" means "objects pass through but fire OnTriggerEnter", explain "tag" means "a label you set in Inspector"
- Inspector wiring uses `| Field | Drag From |` tables — never "wire the usual refs"
- Every GO in scene setup has: name, parent, components, and field values
- Every test has: initial state → primary action → edge cases → error conditions → checklist

## GUIDE.md Per Phase

**GUIDE.md is beginner-friendly** — same voice as FLOW.md. Written so someone who has never seen this codebase can follow it. Explains *why* each script exists, not just *what* it is. Uses plain English, avoids jargon, includes concrete examples where helpful.

Every phase folder has a `GUIDE.md` with:
- **What it looks like when running** (detailed, conversational — describe the player experience)
- **Folder structure**
- **Script Purpose** — one sentence per script (if it doesn't fit one sentence, split it)
- **Hand-typing order** (compile groups with stop-and-test points)
- **Vertical Slice Tests** — beginner-friendly step-by-step for each `.cs` test:
  - Conversational intro explaining what this test proves
  - "What you need to type first" / "What you DON'T need"
  - Step-by-step scene setup (numbered: create GO, add component, wire fields with `| Field | Drag From |` tables)
  - "How to test" table: `| Key | What it does | What you should see |`
  - "Full test flow" (ordered steps for complex tests: "do this first, then this, expect this")
  - Checklist — pass/fail items
- **Art & Scene Work (Non-Script)** — everything the user needs to create in Unity Editor:
  - Animation assets: clip names, AnimatorController state machine flow (states, transitions, HasExitTime settings)
  - Audio: which clips are needed, where they play, what triggers them (even if SoundManager is Phase H — list them as stubs so the user knows what to prepare)
  - Highlight/shader profiles or presets to create as assets
  - Prefab hierarchies: tool prefab (WorldModel/ViewModel children), UI prefab (Field_ GO hierarchy), etc.
  - Layers & Tags required
  - Any SO assets to create (e.g. SO_InteractionOption "Take"/"Destroy")
- **Scene setup** (step-by-step — every GO, every component, every `[SerializeField]` wiring)
- **Modifications to Earlier Phases** (table: File | How | Change | Why)
- **Source vs Phase diff** (what original did vs what we changed)
- **Systems & Testability** (at the end):
  - **Individual Systems** table — each system's name, scripts, and how it's decoupled (which GameEvents)
  - **Testability Matrix** table — which `.cs` test and `Manual/*.md` covers each system, and what other systems it does/doesn't need
  - Final count: X systems, Y scripts, Z `.cs` tests, W manual tests. Zero tight coupling.

Every phase folder also has a `FLOW.md` with:
- **System Map** — ASCII box diagram showing all systems in that phase, what each owns, how they connect
- **Data Flows** — one per major user action (e.g. "Tool Pickup", "Tool Switch", "Open Inventory", "Drag-Drop"). Written in **conversation-style plain English** with `code references`, **bold** for key visual moments, *italics* for context. NOT swim lanes or ASCII tables — readable prose that explains what happens step by step.
- **Event Registry** — table of every GameEvent in that phase: who fires it, who subscribes, why
- Every connection in the flow must be either a GameEvent or a `[SerializeField]` — if you find a direct method call across systems, that's tight coupling and should be refactored
- See `phase-b(New)/FLOW.md` as the reference example

----

## Phase Overview

| Phase | Name | Weight | Difficulty |
|-------|------|--------|------------|
| A | World Interaction + Shop Cart | 7% | Easy |
| A½ | The Mine — Environment & Elevator | 3% | Easy |
| B | Player Controller + Inventory + Tools + Grabbing | 15% | Hard |
| C | Mining & Ore System | 14% | Medium |
| D | Building & Conveyor System | 14% | Hard |
| E | Ore Processing Machines | 18% | Medium |
| F | Quest & Research System | 10% | Medium |
| G | Save/Load System | 8% | Hard |
| H | Sound, Settings & UI Polish | 5% | Easy |
| I | Contracts, World Events & Menus | 4% | Easy |
| J | Debug, Demo & Final Polish | 2% | Easy |

---

## For Future Agents

This section captures hard-won decisions from Phase A + B implementation. Read before building any phase.

### Always Do First
1. **Read the original source file** in `Scripts/Assembly-CSharp/` before writing any script. Match the behavior 100%.
2. **Read GOAL.md** (this file) for architecture rules + naming conventions.
3. **Read the target phase section in PhaseMap.md** for file list + modifications table.
4. **Check existing phases** for patterns — Phase A (shop) and Phase B (player/inventory) are the reference implementations.

### Splitting Rules
- **Split when one sentence isn't enough.** Original `PlayerController.cs` (888 lines) → 4 scripts (Movement, Camera, Grab, Outline). Each fits one sentence.
- **Don't split when the file is small + single purpose.** `ShopTerminal.cs` (22 lines) stays as-is. `ToolMiningHat.cs` (20 lines) stays as-is.
- **Original god-objects WILL need splitting.** `ComputerShopUI` → ShopUI + ShopUIOrchestrator + ShopDataService. `PlayerController` → PlayerMovement + PlayerCamera + PlayerGrab + PlayerOutline. Expect similar splits for future large files.

### Inheritance
- **Keep the original inheritance chain** unless it's genuinely unnecessary. `BasePhysicsObject → BaseSellableItem → BaseHeldTool → ToolPickaxe` exists because Phase C's `OrePiece` shares `BaseSellableItem`. Don't flatten chains that future phases depend on.
- **Interfaces go in 2-Data/Interface/.** Stub them if the full implementation comes in a later phase (e.g. `ISaveLoadableObject` stub in Phase B, expanded in Phase G).

### Cross-Phase Pattern
- **`GameEvents`** — always use `partial class`. Each phase adds events in its own `0-Core/GameEvents.cs`. Phase A's file needs the `partial` keyword.
- **MonoBehaviours** — when modifying existing scripts (adding `[SerializeField]`, changing method body), document it in the GUIDE.md modifications table: `| File | How | Change | Why |`
- **Prefer extending over modifying.** If you can add behavior via GameEvents subscription instead of editing an existing script, do that.

### The User's Coding Style
**Read `learn/handTyped(latest)/` to see the ACTUAL code.** Match this style, not generic C#.

Conventions:
- `#region` blocks (not `// ───` comments)
- `W` prefix for DataWrappers (`WShopItem`, not `ShopItem`)
- `DOC__` prefix for Dictionary lookups (`DOC__category_wShopItem`, `DOC__CartItem__Field`)
- `ALL_CAPS` for List/collection fields (`CATEGORY`, `ITEM_DEF`, `CARTITEM`)
- `.Ins` for Singleton access (not `.Instance`)
- `// purpose:` one-liner on every `.Raise...()` call and every `+=` subscription
- `// when X >>` and `// << when X` comment blocks around GameEvents in GameEvents.cs
- No blank lines between `#endregion` and next `#region`
- Least possible public API. Least possible private methods. Don't over-fragment.
- **`[SerializeField]` fields are ALWAYS private** — never `[SerializeField] public`. If another script needs to read a field, add a `Get...()` method. If another script needs to write it, add a `Set...()` method. Only add getters/setters for fields that are actually accessed externally — audit who reads what before exposing.
  ```csharp
  // ❌ WRONG — exposes field to everything
  [SerializeField] public Material GhostMaterial;
  
  // ✅ RIGHT — private field + getter only if external scripts need it
  [SerializeField] Material _ghostMaterial;
  public Material GetGhostMaterial() => _ghostMaterial;
  ```
- **No C# property accessors** (`{ get; set; }`, `{ get => ...; set => ...; }`). Always use explicit `Get...()` / `Set...()` methods. Properties look like fields but hide logic — `Get/Set` methods make the access cost visible.
  ```csharp
  // ❌ WRONG — property accessor
  public float Speed { get => _speed; set { _speed = value; Recalculate(); } }
  
  // ✅ RIGHT — explicit Get/Set methods
  public float GetSpeed() => _speed;
  public void SetSpeed(float val) { _speed = val; Recalculate(); }
  ```
- **Exceptions to Get/Set rule:**
  - `[Serializable]` entities/structs (data containers) — public fields are standard (`OrePieceKey.ResourceType`, `BuildingRotationInfo.Rotation`)
  - `SO_` ScriptableObjects — pure data, public fields by convention
  - Interface property contracts (`ISaveLoadableObject.HasBeenSaved { get; set; }`) — C# interfaces require property syntax
  - Static self-registration lists (`AllConveyorBelts`, `AllOrePieces`) — `{ get; private set; }` is standard for static collections
- The user types every script by hand — keep files concise.

SPACE_UTIL extensions the user has (use these, don't reinvent):
- `.map()` — same as `.Select()` from LINQ
- `.find()` — same as `.FirstOrDefault()`
- `.all()` / `.any()` — same as LINQ `.All()` / `.Any()`
- `.gc<T>()` — same as `.GetComponent<T>()`
- `.destroyLeaves()` — destroys all children of a Transform
- `.toggle(value)` — same as `.SetActive(value)`
- `.colorTag("color")` — wraps string in rich text color tag for Debug.Log
- `.formatMoney()` / `.formatMoneyShort()` — `$"${amount:#,##0.00}"` / `$"${amount:#,##0.##}"`
- `.parseInt()` — parses string to int
- `.getRandom()` — random element from list
- `.repeat(n)` — repeats a char n times
- `.ToNSJson(pretify: true)` — Newtonsoft JSON serialization
- `C.method(this)` — logs `[ClassName.MethodName]` for debug
- `INPUT.K.InstantDown(KeyCode)` — same as `Input.GetKeyDown`
- `INPUT.UI.SetCursor(isFpsMode)` — cursor lock/visibility
- `LOG.AddLog(string, "json")` — writes to persistent log file

### Tight Coupling Red Flags — NEVER Do These
The user WILL push back hard if you do any of these. Catch them before the user does.

```
❌ FindObjectOfType<AnyType>()          → use [SerializeField], Owner chain, or GameEvents
❌ Singleton<X>.Ins.DoSomething()        → fire GameEvents.Raise...(), let X subscribe
   (Exception: Singleton reads for queries like EconomyManager.Ins.GetMoney() are OK)
❌ Script A calls Script B's method directly (cross-system)
                                         → fire GameEvents, B subscribes
❌ Tool calls FindObjectOfType<InventoryOrchestrator>()
                                         → tool fires GameEvents.RaiseToolPickupRequested(this)
❌ MonoBehaviour accesses another MonoBehaviour via FindObjectOfType
                                         → use [SerializeField] inspector ref or Owner.GetComponent<>()
```

### Public API Obsession — The User's #1 Priority
**Before making ANY method public, ask: "does another script ACTUALLY call this?"**
If no → it's private. If only subclasses → it's protected. If only same class → inline it.

```
❌ WRONG: public void Release() on PlayerGrab
   → nobody calls it externally. Grab state is via GameEvents. Make it private.

❌ WRONG: public void TryAddTool() on InventoryOrchestrator
   → only called from HandleToolPickup event handler. Make it private.

❌ WRONG: public void ToggleLight() on ToolMiningHat
   → only called from own OnEnable/OnDisable. Make it private.

❌ WRONG: public GameObject WorldModel on BaseHeldTool
   → only this class and subclasses use it. Make it [SerializeField].

✅ RIGHT: public void Init() on Orchestrator
   → SubManager.Start() calls it. Genuinely external.

✅ RIGHT: public virtual void PrimaryFire() on BaseHeldTool
   → InventoryOrchestrator calls active.PrimaryFire(). Genuinely external.

✅ RIGHT: public float GetMoney() on EconomyManager
   → multiple scripts query money. Genuinely external.
```

**After writing every script, audit: can any public method be made private/protected?**

### Splitting Judgment — Real Examples

```
SPLIT — ShopUI (260 lines doing 4 things):
  → ShopUI (SubManager: toggle only, 40 lines)
  → ShopUIOrchestrator (wire Field_, 150 lines)
  → ShopDataService (collections, 45 lines)
  → ShopCartService (cart math, 55 lines) — merged into ShopDataService by user
  WHY: 4 distinct responsibilities. SubManager shouldn't wire UI. Data shouldn't need Unity.

SPLIT — PlayerController (888 lines doing 10 things):
  → PlayerMovement (WASD, jump, slope, duck)
  → PlayerCamera (look, FOV, bob)
  → PlayerGrab (SpringJoint, rope)
  → PlayerOutline (FresnelHighlighter)
  WHY: each fits one sentence. Camera bob has nothing to do with grab physics.

DON'T SPLIT — ToolMagnet (108 lines):
  → manages List<Rigidbody> + SpringJoints, but every operation needs Unity physics
  → DataService would be an empty list wrapper — no testable logic
  WHY: data is inseparable from Unity physics. One file, one purpose.

DON'T SPLIT — ShopTerminal (22 lines):
  → implements IInteractable, fires one GameEvent
  WHY: already one sentence. Splitting would create 2 files with 11 lines each.

DON'T SPLIT — ToolMiningHat (20 lines):
  → toggles a light on enable/disable
  WHY: trivially small, one purpose.
```

### Self-Init Lifecycle — `isFirstEnable` Pattern
Every UI SubManager uses this pattern. See SubManager Pattern section above for full code.

**Key facts:**
- `isFirstEnable` flag skips RaiseMenuStateChanged on first OnEnable — no false pulse
- First OnEnable: subscribe events + build data + self-disable. Return early.
- Subsequent OnEnable: announce menu state normally
- Start() is NOT used for subscriptions — everything happens in first OnEnable
- OnDisable: always announce menu closed
- UIManager never calls SetActive on SubManagers — they own their own lifecycle

### Small SubManagers with Inline Orchestrator
When a SubManager's orchestration is small (e.g. InteractionWheelUI — just spawn/destroy a few buttons), the Orchestrator logic can live **inline** in the SubManager using `#region Orchestrator(Since its just one tab)`.
No separate Orchestrator file needed. Only split when orchestration is large (ShopUIOrchestrator = 150+ lines).

### UIManager — Grows Per Phase

UIManager → "I report menu state + close all panels + route keyboard input with priority"

Three responsibilities that evolve:

| Responsibility | What | When |
|---------------|------|------|
| **Report state** | `isAnyMenuOpen` property — scripts read it | Phase A (always) |
| **Close all panels** | `CloseAllSubManager()` — fires all Close events | Phase A (always) |
| **Route keyboard with priority** | ESC/Q/Tab — only opens panel if others aren't blocking | Phase F+ |

**Growth roadmap (minimal — add only what the phase needs):**

```
Phase A:
  isAnyMenuOpen (property, set via OnMenuStateChanged subscription)
  CloseAllSubManager() → RaiseCloseShopView + RaiseCloseInteractionView

Phase B:
  CloseAllSubManager() → + RaiseCloseInventoryView

Phase F:
  Update() → Q key: if (!isInShop && !isInPause && !isInContracts) RaiseOpenQuestTreeView
  CloseAllSubManager() → + RaiseCloseQuestTreeView

Phase H:
  Update() → Tab key: if (!isInShop && !isInPause && !isInContracts) RaiseOpenInventoryView
  Update() → ESC: if (isAnyMenuOpen) CloseAllSubManager() else RaiseOpenPauseMenu
  cursor lock/unlock based on isAnyMenuOpen

Phase I:
  CloseAllSubManager() → + RaiseCloseContractsView + RaiseClosePauseMenu
```

**Rules:**
- UIManager NEVER calls SetActive on SubManagers — fires Close events, SubManagers disable themselves
- Opening from world (terminal) = GameEvents (decoupled)
- Opening from keyboard (Q/Tab) = UIManager routes with priority (centralized)
- Closing = UIManager fires all Close events via CloseAllSubManager() (centralized)
- Each phase adds 1-2 lines to CloseAllSubManager() and optionally a key check in Update()

### What's Been Proven (Phase A + B)
- ✅ DataService tested via plain C# instance (`DEBUG_Check`, `DEBUG_CheckB`)
- ✅ Orchestrator pattern (shop UI + inventory)
- ✅ SubManager self-init pattern (ShopUI, InventoryUI, BgUI)
- ✅ `partial` GameEvents across phase folders
- ✅ Field_ display-only components
- ✅ Vertical slice tests per system (7 tests across 2 phases)
- ✅ Player split from 888-line god-object into 4 focused scripts
- ✅ Tool inheritance chain (BasePhysicsObject → BaseSellableItem → BaseHeldTool → concrete tools)

### What's NOT Been Tested Yet
- ❌ `3-MonoBehaviours/` with domain subfolders (expect ~80+ files by Phase E)
- ❌ Cross-DataService communication (Phase F: quests unlock shop items)
- ❌ Scene switching + `OnDestroy` cleanup (Phase I)
- ❌ Save/load serialization of DataService state (Phase G)
- ❌ `SettingsManager` singleton reads replacing hardcoded values (Phase H)