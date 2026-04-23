# Building Placement — Manual Test

> Verifies: ghost preview, grid snap, rotation, conveyor auto-snap, place, take, pack.

---

## Prerequisites

- BuildingManager + ConveyorBeltManager singletons
- BuildingTest script on a GO
- SO_BuildingInventoryDefinition asset with a conveyor belt prefab assigned
- Camera in scene (for raycast placement)

---

## Setup Guide

### Step 1 — Singletons

| GO Name | Component | Key Fields |
|---------|-----------|------------|
| `BuildingManager` | `BuildingManager` | Assign all materials (Ghost, Invalid, Requirement, NodeGhost, lights), layer masks, BuildingCratePrefab, BuildingToolPrefab |
| `ConveyorBeltManager` | `ConveyorBeltManager` | No fields to wire |

### Step 2 — Layers

Create in Edit → Project Settings → Tags and Layers:
- `BuildingGhost` — ghost objects during placement
- `BuildingObject` — placed buildings' physical colliders

### Step 3 — Materials

Create 5 materials:
- `M_Ghost_Valid` — transparent green (0.2, 1, 0.2, 0.3), Standard/URP Lit, Surface=Transparent
- `M_Ghost_Invalid` — transparent red (1, 0.2, 0.2, 0.3)
- `M_Ghost_Requirement` — transparent yellow (1, 1, 0.2, 0.3)
- `M_BuildingNodeGhost` — transparent cyan (0.2, 0.8, 1, 0.3)
- `M_BuildingNodeGhost_WrongType` — transparent grey (0.5, 0.5, 0.5, 0.3)

Assign all to BuildingManager inspector fields.

### Step 4 — SO_BuildingInventoryDefinition

1. Create → SO → SO_BuildingInventoryDefinition → name `ConveyorBelt_Def`
2. Name = "Conveyor Belt", MaxInventoryStackSize = 50
3. BuildingPrefabs → add your ConveyorBelt prefab (must have BuildingObject component)

### Step 5 — Conveyor Belt Prefab

```
ConveyorBelt_Prefab (root)
├── BuildingObject component:
│     _definition → ConveyorBelt_Def
│     _savableObjectID → matching enum
│     ConveyorInputSnapPositions → [InputSnap child transform]
│     ConveyorOutputSnapPositions → [OutputSnap child transform]
├── PhysicalColliderObject (child, BoxCollider, layer will be set to BuildingObject)
├── BuildingPlacementColliderObject (child, BoxCollider for overlap checks)
├── ConveyorBelt component (Speed=0.8, trigger collider IsTrigger=true)
├── InputSnap (empty child at belt input end)
└── OutputSnap (empty child at belt output end)
```

### Step 6 — BuildingTest Script

1. Create GO → add `BuildingTest` component
2. Wire: `_cam` → Camera, `_testDefinition` → ConveyorBelt_Def

### Step 7 — Floor + Walls

- Floor plane at y=0 (layer Default or Ground)
- Optional: wall cubes on layer `BuildingObject` for overlap testing

---

## How It Works (System Flow)

**Ghost preview:** Every frame, `ToolBuilder.Update()` raycasts from camera → calls `BuildingManager.DataService.GetClosestGridPosition(hit.point)` to snap to 1m grid → calls `BuildingManager.UpdateGhostObject(gridPos, prefab, rotation, this)`. First call **Instantiates** the building as a ghost — `IsGhost = true`, triggers destroyed, MonoBehaviours disabled, layer set to `BuildingGhost`, Rigidbodies kinematic. The ghost follows the grid position. `CanPlaceObject` validates: **OverlapBox** check against ghost colliders, **flat ground** raycast, **conveyor snap** via `BuildingDataService.GetNearbySnapConnections` (tests 4 rotations × input/output snap points, picks best-voted). Based on result, ghost materials swap to **green** (valid), **red** (invalid), or **yellow** (requirements not met).

**Placing:** `ToolBuilder.PrimaryFire()` checks `CanPlaceObject == Valid` → `Object.Instantiate(prefab, position, rotation)` → `IsGhost = false` → `BuildingObject.Start()` sets physical collider layer to `BuildingObject` → `RaiseBuildingPlaced` fires → `Quantity--`.

**Taking:** Player interacts with placed building → `BuildingObject.TryTakeOrPack()` → fires `RaiseBuildingTakeRequested(definition)` → InventoryOrchestrator creates ToolBuilder + adds to hotbar → `RaiseBuildingRemoved` → `Destroy(gameObject)`.

**Packing:** `BuildingObject.Pack()` → Instantiate crate at spawn point with random velocity → `RaiseBuildingRemoved` → `Destroy(gameObject)`.

---

## 1. Initial State

**DO:** Press Play
**EXPECT:**
- No ghost visible (ToolBuilder not equipped via test — BuildingTest calls PrimaryFire directly)
- BuildingManager singleton exists
- Console: no errors

---

## 2. Place First Building

**DO:** Aim camera at floor → press `Space`
**EXPECT:**
- **Conveyor belt appears** at grid-snapped position on the floor
- Console: `[BuildingTest] Placed: Conveyor Belt`
- Belt has physical collider on `BuildingObject` layer

**Behind the scenes:** `ToolBuilder.PrimaryFire()` → raycast hit floor → `GetClosestGridPosition` snaps to grid → `CanPlaceObject` returns Valid (no overlap, no requirements) → `Instantiate(prefab)` → `RaiseBuildingPlaced` fires.

---

## 3. Rotate + Place Second

**DO:** Press `R` (rotate 90°) → aim near first belt → press `Space`
**EXPECT:**
- Second belt placed at **90° to first**
- If input/output snap points are close → **auto-snapped** (belt aligns to first belt's output)

---

## 4. Overlap Rejection

**DO:** Aim at existing belt → press `Space`
**EXPECT:**
- **Nothing placed** — `CanPlaceObject` returns `Invalid` (OverlapBox detected collision)
- No console "Placed" message

---

## 5. Pack Building

**DO:** (manual test — call Pack on a BuildingObject via inspector or add test key)
**EXPECT:**
- Building **disappears**
- **Crate appears** at BuildingCrateSpawnPoint with random velocity
- Console: `[BuildingTest] Removed: Conveyor Belt`

---

## Summary Checklist

- [ ] Ghost appears at grid-snapped position (0.5 offset)
- [ ] Ghost material: green when valid, red when overlapping
- [ ] R key rotates ghost 90° per press
- [ ] Space places real building (ghost position + rotation)
- [ ] Conveyor auto-snap aligns to neighbor output→input
- [ ] Overlapping placement rejected
- [ ] Take removes building, fires RaiseBuildingTakeRequested
- [ ] Pack spawns crate with random velocity, fires RaiseBuildingRemoved
- [ ] Console logs every GameEvent fire
- [ ] Zero errors