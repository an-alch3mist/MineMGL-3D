# Phase C — Data Flow

> How data moves between scripts. Every connection is either a **GameEvent** or a `[SerializeField]`. Zero direct cross-system calls.
> Format: **conversation-style plain English** with `code references` and **bold** for key moments.

---

## System Map

```
┌─────────────────────────────────────────────────────────────┐
│                      MINING SYSTEM                           │
│                                                             │
│  OreNode (IDamageable)                                       │
│    ├── TakeDamage → BreakNode → spawn OrePieces via pool    │
│    └── Fires: RaiseOreMined                                 │
│                                                             │
│  OrePiece (BaseSellableItem)                                 │
│    ├── Self-registers in static AllOrePieces list            │
│    ├── SellAfterDelay → fires RaiseOreSold                  │
│    └── TryConvertToCrushed (ToolPickaxe integration)        │
│                                                             │
│  DamageableOrePiece (OrePiece + IDamageable)                 │
│    └── Takes collision damage → CompleteClusterBreaking      │
└─────────────────────────────────────────────────────────────┘
         │                              ▲
         │ SpawnPooledOre               │ ReturnToPool
         ▼                              │
┌─────────────────────────────────────────────────────────────┐
│                    POOL + LIMIT SYSTEM                        │
│                                                             │
│  OrePiecePoolManager (Singleton)                             │
│    ├── Dictionary<OrePieceKey, Queue<OrePiece>>             │
│    ├── SpawnPooledOre → dequeue or Instantiate              │
│    └── ReturnToPool → reset + enqueue                       │
│                                                             │
│  OreLimitManager (Singleton)                                 │
│    ├── Counts non-sleeping OrePieces every 15s              │
│    ├── Fires: RaiseOreLimitChanged                          │
│    └── GetAutoMinerSpawnTimeMultiplier (read by AutoMiner)  │
│                                                             │
│  PhysicsLimitUIWarning                                       │
│    └── Subscribes: OnOreLimitChanged → show/hide warning    │
└─────────────────────────────────────────────────────────────┘
         │
         │ Reads: ShouldBlockOreSpawning, GetAutoMinerSpawnTimeMultiplier
         ▼
┌─────────────────────────────────────────────────────────────┐
│                    AUTO MINING SYSTEM                         │
│                                                             │
│  AutoMiner                                                   │
│    ├── Rotates continuously, timer-based ore spawning       │
│    ├── Reads: OreLimitManager for throttle                  │
│    └── Uses: SO_AutoMinerResourceDefinition for weighted drops│
│                                                             │
│  SO_AutoMinerResourceDefinition                              │
│    └── SpawnProbability, SpawnRate, weighted ore prefab list │
└─────────────────────────────────────────────────────────────┘
         │
         │ OrePiece enters trigger
         ▼
┌─────────────────────────────────────────────────────────────┐
│                    SELLING SYSTEM                             │
│                                                             │
│  SellerMachine                                               │
│    ├── OnTriggerEnter → OrePiece.SellAfterDelay()           │
│    └── BaseSellableItem → direct EconomyManager.AddMoney    │
│                                                             │
│  EconomyManager (phase-All)                                  │
│    └── AddMoney → RaiseMoneyChanged                         │
└─────────────────────────────────────────────────────────────┘
         │
         │ RaiseOreSold
         ▼
┌─────────────────────────────────────────────────────────────┐
│                    DATA SYSTEM                                │
│                                                             │
│  OreManager (Singleton)                                      │
│    ├── Owns OreDataService                                  │
│    └── Round-robin cleanup of invalid ore in Update         │
│                                                             │
│  OreDataService (pure C#)                                    │
│    ├── Build(List<ResourceDescription>)                     │
│    └── GetResourceColor, GetColoredFormattedString, etc.    │
└─────────────────────────────────────────────────────────────┘
```

---

## Flow 1 — Mining an Ore Node (Pickaxe Hit)

The player **equips the Pickaxe** and **holds left-click** near an `OreNode`. `ToolPickaxe.PrimaryFireHeld()` triggers `SwingPickaxe()`, which starts a coroutine with a **0.2s delay** before raycasting.

After the delay, `PerformAttack` raycasts from the camera. The ray **hits the OreNode's collider**. The pickaxe gets `IDamageable` from the hit collider and calls `TakeDamage(damage, hitPoint)`.

`OreNode.TakeDamage()` subtracts damage from `_health`. If health is still above zero, *nothing else happens* (a sound stub plays in Phase H). If health **reaches zero**, `BreakNode(hitPosition)` fires.

Inside `BreakNode`: it picks a random drop count between `_minDrops` and `_maxDrops` (e.g. 2-4). For each drop, it calls `UtilsPhaseC.WeightedRandom()` on `_possibleDrops` to pick an `OrePiece` prefab, then spawns it via `Singleton<OrePiecePoolManager>.Ins.SpawnPooledOre(prefab, pos, rotation)`. Each spawned piece gets **random velocity** (upward + lateral spread) and **random angular velocity** — they fly out and bounce.

`ParticleManager.CreateParticle()` spawns a **break particle burst** at the hit point. Then `GameEvents.RaiseOreMined(resourceType, position)` fires — *Phase F's quest system will subscribe to this*.

Finally, `Destroy(gameObject)` removes the node permanently.

---

## Flow 2 — Ore Piece Lifecycle (Spawn → Pool → Sell)

When `OrePiecePoolManager.SpawnPooledOre()` is called, it looks up the `OrePieceKey` (ResourceType + PieceType + IsPolished) in its dictionary of queues. If a **recycled piece exists** in the queue, it dequeues it, repositions it, and `SetActive(true)`. If the queue is empty, it **Instantiates** a new piece from the registered prefab.

On `SetActive(true)`, `OrePiece.OnEnable()` fires and **adds itself** to the static `AllOrePieces` list. `OrePiece.Start()` picks a **random mesh** from `_possibleMeshes`, applies it to `MeshFilter` and `MeshCollider`, applies **random scale variance**, and sets a `randomPriceMultiplier` between 0.9 and 1.1.

The piece **lives in the world** as a physics object — it can be grabbed (Phase B SpringJoint), pulled by magnet (Phase B ToolMagnet), or pushed onto conveyors (Phase D).

When the piece enters a `SellerMachine` trigger, `OnTriggerEnter` calls `orePiece.SellAfterDelay(2f)`. This **detaches from magnet** if held, calls `SetTag(TagType.MarkedForDestruction)` (so other triggers skip it via `HasTag` check), then starts a **2-second coroutine**. After the delay, `EconomyManager.AddMoney(sellValue)` is called and `GameEvents.RaiseOreSold(price, type, pieceType)` fires.

`OrePiece.Delete()` calls `OrePiecePoolManager.ReturnToPool(this)`. The pool **deactivates** the piece, resets velocity/drag/rotation, clears baskets and magnet references, re-tags via `SetTag(TagType.Grabbable)`, and **enqueues** it back into the pool. `OnDisable()` removes it from `AllOrePieces`.

---

## Flow 3 — Ore Limit Throttling

`OreLimitManager.Update()` runs a check **every 15 seconds**. It counts how many `OrePiece` in `AllOrePieces` have **non-sleeping Rigidbodies** (actively moving).

If the count exceeds `_movingObjectLimit + 200` → state becomes `Blocked`. Between `+100` and `+200` → `HighlyLimited`. Between limit and `+100` → `SlightlyLimited`. Below limit → `Regular`.

When the state **changes**, `GameEvents.RaiseOreLimitChanged(state)` fires. `PhysicsLimitUIWarning` is subscribed — it **shows/hides** the soft limit or hard limit warning text on the UI.

`AutoMiner` reads `OreLimitManager.GetAutoMinerSpawnTimeMultiplier()` each spawn cycle — at `SlightlyLimited` the spawn interval is **25% longer**, at `Blocked` it's **doubled**. If `ShouldBlockOreSpawning()` returns true, `AutoMiner.TrySpawnOre()` **skips entirely**.

---

## Flow 4 — Auto Miner Spawning

`AutoMiner.Update()` runs every frame when `_enabled` is true. It rotates the `_rotator` child at a speed based on `_spawnRate * _oresPerRotation`. A timer counts down — when it hits zero, `TrySpawnOre()` fires.

Inside `TrySpawnOre`: it checks `OreLimitManager.ShouldBlockOreSpawning()` — if blocked, *returns immediately*. Then a random roll against `_spawnProbability` (default 80%) — if it fails, *no spawn*. If both pass, it calls `UtilsPhaseC.PickOrePrefab(_resourceDefinition.PossibleOrePrefabs, true)` which reads the SO's public field list and uses `WeightedRandom` to pick a prefab, then `OrePiecePoolManager.SpawnPooledOre(prefab, spawnPoint)`. The SO itself has **zero methods** — AutoMiner reads its fields, UtilsPhaseC does the selection logic.

The timer resets to `_spawnRate * OreLimitManager.GetAutoMinerSpawnTimeMultiplier()` — **throttled** during high load.

---

## Flow 5 — OreManager Cleanup

`OreManager.Update()` checks **one ore piece per frame** via round-robin index. If the piece is `null` (destroyed externally), it removes it from `AllOrePieces`. If the piece's position is below `y = -1000` (fell out of world), it calls `piece.Delete()` — returning it to the pool.

This prevents leaked ore pieces from accumulating forever.

---

## Event Registry — Phase C

| Event | Fired By | Subscribed By |
|-------|----------|---------------|
| `OnOreMined(ResourceType, Vector3)` | `OreNode.BreakNode()` | Phase F: QuestManager (mining quests), tests |
| `OnOreSold(float, ResourceType, PieceType)` | `OrePiece.DelayThenSell()`, `SellerMachine` | Phase F: QuestManager (resource deposits), tests |
| `OnOreLimitChanged(OreLimitState)` | `OreLimitManager.SetState()` | `PhysicsLimitUIWarning` (show/hide), tests |
| `OnMoneyChanged(float)` | `EconomyManager.AddMoney()` (phase-All) | MoneyHUD (Phase A), InventoryOrchestrator (Phase B), tests |