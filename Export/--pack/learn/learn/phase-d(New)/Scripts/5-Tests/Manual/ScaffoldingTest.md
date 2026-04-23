# Scaffolding Supports — Manual Test

> Verifies: dynamic leg spawning, raycast downward, support type matching, wrench toggle.

---

## Prerequisites

- BuildingManager singleton (for collision layer refs)
- A placed BuildingObject with ModularBuildingSupports or ScaffoldingSupportLeg child components
- Ground/floor below the building at varying heights

---

## Setup Guide

### Step 1 — Building with Supports

Use a building prefab that has `ModularBuildingSupports` or `ScaffoldingSupportLeg` as a child component:

```
TestBuilding (root)
├── BuildingObject component
├── PhysicalColliderObject (BoxCollider)
├── ModularBuildingSupports component:
│     _supportType = Conveyor
│     _topSupportPrefab → top leg mesh prefab
│     _middleSupportPrefab → middle leg mesh prefab
│     _bottomCapPrefab → bottom cap mesh prefab
│     _supportSpacing = 1
│     _maxSupports = 15
└── ScaffoldingSupportLeg component (optional, simpler legs):
      _supportPrefab → single leg mesh prefab
      _supportSpacing = 1
      _maxSupports = 15
```

### Step 2 — Terrain Setup

- Create floor at y=0
- Create a raised platform at y=3 (building will be placed here)
- The gap between building and floor = 3 units → should spawn 3 support legs

### Step 3 — Wrench Tool (for toggle test)

- Have a ToolSupportsWrench in the inventory (Phase B)
- Or manually call `BuildingObject.EnableBuildingSupports(false/true)` via inspector

---

## How It Works (System Flow)

**Spawn:** When `BuildingObject.Start()` runs (non-ghost), each `BaseModularSupports` child's `Start()` fires → calls `GetComponentInParent<BuildingObject>()` to find the parent → calls `SpawnSupports()`.

**ModularBuildingSupports.SpawnSupports():** Raycasts **downward** from `transform.position + _raycastOffset` using `BuildingSupportsCollisionLayers`. Calculates leg count: `RoundToInt(hitDistance / _supportSpacing) + 1`. Checks what the ray hit — if it's another `ModularBuildingSupports`, uses the **matching connection prefab** (e.g. hit Roller type → use `_bottomToRollerPrefab`). Then Instantiates `_topSupportPrefab` at top, N × `_middleSupportPrefab` stepping down by `_supportSpacing`, and `_bottomCapPrefab` at the ground hit point (with random rotation/scale variation). All legs are **parented to the support transform**.

**ScaffoldingSupportLeg.SpawnSupports():** Simpler — destroys existing children, raycasts down, spawns N × `_supportPrefab` at spacing intervals, parented to self.

**Wrench toggle:** `BuildingObject.EnableBuildingSupports(false)` sets `BuildingSupportsEnabled = false` → loops all `BaseModularSupports` → calls `RespawnSupports()`. Since `BuildingSupportsEnabled` is false, `SpawnSupports()` returns early — **all legs disappear**. Toggle back to true → legs respawn.

**Building removed:** `BuildingObject.OnDestroy()` calls `UpdateSupportsAbove(true)` — raycasts up to find any `ModularBuildingSupports` above → tells them to `RespawnSupports(nextFrame: true)` so legs adjust to the gap.

---

## 1. Initial State — Legs Spawn

**DO:** Place building on raised platform → Press Play
**EXPECT:**
- Support legs **appear** between building and floor
- Number of legs matches gap height ÷ spacing
- Top support + N middle supports + bottom cap visible

**Behind the scenes:** `BaseModularSupports.Start()` → `GetComponentInParent<BuildingObject>()` → `SpawnSupports()` → raycast down → calculate count → Instantiate legs.

---

## 2. Wrench Disable

**DO:** Equip ToolSupportsWrench → left-click on building (or call `EnableBuildingSupports(false)`)
**EXPECT:**
- All support legs **disappear**
- Building stays in place (it's static, not affected by gravity)

---

## 3. Wrench Re-enable

**DO:** Right-click on building (or call `EnableBuildingSupports(true)`)
**EXPECT:**
- Support legs **respawn** — same count, same positions

---

## 4. Building Removed — Supports Above Adjust

**DO:** Destroy a building that has another building stacked above it with supports
**EXPECT:**
- Lower building **destroyed**
- Upper building's supports **respawn** (longer legs to reach new ground level)

**Behind the scenes:** `OnDestroy()` → `UpdateSupportsAbove(true)` → raycast up → find `ModularBuildingSupports` → `RespawnSupports(nextFrame: true)` → coroutine waits 1 FixedUpdate → rebuilds.

---

## 5. Edge Case — No Ground Below

**DO:** Place building in mid-air with no floor below (or floor > maxSupports * spacing away)
**EXPECT:**
- **No legs spawn** — raycast misses (nothing within range)
- No errors

---

## Summary Checklist

- [ ] Supports spawn on Start with correct count based on gap height
- [ ] Top + middle + bottom cap prefabs all visible
- [ ] Bottom cap has slight random rotation/scale variation
- [ ] Wrench disable → all legs disappear
- [ ] Wrench re-enable → all legs respawn correctly
- [ ] Building destroyed → supports above adjust leg count
- [ ] No ground → no legs, no error
- [ ] Different SupportType connections (Roller, Conveyor, etc.) use matching prefabs
- [ ] Zero console errors
