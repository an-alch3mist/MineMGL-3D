# Phase B — Player Controller + Inventory + Tools + Grabbing (15%)

## What It Looks Like When Running

```
Full FPS controller: walk, sprint, duck, jump, slope sliding.
Look around with mouse, FOV widens when sprinting.

Walk up to a dropped pickaxe on the ground → press E → it goes
into your hotbar. Press 1-0 to switch tools. Scroll wheel cycles.
Active tool shows as a view model (first-person hands).

Hold right-click on a physics cube → SpringJoint grabs it,
a LineRenderer rope connects you to the object. Move mouse to
drag it around. Click again to release. Object bounces naturally.

Equip pickaxe → hold left-click → swing animation plays,
delayed raycast hits world objects.

Equip magnet tool → hold right-click → nearby physics objects
fly toward you via spring joints. Left-click to launch them.
R to drop gently. Q to cycle grab mode.

FresnelHighlighter outlines whatever you're looking at.
Each system testable independently.
```

---

## Folder Structure

```
phase-b(New)/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnToolSwitched, OnItemPickedUp, etc.)
├── 1-Managers/
│   └── SubManager/
│       └── InventoryUI.cs                  → "I open and close the inventory panel"
├── 2-Data/
│   ├── SO_FootstepSoundDefinition.cs       → "I pair left/right footstep sounds"
│   ├── Field_InventorySlot.cs              → "I display one inventory slot"
│   ├── Interface/
│   │   ├── IIconItem.cs                    → "I have an inventory icon"
│   │   └── ISaveLoadableObject.cs          → "I can be saved/loaded (stub)"
│   ├── DataService/
│   │   └── InventoryDataService.cs         → "I manage all inventory slots" (nested: Slot)
│   └── Enums/
│       └── GlobalEnumsB.cs                 → "all Phase B enums: InteractionType, MagnetToolSelectionMode, SavableObjectID"
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   └── InventoryOrchestrator.cs        → "I wire inventory slot Field_ instances"
│   ├── Player/
│   │   ├── PlayerMovement.cs               → "I handle walk, sprint, duck, jump, slope sliding"
│   │   ├── PlayerCamera.cs                 → "I handle mouse look, FOV, camera bobbing"
│   │   ├── PlayerGrab.cs                   → "I grab physics objects with SpringJoint + LineRenderer"
│   │   ├── PlayerFootsteps.cs              → "I play footstep sounds based on movement"
│   │   ├── PlayerSpawnPoint.cs             → "I mark where the player spawns"
│   │   └── RigidbodyDraggerController.cs   → "I auto-release grab when SpringJoint breaks"
│   ├── Tool/
│   │   ├── BaseHeldTool.cs                 → "I'm the base class for all equippable tools"
│   │   ├── ToolPickaxe.cs                  → "I swing and raycast-hit with delay"
│   │   ├── ToolMagnet.cs                   → "I pull nearby physics objects via spring joints"
│   │   ├── ToolHammer.cs                   → "I pick up / pack placed buildings"
│   │   ├── ToolMiningHat.cs                → "I toggle a light on equip/unequip"
│   │   ├── ToolSupportsWrench.cs           → "I toggle building supports on/off"
│   │   ├── ToolResourceScanner.cs          → "I show resource info on raycast hit"
│   │   ├── ToolBuilder.cs                  → "I show ghost + place buildings (partial)"
│   │   └── ToolHardHat.cs                  → "I'm a separate tool extending ToolPickaxe"
│   ├── UIRelay/
│   │   └── UIEventRelay.cs                 → "I relay EventSystem events to Action callbacks"
│   ├── Physics/
│   │   ├── BasePhysicsObject.cs            → "I accumulate conveyor velocities for FixedUpdate"
│   │   ├── BaseSellableItem.cs             → "I have a base sell value"
│   │   ├── PhysicsSoundPlayer.cs           → "I play sound on collision impact"
│   │   └── PhysicsGib.cs                   → "I'm a debris piece that despawns after time"
│   └── FresnelHighlighter.cs               → "I outline whatever the player looks at"
├── 4-Utils/
│   ├── UtilsPhaseB.cs                      → "I hold physics helpers"
│   └── PhaseBLOG.cs                        → "I format inventory snapshots"
└── 5-Tests/
    ├── DEBUG_CheckB.cs                      → "I test InventoryDataService (plain C#)"
    ├── PlayerMovementTest.cs                → "I test WASD + jump + sprint"
    ├── PlayerGrabTest.cs                    → "I test SpringJoint grab on cubes"
    └── InventoryTest.cs                     → "I test add/remove/switch tools"
```

---

## Script Purpose — One Sentence Each

| Script | Purpose |
|--------|---------|
| `GameEvents.cs` | I deliver Phase B messages (tool switch, pickup, drop, inventory view) |
| `InventoryUI.cs` | I open and close the inventory panel |
| `SO_FootstepSoundDefinition.cs` | I pair left/right footstep sounds |
| `Field_InventorySlot.cs` | I display one inventory slot (icon, name, amount, selection) |
| `IIconItem.cs` | I'm a contract for items with inventory icons |
| `ISaveLoadableObject.cs` | I'm a stub contract for save/load (expanded Phase G) |
| `InventoryDataService.cs` | I manage all inventory slots — add/remove/switch/stack |
| `GlobalEnumsB.cs` | I hold all Phase B enums: InteractionType, MagnetToolSelectionMode, SavableObjectID |
| `InventoryOrchestrator.cs` | I wire Field_InventorySlot instances to InventoryDataService |
| `PlayerMovement.cs` | I handle walk, sprint, duck, jump, slope sliding |
| `PlayerCamera.cs` | I handle mouse look, FOV, camera bobbing, viewmodel bobbing |
| `PlayerGrab.cs` | I grab physics objects with SpringJoint + LineRenderer rope |
| `PlayerFootsteps.cs` | I play footstep sounds based on movement speed |
| `PlayerSpawnPoint.cs` | I mark where the player spawns |
| `BaseHeldTool.cs` | I'm the base class for all equippable tools |
| `ToolPickaxe.cs` | I swing and raycast-hit with delay |
| `ToolMagnet.cs` | I pull nearby physics objects via spring joints |
| `ToolHammer.cs` | I pick up / pack placed buildings |
| `ToolMiningHat.cs` | I toggle a light on equip/unequip |
| `ToolSupportsWrench.cs` | I toggle building supports on/off |
| `ToolResourceScanner.cs` | I show resource info on raycast hit |
| `ToolBuilder.cs` | I show ghost preview + place buildings (partial — Phase D completes) |
| `BasePhysicsObject.cs` | I accumulate conveyor velocities for FixedUpdate |
| `BaseSellableItem.cs` | I have a base sell value |
| `PhysicsSoundPlayer.cs` | I play sound on collision impact |
| `PhysicsGib.cs` | I'm a debris piece that despawns after time |
| `FresnelHighlighter.cs` | I outline whatever the player looks at |
| `RigidbodyDraggerController.cs` | I auto-release grab when SpringJoint breaks |
| `ToolHardHat.cs` | I'm a separate tool type extending ToolPickaxe |
| `UIEventRelay.cs` | I relay Unity EventSystem events to Action callbacks (drag-drop, pointer) |
| `UtilsPhaseB.cs` | I hold physics helpers (IgnoreAllCollisions, SimpleExplosion, SetLayerRecursively) |
| `PhaseBLOG.cs` | I format inventory + tool snapshots for test logging |

---

## Hand-Typing Order (Compile Groups)

### Group 1 — Pure Data (compiles alone, zero Unity dependency)
1. `GlobalEnumsB.cs`
2. `IIconItem.cs`
3. `ISaveLoadableObject.cs`

**STOP — compile. Zero errors expected.**

### Group 2 — DataService
4. `InventoryDataService.cs`

**STOP — compile. Run DEBUG_CheckB to verify add/remove/switch.**

### Group 3 — SO + Field
5. `SO_FootstepSoundDefinition.cs`
6. `Field_InventorySlot.cs`

**STOP — compile.**

### Group 4 — Physics Chain
7. `BasePhysicsObject.cs`
8. `BaseSellableItem.cs`
9. `PhysicsSoundPlayer.cs`
10. `PhysicsGib.cs`

**STOP — compile.**

### Group 5 — Tools
11. `BaseHeldTool.cs`
12. `ToolPickaxe.cs`
13. `ToolMagnet.cs`
14. `ToolHammer.cs`
15. `ToolMiningHat.cs`
16. `ToolSupportsWrench.cs`
17. `ToolResourceScanner.cs`
18. `ToolBuilder.cs`
19. `ToolHardHat.cs`

**STOP — compile.**

### Group 6 — GameEvents + Utils + UIRelay
20. `GameEvents.cs` (partial)
21. `UtilsPhaseB.cs`
22. `PhaseBLOG.cs`
23. `UIEventRelay.cs`

**STOP — compile.**

### Group 7 — Player Scripts
24. `PlayerMovement.cs`
25. `PlayerCamera.cs`
26. `PlayerGrab.cs`
27. `RigidbodyDraggerController.cs`
28. `PlayerFootsteps.cs`
29. `PlayerSpawnPoint.cs`
30. `FresnelHighlighter.cs`

**STOP — compile. Run PlayerMovementTest.**

### Group 8 — Orchestrator + SubManager
31. `InventoryOrchestrator.cs`
32. `InventoryUI.cs`

**STOP — compile. Run InventoryTest.**

### Group 9 — Tests
33. `DEBUG_CheckB.cs`
34. `PlayerMovementTest.cs`
35. `PlayerGrabTest.cs`
36. `InventoryTest.cs`
37. `ToolActionTest.cs`

**STOP — compile. Run all 5 vertical slice tests + 4 manual tests.**

---

## Vertical Slice Tests (`.cs` — automated bootstrap)

### 1. DEBUG_CheckB — InventoryDataService (Data-Level)

> This test proves InventoryDataService works as pure C# — zero scene, zero UI, zero tools.
> One GO, press keys, check the console. If this passes, your data layer is solid.

**What you need to type first:** `InventoryDataService.cs`, `GlobalEnumsB.cs`
**What you DON'T need:** Player, tools, UI, shop, interaction — nothing. Just the DataService.

**Step-by-step scene setup:**
1. Create a new empty scene in Unity
2. Create an Empty GO → name it `DEBUG_CheckB`
3. Add the `DEBUG_CheckB` component to it
4. No inspector wiring needed — the test creates its own DataService via `new`
5. Press Play

**How to test:**

| Key | What it does | What you should see in Console |
|-----|-------------|-------------------------------|
| `Space` | Builds 40 slots (10 hotbar + 30 extended) + logs snapshot | JSON with 40 slots, all empty, activeSlotIndex = 0 |
| `U` | TryAdd a mock tool at preferred slot | Logs which slot the tool was added to |
| `I` | Remove tool at slot 0 | Logs removal confirmation |
| `O` | SwitchTo slot 3 | Logs new activeSlotIndex = 3 |
| `P` | Log full snapshot | JSON with all 40 slots + current active index |

**Checklist:**
- [ ] Space → 40 slots created (10 hotbar + 30 extended)
- [ ] U → TryAdd places tool in first empty slot
- [ ] I → Remove nulls the slot
- [ ] O → SwitchTo changes activeSlotIndex
- [ ] P → Snapshot logs all slot states correctly
- [ ] Zero console errors

---

### 2. PlayerMovementTest — WASD + Jump + Sprint (UI-Level)

> This test proves the player can walk, jump, sprint, duck, and look around.
> No inventory, no tools — just movement physics on a floor with some walls.

**What you need to type first:** `PlayerMovement.cs`, `PlayerCamera.cs`, `PlayerFootsteps.cs`, `PlayerSpawnPoint.cs`, `GameEvents.cs`
**What you DON'T need:** Inventory, tools, shop, interaction, grab

**Step-by-step scene setup:**
1. Create an Empty GO → name it `Player`
2. Add `CharacterController` (height 2, radius 0.5, center 0,1,0)
3. Add `PlayerMovement` component
4. Add `PlayerCamera` component
5. Create child of Player → name `Camera` → add `Camera` component
6. Create child of Camera → name `ViewModelContainer` (empty — tools parent here later)
7. Wire `PlayerMovement`:

| Field | Drag From / Value |
|-------|-------------------|
| `_cc` | CharacterController (self) |
| `_playerCam` | Camera child |
| `_viewModelContainer` | ViewModelContainer child |
| `_groundLayer` | "Ground" layer |
| `_walkSpeed` | 4 |
| `_sprintSpeed` | 6 |

8. Wire `PlayerCamera`:

| Field | Drag From |
|-------|-----------|
| `_cam` | Camera child |
| `_movement` | PlayerMovement (on parent) |
| `_viewModelContainer` | ViewModelContainer child |

9. Create Floor — Plane at y=0, layer "Ground"
10. Create a few wall cubes (scale 3,3,1) to test collision
11. Create `PlayerMovementTest` GO → add `PlayerMovementTest` component
12. Create `PlayerSpawnPoint` GO at (0, 1, 0)
13. Press Play

**How to test:**

| Input | What should happen |
|-------|-------------------|
| `WASD` | Player moves in camera direction |
| `Space` | Jump (only works when grounded) |
| `Shift + W` | Sprint — faster + FOV widens slightly |
| `C` | Duck — height shrinks, camera lowers |
| `C` under ceiling | Release C → player can't stand up (blocked) |
| `Walk off ledge` | Gravity pulls down, lands on floor |
| `Walk on steep slope` | Slides downhill automatically |
| `M` | Menu open → WASD/mouse look disabled, cursor unlocked |
| `N` | Menu close → WASD/look re-enabled, cursor locked |
| `V` | Noclip toggle — fly through walls, Space/C for up/down |
| `Fall below y=-200` | Auto-respawn at PlayerSpawnPoint |

**Checklist:**
- [ ] WASD moves relative to camera facing
- [ ] Jump only works on ground
- [ ] Sprint increases speed + FOV widens
- [ ] Duck lowers height, blocked stand-up under ceiling
- [ ] Slope sliding past slope limit
- [ ] Gravity when airborne
- [ ] M → input frozen, cursor free. N → input restored, cursor locked
- [ ] V → noclip fly, Space/C up/down, Shift fast
- [ ] Fall respawn works
- [ ] Zero console errors

---

### 3. PlayerGrabTest — SpringJoint Grab (UI-Level)

> This test proves you can right-click to grab a physics object, drag it around with a rope,
> and release it. See `Manual/GrabRopeTest.md` for the full setup — this is the same scene.

**What you need to type first:** `PlayerGrab.cs`, `RigidbodyDraggerController.cs`, `PlayerMovement.cs`, `PlayerCamera.cs`, `GameEvents.cs`
**What you DON'T need:** Inventory, tools, shop

**Step-by-step scene setup:** Follow `Manual/GrabRopeTest.md` Steps 1-10 exactly — it has the full Player hierarchy, RigidbodyDragger setup, LineRenderer, and grabbable cubes. Then add a `PlayerGrabTest` GO with the test component.

**How to test:**

| Input | What should happen |
|-------|-------------------|
| `Right-click` on cube | SpringJoint connects, rope appears between hand and cube |
| `WASD` while holding | Cube drags behind you on the spring |
| `Right-click` again | Release — rope disappears, cube bounces |
| `Pull cube very far` | SpringJoint breaks → ForceRelease, rope disappears |
| `M` then right-click | Grab blocked (menu is open) |
| `N` then right-click | Grab works again |

**Checklist:**
- [ ] Right-click on Grabbable → SpringJoint connects, rope visible
- [ ] WASD moves player, cube follows on spring
- [ ] Right-click again → clean release, rope gone
- [ ] Pulling too far → joint breaks → auto-release
- [ ] Menu open blocks grab
- [ ] Zero console errors

---

### 4. InventoryTest — Add/Remove/Switch Tools (UI-Level)

> This test proves the full inventory UI: pick up tools, switch hotbar slots, drag-drop between
> slots, open extended panel, selected item info. See `Manual/InventoryUITest.md` for full UI setup.

**What you need to type first:** `InventoryDataService.cs`, `InventoryOrchestrator.cs`, `InventoryUI.cs`, `Field_InventorySlot.cs`, `UIEventRelay.cs`, `BaseHeldTool.cs`, `GameEvents.cs`
**What you DON'T need:** Player movement, grab, shop, interaction

**Step-by-step scene setup:** Follow `Manual/InventoryUITest.md` Steps 1-14 for the full Canvas + hotbar + extended panel + InventoryOrchestrator wiring. Then add an `InventoryTest` GO with the test component and wire `_testTools` to 2-3 BaseHeldTool instances in the scene.

**How to test:**

| Input | What should happen |
|-------|-------------------|
| `Space` | Fires RaiseToolPickupRequested → tool appears in hotbar slot 0 |
| `Space` again | Second tool → slot 1 |
| `1` | Switch to slot 0 (highlight moves) |
| `2` | Switch to slot 1 |
| `Scroll up/down` | Cycle through occupied hotbar slots |
| `G` | Drop active tool → WorldModel appears in world, slot empties |
| `Tab` | Open extended inventory panel |
| `Drag slot 0 → slot 15` | Tools swap positions |
| `Drag slot outside UI` | Tool drops to world |
| `Click slot in extended` | Selected item info panel shows name/desc/icon |
| `Click Equip button` | Equips tool + closes inventory |
| `Click Drop button` | Drops tool from info panel |
| `ESC` or `Tab` | Close inventory |

**Checklist:**
- [ ] Space picks up tool → icon appears in hotbar
- [ ] 1-0 keys switch active slot (highlight moves)
- [ ] Scroll cycles through occupied slots
- [ ] G drops tool → WorldModel visible, slot empties
- [ ] Tab opens/closes extended inventory
- [ ] Drag-drop swaps slots
- [ ] Drag outside UI → tool drops to world
- [ ] Selected item info shows name/desc/icon
- [ ] Equip button equips + closes panel
- [ ] Drop button drops + hides info
- [ ] Console logs every GameEvent fire
- [ ] Zero console errors

### 5. ToolActionTest — Pickaxe + Magnet + Hammer + MiningHat (UI-Level)

> This test proves all 4 main tools work: pickaxe swings, magnet pulls, hammer raycasts,
> mining hat toggles light. See `Manual/ToolViewModelTest.md` for full prefab setup.

**What you need to type first:** All tool scripts + PlayerMovement + PlayerCamera + InventoryOrchestrator + InventoryUI + GameEvents
**What you DON'T need:** Shop, interaction system, ore nodes, buildings

**Step-by-step scene setup:** Follow `Manual/ToolViewModelTest.md` Steps 1-5 for the full player + inventory + tool prefab setup. Key additions:
1. Create `ToolActionTest` GO → add `ToolActionTest` component
2. Wire `_testTools` list with 4 tool instances in scene (Pickaxe, Magnet, Hammer, MiningHat)
3. Place 5-6 cubes with `Rigidbody`, tag `Grabbable`, layer `Interact` (magnet pull targets)
4. Place 1 wall cube, layer `Interact`, no tag (pickaxe hit target)
5. Press Play

**How to test:**

| Input | What should happen |
|-------|-------------------|
| `Space` | Picks up Pickaxe → appears in hotbar slot 0, ViewModel shows |
| `U` | Picks up Magnet → slot 1 |
| `I` | Picks up Hammer → slot 2 |
| `O` | Picks up MiningHat → slot 3 |
| `1` | Switch to Pickaxe |
| `Hold left-click` (Pickaxe) | Swing animation plays → after 0.2s delay, raycast hits → cube gets force push |
| `Hold left-click` at wall | Swing hits wall → no error (no Rigidbody, no crash) |
| `2` | Switch to Magnet |
| `Hold right-click` (Magnet) | Nearby cubes fly toward you via SpringJoints |
| `Left-click` (Magnet) | All held cubes launch forward |
| `R` (Magnet) | All held cubes drop gently |
| `Q` (Magnet) | Selection mode cycles: Everything → NotInFilter → NotOnConveyors. TMP text updates on ViewModel |
| `3` | Switch to Hammer |
| `Left-click` (Hammer) | Raycast fires — no effect (buildings are Phase D) |
| `4` | Switch to MiningHat |
| `Left-click` (MiningHat) | Light toggles on/off on both WorldModel and ViewModel |
| `G` (any tool) | Drop active tool → WorldModel appears in world with forward velocity |

**Checklist:**
- [ ] Pickaxe: hold left-click → swing animation at cooldown rate
- [ ] Pickaxe: swing hits cube → force impulse applied
- [ ] Pickaxe: swing hits wall → no error
- [ ] Magnet: hold right-click → cubes pulled via SpringJoints
- [ ] Magnet: left-click → launch forward
- [ ] Magnet: R → gentle drop
- [ ] Magnet: Q → mode cycles, TMP updates
- [ ] Magnet: pull too far → SpringJoint breaks, auto-detach
- [ ] Hammer: left-click raycasts (no effect, Phase D)
- [ ] MiningHat: left-click → light on/off toggle
- [ ] G → tool drops with velocity
- [ ] 1-4 keys switch tools, correct ViewModel shows/hides
- [ ] Console logs every GameEvent fire
- [ ] Zero console errors

---

## Manual Tests (`5-Tests/Manual/*.md` — hands-on, no script)

> These `.md` files teach the system's internal flow AND test it visually. Each contains:
> - **Setup Guide** — beginner-level GO creation, prefab hierarchy, wiring tables
> - **How It Works** — data flow in plain English (which script → event → subscriber → GO state change)
> - **DO/EXPECT steps** — each step includes behind-the-scenes: which method runs, which event fires, which GOs activate/deactivate
> - **Checklist** — pass/fail items
>
> The reader should understand the full architecture by reading the manual test.

| # | File | What to verify |
|---|------|---------------|
| 1 | `InventoryUITest.md` | Full inventory UI: Canvas setup, slot prefab, drag-drop flow (which scripts fire, which GOs SetActive), info panel, equip/drop |
| 2 | `ToolViewModelTest.md` | ViewModel equip/unequip (OnEnable parents to ViewModelContainer), animation timing, magnet SpringJoint visuals |
| 3 | `GrabRopeTest.md` | SpringJoint + LineRenderer flow (PlayerGrab.GrabObject → dragger activates → rope enables), joint break → ForceRelease |
| 4 | `FresnelHighlightTest.md` | Highlight Plus outline on hover (raycast → HighlightEffect added at runtime), clear on look away |

---

## Art & Scene Work (Non-Script)

> These are Unity Editor tasks — assets to create, GOs to set up, inspector wiring.

### Animation Assets

| Asset | Type | Where Used | How to Create |
|-------|------|-----------|--------------|
| `ToolPickaxe_Attack1.anim` | Animation Clip | ToolPickaxe swing | Animate ViewModel child: rotate down 45°→up over 0.3s |
| `ToolPickaxe_Controller` | AnimatorController | ToolPickaxe ViewModel | Create controller, add state named `"Attack1"`, assign clip |
| `ToolMagnet_Attack1.anim` | Animation Clip | ToolMagnet pulse (optional) | Subtle scale pulse on fire |
| `ToolHammer_Attack1.anim` | Animation Clip | ToolHammer swing | Similar to pickaxe but heavier arc |

**Animator Controller State Machine (per tool):**

```
ToolPickaxe_Controller:

  ┌──────────┐    Play("Attack1")    ┌──────────┐
  │   Idle   │ ────────────────────► │ Attack1  │
  │ (empty)  │ ◄──────────────────── │ (swing)  │
  └──────────┘    HasExitTime=true   └──────────┘
                  ExitTime=1.0
                  TransitionDuration=0.1

  - Idle = default state (orange). Empty/no clip — tool just sits in hand.
  - Attack1 = swing clip. Plays once, auto-returns to Idle via HasExitTime.
  - No parameters needed — code calls .Play("Attack1", -1, 0f) directly.
  - TransitionDuration 0.1 = fast blend back to idle after swing finishes.

ToolHammer_Controller: same flow, different Attack1 clip (heavier arc).
ToolMagnet_Controller: optional — Attack1 = subtle pulse. Or skip entirely.
```

**Wiring:** Each tool prefab's ViewModel child GO needs:
1. `Animator` component → assign the controller
2. On the tool script, `_viewModelAnimator` → drag the Animator

### Audio Clips (Phase H wires these — list here so you know what to prepare)

| Clip | Triggered By | When |
|------|-------------|------|
| `Pickaxe_Swing` | `ToolPickaxe.SwingPickaxe()` | Every swing before delayed raycast |
| `Pickaxe_Hit_Node` | `ToolPickaxe.PerformAttack()` | Raycast hits IDamageable (Phase C) |
| `Pickaxe_Hit_World` | `ToolPickaxe.PerformAttack()` | Raycast hits non-damageable surface |
| `Tool_Pickup` | `InventoryOrchestrator.HandleToolPickup()` | Tool enters inventory |
| `Tool_Drop` | `InventoryOrchestrator.HandleDropActiveTool()` | Tool leaves inventory |
| `MiningHat_Toggle` | `ToolMiningHat.ToggleLight()` | Light on/off |
| `Magnet_Cycle` | `ToolMagnet.CycleSelectionMode()` | Selection mode changed |
| `Footstep_Left/Right` | `PlayerFootsteps.Update()` | Walking on ground |
| `Footstep_Water_Left/Right` | `PlayerFootsteps.Update()` | Walking in water |
| `Player_Respawn` | `PlayerMovement.RespawnPlayer()` | Player falls below y=-200 |
| `Physics_Impact` | `PhysicsSoundPlayer.OnCollisionEnter()` | Physics objects collide |

> All marked as `// Phase H:` stubs in scripts. No AudioSource/SoundManager needed until Phase H.

### Fresnel Highlight (URP — temporary, replace with Highlight Plus later)

> This is a **temporary** URP-native fresnel highlight using Shader Graph + Renderer Feature.
> When Highlight Plus is imported, replace the layer-swap logic in `FresnelHighlighter.cs`
> with `HighlightEffect.SetHighlighted(true/false)` per object.

**Step 1 — Create "Highlighted" layer:**
1. Edit → Project Settings → Tags and Layers
2. Add a new layer at slot 31 (or any free slot) → name it `"Highlighted"`

**Step 2 — Create Shader Graph: `Highlight_Fresnel_Additive`:**
1. Project panel → Create → Shader Graph → URP → Unlit Shader Graph
2. Name it `Highlight_Fresnel_Additive`
3. Add these properties (Blackboard):
   - `_Color` (Color, default cyan `0.25, 0.85, 1, 1`)
   - `_Power` (Float, default `2`, range 0.5–8)
   - `_Intensity` (Float, default `1.2`, range 0–3)
4. Build the graph:
   ```
   Fresnel Effect (Power = _Power)
       ↓
   Multiply (A = Fresnel output, B = _Intensity)
       ↓
   Multiply (A = above, B = _Color)
       ↓
   → Emission (on Fragment output)
   ```
5. Graph Inspector settings:
   - Surface Type: **Transparent**
   - Blending Mode: **Additive**
   - Render Face: **Both**
   - ZWrite: **Off**
   - ZTest: **LessEqual** (change to Always for xray in future)
6. Save the Shader Graph

**Step 3 — Create Material:**
1. Project panel → Create → Material
2. Name it `M_Highlight_Fresnel`
3. Assign the `Highlight_Fresnel_Additive` shader
4. Set defaults: Color = cyan (0.25, 0.85, 1), Power = 2, Intensity = 1.2

**Step 4 — URP Renderer Feature:**
1. Find your URP Renderer Data asset (Project Settings → Graphics → Scriptable Render Pipeline → click the renderer)
2. Add Renderer Feature → **Render Objects**
3. Configure:
   - Name: `FresnelHighlight`
   - Event: **AfterRenderingOpaques**
   - Filters → Queue: Opaque
   - Filters → Layer Mask: **Highlighted** (the layer from Step 1)
   - Overrides → Material: `M_Highlight_Fresnel`
   - Overrides → Depth → Write Depth: **Off**
   - Overrides → Depth → Depth Test: **LessEqual**

**Result:** Any object on the "Highlighted" layer gets a second render pass with the fresnel additive material — cyan rim glow on edges. `FresnelHighlighter.cs` swaps layers on raycast hit and restores them each frame.

**Color presets** (set on `FresnelHighlighter` inspector):

| Preset | Color | Used By |
|--------|-------|---------|
| `_toolColor` | Cyan (0.25, 0.85, 1) | Tools, terminals, crates |
| `_grabbableColor` | Cyan (0.25, 0.85, 1) | Grabbable physics objects |
| Phase D: `_buildingColor` | Cyan (0.25, 0.85, 1) | Buildings when holding hammer |
| Phase D: `_wrenchEnableColor` | Green (0.3, 1, 0.3) | Building supports can be enabled |
| Phase D: `_wrenchDisableColor` | Red (1, 0.3, 0.3) | Building supports can be disabled |

### Tool Prefabs (per tool type)

Each tool prefab needs this hierarchy:

```
ToolPickaxe (root)
├── WorldModel (visible when on ground, has Rigidbody + Collider, tag "Grabbable")
│   └── pickaxe_mesh
└── ViewModel (visible when equipped, has Animator)
    └── hands_with_pickaxe_mesh
```

**Inspector wiring per tool prefab:**
- `_worldModel` → WorldModel child
- `_viewModel` → ViewModel child
- `_viewModelAnimator` → Animator on ViewModel
- `_name` → "Pickaxe" / "Magnet" / etc.
- `_inventoryIcon` → sprite for hotbar
- `_savableObjectID` → matching enum value
- `_interactions` → SO_InteractionOption assets ("Take", "Destroy")

### Inventory Slot Prefab

```
InventorySlot (root, has Image for raycastTarget)
├── Background       — colored rectangle behind everything, changes color on select/hover
├── Icon             — tool sprite, disabled when slot is empty, enabled when occupied
├── NameText         — fallback text when tool has no icon sprite (shows tool name instead)
├── AmountText       — shows stack count (e.g. "5"), hidden when qty ≤ 1
├── OrangeBarThing   — thin colored bar at bottom, visible only on hotbar slots (hidden on extended)
└── HideWhenDragged  — wrapper around Icon+Text, hidden during drag so slot looks "picked up"
```

Attach `Field_InventorySlot` to root → wire all refs in inspector.
Root **must** have an `Image` component with `raycastTarget = true` for drag-drop events to fire.

### Layers & Tags

| Name | Type | Used By |
|------|------|---------|
| `Ground` | Layer | PlayerMovement slope/ground check |
| `Interact` | Layer | FresnelHighlighter + InteractionSystem raycast |
| `Grabbable` | Tag | PlayerGrab + FresnelHighlighter |

---

## Scene Setup

### Full Phase B Scene

1. **Player GO** (root)
   - Components: `CharacterController`, `PlayerMovement`, `PlayerCamera`, `PlayerGrab`, `PlayerFootsteps`
   - Wiring: `_playerCam` → Camera, `_cc` → CharacterController (self), `_groundCheck` → GroundCheck child, `_groundLayer` → "Ground", `_characterModel` → CharacterModel child, `_viewModelContainer` → ViewModelContainer, `_holdPosition` → HoldPosition, `_magnetToolPosition` → MagnetToolPosition, `_interactLayerMask` → "Interact", `_nightVisionLight` → NightVisionLight child, `_miningHatLight` → MiningHatLight child

2. **Camera** (child of Player)
   - Components: `Camera`
   - `PlayerCamera` wiring: `_cam` → this Camera, `_movement` → PlayerMovement on parent, `_viewModelContainer` → ViewModelContainer

3. **FresnelHighlighter** (on Camera GO or separate GO)
   - Wiring: `_cam` → Camera, `_interactLayerMask` → "Interact", all 5 HighlightProfile assets

4. **ViewModelContainer** (child of Camera)
   - Tools parent here when equipped

5. **HoldPosition** (child of Camera, offset forward ~1m)
   - Grab target position

6. **MagnetToolPosition** (child of Camera, offset forward ~0.5m)
   - Magnet pull origin target

7. **RigidbodyDragger** (child of Player, starts **inactive**)
   - Components: `Rigidbody` (isKinematic=true), `RigidbodyDraggerController`
   - Wiring: `_playerGrab` → PlayerGrab on parent

8. **LineRenderer** (on Player GO)
   - Material: simple unlit line, start/end width ~0.02

9. **GroundCheck** (child of Player, positioned at feet y=-1)

10. **CharacterModel** (child of Player)
    - Capsule or character mesh, scales with duck height

11. **NightVisionLight** (child of Player, default light)

12. **MiningHatLight** (child of Player, starts inactive, brighter)

13. **Canvas**
    - **HotbarPanel** (HorizontalLayoutGroup) — 10 slots
    - **ExtendedInventoryPanel** (GridLayoutGroup) — 30 slots, inside InventoryUI GO
    - **InventoryUI GO** (starts active, has `InventoryUI` component)
    - **SelectedItemInfoPanel** (name/desc/amount texts + icon Image + Equip/Drop buttons)
    - **DragGhostIcon** (Image + TMP_Text, starts inactive, high sibling index)
    - **BgUI GO**

14. **InventoryOrchestrator** GO
    - Wiring: `_hotbarContainer`, `_extendedContainer`, `_pfInventorySlot` (prefab), `_dragGhostIcon`, `_dragGhostImage`, `_dragGhostAmountText`, `_selectedItemInfo`, `_selectedItemNameText`, `_selectedItemDescText`, `_selectedItemAmountText`, `_selectedItemIcon`, `_equipButtonText`, `_equipButton`, `_dropButton`

15. **Floor** (Plane, layer "Ground")

16. **Test tool prefabs** — 2-3 tools (ToolPickaxe, ToolMagnet, ToolHammer) placed in scene with WorldModel visible, tag "Grabbable", layer "Interact"

17. **Grabbable cubes** — 3-4 cubes with `Rigidbody`, tag "Grabbable", layer "Interact"

18. **PlayerSpawnPoint** GO — position where player starts

---

## Modifications to Earlier Phases

| File (Phase) | How | Change | Why |
|-------------|-----|--------|-----|
| `GameEvents.cs` (A) | **partial extend** in `phase-b/0-Core/GameEvents.cs` | Add `OnToolSwitched`, `OnItemPickedUp`, `OnItemDropped`, `OnOpenInventoryView`, `OnCloseInventoryView`, `OnToolPickupRequested` | No modification to Phase A's file |
| `UIManager.cs` (A) | **direct modify** | Add `GameEvents.RaiseCloseInventoryView()` to `CloseAllSubManager()` | Inventory panel must close with all others |
| `InteractionSystem.cs` (A) | **direct modify** | Add check: `if (PlayerGrab has held object) return` before interact | Grab + interact conflict |
| `SimplePlayerController.cs` (A) | **replaced** | Delete — `PlayerMovement` + `PlayerCamera` supersede it | Split architecture |
| `StartingElevator.cs` (A½) | **direct modify** | Update `TeleportPlayer` to use `Singleton<PlayerMovement>.Ins` | New controller structure |

---

## Source vs Phase Diff

| What | Original Did | What We Did | Why |
|------|-------------|-------------|-----|
| Player controller | Single 888-line `PlayerController.cs` | Split into `PlayerMovement` + `PlayerCamera` + `PlayerGrab` + `FresnelHighlighter` | Each fits one sentence |
| Inventory data | `PlayerInventory.Items` (plain List) | `InventoryDataService` with `Slot` nested type | Testable via `new`, pure C# |
| Inventory UI | `InventorySlotUI` (193 lines, has drag logic + FindObjectOfType) | `Field_InventorySlot` (display only) + `InventoryOrchestrator` (wiring) | Separation of display from logic |
| Tool pickup | `FindObjectOfType<PlayerInventory>().TryAddToInventory()` | `GameEvents.RaiseToolPickupRequested(tool)` → Orchestrator subscribes | Decoupled, no FindObjectOfType |
| Tool drop | `FindObjectOfType<PlayerInventory>().RemoveFromInventory()` | Orchestrator handles drop → fires `RaiseItemDropped` | Owner chain, no FindObjectOfType |
| Tool Owner | `Owner = PlayerController` (set via GetComponent) | `Owner = PlayerMovement` (set by InventoryOrchestrator on equip) | Owner chain pattern |
| Outline logic | Inside `PlayerController.Update()` | Self-contained in `FresnelHighlighter.Update()` | One sentence per script |
| Settings reads | `Singleton<SettingsManager>.Ins.MouseSensitivity` | Hardcoded defaults (Phase H adds settings) | Settings system is Phase H |
| Sound calls | `Singleton<SoundManager>.Ins.PlaySound(...)` | Commented stubs `// Phase H: play sound` | Sound system is Phase H |
| Inventory panel | `InventoryUIManager` (singleton, 187 lines, mixed concerns) | `InventoryUI` (SubManager, lifecycle only) + `InventoryOrchestrator` | Separation |
| Keybinds | `PlayerInputActions` (Input System) | `Input.GetKeyDown` / `INPUT.K.InstantDown` | Keybind rebinding is Phase H |

---

## Systems & Testability

### Individual Systems

| # | System | Scripts | Decoupled Via |
|---|--------|---------|---------------|
| 1 | **Player Movement** | `PlayerMovement`, `PlayerCamera`, `PlayerFootsteps`, `PlayerSpawnPoint` | `OnMenuStateChanged`, `OnCamViewPunch` |
| 2 | **Player Grab** | `PlayerGrab`, `RigidbodyDraggerController` | `OnMenuStateChanged`, `OnToolEquipped` |
| 3 | **Inventory** | `InventoryUI`, `InventoryOrchestrator`, `InventoryDataService`, `Field_InventorySlot`, `UIEventRelay` | `OnToolPickupRequested`, `OnToolEquipped`, `OnToolSwitched`, `OnItemPickedUp`, `OnItemDropped`, `OnOpenInventoryView`, `OnCloseInventoryView` |
| 4 | **Tools** | `BaseHeldTool`, `ToolPickaxe`, `ToolMagnet`, `ToolHammer`, `ToolMiningHat`, `ToolSupportsWrench`, `ToolResourceScanner`, `ToolBuilder`, `ToolHardHat` | `RaiseToolPickupRequested` (fires on Take), Owner chain (set via event) |
| 5 | **Physics** | `BasePhysicsObject`, `BaseSellableItem`, `PhysicsSoundPlayer`, `PhysicsGib` | None — inheritance chain, self-contained |
| 6 | **Fresnel Highlight** | `FresnelHighlighter` | Standalone — raycasts from camera, no events needed |
| 7 | **UI Management** | `UIManager` (phase-All) | `OnMenuStateChanged`, `OnCloseAllSubManagers`, `OnOpenInventoryView` |

### Testability Matrix

| System | `.cs` Test | `Manual/*.md` | Needs other systems? |
|--------|-----------|---------------|---------------------|
| Inventory (data) | `DEBUG_CheckB` | — | **Nothing** — plain C# `new` |
| Player Movement | `PlayerMovementTest` | — | No inventory, no tools |
| Player Grab | `PlayerGrabTest` | `GrabRopeTest.md` | No inventory, no tools |
| Inventory (UI) | `InventoryTest` | `InventoryUITest.md` | No player movement, no grab |
| Tools | `ToolActionTest` | `ToolViewModelTest.md` | Needs player + inventory (tools equip via inventory) |
| Fresnel Highlight | — | `FresnelHighlightTest.md` | Needs player (camera raycast) |
| Player Camera | — | — | Covered by PlayerMovementTest (same GO) |
| PlayerFootsteps | — | — | Sound stubs — testable after Phase H |

**7 systems, 37 scripts, 5 `.cs` tests, 4 manual tests. Zero tight coupling between systems.**