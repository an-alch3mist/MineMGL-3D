# Seller Machine — Manual Test

> Verifies: ore enters trigger → waits → money increases → ore returns to pool.

---

## Prerequisites

- All singletons from MiningFlowTest setup
- OrePiece prefabs registered in pool
- OreTest script on a GO (for U key ore spawning)

---

## Setup Guide

### Step 1 — SellerMachine GO

1. Create Empty GO → name `SellerMachine`
2. Add `BoxCollider`:
   - **IsTrigger = true** ← critical
   - Size: (2, 1, 2) — large enough for ore to roll/fall into
3. Add `SellerMachine` component (no SerializeFields to wire)
4. Position on the floor — visible and accessible

Optional visual: add a child GO with a MeshRenderer (e.g. a funnel/hopper shape) so you can see where the trigger is.

### Step 2 — Verify OreTest Wiring

On `OreTest` component:

| Field | Drag From |
|-------|-----------|
| `_testOrePrefab` | OrePiece_Iron prefab |
| `_cam` | Camera (for U key spawn direction) |

### Step 3 — Money Logging

OreTest.Start already subscribes:
```
GameEvents.OnOreSold → logs "[OreTest] Sold: Iron Ore for $X.XX"
GameEvents.OnMoneyChanged → logs "[OreTest] Money: $XXX.XX"
```

### Final Hierarchy (add to MiningFlowTest scene)

```
Scene Root
├── ... (singletons from MiningFlowTest)
├── SellerMachine (with trigger collider on floor)
├── OreTest
└── Floor
```

---

## How It Works (System Flow)

**Trigger enter:** When any `Rigidbody` enters the `SellerMachine`'s trigger collider (`IsTrigger = true`), Unity calls `SellerMachine.OnTriggerEnter(Collider other)`. First it checks `other.CompareTag("MarkedForDestruction")` — if true, returns (already being sold). Then checks `other.attachedRigidbody == null` — if no rigidbody, returns (ignore static objects).

**OrePiece path:** `other.GetComponentInParent<OrePiece>()` — if found, calls `orePiece.SellAfterDelay(2f)`. Inside `SellAfterDelay`: if the piece is held by a magnet, `CurrentMagnetTool.DetachBody(Rb)` releases it first. Then `gameObject.tag = "MarkedForDestruction"` — this prevents other triggers from double-selling it. A **coroutine** starts: waits 2 seconds, then calls `Singleton<EconomyManager>.Ins.AddMoney(sellValue)` → `GameEvents.RaiseMoneyChanged(newTotal)` → then `GameEvents.RaiseOreSold(price, resourceType, pieceType)` (for quest tracking in Phase F) → finally `Delete()` which calls `OrePiecePoolManager.ReturnToPool(this)`. The pool **deactivates** the piece (`SetActive(false)`), resets all state (velocity, drag, tag back to "Grabbable"), parents under pool root, and **enqueues** it for reuse.

**BaseSellableItem path:** If not an OrePiece, checks `other.GetComponentInParent<BaseSellableItem>()` — if found, directly calls `EconomyManager.AddMoney(sellValue)` and `Destroy(gameObject)` (not pooled — only OrePiece uses the pool).

**MarkedForDestruction guard:** Once tagged, any other seller trigger that receives this ore will see `CompareTag("MarkedForDestruction")` → return immediately. This prevents double-sell even if ore enters multiple triggers.

---

## 1. Initial State

**DO:** Press Play, open Console
**EXPECT:**
- SellerMachine trigger visible in **Scene view** as green wireframe box
- Starting money: `$400.00` (EconomyManager default)
- No ore pieces in scene
- No console errors

---

## 2. Spawn Ore Near Seller

**DO:** Press `U` to spawn an ore piece — aim so it lands **inside or near** the seller trigger
**EXPECT:**
- Ore piece **appears** 2m in front of camera
- Falls via gravity toward the seller
- If it lands inside the trigger → proceed to Step 3
- If it misses → spawn another closer to the trigger

---

## 3. Ore Enters Trigger

**DO:** Observe when ore piece enters the SellerMachine trigger collider
**EXPECT:**
- `OnTriggerEnter` fires on SellerMachine
- Ore is **tagged "MarkedForDestruction"** immediately
  - Check in Inspector during Play: tag changed from "Grabbable" to "MarkedForDestruction"
- Other triggers will now **ignore** this ore (they check `CompareTag("MarkedForDestruction")`)
- Ore is **still visible** — it hasn't sold yet (2s delay starts)

---

## 4. After 2-Second Delay

**DO:** Wait 2 seconds after ore entered trigger
**EXPECT:**
- Ore piece **disappears** (returned to pool → `SetActive(false)`)
- Console: `[OreTest] Sold: Iron Ore for $X.XX` (price = BaseSellValue × RandomPriceMultiplier)
- Console: `[OreTest] Money: $40X.XX` (starting 400 + sell value)
- Money on HUD updates (if MoneyHUD exists)

---

## 5. Multiple Ore Simultaneously

**DO:** Spawn 3-4 ore pieces rapidly (press `U` multiple times) → let all enter trigger
**EXPECT:**
- Each piece is tagged "MarkedForDestruction" independently on trigger enter
- Each sells after its own 2s delay (they don't batch)
- Console shows **multiple** Sold messages — one per piece
- Money increases **incrementally** per piece
- `OrePiece.AllOrePieces` count decreases as each is returned to pool

**DO:** Press `I` to check ore count after all sell
**EXPECT:**
- Count decreased by the number sold

---

## 6. Pool Recycling Verification

**DO:** After selling 3+ ore, press `U` again to spawn new ore
**EXPECT:**
- New ore **reuses a pooled piece** — no new Instantiate (you can verify in Hierarchy: piece name ends with `[Pooled]`)
- Pooled piece has its **original mesh + scale** from first spawn (pool reset doesn't re-randomize mesh/scale — `Start()` only runs once per Instantiate, not on pool recycle)

---

## 7. MarkedForDestruction Prevents Double-Sell

**DO:** Spawn ore → let it enter seller trigger → immediately push it into a second seller trigger (if you have two)
**EXPECT:**
- Only **one** sell occurs — second trigger checks `CompareTag("MarkedForDestruction")` and returns early
- No double money, no console double-log

---

## 8. Non-OrePiece Sellable (BaseSellableItem)

**DO:** Create a simple GO with `BaseSellableItem` component (BaseSellValue = 5), Rigidbody, Collider, push it into seller
**EXPECT:**
- Item is **Destroyed** (not pooled — only OrePiece uses pool)
- Money increases by 5
- Console: `[OreTest] Sold: INVALID INVALID for $5.00` (INVALID because BaseSellableItem doesn't have ResourceType/PieceType)

---

## 9. Edge Case — Ore Already MarkedForDestruction

**DO:** Manually tag an ore piece as "MarkedForDestruction" in Inspector → push into seller
**EXPECT:**
- `OnTriggerEnter` checks tag first → **returns immediately**
- No sell, no money change, no console log

---

## Summary Checklist

- [ ] Ore enters trigger → tagged "MarkedForDestruction" immediately
- [ ] 2s delay before sell completes
- [ ] Money increases by sell value (BaseSellValue × RandomPriceMultiplier)
- [ ] Console: `OnOreSold` fires with correct ResourceType + PieceType
- [ ] Console: `OnMoneyChanged` fires with new total
- [ ] Ore returns to pool (disappears via SetActive false, not Destroyed)
- [ ] Multiple ore sell independently (own 2s timers)
- [ ] Pool recycles: next spawn reuses dequeued piece (name has [Pooled])
- [ ] MarkedForDestruction prevents double-sell
- [ ] Non-OrePiece sellable: Destroyed, not pooled
- [ ] Pre-tagged MarkedForDestruction ore: ignored by seller
- [ ] Zero console errors throughout