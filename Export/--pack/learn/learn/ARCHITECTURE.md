# MINEMGL — Detailed Architecture Documentation

> **Purpose:** This document provides a comprehensive architectural analysis of the MINEMGL codebase — a first-person mining/factory simulation game built in Unity (C#). It is intended as a learning reference for understanding how each system is designed and how they interconnect.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [High-Level Architecture Diagram](#2-high-level-architecture-diagram)
3. [Core Architectural Pattern: Singleton Managers](#3-core-architectural-pattern-singleton-managers)
4. [System-by-System Breakdown](#4-system-by-system-breakdown)
   - 4.1 [Game Manager](#41-game-manager)
   - 4.2 [Economy System](#42-economy-system)
   - 4.3 [Save/Load System](#43-saveload-system)
   - 4.4 [Player Controller](#44-player-controller)
   - 4.5 [Player Inventory](#45-player-inventory)
   - 4.6 [Tool System](#46-tool-system)
   - 4.7 [Ore & Resource System](#47-ore--resource-system)
   - 4.8 [Building & Automation System](#48-building--automation-system)
   - 4.9 [Quest System](#49-quest-system)
   - 4.10 [Research System](#410-research-system)
   - 4.11 [Contracts System](#411-contracts-system)
   - 4.12 [Sound System](#412-sound-system)
   - 4.13 [UI System](#413-ui-system)
   - 4.14 [Settings & Input System](#414-settings--input-system)
   - 4.15 [Visual Feedback: Fresnel Highlighter](#415-visual-feedback-fresnel-highlighter)
   - 4.16 [Debug System](#416-debug-system)
   - 4.17 [Auto-Save System](#417-auto-save-system)
5. [Key Design Patterns & Techniques](#5-key-design-patterns--techniques)
6. [Critique & Thoughts](#6-critique--thoughts)
7. [File Index](#7-file-index)

---

## 1. Project Overview

**MINEMGL** is a **first-person mining/factory simulation game** built in **Unity (C#)**. The player mines ore nodes underground, processes raw ore through a chain of industrial machines (crushing, smelting, casting, polishing, packaging), builds conveyor-belt-driven automation lines, sells refined products for money, and progressively unlocks new equipment through a quest and research tree system.

### Project Structure

```
minemgl/
├── Assemblies/          # Compiled DLLs (Unity engine, third-party, game code)
├── Assets/              # Game assets
│   ├── AudioClip/       # Sound effects and music
│   ├── Mesh/            # 3D models (550 items)
│   ├── Shader/          # Custom shaders (125 items)
│   ├── Sprite/          # 2D sprites and icons (271 items)
│   ├── PrefabHierarchyObject/  # Prefab hierarchy data (297 items)
│   ├── Resources/       # Runtime-loadable resources
│   ├── Scenes/          # Game scenes
│   ├── TerrainData/     # Terrain heightmaps and splats
│   └── ...
└── Scripts/
    ├── Assembly-CSharp/          # ★ GAME CODE (~200 scripts)
    ├── Assembly-CSharp-firstpass/ # Plugin scripts
    ├── DOTween/                   # Animation tweening library
    ├── Unity.InputSystem/         # New Input System
    ├── Unity.TextMeshPro/         # Text rendering
    ├── UnityUIExtensions/         # Extended UI components
    └── ... (Unity engine modules)
```

### Third-Party Dependencies

| Library | Purpose |
|---------|---------|
| **DOTween / DOTweenPro** | Animation tweening |
| **TextMeshPro** | Advanced text rendering |
| **Unity UI Extensions** | Extended UI widgets |
| **Unity Post-Processing** | Visual post-effects |
| **Unity Input System** | Rebindable input |
| **Unity AI Navigation** | NavMesh (likely for future/unused AI) |
| **Unity Animation Rigging** | Procedural animation |
| **Unity Timeline** | Cutscene/sequence playback |
| **Unity Recorder** | Video/screenshot capture |

---

## 2. High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                       SINGLETON MANAGERS                         │
│  GameManager · EconomyManager · BuildingManager                 │
│  SavingLoadingManager · QuestManager · ResearchManager          │
│  ContractsManager · OreManager · OreLimitManager                │
│  SoundManager · UIManager · SettingsManager                     │
│  KeybindManager · DebugManager · AutoSaveManager                │
│  OrePiecePoolManager · InventoryUIManager · ParticleManager     │
└──────────────┬──────────────────────────────────┬───────────────┘
               │                                  │
      ┌────────▼────────┐             ┌───────────▼──────────┐
      │  PLAYER SYSTEMS  │             │ WORLD/BUILDING SYS   │
      │  PlayerController│             │ BuildingObject        │
      │  PlayerInventory │             │ BuildingPlacementNode │
      │  BaseHeldTool    │             │ ConveyorBelt          │
      │  ToolPickaxe     │             │ ModularSupports       │
      │  ToolMagnet      │             │ AutoMiner             │
      │  ToolBuilder     │             └───────────┬──────────┘
      │  ToolMiningHat   │                         │
      └────────┬─────────┘             ┌───────────▼──────────┐
               │                       │ ORE PROCESSING        │
               │                       │ OrePiece · OreNode    │
               │                       │ CastingFurnace        │
               │                       │ RollingMill           │
               │                       │ PolishingMachine      │
               │                       │ PackagerMachine       │
               │                       │ SellerMachine         │
               │                       │ DepositBox            │
               │                       │ SorterMachine         │
               │                       └──────────────────────┘
               │
      ┌────────▼──────────────┐
      │ UI SYSTEM              │
      │ ComputerShopUI         │
      │ ContractsTerminalUI    │
      │ QuestTreeUI · QuestHud │
      │ PauseMenu · SettingsMenu│
      │ InventorySlotUI         │
      │ InteractionWheelUI      │
      └────────────────────────┘
```

### Data Flow: Ore Lifecycle

```
OreNode (mined by pickaxe)
    │
    ▼
OrePiece [Ore] ──► CrusherMachine ──► OrePiece [Crushed]
    │                                       │
    ▼                                       ▼
AutoMiner (spawns ore)              CastingFurnace ──► OrePiece [Ingot/JunkCast]
                                            │
                                            ▼
                                    RollingMill ──► OrePiece [Plate]
                                    PipeRoller  ──► OrePiece [Pipe]
                                    RodExtruder ──► OrePiece [Rod]
                                            │
                                            ▼
                                    ThreadingLathe ──► OrePiece [ThreadedRod]
                                            │
                                            ▼
                                    PolishingMachine ──► OrePiece [Polished variant]
                                            │
                                            ▼
                                    PackagerMachine ──► BoxObject (packaged)
                                            │
                                            ▼
                                    SellerMachine ──► Money ($)
```

---

## 3. Core Architectural Pattern: Singleton Managers

### 3.1 The Singleton Base Class

**File:** `Singleton.cs`

```csharp
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null)
            Instance = this as T;
        else if (Instance != this)
        {
            Debug.Log($"{typeof(T)} singleton already exists, destroying duplicate");
            Object.Destroy(gameObject);
        }
    }
}
```

**How it works:**
- A generic MonoBehaviour singleton pattern
- First instance wins; duplicates are destroyed
- Accessed globally via `Singleton<T>.Instance`
- All major systems inherit from this

**Usage throughout codebase:**
```csharp
Singleton<EconomyManager>.Instance.AddMoney(100f);
Singleton<UIManager>.Instance.ShowPauseMenu(true);
Singleton<SoundManager>.Instance.PlaySoundAtLocation(sound, position);
```

### 3.2 Execution Order

The game uses `[DefaultExecutionOrder]` attributes to ensure proper initialization:

| Priority | Manager | Reason |
|----------|---------|--------|
| -1000 | `DebugManager` | Must exist before anything logs |
| -900 | `SavingLoadingManager` | Prefab/lookup dictionaries needed by everything |
| -100 | `GameManager`, `EconomyManager`, `QuestManager`, `ContractsManager` | Core gameplay systems |
| -90 | `ResearchManager` | Depends on economy |
| -10 | `SoundManager`, `KeybindManager` | Utility systems |
| -1 | `SettingsManager` | Applies saved preferences |
| 0 | (default) | Most gameplay components |
| 100 | `DepositBox` | Late init, subscribes to economy events |

---

## 4. System-by-System Breakdown

### 4.1 Game Manager

**File:** `GameManager.cs` (23 lines)

The simplest manager — a lightweight event hub for game pause/unpause.

```csharp
[DefaultExecutionOrder(-100)]
public class GameManager : Singleton<GameManager>
{
    public event Action GamePaused;
    public event Action GameUnpaused;

    public void OnGamePauseToggled(bool isPaused)
    {
        if (isPaused) GamePaused?.Invoke();
        else GameUnpaused?.Invoke();
    }
}
```

**Key idea:** Uses C# events (`event Action`) so other systems can subscribe without coupling. The `?.Invoke()` pattern safely handles no subscribers.

---

### 4.2 Economy System

**Files:** `EconomyManager.cs`, `ShopItem.cs`, `ShopItemDefinition.cs`, `ShopCategory.cs`, `ShopPurchases.cs`, `ComputerShopUI.cs`

**`EconomyManager`** is the singleton that owns the player's money and shop state.

**Core responsibilities:**
- Track `Money` (float) with change notifications via `event Action<float> OnMoneyUpdated`
- Manage all `ShopItem` instances (created from `ShopItemDefinition` ScriptableObjects)
- Group items into `ShopCategory` lists
- Lock/unlock items based on quests, research, or debug commands
- Format money strings with color tags for UI

**Key implementation details:**

```csharp
public float Money
{
    get => _money;
    private set
    {
        _money = value;
        OnMoneyUpdated?.Invoke(_money);  // Notify UI on every change
    }
}
```

- **Shop categories** can be holiday-specific (conditionally hidden)
- **Demo mode** locks certain items via `IsLockedInDemo` flag on definitions
- **Debug categories** only shown when dev mode is active
- **Duplicate detection** — uses `HashSet<ShopItemDefinition>` to prevent duplicate items across categories

---

### 4.3 Save/Load System

**File:** `SavingLoadingManager.cs` (853 lines — the largest manager)

This is the most architecturally significant system in the codebase.

#### Save File Format

JSON files stored in `{persistentDataPath}/Saves/`. Current version: **15**.

```
SaveFile
├── SaveVersion (int)
├── GameVersion (string)
├── SaveTimestamp (ISO 8601 string)
├── LevelID (string)
├── Money (float)
├── ResearchTickets (int)
├── TotalPlayTimeSeconds (double)
├── PlayerPosition / PlayerRotation (Vector3)
├── Entries[] ──── SaveEntry (general objects)
│   ├── SavableObjectID (enum)
│   ├── Position / Rotation (Vector3)
│   └── CustomDataJson (string)
├── BuildingObjects[] ──── BuildingObjectEntry
│   ├── (same as SaveEntry)
│   └── BuildingSupportsEnable (bool)
├── OrePieces[] ──── OrePieceEntry
│   ├── Position / Rotation / Scale (Vector3)
│   ├── MeshID (int)
│   ├── ResourceType / PieceType (enum)
│   └── PolishedPercent (float)
├── WorldEventEntries[] ──── WorldEventEntry
├── ShopPurchases
├── CompletedResearchItems[]
├── CompletedQuestsIDs[]
├── ActiveQuests[]
└── DestroyedStaticBreakablePositions[]
```

#### Key Architectural Decisions

**1. Prefab Lookup Dictionary**
```csharp
private Dictionary<SavableObjectID, GameObject> _lookup;
```
On `Awake()`, all savable prefabs are registered by their `SavableObjectID` enum value. This enables O(1) prefab retrieval during loading. Duplicates and missing IDs are validated with error logging.

**2. Atomic File Writes**
```csharp
private void WriteSaveAtomically(string fullFilePath, string json)
{
    string tmpPath = fullFilePath + ".tmp";
    File.WriteAllText(tmpPath, json);
    if (File.Exists(fullFilePath)) File.Delete(fullFilePath);
    File.Move(tmpPath, fullFilePath);
}
```
Prevents save corruption if the game crashes mid-write.

**3. Automatic Backups**
```csharp
private void TryBackupExistingSave(string fullFilePath)
{
    // Validates existing save is readable, then copies to .bak
    File.Copy(fullFilePath, fullFilePath + ".bak", overwrite: true);
}
```

**4. Version Compatibility**
```csharp
public bool IsSaveFileCompatible(int version) => version switch
{
    4 => true, 5 => true, ..., 15 => true,
    _ => false
};
```
Supports loading save files from version 4 through 15 with migration logic.

**5. Interface-Based Saving**

Four interfaces define what can be saved:

| Interface | Purpose | Example |
|-----------|---------|---------|
| `ISaveLoadableObject` | General objects with position/rotation/custom data | `BaseHeldTool`, `AutoMiner` |
| `ISaveLoadableBuildingObject` | Buildings with support state | `BuildingObject` |
| `ISaveLoadableStaticBreakable` | Destructible world geometry | `OreNode` |
| `ISaveLoadableWorldEvent` | One-time events | `DetonatorExplosion` |
| `ICustomSaveDataProvider` | Component-level custom JSON | `CastingFurnace` |

**6. The Save Process (simplified):**
```
1. Backup existing save file
2. Take screenshot coroutine
3. Find all ISaveLoadableObject in scene
4. Also include player inventory items
5. Serialize each to SaveEntry or BuildingObjectEntry
6. Serialize all OrePiece objects
7. Serialize world events, shop purchases, research, quests
8. Write JSON atomically
```

**7. The Load Process (simplified):**
```
1. Destroy all existing ISaveLoadableObject instances
2. Destroy all OrePiece instances
3. Process destroyed static breakable positions (remove ore nodes)
4. Instantiate SaveEntry objects from prefab lookup
5. Instantiate BuildingObjectEntry objects with support data
6. Instantiate OrePieceEntry objects (with mesh/scale/polish)
7. Restore world events
8. Restore economy, research, quests
9. Teleport player to saved position
10. Handle version migrations
```

---

### 4.4 Player Controller

**File:** `PlayerController.cs` (888 lines)

A comprehensive first-person character controller built on Unity's `CharacterController` component.

#### Movement System

```csharp
// Core movement in Update()
Vector3 moveDir = transform.right * MoveInput.x + transform.forward * MoveInput.y;
SelectedWalkSpeed = isSprinting ? SprintSpeed : (isDucking ? DuckSpeed : WalkSpeed);
CharacterController.Move(moveDir * SelectedWalkSpeed * Time.deltaTime);
```

**Features:**
- **Walk/Sprint/Duck** — Three speed tiers with smooth transitions
- **Jump** — `velocity.y = sqrt(JumpHeight * -2 * Gravity)`
- **Slope sliding** — 6 radial raycasts detect steep surfaces, applies slide velocity when all rays hit steep slopes
- **Noclip** — Dev mode flight, moves along camera forward vector
- **Duck** — Adjustable CharacterController height with ceiling-check before standing (prevents clipping)

#### Camera System

```csharp
// Mouse look
_xRotation -= mouseY * sensitivity;
_xRotation = Mathf.Clamp(_xRotation, -88f, 88f);
transform.Rotate(Vector3.up * mouseX);

// FOV — wider when sprinting
float desiredFOV = isSprinting ? baseFOV * 1.05f : baseFOV;
_currentFOV = Mathf.SmoothDamp(_currentFOV, desiredFOV, ref _fovVelocity, 0.1f);
```

#### Camera Bobbing

Sinusoidal head bob with three axes (vertical, pitch, yaw). Speed-proportional.

```csharp
float sinValue = Mathf.Sin(_bobbingCounter);
_bobbingVerticalOffset = SmoothDamp(target: BaseBobbingAmount * sinValue * speedRatio);
_bobbingPitch = SmoothDamp(target: sinValue * BaseBobbingPitchAmount * speedRatio);
_bobbingYaw = SmoothDamp(target: sinValue * BaseBobbingYawAmount * speedRatio * yawDir);
```

Yaw direction alternates each full cycle (`2π`) for a natural side-to-side sway.

#### View Model Bobbing (Separate System)

The held tool bobs independently from the camera with:
- **Vertical bob** — Up/down with movement
- **Pitch/Yaw bob** — Rotational sway
- **Jump/land bounce** — Offset on takeoff and landing
- **Look sway** — Tool lags behind mouse movement

All values use `Mathf.SmoothDamp` for smooth interpolation with NaN safety checks.

#### Object Grabbing

Physics-based grab using `SpringJoint`:

```csharp
_grabJoint = RigidbodyDragger.AddComponent<SpringJoint>();
_grabJoint.spring = 100f;
_grabJoint.damper = 25f;
_grabJoint.breakForce = 120f;
_grabJoint.connectedBody = heldRigidbody;
```

A `LineRenderer` draws a rope between the dragger and the grabbed object. On release, original drag/angular drag values are restored.

#### Interaction System

```csharp
// Raycast from camera center
Physics.Raycast(camera.position, camera.forward, out hit, interactRange, InteractLayerMask);

// Collect all IInteractable on hit object
List<IInteractable> interactables = hit.collider.GetComponentsInParent<IInteractable>();

// Single interaction = direct call; Multiple = show wheel UI
if (interactables.Count == 1 && !interactables[0].ShouldUseInteractionWheel())
    interactables[0].Interact(interactables[0].GetInteractions().FirstOrDefault());
else
    InteractionWheelUI.PopulateInteractionWheel(interactable);
```

---

### 4.5 Player Inventory

**File:** `PlayerInventory.cs` (418 lines)

- **10-slot hotbar** + **30-slot extended inventory** = 40 total slots
- Items are `BaseHeldTool` references in `List<BaseHeldTool>`
- Hotbar keys 1-0, scroll wheel cycles non-null slots
- **Stacking** for `ToolBuilder` items: checks existing stacks before creating new slots

**Tool input routing:**
```csharp
if (PrimaryAttack.WasPressedThisFrame()) ActiveTool.PrimaryFire();
if (PrimaryAttack.IsPressed())           ActiveTool.PrimaryFireHeld();
if (SecondaryAttack.WasPressedThisFrame()) ActiveTool.SecondaryFire();
if (SecondaryAttack.IsPressed())           ActiveTool.SecondaryFireHeld();
if (RotateObject.WasPressedThisFrame())    ActiveTool.Reload();
if (DropTool.WasPressedThisFrame())        ActiveTool.DropItem();
if (MirrorObject.WasPressedThisFrame())    ActiveTool.QButtonPressed();
```

**Slot swapping** via `SwapSlots(indexA, indexB)` — used by drag-and-drop UI.

---

### 4.6 Tool System

**Base class:** `BaseHeldTool.cs` — implements `IInteractable`, `ISaveLoadableObject`, `IIconItem`

#### World Model vs View Model

Every tool has two visual representations:
- **World Model** — Visible when dropped on the ground (physics-enabled)
- **View Model** — Visible in first-person when equipped (parented to `ViewModelContainer`)

```csharp
protected override void OnEnable()
{
    if (Owner == null)
    {
        HideViewModel();       // Show world model
        HideWorldModel(false);
    }
    else
    {
        HideWorldModel();       // Show view model
        HideViewModel(false);
        transform.parent = Owner.ViewModelContainer;
    }
}
```

#### Concrete Tools

| Tool | File | Primary Action | Secondary Action | Special |
|------|------|---------------|-----------------|---------|
| **ToolPickaxe** | `ToolPickaxe.cs` | Mine (held) — delayed raycast hit synced to animation | — | Can break ore into crushed pieces |
| **ToolMagnet** | `ToolMagnet.cs` | Launch held objects | Pull objects (held) — spring joint attach | 3 selection modes, R to drop gently |
| **ToolBuilder** | `ToolBuilder.cs` | Place building | — | Ghost preview, 90° rotation, snap detection |
| **ToolMiningHat** | `ToolMiningHat.cs` | — | — | Toggleable headlamp light |
| **ToolResourceScanner** | `ToolResourceScanner.cs` | Scan resources | — | Identifies resource types |
| **ToolSupportsWrench** | `ToolSupportsWrench.cs` | Toggle building supports | — | Enable/disable scaffolding |
| **ToolHammer** | `ToolHammer.cs` | Pick up buildings | — | Take/pack buildings |

#### ToolPickaxe Detail — Delayed Hit

The pickaxe uses a coroutine to delay the damage raycast after the swing animation starts:

```csharp
private void SwingPickaxe()
{
    ViewModelAnimator.Play("Attack1", -1, 0f);
    _swingSoundPlayer.PlaySound(_sound_swing);
    StartCoroutine(PerformAttack(0.2f));  // 200ms delay
}

private IEnumerator PerformAttack(float delaySeconds)
{
    yield return new WaitForSeconds(delaySeconds);
    // Now do the actual raycast and damage
    Physics.Raycast(camera.position, camera.forward, out hit, UseRange, HitLayers);
    hit.GetComponent<IDamageable>()?.TakeDamage(Damage, hit.point);
}
```

#### ToolMagnet Detail — Spring Joint Physics

The magnet creates per-object `SpringJoint` connections anchored to a kinematic follow point:

```csharp
GameObject anchor = new GameObject("MagnetAnchor");
anchor.transform.parent = PullOrigin;
anchor.AddComponent<Rigidbody>().isKinematic = true;

SpringJoint joint = anchor.AddComponent<SpringJoint>();
joint.connectedBody = targetRb;
joint.spring = 100f;
joint.damper = 25f;
joint.breakForce = 120f;
```

Three selection modes control what gets grabbed:
- **Everything** — All physics objects
- **Resources not in baskets** — Skips ore in `BaseBasket` containers
- **Resources not on conveyors** — Skips ore touching `ConveyorBelt`

#### ToolBuilder Detail — Building Placement

```csharp
private void Update()
{
    Vector3 buildPos = GetBuildPosition(camera);         // Raycast or max range
    Vector3Int gridPos = BuildingManager.GetClosestGridPosition(buildPos);  // Snap to 1m grid
    BuildingManager.UpdateGhostObject(gridPos, prefab, rotation, this);    // Show ghost
}

public override void PrimaryFire()
{
    if (BuildingManager.CanPlaceObject(...) == CanPlaceBuilding.Valid)
    {
        Instantiate(prefab, ghostPosition, ghostRotation);
        Quantity--;
        if (Quantity <= 0) Destroy(gameObject);
    }
}
```

---

### 4.7 Ore & Resource System

**Core files:** `OrePiece.cs`, `OreNode.cs`, `OreManager.cs`, `OrePiecePoolManager.cs`, `OreLimitManager.cs`

#### Resource Types (enum `ResourceType`)
Iron, Gold, Copper, Steel, Coal, Slag, and more.

#### Piece Types (enum `PieceType`)
Ore, Crushed, Ingot, Plate, Rod, Pipe, ThreadedRod, Gear, DrillBit, OreCluster, JunkCast.

#### OrePiece — The Central Data Object

`OrePiece` extends `BaseSellableItem` and is the physical representation of every resource in the game world. Each piece carries:

```csharp
public ResourceType ResourceType;
public PieceType PieceType;
public float PolishedPercent;      // 0 to 1
public float SievePercent;         // 0 to 1
public float RandomPriceMultiplier; // 0.9 to 1.1 (variety)
```

**Transformation prefab chain:**
```csharp
public GameObject CrushedPrefab;   // Ore → Crushed
public GameObject IngotPrefab;     // Crushed → Ingot (via furnace)
public GameObject PlatePrefab;     // Ingot → Plate
public GameObject PipePrefab;      // Ingot → Pipe
public GameObject RodPrefab;       // Ingot → Rod
public GameObject ThreadedPrefab;  // Rod → Threaded Rod
public GameObject PolishedPrefab;  // Any → Polished variant
```

**Key methods:**
- `TryConvertToCrushed()` — Spawns 2× crushed pieces, plays sound
- `ConvertToPlate()`, `ConvertToRod()`, `ConvertToPipe()`, `ConvertToThreaded()` — Shape transformations
- `AddPolish(float)` — Incremental polishing; at 100%, spawns polished prefab or swaps material
- `AddSieveValue(float)` — Incremental sieving; at 100%, weighted random output selection
- `CompleteClusterBreaking()` — Weighted random drops with configurable count range
- `SellAfterDelay(float)` — Coroutine: wait, then add sell value to economy

**Sell value calculation:**
```csharp
public override float GetSellValue()
{
    return Mathf.Round(BaseSellValue * RandomPriceMultiplier * 100f) / 100f;
}
```

#### Weighted Random Selection (used in OreNode, sieving, cluster breaking)

```csharp
float totalWeight = drops.Sum(d => d.Weight);
float roll = Random.value * totalWeight;
float cumulative = 0f;
foreach (var drop in drops)
{
    cumulative += drop.Weight;
    if (roll <= cumulative) return drop.OrePrefab;
}
```

#### OreNode — Static Mineable Objects

`OreNode` implements `IDamageable` and `ISaveLoadableStaticBreakable`.

```csharp
public void TakeDamage(float damage, Vector3 position)
{
    Health -= damage;
    if (Health <= 0f) BreakNode(position);
}

public void BreakNode(Vector3 position)
{
    int dropCount = Random.Range(MinDrops, MaxDrops + 1);
    for (int i = 0; i < dropCount; i++)
        OrePiecePoolManager.SpawnPooledOre(GetOrePrefab(), position, Quaternion.identity);
    
    MarkStaticPositionAsBroken();  // Persisted in save file
    Destroy(gameObject);
}
```

Destroyed positions are saved as `List<Vector3>` and checked on load to prevent respawning.

#### OrePiecePoolManager — Object Pooling

**Critical for performance** — with thousands of ore pieces, instantiation/destruction would cause GC spikes.

```csharp
private readonly struct OreKey : IEquatable<OreKey>
{
    private readonly int _resourceType;
    private readonly int _pieceType;
    private readonly int _polished;
    // Custom Equals + GetHashCode for dictionary key
}

private readonly Dictionary<OreKey, Queue<OrePiece>> _pools;
```

**Spawn:** Dequeue from pool or instantiate new. **Return:** Deactivate, reset all state, re-parent to pool root, enqueue.

```csharp
public void ReturnToPool(OrePiece piece)
{
    piece.gameObject.SetActive(false);
    piece.Rb.linearVelocity = Vector3.zero;
    piece.Rb.Sleep();
    piece.BasketsThisIsInside.Clear();
    piece.SievePercent = 0f;
    piece.CurrentMagnetTool = null;
    piece.PolishedPercent = piece.IsPolished ? 1f : 0f;
    piece.ClearTouchingConveyorBelts();
    piece.gameObject.tag = "Grabbable";
    _pools[key].Enqueue(piece);
}
```

#### OreLimitManager — Performance Self-Regulation

Periodically counts non-sleeping `Rigidbody` instances among ore pieces:

| State | Trigger | Auto-Miner Multiplier |
|-------|---------|----------------------|
| Regular | Below limit | 1.0× (normal) |
| SlightlyLimited | limit + 0-100 | 1.25× (slower) |
| HighlyLimited | limit + 100-200 | 1.5× (much slower) |
| Blocked | limit + 200+ | 2.0× (very slow, blocks spawning) |

---

### 4.8 Building & Automation System

**Files:** `BuildingManager.cs`, `BuildingObject.cs`, `BuildingPlacementNode.cs`, `ConveyorBelt.cs`, `ModularBuildingSupports.cs`

#### BuildingObject — The Core Building Component

Every placeable building has a `BuildingObject` component with:

```csharp
public SavableObjectID SavableObjectID;          // For save/load lookup
public BuildingInventoryDefinition Definition;    // ScriptableObject config
public Vector3 BuildModePlacementOffset;          // Offset from grid center
public PlacementNodeRequirement PlacementNodeRequirement;  // Attachment point type
public List<Transform> ConveyorInputSnapPositions;   // Snap alignment points
public List<Transform> ConveyorOutputSnapPositions;
```

**Interactions:** "Take" (add to inventory) and "Pack" (spawn as crate).

**BuildingSupports:** Buildings can have `ModularBuildingSupports` that raycast downward to dynamically spawn scaffolding legs. Togglable via wrench tool.

#### BuildingManager — Placement Logic

**Ghost object system:**
1. Instantiate building prefab as "ghost"
2. Set `IsGhost = true`, disable all MonoBehaviours except `BuildingObject`
3. Set to `BuildingGhost` layer, make rigidbodies kinematic
4. Swap all materials to ghost/invalid/requirement materials
5. Move ghost to follow player's aim point (snapped to grid)

**Collision checking:**
```csharp
// For each box collider on the ghost:
Physics.OverlapBox(center, halfExtents, rotation, layerMask);
// If any overlaps → CanPlaceBuilding.Invalid
```

**Conveyor snap detection:**
```csharp
// Test 4 rotations (0°, 90°, 180°, 270°) for each ghost input point:
for (int i = 0; i < 4; i++)
{
    Quaternion rot = Quaternion.Euler(0, i * 90, 0);
    // Transform ghost input point by rotation
    // Compare distance to neighbor output points
    if (distance < 0.25f) // Snap!
        possibleRotations.Add(rot);
}
// Pick rotation with most connections (frequency counting)
```

#### ConveyorBelt — Physics Item Transport

```csharp
public class ConveyorBelt : MonoBehaviour
{
    public float Speed = 0.8f;
    protected List<BasePhysicsObject> _physicsObjectsOnBelt;

    protected virtual void FixedUpdate()
    {
        for (int i = _physicsObjectsOnBelt.Count - 1; i >= 0; i--)
            _physicsObjectsOnBelt[i].AddConveyorVelocity(_pushVelocity, RetainYVelocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Add BasePhysicsObject (auto-add component if missing)
    }
}
```

Static list `AllConveyorBelts` enables global queries (e.g., magnet tool checking if ore is on a conveyor).

#### Processing Machines

| Machine | Input | Output | Mechanism |
|---------|-------|--------|-----------|
| **AutoMiner** | Time | OrePiece [Ore] | Timer-based spawn with probability. `AutoMinerResourceDefinition` drives config. |
| **CrusherMachine** | OrePiece [Ore] | OrePiece [Crushed] | Trigger-based, calls `TryConvertToCrushed()` |
| **CastingFurnace** | OrePiece [Crushed/Ingot] | OrePiece [Ingot/JunkCast] | Queue-based, majority resource type wins. Iron + Coal = Steel. Mixed = Slag. |
| **RollingMill** | OrePiece [Ingot] | OrePiece [Plate] | Calls `ConvertToPlate()` |
| **PipeRoller** | OrePiece [Ingot] | OrePiece [Pipe] | Calls `ConvertToPipe()` |
| **RodExtruder** | OrePiece [Ingot] | OrePiece [Rod] | Calls `ConvertToRod()` |
| **ThreadingLathe** | OrePiece [Rod] | OrePiece [ThreadedRod] | Calls `ConvertToThreaded()` |
| **PolishingMachine** | Any OrePiece | Polished variant | Gradual `AddPolish()` |
| **ShakerTable** | OrePiece | Sieved variant | Gradual `AddSieveValue()`, weighted random output |
| **ClusterBreaker** | OrePiece [OreCluster] | Multiple OrePiece | `CompleteClusterBreaking()`, configurable count |
| **PackagerMachine** | OrePiece | BoxObject | Packages into container |
| **SorterMachine** | Mixed OrePiece | Sorted outputs | Routes by ResourceType |
| **SellerMachine** | OrePiece/BoxObject | Money | Trigger-based, calls `SellAfterDelay()` |
| **DepositBox** | Visual | Visual | Animated bucket elevator, motor sounds |

#### CastingFurnace Detail — Resource Smelting Logic

```csharp
private ResourceType DetermineOutputResourceType(List<ResourceType> resources)
{
    // 1. Find most common resource type
    ResourceType mostCommon = resources.GroupBy(r => r)
        .OrderByDescending(g => g.Count())
        .First().Key;
    
    // 2. Iron + sufficient Coal → Steel
    if (mostCommon == ResourceType.Iron && CoalAmount > requiredCoal)
        return ResourceType.Steel;
    
    // 3. All same type → that type
    if (resources.All(r => r == mostCommon))
        return mostCommon;
    
    // 4. Mixed → Slag
    return ResourceType.Slag;
}
```

Each mold area has a recipe set mapping `(CastingMoldType, InputResourceType) → OutputPrefab`.

---

### 4.9 Quest System

**Files:** `QuestManager.cs`, `Quest.cs`, `QuestDefinition.cs`, `QuestRequirement.cs`

#### Architecture

```
QuestDefinition (ScriptableObject)  ──GenerateQuest()──►  Quest (runtime instance)
    ├── QuestID (enum)                                       ├── QuestRequirements[]
    ├── Prerequisites                                        ├── ShopItemsToUnlock[]
    ├── Reward definitions                                   ├── QuestsToAutoStart[]
    └── Requirement definitions                              └── RewardMoney / RewardResearchTickets
```

#### Quest Requirement Types

| Type | Trigger | Example |
|------|---------|---------|
| `ResourceQuestRequirement` | `OnResourceDeposited()` from SellerMachine | "Sell 10 Iron Ingots" |
| `TriggeredQuestRequirement` | `ActivateQuestTrigger()` from game events | "Collect a Pickaxe" |
| `TimedQuestRequirement` | Time-based | "Wait 5 minutes" |
| `UnlockResearchQuestRequirement` | Research completion | "Research Conveyor Belt" |
| `ShopItemQuestRequirement` | Shop purchase | "Buy an Auto-Miner" |

#### Quest Lifecycle

```
1. QuestDefinition.GenerateQuest() → creates Quest with requirements
2. Starting quest auto-activates in QuestManager.OnEnable()
3. QuestManager.Update() checks active quests:
   foreach (activeQuest)
       if (quest.IsCompleted())
           → Auto-start chained quests
           → Unlock shop items
           → Award money + research tickets
           → Fire QuestCompleted event
4. Resource quests track progress via OnResourceDeposited(type, piece, polish, count)
5. Triggered quests track progress via ActivateQuestTrigger(type, amount)
```

---

### 4.10 Research System

**Files:** `ResearchManager.cs`, `ResearchItemDefinition.cs`

- **Research tickets** — Currency earned from quest rewards
- **Research items** — ScriptableObjects with ticket cost, money cost, prerequisites
- `CompletedResearchItems` tracked as `List<SavableObjectID>`
- On research: deducts costs, calls `definition.OnResearched()`, fires event
- `MigrateNewResearchPrices()` — Recalculates tickets from complete quest history for save format migration

---

### 4.11 Contracts System

**Files:** `ContractsManager.cs`, `ContractDefinition.cs`, `ContractInstance.cs`

Separate from quests — a secondary objective system.

- Generated from `ContractDefinition` ScriptableObjects
- One contract active at a time
- `BoxObject` containers deposited at `ContractSellTrigger`
- Contract checks `ResourceQuestRequirement` entries against box contents
- Rewards are money-based, claimed manually

---

### 4.12 Sound System

**File:** `SoundManager.cs` (204 lines)

#### Object Pool Architecture

```csharp
private Queue<SoundPlayer> soundPlayersPool; // Pre-instantiated pool (default 30)

public void PlaySoundAtLocation(SoundDefinition def, Vector3 position)
{
    // Distance culling
    float sqrDist = (PlayerTransform.position - position).sqrMagnitude;
    if (sqrDist > (def.maxRange * 1.25f)²) return;
    
    SoundPlayer player = soundPlayersPool.Dequeue();
    player.transform.position = position;
    player.PlaySound(definition);
    // Player returns itself to pool when clip finishes
}
```

#### Spatial Conveyor Sound System

```csharp
private void UpdateConveyorSounds()
{
    // OverlapSphereNonAlloc to find nearby conveyors (max 128 hits)
    int hits = Physics.OverlapSphereNonAlloc(playerPos, radius, _conveyorHits, mask);
    
    // Deduplicate via HashSet
    // Sort by distance, keep N closest (insertion sort)
    // Assign audio sources to closest conveyors
    // Deactivate unused audio sources
}
```

Runs in `FixedUpdate` for consistent timing.

---

### 4.13 UI System

**File:** `UIManager.cs` (276 lines)

Central UI state manager. Key responsibilities:

- **Menu state tracking** — `IsInAnyMenu()`, `IsInComputerShop()`, `IsInPauseMenu()`, etc.
- **Cursor management** — Locked/hidden during gameplay, free in menus
- **Background blur** — Activated when any major menu is open
- **On-screen controls text** — Dynamically shows keybind hints for current tool
- **ESC key handling** — Toggle pause menu, close other menus
- **HUD toggle** — F1 (configurable) to show/hide HUD

#### Menu Hierarchy

```
UIManager
├── ComputerShopUI (in-world purchase terminal)
├── ContractsTerminalUI (contract management)
├── QuestTreeUI (quest/research tree)
├── PauseMenu (save, load, settings, quit)
│   ├── SettingsMenu
│   └── ErrorMessagePopup / InfoMessagePopup
├── InteractionWheelUI (radial interaction selector)
├── EditTextPopup (sign editing)
├── InventoryUIManager (hotbar + extended inventory)
└── QuestHud (active quest tracking)
```

---

### 4.14 Settings & Input System

#### SettingsManager

**File:** `SettingsManager.cs` — All preferences via `PlayerPrefs`:

| Setting | Default | Type |
|---------|---------|------|
| MouseSensitivity | 1.0 | float |
| DesiredFOV | 80 | float |
| CameraBobScale | 1.0 | float |
| ViewmodelBobScale | 1.0 | float |
| MasterVolume | 1.0 | float |
| VSyncEnabled | true | bool |
| FPSLimit | 300 | int |
| MovingPhysicsObjectLimit | 2000 | int |
| InvertMouseX/Y | false | bool |
| ToggleDucking | false | bool |
| UseReverseHotbarScrolling | false | bool |

#### KeybindManager

**File:** `KeybindManager.cs` — Uses Unity's new Input System.

- `PlayerInputActions` asset generated from Input Action definitions
- Keybind overrides saved to `keybinds.json` in persistent data
- `GetBindingText(KeybindAction)` → human-readable key name
- Token replacement: `"Press [Interact] to open"` → `"Press E to open"`
- Full rebinding support via `SettingKeybind` UI component

---

### 4.15 Visual Feedback: Fresnel Highlighter

**File:** `FresnelHighlighter.cs` (151 lines)

A GPU-side object highlighting system using Unity's `CommandBuffer` API.

#### How It Works

```csharp
// 1. On Enable: Create command buffer, attach to camera
_cb = new CommandBuffer { name = "Fresnel Highlighter" };
_cam.AddCommandBuffer(CameraEvent.AfterLighting, _cb);

// 2. Each frame (LateUpdate): Rebuild command buffer
_cb.Clear();
foreach (var (style, renderers) in _styleBuckets)
{
    _mat.SetColor("_Color", style.Color);
    _mat.SetFloat("_Power", style.RimPower);
    _mat.SetFloat("_Intensity", style.Intensity);
    _mat.SetFloat("_ZTest", style.XrayThroughWalls ? Always : LEqual);
    
    foreach (var renderer in renderers)
        for (int sub = 0; sub < renderer.sharedMaterials.Length; sub++)
            _cb.DrawRenderer(renderer, _mat, sub, 0);
}
```

#### Highlight Presets

| Preset | Use | Color |
|--------|-----|-------|
| `ToolPreset` | Pickups, terminals | Cyan |
| `GenericGrabbablePreset` | Physics objects | Cyan |
| `BuildingPreset` | Buildings (hammer tool) | Cyan |
| `WrenchEnableSupports` | Buildings needing supports | Custom |
| `WrenchDisableSupports` | Buildings with supports | Custom |

**Why this is good:** No extra camera pass, no render texture. The command buffer injects draw calls directly into the rendering pipeline. Minimal overhead.

---

### 4.16 Debug System

**File:** `DebugManager.cs` (171 lines)

#### Activation

Secret code typed during gameplay: `"shaftmaster"` — input buffer with 3-second reset timer.

#### Debug Commands (when active)

| Key | Action |
|-----|--------|
| V | Toggle noclip (in PlayerController) |
| I | Unlock all shop items |
| U | Toggle unlimited building |
| M | Add $1000 |
| Z | Complete next active quest |
| - | Halve time scale |
| = | Double time scale |
| Backspace | Reset time scale to 1.0 |

#### Error Handling

```csharp
Application.logMessageReceived += HandleLog;

private void HandleLog(string message, string stackTrace, LogType type)
{
    if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        UIManager.PauseMenu.ShowErrorPopup(message, stackTrace);
}
```

Shows runtime errors as in-game popups with dismissal option.

---

### 4.17 Auto-Save System

**File:** `AutoSaveManager.cs` (58 lines)

```csharp
private void Update()
{
    if (AutoSaveEnabled && Time.time - _lastAutoSaveTime >= AutoSaveFrequency * 60f)
    {
        _lastAutoSaveTime = Time.time;
        StartCoroutine(AutoSave());
    }
}
```

Default frequency: 5 minutes. Shows UI warning during save. Skips main menu and legacy demo scenes.

---

## 5. Key Design Patterns & Techniques

| Pattern | Where Used | How It Works |
|---------|-----------|-------------|
| **Singleton** | Every manager class | Generic `Singleton<T>` base, first-instance-wins, destroys duplicates |
| **Object Pooling** | `OrePiecePoolManager`, `SoundManager` | Dictionary-keyed queues, deactivate-and-reuse instead of destroy |
| **Component/Interface** | `ISaveLoadableObject`, `IInteractable`, `IDamageable` | Loose coupling — systems discover capabilities via interface |
| **ScriptableObject Data** | `ShopItemDefinition`, `QuestDefinition`, `SoundDefinition` | Data-driven design, editor-configurable without code changes |
| **Event-Driven** | `OnMoneyUpdated`, `QuestCompleted`, `OnBuildingRemoved` | C# `event Action` for decoupled publish-subscribe |
| **Enum State Machine** | `OreLimitState`, `MagnetToolSelectionMode` | Simple state transitions via enum values |
| **Weighted Random** | `OreNode` drops, sieving, cluster breaking | Cumulative weight selection algorithm |
| **Spring Joint Physics** | `ToolMagnet`, `PlayerController` grab | Soft-constraint physics manipulation via Unity joints |
| **Command Buffer Rendering** | `FresnelHighlighter` | GPU-side rendering injection without extra cameras |
| **Atomic File I/O** | `WriteSaveAtomically()` | Write to `.tmp`, then rename — prevents corruption |
| **Coroutine Sequencing** | Scene loading, auto-save, delayed attacks | `IEnumerator` + `yield` for async-like workflows |
| **Static Collections** | `OrePiece.AllOrePieces`, `ConveyorBelt.AllConveyorBelts` | Global queries without `FindObjectsOfType` |

---

## 6. Critique & Thoughts

### What's Done Well

1. **Singleton + Event architecture is pragmatic.** For a game of this scope, the singleton pattern with C# events works cleanly. Each manager has a clear responsibility. The `[DefaultExecutionOrder]` usage prevents initialization race conditions — a common Unity pitfall handled properly here.

2. **The Save/Load system is impressively robust.** Versioned files with backward compatibility across 12 versions, atomic writes, automatic backups, legacy migration, and screenshot capture. This is production-grade save handling. The interface-based approach allows new savable types without modifying the save manager.

3. **Object pooling for OrePieces is essential and well-done.** With potentially thousands of physics ore pieces, the `OrePiecePoolManager` with its composite key `(ResourceType, PieceType, IsPolished)` prevents catastrophic GC pressure. The `ReturnToPool` method properly resets all state.

4. **The `OreLimitManager` is a clever self-regulating system.** Rather than crashing or lagging, the game gracefully degrades auto-miner output as physics load increases. The tiered approach with multiplied spawn intervals is elegant.

5. **Conveyor snap detection is genuinely useful.** Tests 4 rotations × all input/output points, picks best by frequency counting. Makes factory building user-friendly while staying simple.

6. **Fresnel Highlighter uses CommandBuffers** instead of a second camera or post-processing — the correct, performance-friendly approach.

7. **Static collections (`AllOrePieces`, `AllConveyorBelts`)** avoid expensive `FindObjectsOfType` calls in hot paths.

### Areas for Improvement (Learning Opportunities)

1. **Overuse of `FindObjectOfType` at runtime.** Multiple scripts call `FindObjectOfType<PlayerInventory>()` in runtime methods (not just init). This is O(N) over all objects. Should be cached on `Start()` or passed as a reference.

2. **The `PlayerController` is a God Object (888 lines).** It handles movement, camera, ducking, bobbing, interaction, grabbing, highlighting, flashlight, noclip, and respawning. Would benefit from being split: `PlayerMovement`, `PlayerInteraction`, `PlayerCameraController`, `PlayerGrabSystem`.

3. **LINQ in hot paths.** `EconomyManager.GetShopItemFromSavableObjectID()` uses `.FirstOrDefault()` with LINQ. For frequently-called lookups, a `Dictionary` would be O(1) vs O(N).

4. **`CastingFurnace.DetermineOutputResourceType()` allocates every call** via LINQ `GroupBy`. A reusable `Dictionary<ResourceType, int>` count would eliminate GC pressure.

5. **`ConveyorBelt` uses `List` allowing duplicates.** `OnTriggerEnter` can fire multiple times for compound colliders. A `HashSet` would prevent duplicates structurally.

6. **No null safety in `OrePiecePoolManager` dequeue.** If a pooled piece was externally destroyed, the queue would dequeue null. A null check after dequeue would add resilience.

7. **Interface hierarchy could be simplified.** Four save interfaces (`ISaveLoadableObject`, `ISaveLoadableBuildingObject`, `ISaveLoadableStaticBreakable`, `ISaveLoadableWorldEvent`) could be unified into a single `ISavable` with capability flags.

8. **No dependency injection.** Everything uses `Singleton<T>.Instance` globally. Fine for this scope, but constructor injection would improve testability and make dependencies explicit in larger projects.

### Overall Assessment

This is a **well-structured indie game codebase** that demonstrates solid Unity development practices. The data-driven approach (ScriptableObjects), event-based inter-manager communication, robust save system, and performance-aware object pooling show thoughtful engineering. The ore processing pipeline is particularly well-designed — each machine is a focused component transforming `OrePiece` objects through a clear chain.

**For learning purposes, this is excellent study material** for understanding how to architect a complete game systems in Unity.

---

## 7. File Index

### Manager Singletons
| File | Lines | Purpose |
|------|-------|---------|
| `GameManager.cs` | 23 | Pause/unpause events |
| `EconomyManager.cs` | 162 | Money, shop items |
| `SavingLoadingManager.cs` | 853 | Save/load, prefab registry |
| `BuildingManager.cs` | 404 | Building placement, ghost system |
| `QuestManager.cs` | 238 | Quest lifecycle |
| `ResearchManager.cs` | 121 | Research tree |
| `ContractsManager.cs` | 112 | Contract objectives |
| `OreManager.cs` | 114 | Resource definitions, display formatting |
| `OreLimitManager.cs` | 97 | Physics performance throttle |
| `SoundManager.cs` | 204 | Sound pooling, conveyor ambient |
| `UIManager.cs` | 276 | UI state management |
| `SettingsManager.cs` | 107 | Player preferences |
| `KeybindManager.cs` | 199 | Input rebinding |
| `DebugManager.cs` | 171 | Dev tools |
| `AutoSaveManager.cs` | 58 | Periodic auto-save |
| `OrePiecePoolManager.cs` | 173 | Ore object pooling |

### Player Systems
| File | Lines | Purpose |
|------|-------|---------|
| `PlayerController.cs` | 888 | FPS controller, camera, interaction |
| `PlayerInventory.cs` | 418 | Hotbar + inventory management |
| `BaseHeldTool.cs` | 265 | Tool base class |
| `ToolPickaxe.cs` | 115 | Mining tool |
| `ToolMagnet.cs` | 327 | Physics magnet tool |
| `ToolBuilder.cs` | 230 | Building placement tool |

### Ore & Processing
| File | Lines | Purpose |
|------|-------|---------|
| `OrePiece.cs` | 443 | Core resource object |
| `OreNode.cs` | 128 | Mineable world object |
| `CastingFurnace.cs` | 456 | Smelting with recipe system |
| `AutoMiner.cs` | 227 | Automated ore spawning |
| `SellerMachine.cs` | 43 | Trigger-based selling |
| `DepositBox.cs` | 235 | Animated elevator |
| `ConveyorBelt.cs` | 112 | Physics item transport |

### Buildings
| File | Lines | Purpose |
|------|-------|---------|
| `BuildingObject.cs` | 271 | Building component |
| `BuildingPlacementNode.cs` | ~100 | Attachment points |
| `ModularBuildingSupports.cs` | ~200 | Dynamic scaffolding |

### Visual
| File | Lines | Purpose |
|------|-------|---------|
| `FresnelHighlighter.cs` | 151 | CommandBuffer outline rendering |

### Core Utilities
| File | Lines | Purpose |
|------|-------|---------|
| `Singleton.cs` | 20 | Generic singleton base |
| `TimeSince.cs` | ~50 | Implicit float timer |
| `TimeUntil.cs` | ~60 | Countdown timer |
| `MathExtensions.cs` | ~30 | Vector rounding/truncation |
| `PhysicsUtils.cs` | ~50 | Collision ignoring, explosions |
| `UniqueQueue.cs` | ~30 | Queue with uniqueness |