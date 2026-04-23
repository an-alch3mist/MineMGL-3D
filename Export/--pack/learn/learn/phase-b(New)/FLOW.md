# Phase B — Data Flow

> How data moves between scripts. Every connection is either a **GameEvent** or a `[SerializeField]`. Zero direct cross-system calls.
> Format: **conversation-style plain English** with `code references` and **bold** for key moments.

---

## System Map

```
┌─────────────────────────────────────────────────────────────┐
│                        PLAYER SYSTEM                         │
│                                                             │
│  PlayerMovement ◄──── [same GO] ────► PlayerCamera          │
│       │                                    │                │
│       │ [SerializeField]                   │ [SerializeField]│
│       ▼                                    ▼                │
│  PlayerGrab ◄─── OnToolEquipped ─── (sets tool.Owner)       │
│       │                                                     │
│  FresnelHighlighter (raycast → outline on hover)            │
│  PlayerFootsteps (reads movement speed)                     │
│  RigidbodyDraggerController (OnJointBreak → ForceRelease)   │
└─────────────────────────────────────────────────────────────┘
         │                              ▲
         │ OnMenuStateChanged           │ OnMenuStateChanged
         ▼                              │
┌─────────────────────────────────────────────────────────────┐
│                       UI SYSTEM                              │
│                                                             │
│  UIManager (Singleton)                                       │
│    ├── isAnyMenuOpen (read by anyone)                       │
│    ├── CloseAllSubManager() → RaiseCloseAllSubManagers      │
│    └── Update: Tab → RaiseOpenInventoryView                 │
│                                                             │
│  InventoryUI (SubManager)                                    │
│    ├── OnOpenInventoryView → SetActive(true)                │
│    ├── OnCloseAllSubManagers → SetActive(false)             │
│    ├── OnEnable → RaiseMenuStateChanged(true)               │
│    └── OnDisable → RaiseMenuStateChanged(false)             │
└─────────────────────────────────────────────────────────────┘
         │
         │ Init(_orchestrator, dataService)
         ▼
┌─────────────────────────────────────────────────────────────┐
│                    INVENTORY SYSTEM                           │
│                                                             │
│  InventoryOrchestrator                                       │
│    ├── Owns: InventoryDataService (pure C#)                 │
│    ├── Creates: Field_InventorySlot × 40                    │
│    ├── Attaches: UIEventRelay × 40 (drag-drop)              │
│    ├── Subscribes: OnToolPickupRequested → HandleToolPickup │
│    ├── Fires: RaiseToolEquipped, RaiseToolSwitched          │
│    ├── Fires: RaiseItemPickedUp, RaiseItemDropped           │
│    └── Update: 1-0 keys, scroll, tool actions, G drop      │
│                                                             │
│  InventoryDataService (plain C# — no Unity dependency)      │
│    ├── 40 Slots: [0-9] hotbar, [10-39] extended             │
│    ├── TryAdd / Remove / SwitchTo / Scroll / Swap           │
│    └── GetSnapshot() for test logging                       │
│                                                             │
│  Field_InventorySlot (display only)                          │
│    └── SetData / SetEmpty / SetHighlighted / SetHovered     │
│                                                             │
│  UIEventRelay (input relay only)                             │
│    └── onBeginDrag / onDrag / onEndDrag / onDrop / onPointerDown │
└─────────────────────────────────────────────────────────────┘
         │                              ▲
         │ RaiseToolPickupRequested     │ tool.Interact("Take")
         ▼                              │
┌─────────────────────────────────────────────────────────────┐
│                      TOOL SYSTEM                             │
│                                                             │
│  BaseHeldTool (base class)                                   │
│    ├── Owner: PlayerMovement (set by PlayerGrab via event)  │
│    ├── Interact("Take") → RaiseToolPickupRequested          │
│    ├── DropItem() → unparent, show WorldModel, velocity     │
│    ├── OnEnable → parent to Owner.ViewModelContainer        │
│    └── Equip() / UnEquip() (virtual)                        │
│                                                             │
│  ToolPickaxe    → PrimaryFireHeld: swing + delayed raycast  │
│  ToolMagnet     → SecondaryFireHeld: pull nearby objects    │
│  ToolHammer     → PrimaryFire: raycast for buildings (D)    │
│  ToolMiningHat  → PrimaryFire: toggle light                 │
│  ToolBuilder    → PrimaryFire: place building (D)           │
│  ToolSupportsWrench → PrimaryFire: toggle supports (D)     │
│  ToolResourceScanner → Update: show name of looked-at object│
│  ToolHardHat    → extends ToolPickaxe (empty)               │
│                                                             │
│  Inheritance: BasePhysicsObject → BaseSellableItem          │
│               → BaseHeldTool → concrete tools               │
└─────────────────────────────────────────────────────────────┘
```

---

## Flow 1 — Tool Pickup

The player **presses E** near a `ToolPickaxe` on the ground. `InteractionSystem` raycasts from the camera and **hits the pickaxe's collider**. It fires `RaiseOpenInteractionView`, which tells `InteractionWheelUI` to spawn a **"Take" button**.

The player **clicks "Take"**. The WheelUI's `onClick` listener calls `BaseHeldTool.Interact("Take")` on the pickaxe. The pickaxe *doesn't know anything about inventory* — it just fires `GameEvents.RaiseToolPickupRequested(this)`.

`InventoryOrchestrator` is subscribed. It receives the tool and asks `dataService.TryAdd(tool)`, which **finds the first empty slot** (index 0) and stores the tool reference there.

The orchestrator calls `tool.gameObject.SetActive(false)` — the **pickaxe disappears from the ground** (WorldModel hidden). Then, because `EquipWhenPickedUp` is true, it calls `SwitchToSlot(0)`.

Inside `SwitchToSlot`, the orchestrator calls `tool.gameObject.SetActive(true)`. This triggers `BaseHeldTool.OnEnable()`, which checks `Owner != null` — *but Owner isn't set yet*, so it defaults to showing WorldModel. However, immediately after, the orchestrator fires `GameEvents.RaiseToolEquipped(tool)`.

`PlayerGrab` is subscribed to `OnToolEquipped`. It receives the tool and sets `tool.Owner = _movement` — giving the tool access to `Owner.PlayerCam` for raycasts and `Owner.ViewModelContainer` for parenting. Now the tool **parents itself to ViewModelContainer** and shows the **ViewModel** — the pickaxe appears as a *first-person held tool* in front of the camera.

The orchestrator also fires `GameEvents.RaiseToolSwitched(0)` (for `FresnelHighlighter` and test logging) and `GameEvents.RaiseItemPickedUp(tool)` (for test logging).

Finally, `RefreshAllSlots()` loops through all 40 `Field_InventorySlot` displays. **Slot 0** now calls `SetData(pickaxeIcon, "Pickaxe", qty=1)` and `SetHighlighted(true)`. Slots 1–39 call `SetEmpty()`.

---

## Flow 2 — Tool Switch (press 2)

The player **presses 2** while Pickaxe is in slot 0 and Magnet is in slot 1.

`InventoryOrchestrator.Update()` detects `KeyCode.Alpha2` and calls `SwitchToSlot(1)`.

First, the **previous tool** (Pickaxe) gets `SetActive(false)` — its ViewModel *disappears* from the camera. Then `dataService.SwitchTo(1)` updates `activeSlotIndex` to 1.

The **new tool** (Magnet) gets `SetActive(true)`. `BaseHeldTool.OnEnable()` fires — it parents to `ViewModelContainer` and shows the Magnet's ViewModel. The orchestrator fires `GameEvents.RaiseToolEquipped(magnet)` → `PlayerGrab` sets `magnet.Owner = _movement`.

`GameEvents.RaiseToolSwitched(1)` fires for external listeners.

`RefreshAllSlots()` updates the display — **slot 0** loses its highlight, **slot 1** gets highlighted. Both still show their tool icons.

---

## Flow 3 — Tool Drop (press G)

The player **presses G** while Magnet is equipped.

`InventoryOrchestrator.Update()` detects the key and calls `HandleDropActiveTool()`.

It gets the active tool from `dataService.ActiveTool` (Magnet) and calls `tool.DropItem()`. Inside `DropItem`:
- The tool calls `SetActive(true)` (it was already active, but ensures it)
- `HideWorldModel(false)` — **WorldModel becomes visible** (the magnet mesh on the ground)
- `HideViewModel()` — **ViewModel disappears** from camera
- It gets the `Rigidbody`, *unparents* the tool from ViewModelContainer, positions it in front of the camera, and sets `rb.linearVelocity = cam.forward * 5f` — the magnet **flies forward**
- `Owner = null` — the tool is no longer linked to the player

Back in the orchestrator, `dataService.Remove(tool)` nulls slot 1. It fires `GameEvents.RaiseItemDropped(tool)` for test logging. `RefreshAllSlots()` updates the display — **slot 1 becomes empty**.

---

## Flow 4 — Open/Close Inventory

### Open (Tab)

The player **presses Tab**. `UIManager.Update()` checks `!isAnyMenuOpen` — it's false (no menu open), so it fires `GameEvents.RaiseOpenInventoryView()`.

`InventoryUI` is subscribed — it calls `this.gameObject.SetActive(true)`. This triggers `OnEnable`. Since `isFirstEnable` is already false (setup happened on scene load), it goes straight to firing `GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: true)`.

`UIManager` receives this and sets `isAnyMenuOpen = true`. `PlayerMovement` receives this and **unlocks the cursor** + *disables WASD/look input*. The extended inventory panel is now visible with 30 slots.

### Close (ESC or Tab while open)

The player **presses ESC** (or Tab again). `InventoryUI.Update()` detects the key and fires `GameEvents.RaiseCloseInventoryView()`.

`InventoryUI` is subscribed to its own close event — it calls `this.gameObject.SetActive(false)`. `OnDisable` fires, which fires `GameEvents.RaiseMenuStateChanged(isAnyMenuOpen: false)`.

`UIManager` sets `isAnyMenuOpen = false`. `PlayerMovement` **re-locks the cursor** and *re-enables input*. The extended panel disappears.

### Close All (ESC from any menu)

If ESC is pressed while `isAnyMenuOpen` is true, `UIManager` calls `CloseAllSubManager()`, which fires `GameEvents.RaiseCloseAllSubManagers()`. *Every* SubManager — `InventoryUI`, `ShopUI`, `InteractionWheelUI` — is subscribed and calls `SetActive(false)` on itself. All panels close at once.

---

## Flow 5 — Drag-Drop (Hotbar ↔ Extended)

The player **opens the extended inventory** (Tab) and **clicks-and-drags slot 0** (Pickaxe).

`UIEventRelay` on slot 0 fires `OnBeginDrag`. The orchestrator's `HandleBeginDrag` receives it, records `dragFromIndex = 0`, calls `FIELD_SLOT[0].SetDragVisible(false)` — the slot *looks empty* (its content is hidden). The **DragGhostIcon activates** with the pickaxe sprite and moves to the cursor position.

As the player **moves the mouse**, `UIEventRelay.OnDrag` fires every frame. The orchestrator's `HandleDrag` updates `DragGhostIcon.transform.position = e.position` — the ghost **follows the cursor**.

The player **drops on slot 15** (an extended slot). `UIEventRelay[15].OnDrop` fires. The orchestrator's `HandleDrop` calls `dataService.Swap(0, 15)` — the Pickaxe moves from slot 0 to slot 15 in data. `RefreshAllSlots()` updates all Field_ displays.

Then `UIEventRelay[0].OnEndDrag` fires (drag source cleanup). The orchestrator's `HandleEndDrag` calls `FIELD_SLOT[0].SetDragVisible(true)` — restoring the slot visual. The **DragGhostIcon deactivates**.

**Result:** Pickaxe is now in extended slot 15, hotbar slot 0 is empty.

### Drop Outside UI

If the player releases the mouse **outside any slot** (`e.pointerEnter == null`), the orchestrator calls `tool.DropItem()` — the tool **appears in the world** with forward velocity — and `dataService.Remove(tool)` removes it from inventory.

---

## Flow 6 — Selected Item Info

The player **clicks slot 1** (Magnet) while the extended inventory is open.

`UIEventRelay[1].OnPointerDown` fires. The orchestrator calls `UpdateSelectedItemInfo(slot[1].Tool)`. The **SelectedItemInfoPanel activates** — showing the magnet's *name*, *description*, *icon*, and *amount*. The equip button text says **"Equip"** (or "Build" if it's a `ToolBuilder`).

If the player **clicks "Equip"**: the orchestrator calls `EquipSelectedTool()`, which calls `SwitchToSlot(1)` (equipping the Magnet) and fires `GameEvents.RaiseCloseInventoryView()` — the **inventory closes** and the Magnet ViewModel appears in camera.

If the player **clicks "Drop"**: the orchestrator calls `DropSelectedTool()`, which calls `tool.DropItem()` (WorldModel appears in world), `dataService.Remove(tool)` (slot emptied), fires `RaiseItemDropped`, and calls `UpdateSelectedItemInfo(null)` — the **info panel hides**.

---

## Flow 7 — Grab Physics Object

The player **right-clicks** while looking at a Grabbable cube.

`PlayerGrab.Update()` detects `Mouse1` and calls `TryGrab()`. It checks: is a menu open? (*no*) Is something already held? (*no*) It raycasts from `_cam` and **hits the cube** — `HasTag(TagType.Grabbable)` returns true, so it proceeds to `GrabObject(hit)`.

Inside `GrabObject`: the **RigidbodyDragger GO activates**. A `SpringJoint` is added to the dragger and connected to the cube's `Rigidbody`. `IgnoreAllCollisions` prevents the cube from colliding with the player. The cube's `linearDamping` increases (it feels *heavy and dampened*). `Interpolation` is set to `Interpolate` for smooth visual. The **LineRenderer rope enables** with 2 positions.

**Every frame** while holding, `Update` sets `_rope.SetPosition(0, dragger)` and `_rope.SetPosition(1, cubeAnchor)` — the rope **visually connects** hand to cube and follows both.

The player **right-clicks again**. `TryGrab` sees `heldObject != null` and calls `Release()`. The SpringJoint is *destroyed*, the dragger **deactivates**, the cube's drag is *restored*, collisions re-enabled, and the rope **disappears**. A coroutine waits 3 seconds then disables interpolation on the cube (so it's not wasting CPU when idle).

If the player **pulls too far**, the SpringJoint exceeds `breakForce = 120` and *snaps*. `RigidbodyDraggerController.OnJointBreak()` fires and calls `PlayerGrab.ForceRelease()` — same cleanup as a normal release.

---

## Flow 8 — Magnet Pull/Launch

The player **equips the Magnet** and **holds right-click**.

`InventoryOrchestrator.Update()` detects `Input.GetMouseButton(1)` and calls `activeTool.SecondaryFireHeld()`. `ToolMagnet` receives this and sets `wantsToMagnet = true`.

In `ToolMagnet.FixedUpdate()` (every physics frame): the `_pullOrigin` position smoothly follows `Owner.MagnetToolPosition` via `SmoothDamp`. It cleans up any *broken joints* and any *recently dropped bodies* (disabling interpolation after 3 seconds). Then, since `wantsToMagnet` is true, it calls `GrabNearbyObjects()`.

`GrabNearbyObjects` does `Physics.OverlapSphere` at `_pullOrigin`. For each Grabbable `Rigidbody` found, it creates a **MagnetAnchor GO** with a kinematic `Rigidbody` + `SpringJoint`, connects it to the target body, increases drag, enables interpolation, and ignores collisions with the player. The body **flies toward the pull origin** — cubes jitter and bounce near the magnet (spring physics). `wantsToMagnet` resets to false until the next frame.

The player **clicks left-click** → `PrimaryFire` → `DropObjects(_pushForce)`. All joints are *destroyed*, all held bodies get `AddForce(cam.forward * pushForce)` — cubes **launch forward**. Drag is restored, collisions re-enabled.

The player **presses R** → `Reload` → `DropObjects(_dropForce)`. Same as above but with *gentle force* — cubes **drop softly**.

The player **presses Q** → `QButtonPressed` → `CycleSelectionMode()`. The mode cycles through `Everything` → `ResourcesNotInFilter` → `ResourcesNotOnConveyors`. The `_selectionModeText` TMP on the ViewModel **updates** to show the current mode.

---

## Event Registry — Phase B

| Event | Fired By | Subscribed By |
|-------|----------|---------------|
| `OnMenuStateChanged(bool)` | InventoryUI, ShopUI, InteractionWheelUI | UIManager, PlayerMovement, PlayerCamera, PlayerGrab |
| `OnCloseAllSubManagers` | UIManager.CloseAllSubManager() | Every SubManager (self-close) |
| `OnOpenInventoryView` | UIManager (Tab key), InventoryTest | InventoryUI (SetActive true) |
| `OnCloseInventoryView` | InventoryUI (ESC/Tab), InventoryOrchestrator (equip) | InventoryUI (SetActive false) |
| `OnToolPickupRequested(tool)` | BaseHeldTool.Interact("Take") | InventoryOrchestrator.HandleToolPickup |
| `OnToolEquipped(tool)` | InventoryOrchestrator.SwitchToSlot | PlayerGrab (sets tool.Owner) |
| `OnToolSwitched(index)` | InventoryOrchestrator.SwitchToSlot | FresnelHighlighter (future), tests |
| `OnItemPickedUp(tool)` | InventoryOrchestrator.HandleToolPickup | Tests (logging) |
| `OnItemDropped(tool)` | InventoryOrchestrator.HandleDropActiveTool | Tests (logging) |
| `OnMoneyChanged(float)` | EconomyManager.AddMoney | InventoryOrchestrator (RefreshAllSlots) |
| `OnCamViewPunch(vec3, float)` | StartingElevator, future explosions | PlayerCamera (view punch decay) |