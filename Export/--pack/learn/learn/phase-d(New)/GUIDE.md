# Phase D — Building & Conveyor System (15%)

## What It Looks Like When Running

```
Open shop → buy "Conveyor Belt" → a crate spawns near elevator.
Walk to crate, press E → "Take" → goes into hotbar as ToolBuilder.

Equip the conveyor tool from hotbar:
  - A transparent green ghost follows your camera aim, snapped to 1m grid
  - Move mouse → ghost slides along grid positions
  - Look at invalid spot (overlapping building) → ghost turns red
  - Press R → ghost rotates 90°
  - Press Q → ghost mirrors (for L/R variants)
  - Place near another conveyor → auto-snaps input→output
  - Left-click → real conveyor belt instantiates at ghost position
  - Tool quantity decreases. At 0, tool is consumed.

Conveyor belt in the world:
  - Ore touching the belt trigger gets pushed forward via physics
  - Place multiple belts end-to-end → ore flows along the line
  - Belt has visual texture scroll (ConveyorRenderer batch rendering)

Conveyor variants:
  - ConveyorBeltShaker: oscillates left-right + up-down (sieving)
  - ConveyorBeltShakerHorizontal: oscillates left-right only
  - ConveyorBlocker: hinge gate, disables belt when closed
  - ConveyorBlockerT2: interactable sliding gate (IInteractable "Toggle")
  - ConveyorSplitterT2: rotating arm that alternates ore direction
  - RoutingConveyor: interactable direction switch (IInteractable)

Equip hammer → look at placed building:
  - FresnelHighlighter outlines it in cyan
  - Press E → interaction wheel: "Take" or "Pack"
  - Take → building goes back into inventory as ToolBuilder
  - Pack → building becomes a crate on the ground

Buildings on uneven ground:
  - Modular scaffolding legs raycast downward
  - Legs spawn dynamically to reach the floor
  - Toggle supports on/off with wrench tool
```

---

## Folder Structure

```
phase-d(New)/
├── 0-Core/
│   └── GameEvents.cs                           (partial: OnBuildingPlaced, OnBuildingRemoved)
├── 1-Managers/
│   ├── BuildingManager.cs                       → "I manage ghost preview + grid placement + conveyor snap"
│   └── ConveyorBeltManager.cs                   → "I apply accumulated conveyor velocities in FixedUpdate"
├── 2-Data/
│   ├── SO_BuildingInventoryDefinition.cs        → "I define a building type (name, icon, prefabs, stack size)"
│   ├── DataService/
│   │   └── BuildingDataService.cs               → "I validate grid placement + detect conveyor snap alignment"
│   ├── Enums/
│   │   └── GlobalEnumsD.cs                      → "CanPlaceBuilding, PlacementNodeRequirement, SupportType"
│   ├── Interface/
│   │   └── ISaveLoadableBuildingObject.cs    → "I persist buildings across saves (Phase G)"
│   └── Entities/
│       ├── BuildingRotationInfo.cs              → "snap rotation + mirror flag for conveyor alignment"
│       ├── BuildingObjectEntry.cs               → "save data for one placed building (Phase G)"
│       └── RoutingConveyorSaveData.cs           → "save data for toggleable components (IsClosed)"
├── 3-MonoBehaviours/
│   ├── Building/
│   │   ├── BuildingObject.cs                    → "I'm a placed building with IInteractable (Take/Pack)"
│   │   ├── BuildingCrate.cs                     → "I'm a packed building crate on the ground"
│   │   ├── BuildingPlacementNode.cs             → "I mark where AutoMiners attach (resource node spots)"
│   │   ├── BaseModularSupports.cs               → "I'm the base for scaffolding leg systems"
│   │   ├── ModularBuildingSupports.cs           → "I spawn dynamic scaffolding legs via raycast"
│   │   ├── ScaffoldingSupportLeg.cs             → "I spawn simple repeating support legs"
│   │   ├── ChuteHatch.cs                        → "I'm a toggleable hatch on a chute building"
│   │   ├── ChuteTop.cs                          → "I check for Hopper above and convert if found"
│   │   └── RobotGrabberArm.cs                   → "I'm an automated arm that grabs ore along an arc"
│   ├── Conveyor/
│   │   ├── ConveyorBelt.cs                      → "I push physics objects forward via trigger"
│   │   ├── ConveyorBeltShaker.cs                → "I shake left-right + up-down (sieving table)"
│   │   ├── ConveyorBeltShakerHorizontal.cs      → "I shake left-right only"
│   │   ├── ConveyorBlocker.cs                   → "I disable belt when hinge gate is closed"
│   │   ├── ConveyorBlockerT2.cs                 → "I'm an interactable sliding gate blocker"
│   │   ├── ConveyorSplitterT2.cs                → "I swing a rotating arm to alternate ore direction"
│   │   ├── RoutingConveyor.cs                   → "I'm an interactable direction switch conveyor"
│   │   ├── ConveyorRenderer.cs                  → "I batch-render all conveyor meshes via DrawMeshInstanced"
│   │   ├── ConveyorBatchRenderingComponent.cs   → "I cache my transform matrix for batch rendering"
│   │   └── ConveyorSoundSource.cs               → "I mark where conveyor sound plays (Phase H)"
│   └── Tool/
│       └── ToolBuilder.cs                       → "I complete the Phase B partial — ghost + grid + place"
├── 4-Utils/
│   ├── UtilsPhaseD.cs                           → "grid math + building placement helpers"
│   └── PhaseDLOG.cs                             → "building/conveyor snapshot formatters"
└── 5-Tests/
    ├── BuildingTest.cs                           → "I test place/rotate/snap without player"
    ├── ConveyorTest.cs                           → "I test ore flows along belt chain"
    └── Manual/
        ├── BuildingPlacementTest.md             (ghost preview, grid snap, rotation, conveyor snap)
        ├── ConveyorFlowTest.md                  (belt texture scroll, ore flow, variants)
        └── ScaffoldingTest.md                   (scaffolding legs raycast, wrench toggle)
```

---

## Script Purpose — One Sentence Each

| Script | Purpose |
|--------|---------|
| `GameEvents.cs` | I deliver Phase D messages (building placed, building removed) |
| `BuildingManager.cs` | I manage ghost preview, grid placement validation, conveyor snap detection |
| `ConveyorBeltManager.cs` | I apply accumulated conveyor velocities to registered physics objects in FixedUpdate |
| `SO_BuildingInventoryDefinition.cs` | I define a building type with name, icon, prefabs, stack size, placement options |
| `BuildingDataService.cs` | I validate grid placement (overlap, flat ground, node requirement) + detect conveyor snap |
| `GlobalEnumsD.cs` | I hold Phase D enums: CanPlaceBuilding, PlacementNodeRequirement, SupportType |
| `BuildingRotationInfo.cs` | I store a snap rotation + mirror flag for conveyor alignment voting |
| `BuildingObjectEntry.cs` | I store save data for one placed building (Phase G uses this) |
| `BuildingObject.cs` | I'm a placed building with IInteractable — Take returns to inventory, Pack spawns crate |
| `BuildingCrate.cs` | I'm a packed building crate on the ground — Take creates ToolBuilder + adds to inventory |
| `BuildingPlacementNode.cs` | I mark where AutoMiners attach — self-registers, shows ghost indicator |
| `BaseModularSupports.cs` | I'm the base class for all scaffolding leg systems |
| `ModularBuildingSupports.cs` | I spawn dynamic scaffolding legs via downward raycast + support type matching |
| `ScaffoldingSupportLeg.cs` | I spawn simple repeating support legs downward |
| `ChuteHatch.cs` | I'm a toggleable hatch on a chute — dual rotating parts + light indicator |
| `ChuteTop.cs` | I check for Hopper above on enable — converts to hopper-chute version if found |
| `RobotGrabberArm.cs` | I'm an automated IK arm that grabs ore from trigger, moves along arc, drops at target |
| `ISaveLoadableBuildingObject.cs` | I extend ISaveLoadableObject with building-specific save data (support state) |
| `RoutingConveyorSaveData.cs` | I store IsClosed state for toggleable components (Phase G save/load) |
| `ConveyorBelt.cs` | I push physics objects forward via trigger enter/exit + FixedUpdate velocity |
| `ConveyorBeltShaker.cs` | I extend ConveyorBelt with left-right + up-down oscillation (sieving) |
| `ConveyorBeltShakerHorizontal.cs` | I extend ConveyorBelt with left-right oscillation only |
| `ConveyorBlocker.cs` | I disable my paired ConveyorBelt when hinge gate angle exceeds threshold |
| `ConveyorBlockerT2.cs` | I'm an interactable sliding gate — Toggle opens/closes with tween |
| `ConveyorSplitterT2.cs` | I swing a rotating arm back and forth to alternate ore flow direction |
| `RoutingConveyor.cs` | I'm an interactable direction switch — Toggle rotates between two directions |
| `ConveyorRenderer.cs` | I batch-render all conveyor meshes using DrawMeshInstanced for performance |
| `ConveyorBatchRenderingComponent.cs` | I cache my transform matrix and self-register for batch rendering |
| `ConveyorSoundSource.cs` | I mark where conveyor sound plays (Phase H wires audio) |
| `ToolBuilder.cs` | I complete the Phase B partial — ghost preview, grid snap, place, consume quantity |
| `UtilsPhaseD.cs` | I hold grid math helpers + building layer utilities |
| `PhaseDLOG.cs` | I format building/conveyor snapshots for test logging |

---

## Hand-Typing Order (Compile Groups)

### Group 1 — Pure Data
1. `GlobalEnumsD.cs`
2. `BuildingRotationInfo.cs`
3. `BuildingObjectEntry.cs`

**STOP — compile.**

### Group 2 — SO + DataService
4. `SO_BuildingInventoryDefinition.cs`
5. `BuildingDataService.cs`

**STOP — compile. Run DEBUG if needed.**

### Group 3 — GameEvents + Utils
6. `GameEvents.cs` (partial)
7. `UtilsPhaseD.cs`
8. `PhaseDLOG.cs`

**STOP — compile.**

### Group 4 — Conveyor Chain
9. `ConveyorBelt.cs`
10. `ConveyorBeltShaker.cs`
11. `ConveyorBeltShakerHorizontal.cs`
12. `ConveyorBlocker.cs`
13. `ConveyorBlockerT2.cs`
14. `ConveyorSplitterT2.cs`
15. `RoutingConveyor.cs`
16. `ConveyorBatchRenderingComponent.cs`
17. `ConveyorRenderer.cs`
18. `ConveyorSoundSource.cs`

**STOP — compile.**

### Group 5 — Building Chain
19. `BaseModularSupports.cs`
20. `ModularBuildingSupports.cs`
21. `ScaffoldingSupportLeg.cs`
22. `BuildingObject.cs`
23. `BuildingCrate.cs`
24. `BuildingPlacementNode.cs`
25. `ChuteHatch.cs`
26. `ChuteTop.cs`
27. `RobotGrabberArm.cs`
28. `ISaveLoadableBuildingObject.cs`
29. `RoutingConveyorSaveData.cs`

**STOP — compile.**

### Group 6 — Managers
30. `BuildingManager.cs`
31. `ConveyorBeltManager.cs`

**STOP — compile.**

### Group 7 — ToolBuilder (complete Phase B partial)
32. `ToolBuilder.cs`

**STOP — compile.**

### Group 8 — Tests
33. `BuildingTest.cs`
34. `ConveyorTest.cs`

**STOP — compile. Run all tests + 3 manual tests.**

---

## Vertical Slice Tests (`.cs` — automated bootstrap)

> These scripts fire GameEvents + log to console. Run in Play mode.

### 1. BuildingTest — Place/Rotate/Snap/Take/Pack (UI-Level)

**What you need to type first:** All Phase D scripts + BuildingManager + ConveyorBeltManager + ToolBuilder + GameEvents
**What you DON'T need:** Player movement, shop, ore, interaction system

**Step-by-step scene setup:**
1. Create `BuildingManager` singleton GO → assign all materials (Ghost valid/invalid/requirement, node ghosts, light materials), all layer masks, BuildingCratePrefab, BuildingToolPrefab
2. Create `ConveyorBeltManager` singleton GO (no fields)
3. Create `EconomyManager` singleton GO (phase-All, _defaultMoney = 400)
4. Create SO_BuildingInventoryDefinition asset → name "ConveyorBelt_Def" → assign a ConveyorBelt BuildingObject prefab
5. Create `BuildingTest` GO → add `BuildingTest` component → assign `_cam` (Camera), `_testDefinition` (ConveyorBelt_Def SO)
6. Create Camera GO (or use existing)
7. Create Floor (Plane at y=0)
8. Add layers: "BuildingGhost", "BuildingObject" in Project Settings → Tags and Layers
9. Press Play

**Controls:**

| Key | What it does | What you should see |
|-----|-------------|---------------------|
| `Space` | Place building at camera raycast point | Console: `[BuildingTest] Placed: Conveyor Belt` |
| `R` | Rotate ghost 90° | Ghost changes orientation |
| `Q` | Cycle building variant | Ghost swaps to alternate prefab |
| `U` | Take nearest building back | Console: `[BuildingTest] TakeRequested: Conveyor Belt x1` + `Removed:` |
| `I` | Pack nearest into crate | Crate appears with random velocity, Console: `Removed:` |
| `O` | Log conveyor belt snapshot | Console: belt count + JSON |
| `M/N` | Menu toggle (sim) | Locks/unlocks input |

**Checklist:**
- [ ] Space places building at grid-snapped position
- [ ] Ghost preview visible (green=valid, red=overlap)
- [ ] R rotates ghost 90° per press
- [ ] Conveyor auto-snap aligns to neighbor output→input
- [ ] U takes building back → Console logs TakeRequested + Removed
- [ ] I packs building → crate appears with random velocity
- [ ] Overlapping placement rejected (ghost stays red, Space does nothing)
- [ ] Console logs every GameEvent fire
- [ ] Zero errors

### 2. ConveyorTest — Ore Flow + Variants (UI-Level)

**What you need to type first:** ConveyorBelt + all variants + ConveyorBeltManager + Phase C OrePiece + OrePiecePoolManager
**What you DON'T need:** Player movement, shop, building placement

**Step-by-step scene setup:**
1. Create `ConveyorBeltManager` singleton GO
2. Create `OrePiecePoolManager` singleton GO → assign `_allOrePiecePrefabs` list with OrePiece_Iron
3. Create `EconomyManager` singleton GO (phase-All)
4. Place 3-4 ConveyorBelt prefabs end-to-end on floor (forward arrows aligned left→right)
5. Optionally place: ConveyorBeltShaker, ConveyorBlockerT2, RoutingConveyor, ConveyorSplitterT2, SellerMachine
6. Create `ConveyorTest` GO → assign `_testOrePrefab` (OrePiece_Iron), `_spawnPoint` (transform above first belt), `_testBlocker`, `_testRouter`
7. Create Camera GO
8. Press Play

**Controls:**

| Key | What it does | What you should see |
|-----|-------------|---------------------|
| `Space` | Spawn ore above first belt | Ore appears, falls, slides forward |
| `U` | Toggle ConveyorBlockerT2 | Console: `Blocker: CLOSED/OPEN` |
| `I` | Toggle RoutingConveyor | Console: `Router: CLOSED/OPEN` |
| `O` | Log ore + belt counts | Console: counts |
| `P` | Log conveyor snapshot | Console: JSON of all belts |
| `M/N` | Menu toggle (sim) | — |

**Checklist:**
- [ ] Ore on belt → pushed forward at Speed (0.8 m/s)
- [ ] Multiple belts → ore flows through chain seamlessly
- [ ] ConveyorBeltShaker → ore oscillates left-right + up-down
- [ ] ConveyorBlockerT2 CLOSED → ore stops; OPEN → resumes
- [ ] RoutingConveyor Toggle → ore follows new path
- [ ] ConveyorSplitterT2 → arm swings, alternates ore
- [ ] SellerMachine at end → ore sold, Console: `Sold:` + `Money:`
- [ ] Console logs every GameEvent fire
- [ ] Zero errors

---

## Manual Tests (`5-Tests/Manual/*.md`)

> These teach the system's internal flow AND test visually. Setup + How It Works + DO/EXPECT + Checklist.

| # | File | What to verify |
|---|------|---------------|
| 1 | `BuildingPlacementTest.md` | Ghost preview: `BuildingManager.UpdateGhostObject` → grid snap → OverlapBox validation → material swap green/red → conveyor snap voting → place via Instantiate |
| 2 | `ConveyorFlowTest.md` | Belt physics: `ConveyorBelt.OnTriggerEnter` → `AddPhysicsObject` → `FixedUpdate` velocity → `ConveyorBeltManager` applies accumulated velocity. All variants tested. |
| 3 | `ScaffoldingTest.md` | Support spawning: `BaseModularSupports.Start` → `SpawnSupports` → downward raycast → leg prefab instantiation. Wrench toggle: `EnableBuildingSupports` → `RespawnSupports` |

---

## Art & Scene Work (Non-Script)

### Materials

| Material | Used By | Description |
|----------|---------|-------------|
| `M_Ghost_Valid` | BuildingManager | Transparent green for valid placement |
| `M_Ghost_Invalid` | BuildingManager | Transparent red for invalid/overlapping |
| `M_Ghost_Requirement` | BuildingManager | Transparent yellow for unmet requirements |
| `M_BuildingNodeGhost` | BuildingPlacementNode | Shows matching node indicator |
| `M_BuildingNodeGhost_WrongType` | BuildingPlacementNode | Shows wrong-type node indicator |
| `M_GreenLight` | AutoMiner/BuildingManager | Green indicator on machines |
| `M_RedLight` | AutoMiner/BuildingManager | Red indicator on machines |

### Layers

| Name | Used By |
|------|---------|
| `BuildingGhost` | Ghost object during placement preview |
| `BuildingObject` | Placed buildings' physical colliders |

### Building Prefab Hierarchy

```
ConveyorBelt_Prefab (root)
├── BuildingObject component (Definition, SavableObjectID, snap positions)
├── PhysicalColliderObject (child — BoxCollider, layer BuildingObject)
├── BuildingPlacementColliderObject (child — for overlap checks)
├── ConveyorBelt component (Speed, trigger collider IsTrigger=true)
├── ConveyorBatchRenderingComponent (MeshIndex for batch render)
├── ConveyorInputSnapPositions (empty children marking input snap points)
├── ConveyorOutputSnapPositions (empty children marking output snap points)
├── ModularBuildingSupports (optional — for scaffolding)
└── BuildingCrateSpawnPoint (empty child — where crate spawns on Pack)
```

---

## Modifications to Earlier Phases

| File (Phase) | How | Change | Why |
|-------------|-----|--------|-----|
| `ToolBuilder.cs` (B) | **replace** | Complete placement logic — ghost + grid + place | Phase B was partial stubs |
| `SO_ShopItemDef.cs` (A) | **direct modify** | Add `SO_BuildingInventoryDefinition` field | Links shop items to building data |
| `BasePhysicsObject.cs` (B) | **direct modify** | Complete conveyor registration stubs | Phase B had `// Phase D:` stubs |
| `FresnelHighlighter.cs` (B) | **direct modify** | Add building + wrench highlight checks | Phase D outlines buildings with hammer/wrench |

---

## Source vs Phase Diff

| What | Original Did | What We Did | Why |
|------|-------------|-------------|-----|
| BuildingManager | 404 lines, all placement + ghost + snap in one class | Split: `BuildingManager` (ghost/material, 1-Manager) + `BuildingDataService` (pure math validation) | DataService testable without scene |
| BuildingObject.TryAddToInventory | `FindObjectOfType<PlayerInventory>()` | `GameEvents.RaiseBuildingTakeRequested(definition)` → InventoryOrchestrator subscribes | Decoupled |
| ConveyorBeltManager | Static Register/Unregister + FixedUpdate velocity | Same pattern — centralized FixedUpdate avoids per-belt overhead | Matches original, good design |
| RoutingConveyor/ConveyorBlockerT2 | DOTween for animation | Same (DOTween is allowed) or fallback to coroutine lerp | Keep DOTween if available |
| SetLayerRecursively | In BuildingManager as private | In `UtilsPhaseB.SetLayerRecursively` (already exists) | DRY |

---

## Systems & Testability

### Individual Systems

| # | System | Scripts | Decoupled Via |
|---|--------|---------|---------------|
| 1 | **Building Placement** | `BuildingManager`, `BuildingDataService`, `ToolBuilder`, `BuildingPlacementNode` | `OnBuildingPlaced` |
| 2 | **Building Lifecycle** | `BuildingObject`, `BuildingCrate` | `OnBuildingRemoved`, IInteractable |
| 3 | **Conveyor Physics** | `ConveyorBelt`, `ConveyorBeltShaker`, `ConveyorBeltShakerHorizontal`, `ConveyorBeltManager` | Self-registration (`AllConveyorBelts`), `BasePhysicsObject` accumulation |
| 4 | **Conveyor Variants** | `ConveyorBlocker`, `ConveyorBlockerT2`, `ConveyorSplitterT2`, `RoutingConveyor` | IInteractable for toggleable variants |
| 5 | **Conveyor Rendering** | `ConveyorRenderer`, `ConveyorBatchRenderingComponent` | Static list, DrawMeshInstanced |
| 6 | **Scaffolding** | `BaseModularSupports`, `ModularBuildingSupports`, `ScaffoldingSupportLeg` | `BuildingObject.EnableBuildingSupports` |
| 7 | **Chute/Grabber** | `ChuteHatch`, `ChuteTop`, `RobotGrabberArm` | IInteractable (ChuteHatch), self-contained (others) |

### Testability Matrix

| System | `.cs` Test | `Manual/*.md` | Needs other systems? |
|--------|-----------|---------------|---------------------|
| Placement + Ghost | `BuildingTest` | `BuildingPlacementTest.md` | Needs BuildingManager, ToolBuilder |
| Conveyor Flow | `ConveyorTest` | `ConveyorFlowTest.md` | Needs OrePiece (Phase C), ConveyorBeltManager |
| Scaffolding | — | `ScaffoldingTest.md` | Needs BuildingObject + ModularBuildingSupports |
| Building Lifecycle | `BuildingTest` | `BuildingPlacementTest.md` | Needs BuildingManager |
| Conveyor Variants | `ConveyorTest` | `ConveyorFlowTest.md` | Needs ConveyorBelt base |
| Batch Rendering | — | `ConveyorFlowTest.md` | Visual only — DrawMeshInstanced |

**7 systems, 32 scripts, 2 `.cs` tests, 3 manual tests. Zero tight coupling between systems.**