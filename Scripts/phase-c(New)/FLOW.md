# Phase C — Data Flow & Event Architecture

## Portability Diagram

```
═══════════════════════════════════════════════════════════════════════════
  FREE — these DO NOT count as dependencies (shared infra, always available)
═══════════════════════════════════════════════════════════════════════════
  GameEvents (partial)    — static event bus, every system fires/subscribes
  Singleton<T>            — base class for managers
  Singleton<UIManager>    — the ONLY singleton read allowed for L0
  GlobalEnumsAll/C        — shared enums (TagType, ResourceType, PieceType)
  SPACE_UTIL / INPUT.K    — user utility extensions
  UtilsPhaseAll/C         — shared static helpers
  BaseSellableItem etc.   — phase-b 3-MonoBehaviours/Physics/ (shared infra)
  1-Managers/             — ParticleManager lives here (shared, not _-Systems/)

  → Using ANY of these = still L0. They're project infrastructure.
═══════════════════════════════════════════════════════════════════════════


═══════════════════════════════════════════════════════════════════════════
  L0 — TRULY PORTABLE (zero deps on other _-Systems/ folders)
  Copy this folder to ANY Unity project → it compiles and works.
═══════════════════════════════════════════════════════════════════════════

  ┌─────────────────────────────────────────────────────────────┐
  │  MiningSystem/                              L0 ✅ PORTABLE │
  │                                                             │
  │  Owns: IDamageable (interface)                              │
  │  Scripts: OreNode, WeightedNodeDrop                         │
  │  Deps on other _-Systems/: NONE                             │
  │  Communicates via: GameEvents only                          │
  │    fires → OnSpawnOreRequested                              │
  │    fires → OnCreateParticleRequested                        │
  │    fires → OnOreNodeBroken, OnOreMined                      │
  │    fires → OnStaticBreakableBroken                          │
  │                                                             │
  │  Copy test: ✅ drop in empty project + subscribe events     │
  └─────────────────────────────────────────────────────────────┘


  ┌─────────────────────────────────────────────────────────────┐
  │  OreSystem/                                 L0 ✅ PORTABLE │
  │                                                             │
  │  Scripts: OrePiece, DamageableOrePiece, OreDataService,     │
  │    OrePiecePoolManager, OreManager, OreLimitManager,        │
  │    PhysicsLimitUIWarning, BaseBasket, OreSled,              │
  │    SO_AutoMinerResourceDefinition + Ext, entities           │
  │  Deps on other _-Systems/: NONE                             │
  │    (IDamageable is in phase-b DamageSystem — FREE infra)    │
  │  Communicates via: GameEvents                               │
  │    subscribes ← OnSpawnOreRequested (spawns ore for nodes)  │
  │    subscribes ← OnOreLimitChanged (UI warning)              │
  │    fires → OnOreSold                                        │
  │    fires → OnOreLimitChanged                                │
  │                                                             │
  │  Copy test: ✅ drop in empty project → works                │
  └─────────────────────────────────────────────────────────────┘


═══════════════════════════════════════════════════════════════════════════
  L1+ — external _-Systems/ dependencies (GAME-SPECIFIC)
  NOT portable — tied to mineMGL. Brings concrete classes.
═══════════════════════════════════════════════════════════════════════════

  ┌─────────────────────────────────────────────────────────────┐
  │  AutoMinerSystem/                           L2 (2 deps)    │
  │                                                             │
  │  Scripts: AutoMiner, AutoMinerSaveData                      │
  │                                                             │
  │  Dep #1: OreSystem ─── concrete                             │
  │    └─ Singleton<OrePiecePoolManager>.Ins (spawn ore)        │
  │    └─ Singleton<OreLimitManager>.Ins (throttle check)       │
  │    └─ OrePiece class (fallback prefab type)                 │
  │    └─ SO_AutoMinerResourceDefinition + Ext                  │
  │                                                             │
  │  Dep #2: InteractionSystem ─── interface                    │
  │    └─ implements IInteractable (turn on/off)                │
  │                                                             │
  │  Cannot copy without: OreSystem + IInteractable.cs          │
  └─────────┬───────────────────────┬───────────────────────────┘
            │ concrete               │ interface
            ▼                        ▼
       OreSystem               IInteractable
    (OrePiecePoolMgr,        (InteractionSystem)
     OreLimitMgr,
     OrePiece)

  ┌─────────────────────────────────────────────────────────────┐
  │  SellerSystem/                              L1 (1 dep)     │
  │                                                             │
  │  Scripts: SellerMachine                                     │
  │                                                             │
  │  Dep #1: OreSystem ─── concrete                             │
  │    └─ OrePiece class (GetComponentInParent<OrePiece>)       │
  │    └─ BaseSellableItem (shared infra — FREE, doesn't count)│
  │                                                             │
  │  Communicates via: GameEvents                               │
  │    fires → OnOreSold                                        │
  │                                                             │
  │  Cannot copy without: OrePiece.cs from OreSystem            │
  └─────────┬───────────────────────────────────────────────────┘
            │ concrete
            ▼
       OreSystem
       (OrePiece)


═══════════════════════════════════════════════════════════════════════════
  PHASE C PORTABILITY SCORECARD
═══════════════════════════════════════════════════════════════════════════

  System             │ Level │ External Deps                   │ Portable?
  ───────────────────┼───────┼─────────────────────────────────┼──────────
  MiningSystem       │  L0   │ none                            │ ✅ YES
  OreSystem          │  L0   │ none (IDamageable is FREE)      │ ✅ YES
  SellerSystem       │  L1   │ OrePiece (concrete, 1 system)   │ ❌ NO
  AutoMinerSystem    │  L2   │ OreSystem + IInteractable       │ ❌ NO

  Portable: 2/4 systems (MiningSystem + OreSystem)
  Game-specific: 2/4 (AutoMinerSystem + SellerSystem)

  Note: IDamageable now lives in phase-b DamageSystem (FREE infra).
        OreSystem has zero _-Systems/ deps → L0.
        Concrete dep = must bring entire system + its deps.
```

## Data Flows

### Mining a Node (Player with Pickaxe)

The player equips the pickaxe from Phase B's inventory, holds left-click. After the swing delay, `ToolPickaxe` raycasts from the camera. If the ray hits a collider whose parent has `IDamageable`, it calls `TakeDamage(damage, hitPoint)`.

**Inside OreNode.TakeDamage:** `health -= damage`. If health is still above zero, nothing else happens — the node took a hit but survived. The player sees sparks (Phase H will add hit particles). When health finally reaches zero, `BreakNode(hitPosition)` fires.

**BreakNode** is the big moment. First it picks `Random.Range(_minDrops, _maxDrops + 1)` — say 3 drops. For each drop, `UtilsPhaseC.WeightedRandom(_possibleDrops, d => d.Weight)` picks a prefab from the weighted list. Then `GameEvents.RaiseSpawnOreRequested(prefab, position, rotation, velocity, angularVelocity)` fires. The velocity is randomized: upward (2–4) + lateral spread (-1.5 to 1.5). Angular velocity is a random unit sphere direction × random magnitude (1–50).

**OrePiecePoolManager** subscribes to `OnSpawnOreRequested`. It receives the prefab GameObject, extracts `OrePiece` via `GetComponent`, looks up the pool key (`ResourceType + PieceType + IsPolished`), checks if there's a recycled piece in the queue. First time: **Instantiate**. Later: **dequeue**. Sets position, rotation, `SetActive(true)`, applies velocity and angular velocity. **Ore pieces fly out from the node, tumbling with physics.**

Then OreNode fires `RaiseCreateParticleRequested` — **ParticleManager** subscribes and Instantiates the break particle prefab at the hit point. A burst of particles plays.

Then `RaiseOreNodeBroken(position, resourceType)` fires for Phase D (update building supports above) and other future subscribers. `RaiseOreMined` fires for Phase F quest tracking. `RaiseStaticBreakableBroken` fires for Phase G save. Finally, `Destroy(gameObject)` — the node is gone.

### Selling Ore

Ore pieces on the ground can be pushed into a `SellerMachine` trigger volume. When an `OrePiece`'s Rigidbody enters the trigger, `SellerMachine.OnTriggerEnter` fires.

**Inside OnTriggerEnter:** First guard: `HasTag(markedForDestruction)` → skip (already being sold). Second guard: `attachedRigidbody == null` → skip. Then `GetComponentInParent<OrePiece>()` → found → calls `orePiece.SellAfterDelay(2f)`.

**Inside SellAfterDelay:** Detaches from magnet if held. Tags the entire GO hierarchy as `markedForDestruction` — **this prevents double-sell from multiple triggers**. Starts a coroutine that waits 2 seconds.

**After 2 seconds:** `GetSellValue()` computes `BaseSellValue × RandomPriceMultiplier` (each piece has a 0.9x–1.1x multiplier set on Start). `GameEvents.RaiseOreSold(sellValue, resourceType, pieceType, polishedPercent)` fires — **EconomyManager** (Phase A) subscribes to add money, **QuestManager** (Phase F) subscribes to track deposit progress. Then `Delete()` → `ReturnToPool` → piece deactivates, physics reset, re-tagged as Grabbable, enqueued for reuse. **The ore disappears from the world, money increases.**

### AutoMiner Spawning

AutoMiner sits at a mining node and spawns ore automatically. Each frame in `Update`: rotates the drill visual, decrements `timeUntilNextSpawn`. When it hits zero:

**TrySpawnOre:** Checks `OreLimitManager.ShouldBlockOreSpawning()` — if the physics limit is `blocked`, skip entirely. Rolls probability: `Random.Range(0, 100) <= SpawnProbability` (default 80%). If passes: `_resourceDefinition.GetOrePrefab(canProduceGems)` does a weighted random pick from the SO's ore list. `OrePiecePoolManager.SpawnPooledOre(prefab, spawnPoint.position)` spawns the ore. Timer resets with `SpawnRate × OreLimitManager.GetAutoMinerSpawnTimeMultiplier()` (1.0x normally, up to 2.0x when limited). **Ore appears at the spawn point and falls.**

### Physics Limit Warning

Every 15 seconds, `OreLimitManager.Update` counts non-sleeping `Rigidbody` entries in `OrePiece.AllOrePieces`. Based on count vs threshold: sets `OreLimitState` and fires `RaiseOreLimitChanged(state)`. **PhysicsLimitUIWarning** subscribes and switches panel visibility — `regular` = hidden, `slightlyLimited`/`highlyLimited` = soft warning (yellow), `blocked` = hard warning (red).

### Pool Lifecycle

`OrePiece.OnEnable` → add to `AllOrePieces`. `OrePiece.OnDisable` → remove from `AllOrePieces`. `ReturnToPool` → deactivate, zero physics, clear runtime state (baskets, sieve%, magnet ref, conveyor list), re-tag as Grabbable, parent under pool root, enqueue. Next `SpawnPooledOre` call with same key → dequeue instead of Instantiate. **Zero GC pressure after warmup.**

## Event Registry

| Event | Who Fires | Who Subscribes | Data |
|-------|-----------|---------------|------|
| `OnSpawnOreRequested` | OreNode.BreakNode | OrePiecePoolManager | `(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)` |
| `OnCreateParticleRequested` | OreNode.BreakNode | ParticleManager | `(GameObject prefab, Vector3 pos, Quaternion rot)` |
| `OnOreNodeBroken` | OreNode.BreakNode | Phase D (supports), Phase G (save) | `(Vector3 pos, ResourceType type)` |
| `OnOreMined` | OreNode.BreakNode | Phase F (quests) | `(ResourceType type, Vector3 pos)` |
| `OnStaticBreakableBroken` | OreNode.BreakNode | Phase G (save) | `(Vector3 truncatedPos)` |
| `OnOreSold` | OrePiece.DelayThenSell, SellerMachine | EconomyManager (Phase A), QuestManager (Phase F) | `(float value, ResourceType, PieceType, float polished%)` |
| `OnOreLimitChanged` | OreLimitManager.Update | PhysicsLimitUIWarning | `(OreLimitState state)` |

Every connection is a GameEvent. No direct cross-system method calls. MiningSystem → OreSystem communication is purely via events. AutoMinerSystem and SellerSystem are L2 (game-specific) — they call OrePiecePoolManager and OreLimitManager singletons directly.