# Phase C — Mining & Ore System (14%)

## What It Looks Like When Running

```
You walk into a mine tunnel from the starting room.
Glowing ore nodes embedded in the walls/floor — different
colors for Iron (grey), Gold (yellow), Copper (orange), Coal (black).

Equip pickaxe from hotbar → hold left-click:
  - Swing animation plays
  - 0.2s delay, then raycast hits the node
  - Particle sparks fly from impact point
  - Node health decreases
  - After 3-4 hits, node shatters:
    → 2-4 ore pieces fly out with random velocity
    → Pieces bounce and roll on the ground (Rigidbody physics)
    → Break particle burst plays
    → Node disappears permanently

Ore pieces on the ground:
  - Grabbable with hand (right-click SpringJoint from Phase B)
  - Pullable with magnet tool (Phase B)
  - Each has ResourceType (Iron, Gold, etc.) + PieceType (Ore, Crushed, etc.)
  - Random mesh variant + slight scale variation for visual variety
  - Random price multiplier (0.9x–1.1x)

AutoMiner placed at a node → rotates continuously, spawns ore
on a timer. Probability-based (80% default). Rate adjustable.

SellerMachine (trigger volume) → ore enters → waits 2s →
money increases → ore returns to pool.

With 500+ ore pieces active, OreLimitManager kicks in:
  - UI warning appears
  - Auto-miner spawn rate slows down
  - At limit+ moving objects, spawning blocks entirely

OrePiecePoolManager recycles all ore — zero Instantiate/Destroy
after initial pool warmup. Smooth performance.
```

---

## Folder Structure

```
phase-c(New)/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnOreMined, OnOreSold, OnOreLimitChanged)
├── 1-Managers/
│   ├── OreManager.cs                       → "I clean up invalid ore pieces (round-robin)"
│   ├── OrePiecePoolManager.cs              → "I recycle ore objects to avoid GC spikes"
│   ├── OreLimitManager.cs                  → "I throttle spawning when too many physics objects exist"
│   └── ParticleManager.cs                  → "I spawn particle prefabs at world positions"
├── 2-Data/
│   ├── SO_AutoMinerResourceDefinition.cs   → "I configure auto-miner spawn rate + weighted ore drops"
│   ├── Interface/
│   │   └── IDamageable.cs                  → "I can take damage at a position"
│   ├── DataService/
│   │   └── OreDataService.cs               → "I manage resource descriptions + sell values + formatting"
│   └── Enums/
│       └── GlobalEnumsC.cs                 → "all Phase C enums: ResourceType, PieceType, OreLimitState"
├── 3-MonoBehaviours/
│   ├── OreNode.cs                          → "I'm a breakable rock that drops ore pieces when mined"
│   ├── OrePiece.cs                         → "I'm a physical resource object with type + piece type"
│   ├── DamageableOrePiece.cs               → "I'm an OrePiece that breaks on collision damage"
│   ├── AutoMiner.cs                        → "I spawn ore on a timer at a node"
│   ├── SellerMachine.cs                    → "I sell ore that enters my trigger for money"
│   └── PhysicsLimitUIWarning.cs            → "I show/hide ore limit warning text"
├── 4-Utils/
│   ├── UtilsPhaseC.cs                      → "I hold WeightedRandom<T> + ore helpers"
│   └── PhaseCLOG.cs                        → "I format resource snapshots"
└── 5-Tests/
    ├── DEBUG_CheckC.cs                      → "I test OreDataService (plain C#)"
    ├── OreTest.cs                           → "I test spawn/mine/sell flow"
    └── Manual/
        ├── MiningFlowTest.md               (manual: hit node → particles → shatter → ore pieces fly)
        ├── AutoMinerVisualTest.md          (manual: rotator spins, ore spawns on timer)
        └── SellerMachineTest.md            (manual: ore enters trigger → waits → money increases)
```

---

## Script Purpose — One Sentence Each

| Script | Purpose |
|--------|---------|
| `GameEvents.cs` | I deliver Phase C messages (ore mined, ore sold, ore limit changed) |
| `OreManager.cs` | I clean up invalid ore pieces via round-robin check |
| `SO_AutoMinerResourceDefinition.cs` | I hold auto-miner spawn rate + weighted ore drops (pure data, zero methods) |
| `IDamageable.cs` | I'm a contract for anything that takes damage at a position |
| `OreDataService.cs` | I manage resource descriptions, sell values, and formatted strings |
| `GlobalEnumsC.cs` | I hold all Phase C enums: ResourceType, PieceType, OreLimitState |
| `OreNode.cs` | I'm a breakable rock that drops ore pieces when mined |
| `OrePiece.cs` | I'm a physical resource object with type + piece type |
| `DamageableOrePiece.cs` | I'm an OrePiece that breaks when hit hard enough (collision damage) |
| `OrePiecePoolManager.cs` | I recycle ore objects to avoid GC spikes |
| `OreLimitManager.cs` | I throttle spawning when too many physics objects exist |
| `AutoMiner.cs` | I spawn ore on a timer at a node |
| `SellerMachine.cs` | I sell ore that enters my trigger for money |
| `ParticleManager.cs` | I spawn particle prefabs at world positions |
| `PhysicsLimitUIWarning.cs` | I show/hide ore limit warning text |
| `UtilsPhaseC.cs` | I hold WeightedRandom generic selector + ore helpers |
| `PhaseCLOG.cs` | I format resource description snapshots for test logging |

---

## Hand-Typing Order (Compile Groups)

### Group 1 — Pure Data (compiles alone)
1. `GlobalEnumsC.cs`
2. `IDamageable.cs`

**STOP — compile. Zero errors expected.**

### Group 2 — Entities + SO
3. `SO_AutoMinerResourceDefinition.cs`

**STOP — compile.**

### Group 3 — Utils + DataService
4. `UtilsPhaseC.cs`
5. `PhaseCLOG.cs`
6. `OreDataService.cs`

**STOP — compile. Run DEBUG_CheckC to verify resource lookups.**

### Group 4 — GameEvents
7. `GameEvents.cs` (partial)

**STOP — compile.**

### Group 5 — Managers (Singletons)
8. `ParticleManager.cs`
9. `OrePiecePoolManager.cs`
10. `OreLimitManager.cs`
11. `OreManager.cs`

**STOP — compile.**

### Group 6 — MonoBehaviours
12. `OrePiece.cs`
13. `DamageableOrePiece.cs`
14. `OreNode.cs`
15. `PhysicsLimitUIWarning.cs`
16. `AutoMiner.cs`
17. `SellerMachine.cs`

**STOP — compile. Run OreTest.**

### Group 7 — Tests
18. `DEBUG_CheckC.cs`
19. `OreTest.cs`

**STOP — compile. Run all tests + 3 manual tests.**

---

## Vertical Slice Tests (`.cs` — automated bootstrap)

### 1. DEBUG_CheckC — OreDataService (Data-Level)

> This test proves the OreDataService works as pure C# — zero scene, zero physics, zero prefabs.
> You create an empty scene with ONE GameObject, press keys, and check the console.

**What you need to type first:** `OreDataService.cs`, `GlobalEnumsC.cs`, `PhaseCLOG.cs`
**What you DON'T need:** Player, tools, nodes, pool, managers — nothing. Just the DataService.

**Step-by-step scene setup:**
1. Create a new empty scene in Unity
2. Create an Empty GO → name it `DEBUG_CheckC`
3. Add the `DEBUG_CheckC` component to it
4. In the Inspector, expand `_testDescriptions` list → add 3 entries:

| Index | ResourceType | DisplayColor |
|-------|-------------|-------------|
| 0 | Iron | Grey (0.6, 0.6, 0.6) |
| 1 | Gold | Yellow (1, 0.84, 0) |
| 2 | Coal | Dark Grey (0.2, 0.2, 0.2) |

5. That's it — press Play

**How to test (press these keys one at a time):**

| Key | What it does | What you should see in Console |
|-----|-------------|-------------------------------|
| `Space` | Calls `ds.Build(_testDescriptions)` then logs snapshot | JSON showing 3 resource descriptions with their types + hex colors |
| `U` | Calls `GetColoredResourceTypeString(Iron)` + `GetColoredFormattedResourcePieceString` for 3 combos | `<color=#999999>Iron</color>` then `Iron Ore`, `Gold Drill Bit`, `Junk Pipe` (all with color tags) |
| `O` | Logs full snapshot via `LOG.AddLog` | Same JSON as Space but routed through the LOG system |

**Checklist — every item must pass:**
- [ ] Space → snapshot shows all 3 resource descriptions with correct hex colors
- [ ] U → `GetColoredResourceTypeString(Iron)` wraps "Iron" in `<color=#hex>` tag
- [ ] U → `GetColoredFormattedResourcePieceString(Gold, DrillBit)` shows "Gold Drill Bit" (not "Gold DrillBit")
- [ ] U → `GetColoredFormattedResourcePieceString(Slag, Pipe)` shows "Junk Pipe" (Slag Pipe → "Junk" override)
- [ ] O → snapshot logs via LOG system without errors
- [ ] Zero console errors throughout

---

### 2. OreTest — Spawn/Mine/Sell Flow (UI-Level)

> This test proves the full ore lifecycle: mine nodes → pieces fly out → sell at machine → money increases → pool recycles.
> You need all Phase C singletons but NO player — OreTest provides keyboard controls.

**What you need to type first:** All Phase C scripts + `GameEvents.cs` (partial)
**What you DON'T need:** Player, inventory, shop, interaction — OreTest handles input directly

**Step-by-step scene setup:**

1. **Create singletons** — one Empty GO each, add the component:

| GO Name | Component | What to set in Inspector |
|---------|-----------|------------------------|
| `ParticleManager` | `ParticleManager` | Assign 3 particle prefabs (or leave empty — won't crash, just no particles) |
| `OrePiecePoolManager` | `OrePiecePoolManager` | `_allOrePiecePrefabs` → add all OrePiece prefabs (see step 3) |
| `OreLimitManager` | `OreLimitManager` | `_movingObjectLimit` = 500 |
| `OreManager` | `OreManager` | `_allResourceDescriptions` → add Iron/Gold/Coal entries (see DEBUG_CheckC step 4) |
| `EconomyManager` | `EconomyManager` | `_defaultMoney` = 400 |

2. **Create OrePiece prefab** (at minimum, one):
   - New GO → add `Rigidbody` (mass 0.5, gravity ON) + `MeshFilter` + `MeshRenderer` + `BoxCollider`
   - Add `OrePiece` component: `_resourceType` = Iron, `_pieceType` = Ore, `BaseSellValue` = 1.0
   - Tag: `Grabbable`, Layer: `Interact`
   - Save as prefab → drag into `OrePiecePoolManager._allOrePiecePrefabs`

3. **Create OreNode prefab** (at minimum, one):
   - New GO → add `BoxCollider` + `OreNode` component
   - Set: `_resourceType` = Iron, `_health` = 100, `_minDrops` = 2, `_maxDrops` = 4
   - `_possibleDrops` → add one entry: OrePrefab = your OrePiece_Iron prefab, Weight = 100
   - Layer: `Interact`
   - Place instance in scene on the floor

4. **Create SellerMachine:**
   - New GO → add `BoxCollider` (IsTrigger = true, Size 2,1,2) + `SellerMachine` component
   - Place on floor near the OreNodes

5. **Create OreTest GO:**
   - New GO → add `OreTest` component
   - Wire:

| Field | Drag From |
|-------|-----------|
| `_testNode` | One of the OreNode instances in scene |
| `_testOrePrefab` | OrePiece_Iron prefab from Project |
| `_cam` | Main Camera in scene |

6. **Floor** — Plane at y=0

7. Press Play

**How to test (press these keys one at a time):**

| Key | What it does | What you should see |
|-----|-------------|-------------------|
| `Space` | Damages `_testNode` by 50 | First press: node still exists (health 50). Second press: node **breaks** → 2-4 ore pieces fly out, particles, console `[OreTest] Mined: Iron at (x,y,z)` |
| `U` | Spawns OrePiece at camera forward | Ore piece appears 2m ahead, falls to floor |
| `I` | Logs ore count | Console: `OrePiece active: X, pooled: Y` |
| `O` | Logs OreDataService snapshot | Console: JSON with resource descriptions |
| `M` | Simulates menu open | `RaiseMenuStateChanged(true)` |
| `N` | Simulates menu close | `RaiseMenuStateChanged(false)` |

**Full test flow (do these in order):**
1. Press `Space` once → node still alive, console shows nothing
2. Press `Space` again → node **breaks**, ore flies, console: `[OreTest] Mined: Iron`
3. Press `I` → shows active ore count (should be 2-4)
4. Push ore into SellerMachine trigger (spawn near it with `U` aimed at seller)
5. Wait 2 seconds → ore disappears, console: `[OreTest] Sold: Iron Ore for $X.XX` and `[OreTest] Money: $40X.XX`
6. Press `I` → active count decreased, pooled count increased
7. Press `U` → new ore spawns (reuses pooled piece if available — name ends with `[Pooled]`)
8. Spam `U` 500+ times → console eventually shows `[OreTest] LimitState: SlightlyLimited` then `Blocked`

**Checklist — every item must pass:**
- [ ] Space damages node → health decreases per hit
- [ ] Node at 0 health → shatters → 2-4 ore pieces fly out with random velocity
- [ ] Ore pieces have random mesh + random scale
- [ ] Break particles spawn at hit point (if ParticleManager prefabs assigned)
- [ ] Console: `[OreTest] Mined: Iron at (x,y,z)` on break
- [ ] Ore enters seller trigger → tagged MarkedForDestruction → waits 2s → disappears
- [ ] Console: `[OreTest] Sold: Iron Ore for $X.XX` after sell
- [ ] Console: `[OreTest] Money: $4XX.XX` after sell
- [ ] I key shows `active: X, pooled: Y` — pooled increases after selling
- [ ] U key spawns reuse pooled pieces (name has `[Pooled]`)
- [ ] OreLimitManager fires LimitState changes when enough pieces are moving
- [ ] Zero console errors throughout

---

## Manual Tests (`5-Tests/Manual/*.md` — hands-on, no script)

> These `.md` files teach the system's internal flow AND test it visually. Each contains:
> - **Setup Guide** — beginner-level singleton creation, prefab hierarchies, wiring tables
> - **How It Works** — data flow in plain English (which script → event → subscriber → GO state change)
> - **DO/EXPECT steps** — each step includes behind-the-scenes: which method runs, which event fires, which GOs activate/deactivate
> - **Checklist** — pass/fail items
>
> The reader should understand the full mining architecture by reading these manual tests.

| # | File | What to verify |
|---|------|---------------|
| 1 | `MiningFlowTest.md` | Mining flow: `OreNode.TakeDamage` → health decreases → `BreakNode` → `OrePiecePoolManager.SpawnPooledOre` → ore pieces fly → `RaiseOreMined` fires |
| 2 | `AutoMinerVisualTest.md` | Auto mining: `AutoMiner.Update` rotates + timer → `TrySpawnOre` → `OreLimitManager` throttle → `SO_AutoMinerResourceDefinition.GetOrePrefab` weighted selection |
| 3 | `SellerMachineTest.md` | Selling flow: `SellerMachine.OnTriggerEnter` → `OrePiece.SellAfterDelay` → tag MarkedForDestruction → 2s coroutine → `EconomyManager.AddMoney` → `RaiseOreSold` → `Delete` → pool return |

---

## Art & Scene Work (Non-Script)

### Particle Prefabs

| Asset | Where Used | Description |
|-------|-----------|-------------|
| `OreNodeHitParticle` | ToolPickaxe hit | Sparks at impact point |
| `BreakOreNodeParticle` | OreNode.BreakNode | Burst of particles on shatter |
| `GenericHitImpactParticle` | ToolPickaxe non-damageable hit | Small impact puff |

### Audio Clips (Phase H stubs)

| Clip | Triggered By | When |
|------|-------------|------|
| `Node_TakeDamage` | `OreNode.TakeDamage()` | Each pickaxe hit |
| `Node_Break` | `OreNode.BreakNode()` | Node shatters |
| `Ore_Crush` | `OrePiece.TryConvertToCrushed()` | Ore crushed by pickaxe |
| `Ore_Sell` | `OrePiece.SellAfterDelay()` | Ore sold at machine |

### OreNode Prefab

```
OreNode (root)
├── Model_Variant_0 (mesh, one active at random)
├── Model_Variant_1
├── Model_Variant_2
└── Collider (MeshCollider or BoxCollider)

Components on root:
  - OreNode: ResourceType, Health, MinDrops, MaxDrops, _possibleDrops, _models[]
```

### OrePiece Prefab

```
OrePiece (root)
├── MeshFilter + MeshRenderer (random mesh assigned in Start)
├── MeshCollider (sharedMesh updated to match)
└── Rigidbody (mass ~0.5, no constraints)

Components on root:
  - OrePiece: ResourceType, PieceType, BaseSellValue, CrushedPrefab, _possibleMeshes[]
  - Tag: "Grabbable", Layer: "Interact"
```

### Layers & Tags

| Name | Type | Used By |
|------|------|---------|
| `Grabbable` | Tag | OrePiece (from Phase B) |
| `MarkedForDestruction` | Tag | OrePiece during sell delay |
| `Interact` | Layer | OrePiece, OreNode |

---

## Scene Setup

### Full Phase C Scene (extends Phase B scene)

1. **ParticleManager** singleton GO — assign 3 particle prefabs
2. **OrePiecePoolManager** singleton GO — assign all OrePiece prefabs list
3. **OreLimitManager** singleton GO — `_movingObjectLimit` [SerializeField]
4. **OreManager** singleton GO — assign `_allResourceDescriptions` list
5. **PhysicsLimitUIWarning** GO on Canvas — `_softLimitObject`, `_hardLimitObject`
6. **OreNode** prefabs placed in scene — walls/floor, various ResourceTypes
7. **SellerMachine** GO with trigger collider (IsTrigger=true)
8. **AutoMiner** GO (optional) — `_resourceDefinition` SO, `Rotator` child, `OreSpawnPoint` child

---

## Modifications to Earlier Phases

| File (Phase) | How | Change | Why |
|-------------|-----|--------|-----|
| `GameEvents.cs` (A) | **partial extend** in `phase-c/0-Core/GameEvents.cs` | Add `OnOreMined`, `OnOreSold`, `OnOreLimitChanged` | No modification to Phase A's file |
| `ToolPickaxe.cs` (B) | **direct modify** | Add `IDamageable.TakeDamage` call in `PerformAttack` after raycast hit | Pickaxe now damages OreNodes |
| `ToolPickaxe.cs` (B) | **direct modify** | Add crushed ore conversion: `TryConvertToCrushed()` check | Pickaxe breaks ore into crushed |

---

## Source vs Phase Diff

| What | Original Did | What We Did | Why |
|------|-------------|-------------|-----|
| Resource descriptions | `[Serializable] ResourceDescription` on OreManager | `OreDataService` manages list, OreManager delegates | Pure C# testable via `new` |
| Weighted selection | Duplicated in OreNode, AutoMinerResourceDef, OrePiece (3 places) | `UtilsPhaseC.WeightedRandom<T>()` — single generic utility | DRY |
| OrePiece.SellAfterDelay | Calls `QuestManager.OnResourceDeposited()` directly | Fires `GameEvents.RaiseOreSold()` — QuestManager subscribes in Phase F | Decoupled |
| OreLimitManager | Reads `SettingsManager.Instance.MovingPhysicsObjectLimit` | Uses `[SerializeField] int _movingObjectLimit` | Settings is Phase H |
| PhysicsLimitUIWarning | Static `_instance` pattern + static `SwitchState()` | Subscribes to `GameEvents.OnOreLimitChanged` | Decoupled, no static instance |
| AutoMiner | Implements IInteractable + ICustomSaveDataProvider + references BuildingObject | Simplified: rotate + timer + spawn only. Phase D adds building integration | Phase scope |
| OreManager sell/volume lookups | Queries `SavingLoadingManager.AllOrePiecePrefabs` | Queries `OreDataService` | Save system is Phase G |

---

## Systems & Testability

### Individual Systems

| # | System | Scripts | Decoupled Via |
|---|--------|---------|---------------|
| 1 | **Mining** | `OreNode`, `IDamageable`, `ParticleManager` | `OnOreMined` (fires on node break) |
| 2 | **Ore Lifecycle** | `OrePiece`, `DamageableOrePiece`, `OrePiecePoolManager` | Self-registration (`AllOrePieces` list), pool return |
| 3 | **Selling** | `SellerMachine` | `OnOreSold` (fires on sell), EconomyManager.AddMoney |
| 4 | **Auto Mining** | `AutoMiner`, `SO_AutoMinerResourceDefinition` | Reads OreLimitManager for throttle |
| 5 | **Limit Management** | `OreLimitManager`, `PhysicsLimitUIWarning` | `OnOreLimitChanged` (fires on state change) |
| 6 | **Data** | `OreDataService`, `OreManager` | None — pure C# / singleton read |

### Testability Matrix

| System | `.cs` Test | `Manual/*.md` | Needs other systems? |
|--------|-----------|---------------|---------------------|
| Data (OreDataService) | `DEBUG_CheckC` | — | **Nothing** — plain C# `new` |
| Mining + Selling + Pool | `OreTest` | `MiningFlowTest.md`, `SellerMachineTest.md` | Needs ParticleManager, OrePiecePoolManager, EconomyManager |
| Auto Mining | `OreTest` | `AutoMinerVisualTest.md` | Needs OrePiecePoolManager, OreLimitManager |
| Limit + Warning | `OreTest` | — | Standalone — counts OrePiece.AllOrePieces |

**6 systems, 19 scripts, 2 `.cs` tests, 3 manual tests. Zero tight coupling between systems.**