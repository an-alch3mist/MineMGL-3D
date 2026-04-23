# Conveyor Flow — Manual Test

> Verifies: belt physics push, shaker oscillation, blocker gate, splitter arm, routing switch, batch rendering.

---

## Prerequisites

- All Phase C singletons (OrePiecePoolManager, EconomyManager, etc.)
- ConveyorBeltManager singleton
- ConveyorTest script on a GO
- Pre-placed conveyor belt chain in scene (3-4 belts end-to-end)

---

## Setup Guide

### Step 1 — Conveyor Belt Chain

Place 3-4 ConveyorBelt prefabs end-to-end on the floor:
- Each has `ConveyorBelt` component with `Speed = 0.8`, trigger collider `IsTrigger = true`
- Align forward directions (blue arrow in Scene view) so ore flows left→right

### Step 2 — Conveyor Variants (optional, place alongside chain)

- **ConveyorBeltShaker** — same as belt but with shaker component (ShakeSpeed=2, ShakeFrequency=2)
- **ConveyorBlockerT2** — place after belt chain, assign `_movingPart`, `_closedPosition`, `_openPosition`
- **RoutingConveyor** — place at fork point, assign `_rotatingPart`, `_closedRotation`, `_openRotation`
- **ConveyorSplitterT2** — place at T-junction, assign `_rotatingThing`

### Step 3 — ConveyorTest Script

1. Create GO → add `ConveyorTest` component
2. Wire: `_testOrePrefab` → OrePiece_Iron, `_spawnPoint` → transform above first belt, `_testBlocker` → ConveyorBlockerT2 instance, `_testRouter` → RoutingConveyor instance

### Step 4 — Seller (optional)

Place a SellerMachine trigger at the end of the belt chain to verify full flow.

---

## How It Works (System Flow)

**Ore enters belt:** When an OrePiece's Rigidbody collider enters a `ConveyorBelt`'s trigger, `OnTriggerEnter` fires → gets `BasePhysicsObject` component (adds one if missing) → calls `AddPhysicsObject(obj)` → object added to `_physicsObjectsOnBelt` list. `obj.AddTouchingConveyorBelt(this)` registers this belt on the physics object (for multi-belt accumulation).

**Physics push:** Every `FixedUpdate`, `ConveyorBelt.FixedUpdate()` loops all objects on belt → calls `obj.AddConveyorVelocity(_pushVelocity, RetainYVelocity)`. This **accumulates** — if ore is on two overlapping belts, both add velocity. Then `ConveyorBeltManager.FixedUpdate()` (runs after all belts) loops all registered `BasePhysicsObject` → applies `rb.linearVelocity = sumVelocity / count`. Y velocity is preserved if `RetainY` is true.

**Shaker:** `ConveyorBeltShaker.FixedUpdate()` overrides base — adds sinusoidal oscillation: `rightDir * ShakeSpeed * sign(sin(t * 2π * freq))` for left-right + `upDir * VerticalShakeSpeed * sin(t * 2π * vertFreq)` for up-down. Combined with forward push, ore shakes while moving forward.

**Blocker:** `ConveyorBlocker.Update()` checks `HingeJoint.angle < closedAngle` → sets `Conveyor.Disabled = true`. When disabled, `ConveyorBelt.FixedUpdate` returns early — **no push**. `ConveyorBlockerT2` is interactable — Toggle moves the `_movingPart` between open/closed positions.

**Routing:** `RoutingConveyor.ToggleDirection()` rotates `_rotatingPart` between two euler angles, swaps `_closedObjects`/`_openObjects` active state — physically changes which belt path ore follows.

**Batch rendering:** Each belt has a `ConveyorBatchRenderingComponent` which caches `Matrix4x4.TRS` on enable and adds to static `AllConveyors` list. `ConveyorRenderer.LateUpdate()` groups by `MeshIndex`, then calls `Graphics.DrawMeshInstanced` in batches of 1023. The actual Renderer on each belt is **disabled** — all visual rendering is batched.

---

## 1. Ore Flows Along Belt Chain

**DO:** Press `Space` (spawn ore above first belt)
**EXPECT:**
- Ore **appears** above first belt, falls onto it
- Ore **slides forward** along the belt at Speed (0.8 m/s)
- Ore transfers to next belt seamlessly (OnTriggerExit + OnTriggerEnter)
- Continues through all 3-4 belts

**Behind the scenes:** `OnTriggerEnter` → `AddPhysicsObject` → `FixedUpdate` → `AddConveyorVelocity` → `ConveyorBeltManager.FixedUpdate` → `rb.linearVelocity = pushVelocity`.

---

## 2. Shaker Oscillation

**DO:** Place ore on ConveyorBeltShaker
**EXPECT:**
- Ore **moves forward** AND **oscillates left-right + up-down**
- Visible wobble as it travels

---

## 3. Blocker Gate

**DO:** Press `U` (toggle ConveyorBlockerT2)
**EXPECT:**
- Gate **slides closed** (movingPart moves to closedPosition)
- Ore on belt behind gate **stops** (belt disabled)
- Console: `[ConveyorTest] Blocker: CLOSED`

**DO:** Press `U` again
**EXPECT:**
- Gate **slides open**
- Ore **resumes flowing**

---

## 4. Routing Direction

**DO:** Press `I` (toggle RoutingConveyor)
**EXPECT:**
- Rotating part **switches direction**
- Ore entering after toggle follows **new path**
- Console: `[ConveyorTest] Router: CLOSED/OPEN`

---

## 5. Splitter Arm

**DO:** Observe ConveyorSplitterT2 with ore flowing through
**EXPECT:**
- Arm **swings back and forth** (minY to maxY with SmoothStep)
- Ore alternates between two output paths
- Arm pauses at each extreme for `_pauseTime`
- When no ore passes for `_idleTime`, arm **stops swinging**

---

## 6. Batch Rendering

**DO:** Place 50+ conveyor belts → check Hierarchy vs Scene view
**EXPECT:**
- Individual belt Renderers are **disabled** (ConveyorBatchRenderingComponent.OnEnable)
- Belts still **visible** in Game view (rendered via DrawMeshInstanced)
- Frame rate stays smooth (batched rendering, not per-object)

---

## Summary Checklist

- [ ] Ore enters belt trigger → pushed forward at Speed
- [ ] Multiple belts end-to-end → ore flows through chain seamlessly
- [ ] Shaker: ore oscillates left-right + up-down while moving forward
- [ ] Blocker closed → belt disabled, ore stops; open → resumes
- [ ] BlockerT2 Toggle → gate slides, interactable
- [ ] RoutingConveyor Toggle → direction switches, ore follows new path
- [ ] SplitterT2 → arm swings, alternates ore direction, idles when empty
- [ ] Batch rendering: individual Renderers disabled, DrawMeshInstanced visible
- [ ] ConveyorBeltManager round-robin cleans null objects
- [ ] Zero console errors