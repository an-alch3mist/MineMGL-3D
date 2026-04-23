# PhaseMap — Full Build Roadmap

> Every phase is a self-contained vertical slice. Each system works standalone first, connects to others via GameEvents.
> Refer to `GOAL.md` for architecture rules, naming conventions, folder structure.
>
> **This plan evolves** — files may be added, split, or merged as implementation reveals needs.

---

## Overview

| Phase | Name | Weight | Cumulative | Difficulty | Status |
|-------|------|--------|------------|------------|--------|
| **A** | World Interaction + Shop Cart | 7% | 7% | Easy | In Progress |
| **A½** | The Mine — Environment & Elevator | 3% | 10% | Easy | Planned |
| **B** | Player Controller + Inventory + Tools + Grabbing | 15% | 25% | Hard | Planned |
| **C** | Mining & Ore System | 14% | 39% | Medium | Planned |
| **D** | Building & Conveyor System | 14% | 53% | Hard | Planned |
| **E** | Ore Processing Machines | 18% | 71% | Medium | Planned |
| **F** | Quest & Research System | 10% | 81% | Medium | Planned |
| **G** | Save/Load System | 8% | 89% | Hard | Planned |
| **H** | Sound, Settings & UI Polish | 5% | 94% | Easy | Planned |
| **I** | Contracts, World Events & Menus | 4% | 98% | Easy | Planned |
| **J** | Debug, Demo & Final Polish | 2% | 100% | Easy | Planned |

---

## Phase A — World Interaction + Shop Cart (7%)

### What It Looks Like

```
First-person player on a flat plane. Walk to a cube (shop terminal),
press E. Shop panel opens with category tabs, item list, cart.
Add items, adjust quantity, purchase. Items spawn near terminal.
Money updates on HUD. ESC closes shop, cursor re-locks.
Each system testable independently via vertical slice tests.
```

### Script Purpose

```
Singleton              → "I ensure one instance"
GameEvents             → "I deliver messages between systems"
EconomyManager         → "I own money"
UIManager              → "I report if any menu is open"
ShopUI                 → "I open and close the shop panel"
BgUI                   → "I show/hide blur when menus change"
SO_ShopItemDef         → "I define what a shop item IS"
SO_ShopCategory        → "I group items into a category"
SO_Interaction         → "I define one interaction option"
Field_ShopCategory     → "I display one category tab"
Field_ShopItem         → "I display one item row"
Field_ShopCartItem     → "I display one cart row"
WShopItem              → "I track what happened to one item this session"
ShopDataService        → "I manage all shop data + cart as a collection"
ShopUIOrchestrator     → "I wire UI fields to data and handle actions"
ShopTerminal           → "I'm an interactable that fires open shop event"
ShopSpawnPoint         → "I mark where purchased items spawn"
SimplePlayerController → "I handle WASD movement + mouse look"
InteractionSystem      → "I raycast from camera and trigger IInteractable"
InteractionWheelUI     → "I show radial buttons for multi-option interactions"
MoneyOrchestrator      → "I show money on HUD"
```

### Files (22 scripts)

```
0-Core/
├── Singleton.cs
└── GameEvents.cs

1-Managers/
├── EconomyManager.cs
├── UIManager.cs
└── SubManager/
    ├── ShopUI.cs
    └── BgUI.cs

2-Data/
├── SO_Interaction.cs
├── SO_ShopCategory.cs
├── SO_ShopItemDef.cs
├── Field_ShopCategory.cs
├── Field_ShopItem.cs
├── Field_ShopCartItem.cs
├── Interface/
│   └── IInteractable.cs
├── DataWrapper/
│   └── WShopItem.cs
└── DataService/
    └── ShopDataService.cs       (includes CartItem as nested class)

3-MonoBehaviours/
├── Orchestrator/
│   ├── ShopUIOrchestrator.cs
│   └── MoneyOrchestrator.cs
├── ShopTerminal.cs
├── ShopSpawnPoint.cs
├── SimplePlayerController.cs
├── InteractionSystem.cs
└── InteractionWheelUI.cs

4-Utils/
├── UtilsPhaseA.cs               (formatMoney extension, TrySpawnAtShopPoint)
└── PhaseALOG.cs                 (per-collection snapshot formatters)

5-Tests/
├── ShopUITest.cs                (UI-level vertical slice)
├── InteractionTest.cs           (interaction vertical slice)
├── PlayerControllerTest.cs      (player vertical slice)
├── DEBUG_Check.cs               (data-level test — plain C# instances)
└── Manual/
    ├── ShopUITest.md            (manual: cart flow, category tabs, Field_ prefab setup)
    └── InteractionWheelTest.md  (manual: radial buttons, single vs multi-option)
```

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| DEBUG_Check | ShopDataService (plain C# instance, zero dependency) | Everything else |
| ShopUITest | Full shop UI flow (Space/T/Y/L keys) | PlayerController, InteractionSystem, UIManager |
| InteractionTest | Raycast + IInteractable + wheel UI | ShopUI, EconomyManager, PlayerController |
| PlayerControllerTest | WASD + mouse look + cursor lock via events | ShopUI, InteractionSystem, EconomyManager |

### Original Source Reference

`ComputerShopUI.cs`, `ComputerTerminal.cs`, `ShopItemDefinition.cs`, `ShopCategory.cs`, `ShopItem.cs`, `ShopItemButton.cs`, `ShopCartItemButton.cs`, `ShopCategoryButton.cs`, `ShopSpawnPoint.cs`, `EconomyManager.cs`, `IInteractable.cs`, `Interaction.cs`, `InteractionWheelUI.cs`, `UIManager.cs`, `Singleton.cs`

---

## Phase A½ — The Mine: Environment & Elevator (3%)

### What It Looks Like

```
No more flat plane. The player starts inside an enclosed underground
mine room — rocky walls, dim lighting, industrial feel.

New Game → StartingElevator lowers the player down from above.
Elevator shakes with Perlin noise, landing particle plays on arrival.
Roof collider prevents jumping out during descent.

The mine floor has:
- Shop terminal (computer) against a wall
- ShopSpawnPoints near the elevator shaft (items drop from above)
- Tunnel openings leading to mining areas (empty for now)
- Basic lighting (point lights, ambient)

Everything from Phase A still works — just inside a proper mine.
```

### Script Purpose

```
StartingElevator → "I lower the player into the mine on scene start"
CameraShaker     → "I add ambient Perlin noise sway + view punch to camera"
```

### Files

```
3-MonoBehaviours/
├── StartingElevator.cs          — code-driven elevator descent with shake
└── CameraShaker.cs              — Perlin noise camera sway + view punch

0-Core/
└── GameEvents.cs                — Modify: add OnElevatorLanded, OnGamePaused, OnGameUnpaused

5-Tests/
└── Manual/
    └── ElevatorDescentTest.md   (manual: elevator lowers, shake, particles, view punch)

Scene work:
├── Mine environment             — ProBuilder/terrain, lighting, colliders
└── Reposition Phase A objects   — terminal + spawn points inside mine
```

### Original Source Reference

`StartingElevator.cs`, `MainMenuCameraShaker.cs`

---

## Phase B — Player Controller + Inventory + Tools + Grabbing (15%)

### What It Looks Like

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

### Script Purpose

```
PlayerMovement         → "I handle walk, sprint, duck, jump, slope sliding"
PlayerCamera           → "I handle mouse look, FOV, camera bobbing"
PlayerGrab             → "I grab physics objects with SpringJoint + LineRenderer"
PlayerInventory        → "I manage hotbar + extended inventory slots"
BaseHeldTool           → "I'm the base class for all equippable tools"
ToolPickaxe            → "I swing and raycast-hit with delay"
ToolMagnet             → "I pull nearby physics objects via spring joints"
ToolHammer             → "I pick up / pack placed buildings"
ToolBuilder            → "I show ghost preview and place buildings (partial — Phase D completes)"
InventoryOrchestrator  → "I wire inventory slot Field_ instances"
Field_InventorySlot    → "I display one inventory slot"
FresnelHighlighter     → "I outline whatever the player looks at"
WInventorySlot         → "I track what's in one inventory slot this session"
InventoryDataService   → "I manage all inventory slot data"
```

### Files

```
0-Core/
└── GameEvents.cs                    — Modify: add OnToolSwitched, OnItemPickedUp, OnItemDropped

1-Managers/
├── UIManager.cs                     — Modify: add inventory panel check to IsInAnyMenu()
└── SubManager/
    └── InventoryUI.cs               — toggle inventory panel (lifecycle + toggle only)

2-Data/
├── SO_FootstepSoundDefinition.cs
├── Field_InventorySlot.cs           — display-only: slot icon, name, amount, selection state
├── Interface/
│   ├── IIconItem.cs                 — interface for items with inventory icons
│   └── ISaveLoadableObject.cs       — stub interface (expanded Phase G)
├── DataWrapper/
│   └── WInventorySlot.cs            — runtime slot state
├── DataService/
│   └── InventoryDataService.cs      — manages all slots, add/remove/switch/stack
└── Entities/
    ├── MagnetToolSelectionMode.cs    — enum
    ├── SavableObjectID.cs            — enum stub (expanded Phase G)
    └── HighlightStyle.cs             — serializable struct

3-MonoBehaviours/
├── Orchestrator/
│   └── InventoryOrchestrator.cs     — wires Field_InventorySlot instances
├── PlayerMovement.cs                — WASD, sprint, duck, jump, gravity, slope
├── PlayerCamera.cs                  — mouse look, FOV, camera bob, view model bob
├── PlayerGrab.cs                    — SpringJoint grab + LineRenderer rope
├── PlayerFootsteps.cs
├── PlayerSpawnPoint.cs
├── BasePhysicsObject.cs
├── BaseSellableItem.cs
├── PhysicsSoundPlayer.cs
├── PhysicsGib.cs
├── FresnelHighlighter.cs            — Highlight Plus wrapper
├── BaseHeldTool.cs                  — base class for all tools
├── ToolPickaxe.cs
├── ToolMagnet.cs
├── ToolHammer.cs
├── ToolMiningHat.cs
├── ToolSupportsWrench.cs
├── ToolResourceScanner.cs
└── ToolBuilder.cs                   — partial (placement logic in Phase D)

4-Utils/
├── UtilsPhaseB.cs                   — physics helpers
└── PhaseBLOG.cs                     — inventory + tool snapshot formatters

5-Tests/
├── PlayerMovementTest.cs            — WASD + jump + sprint without inventory/tools
├── PlayerGrabTest.cs                — grab physics cubes without inventory
├── InventoryTest.cs                 — add/remove/switch tools without player
├── ToolActionTest.cs                — pickaxe swing, magnet pull/launch/cycle, hammer, hat
├── DEBUG_CheckB.cs                  — InventoryDataService plain C# test
└── Manual/
    ├── InventoryUITest.md           (manual: drag-drop, hotbar↔extended, info panel, slot prefab setup)
    ├── ToolViewModelTest.md         (manual: equip/unequip ViewModel swap, animation timing)
    ├── GrabRopeTest.md              (manual: SpringJoint + LineRenderer rope visual)
    └── FresnelHighlightTest.md      (manual: outline on hover tools/grabbables, clears on look away)
```

> **Note:** Original `PlayerController.cs` (888 lines) is split into `PlayerMovement`, `PlayerCamera`, `PlayerGrab` — each fits one sentence. Original `InventorySlotUI` (203 lines with business logic) becomes `Field_InventorySlot` (display only) + `InventoryOrchestrator` (wiring).

### Modifications to Earlier Phases

| File (Phase) | How | Change | Why |
|-------------|-----|--------|-----|
| `GameEvents.cs` (A) | **partial extend** — `phase-b/0-Core/GameEvents.cs` | Add `OnToolSwitched`, `OnItemPickedUp`, `OnItemDropped` | No modification to Phase A's file |
| `UIManager.cs` (A) | **direct modify** — needs `[SerializeField]` | Add inventory panel ref + `IsInAnyMenu()` check + Tab key routing | Inspector field required |
| `InteractionSystem.cs` (A) | **direct modify** | Add check: skip interact if player is grabbing | Grab + interact conflict |
| `SimplePlayerController.cs` (A) | **replaced** | Delete — `PlayerMovement` + `PlayerCamera` supersede it | Split architecture |
| `StartingElevator.cs` (A½) | **direct modify** | Update `TeleportPlayer` to reference new controller | New controller structure |

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| DEBUG_CheckB | InventoryDataService (plain C# instance) | Everything else |
| PlayerMovementTest | WASD + jump + sprint + cursor lock | Inventory, tools, shop |
| PlayerGrabTest | SpringJoint grab on physics cubes | Inventory, tools, shop |
| InventoryTest | Add/remove/switch tools in hotbar UI | PlayerMovement, shop, interaction |

---

## Phase C — Mining & Ore System (15%)

### What It Looks Like

```
You walk into a mine tunnel from the starting room.
Glowing ore nodes embedded in the walls/floor — different
colors for Iron (grey), Gold (yellow), Copper (orange), Coal (black).

Equip pickaxe from hotbar → hold left-click:
  - Swing animation plays
  - 0.2s delay, then raycast hits the node
  - Particle sparks fly from impact point
  - Node health bar decreases
  - After 3-4 hits, node shatters:
    → 2-4 ore pieces fly out with random velocity
    → Pieces bounce and roll on the ground (Rigidbody physics)
    → Break particle burst plays
    → Node disappears permanently (position saved for persistence)

Ore pieces on the ground:
  - Grabbable with hand (right-click SpringJoint from Phase B)
  - Pullable with magnet tool (Phase B)
  - Each has ResourceType (Iron, Gold, etc.) + PieceType (Ore, Crushed, etc.)
  - Random mesh variant + slight scale variation for visual variety
  - Random price multiplier (0.9x–1.1x)

AutoMiner placed at a node → rotates continuously, spawns ore
on a timer. Probability-based (80% default). Rate adjustable.

SellerMachine (trigger volume) → ore enters → waits 2s →
money increases → ore returns to pool.

With 500+ ore pieces active, OreLimitManager kicks in:
  - UI warning appears
  - Auto-miner spawn rate slows down
  - At 2000+ moving objects, spawning blocks entirely

OrePiecePoolManager recycles all ore — zero Instantiate/Destroy
after initial pool warmup. Smooth performance.
```

### Script Purpose

```
OreNode              → "I'm a breakable rock that drops ore pieces when mined"
OrePiece             → "I'm a physical resource object with type + piece type"
OreManager           → "I clean up invalid ore pieces (round-robin)"
OrePiecePoolManager  → "I recycle ore objects to avoid GC spikes"
OreLimitManager      → "I throttle spawning when too many physics objects exist"
AutoMiner            → "I spawn ore on a timer at a node"
SellerMachine        → "I sell ore that enters my trigger for money"
OreDataService       → "I manage ore pool, weighted drops, resource descriptions"
```

### Files

```
0-Core/
└── GameEvents.cs                        — Modify: add OnOreMined, OnOreSold, OnOreLimitChanged

1-Managers/
└── OreManager.cs                        — singleton: round-robin ore cleanup

2-Data/
├── SO_ResourceDescription.cs
├── SO_AutoMinerResourceDefinition.cs
├── SO_WeightedOreChance.cs
├── SO_WeightedNodeDrop.cs
├── Interface/
│   └── IDamageable.cs
├── DataWrapper/
│   └── WOrePiece.cs                     — runtime ore state (resource type, piece type, polish %)
├── DataService/
│   └── OreDataService.cs               — pool management, weighted drops, resource queries
└── Entities/
    ├── ResourceType.cs                  — enum
    ├── PieceType.cs                     — enum
    ├── OrePieceKey.cs
    └── OrePieceEntry.cs

3-MonoBehaviours/
├── OreNode.cs
├── OrePiece.cs
├── OrePiecePoolManager.cs
├── OreLimitManager.cs
├── AutoMiner.cs
├── SellerMachine.cs
├── ParticleManager.cs
└── PhysicsLimitUIWarning.cs

4-Utils/
├── UtilsPhaseC.cs
└── PhaseCLOG.cs

5-Tests/
├── OreTest.cs                           — spawn/mine/sell flow without player
├── DEBUG_CheckC.cs                      — OreDataService plain C# test
└── Manual/
    ├── MiningFlowTest.md                (manual: hit node → particles → shatter → ore pieces fly)
    ├── AutoMinerVisualTest.md           (manual: rotator spins, ore spawns on timer, probability)
    └── SellerMachineTest.md             (manual: ore enters trigger → waits → money increases)
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `0-Core/GameEvents.cs` (A) | Add `OnOreMined`, `OnOreSold`, `OnOreLimitChanged` | Quest system (Phase F) subscribes to these |

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| DEBUG_CheckC | OreDataService (weighted drops, pool logic) | Everything else |
| OreTest | Spawn ore, mine node, sell at machine | Player (use test controls instead) |

---

## Phase D — Building & Conveyor System (15%)

### What It Looks Like

```
Open shop → buy "Conveyor Belt" → a crate spawns near elevator.
Walk to crate, press E → "Take" → goes into hotbar as ToolBuilder.

Equip the conveyor tool from hotbar:
  - A transparent green ghost of the conveyor belt follows
    your camera aim, snapped to a 1m world grid
  - Move mouse → ghost slides along grid positions
  - Look at invalid spot (overlapping another building) →
    ghost turns red
  - Press R → ghost rotates 90°
  - Press Q → ghost mirrors (for L/R variants)
  - Place near another conveyor → auto-snaps input→output
    (tests 4 rotations, picks best alignment)
  - Left-click → real conveyor belt instantiates at ghost position
  - Tool quantity decreases. At 0, tool is consumed.

Conveyor belt in the world:
  - Ore pieces that touch the belt trigger get pushed forward
    via physics velocity in FixedUpdate
  - Place multiple belts end-to-end → ore flows along the line
  - Belt has visual texture scroll (ConveyorRenderer)

Equip hammer → look at any placed building:
  - FresnelHighlighter outlines it in cyan
  - Press E → interaction wheel: "Take" or "Pack"
  - Take → building goes back into inventory as ToolBuilder
  - Pack → building becomes a crate on the ground

Buildings on uneven ground:
  - Modular scaffolding legs raycast downward
  - Legs spawn dynamically to reach the floor
  - Toggle supports on/off with wrench tool
```

### Files

```
1-Managers/
└── BuildingManager.cs               — singleton: grid placement, ghost preview

2-Data/
├── SO_BuildingInventoryDefinition.cs
├── Interface/
│   └── (uses existing IInteractable)
├── DataService/
│   └── BuildingDataService.cs       — placement validation, conveyor snap detection
└── Entities/
    ├── CanPlaceBuilding.cs          — enum
    ├── PlacementNodeRequirement.cs  — enum
    ├── SupportType.cs               — enum
    └── BuildingRotationInfo.cs

3-MonoBehaviours/
├── BuildingObject.cs
├── BuildingPlacementNode.cs
├── BuildingCrate.cs
├── ModularBuildingSupports.cs
├── ScaffoldingSupportLeg.cs
├── BaseModularSupports.cs
├── ConveyorBelt.cs
├── ConveyorBeltManager.cs
├── ConveyorRenderer.cs
├── ConveyorSoundSource.cs
└── ToolBuilder.cs                   — Modify: complete placement logic

4-Utils/
└── UtilsPhaseD.cs
5-Tests/
├── BuildingTest.cs                  — place/rotate/snap without player
├── ConveyorTest.cs                  — conveyor flow without player
└── Manual/
    ├── BuildingPlacementTest.md     (manual: ghost preview, grid snap, rotation, conveyor snap)
    ├── ConveyorFlowTest.md          (manual: belt texture scroll, ore flow, splitter routing)
    └── ScaffoldingTest.md           (manual: scaffolding legs raycast, spawn dynamically, wrench toggle)
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `ToolBuilder.cs` (B) | Complete placement logic — grid snap, ghost, conveyor snap | Phase B creates partial; Phase D finishes |
| `SO_ShopItemDef.cs` (A) | Add `SO_BuildingInventoryDefinition` field | Links shop items to building data |
| `ShopUIOrchestrator.cs` (A) | Handle BuildingCrate spawning in purchase flow | Buildings spawn as crates |

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| BuildingTest | Place/rotate/snap buildings via test controls | Player, shop, ore |
| ConveyorTest | Ore flows along belt chain | Player, shop |

---

## Phase E — Ore Processing Machines (18%)

### What It Looks Like

```
Full factory pipeline:
AutoMiner → Conveyor → Crusher → Furnace → Shaping → Polish → Sort → Package → Sell
Each machine is self-contained with trigger-based I/O.
Advanced conveyors: splitters, blockers, routing.
DepositBox: animated bucket elevator.
Player builds fully automated ore-to-money pipeline.
```

### Script Purpose

```
CastingFurnace    → "I smelt crushed ore by majority type into ingots"
CrusherMachine    → "I crush ore into 2x smaller pieces"
RollingMill       → "I flatten ingots into plates"
PolishingMachine  → "I gradually polish ore pieces to increase value"
SorterMachine     → "I route ore to different outputs by type"
PackagerMachine   → "I box loose ore into BoxObject containers"
DepositBox        → "I animate a bucket elevator for selling"
```

### Files

```
2-Data/
├── SO_CastingFurnaceRecipe.cs
├── SO_CastingFurnaceMoldRecipeSet.cs
├── DataWrapper/
│   └── WBoxContents.cs              — runtime box state
└── Entities/
    ├── CastingMoldType.cs           — enum
    └── BoxContentEntry.cs

3-MonoBehaviours/
├── CastingFurnace.cs
├── CastingFurnaceCoalInput.cs
├── CastingFurnaceInteractionHandler.cs
├── CastingFurnaceMoldArea.cs
├── BlastFurnace.cs
├── RollingMill.cs
├── PipeRoller.cs
├── RodExtruder.cs
├── ThreadingLathe.cs
├── PolishingMachine.cs
├── CrusherMachine.cs
├── ClusterBreaker.cs
├── ShakerTable.cs
├── SorterMachine.cs
├── BulkSorter.cs
├── PackagerMachine.cs
├── DepositBox.cs
├── RapidAutoMiner.cs
├── OreAnalyzer.cs
├── ConveyorBlocker.cs
├── ConveyorSplitterT2.cs
├── RollerSplitter.cs
├── RoutingConveyor.cs
├── BoxObject.cs
├── BaseBasket.cs
├── SorterFilterBasket.cs
└── Hopper.cs

4-Utils/
└── PhaseELOG.cs
5-Tests/
├── MachineTest.cs                   — test individual machines with spawned ore
└── Manual/
    ├── FurnaceUITest.md             (manual: coal gauge, mold placement, liquid animation)
    ├── DepositBoxTest.md            (manual: bucket elevator animation, tier visual)
    └── MachinePipelineTest.md       (manual: Crusher→Furnace→Mill→Polish→Sort→Package end-to-end)
```

### Modifications to Earlier Phases

None — all new machine scripts. Each is self-contained with trigger/collision-based I/O.

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| MachineTest | Individual machine I/O (spawn ore at input, check output) | Player, shop, quests |

---

## Phase F — Quest & Research System (10%)

### What It Looks Like

```
Quest HUD shows active quest. Quests chain: mine → sell → unlock → research.
Quest Tree UI (Q key). Research Tree UI (separate tab).
Progression loop: Mine → Sell → Quest → Unlock → Build → Research → Repeat.
```

### Script Purpose

```
QuestManager       → "I manage quest lifecycle (activate, progress, complete)"
ResearchManager    → "I manage research items (spend tickets, unlock)"
QuestDataService   → "I manage quest collections + progress tracking"
WQuest             → "I track one quest's progress this session"
QuestOrchestrator  → "I wire quest tree Field_ instances"
Field_QuestItem    → "I display one quest in the tree"
Field_ResearchItem → "I display one research item"
```

### Files

```
0-Core/
└── GameEvents.cs                    — Modify: add OnQuestCompleted, OnQuestActivated, OnResearchCompleted

1-Managers/
├── QuestManager.cs
├── ResearchManager.cs
├── UIManager.cs                     — Modify: add quest tree to IsInAnyMenu() + Q key routing
└── SubManager/
    └── QuestTreeUI.cs               — toggle quest tree panel

2-Data/
├── SO_QuestDefinition.cs
├── SO_ResearchItemDefinition.cs
├── Field_QuestItem.cs
├── Field_ResearchItem.cs
├── DataWrapper/
│   └── WQuest.cs
├── DataService/
│   └── QuestDataService.cs         — quest collections, progress, requirement checks
└── Entities/
    ├── QuestID.cs                   — enum
    ├── TriggeredQuestRequirementType.cs — enum
    ├── QuestRequirement.cs          — base class
    ├── ResourceQuestRequirement.cs
    ├── TriggeredQuestRequirement.cs
    ├── TimedQuestRequirement.cs
    ├── UnlockResearchQuestRequirement.cs
    └── ShopItemQuestRequirement.cs

3-MonoBehaviours/
├── Orchestrator/
│   ├── QuestOrchestrator.cs
│   └── ResearchOrchestrator.cs
└── QuestHud.cs

4-Utils/
└── PhaseFLOG.cs
5-Tests/
├── QuestTest.cs                     — activate/progress/complete quests without ore/machines
├── DEBUG_CheckF.cs                  — QuestDataService plain C# test
└── Manual/
    ├── QuestTreeUITest.md           (manual: tree layout, connection lines, state colors, rewards)
    ├── ResearchTreeUITest.md        (manual: research buttons, prerequisites, cost, tickets)
    └── QuestHudTest.md              (manual: active quest cards on HUD, progress updates, complete → remove)
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `0-Core/GameEvents.cs` (A) | Add `OnQuestCompleted`, `OnQuestActivated`, `OnResearchCompleted` | Decoupled quest/research notifications |
| `1-Managers/UIManager.cs` (A) | Add quest tree to `IsInAnyMenu()` + Q key priority routing | Quest tree opens only when not in shop/pause |
| `ShopDataService.cs` (A) | Quest completion unlocks shop items via events | Quests unlock shop items |

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| DEBUG_CheckF | QuestDataService (plain C# instance) | Everything else |
| QuestTest | Activate/progress/complete quests via test controls | Player, ore, machines, shop |

---

## Phase G — Save/Load System (8%)

### What It Looks Like

```
Press ESC → Pause Menu appears (game time freezes).
FPS capped to 50 while paused (saves GPU).

Click "Save Game":
  - Screen briefly captures a JPG screenshot
  - "Auto-saving..." warning appears on HUD
  - JSON file written atomically (write to .tmp, rename)
  - Backup .bak created before overwriting
  - Save includes:
    • Every tool in inventory (position, slot, custom data)
    • Every placed building (position, rotation, supports)
    • Every ore piece in world (position, rotation, scale,
      mesh ID, resource type, piece type, polish %)
    • Player position + rotation
    • Money, research tickets
    • All quest progress (completed + active + counters)
    • Shop purchase history
    • Destroyed ore node positions
    • World events (explosions, etc.)
    • Total play time

Click "Load Game":
  - All existing objects destroyed
  - Scene reloaded
  - Every saved object reinstantiated from prefab lookup
  - Player teleported to saved position
  - Economy/quests/research restored
  - Destroyed nodes re-destroyed

Auto-save runs every 5 minutes (configurable):
  - Shows "Auto-saving..." warning briefly
  - Same atomic write process

Save file format: versioned JSON (version 15).
Backward compatible from version 4+.
Legacy save migration from old folder structure.
```

### Files

```
1-Managers/
├── SavingLoadingManager.cs          — singleton: save, load, auto-save, versioning
└── AutoSaveManager.cs

2-Data/
├── Interface/
│   ├── ISaveLoadableObject.cs
│   ├── ISaveLoadableBuildingObject.cs
│   ├── ISaveLoadableStaticBreakable.cs
│   ├── ISaveLoadableWorldEvent.cs
│   └── ICustomSaveDataProvider.cs
├── DataService/
│   └── SaveDataService.cs           — serialize/deserialize all systems, prefab lookup
└── Entities/
    ├── SaveFile.cs, SaveEntry.cs, SaveFileHeader.cs
    ├── SavableObjectID.cs           — enum (expanded from Phase B stub)
    ├── SavableWorldEventType.cs     — enum
    ├── WorldEventEntry.cs
    └── ShopPurchases.cs

3-MonoBehaviours/
├── SaveFileScreenshotCamera.cs
├── Field_SaveFileButton.cs          — display-only save file row
└── AutoSavingWarning.cs

4-Utils/
└── PhaseGLOG.cs
5-Tests/
├── SaveLoadTest.cs                  — save/load cycle without full gameplay
└── Manual/
    └── SaveLoadUITest.md            (manual: save file list, screenshot, load/delete/rename)
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `BaseHeldTool.cs` (B) | Add `ISaveLoadableObject` interface | Tools persist across saves |
| `BuildingObject.cs` (D) | Add `ISaveLoadableBuildingObject` interface | Buildings persist |
| `OreNode.cs` (C) | Add `ISaveLoadableStaticBreakable` interface | Broken nodes persist |
| `AutoMiner.cs` (C) | Add `ICustomSaveDataProvider` interface | Save on/off state |
| `CastingFurnace.cs` (E) | Add `ICustomSaveDataProvider` interface | Save coal, mold types |
| `EconomyManager.cs` (A) | Add `SetMoney()` for load | Restore money |
| `ShopDataService.cs` (A) | Add `ShopPurchases` tracking | Persistent purchase history |
| `QuestManager.cs` (F) | Add `LoadFromSaveFile()` | Restore quest progress |
| `ResearchManager.cs` (F) | Add `LoadFromSaveFile()` | Restore research progress |

### Vertical Slice Tests

| Test | Tests what independently | NOT required |
|------|------------------------|-------------|
| SaveLoadTest | Save → load → verify state matches | Only needs a minimal scene with a few objects |

---

## Phase H — Sound, Settings & UI Polish (5%)

### What It Looks Like

```
Full audio: pickaxe, ore, conveyors, machines, footsteps, UI, elevator.
SoundManager pools 30 AudioSources. Distance culling.
Settings menu: sensitivity, FOV, volume, keybinds, display mode.
All settings persist via PlayerPrefs.
```

### Files

```
1-Managers/
├── SoundManager.cs                  — singleton: pooled AudioSources, distance culling
├── SettingsManager.cs               — singleton: PlayerPrefs for all settings
├── KeybindManager.cs                — singleton: rebindable Input System
├── UIManager.cs                     — Modify: add PauseMenu + ESC routing
└── SubManager/
    ├── PauseMenuUI.cs               — toggle pause, save/load/settings buttons
    └── SettingsUI.cs                — toggle settings panel

2-Data/
├── SO_SoundDefinition.cs
├── Field_SettingSlider.cs
├── Field_SettingToggle.cs
├── Field_SettingKeybind.cs
└── Entities/
    └── KeybindEntry.cs

3-MonoBehaviours/
├── Orchestrator/
│   └── SettingsOrchestrator.cs      — wires settings Field_ instances
├── SoundPlayer.cs
├── LoopingSoundPlayer.cs
├── LoopingSoundFader.cs
├── ResolutionSetting.cs
├── DisplayModeSetting.cs
└── KeybindTokenText.cs

4-Utils/
└── PhaseHLOG.cs
5-Tests/
├── SoundTest.cs                     — play sounds without gameplay
└── Manual/
    ├── SettingsUITest.md            (manual: slider drag, toggle, keybind rebind, resolution)
    └── PauseMenuTest.md             (manual: pause/unpause, time freeze, FPS cap, save/load buttons)
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `PlayerCamera.cs` (B) | Read `SettingsManager.Ins` for sensitivity/FOV/bob | Settings become configurable |
| `PlayerMovement.cs` (B) | Replace `Input.GetKeyDown` → Input System | Keybinds rebindable |
| `InteractionSystem.cs` (A) | Replace `KeyCode.E` → Input System action | Interact key rebindable |
| `ShopUIOrchestrator.cs` (A) | Add sound calls on purchase/add/remove | UI sounds |
| `StartingElevator.cs` (A½) | Wire SoundPlayer for descent sound | Elevator audio |
| `Machines/*.cs` (E) | Add processing sounds | Machine audio |
| `ToolPickaxe.cs` (B) | Add swing/hit sounds | Mining audio |

---

## Phase I — Contracts, World Events & Menus (4%)

### What It Looks Like

```
Contracts terminal: accept contracts, fill boxes, deposit for money.
World objects: dynamite, breakable crates, editable signs, water, fire.
Main menu: new game, load game (save browser), settings, quit.
```

### Files

```
1-Managers/
├── ContractsManager.cs
├── UIManager.cs                     — Modify: add contracts panel + ESC close
└── SubManager/
    └── ContractsUI.cs               — toggle contracts panel

2-Data/
├── SO_ContractDefinition.cs
├── Field_ContractInfo.cs
├── DataWrapper/
│   └── WContractInstance.cs
├── DataService/
│   └── ContractDataService.cs
└── Entities/
    └── (grouped contract entries)

3-MonoBehaviours/
├── Orchestrator/
│   └── ContractOrchestrator.cs
├── ContractsTerminal.cs
├── ContractSellTrigger.cs
├── DetonatorExplosion.cs
├── DetonatorTrigger.cs
├── DetonatorBuySign.cs
├── BreakableCrate.cs
├── EditableSign.cs
├── ExtinguishableFire.cs
├── WaterVolume.cs
├── MainMenu.cs
├── LoadingMenu.cs
├── NewGameMenu.cs
├── MapSelectButton.cs
└── EditTextPopup.cs

4-Utils/
└── PhaseILOG.cs
5-Tests/
├── ContractTest.cs                  — accept/fill/deposit contract without full gameplay
└── Manual/
    ├── ContractsUITest.md           (manual: contract cards, active/inactive, fill, claim)
    └── MainMenuTest.md              (manual: new game, map select, load, elevator anim)
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `UIManager.cs` (A) | Add contracts panel + ESC routing | Contracts UI in menu state |
| `StartingElevator.cs` (A½) | Wire to SceneWasLoadedFromNewGame check | Only lower on new game |
| `PauseMenuUI.cs` (H) | Add save/load file browser | Full pause menu |

---

## Phase J — Debug, Demo & Final Polish (2%)

### What It Looks Like

```
Dev mode: type "shaftmaster" → debug keys (noclip, money, unlock all, time scale).
Error popup. Version display. Demo mode restrictions. Visual polish.
Game is feature-complete.
```

### Files

```
1-Managers/
├── DebugManager.cs
├── VersionManager.cs
├── LevelManager.cs
└── DemoManager.cs

2-Data/
└── Entities/
    └── LevelInfo.cs

3-MonoBehaviours/
├── DebugOreSpawner.cs
├── ToolDebugSpawnTool.cs
├── DisplacementMeshGenerator.cs
├── VertexPainter.cs
├── DecalDestroyer.cs
├── ErrorMessagePopup.cs
└── InfoMessagePopup.cs

5-Tests/
└── DebugTest.cs                     — verify debug keys work
```

### Modifications to Earlier Phases

| File (Phase) | Change | Why |
|-------------|--------|-----|
| `PlayerMovement.cs` (B) | Add noclip toggle (dev mode only) | Debug flight mode |
| `EconomyManager.cs` (A) | Add `UnlockAllShopItems()` | Dev shortcut |
| `ShopDataService.cs` (A) | Show debug categories when dev mode active | Debug items visible |

---

## Dependency Chain

```
Phase A ─── foundation (interaction, shop, economy, events)
   │
   ▼
Phase A½ ── mine environment, elevator descent
   │
   ▼
Phase B ─── player, inventory, tools, grabbing, physics, highlighting
   │
   ├──► Phase C ─── ore, mining, pooling, selling
   │       │
   │       ▼
   │    Phase D ─── buildings, conveyors, grid placement
   │       │
   │       ▼
   │    Phase E ─── processing machines (full factory pipeline)
   │
   ├──► Phase F ─── quests, research (can start after B, benefits from C-E)
   │
   ▼
Phase G ─── save/load (needs all above systems to exist)
   │
   ▼
Phase H ─── sound, settings, keybinds (polish layer)
   │
   ├──► Phase I ─── contracts, world events, menus
   │
   └──► Phase J ─── debug, demo, final polish
```

---

## Parallel Execution (2 agents)

```
Wave:  1    2    3       4          5    6    7    8       9
       ┌──┐ ┌──┐ ┌────┐ ┌────┐    ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌────┐
Agent1 │ A│ │A½│ │ B  │ │ C  │───►│ D│ │ E│ │ G│ │ H│ │ I  │
       └──┘ └──┘ └────┘ ├────┤    └──┘ └──┘ └──┘ └──┘ ├────┤
Agent2                   │ F  │                         │ J  │
                         └────┘                         └────┘
```

Sequential: 11 phases. With 2 agents: **9 waves**.

---

> **For Future Agents:**
> 1. Read `GOAL.md` first — especially the "For Future Agents" section at the bottom.
> 2. Read the original source in `Scripts/Assembly-CSharp/` before writing any script. Match behavior 100%.
> 3. Phase A (shop) and Phase B (player/inventory) are the reference implementations — follow their patterns.
> 4. Use `partial class` for GameEvents extensions. Only direct-modify MonoBehaviours when `[SerializeField]` or method body changes are unavoidable.
> 5. Every script must fit one sentence. If it doesn't, split it.
> 6. The user types everything by hand — keep files concise, minimal public API, minimal private methods.
> 7. Every phase GUIDE.md must have: Script Purpose, Hand-Typing Order (with stop-and-test points), Vertical Slice Tests, Modifications table.
> 8. This roadmap evolves — files may be added, split, or merged as implementation reveals needs.

---

## Gap Audit — Missing Files & Features Per Phase

> Cross-referenced all ~269 original source files against PhaseMap. Below are files/features NOT listed above but present in the original source.
> **Critical** = breaks functionality if missing. **Important** = noticeable gap. **Polish** = can defer.

### Phase B — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `RigidbodyDraggerController.cs` | OnJointBreak → auto-releases grab | 15 | **Critical** | Without it, broken SpringJoint leaves orphan grab state |
| 2 | Selected item info panel + Equip/Drop buttons | `InventoryUIManager:108-148` | ~60 | **Critical** | Extended inventory is useless without item preview + equip/drop |
| 3 | `BaseHeldTool.Equip()` / `UnEquip()` virtual methods | `BaseHeldTool:139-148` | 10 | **Critical** | Called during SwitchTool — hides world/view models during transitions |
| 4 | `ToolMagnet.DetachBody(rb)` + DroppedBodyInfo cooldown | `ToolMagnet:8-13, 118-138, 222-256` | ~80 | **Critical** | Phase C OrePiece calls DetachBody when entering machines. Cooldown prevents interpolation leak |
| 5 | Mining hat dual-light system (nightVisionLight + miningHatLight on player) | `PlayerController:396-456` | ~60 | **Important** | Full flashlight behavior — toggles between hat light and fallback light |
| 6 | Noclip movement (V key toggle) | `PlayerController:365-394` | 30 | **Important** | Debug fly mode — no gravity, fly through walls |
| 7 | `ToolHardHat.cs` | Empty class extending ToolPickaxe | 4 | **Important** | Separate tool type in the game, distinct SavableObjectID |
| 8 | `ToolMagnet._selectionModeText` TMP display | `ToolMagnet:30, 80-83` | 5 | **Important** | Player can't see which grab filter is active |
| 9 | `InventoryItemPreview.cs` + `PreviewCameraOrbit.cs` + `InventoryItemPreviewHoverDetector.cs` | 3D spinning preview of selected tool | ~320 | **Polish** | Nice visual — extended inventory shows 3D render of item |
| 10 | `InventoryIconBaker.cs` | Editor-time icon generation from 3D models | ~100 | **Polish** | Dev workflow — not needed for gameplay |
| 11 | `PhysicsUtils.SetLayerRecursively()` | `PhysicsUtils:36-47` | 12 | **Polish** | Used by BuildingManager (Phase D), add to UtilsPhaseB |

### Phase C — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `DamageableOrePiece.cs` | OrePiece + IDamageable — breaks on collision damage | 69 | **Critical** | Ore clusters that break when hit hard enough |
| 2 | `OreLimitState.cs` | Enum: Regular/SlightlyLimited/HighlyLimited/Blocked | 8 | **Critical** | Used by OreLimitManager state machine |
| 3 | `ParticleCollision.cs` | Particle system collision callbacks | ~30 | **Important** | Mining particles that interact with world |
| 4 | `OreSled.cs` | Sled variant of ore transport | ~50 | **Polish** | Alternate transport method |

### Phase D — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `ChuteHatch.cs` | IInteractable — toggle open/closed on chute buildings | 149 | **Critical** | Chute system is a core building type |
| 2 | `ChuteTop.cs` | Top piece of chute system | ~50 | **Critical** | Companion to ChuteHatch |
| 3 | `RobotGrabberArm.cs` | Automated arm that grabs/moves ore by filter | 148 | **Important** | Automation building — grabs specific ore types |
| 4 | `ConveyorBatchRenderingComponent.cs` | Batch rendering optimization | ~50 | **Important** | Performance — many conveyors in scene |
| 5 | `ConveyorBeltShaker.cs` | Visual shake on conveyor belts | ~30 | **Polish** | Visual feedback — belt vibrates when active |
| 6 | `ConveyorBeltShakerHorizontal.cs` | Horizontal variant | ~30 | **Polish** | Variant of above |
| 7 | `ConveyorBlockerT2.cs` | Tier 2 blocker variant | ~50 | **Important** | Different from ConveyorBlocker in Phase E — this is a building variant |

### Phase E — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `ToolCastingMold.cs` | Equippable mold tool for furnace — swing + place on mold area | 86 | **Critical** | Required to use casting furnace |
| 2 | `RapidAutoMinerDrillBit.cs` | Drill bit tool with durability curve + mesh swap | 250 | **Critical** | Required for RapidAutoMiner to function |
| 3 | `CoalGaugeNeedle.cs` | Visual gauge needle on furnace showing coal level | ~20 | **Polish** | Visual feedback only |
| 4 | `DepositBoxCrusher.cs` | Crusher component inside deposit box | ~50 | **Important** | Deposit box variant with crushing |
| 5 | `PackagerMachineInteractor.cs` | IInteractable handler for packager | ~40 | **Important** | Lets player interact with packager machine |

### Phase F — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `QuestTreeQuestInfoUI.cs` | Quest info panel in tree — name, desc, requirements, rewards, activate/pause buttons | 112 | **Critical** | Can't preview quest details without this |
| 2 | `ResearchTreeSelectedResearchInfoUI.cs` | Selected research item info panel | ~80 | **Critical** | Can't see research details without this |
| 3 | `QuestPreviewRewardEntry.cs` | Reward line item in quest preview | ~20 | **Important** | Shows what you get for completing quest |
| 4 | `ShopItemQuestRequirementType.cs` | Enum for shop item quest requirement types | ~10 | **Important** | Used by ShopItemQuestRequirement |

### Phase G — Missing Save Data Entities

| # | File | Original | Lines | Priority | Why |
|---|------|----------|-------|----------|-----|
| 1 | `BaseHeldToolSaveData.cs` | IsInPlayerInventory, InventorySlotIndex | ~10 | **Critical** | Every tool needs this to save/load |
| 2 | `ToolMagnetSaveData.cs` | Extends above + SelectionMode | ~10 | **Critical** | Magnet tool saves its filter mode |
| 3 | `ToolBuilderSaveData.cs` | Extends above + BuildObjectID, Quantity | ~10 | **Critical** | Builder tool saves building type + count |
| 4 | `RapidAutoMinerDrillBitToolSaveData.cs` | Extends above + durability | ~10 | **Critical** | Drill bit saves wear state |
| 5 | `AutoMinerSaveData.cs` | IsOn, ResourceDefinition index | ~10 | **Critical** | Auto-miner saves on/off + resource config |
| 6 | `BuildingCrateSaveData.cs` | Definition ID, Quantity | ~10 | **Critical** | Crates save what building they contain |
| 7 | `CastingFurnaceSaveData.cs` | Coal, mold types, contents | ~15 | **Critical** | Furnace saves full state |
| 8 | `RoutingConveyorSaveData.cs` | IsClosed direction state | ~10 | **Important** | Routing conveyors save direction |
| 9 | `DetonatorExplosionSaveData.cs` | Explosion state | ~10 | **Important** | Explosions save armed/triggered/exploded |
| 10 | `EditableSignSaveData.cs` | Sign text | ~10 | **Important** | Signs save custom text |
| 11 | `OldSaveLoadMenu.cs` | Legacy save file migration | ~100 | **Polish** | Only needed for backward compat with old saves |

### Phase H — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `PlayerInputActions.cs` | Input System action map class (auto-generated) | ~500 | **Critical** | Entire rebindable keybind system depends on this |
| 2 | `BaseSettingOption.cs` | Base class for setting UI options | ~30 | **Important** | Common base for sliders/toggles/keybinds |
| 3 | `UIButtonSounds.cs` | Plays hover/click sounds on any UI button | ~30 | **Important** | Global UI sound feedback |
| 4 | `TMPBounceEffect.cs` | Text bounce animation | ~40 | **Polish** | Visual polish on important text |

### Phase I — Missing

| # | File / Feature | Original | Lines | Priority | Why |
|---|---------------|----------|-------|----------|-----|
| 1 | `DetonatorExplosionState.cs` | Enum: Armed/Triggered/Exploded | ~8 | **Critical** | Detonator state machine |
| 2 | `MenuData.cs` | Main menu data container | ~20 | **Important** | Main menu state management |
| 3 | `SteamNewsFetcher.cs` | Fetches Steam news API | ~80 | **Polish** | Steam integration — can skip if not on Steam |
| 4 | `SteamNewsItemUI.cs` | Displays one news item | ~40 | **Polish** | Companion to above |

### Utility Files — Not Assigned to Any Phase

| # | File | Lines | Suggested Phase | Priority | Why |
|---|------|-------|----------------|----------|-----|
| 1 | `GameManager.cs` | 23 | **A½** | **Important** | Singleton for GamePaused/GameUnpaused — our GameEvents already covers this, may be redundant |
| 2 | `PhysicsUtils.cs` | 49 | **B** (UtilsPhaseB) | **Critical** | Our UtilsPhaseB covers 2/3, missing `SetLayerRecursively` |
| 3 | `MathExtensions.cs` | ~30 | **B** (UtilsPhaseB) | **Important** | Math helpers used across phases |
| 4 | `TimeSince.cs` / `TimeUntil.cs` / `TimeUtil.cs` | ~60 total | **0-Core** | **Polish** | Time helper structs — nice but not blocking |
| 5 | `Vector3Utils.cs` | ~30 | **B** (or SPACE_UTIL) | **Polish** | May already be in SPACE_UTIL |
| 6 | `UniqueQueue.cs` | ~40 | **0-Core** | **Polish** | Data structure — used sparingly |
| 7 | `CollisionDisabler.cs` | ~20 | **B** | **Polish** | Temp collision disable utility |
| 8 | `TemporaryContinuousCollisionSetter.cs` | ~20 | **B** | **Polish** | Temp CCD setter |
| 9 | `CookieFlipbook.cs` | ~30 | **H** | **Polish** | Light cookie animation |
| 10 | `RandomizeTweenDelay.cs` | ~15 | **H** | **Polish** | DOTween delay randomizer |
| 11 | `GunAim.cs` / `GunShoot.cs` | ~100 | **J** (debug/cut) | **Polish** | Possibly cut content or debug tools |
| 12 | `JackOLantern.cs` / `HolidayType.cs` | ~50 | **J** (seasonal) | **Polish** | Halloween seasonal content |