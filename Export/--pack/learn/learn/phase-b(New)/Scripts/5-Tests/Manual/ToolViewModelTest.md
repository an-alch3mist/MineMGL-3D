# Tool ViewModel — Manual Test Flow

> Verifies tool equip/unequip ViewModel swap, animation timing, and visual transitions.

---

## Prerequisites

- Phase B scene with Player GO fully wired (see InventoryUITest.md → or GUIDE.md Scene Setup)
- InventoryOrchestrator + InventoryUI wired with hotbar panel
- EconomyManager + UIManager singletons (phase-All)

---

## Setup Guide — Step by Step

### Step 1 — Player GO (if not already set up)

Create the full player hierarchy per GUIDE.md Scene Setup items 1-12. Key components needed for this test:
- `PlayerMovement` (Singleton) on root → with `_playerCam`, `_viewModelContainer`, `_cc`
- `PlayerCamera` on root → with `_cam`, `_movement`, `_viewModelContainer`
- `PlayerGrab` on root → with `_cam`, `_movement` (subscribes to OnToolEquipped)
- **ViewModelContainer** child of Camera → tools parent here when equipped

### Step 2 — Inventory (if not already set up)

- **HotbarPanel** on Canvas with `HorizontalLayoutGroup`
- **InventoryOrchestrator** GO → wired per InventoryUITest.md Step 7
- **InventoryUI** GO → wired to Orchestrator

### Step 3 — ToolActionTest Script

1. Create Empty GO → name `ToolActionTest`
2. Add `ToolActionTest` component
3. Wire `_testTools` list with 4 tool prefab instances in scene (order: Pickaxe, Magnet, Hammer, MiningHat)

### Step 4 — Grabbable Cubes (for magnet test)

- 5-6 cubes with `Rigidbody`, tag `"Grabbable"`, layer `"Interact"`
- Place near player spawn

### Step 5 — Tool Prefabs (create 4, place on ground)

Each tool needs the WorldModel/ViewModel hierarchy below. **Save each as prefab** then place instances in the scene.

## Tool Prefab Hierarchies

```
ToolPickaxe (root, has ToolPickaxe component)
├── WorldModel (active when on ground)
│   - Rigidbody, Collider, tag "Grabbable", layer "Interact"
│   └── pickaxe_mesh (visible mesh)
└── ViewModel (active when equipped)
    - Animator component → ToolPickaxe_Controller (has "Attack1" state)
    └── hands_with_pickaxe_mesh

Wire on ToolPickaxe:
  _worldModel → WorldModel
  _viewModel → ViewModel
  _viewModelAnimator → ViewModel/Animator
  _name → "Pickaxe"
  _inventoryIcon → pickaxe sprite
  _hitLayers → "Interact" + "Ground"
  _useRange → 2
  _attackCooldown → 1
```

```
ToolMagnet (root, has ToolMagnet component)
├── WorldModel (same as above)
├── ViewModel
│   ├── magnet_mesh
│   └── SelectionModeText (TMP_Text, shows "Everything" etc.)
└── PullOrigin (empty child, positioned in front of camera when equipped)

Wire on ToolMagnet:
  _pullOrigin → PullOrigin child
  _grabbableLayer → layer with "Grabbable" tagged objects
  _selectionModeText → ViewModel/SelectionModeText
  _pullRadius → 2
  _pushForce → 3
  _dropForce → 1
```

```
ToolMiningHat (root, has ToolMiningHat component)
├── WorldModel
│   └── hat_mesh
│   └── WorldModelLight (Light component, directional or spot)
└── ViewModel
    └── hat_viewmodel_mesh
    └── ViewModelLight (Light component)

Wire on ToolMiningHat:
  _worldModelLight → WorldModel/WorldModelLight
  _viewModelLight → ViewModel/ViewModelLight
```

### ToolActionTest Wiring Table

| Field | Drag From |
|-------|-----------|
| `_testTools[0]` | ToolPickaxe instance in scene |
| `_testTools[1]` | ToolMagnet instance in scene |
| `_testTools[2]` | ToolHammer instance in scene |
| `_testTools[3]` | ToolMiningHat instance in scene |

### Final Scene Hierarchy

```
Scene Root
├── Player (PlayerMovement, PlayerCamera, PlayerGrab, PlayerFootsteps)
│   ├── Camera (Camera component)
│   │   └── ViewModelContainer (tools parent here)
│   ├── HoldPosition
│   ├── RigidbodyDragger (inactive)
│   └── LineRenderer
├── Canvas
│   ├── HotbarPanel (10 slots)
│   └── InventoryUI (self-disables)
├── InventoryOrchestrator
├── UIManager (phase-All)
├── EconomyManager (phase-All)
├── ToolActionTest (_testTools wired to 4 tool instances)
├── ToolPickaxe_01 (on ground, WorldModel visible)
├── ToolMagnet_01 (on ground)
├── ToolHammer_01 (on ground)
├── ToolMiningHat_01 (on ground)
├── GrabbableCube × 5 (Rigidbody, tag Grabbable, layer Interact)
├── Floor (Plane)
└── PlayerSpawnPoint
```

---

## How It Works (System Flow)

**Tool on ground:** Each tool prefab has two child GOs: `WorldModel` (visible on ground) and `ViewModel` (visible when equipped). When the tool is on the ground, both are active but `BaseHeldTool.OnEnable()` checks `Owner == null` → shows WorldModel, hides ViewModel.

**Pickup → Equip:** Player fires `RaiseToolPickupRequested(tool)` → `InventoryOrchestrator.HandleToolPickup` → `dataService.TryAdd(tool)` → `tool.gameObject.SetActive(false)` (disappears from ground) → `SwitchToSlot(idx)` → `tool.gameObject.SetActive(true)` → `BaseHeldTool.OnEnable()` fires → `RaiseToolEquipped` → `PlayerGrab` sets `tool.Owner = _movement` → tool calls `HideWorldModel()` (WorldModel child `SetActive(false)`) + `HideViewModel(false)` (ViewModel child `SetActive(true)`) → tool **parents itself to `Owner.ViewModelContainer`** (child of camera) → ViewModel now visible as first-person held tool.

**Tool switch:** `InventoryOrchestrator.SwitchToSlot(newIndex)` → previous tool `SetActive(false)` → ViewModel disappears → new tool `SetActive(true)` → `OnEnable` → parents to ViewModelContainer → ViewModel appears.

**Swing animation:** `ToolPickaxe.PrimaryFireHeld()` → `SwingPickaxe()` → `_viewModelAnimator.Play("Attack1", -1, 0f)` → Animator plays Attack1 state → `HasExitTime = true` → auto-returns to Idle after clip finishes. Cooldown timer prevents spam.

**Drop:** `tool.DropItem()` → `HideWorldModel(false)` (WorldModel visible) → `HideViewModel()` (ViewModel hidden) → unparent from ViewModelContainer → `rb.linearVelocity = cam.forward * 5f` → tool flies forward → `Owner = null`.

---

## 1. Initial State

**DO:** Place all 4 tools on ground in scene → Press Play
**EXPECT:**
- All tools show **WorldModel** (mesh visible on ground)
- All tools have **ViewModel hidden**
- No Animator playing

**Behind the scenes:** Each tool's `BaseHeldTool.OnEnable()` fires on scene load. `Owner == null` (not equipped yet), so it defaults to showing WorldModel. ViewModel stays hidden until `SwitchToSlot` equips it.

---

## 2. Pick Up Pickaxe

**DO:** Press `Space` (pickup first tool)
**EXPECT:**
- Pickaxe WorldModel **disappears** from ground
- Pickaxe ViewModel **appears** attached to ViewModelContainer (in front of camera)
- Pickaxe mesh visible as first-person view
- Animator is in **Idle** state (no animation playing)

---

## 3. Pickaxe Swing Animation

**DO:** Hold left-click
**EXPECT:**
- **Attack1** animation plays — pickaxe swings down then back up
- Animation completes in ~0.3s, blends back to Idle
- If still holding after cooldown (~1s), **swings again**
- Release left-click → no more swings after current finishes

**DO:** Tap left-click rapidly (faster than cooldown)
**EXPECT:**
- Only 1 swing per cooldown period — no spam
- Cooldown is `_attackCooldown` (default 1s)

---

## 4. Switch To Magnet

**DO:** Press `2` (switch to magnet)
**EXPECT:**
- Pickaxe ViewModel **disappears** instantly
- Magnet ViewModel **appears** in ViewModelContainer
- SelectionModeText on magnet shows **"Everything"**

---

## 5. Magnet Pull Visual

**DO:** Look at Grabbable cubes → hold right-click
**EXPECT:**
- Cubes within `_pullRadius` **fly toward** PullOrigin position
- SpringJoints visible in Scene view (not in Game view — physics only)
- Cubes jitter/bounce near pull origin (spring physics)
- Cubes follow player movement with slight lag (SmoothDamp)

**DO:** Release right-click
**EXPECT:**
- Cubes stay held (SpringJoints persist) — release doesn't drop

**DO:** Hold right-click again near more cubes
**EXPECT:**
- New cubes also grabbed — additive, doesn't drop existing

---

## 6. Magnet Launch + Drop

**DO:** With cubes held → press left-click
**EXPECT:**
- All held cubes **launch forward** with push force
- SpringJoints destroyed — cubes fly freely
- After ~3s, cubes disable interpolation (DroppedBodyInfo cooldown)

**DO:** Grab cubes again → press `R`
**EXPECT:**
- Cubes **drop gently** (low force) — fall mostly straight down

---

## 7. Magnet Mode Cycle

**DO:** Press `Q`
**EXPECT:**
- SelectionModeText changes: "Everything" → "ResourcesNotInFilter"

**DO:** Press `Q` again
**EXPECT:**
- Text: "ResourcesNotOnConveyors"

**DO:** Press `Q` again
**EXPECT:**
- Text wraps: "Everything"

---

## 8. Switch To MiningHat

**DO:** Press `4` (switch to MiningHat)
**EXPECT:**
- Magnet ViewModel **disappears** (held cubes dropped via OnDisable)
- MiningHat ViewModel **appears**
- ViewModelLight state matches `isOn` (default off)

---

## 9. MiningHat Light Toggle

**DO:** Press left-click
**EXPECT:**
- ViewModelLight **turns on** (visible light cone in Game view)
- WorldModelLight also on (but WorldModel is hidden since equipped)

**DO:** Press left-click again
**EXPECT:**
- Light **turns off**

---

## 10. Tool Drop Visual

**DO:** With any tool equipped → press `G`
**EXPECT:**
- ViewModel **disappears**
- WorldModel **appears** in front of camera
- Tool flies forward with velocity (Rigidbody)
- Tool lands on ground, physics settle
- Hotbar slot becomes empty

---

## 11. Re-Pick Dropped Tool

**DO:** Walk to dropped tool → press `Space` (or interact E key if wired)
**EXPECT:**
- WorldModel disappears from ground
- ViewModel appears in camera
- Tool back in hotbar slot

---

## 12. Toggle Same Slot (Deselect)

**DO:** Equip tool in slot 1 → press `1` again
**EXPECT:**
- ViewModel **disappears** — tool deselected
- Slot 1 highlight **off**
- No tool active — bare hands

**DO:** Press `1` again
**EXPECT:**
- ViewModel **re-appears** — tool re-selected
- Slot 1 highlight **on**

---

## Summary Checklist

- [ ] WorldModel visible on ground, ViewModel hidden when not equipped
- [ ] Pickup: WorldModel hides, ViewModel shows in ViewModelContainer
- [ ] Pickaxe: Attack1 animation plays on hold, respects cooldown
- [ ] Magnet: cubes pull toward PullOrigin on right-click hold
- [ ] Magnet: left-click launches, R drops gently
- [ ] Magnet: Q cycles mode, TMP text updates
- [ ] Magnet: SpringJoint break → cube auto-detaches
- [ ] MiningHat: left-click toggles light on/off
- [ ] Switch: previous ViewModel hides, new ViewModel shows
- [ ] Drop (G): ViewModel hides, WorldModel appears with forward velocity
- [ ] Toggle same slot: deselects (no ViewModel), press again re-equips