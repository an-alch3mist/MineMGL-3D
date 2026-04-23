# Phase D — Data Flow

> Every connection is a **GameEvent** or `[SerializeField]`. Zero direct cross-system calls.
> Format: **conversation-style plain English**.

---

## System Map

```
┌─────────────────────────────────────────────────────────────┐
│                   BUILDING PLACEMENT SYSTEM                   │
│                                                             │
│  ToolBuilder (Update: ghost follows cursor)                  │
│    ├── Reads: BuildingManager.DataService.GetClosestGridPos │
│    ├── Calls: BuildingManager.UpdateGhostObject (ghost mat) │
│    ├── Calls: BuildingManager.CanPlaceObject (validation)   │
│    ├── PrimaryFire: Instantiate real building               │
│    └── Fires: RaiseBuildingPlaced, RaiseItemDropped         │
│                                                             │
│  BuildingManager (Singleton)                                 │
│    ├── Owns: BuildingDataService (snap math)                │
│    ├── Ghost: Instantiate, material swap, layer set         │
│    └── CanPlaceObject: overlap, flat, node, snap            │
│                                                             │
│  BuildingPlacementNode (self-registers in static list)       │
│    └── ShowGhost: indicator at AutoMiner spots              │
└─────────────────────────────────────────────────────────────┘
         │ RaiseBuildingPlaced              │ RaiseBuildingRemoved
         ▼                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                   BUILDING LIFECYCLE                          │
│                                                             │
│  BuildingObject (IInteractable)                              │
│    ├── Take → RaiseBuildingTakeRequested → Orchestrator     │
│    ├── Pack → Instantiate BuildingCrate → destroy self      │
│    └── EnableBuildingSupports → RespawnSupports              │
│                                                             │
│  BuildingCrate (IInteractable)                               │
│    └── Take → RaiseBuildingTakeRequested → Orchestrator     │
└─────────────────────────────────────────────────────────────┘
         │ OnTriggerEnter/Exit
         ▼
┌─────────────────────────────────────────────────────────────┐
│                   CONVEYOR SYSTEM                             │
│                                                             │
│  ConveyorBelt (base)                                         │
│    ├── OnTriggerEnter → AddPhysicsObject                    │
│    ├── FixedUpdate → AddConveyorVelocity per object         │
│    └── Static AllConveyorBelts list                         │
│                                                             │
│  ConveyorBeltManager (Singleton, FixedUpdate)                │
│    ├── Applies averaged velocity to registered objects      │
│    └── Round-robin ClearNullObjectsOnBelt in Update         │
│                                                             │
│  Variants: Shaker, ShakerHorizontal, Blocker, BlockerT2,   │
│            SplitterT2, RoutingConveyor                      │
│  Rendering: ConveyorRenderer + ConveyorBatchRenderingComponent│
└─────────────────────────────────────────────────────────────┘
```

---

## Flow 1 — Building Placement (ToolBuilder.PrimaryFire)

The player **equips ToolBuilder** from hotbar. `ToolBuilder.Update()` runs every frame — it raycasts from the camera and calls `BuildingManager.DataService.GetClosestGridPosition(hit.point)` to snap to the 1m grid. Then `BuildingManager.UpdateGhostObject(gridPos, prefab, rotation, this)` is called.

Inside `UpdateGhostObject`: if no ghost exists, `SetupGhostObject` **Instantiates** the building prefab as a ghost — sets `IsGhost = true`, destroys triggers, disables all MonoBehaviours except BuildingObject, disables audio/particles, sets layer to `"BuildingGhost"`, makes all Rigidbodies kinematic. The ghost is positioned at `gridPos + (0.5, 0, 0.5) + BuildModePlacementOffset`.

Then `CanPlaceObject` runs validation: **overlap check** (Physics.OverlapBox on ghost colliders against placement layer mask), **flat ground check** (raycast down, dot with up > 0.9), **node requirement check** (find nearest unoccupied BuildingPlacementNode), **conveyor snap** (OverlapSphere finds neighbors, `BuildingDataService.GetNearbySnapConnections` tests 4 rotations × input/output positions, `ResolveBestSnap` picks most-voted rotation).

Based on the result, all ghost renderers get their materials swapped to **green** (valid), **red** (invalid), or **yellow** (requirements not met).

When the player **left-clicks**, `ToolBuilder.PrimaryFire()` checks `CanPlaceObject == Valid`, then `Object.Instantiate(prefab, position, rotation)` creates the **real building**. `GameEvents.RaiseBuildingPlaced(building)` fires. `Quantity--` — if zero, the tool is consumed (destroyed + ghost cleaned up).

---

## Flow 2 — Conveyor Belt Physics

When ore enters a `ConveyorBelt`'s trigger collider, `OnTriggerEnter` fires. It gets the `Rigidbody`, finds or adds a `BasePhysicsObject` component, then calls `AddPhysicsObject(obj)` — adding it to `_physicsObjectsOnBelt` list and calling `obj.AddTouchingConveyorBelt(this)`.

Every **FixedUpdate**, `ConveyorBelt.FixedUpdate()` loops through all objects on the belt and calls `obj.AddConveyorVelocity(_pushVelocity, RetainYVelocity)`. This **accumulates** the velocity — if ore is on multiple belts, each belt adds its contribution.

Then `ConveyorBeltManager.FixedUpdate()` (runs after all belts, `[DefaultExecutionOrder(-10)]`) loops all registered `BasePhysicsObject` and applies the **averaged velocity**: `rb.linearVelocity = sumVelocity / count`. If `RetainY` is true, the Y component keeps the object's current vertical velocity (gravity preserved). After applying, `ResetAccum()` clears for next frame.

When ore **exits** the trigger, `OnTriggerExit` removes it from the belt's list and calls `obj.RemoveTouchingConveyorBelt(this)`.

**ConveyorBeltShaker** overrides FixedUpdate — adds sinusoidal left-right + up-down oscillation on top of forward velocity. **ConveyorBlocker** checks `HingeJoint.angle` — if below threshold, sets `Conveyor.Disabled = true` (belt stops pushing). **RoutingConveyor** rotates between two directions on Toggle.

---

## Flow 3 — Building Take/Pack

The player equips **ToolHammer** and looks at a placed `BuildingObject`. `FresnelHighlighter` outlines it in cyan. The player presses E → `InteractionWheelUI` shows "Take" and "Pack" options.

**Take:** `BuildingObject.Interact(InteractionType.Take)` → `TryTakeOrPack()` → fires `GameEvents.RaiseBuildingTakeRequested(definition, qty)` → `InventoryOrchestrator` subscribes → creates a new `ToolBuilder` from the definition → adds to inventory. `OnBuildingRemoved` event fires. `GameEvents.RaiseBuildingRemoved(this)`. `Destroy(gameObject)` removes the building from the world.

**Pack:** `BuildingObject.Pack()` → `Instantiate(PackedPrefab or default crate)` at `BuildingCrateSpawnPoint` with random velocity. Sets `crate.Definition`. Fires `OnBuildingRemoved` + `RaiseBuildingRemoved`. `Destroy(gameObject)`.

---

## Flow 4 — Scaffolding Supports

When a `BuildingObject.Start()` runs (non-ghost), `BaseModularSupports.Start()` fires on each support component child. It calls `GetComponentInParent<BuildingObject>()` to find the parent building, then `SpawnSupports()`.

`ModularBuildingSupports.SpawnSupports()` raycasts **downward** from the support position using `BuildingSupportsCollisionLayers`. It calculates how many legs are needed based on `hit.distance / SupportSpacing`. It checks what the raycast hit — if it's another `ModularBuildingSupports`, it uses the matching connection prefab (Roller→BottomToRollerPrefab, Conveyor→BottomToConveyorPrefab, etc.). Then it instantiates `TopSupportPrefab` + N × `MiddleSupportPrefab`, all parented to the support transform.

When the player uses **ToolSupportsWrench** on a building, `BuildingObject.EnableBuildingSupports(false)` clears all support children and re-runs `SpawnSupports()` (which now returns early because `BuildingSupportsEnabled` is false). Toggle back → supports respawn.

---

## Event Registry — Phase D

| Event | Fired By | Subscribed By |
|-------|----------|---------------|
| `OnBuildingPlaced(BuildingObject)` | `ToolBuilder.PrimaryFire` | Phase F: QuestManager, tests |
| `OnBuildingRemoved(BuildingObject)` | `BuildingObject.TryTakeOrPack`, `BuildingObject.Pack` | Phase F: QuestManager, tests |
| `OnBuildingTakeRequested(SO_BuildingInventoryDef, qty)` | `BuildingObject.TryTakeOrPack`, `BuildingCrate.TryAddToInventory` | InventoryOrchestrator (creates ToolBuilder + adds to hotbar) |