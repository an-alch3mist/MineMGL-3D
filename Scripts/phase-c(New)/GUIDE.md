# Phase C — Mining & Ore System — GUIDE

## What It Looks Like When Running

You walk into a mine tunnel from the starting room. Glowing ore nodes are embedded in the walls and floor — different colors for each resource type (grey for Iron, yellow for Gold, orange for Copper, black for Coal).

Equip the pickaxe from your hotbar, hold left-click. The pickaxe swings, and after a short delay a raycast hits the node. Sparks fly from the impact point. The node takes damage. After 2-3 hits, the node shatters — 2 to 4 ore pieces fly out with random velocity, bouncing and rolling on the ground with physics. A burst of break particles plays at the impact point. The node disappears permanently.

The ore pieces on the ground are physical objects you can grab with the hand (right-click SpringJoint from Phase B) or pull with the magnet tool. Each piece has a random mesh variant and slight scale variation, plus a random price multiplier (0.9x-1.1x) so each piece is worth slightly different.

An AutoMiner machine placed at a node rotates continuously and spawns ore on a timer. 80% probability each cycle, configurable spawn rate. When too many physics objects are active (500+ moving), OreLimitManager kicks in — slows auto-miner rate, eventually blocks spawning entirely. A UI warning appears.

Push ore into a SellerMachine trigger volume — ore sits for 2 seconds, then money increases and the ore disappears (returned to the pool for reuse). OrePiecePoolManager recycles all ore — zero Instantiate/Destroy after the initial warmup. Smooth performance.

---

## Folder Structure

```
phase-c(New)/
├── Scripts/
│   ├── 0-Core/
│   │   └── GameEvents.cs                    (partial — phase-c events)
│   │
│   ├── _-Systems/
│   │   ├── MiningSystem/                    ← L0 PORTABLE (owns IDamageable)
│   │   │   ├── IDamageable.cs               (interface — generic, no ore dep)
│   │   │   ├── OreNode.cs                   (breakable node, fires events)
│   │   │   ├── WeightedNodeDrop.cs          (entity: GameObject + Weight)
│   │   │   └── MiningTest.md
│   │   │
│   │   ├── OreSystem/                       ← L0 PORTABLE
│   │   │   ├── OrePiece.cs                  (physical resource object)
│   │   │   ├── DamageableOrePiece.cs        (extends OrePiece + IDamageable)
│   │   │   ├── OreDataService.cs            (pure C# — colors, formatted names)
│   │   │   ├── OrePiecePoolManager.cs       (Singleton — recycles ore)
│   │   │   ├── OreManager.cs               (Singleton — owns DataService + cleanup)
│   │   │   ├── OreLimitManager.cs           (Singleton — throttles spawning)
│   │   │   ├── PhysicsLimitUIWarning.cs     (UI panel for limit warning)
│   │   │   ├── SO_AutoMinerResourceDefinition.cs (pure data SO)
│   │   │   ├── WeightedOreChance.cs         (entity: OrePiece + Weight)
│   │   │   ├── ResourceDescription.cs       (entity: ResourceType + Color)
│   │   │   ├── OrePieceKey.cs               (composite key for pool)
│   │   │   ├── OrePieceEntry.cs             (save data entity — Phase G)
│   │   │   └── OreTest.md
│   │   │
│   │   ├── AutoMinerSystem/                 ← L2 GAME-SPECIFIC
│   │   │   ├── AutoMiner.cs                 (automated ore spawner)
│   │   │   └── AutoMinerTest.md
│   │   │
│   │   └── SellerSystem/                    ← L2 GAME-SPECIFIC
│   │       ├── SellerMachine.cs             (trigger sells ore)
│   │       └── SellerTest.md
│   │
│   ├── 1-Managers/
│   │   └── ParticleManager.cs               (Singleton — spawns particles)
│   │
│   ├── 2-Data/
│   │   └── Enums/
│   │       └── GlobalEnumsC.cs              (ResourceType, PieceType, OreLimitState)
│   │
│   ├── 4-Utils/
│   │   ├── UtilsPhaseC.cs                  (WeightedRandom<T>, SimpleExplosion)
│   │   └── PhaseCLOG.cs                    (snapshot formatters)
│   │
│   └── 5-Tests/
│       ├── DEBUG_CheckC.cs                  (OreDataService plain C# test)
│       ├── OreTest.cs                       (full flow test — M/N/K/L keys)
│       └── Manual/
│           ├── MiningFlowTest.md            (hit node → ore flies out)
│           ├── AutoMinerVisualTest.md       (rotator spins, ore spawns on timer)
│           └── SellerMachineTest.md         (ore enters trigger → sells after delay)
│
├── GUIDE.md                                 (this file)
└── FLOW.md                                  (system map + data flows + event registry)
```

---

## Script Purpose — One Sentence Each

```
IDamageable              → "I'm a contract for anything that takes damage at a position"
OreNode                  → "I'm a breakable rock that drops ore when mined"
WeightedNodeDrop         → "I'm one possible drop with a weight for random selection"
OrePiece                 → "I'm a physical resource object with type + piece type"
DamageableOrePiece       → "I'm an OrePiece that breaks from collision damage"
OreDataService           → "I manage resource descriptions, color lookups, formatted strings"
OrePiecePoolManager      → "I recycle ore objects to avoid GC spikes"
OreManager               → "I own the OreDataService + clean up invalid ore pieces"
OreLimitManager          → "I throttle spawning when too many physics objects exist"
PhysicsLimitUIWarning    → "I show/hide a warning panel when the physics limit is reached"
SO_AutoMinerResourceDef  → "I configure auto-miner spawn rate + weighted ore drops"
AutoMiner                → "I spawn ore on a timer at a mining node"
SellerMachine            → "I sell ore that enters my trigger for money"
OreSled                  → "I'm a sellable, interactable sled for alternate ore transport"
ParticleManager          → "I instantiate particle prefabs at world positions"
GlobalEnumsC             → "I define ResourceType, PieceType, OreLimitState enums"
UtilsPhaseC              → "I provide WeightedRandom<T> + SimpleExplosion"
PhaseCLOG                → "I format resource description snapshots for test logging"
DEBUG_CheckC             → "I test OreDataService with plain C# (no scene)"
OreTest                  → "I test the full mining/spawn/sell flow with keyboard shortcuts"
```

---

## Hand-Typing Order

### Group 1 — Foundation (compile, can't test yet)

1. `GlobalEnumsC.cs` — ResourceType, PieceType, OreLimitState
2. `GameEvents.cs` — partial class with Phase C events
3. `UtilsPhaseC.cs` — WeightedRandom, SimpleExplosion
4. `PhaseCLOG.cs` — snapshot formatters

### Group 2 — Entities + DataService (compile + test with DEBUG_CheckC)

5. `ResourceDescription.cs` — entity
6. `WeightedOreChance.cs` — entity
7. `OrePieceKey.cs` — composite key
8. `OrePieceEntry.cs` — save data entity
9. `WeightedNodeDrop.cs` — entity (MiningSystem)
10. `OreDataService.cs` — pure C#
11. `DEBUG_CheckC.cs` — **STOP AND TEST** — compile, press Play, check Console

### Group 3 — Core System (compile + test with OreTest)

12. `IDamageable.cs` — interface
13. `OrePiece.cs` — physical ore (extends BaseSellableItem from Phase B)
14. `DamageableOrePiece.cs` — extends OrePiece + IDamageable
15. `OrePiecePoolManager.cs` — Singleton, pool manager
16. `OreManager.cs` — Singleton, owns DataService
17. `OreLimitManager.cs` — Singleton, throttle
18. `PhysicsLimitUIWarning.cs` — UI warning
19. `ParticleManager.cs` — Singleton, particles
20. `OreNode.cs` — breakable node

**STOP AND TEST** with `OreTest.cs`: M=hit node, N=spawn ore, K=counts, L=force limit check

### Group 4 — Game-Specific Systems

21. `SO_AutoMinerResourceDefinition.cs` — SO
22. `AutoMiner.cs` — automated spawner
23. `SellerMachine.cs` — sell trigger

**STOP AND TEST** — full flow: AutoMiner spawns → ore falls → push into seller → sells

---

## Vertical Slice Tests

### DEBUG_CheckC — OreDataService (Data-Level)

**What this test proves:** OreDataService creates a plain C# instance, builds with test data, and all color lookups + formatted strings work without any Unity scene objects.

**What you need to type first:** GlobalEnumsC, OreDataService, ResourceDescription, PhaseCLOG

**What you DON'T need:** OrePiece, OrePiecePoolManager, OreManager, ParticleManager, any scene objects

**Scene setup:**
1. Create empty scene
2. Create GO `"DEBUG_CheckC"` → Add Component → `DEBUG_CheckC`
3. Press Play

**How to test:**

| Action | What You Should See |
|--------|-------------------|
| Press Play | Console: resource descriptions JSON, colored string samples, "PASSED" |

**Checklist:**
- [ ] No compile errors
- [ ] Console shows formatted resource strings with color tags
- [ ] "PASSED" at end

### OreTest — Full Mining Flow (UI-Level)

**What this test proves:** OreNode takes damage → breaks → ore spawns via pool → pieces have physics → sell via SellerMachine → pool recycles. The entire Phase C pipeline without a player controller.

**What you need to type first:** Everything in Groups 1-4

**What you DON'T need:** PlayerMovement, PlayerCamera, ToolPickaxe, InventorySystem, ShopSystem

**Scene setup:** (See Manual/MiningFlowTest.md for full step-by-step)
1. OreManager, OrePiecePoolManager, OreLimitManager, ParticleManager (singletons)
2. At least one OrePiece prefab in pool manager's list
3. At least one OreNode in the scene
4. OreTest script on a GO
5. SellerMachine trigger (optional — for sell testing)

**How to test:**

| Key | What It Does | What You Should See |
|-----|-------------|-------------------|
| M | Hit nearest OreNode for 15 damage | Node takes damage. After 2 hits: shatters, ore flies out |
| N | Spawn random ore at camera | Ore piece appears, falls with physics |
| K | Print active/pooled counts | Console: `Active: X, Pooled: Y` |
| L | Force OreLimitManager recheck | Console: limit state |

**Checklist:**
- [ ] M hits node, health decreases
- [ ] Node shatters after enough hits, ore pieces fly out
- [ ] Break particles play
- [ ] N spawns ore from pool
- [ ] K shows correct counts
- [ ] Ore pushed into seller → disappears after 2s
- [ ] Pool recycling works (pooled count increases)

---

## Modifications to Earlier Phases

### Phase A — EconomyManager

EconomyManager (Phase A) should subscribe to `OnOreSold` to add money:

```csharp
// File: phase-a/_-Systems/EconomySystem/EconomyManager.cs
// ADD in Start() or Awake():

// purpose: SellerMachine and OrePiece fire this when ore is sold
GameEvents.OnOreSold += (sellValue, resourceType, pieceType, polishedPercent) =>  // ← ADD
{                                                                                  // ← ADD
    AddMoney(sellValue);                                                           // ← ADD
    DispatchOnItemSoldEvent();                                                     // ← ADD
};                                                                                 // ← ADD
```

### Phase B — ToolPickaxe (CRITICAL — mining won't work without this)

ToolPickaxe.PerformAttack must check `IDamageable` on the raycast hit and call `TakeDamage`. Without this, swinging the pickaxe at an OreNode does nothing. Also adds the `CanBreakOreIntoCrushed` check for Phase E crushable ore, and particle creation via GameEvents.

```csharp
// File: phase-b(New)/Scripts/_-Systems/ToolSystem/ToolBaseSub/ToolPickaxe.cs
// REPLACE the PerformAttack coroutine body — after the raycast hit:

// → Phase C: if this pickaxe can break ore into crushed pieces, try that first  // ← ADD
if (_canBreakOreIntoCrushed)                                                       // ← ADD
{                                                                                   // ← ADD
    OrePiece orePiece = hit.collider.GetComponent<OrePiece>();                     // ← ADD
    if (orePiece != null && orePiece.CrushedPrefab != null                         // ← ADD
        && orePiece.CrushedPrefab.GetComponent<OrePiece>() != null                 // ← ADD
        && orePiece.PieceType == PieceType.ore                                     // ← ADD
        && orePiece.TryConvertToCrushed())                                         // ← ADD
    {                                                                               // ← ADD
        GameEvents.RaiseCreateParticleRequested(                                   // ← ADD
            Singleton<ParticleManager>.Ins.GetOreNodeHitParticlePrefab(),           // ← ADD
            hit.point, Quaternion.LookRotation(hit.normal));                       // ← ADD
        yield return new WaitForFixedUpdate();                                     // ← ADD
        UtilsPhaseC.SimpleExplosion(hit.point, 0.5f, 2f, 0.5f);                   // ← ADD
        yield break;                                                               // ← ADD
    }                                                                               // ← ADD
}                                                                                   // ← ADD

// → Phase C: check IDamageable (OreNode, DamageableOrePiece)                      // ← ADD
IDamageable damageable = hit.collider.GetComponent<IDamageable>();                  // ← ADD
if (damageable != null)                                                             // ← ADD
{                                                                                   // ← ADD
    damageable.TakeDamage(_damage, hit.point);                                     // ← ADD
    GameEvents.RaiseCreateParticleRequested(                                       // ← ADD
        Singleton<ParticleManager>.Ins.GetOreNodeHitParticlePrefab(),              // ← ADD
        hit.point, Quaternion.LookRotation(hit.normal));                           // ← ADD
}                                                                                   // ← ADD
else                                                                                // ← ADD
{                                                                                   // ← ADD
    GameEvents.RaiseCreateParticleRequested(                                       // ← ADD
        Singleton<ParticleManager>.Ins.GetGenericHitImpactParticle(),              // ← ADD
        hit.point, Quaternion.LookRotation(hit.normal));                           // ← ADD
}                                                                                   // ← ADD

// → existing AddForce + PhysicsSoundPlayer code stays below                        // ← KEEP
```

### Phase B — BasePhysicsObject

OrePiece extends `BaseSellableItem` which extends `BasePhysicsObject`. Ensure `BasePhysicsObject` has:
- `public Rigidbody Rb` field (or getter)
- `ClearTouchingConveyorBelts()` method (stub if Phase D not done yet)
- `protected virtual void OnEnable()` / `OnDisable()` (OrePiece overrides these)

```csharp
// File: phase-b/3-MonoBehaviours/Physics/BasePhysicsObject.cs
// ENSURE these exist (may already be there):

public Rigidbody Rb;                           // ← ENSURE
public void ClearTouchingConveyorBelts() { }   // ← ADD stub if missing (Phase D completes)
protected virtual void OnEnable() { }           // ← ENSURE virtual
protected virtual void OnDisable() { }          // ← ENSURE virtual
```

### Phase All — GlobalEnumsAll (TagType)

Add `markedForDestruction` to the TagType enum if not already present:

```csharp
// File: phase-All/2-Data/Enums/GlobalEnumsAll.cs
public enum TagType
{
    grabbable,
    markedForDestruction,  // ← ADD if missing (used by SellAfterDelay)
}
```

---

## Source vs Phase Diff

| What | Original Source | Our Implementation |
|------|----------------|-------------------|
| OreNode → OrePiecePoolManager | Direct `Singleton<OrePiecePoolManager>.Instance.SpawnPooledOre()` call | Fires `GameEvents.RaiseSpawnOreRequested` — OrePiecePoolManager subscribes. Makes MiningSystem L0 portable. |
| OreNode → ParticleManager | Direct `Singleton<ParticleManager>.Instance.CreateParticle()` call | Fires `GameEvents.RaiseCreateParticleRequested` — ParticleManager subscribes. MiningSystem doesn't import ParticleManager. |
| OreNode → SavingLoadingManager | Direct `Singleton<SavingLoadingManager>.Instance.AddDestroyedStaticBreakablePosition()` | Fires `GameEvents.RaiseStaticBreakableBroken()` — Phase G subscribes. |
| OreNode → BuildingManager | Direct `Singleton<BuildingManager>.Instance.BuildingSupportsCollisionLayers` | Fires `GameEvents.RaiseOreNodeBroken()` — Phase D subscribes for support update. |
| OreNode → SoundManager | Direct `Singleton<SoundManager>.Instance.PlaySoundAtLocation()` | Stubbed as `// Phase H:` — SoundManager is Phase H. |
| OrePiece.SellAfterDelay → EconomyManager | Direct `Singleton<EconomyManager>.Instance.AddMoney()` | Fires `GameEvents.RaiseOreSold()` — EconomyManager subscribes. Decoupled. |
| OrePiece.SellAfterDelay → QuestManager | Direct `Singleton<QuestManager>.Instance.OnResourceDeposited()` | Fires `GameEvents.RaiseOreSold()` — QuestManager subscribes (Phase F). |
| PhysicsLimitUIWarning | Static instance with `SwitchState(state)` called by OreLimitManager | Subscribes to `GameEvents.OnOreLimitChanged` — no direct call from OreLimitManager. |
| OreLimitManager → SettingsManager | Direct `Singleton<SettingsManager>.Instance.MovingPhysicsObjectLimit` | Uses `[SerializeField] int _movingPhysicsObjectLimit` — Phase H replaces with SettingsManager read. |
| AutoMiner → BuildingManager | Direct access for green/red light materials | Uses `[SerializeField] Material _greenLightMaterial/_redLightMaterial` — self-contained. |
| AutoMiner.OnEnable building logic | Complex BuildingPlacementNode finding + Pack validation | Simplified — configures from definition, turns off if no definition. Phase D completes building integration. |
| WeightedNodeDrop.OrePrefab | `OrePiece` type | `GameObject` type — MiningSystem doesn't import OrePiece. OrePiecePoolManager extracts component. |
| Original enums | PascalCase (ResourceType.Iron, PieceType.Ore) | camelCase (ResourceType.iron, PieceType.ore) per project convention |

---

## Systems & Testability

### Individual Systems

| System | Scripts | Decoupling | Level |
|--------|---------|-----------|-------|
| MiningSystem | IDamageable, OreNode, WeightedNodeDrop | Events only: SpawnOreRequested, CreateParticleRequested, OreNodeBroken, OreMined, StaticBreakableBroken | L0 |
| OreSystem | OrePiece, DamageableOrePiece, OreDataService, OrePiecePoolManager, OreManager, OreLimitManager, PhysicsLimitUIWarning, entities, SO_ | Subscribes: SpawnOreRequested, OreLimitChanged. Fires: OreSold, OreLimitChanged | L0 |
| AutoMinerSystem | AutoMiner | Reads: OrePiecePoolManager.Ins, OreLimitManager.Ins | L2 |
| SellerSystem | SellerMachine | Fires: OreSold. Reads: OrePiece directly | L2 |

### Testability Matrix

| System | .cs Test | Manual/*.md | Needs Other Systems? |
|--------|---------|------------|---------------------|
| MiningSystem | MiningTest.md (driver) | MiningFlowTest.md | NO — fires events only |
| OreSystem | OreTest.cs (N/K keys) | MiningFlowTest.md (shared) | NO — subscribes to events |
| AutoMinerSystem | AutoMinerTest.md | AutoMinerVisualTest.md | YES — OreSystem (spawn) |
| SellerSystem | SellerTest.md | SellerMachineTest.md | YES — OreSystem (OrePiece) |
| OreDataService | DEBUG_CheckC.cs | — | NO — plain C# new |

**Final count:** 4 systems, 19 scripts, 2 .cs tests, 3 manual tests. Zero tight coupling between MiningSystem and OreSystem (event-driven). AutoMiner and Seller are game-specific (L2).