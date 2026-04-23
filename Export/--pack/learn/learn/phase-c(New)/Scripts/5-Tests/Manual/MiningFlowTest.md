# Mining Flow — Manual Test

> Verifies: hit node → particles → health decrease → shatter → ore pieces fly + bounce + settle.

---

## Prerequisites

- `OreTest.cs` script on a GO in scene (provides Space key to damage node)
- `EconomyManager` singleton (from phase-All)

---

## Setup Guide — Step by Step

### Step 1 — Singletons

Create these Empty GOs in scene root. Each needs its singleton component:

| GO Name | Component | Notes |
|---------|-----------|-------|
| `ParticleManager` | `ParticleManager` | Assign: `GenericHitImpactParticle`, `OreNodeHitParticlePrefab`, `BreakOreNodeParticlePrefab` (create 3 particle system prefabs or use placeholders) |
| `OrePiecePoolManager` | `OrePiecePoolManager` | Assign: `_allOrePiecePrefabs` → list of all OrePiece prefabs (see Step 3) |
| `OreLimitManager` | `OreLimitManager` | `_movingObjectLimit` = 500 (default) |
| `OreManager` | `OreManager` | Assign: `_allResourceDescriptions` → list of ResourceDescription entries (see Step 2) |
| `EconomyManager` | `EconomyManager` | `_defaultMoney` = 400 (from phase-All) |

### Step 2 — Resource Descriptions

On `OreManager` → `_allResourceDescriptions` list, add entries:

| Index | ResourceType | DisplayColor |
|-------|-------------|-------------|
| 0 | Iron | Grey (0.6, 0.6, 0.6) |
| 1 | Gold | Yellow (1, 0.84, 0) |
| 2 | Coal | Dark Grey (0.2, 0.2, 0.2) |
| 3 | Copper | Orange (0.85, 0.5, 0.2) |

### Step 3 — OrePiece Prefab

Create a prefab:

```
OrePiece_Iron (root GO)
│
│  Components on root:
│   - Rigidbody (mass 0.5, no constraints, gravity ON)
│   - OrePiece component:
│       _resourceType = Iron
│       _pieceType = Ore
│       _isPolished = false
│       BaseSellValue = 1.0
│       _useRandomMesh = true
│       _useRandomScale = true
│       _scaleVariance = (0.25, 0.25, 0.25)
│       _possibleMeshes = [assign 2-3 mesh variants]
│       _meshFilter = this GO's MeshFilter
│       _meshCollider = this GO's MeshCollider (optional)
│   - MeshFilter
│   - MeshRenderer (assign a material matching Iron color)
│   - MeshCollider (convex = true) OR BoxCollider
│   - Tag: "Grabbable"
│   - Layer: "Interact"
```

Save as prefab → add to `OrePiecePoolManager._allOrePiecePrefabs` list.

Repeat for Gold, Coal, Copper with different materials/colors.

### Step 4 — WeightedNodeDrop Setup

For each OrePiece prefab, you'll reference it in OreNode's `_possibleDrops` list (Step 5).

### Step 5 — OreNode Prefab

Create a prefab:

```
OreNode_Iron (root GO)
│
│  Components on root:
│   - OreNode component:
│       _resourceType = Iron
│       _health = 100
│       _minDrops = 2
│       _maxDrops = 4
│       _possibleDrops = [
│           { OrePrefab = OrePiece_Iron, Weight = 100 }
│       ]
│       _models = [Model_0, Model_1, Model_2]
│   - BoxCollider or MeshCollider (for pickaxe raycast to hit)
│   - Layer: "Interact"
│
├── Model_0 (MeshFilter + MeshRenderer, rock mesh variant 1)
├── Model_1 (different rock mesh variant)
└── Model_2 (different rock mesh variant)
```

Place 2-3 OreNode prefabs in the scene (on floor or walls).

### Step 6 — OreTest Script

1. Create Empty GO → name `OreTest`
2. Add `OreTest` component
3. Wire:

| Field | Drag From |
|-------|-----------|
| `_testNode` | One of the OreNode instances in scene |
| `_testOrePrefab` | OrePiece_Iron prefab (from Project) |
| `_cam` | Camera (for U key spawn position) |

### Step 7 — Floor

- Plane at y=0, layer "Default" — ore pieces land here

### Final Scene Hierarchy

```
Scene Root
├── EconomyManager
├── ParticleManager
├── OrePiecePoolManager
├── OreLimitManager
├── OreManager
├── OreTest
├── Floor (Plane)
├── OreNode_Iron_01 (placed on floor)
├── OreNode_Iron_02
└── OreNode_Gold_01
```

---

## How It Works (System Flow)

**Scene loads:** `OreNode.Start()` picks a random index from `_models[]` and calls `SetActive(true)` on that one, `SetActive(false)` on all others — each node shows a random rock variant. `OrePiecePoolManager.Awake()` builds a `Dictionary<OrePieceKey, OrePiece>` from `_allOrePiecePrefabs` — mapping each (ResourceType, PieceType, IsPolished) to its prefab for O(1) lookup.

**Taking damage:** When `OreNode.TakeDamage(damage, position)` is called (by ToolPickaxe or OreTest), it subtracts `damage` from `_health`. If health > 0 → nothing else happens (Phase H would play a hit sound here). If health ≤ 0 → `BreakNode(hitPosition)` fires.

**Breaking:** `BreakNode` picks a random drop count between `_minDrops` and `_maxDrops`. For each drop, `UtilsPhaseC.WeightedRandom(_possibleDrops, d => d.Weight)` selects an `OrePiece` prefab based on weights. Then `Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(prefab, pos, rotation)` either **dequeues a recycled piece** from the pool or **Instantiates a new one**. Each spawned piece gets random velocity (upward + lateral) and random angular velocity — they **fly out and tumble**. `ParticleManager.CreateParticle(BreakOreNodeParticlePrefab, hitPosition)` spawns a burst particle. `GameEvents.RaiseOreMined(resourceType, position)` fires for quest tracking. Finally `Destroy(gameObject)` removes the node permanently.

**OrePiece lifecycle:** On `SetActive(true)` (from pool or fresh), `OrePiece.OnEnable()` adds itself to the static `AllOrePieces` list. `Start()` picks a random mesh from `_possibleMeshes`, applies random scale variance, and sets `randomPriceMultiplier` (0.9–1.1). The piece is now a live physics object — grabbable, pullable by magnet, pushable onto conveyors.

**Pool return:** When `OrePiece.Delete()` is called (by SellerMachine or OreManager cleanup), `OrePiecePoolManager.ReturnToPool(piece)` **deactivates** it (`SetActive(false)`), resets velocity/drag/rotation, clears all state, re-tags as "Grabbable", parents under the pool root, and **enqueues** it. `OnDisable` removes it from `AllOrePieces`.

---

## 1. Initial State

**DO:** Press Play
**EXPECT:**
- Each OreNode shows **one random model variant** (the others are hidden — `_models[i].SetActive(i == chosen)`)
- No ore pieces on ground
- No particles visible
- Console: no errors

**Behind the scenes:** `OreNode.Start()` randomized model variants. `OrePiecePoolManager.Awake()` built the prefab lookup dictionary. `OreManager.Awake()` called `oreDataService.Build(_allResourceDescriptions)`. `OreLimitManager` timer is at 0 — first check in 15 seconds.

---

## 2. First Hit (Health Reduced)

**DO:** Press `Space` (OreTest calls `_testNode.TakeDamage(50, position)`)
**EXPECT:**
- OreNode **still exists** — health was 100, now 50
- **No ore pieces** yet — node didn't break
- Console: no OnOreMined event (node not destroyed)

---

## 3. Breaking Hit (Health Reaches Zero)

**DO:** Press `Space` again (another 50 damage → health = 0)
**EXPECT:**
- OreNode **disappears** from scene (Destroyed)
- **2-4 ore pieces fly out** from a point between node center and hit position
  - Each piece has **random velocity**: lateral ±1.5, upward 2-4
  - Each piece has **random angular velocity** (tumbles)
- Pieces **bounce and roll** on the floor (Rigidbody physics)
- **Break particle burst** appears at hit point (BreakOreNodeParticlePrefab)
- Console: `[OreTest] Mined: Iron at (x, y, z)`

---

## 4. Inspect Ore Piece

**DO:** Pause the game → select an ore piece in Scene view → check Inspector
**EXPECT:**
- Tag: `"Grabbable"`
- Layer: `"Interact"`
- `OrePiece` component:
  - `ResourceType` = Iron (matches the broken node)
  - `PieceType` = Ore
  - `BaseSellValue` = non-zero (e.g. 1.0)
  - `RandomPriceMultiplier` = between 0.9 and 1.1
- MeshFilter: one of the `_possibleMeshes` variants (random)
- Transform.localScale: slightly varied from original (random scale)

---

## 5. Spawn Ore Directly

**DO:** Unpause → press `U` (OreTest spawns ore at camera forward via pool)
**EXPECT:**
- New ore piece **appears** 2m in front of camera
- Falls via gravity, bounces on floor
- Console: no event (direct spawn doesn't fire OnOreMined)

---

## 6. Check Ore Count

**DO:** Press `I`
**EXPECT:**
- Console: `OrePiece count: X` where X = number of active ore pieces in scene

---

## 7. Multiple Node Types

**DO:** Assign `_testNode` to a Gold OreNode (or add a second OreTest with different node). Break it.
**EXPECT:**
- Gold ore pieces fly out (different material/color from Iron)
- Console: `[OreTest] Mined: Gold at (x, y, z)`
- No Iron pieces from a Gold node — drops match node's `_possibleDrops`

---

## 8. Edge Case — Empty Drops List

**DO:** Create an OreNode with `_possibleDrops` = empty list. Break it.
**EXPECT:**
- Node breaks (disappears + particles)
- **Zero ore pieces** spawn — `GetOrePrefab()` returns null, loop skips
- No crash, no console errors

---

## Summary Checklist

- [ ] OreNode shows one random model variant on Start
- [ ] First hit (50 damage) reduces health but doesn't break
- [ ] Second hit (0 health) → node destroyed
- [ ] 2-4 ore pieces fly out with random velocity + angular velocity
- [ ] Pieces have random mesh variant from `_possibleMeshes`
- [ ] Pieces have slight random scale variation
- [ ] Break particle burst at hit point
- [ ] Console: `[OreTest] Mined: Iron at (x,y,z)`
- [ ] U key spawns ore via pool (no event)
- [ ] I key logs current ore count
- [ ] Different node types produce matching ore types
- [ ] Empty drops list → no crash, zero pieces
- [ ] All ore pieces tagged "Grabbable", layer "Interact"