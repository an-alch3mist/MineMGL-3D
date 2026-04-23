# Auto Miner Visual — Manual Test

> Verifies: rotator spins, ore spawns on timer, probability-based, throttled by limit.

---

## Prerequisites

- All singletons from MiningFlowTest setup (ParticleManager, OrePiecePoolManager, OreLimitManager, OreManager, EconomyManager)
- OrePiece prefabs registered in pool

---

## Setup Guide

### Step 1 — SO_AutoMinerResourceDefinition Asset

1. In Project panel: Create → SO → SO_AutoMinerResourceDefinition
2. Name it `AutoMiner_IronDef`
3. Set:
   - `SpawnProbability` = 80
   - `SpawnRate` = 2
   - `_possibleOrePrefabs` list:
     - [0] OrePrefab = `OrePiece_Iron`, Weight = 80
     - [1] OrePrefab = `OrePiece_Gold`, Weight = 20

### Step 2 — AutoMiner Prefab

```
AutoMiner (root GO)
│
│  Components on root:
│   - AutoMiner component:
│       _rotator → Rotator child
│       _oreSpawnPoint → SpawnPoint child
│       _rotateY = true (or _rotateZ = true)
│       _enabled = true
│       _oresPerRotation = 12
│       _resourceDefinition → AutoMiner_IronDef (from Step 1)
│       _spawnProbability = (auto-set from definition in Start)
│       _spawnRate = (auto-set from definition in Start)
│       _fallbackOrePrefab → OrePiece_Iron (fallback if def is null)
│
├── Rotator (visual mesh — this spins)
│   └── drill_mesh (MeshFilter + MeshRenderer)
│
└── SpawnPoint (empty GO positioned below the drill tip)
    - This is where ore appears
```

Place in scene.

### Step 3 — OreTest Script

Use existing OreTest GO from MiningFlowTest. Assign `_cam` for U key spawns.

### Final Hierarchy (add to MiningFlowTest scene)

```
Scene Root
├── ... (singletons from MiningFlowTest)
├── AutoMiner (placed on floor near OreNodes)
├── OreTest
└── Floor
```

---

## How It Works (System Flow)

**Initialization:** `AutoMiner.Start()` sets `timeUntilNextSpawn = _spawnRate`, picks `rotationAxis` based on `_rotateY`/`_rotateZ` flags. If `_resourceDefinition` is assigned, it copies `SpawnProbability` and `SpawnRate` from the SO.

**Every frame:** `AutoMiner.Update()` runs when `_enabled = true`. It rotates `_rotator` child at `360° / (SpawnRate × OresPerRotation)` degrees per second — visually the drill spins. A timer counts down. When it reaches 0 → `TrySpawnOre()` fires.

**Spawn logic:** `TrySpawnOre()` first checks `Singleton<OreLimitManager>.Ins.ShouldBlockOreSpawning()` — if `Blocked`, returns immediately (no spawn). Then a random roll: `Random.Range(0, 100) <= SpawnProbability` — at 80%, roughly 1 in 5 cycles produces nothing. If both checks pass, `_resourceDefinition.GetOrePrefab(CanProduceGems)` uses `UtilsPhaseC.WeightedRandom()` to pick a prefab from the weighted list, then `OrePiecePoolManager.SpawnPooledOre(prefab, spawnPoint)` creates or recycles an ore piece.

**Timer reset:** After spawn attempt, timer resets to `_spawnRate * OreLimitManager.GetAutoMinerSpawnTimeMultiplier()`. The multiplier is 1.0 at Regular, 1.25 at SlightlyLimited, 1.5 at HighlyLimited, 2.0 at Blocked — effectively **throttling** spawn rate under load.

**Limit detection:** `OreLimitManager.Update()` checks every 15 seconds — counts non-sleeping `Rigidbody` in `OrePiece.AllOrePieces`. Exceeding thresholds changes `OreLimitState` → fires `GameEvents.RaiseOreLimitChanged()` → `PhysicsLimitUIWarning` shows/hides warning text.

---

## 1. Initial State

**DO:** Press Play, look at AutoMiner
**EXPECT:**
- Rotator child is **visible** (drill mesh)
- No ore pieces near miner yet
- No console errors

---

## 2. Rotator Spinning

**DO:** Watch the Rotator for a few seconds
**EXPECT:**
- Rotator **spins continuously** around the configured axis (_rotateY → spins around Y)
- Speed: `360° / (SpawnRate × OresPerRotation)` per second = `360° / (2 × 12)` = **15°/s**
- Smooth rotation, no jitter

---

## 3. First Ore Spawn

**DO:** Wait ~2 seconds (SpawnRate = 2)
**EXPECT:**
- **Ore piece appears** at SpawnPoint position
- Falls via gravity onto floor
- Piece is from the weighted drop list (likely Iron at 80% chance)

---

## 4. Continuous Spawning

**DO:** Watch for 20 seconds (~10 spawn cycles)
**EXPECT:**
- New ore piece every ~2 seconds
- **Not every cycle spawns** — ~80% spawn, ~20% skip (probability check)
- Over 10 cycles: expect roughly 7-9 pieces (random)

---

## 5. Weighted Drop Verification

**DO:** Inspect spawned pieces over 20+ spawns → count Iron vs Gold
**EXPECT:**
- **~80% Iron**, **~20% Gold** (matches weights 80:20)
- Some variance is normal — weighted random, not exact ratio

---

## 6. Limit Throttling

**DO:** Spawn many extra ore pieces via `U` key (50+ pieces) until OreLimitManager fires
**EXPECT:**
- Console: `[OreTest] LimitState: SlightlyLimited`
- AutoMiner spawn interval **increases** — visually, ore appears *less frequently*
- Rotator still spins at same speed (rotation is independent of spawn timer)

**DO:** Keep spawning until Blocked state
**EXPECT:**
- Console: `[OreTest] LimitState: Blocked`
- AutoMiner **stops spawning entirely** — no new ore appears
- Rotator still spins (visual only)

---

## 7. Disabled State

**DO:** Set `_enabled = false` on AutoMiner in Inspector during Play mode
**EXPECT:**
- Rotator **stops spinning**
- No ore spawns
- No errors

**DO:** Set `_enabled = true` again
**EXPECT:**
- Rotator **resumes spinning**
- Ore spawning resumes on next timer tick

---

## 8. Edge Case — No Resource Definition

**DO:** Create an AutoMiner with `_resourceDefinition` = None, `_fallbackOrePrefab` = OrePiece_Iron
**EXPECT:**
- Miner uses `_fallbackOrePrefab` instead of weighted selection
- All spawned pieces are Iron (no weighted variety)

---

## Summary Checklist

- [ ] Rotator spins at correct speed on correct axis
- [ ] Ore spawns at SpawnPoint every ~SpawnRate seconds
- [ ] ~80% spawn probability — not every cycle produces ore
- [ ] Weighted drops: ~80% Iron, ~20% Gold (matches SO definition)
- [ ] OreLimitManager SlightlyLimited → slower spawn rate
- [ ] OreLimitManager Blocked → zero spawning
- [ ] Disabled → no spin, no spawn; re-enabled → resumes
- [ ] No definition → uses fallback prefab
- [ ] No console errors throughout