# Grab Rope — Manual Test Flow

> Verifies SpringJoint grab + LineRenderer rope visual + joint break + release behavior.

---

## Prerequisites

- UIManager + EconomyManager singletons (phase-All)
- PlayerGrabTest script on a GO (for M/N menu sim keys)

---

## Setup Guide — Step by Step

### Step 1 — Player GO

1. Create Empty GO → name `Player`
2. Add components: `CharacterController`, `PlayerMovement`, `PlayerCamera`, `PlayerGrab`
3. `PlayerMovement` wiring:

| Field | Value / Drag From |
|-------|-------------------|
| `_cc` | CharacterController (self) |
| `_playerCam` | Camera child (Step 2) |
| `_viewModelContainer` | ViewModelContainer child (Step 3) |
| `_groundLayer` | "Ground" layer |
| `_walkSpeed` | 4 |
| `_sprintSpeed` | 6 |

### Step 2 — Camera (child of Player)

1. Create Empty child of Player → name `Camera`
2. Add `Camera` component
3. `PlayerCamera` wiring on Player:

| Field | Drag From |
|-------|-----------|
| `_cam` | Camera child |
| `_movement` | PlayerMovement (on parent) |

### Step 3 — ViewModelContainer (child of Camera)

1. Create Empty child of Camera → name `ViewModelContainer`
2. No components needed — tools parent here when equipped

### Step 4 — HoldPosition (child of Camera)

1. Create Empty child of Camera → name `HoldPosition`
2. Position: `(0, 0, 1)` local — 1m in front of camera
3. This is where grabbed objects are pulled toward

### Step 5 — RigidbodyDragger (child of Player)

1. Create Empty child of Player → name `RigidbodyDragger`
2. Add `Rigidbody` component: **isKinematic = true**
3. Add `RigidbodyDraggerController` component
4. Wire: `_playerGrab` → PlayerGrab on Player
5. **Set inactive** in Inspector (starts hidden)

### Step 6 — LineRenderer (on Player GO)

1. Select Player GO → Add Component → `LineRenderer`
2. Settings:
   - Material: Default-Line or any unlit material
   - Start Width: `0.02`, End Width: `0.02`
   - Positions: 2 (set at runtime, don't worry about initial values)
   - Use World Space: true

### Step 7 — PlayerGrab Wiring

On Player GO → `PlayerGrab` component:

| Field | Drag From |
|-------|-----------|
| `_cam` | Camera child |
| `_holdPos` | HoldPosition child |
| `_dragger` | RigidbodyDragger child |
| `_rope` | LineRenderer (on Player) |
| `_interactRange` | 2 |
| `_interactLayerMask` | "Interact" layer |
| `_movement` | PlayerMovement (on self) |

### Step 8 — Grabbable Cubes

Create 4-5 cubes:

1. GameObject → 3D Object → Cube
2. Add `Rigidbody` (mass 1, no constraints, gravity ON)
3. Tag: `"Grabbable"`
4. Layer: `"Interact"`
5. Position on the floor in front of player spawn

### Step 9 — PlayerGrabTest Script

1. Create Empty GO → name `PlayerGrabTest`
2. Add `PlayerGrabTest` component (provides M/N keys for menu sim)

### Step 10 — Floor

- GameObject → 3D Object → Plane at y=0
- Layer: `"Ground"`

### Final Scene Hierarchy

```
Scene Root
├── Player (CharacterController, PlayerMovement, PlayerCamera, PlayerGrab)
│   ├── Camera (Camera component)
│   │   ├── ViewModelContainer
│   │   └── HoldPosition (local pos 0,0,1)
│   ├── RigidbodyDragger (inactive, Rigidbody isKinematic, RigidbodyDraggerController)
│   └── LineRenderer (width 0.02)
├── UIManager (phase-All)
├── EconomyManager (phase-All)
├── PlayerGrabTest
├── GrabbableCube_01 (Rigidbody, tag Grabbable, layer Interact)
├── GrabbableCube_02
├── GrabbableCube_03
├── GrabbableCube_04
├── Floor (Plane, layer Ground)
└── PlayerSpawnPoint
```

---

## How It Works (System Flow)

**Grab:** When the player right-clicks, `PlayerGrab.Update()` detects `Mouse1` and calls `TryGrab()`. It checks `isAnyMenuOpen` (blocks if true), then checks if already holding (calls `Release()` if so). Then it raycasts from `_cam` using `_interactLayerMask`. If the hit collider's tag is `"Grabbable"`, it calls `GrabObject(hit)`. Inside: the **RigidbodyDragger child GO activates** (`SetActive(true)`), parents to `_holdPos`, a `SpringJoint` is **added at runtime** to the dragger and connected to the cube's `Rigidbody`. `UtilsPhaseB.IgnoreAllCollisions(cube, player, true)` prevents clipping. The cube's `linearDamping` increases (feels heavy) and `interpolation` is set to `Interpolate` (smooth visual). The **LineRenderer enables** with `positionCount = 2`.

**Every frame while holding:** `PlayerGrab.Update()` calls `_rope.SetPosition(0, _dragger.position)` and `_rope.SetPosition(1, cubeAnchorPoint)` — the rope visually connects hand to cube. The dragger follows `_holdPos` which is a child of the camera — so moving/looking moves the grab target.

**Release:** Right-click again → `TryGrab()` sees `heldObject != null` → calls `Release()`. The `SpringJoint` is **Destroyed** (runtime component removal), the dragger **deactivates** (`SetActive(false)`), cube drag/angular drag restored to originals, collisions re-enabled. A coroutine waits 3s then sets `interpolation = None` on the cube (saves CPU when idle). The **rope disables** (`_rope.enabled = false`).

**Joint break:** If the player pulls too far, the `SpringJoint.breakForce` (120) is exceeded → Unity calls `OnJointBreak` on the dragger → `RigidbodyDraggerController.OnJointBreak()` calls `_playerGrab.ForceRelease()` — same cleanup as normal release.

---

## 1. Initial State

**DO:** Press Play
**EXPECT:**
- All cubes sitting on ground, physics settled
- LineRenderer **not visible** (`_rope.enabled = false` in `PlayerGrab.Start()`)
- RigidbodyDragger GO is **inactive** (set inactive in editor)
- Cursor locked (FPS mode)

---

## 2. Grab a Cube

**DO:** Look at a cube (within 2m) → right-click
**EXPECT:**
- **Rope appears** — line from HoldPosition to cube's grab point
- Rope has 2 points: dragger position → cube anchor point
- Cube starts **following** your aim with spring physics (slight lag + bounce)
- RigidbodyDragger GO is now **active**
- Cube's Rigidbody: linearDamping increased (feels heavy/dampened)
- Cube's Rigidbody: interpolation set to Interpolate (smooth visual)

---

## 3. Move While Grabbing

**DO:** WASD to walk around while holding cube
**EXPECT:**
- Rope **stretches and follows** — line endpoints update every frame
- Cube trails behind with spring tension
- Rope length varies as you move (not fixed distance)
- No rope clipping through walls (rope is just a line, it will clip — that's normal)

**DO:** Look up/down while holding
**EXPECT:**
- Cube follows vertical aim too — HoldPosition is child of camera
- Rope redraws to match new position

---

## 4. Release

**DO:** Right-click again
**EXPECT:**
- Rope **disappears** instantly
- SpringJoint destroyed
- RigidbodyDragger GO goes **inactive**
- Cube's Rigidbody: linearDamping restored to original (bouncy again)
- Cube continues with residual physics velocity (slides/rolls)
- After ~3s: cube's interpolation set back to None (DisableInterpolationLater coroutine)

---

## 5. Grab + Object Destroyed

**DO:** Grab a cube → have another script destroy it (or use Debug console: `Destroy(cube)`)
**EXPECT:**
- Rope **disappears** automatically (Update checks `heldObject.activeInHierarchy`)
- No null ref errors in console
- Grab state resets — can grab another cube immediately

---

## 6. SpringJoint Break (Force Limit)

**DO:** Grab a cube → walk very far away quickly (sprint away)
**EXPECT:**
- At some distance, SpringJoint **breaks** (breakForce = 120)
- `RigidbodyDraggerController.OnJointBreak()` fires → calls `ForceRelease()`
- Rope **disappears**
- Cube's damping restored
- RigidbodyDragger goes inactive
- Console: no errors

**DO:** Try to grab another cube after joint break
**EXPECT:**
- Works normally — state fully reset

---

## 7. Can't Grab When Menu Open

**DO:** Press `M` (simulate menu open) → right-click on cube
**EXPECT:**
- Nothing happens — grab blocked when `isAnyMenuOpen = true`
- Cursor is unlocked (visible)

**DO:** Press `N` (simulate menu close) → right-click on cube
**EXPECT:**
- Grab works normally again

---

## 8. Grab Non-Grabbable Object

**DO:** Look at a wall or floor (no "Grabbable" tag) → right-click
**EXPECT:**
- Nothing happens — only tag "Grabbable" objects can be grabbed
- No rope appears

---

## 9. Rope Visual Quality

**DO:** Grab a cube → look at the rope closely
**EXPECT:**
- Rope is a thin line (width 0.02)
- Start point: at RigidbodyDragger position (near HoldPosition)
- End point: at cube's connected anchor point (where you clicked on the cube)
- Rope updates every frame — no 1-frame lag

---

## Summary Checklist

- [ ] Rope appears on grab (2 points, thin line)
- [ ] Rope follows cube + player movement every frame
- [ ] Right-click releases — rope disappears, damping restored
- [ ] Destroyed object → auto-release, no errors
- [ ] SpringJoint break → ForceRelease, rope gone, state reset
- [ ] Can grab new cube after break/release
- [ ] Menu open blocks grab
- [ ] Non-Grabbable objects ignored
- [ ] After 3s post-release: interpolation disabled on cube
- [ ] RigidbodyDragger active during grab, inactive otherwise