# New Agent — Start Here

> **Current target: phase-b Player Controller + Inventory + Tools + Grabbing**
> Update this line when moving to the next phase.

---

## First Prompt (copy-paste into new conversation)

```
Read these files in this order:
1. learn/GOAL.md — all architecture rules, folder structure, naming conventions, minimal API, decoupling, vertical slice tests
2. learn/PhaseMap.md — phase-b section (files, modifications, vertical slice tests)
3. learn/StructureMap.md — DataService specs per phase (exact collections, methods, nested types)
4. learn/handTyped(latest)/ — my ACTUAL hand-typed code (ground truth for coding style, isFirstEnable pattern, separate Open/Close events)
5. learn/handTyped(latest)/ and its subfolders — reference for new clever architecture patterns (isFirstEnable, separate Open/Close events, SubManager, Orchestrator, DataService, Field_, #region style). Main goal remains: 100% main-source functionality with these improvements.
6. Scripts/Assembly-CSharp/ — original source for phase-b files and refer PhaseMap, StructureMap.md (or if required additional fiel to match new clever architecture/pattern sure)

Now build phase-b(New) with:
- 100% main source functionality
- New architecture from GOAL.md (numbered folders, DataService/Orchestrator/Field_/SubManager pattern)
- Least possible public API exposure
- Zero FindObjectOfType — use GameEvents + [SerializeField] + Owner chain
- partial GameEvents in phase-b/0-Core/GameEvents.cs (no modifying earlier phase files)
- Vertical slice tests per system with full GUIDE.md (prerequisites, scene setup, checklist)
- One-liner purpose per script — if it doesn't fit one sentence, split it
```

---

## What The Agent Must Deliver

**MANDATORY: All documentation files (`GUIDE.md`, `FLOW.md`, `5-Tests/*.cs`, `5-Tests/Manual/*.md`) MUST be written at beginner level — assume the reader has almost zero knowledge of both this codebase AND Unity scene setup.** No shortcuts like "see GUIDE.md" in manual tests. Each file is self-contained. Every GO, component, field, wiring, and inspector value must be explicit. See GOAL.md "MANDATORY: Beginner-Level Documentation" section for the full spec.

For each phase, the agent must produce:

1. **GUIDE.md** — **beginner-friendly**, same conversational voice as FLOW.md. Written so someone who has never seen this codebase can follow it. With ALL of these:
   - What it looks like when running (conversational, describe the player experience)
   - Folder structure (numbered: 0-Core, 1-Managers, 2-Data, 3-MonoBehaviours, 4-Utils, 5-Tests)
   - Script Purpose — one sentence per script
   - Hand-typing order (compile groups with stop-and-test points)
   - Vertical Slice Tests — beginner-friendly step-by-step for each `.cs` test:
     - Conversational intro: what this test proves (1-2 sentences)
     - "What you need to type first" / "What you DON'T need"
     - Step-by-step scene setup (numbered: create GO, add component, wire fields with `| Field | Drag From |` tables)
     - "How to test" table: `| Key | What it does | What you should see |`
     - "Full test flow" for complex tests (ordered steps: do X → expect Y → do Z)
     - Checklist: pass/fail items
     - Reference: see `phase-b(New)/GUIDE.md` tests for the gold standard format
   - Scene Setup (full — GOs, components, wiring checklist)
   - Modifications to Earlier Phases (table: File | How | Change | Why)
   - Source vs Phase diff (what original did vs what we changed)
   - Systems & Testability (at end): Individual Systems table (name, scripts, decoupling events) + Testability Matrix (which test covers what, dependencies). Final count line.

1b. **FLOW.md** with:
   - **System Map** — ASCII box diagram: all systems, what each owns, connections via GameEvents/SerializeField
   - **Data Flows** — one per major user action. Written in **conversation-style plain English** with `code refs`, **bold** for key moments, *italics* for context. NOT swim lanes or ASCII tables — readable prose.
   - **Event Registry** — table: every GameEvent in that phase, who fires it, who subscribes
   - Every connection = GameEvent or [SerializeField]. Direct cross-system calls = tight coupling = refactor.
   - Reference: `phase-b(New)/FLOW.md`

2. **All scripts** following GOAL.md rules:
   - `#region` blocks for sections
   - **Beginner-friendly summaries on everything** (reference-only — user reads while typing, does NOT type these):
     - **Class**: conversation-style first person "I" — what I do, how I work, who uses me, events I fire/subscribe
     - **Public method**: 2-line English explaining the full effect (what happens inside, not just the name)
     - **Private method**: same — describe what actually happens, not just the method name being called
     - **Unity lifecycle** (Start, Update, Awake, OnEnable, OnDisable, OnCollisionEnter, OnTriggerEnter, OnDestroy, FixedUpdate): 2-line explaining what THIS script does in this hook — don't just say "called by Unity"
     - **Interface**: who implements it, who calls it, what it enforces
     - **Enum**: what each value means in context (which system uses it, what changes per value)
   - **`// →` inline flow markers** inside every method body — one per logical step, describes what happens at that point (if a method call does 5 things, list those 5 things)
   - `// purpose:` on every `.Raise...()` and `+=` subscription
   - Zero `FindObjectOfType` — use GameEvents, `[SerializeField]`, Owner chain
   - Minimal public API — private by default
   - `partial class GameEvents` in own `0-Core/GameEvents.cs`
   - Protected helpers for repeated patterns (`OwnerCamRay`, etc.)
   - **Actively create DataServices** for any pure C# logic (collections, lookups, formatting, validation). Ask: "Can I test this via `new`?" If yes → DataService. See GOAL.md "Common candidates" list. Don't leave testable logic buried in Managers/MonoBehaviours.
   - **Actively extract reusable logic into UtilsPhaseX.** If a method appears in 2+ scripts, or is a pure static helper (no `this`), it goes in `4-Utils/UtilsPhaseX.cs`. If logic was moved off an SO_ (pure data rule), put reusable parts in Utils, not in a single consumer.
   - **Reduce verbosity** — actively use `?.` (null-conditional), `??` (null-coalescing), `=>` (expression-bodied), `.sum()`, `.GetOrCreate()`, LINQ extensions. See GOAL.md "C# Features" for full list. Keep guard clauses (`if (x == null) return;`) when the block does multiple things.

3. **Test scripts for Vertical Slice Test of Each System in that phase** with:
   - Summary comment: prerequisites, NOT required, "How to test" steps, controls
   - `// purpose:` on every Raise/Subscribe
   - Console logging via GameEvents subscription
   - Minimal bootstrap — systems handle their own Update()
   - Fire GameEvents to trigger actions (never call methods directly)

4. **Manual Test Guides (`5-Tests/Manual/*.md`)** — **comprehensive, beginner-level, teaches the internal flow** for any system needing visual/hands-on verification (UI, animations, physics, effects, audio):
   - **Prerequisites** — exactly which singletons, prefabs, test scripts needed
   - **Setup Guide** — beginner-level step-by-step Unity Editor instructions:
     - Every GO: name, parent, components, `[SerializeField]` wiring (`| Field | Drag From |` table)
     - Prefab hierarchies: every child with RectTransform, Image, tag, layer, default values
     - Final hierarchy tree showing parent-child layout
   - **How It Works (System Flow)** — the heart of the manual test. Before DO/EXPECT steps, explain the system's **end-to-end data flow** in conversation-style plain English. Break into labelled paragraphs per action (e.g. "Scene loads:", "Tool pickup:", "Drag-drop:"). Each paragraph traces:
     - Which script method → which GameEvent → which subscriber → which GO `SetActive` → which field changes → what player sees
     - Example: *"**Drag-drop:** `UIEventRelay.OnBeginDrag` fires → orchestrator stores `dragFromIndex`, calls `SetDragVisible(false)` (**HideWhenDragged child GO** deactivates) → **DragGhostIcon activates** with sprite. On drop: `dataService.Swap(from, to)` → `RefreshAllSlots()`. OnEndDrag restores slot + **deactivates ghost**."*
     - Example: *"**Breaking:** `BreakNode` → `WeightedRandom` selects prefab → `SpawnPooledOre` **dequeues or Instantiates** → random velocity → **pieces fly out**. `ParticleManager.CreateParticle` → burst. `RaiseOreMined` fires. `Destroy(gameObject)`."*
     - Teaches WHY things happen, not just what the reader sees. **Bold** = GO state changes, `code` = methods, *italics* = context
   - **Manual Test Flow** — numbered DO/EXPECT steps:
     - One action per step (press key, wait Xs, drag here, click there)
     - EXPECT: **bold** for visual changes, `code` for console messages
     - **Behind the scenes per step:** which method runs, which event fires, which GOs SetActive, which component fields change, which Unity callbacks trigger
     - Cover: initial state → primary actions → edge cases → error conditions
   - **Summary Checklist** — pass/fail items
   - **Must be comprehensive enough for a beginner AND teach the architecture** — the reader should understand the full data path by reading the manual test. Not just "click → see result" but "click → this script → this event → this subscriber → this GO activates → you see this."
   - One `.md` per system. Self-contained — no "see GUIDE.md" shortcuts. The `.md` IS the test.
   - **Always analyse main source** for each phase: "Does this system have UI, animations, physics visuals, or dense inspector setup?" If yes → `Manual/*.md`.
   - Gold standard: `phase-b(New)/Scripts/5-Tests/Manual/InventoryUITest.md`
   - Full list: PhaseMap.md + StructureMap.md `All Manual Tests` table.

---

## Reference Files

| File | What it is | Read when |
|------|-----------|-----------|
| `learn/GOAL.md` | Architecture bible — all rules | Always first |
| `learn/PhaseMap.md` | Roadmap — all phases, files, modifications | Before building any phase |
| `learn/StructureMap.md` | DataService specs — exact collections, methods, nested types per phase | Before writing any DataService |
| `learn/handTyped(latest)/` | User's ACTUAL hand-typed code — ground truth for style, `isFirstEnable`, Open/Close events | Always — match this, not generic C# |
| `learn/Estimate.md` | Timeline + hours | For planning |
| `learn/ARCHITECTURE.md` | Original source analysis | When source fidelity questions arise |
| `learn/surfer.md` | Reasoning history | Optional — decisions are in GOAL.md |
| `learn/phase-b(New)/` | Player/inventory reference | For tool inheritance, partial GameEvents, decoupling |
| `learn/phase-All/6-Shaders/ShaderGuide.md` | ALL shaders, materials, URP settings | When creating any material or Shader Graph — centralized, beginner-friendly |
| `Scripts/Assembly-CSharp/` | Original source (~200 files) | To match 100% behavior |

---

## Key Rules (Quick Reference)

- **Check Gap Audit before building any phase.** Both `PhaseMap.md` and `StructureMap.md` have a "Gap Audit" section at the end listing missing files/features per phase with priority ratings. Include all **Critical** and **Important** items. **Polish** items go in `#region extra` blocks.
- **PhaseMap/StructureMap are NOT exhaustive.** Always cross-reference every original source file (`Scripts/Assembly-CSharp/`) for the phase. If the source has functionality not listed in PhaseMap or StructureMap, **include it anyway** — create new files, add `#region extra` blocks, or document as a Gap Audit addition. The main source is the ultimate source of truth for 100% fidelity.
- **MANDATORY: Post-delivery self-audit.** After producing all files for a phase, do a method-by-method comparison: read every original source file line-by-line, check every public method, every field, every interface implementation against what you produced. List any gaps. Fix them before delivering. This is non-negotiable — previous phases missed 5-15% on first delivery because the agent skipped small methods, SO_ logic wasn't moved, or singletons were in wrong folders.
- **One sentence per script.** If it doesn't fit, split it.
- **GlobalEnumsX.cs** — all enums for a phase go in ONE file: `2-Data/Enums/GlobalEnumsX.cs` (e.g. `GlobalEnumsB.cs`). No separate file per enum. Use `InteractionType` enum instead of magic strings for interaction names.
- **TagType enum — no raw string tags.** Never use `CompareTag("string")` or `tag = "string"`. Use `collider.HasTag(TagType.grabbable)` and `gameObject.SetTag(TagType.markedForDestruction)` — extensions in `phase-All/4-Utils/UtilsPhaseAll.cs` (auto-available, no class name needed). `TagType` lives in `phase-All/2-Data/Enums/GlobalEnumsAll.cs`.
- **Enum values use camelCase.** All enum values across all phases: `TagType.grabbable`, `PieceType.ore`, `OreLimitState.slightlyLimited`, `MagnetToolSelectionMode.everything`. NOT PascalCase. See GOAL.md Naming table.
- **`phase-All/`** — scripts shared across ALL phases live here (Singleton, UIManager, EconomyManager, core GameEvents). Never duplicated into phase folders. UIManager grows 1-2 lines per phase (keyboard routing). See GOAL.md folder structure.
- **Prefix = no logic.** `SO_` = pure data. `Field_` = display only. `W` = session wrapper.
- **GameEvents for cross-system.** `[SerializeField]` for same-GO. Owner chain for parent-child.
- **DataService = pure C#.** If it needs Unity physics/lifecycle, keep in MonoBehaviour.
- **partial GameEvents.** Each phase extends in its own `0-Core/GameEvents.cs`.
- **SubManagers self-init.** Start() builds + subscribes + disables self. OnEnable/OnDisable announce menu state.
- **No defensive null checks.** Let it crash — the crash is traceable.
- **Least possible public API.** Default private. Promote only when another script calls it. **After writing every script, audit:** can any public method be made private/protected? If nobody outside the class calls it → make it private. If only subclasses → protected. The user checks this on every review.
- **`[SerializeField]` is ALWAYS private** — never `[SerializeField] public`. External access via `Get...()` / `Set...()` methods only for fields that external scripts actually need. Audit who reads what before exposing. Example: `[SerializeField] Material _ghost; public Material GetGhostMaterial() => _ghost;`
- **No C# property accessors** — never use `{ get; set; }` or `{ get => ...; set => ... }`. Always explicit `Get...()` / `Set...()` methods. Exceptions: `[Serializable]` entities (public fields OK), `SO_` (pure data), interface contracts (`{ get; set; }` required by C#), static collections (`{ get; private set; }`).
- **`// purpose:`** on every Raise call and every += subscription.
- **Vertical slice = standalone scene.** If a test needs another system, fix the architecture.

---

## Common Mistakes The User WILL Push Back On

The user is strict. Catch these before delivering:

1. **Any `FindObjectOfType` in MonoBehaviours** → user will ask you to remove it immediately. Use `[SerializeField]`, Owner chain, or GameEvents instead.
2. **Public methods nobody calls externally** → user will ask "who calls this?" and make you reduce to private/protected. Audit every public method.
3. **Missing `// purpose:` on Raise/Subscribe calls** → user will ask you to add them.
4. **Tight coupling between systems** (Script A directly calls Script B across systems) → user will ask you to decouple via GameEvents.
5. **Defensive null checks on inspector refs** (`if (x != null)`) → user says "let it crash, crash is traceable."
6. **RefreshAll() in Update()** (polling) → user will ask you to make it event-driven.
7. **DataService that needs Unity physics/lifecycle** → user will question if it should be a DataService at all.

8. **Missing `isFirstEnable` pattern on SubManagers** → every SubManager MUST use `isFirstEnable` in OnEnable. NOT Start(). Read GOAL.md "Why isFirstEnable" section. No exceptions.
9. **Toggle instead of separate Open/Close events** → every UI panel must have SEPARATE `OnOpen...View` + `OnClose...View` events. NOT a single toggle. This is how the user's handTyped code works.
10. **Using `Input.GetKeyDown` directly in SubManagers** → SubManagers don't handle input. They subscribe to Open/Close events. Input routing lives elsewhere (UIManager or test scripts fire the events).
11. **Methods on SO_ classes** → SO_ = pure data, zero methods. If the original source has helper methods on an SO (e.g. `GetOrePrefab()`), move them to the consumer (MonoBehaviour, DataService). SO exposes public fields, consumer reads them.
12. **Singleton in `3-MonoBehaviours/` instead of `1-Managers/`** → if a script extends `Singleton<T>`, it goes in `1-Managers/`. Always. PhaseMap may list it wrong — GOAL.md is the authority.

---

## MANDATORY Patterns — Do NOT Skip

### `// purpose:` on EVERY Raise and Subscribe
**After writing each script, ctrl+F for `GameEvents.Raise` and `GameEvents.On`. Every single one MUST have a `// purpose:` comment on the line above.** No exceptions. The user checks this on every audit.

```csharp
// purpose: cursor lock/unlock for player controller
GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);

// purpose: InventoryOrchestrator adds this tool to hotbar
GameEvents.RaiseToolPickupRequested(this);

// purpose: log when ore is sold
GameEvents.OnOreSold += (price) => Debug.Log($"sold for {price}");
```

### `isFirstEnable` on ALL SubManagers
Every SubManager (ShopUI, InventoryUI, InteractionWheelUI, BgUI, etc.) MUST use this pattern:

```csharp
bool isFirstEnable = true;
private void OnEnable()
{
    if (isFirstEnable)
    {
        // subscribe + build + self-disable
        GameEvents.OnOpenThisView += () => this.gameObject.SetActive(true);
        GameEvents.OnCloseThisView += () => this.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
        isFirstEnable = false;
        return;
    }
    GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true);
}
```

Do NOT use Start() for subscriptions. Do NOT use Awake(). Read GOAL.md "Why isFirstEnable" for the reasoning.

### Separate Open/Close Events Per UI Panel
Every UI panel gets TWO events, not a toggle:

```
OnOpenShopView / OnCloseShopView
OnOpenInteractionView / OnCloseInteractionView
OnOpenInventoryView / OnCloseInventoryView
```

SubManagers subscribe to both. `UIManager.CloseAllSubManager()` fires all Close events.

---

## After First Delivery

The user will audit your scripts. Expect:
- "Is the public API minimal?"
- "Any tight coupling?"
- "Does every Raise have a // purpose: comment?"
- "Can this be tested independently in a standalone scene?"

Fix what the user flags. You'll calibrate to their strictness level after 1-2 corrections.

---

## Don't Forget

- **SPACE_UTIL extensions exist.** Read GOAL.md "The User's Coding Style" section for `.map()`, `.gc<>()`, `.destroyLeaves()`, `.toggle()`, `.colorTag()`, `INPUT.K.InstantDown()`, `LOG.AddLog()`, `C.method()` etc. Use them, don't reinvent.
- **Append surfer.md** after completing the phase with critical decisions made.
- **Read `learn/handTyped(latest)/....`** to see the user's ACTUAL coding style — match it.