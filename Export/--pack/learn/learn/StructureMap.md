# StructureMap — All DataServices

> Every DataService is pure C#. Testable via `new`. Zero scene, zero MonoBehaviour dependency.
> Collections use `ALL_CAPS` for lists, `DOC__` prefix for dictionaries.

---

## Quick Reference

| # | DataService | Phase | One-Liner | Status |
|---|-------------|-------|-----------|--------|
| 1 | `ShopDataService` | A | "I manage shop categories + cart as collections" | ✅ Done |
| 2 | `InventoryDataService` | B | "I manage all inventory slots — add/remove/switch/stack" | ✅ Done |
| 3 | `OreDataService` | C | "I manage resource descriptions, weighted drops, ore sell values" | ⬜ Planned |
| 4 | `BuildingDataService` | D | "I validate grid placement and detect conveyor snap alignment" | ⬜ Planned |
| 5 | `QuestDataService` | F | "I manage quest collections + progress tracking + requirement checks" | ⬜ Planned |
| 6 | `ResearchDataService` | F | "I manage completed research items + ticket balance" | ⬜ Planned |
| 7 | `SaveDataService` | G | "I manage prefab lookups + save file serialization/deserialization" | ⬜ Planned |
| 8 | `ContractDataService` | I | "I manage contract instances — accept/fill/deposit/claim" | ⬜ Planned |

---

## 1. ShopDataService (Phase A) ✅

> "I manage shop categories + cart as collections"

### Collections

```
List<SO_ShopCategory>                          CATEGORY
Dictionary<SO_ShopCategory, List<WShopItem>>   DOC__category_wShopItem
List<CartItem>                                 CARTITEM
```

### Nested Types

```csharp
public class CartItem { WShopItem wShopItem; int qty; }
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Build | `BuildCategories(List<SO_ShopCategory>)` | Wraps each SO def into WShopItem, populates DOC |
| Get | `GetCategories()` | Returns CATEGORY list |
| Get | `GetWShopItems(category)` | Returns WShopItems for a category from DOC |
| Get | `GetCartItems()` | Returns CARTITEM list |
| Get | `GetCartTotalPrice()` | Sums price × qty across CARTITEM |
| Add | `TryAddNewCartItem(wShopItem)` | Adds or increments qty if already in cart |
| Remove | `RemoveCartItem(cartItem)` | Removes one cart entry |
| Remove | `ClearCartItems()` | Clears entire cart |
| Alter | `AlterCartItemQty(cartItem, newQty)` | Sets qty, removes if ≤ 0 |
| Alter | `IncreaseCartItemQty(cartItem, dQty)` | Adds delta, removes if ≤ 0 |
| Boolean | `shouldCategoryBeHiddenInView(category)` | True if hideIfAllLocked AND all items locked |
| Boolean | `CanAffordCartItems(useCustomMoney, amount)` | Cart total ≤ money |
| Snapshot | `GetSnapShotForTest(header)` | Combines all PhaseALOG formatters |

---

## 2. InventoryDataService (Phase B) ✅

> "I manage all inventory slots — add/remove/switch/stack"

### Collections

```
List<Slot>   SLOTS          (40 total: 10 hotbar + 30 extended)
int          activeSlotIndex
```

### Nested Types

```csharp
public class Slot { BaseHeldTool Tool; int Index; bool IsHotbar => Index < 10; }
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Build | `Build()` | Creates 40 empty slots |
| Get | `GetSlots()` | Returns SLOTS list |
| Get | `ActiveSlot` / `ActiveTool` | Current selection |
| Get | `GetIndexFor(tool)` | Finds slot index for a tool, or -1 |
| Get | `GetHotbarSize()` / `GetTotalSize()` | Constants |
| Add | `TryAdd(tool, preferredSlot)` | Stack if possible → preferred slot → first empty. Returns index or -1 |
| Remove | `Remove(tool)` | Nulls the slot containing this tool |
| Remove | `Clear()` | Nulls all slots, resets active to 0 |
| Switch | `SwitchTo(index)` | Sets activeSlotIndex (hotbar only) |
| Switch | `Scroll(delta)` | Wraps activeSlotIndex by ±1 within hotbar |
| Swap | `Swap(indexA, indexB)` | Swaps tools between two slots |
| Snapshot | `GetSnapshot()` | PhaseBLOG formatter |

---

## 3. OreDataService (Phase C) ⬜

> "I manage resource descriptions, weighted drops, ore sell values"

### Collections

```
List<SO_ResourceDescription>   RESOURCE_DESC
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Build | `Build(List<SO_ResourceDescription>)` | Stores resource descriptions |
| Get | `GetResourceDescription(ResourceType)` | Lookup by type |
| Get | `GetResourceColor(ResourceType)` | Returns Display Color for a resource |
| Get | `GetDefaultSellValue(ResourceType, PieceType, isPolished)` | Sell price lookup |
| Get | `GetVolumeInBox(ResourceType, PieceType)` | Volume for box packing |
| Format | `GetColoredResourceTypeString(ResourceType)` | `<color=#hex>Iron</color>` |
| Format | `GetColoredFormattedResourcePieceString(ResourceType, PieceType, requirePolished)` | Full formatted name with piece type label mapping (DrillBit→"Drill Bit", etc.) |
| Snapshot | `GetSnapshot()` | PhaseCLOG formatter |

**Note:** Weighted random selection (used by OreNode, AutoMiner, OrePiece sieving/cluster) is a **utility method** in `UtilsPhaseC`, not part of this DataService. See [Utility Extractions](#utility-extractions) below.

---

## 4. BuildingDataService (Phase D) ⬜

> "I validate grid placement and detect conveyor snap alignment"

### Collections

None persistent — operates on passed-in data (ghost position, neighbor buildings).

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Grid | `GetClosestGridPosition(Vector3)` | Snaps world position to 1m integer grid |
| Validate | `CanPlace(position, rotation, requiresFlat, canPlaceInTerrain, nodeRequirement)` | Returns CanPlaceBuilding enum (Valid/Invalid/RequirementsNotMet) |
| Snap | `GetNearbySnapConnections(ghostPos, building, neighbor, isMirrored)` | Tests 4 rotations × input/output snap points, returns List<BuildingRotationInfo> where distance < 0.25f |
| Snap | `ResolveBestSnap(List<BuildingRotationInfo>)` | Groups by rotation, picks most-voted or first |

**Note:** Ghost object instantiation, material swapping, layer setting, and Physics.OverlapBox stay in `BuildingManager` MonoBehaviour — they need Unity API. Only the pure geometry math lives here.

---

## 5. QuestDataService (Phase F) ⬜

> "I manage quest collections + progress tracking + requirement checks"

### Collections

```
List<WQuest>   ALL_QUESTS
List<WQuest>   ACTIVE_QUESTS
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Build | `Build(List<SO_QuestDefinition>)` | Generates WQuest wrappers from definitions |
| Get | `GetQuestByID(QuestID)` | Lookup in ALL_QUESTS |
| Get | `GetCompletedQuestIDs()` | Filters ALL_QUESTS where completed |
| Get | `GetActiveQuestIDs()` | Maps ACTIVE_QUESTS to IDs |
| Get | `GetAvailableQuests()` | Not locked, not completed, not active |
| Get | `GetActiveQuestSaveEntries()` | Serializes active quests for save file |
| Add | `TryActivateQuest(quest)` | Adds to ACTIVE if not locked/completed/already active |
| Add | `ForceActivateQuest(QuestID)` | Adds unconditionally |
| Remove | `PauseQuest(quest)` | Removes from ACTIVE |
| Progress | `OnResourceDeposited(ResourceType, PieceType, polishedPercent, amount)` | Increments matching ResourceQuestRequirements |
| Progress | `ActivateQuestTrigger(TriggeredQuestRequirementType, amount)` | Increments matching TriggeredQuestRequirements |
| Boolean | `IsCompleted(quest)` | All requirements met |
| Boolean | `IsLocked(quest)` | Prerequisites not completed |
| Load | `LoadFromSaveFile(SaveFile)` | Rebuilds quests from save data |
| Snapshot | `GetSnapshot()` | PhaseFLOG formatter |

---

## 6. ResearchDataService (Phase F) ⬜

> "I manage completed research items + ticket balance"

### Collections

```
List<SavableObjectID>   COMPLETED_RESEARCH
int                     researchTickets
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Get | `GetResearchTickets()` | Returns ticket count |
| Get | `IsResearchItemCompleted(researchItem)` | COMPLETED_RESEARCH.Contains check |
| Get | `GetResearchItemByID(SavableObjectID)` | Lookup in definition list |
| Add | `ResearchItem(researchItem)` | Deducts tickets + money, adds to COMPLETED, fires event |
| Add | `AddResearchTickets(amount)` | Increments ticket count |
| Set | `SetResearchTickets(amount)` | For load |
| Boolean | `CanAffordResearch(amount)` | Tickets ≥ cost |
| Load | `LoadFromSaveFile(List<SavableObjectID>)` | Restores completed list |
| Migrate | `MigrateNewResearchPrices()` | Recalculates tickets from quest rewards minus spent — pure math |
| Snapshot | `GetSnapshot()` | PhaseFLOG formatter |

---

## 7. SaveDataService (Phase G) ⬜

> "I manage prefab lookups + save file serialization/deserialization"

### Collections

```
Dictionary<SavableObjectID, GameObject>    DOC__id_prefab
Dictionary<QuestID, SO_QuestDefinition>    DOC__questId_def
Dictionary<OrePieceKey, OrePiece>          DOC__oreKey_prefab
List<Vector3>                              DESTROYED_POSITIONS
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Build | `BuildLookups(prefabs, questDefs, orePrefabs)` | Builds 3 dictionaries with duplicate validation |
| Get | `GetPrefab(SavableObjectID)` | Lookup in DOC__id_prefab |
| Get | `GetOrePiecePrefab(ResourceType, PieceType, isPolished)` | Lookup in DOC__oreKey_prefab |
| Get | `GetQuestDefinition(QuestID)` | Lookup in DOC__questId_def |
| Get | `GetBuildingInventoryDefinition(SavableObjectID)` | Prefab → BuildingObject → Definition |
| Track | `AddDestroyedPosition(Vector3)` | Appends to DESTROYED_POSITIONS |
| File | `IsSaveFileCompatible(version)` | Version 4–15 = true |
| File | `GetFullSaveFilePath(fileName)` | Path.Combine with Saves folder |
| File | `GetAllSaveFilePaths()` | Enumerates .json files in Saves |
| File | `GetAllSaveFileHeaderCombos()` | Parses headers from all save files |
| File | `GetSaveFileHeader(fullPath)` | Reads + parses JSON header |
| Format | `GetFormattedPlaytime(totalSeconds)` | `"2h 05m 30s"` format |
| Format | `GetFormattedLastSaveTime()` | `"3 minutes ago"` format |
| Snapshot | `GetSnapshot()` | PhaseGLOG formatter |

**Note:** `SaveGame()` / `LoadGame()` stay in `SavingLoadingManager` MonoBehaviour — they call `FindObjectsOfType`, `Instantiate`, `Destroy`, coroutines. SaveDataService owns the **data operations** (lookups, path resolution, header parsing). The manager owns the **Unity lifecycle** (scene loading, object spawning).

---

## 8. ContractDataService (Phase I) ⬜

> "I manage contract instances — accept/fill/deposit/claim"

### Collections

```
WContractInstance              activeContract
List<WContractInstance>        INACTIVE_CONTRACTS
```

### Methods

| Group | Method | What It Does |
|-------|--------|-------------|
| Build | `Build(List<SO_ContractDefinition>)` | Generates WContractInstance wrappers |
| Get | `GetActiveContract()` | Returns activeContract |
| Get | `GetInactiveContracts()` | Returns INACTIVE_CONTRACTS |
| Set | `SetContractActive(contract)` | Moves from inactive → active (swaps if one already active) |
| Set | `SetContractInactive(contract)` | Moves from active → inactive |
| Deposit | `DepositBox(BoxContents)` | Iterates contents, matches against active contract requirements, increments amounts |
| Claim | `ClaimReward(contract)` | Returns reward money if completed, removes contract |
| Boolean | `IsCompleted(contract)` | All requirements met |
| Snapshot | `GetSnapshot()` | PhaseILOG formatter |

---

## Utility Extractions (NOT DataServices)

These are **static utility methods** that recur across phases but don't manage collections:

### Weighted Random Selection — `UtilsPhaseC`

The same algorithm appears in 4 places in the main source:

| Where | Operates On |
|-------|-------------|
| `OreNode.GetOrePrefab()` | `List<WeightedNodeDrop>` |
| `AutoMinerResourceDefinition.GetOrePrefab()` | `List<WeightedOreChance>` |
| `OrePiece.CompleteSieving()` | `List<WeightedOreChance>` |
| `OrePiece.SelectClusterBreakerPrefab()` | `List<WeightedOreChance>` |

Extract as:
```csharp
// in UtilsPhaseC
public static T WeightedRandom<T>(List<T> items, Func<T, float> getWeight)
```

Algorithm: sum weights → roll `Random.value * total` → cumulative scan → return match.

---

## DataService Decision Tree

```
Does it manage a COLLECTION (List/Dict/HashSet)?
  └─ No  → Not a DataService (utility or entity)
  └─ Yes →
      Are ALL operations pure C# (no physics, no lifecycle, no Instantiate)?
        └─ No  → Keep in MonoBehaviour (e.g. OrePiecePoolManager, ToolMagnet)
        └─ Yes →
            Is it a SHARED service (not per-instance)?
              └─ No  → It's an Entity (e.g. BoxContents, ContractInstance)
              └─ Yes → ✅ DataService — put in 2-Data/DataService/
```
---

## All SO_ (ScriptableObjects) — Pure Data Blueprints

> **Rule:** `SO_` = config blueprint. Only fields + `[CreateAssetMenu]`. No business logic, no singleton reads.
> If the original has helper methods (GetName, GetIcon, GetOrePrefab), those move to the DataService or the Orchestrator that consumes the SO.
> Exception: `GenerateQuest()` / `GenerateContract()` on definition SOs — factory methods that produce runtime wrappers are acceptable.

### Phase A 

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_ShopItemDef` | `ShopItemDefinition` | `itemDefName`, `descr` [TextArea], `defaultPrice`, `isDefaultLocked`, `sprite`, `pfObject`, `maxStackableCount` |
| `SO_ShopCategory` | `ShopCategory`* | `categoryName`, `sprite`, `hideIfAllItemsLocked`, `List<SO_ShopItemDef> ITEM_DEF` |
| `SO_Interaction` | `Interaction` | `Name`, `Description`, `Icon` (Sprite) |

*`ShopCategory` original is `[Serializable]`, user promoted to ScriptableObject for editor workflow (categories as .asset files).

### Phase B

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_FootstepSoundDefinition` | `FootstepSoundDefinition` | `LeftFootstepDefinition` (SO_SoundDefinition), `RightFootstepDefinition` (SO_SoundDefinition) |

### Phase C

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_AutoMinerResourceDefinition` | `AutoMinerResourceDefinition` | `SpawnProbability` [Range 0–100], `SpawnRate` [Range 0–20], `List<WeightedOreChance> _possibleOrePrefabs` |

**Not SO_ (stay as Entities in `2-Data/Entities/`):**
- `ResourceDescription` — `[Serializable]`, 2 fields (`ResourceType`, `Color DisplayColor`). Embedded in OreManager's inspector list.
- `WeightedNodeDrop` — `[Serializable]`, 2 fields (`OrePiece OrePrefab`, `float Weight`). Embedded in OreNode's inspector list.
- `WeightedOreChance` — `[Serializable]`, 2 fields (`OrePiece OrePrefab`, `float Weight`). Embedded in AutoMinerResourceDef / OrePiece lists.

### Phase D

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_BuildingInventoryDefinition` | `BuildingInventoryDefinition` | `Name`, `ProgrammerInventoryIcon`, `InventoryIcon`, `Description` [TextArea], `QButtonFunction` ("Mirror"), `MaxInventoryStackSize`, `List<BuildingObject> BuildingPrefabs`, `BuildingCrate PackedPrefab`, `UseReverseRotationDirection`, `CanBePlacedInTerrain` |

### Phase E

No new SO_ — machines use Phase C/D SO definitions. Furnace recipe data stays as `[Serializable]` Entities:
- `CastingFurnaceMoldRecipieSet` — `CastingMoldType`, `AmountOfMaterialRequired`, `List<CastingFurnaceRecipie>`
- `CastingFurnaceRecipie` — `InputResourceType`, `OutputPrefab`, `SecondaryOutputPrefab`

### Phase F

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_QuestDefinition` | `QuestDefinition` | `QuestID`, `Name`, `Description` [TextArea], `UIPriority`, `OverrideRewardText`, `OverrideQuestIcon`, `HideInQuestTree`, `UnlockWhenAnyPrerequisitesAreComplete`, `List<SO_QuestDefinition> PrerequisiteQuests`, `List<ResourceQuestRequirement>`, `List<TriggeredQuestRequirement>`, `List<TimedQuestRequirement>`, `List<UnlockResearchQuestRequirement>`, `List<ShopItemQuestRequirement>`, `List<SO_QuestDefinition> QuestsToAutoStart`, `List<SO_ShopItemDef> ShopItemsToUnlock`, `List<SO_ShopItemDef> HiddenShopItemsToUnlock`, `RewardMoney`, `RewardResearchTickets` |
| `SO_ResearchItemDefinition` | `ResearchItemDefinition` (abstract) | `_researchTicketsCost`, `_moneyCost`, `List<SO_ResearchItemDefinition> PrerequisiteResearch`, `IsLockedInDemo` |
| `SO_ShopItemResearchItemDef` | `ShopItemResearchItemDefinition` (extends above) | `_overrideDisplayName`, `List<SO_ShopItemDef> ShopItemDefinitions` |
| `SO_UpgradeDepositBoxResearchItemDef` | `UpgradeDepositBoxResearchItemDefinition` (extends above) | `_displayName`, `_description` [TextArea], `_icon`, `_programmerIcon`, `_savableObjectID` |

### Phase H

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_SoundDefinition` | `SoundDefinition` | `AudioClipDescription[] sounds`, `minPitch` [0.5–2], `maxPitch` [0.5–2], `maxRange` [0–100], `Priority` [0–256] |

**Entity (not SO_):**
- `AudioClipDescription` — `[Serializable] struct`: `AudioClip clip`, `float volume`, `float pitch`, `float maxRange`, `int priority`

### Phase I

| Our Name | Original | Fields |
|----------|----------|--------|
| `SO_ContractDefinition` | `ContractDefinition` | `Name`, `Description` [TextArea], `List<ResourceQuestRequirement> ResourceRequirements`, `RewardMoney` |

### Phase J

No new SO_.

### SO_ Summary Table

| # | SO_ Name | Phase | One-Liner |
|---|----------|-------|-----------|
| 1 | `SO_ShopItemDef` | A | "I define what a shop item IS" |
| 2 | `SO_ShopCategory` | A | "I group items into a category" |
| 3 | `SO_Interaction` | A | "I define one interaction option" |
| 4 | `SO_FootstepSoundDefinition` | B | "I pair left/right footstep sounds" |
| 5 | `SO_AutoMinerResourceDefinition` | C | "I configure auto-miner spawn rate + weighted ore drops" |
| 6 | `SO_BuildingInventoryDefinition` | D | "I define a building's inventory identity + prefab variants" |
| 7 | `SO_QuestDefinition` | F | "I define a quest's requirements, rewards, and progression" |
| 8 | `SO_ResearchItemDefinition` | F | "I define a research item's cost and prerequisites" (abstract) |
| 9 | `SO_ShopItemResearchItemDef` | F | "I unlock shop items when researched" |
| 10 | `SO_UpgradeDepositBoxResearchItemDef` | F | "I upgrade deposit box when researched" |
| 11 | `SO_SoundDefinition` | H | "I define a sound with clip variants, pitch range, and range" |
| 12 | `SO_ContractDefinition` | I | "I define a contract's requirements and reward" |

**Total: 12 SO_ types across all phases.**

---

## All Field_ (Prefab Handles)

> **Rule:** `Field_` = MonoBehaviour attached to ANY prefab (UI or world). It exposes `[SerializeField]` child references so that an external script can access them via one typed reference instead of hierarchy digging.
> Only refs + display setters. **No** business logic, **No** singleton access, **No** Instantiate.
> 
> **When to use Field_:**
> 1. A prefab is **Instantiated at runtime** and the creator (Orchestrator) needs typed access to its parts
> 2. A prefab has **multiple child references** that an external script needs — Field_ eliminates `GetComponentInChildren` / `transform.Find`
> 3. You want to **separate "what are my visual parts" from "what is my logic"** on a complex prefab
>
> **When NOT to use Field_:**
> - The logic script IS on the same prefab and is the ONLY consumer of those refs → keep refs inline as `[SerializeField]` on the logic script
> - The prefab has 1-2 refs → not worth the extra class

### Phase A — 3 Field_ (UI)

| Our Name | Original | Refs | Setters |
|----------|----------|------|---------|
| `Field_ShopCategory` | `ShopCategoryButton` | `Button`, `TMP_Text _nameText`, `Image _img` | `SetNameText(str)`, `SetSelected(bool, Color, Color)` |
| `Field_ShopItem` | `ShopItemButton` | `TMP_Text _nameText/_descrText/_priceText/_buttonText`, `Image _icon/_buttonBg`, `Button _addToCartButton` | `SetData(name, descr, price, buttonText, sprite)`, `SetButtonInteractable(bool, str, Color, Color)` |
| `Field_ShopCartItem` | `ShopCartItemButton` | `TMP_Text _nameText/_descrText/_priceText`, `Image _icon`, `TMP_InputField _qtyInputField`, `Button _addButton/_subButton/_removeButton` | `SetData(name, descr, sprite)`, `SetPrice(float)`, `SetQty(int)` |

### Phase A½ — 0 Field_

No prefab handles needed. StartingElevator + CameraShaker are self-contained.

### Phase B — 1 Field_ (UI)

| Our Name | Original | Refs | Setters |
|----------|----------|------|---------|
| `Field_InventorySlot` | `InventorySlotUI` | `Image Icon/Background`, `TMP_Text Text/AmountText`, `KeybindTokenText SlotNumberText`, `GameObject OrangeBarThing/HideWhenDragged` | `SetData(sprite, name, amount)`, `SetHighlighted(bool)`, `SetSlotNumber(str)` |

**Note:** Original has drag-drop logic + `FindObjectOfType`. In our arch: drag-drop wiring lives in `InventoryOrchestrator`. Field_ only holds refs + setters.

### Phase C — 0 Field_

Analyzed every Phase C prefab:
- **OreNode** — `_models[]`, `_takeDamageSoundDefinition` → only OreNode itself uses them. 3 refs, self-contained.
- **OrePiece** — `MeshFilter`, `MeshCollider`, `_possibleMeshes[]` → only OrePiece itself uses them. Self-contained.
- **AutoMiner** — `Rotator`, `OreSpawnPoint`, `_lightMeshRenderer`, `_audioSource_Loop` → only AutoMiner itself uses them. Self-contained.
- **SellerMachine** — zero child refs, trigger-only.
- **OrePiecePoolManager** — creates a root Transform, no prefab children to expose.
- **PhysicsLimitUIWarning** — 1-2 refs (text), too small.

**Verdict:** All Phase C scripts have their own `[SerializeField]` refs used only by themselves. No external script needs typed access to another prefab's children. No Field_ needed.

### Phase D — 0 Field_

Analyzed every Phase D prefab:
- **BuildingObject** — 12+ refs (`PhysicalColliderObject`, `BuildingPlacementColliderObject`, `ConveyorInputSnapPositions`, `ConveyorOutputSnapPositions`, etc.) → all used by BuildingObject + BuildingManager. BuildingManager accesses some via the BuildingObject reference directly (public fields). No Field_ needed — BuildingObject IS the typed handle.
- **BuildingCrate** — accesses child `Image[]`/`TMP_Text[]` via `GetComponentsInChildren`. Could be Field_, but it's the logic script itself on the prefab. Keep inline.
- **ConveyorBelt** — `Speed`, `Disabled`, physics list. Self-contained.
- **ConveyorRenderer** — texture scroll. Self-contained.

**Verdict:** BuildingObject already acts as a typed handle (public fields accessed by BuildingManager). No separate Field_ needed.

### Phase E — 0 Field_

Analyzed every Phase E machine:
- **CastingFurnace** — 5 `TMP_Text` refs + `Animator` + `Transform LiquidPlane` + sound refs. Heavy, but ALL written by CastingFurnace itself in `Update()`/`RefreshContentsDisplay()`. No external script accesses them.
- **PolishingMachine** — `Animator`, `Renderer[]` brushes, materials, conveyor ref, light, sound. All self-used.
- **CrusherMachine** — `GrindingPiece1/2` (2 refs). Too small.
- **SorterMachine** — `PassTransform/FailTransform` (4 refs) + `Filter`. Self-used.
- **RollingMill** — `Animator`, `PlateTransform`, `OutputTransform`, `ParticleSystem[]`, plate renderers. Self-used.
- **PackagerMachine** — `_manifestText`, `OutputTransform`. Self-used (2 refs).
- **DepositBox** — `Tier1Box/Tier2Box`, `Animation`, belt renderer/materials, bucket Transform lists, audio. 15+ refs but ALL self-used in `Update()`.

**Verdict:** Every machine owns and consumes its own visual refs. No Orchestrator instantiates machine prefabs at runtime (machines are placed via building system). No Field_ needed.

### Phase F — 4 Field_ (UI)

| Our Name | Original | Refs | Setters |
|----------|----------|------|---------|
| `Field_QuestItem` | `QuestTreeItemButton` | `TMP_Text _questNameText/_questProgressText`, `Image _icon`, `GameObject _trackingOutline`, state colors | `SetData(name, progress, icon)`, `SetState(available/active/completed/locked)` |
| `Field_ResearchItem` | `ResearchItemButton` | `TMP_Text _researchNameText/_costText`, `Image _icon`, state colors | `SetData(name, cost, icon)`, `SetState(available/locked/researched/tooExpensive)` |
| `Field_QuestRequirement` | `QuestRequirementUI` | `TMP_Text NameText`, `GameObject CompletedCheckmark` | `SetData(text)`, `SetCompleted(bool)` |
| `Field_QuestInfo` | `QuestInfoUI` | `TMP_Text NameText/RewardText`, `RectTransform RequirementsContainer` | `SetData(name, reward)` |

**Note:** `Field_QuestInfo` is instantiated by `QuestHud` — Hud creates quest info prefabs for each active quest. Original `QuestInfoUI` has Instantiate + Update logic → in our arch: QuestHud (or QuestHudOrchestrator) handles creation, Field_ only holds refs.

### Phase G — 1 Field_ (UI)

| Our Name | Original | Refs | Setters |
|----------|----------|------|---------|
| `Field_SaveFileButton` | `SaveFileButton` | `TMP_Text _saveFileNameText/_saveVersionNumberText/_lastSaveTimeText` | `SetData(name, version, time)`, `SetDemoIncompatible(bool)` |

### Phase H — 3 Field_ (UI)

| Our Name | Original | Refs | Setters |
|----------|----------|------|---------|
| `Field_SettingSlider` | `SettingSlider` | `Slider`, `TMP_InputField valueInput`, label, min/max/default | `SetValue(float)`, `SetRange(min, max)` |
| `Field_SettingToggle` | `SettingToggle` | `Toggle`, `TMP_Text _onOffLabel`, on/off config | `SetValue(bool)` |
| `Field_SettingKeybind` | `SettingKeybind` | `TMP_Text _keybindLabel`, `GameObject _hideWhenUsingDefaultBind` | `SetBindingText(str)`, `SetIsDefault(bool)` |

**Note:** Original settings have PlayerPrefs + callback logic. In our arch: logic stays in `SettingsManager`, wiring in `SettingsOrchestrator`, Field_ only displays.

### Phase I — 1 Field_ (UI)

| Our Name | Original | Refs | Setters |
|----------|----------|------|---------|
| `Field_ContractInfo` | `ContractInfoUI` | `TMP_Text _contractNameText/_contractDescriptionText/_milestoneText/_rewardText`, `GameObject _setActiveButton/_setInactiveButton/_claimRewardButton`, `RectTransform RequirementsContainer` | `SetData(name, desc, milestone, reward)`, `SetState(active/inactive/completed)` |

**Note:** Original (140 lines) instantiates requirement prefabs + has button callbacks. In our arch: Instantiation + wiring go in `ContractOrchestrator`. Field_ only holds refs.

### Phase J — 0 Field_

Debug tools, no prefab handles needed.

### Field_ Summary Table

| # | Field_ Name | Phase | Kind | One-Liner |
|---|-------------|-------|------|-----------|
| 1 | `Field_ShopCategory` | A | UI | "I hold one category tab's refs" |
| 2 | `Field_ShopItem` | A | UI | "I hold one item row's refs" |
| 3 | `Field_ShopCartItem` | A | UI | "I hold one cart row's refs" |
| 4 | `Field_InventorySlot` | B | UI | "I hold one inventory slot's refs" |
| 5 | `Field_QuestItem` | F | UI | "I hold one quest tree button's refs" |
| 6 | `Field_ResearchItem` | F | UI | "I hold one research item button's refs" |
| 7 | `Field_QuestRequirement` | F | UI | "I hold one requirement line's refs" |
| 8 | `Field_QuestInfo` | F | UI | "I hold one active quest HUD card's refs" |
| 9 | `Field_SaveFileButton` | G | UI | "I hold one save file row's refs" |
| 10 | `Field_SettingSlider` | H | UI | "I hold one slider setting's refs" |
| 11 | `Field_SettingToggle` | H | UI | "I hold one toggle setting's refs" |
| 12 | `Field_SettingKeybind` | H | UI | "I hold one keybind setting's refs" |
| 13 | `Field_ContractInfo` | I | UI | "I hold one contract card's refs" |

**Total: 13 Field_ types across all phases. All UI — zero world-object Field_ needed.**

### Why Zero World-Object Field_?

Every world prefab in this codebase (OreNode, OrePiece, AutoMiner, all machines, BuildingObject, ConveyorBelt) has its **logic script ON the same prefab** as the visual refs. The logic script IS the typed handle — it already has `[SerializeField]` refs wired in inspector. No external Orchestrator creates these prefabs and needs to dig into their children.

Field_ shines when an **Orchestrator Instantiates a prefab and needs typed access** (all 13 cases above are UI items created by Orchestrators). If a future phase introduces a world prefab that's created by an external manager AND has complex child refs, that would be the first world-object Field_.

---

## All Orchestrators

> **Rule:** Orchestrator = **Instantiate Field_ prefab + populate from DataService + AddListener**.
> Lives in `3-MonoBehaviours/Orchestrator/`. All `AddListener` calls live here — never in Field_, never in SubManager.
> Refreshes on events only — never polls in `Update()` (exception: tool input routing in InventoryOrchestrator).
>
> **Pattern (from ShopUIOrchestrator):**
> 1. `Init(dataService)` — receives DataService reference from SubManager.Start()
> 2. `Build___View()` — destroyLeaves container → loop data → Instantiate Field_ → SetData → AddListener
> 3. Track Field_↔Data via `DOC__` dictionaries
> 4. Subscribe to GameEvents for refresh
>
> **When to create an Orchestrator:**
> - A SubManager/panel needs to Instantiate Field_ prefabs and wire them to data
> - There are AddListener calls that connect UI actions to DataService operations
>
> **When NOT — keep wiring inline:**
> - Wiring is ≤3 lines in a simple loop (e.g. InteractionWheelUI — 1 Instantiate + 1 AddListener per button)
> - No DataService involved, no DOC__ tracking needed

### Phase A — 1 Orchestrator

| Our Name | Original | What It Orchestrates |
|----------|----------|---------------------|
| `ShopUIOrchestrator` | `ComputerShopUI` | Instantiates `Field_ShopCategory`, `Field_ShopItem`, `Field_ShopCartItem`. Reads `ShopDataService`. Wires: category onClick→SelectCategory, item onClick→AddToCart, cart qty input/+/-/remove buttons. Tracks `DOC__Category__Field`, `DOC__CartItem__Field`. Refreshes on `OnMoneyChanged`. |

**Not an Orchestrator:** `MoneyOrchestrator` — subscribes `OnMoneyChanged` → updates HUD text. No DataService, no Field_. It's a reactive display MonoBehaviour, lives in `3-MonoBehaviours/` not `Orchestrator/`.

### Phase A½ — 0 Orchestrators

No UI panels with Field_ instantiation.

### Phase B — 1 Orchestrator

| Our Name | Original | What It Orchestrates |
|----------|----------|---------------------|
| `InventoryOrchestrator` | `PlayerInventory` + `InventoryUIManager` + `InventorySlotUI` | Instantiates `Field_InventorySlot` (40 slots into hotbar + extended containers). Reads `InventoryDataService`. Wires: tool switching (1-0 keys, scroll), tool actions (primary/secondary fire, drop, Q), drag-drop. Subscribes `OnToolPickupRequested`, `OnToolDropped`, `OnMoneyChanged`. |

### Phase C — 0 Orchestrators

No Field_ instantiation. OreNode/OrePiece/AutoMiner are self-contained world objects that self-init.

### Phase D — 0 Orchestrators

BuildingManager handles ghost creation directly. No Field_ + DataService wiring pattern.

### Phase E — 0 Orchestrators

All machines are self-contained. No runtime UI instantiation.

### Phase F — 3 Orchestrators

| Our Name | Original | What It Orchestrates |
|----------|----------|---------------------|
| `QuestOrchestrator` | `QuestTreeUI` + `QuestTreeItemButton` | Populates quest tree panel with `Field_QuestItem` buttons (pre-placed in scene or instantiated). Reads `QuestDataService`. Wires: quest button onClick→PreviewQuest, activate/pause buttons. Draws connection lines between prerequisite quests. Subscribes `QuestActivated`, `QuestPaused`, `QuestCompleted` for refresh. |
| `ResearchOrchestrator` | `ResearchTreeUI` + `ResearchItemButton` | Populates research tree with `Field_ResearchItem` buttons. Reads `ResearchDataService`. Wires: research button onClick→PreviewResearch, buy button→ResearchItem. Draws connection lines. Subscribes `ResearchTicketsUpdated` for refresh. |
| `QuestHudOrchestrator` | `QuestHud` + `QuestInfoUI` | Instantiates `Field_QuestInfo` prefabs for each active quest on HUD. Reads `QuestDataService.ActiveQuests`. Wires: requirement sub-items via `Field_QuestRequirement`. Subscribes `QuestActivated`, `QuestPaused`, `QuestCompleted` → regenerates quest list. |

### Phase G — 1 Orchestrator

| Our Name | Original | What It Orchestrates |
|----------|----------|---------------------|
| `SaveFileOrchestrator` | `LoadingMenu` | Instantiates `Field_SaveFileButton` rows from `SaveDataService.GetAllSaveFileHeaderCombos()`. Wires: row onClick→SelectSaveFile, load/delete/rename buttons. Refreshes list on panel open. |

### Phase H — 1 Orchestrator

| Our Name | Original | What It Orchestrates |
|----------|----------|---------------------|
| `SettingsOrchestrator` | `SettingsMenu` | Wires all `Field_SettingSlider`, `Field_SettingToggle`, `Field_SettingKeybind` callbacks → `SettingsManager` setters. AddListener: `onValueChanged` for each slider/toggle → Apply methods. No Instantiate (settings are pre-placed in panel), but heavy AddListener wiring (15+ callbacks). |

### Phase I — 1 Orchestrator

| Our Name | Original | What It Orchestrates |
|----------|----------|---------------------|
| `ContractOrchestrator` | `ContractsTerminalUI` + `ContractInfoUI` | Instantiates `Field_ContractInfo` for active + inactive contracts. Reads `ContractDataService`. Wires: setActive/setInactive/claimReward buttons. Each `Field_ContractInfo` also instantiates `Field_QuestRequirement` sub-items. Regenerates on contract state change. |

### Phase J — 0 Orchestrators

Debug tools, no UI orchestration.

### Orchestrator Summary Table

| # | Orchestrator | Phase | Field_ It Wires | DataService It Reads |
|---|-------------|-------|-----------------|---------------------|
| 1 | `ShopUIOrchestrator` | A | `Field_ShopCategory`, `Field_ShopItem`, `Field_ShopCartItem` | `ShopDataService` |
| 2 | `InventoryOrchestrator` | B | `Field_InventorySlot` | `InventoryDataService` |
| 3 | `QuestOrchestrator` | F | `Field_QuestItem` | `QuestDataService` |
| 4 | `ResearchOrchestrator` | F | `Field_ResearchItem` | `ResearchDataService` |
| 5 | `QuestHudOrchestrator` | F | `Field_QuestInfo`, `Field_QuestRequirement` | `QuestDataService` |
| 6 | `SaveFileOrchestrator` | G | `Field_SaveFileButton` | `SaveDataService` |
| 7 | `SettingsOrchestrator` | H | `Field_SettingSlider`, `Field_SettingToggle`, `Field_SettingKeybind` | `SettingsManager` |
| 8 | `ContractOrchestrator` | I | `Field_ContractInfo`, `Field_QuestRequirement` | `ContractDataService` |

**Total: 8 Orchestrators across all phases.**

### Orchestrator ↔ Field_ ↔ DataService Triad

```
SubManager.Start()
  │
  ├── creates DataService → dataService.Build(...)
  │
  └── orchestrator.Init(dataService)
        │
        └── orchestrator.BuildView()
              │
              ├── Instantiate(Field_ prefab)
              ├── field.SetData(...) ← reads DataService
              ├── field._button.onClick.AddListener(...) ← writes DataService
              └── DOC__[dataItem] = field ← tracks mapping
```

Every Orchestrator follows this exact flow. The SubManager owns lifecycle (open/close), the Orchestrator owns wiring (instantiate/populate/listen), the DataService owns data (build/query/modify), and the Field_ owns display (refs/setters).

---

## Splits — Original God-Objects → Clean Architecture

> **Rule:** One sentence per script. If it does more, split via DataService (data ops) or Orchestrator (Field_ wiring).
> Every split below was identified by analyzing the original main source for scripts doing 2+ responsibilities.

| Original | Lines | Responsibilities | Split Into |
|----------|-------|-----------------|------------|
| `ComputerShopUI` | 321 | panel lifecycle + Field_ instantiation + cart data + onClick wiring + currency refresh | `ShopUI` (lifecycle) + `ShopUIOrchestrator` (wiring) + `ShopDataService` (data) |
| `PlayerController` | 888 | movement + camera + grab + outline + inventory input | `PlayerMovement` + `PlayerCamera` + `PlayerGrab` + `FresnelHighlighter` |
| `PlayerInventory` | 418 | slot data + UI instantiation + hotbar keys + tool actions + drag-drop + display | `InventoryDataService` (data) + `InventoryOrchestrator` (wiring) + `Field_InventorySlot` (display) + `InventoryUI` (lifecycle) |
| `EconomyManager` | 162 | money + AllShopItems + unlock logic + ShopPurchases + price queries | `EconomyManager` (money only) + shop items/unlock/purchases → `ShopDataService` |
| `QuestManager` | 238 | quest collections + completion checks + reward distribution + progress + save/load | `QuestDataService` (collections + progress + queries) + `QuestManager` (lifecycle + event firing) |
| `ResearchManager` | 121 | completed list + tickets + afford checks + migration + lookup | `ResearchDataService` (all data ops) + `ResearchManager` (event firing shell) |
| `SavingLoadingManager` | 853 | 3 lookup dicts + save file paths + serialize + deserialize + screenshot + migration + atomic write | `SaveDataService` (lookups + paths + headers + formatting) + `SavingLoadingManager` (Unity lifecycle: scene load, Instantiate, Destroy) |
| `BuildingManager` | 404 | ghost lifecycle + placement validation + conveyor snap math + grid calc | `BuildingDataService` (pure math: snap, grid, validation) + `BuildingManager` (ghost: Instantiate, materials, layers) |
| `QuestTreeUI` | 198 | panel lifecycle + populate buttons + wire activate/pause/preview + draw connections + event subs | `QuestTreeUI` (lifecycle) + `QuestOrchestrator` (populate + wire + connections) |
| `ResearchTreeUI` | 144 | panel lifecycle + populate buttons + wire buy/preview + draw connections | `ResearchTreeUI` (lifecycle) + `ResearchOrchestrator` (populate + wire) |
| `QuestHud` | 73 | instantiate quest info prefabs + subscribe events + regenerate | `QuestHudOrchestrator` (instantiate + populate from QuestDataService) |
| `ContractsTerminalUI` | 85 | panel lifecycle + instantiate contract cards + wire active/inactive/claim | `ContractsUI` (lifecycle) + `ContractOrchestrator` (instantiate + wire) |
| `LoadingMenu` | 337 | panel lifecycle + instantiate save buttons + wire selection/load/delete + confirm dialogs + file info | `LoadingMenuUI` (lifecycle + dialogs) + `SaveFileOrchestrator` (button instantiation + wiring) |
| `SettingsMenu` | 306 | page switching + wire 15+ slider/toggle callbacks → SettingsManager | `SettingsUI` (lifecycle + pages) + `SettingsOrchestrator` (callback wiring) |

**Not split (fits one sentence despite size):**
- `CastingFurnace` (456 lines) — "I smelt ore." Every operation (queue, display, process, output, coal) is part of smelting. All need Unity API (triggers, coroutines, TMP_Text, Animator). Same precedent as ToolMagnet.
- `OrePiece` (443 lines) — "I'm a physical resource." Bulk is conversion variants (ToCrushed, ToPlate, ToRod, etc.) — same pattern repeated.

---

## Per-Phase Hierarchy (Complete)

> Full folder structure per phase — all numbered folders, all files.
> Domain subfolders under `3-MonoBehaviours/` when count gets noisy.

### Phase A — 22 scripts

```
phase-a/
├── 0-Core/
│   ├── Singleton.cs
│   └── GameEvents.cs                       (partial)
├── 1-Managers/
│   ├── EconomyManager.cs
│   ├── UIManager.cs
│   └── SubManager/
│       ├── ShopUI.cs
│       └── BgUI.cs
├── 2-Data/
│   ├── SO_ShopItemDef.cs
│   ├── SO_ShopCategory.cs
│   ├── SO_Interaction.cs
│   ├── Field_ShopCategory.cs
│   ├── Field_ShopItem.cs
│   ├── Field_ShopCartItem.cs
│   ├── Interface/
│   │   └── IInteractable.cs
│   ├── DataWrapper/
│   │   └── WShopItem.cs
│   └── DataService/
│       └── ShopDataService.cs              (nested: CartItem)
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   └── ShopUIOrchestrator.cs
│   ├── ShopTerminal.cs                     → "I fire open-shop event on interact"
│   ├── ShopSpawnPoint.cs                   → "I mark where purchased items spawn"
│   ├── SimplePlayerController.cs           → "I handle WASD + mouse look (replaced by Phase B)"
│   ├── InteractionSystem.cs                → "I raycast from camera and trigger IInteractable"
│   ├── InteractionWheelUI.cs               → "I show radial buttons for multi-option interactions"
│   └── MoneyHUD.cs                         → "I update money text on OnMoneyChanged"
├── 4-Utils/
│   ├── UtilsPhaseA.cs
│   └── PhaseALOG.cs
└── 5-Tests/
    ├── DEBUG_Check.cs
    ├── ShopUITest.cs
    ├── InteractionTest.cs
    └── PlayerControllerTest.cs
```

### Phase A½ — 2 scripts

```
phase-a-1/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnElevatorLanded, OnGamePaused)
└── 3-MonoBehaviours/
    ├── StartingElevator.cs                 → "I lower the player into the mine on scene start"
    └── CameraShaker.cs                     → "I add Perlin noise sway + view punch to camera"
```

### Phase B — 24 scripts

```
phase-b/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnToolSwitched, OnItemPickedUp, OnItemDropped)
├── 1-Managers/
│   └── SubManager/
│       └── InventoryUI.cs
├── 2-Data/
│   ├── SO_FootstepSoundDefinition.cs
│   ├── Field_InventorySlot.cs
│   ├── Interface/
│   │   ├── IIconItem.cs
│   │   └── ISaveLoadableObject.cs          (stub)
│   ├── DataWrapper/
│   │   └── WInventorySlot.cs
│   ├── DataService/
│   │   └── InventoryDataService.cs         (nested: Slot)
│   └── Entities/
│       ├── MagnetToolSelectionMode.cs
│       ├── SavableObjectID.cs              (stub)
│       └── HighlightStyle.cs
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   └── InventoryOrchestrator.cs
│   ├── Player/
│   │   ├── PlayerMovement.cs               → "I handle walk, sprint, duck, jump, slope sliding"
│   │   ├── PlayerCamera.cs                 → "I handle mouse look, FOV, camera bobbing"
│   │   ├── PlayerGrab.cs                   → "I grab physics objects with SpringJoint + LineRenderer"
│   │   ├── PlayerFootsteps.cs              → "I play footstep sounds based on movement"
│   │   └── PlayerSpawnPoint.cs             → "I mark where the player spawns"
│   ├── Tool/
│   │   ├── BaseHeldTool.cs                 → "I'm the base class for all equippable tools"
│   │   ├── ToolPickaxe.cs                  → "I swing and raycast-hit with delay"
│   │   ├── ToolMagnet.cs                   → "I pull nearby physics objects via spring joints"
│   │   ├── ToolHammer.cs                   → "I pick up / pack placed buildings"
│   │   ├── ToolMiningHat.cs                → "I toggle a light on equip/unequip"
│   │   ├── ToolSupportsWrench.cs           → "I toggle building supports on/off"
│   │   ├── ToolResourceScanner.cs          → "I show resource info on raycast hit"
│   │   └── ToolBuilder.cs                  → "I show ghost + place buildings" (partial — Phase D completes)
│   ├── Physics/
│   │   ├── BasePhysicsObject.cs            → "I accumulate conveyor velocities for FixedUpdate"
│   │   ├── BaseSellableItem.cs             → "I have a base sell value"
│   │   ├── PhysicsSoundPlayer.cs           → "I play sound on collision impact"
│   │   └── PhysicsGib.cs                   → "I'm a debris piece that despawns after time"
│   └── FresnelHighlighter.cs               → "I outline whatever the player looks at"
├── 4-Utils/
│   ├── UtilsPhaseB.cs
│   └── PhaseBLOG.cs
└── 5-Tests/
    ├── DEBUG_CheckB.cs
    ├── PlayerMovementTest.cs
    ├── PlayerGrabTest.cs
    └── InventoryTest.cs
```

### Phase C — 14 scripts

```
phase-c/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnOreMined, OnOreSold, OnOreLimitChanged)
├── 1-Managers/
│   └── OreManager.cs
├── 2-Data/
│   ├── SO_AutoMinerResourceDefinition.cs
│   ├── Interface/
│   │   └── IDamageable.cs
│   ├── DataWrapper/
│   │   └── WOrePiece.cs
│   ├── DataService/
│   │   └── OreDataService.cs
│   └── Entities/
│       ├── ResourceType.cs
│       ├── PieceType.cs
│       ├── OrePieceKey.cs
│       ├── OrePieceEntry.cs
│       ├── ResourceDescription.cs
│       ├── WeightedNodeDrop.cs
│       └── WeightedOreChance.cs
├── 3-MonoBehaviours/
│   ├── OreNode.cs                          → "I'm a breakable rock that drops ore pieces when mined"
│   ├── OrePiece.cs                         → "I'm a physical resource object with type + piece type"
│   ├── OrePiecePoolManager.cs              → "I recycle ore objects to avoid GC spikes"
│   ├── OreLimitManager.cs                  → "I throttle spawning when too many physics objects exist"
│   ├── AutoMiner.cs                        → "I spawn ore on a timer at a node"
│   ├── SellerMachine.cs                    → "I sell ore that enters my trigger for money"
│   ├── ParticleManager.cs                  → "I spawn particle prefabs at world positions"
│   └── PhysicsLimitUIWarning.cs            → "I show/hide ore limit warning text"
├── 4-Utils/
│   ├── UtilsPhaseC.cs                     (includes WeightedRandom<T>)
│   └── PhaseCLOG.cs
└── 5-Tests/
    ├── DEBUG_CheckC.cs
    └── OreTest.cs
```

### Phase D — 15 scripts

```
phase-d/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnBuildingPlaced, OnBuildingRemoved)
├── 1-Managers/
│   └── BuildingManager.cs
├── 2-Data/
│   ├── SO_BuildingInventoryDefinition.cs
│   ├── DataService/
│   │   └── BuildingDataService.cs
│   └── Entities/
│       ├── CanPlaceBuilding.cs
│       ├── PlacementNodeRequirement.cs
│       ├── SupportType.cs
│       └── BuildingRotationInfo.cs
├── 3-MonoBehaviours/
│   ├── Building/
│   │   ├── BuildingObject.cs               → "I'm a placed building with interact + supports"
│   │   ├── BuildingPlacementNode.cs        → "I'm a snap point that buildings attach to"
│   │   ├── BuildingCrate.cs                → "I'm a packed building on the ground"
│   │   ├── ModularBuildingSupports.cs      → "I spawn scaffolding legs via raycasts"
│   │   ├── ScaffoldingSupportLeg.cs        → "I'm one support leg segment"
│   │   └── BaseModularSupports.cs          → "I'm the base for support leg types"
│   ├── Conveyor/
│   │   ├── ConveyorBelt.cs                 → "I push physics objects forward in my trigger"
│   │   ├── ConveyorBeltManager.cs          → "I batch-apply conveyor velocities in FixedUpdate"
│   │   ├── ConveyorRenderer.cs             → "I scroll belt texture based on speed"
│   │   └── ConveyorSoundSource.cs          → "I mark my position for proximity sound"
│   └── ToolBuilder.cs                      → (completed: full placement logic from Phase B partial)
├── 4-Utils/
│   └── UtilsPhaseD.cs
└── 5-Tests/
    ├── BuildingTest.cs
    └── ConveyorTest.cs
```

### Phase E — 27 scripts

```
phase-e/
├── 2-Data/
│   ├── DataWrapper/
│   │   └── WBoxContents.cs
│   └── Entities/
│       ├── CastingMoldType.cs
│       ├── CastingFurnaceMoldRecipieSet.cs
│       ├── CastingFurnaceRecipie.cs
│       ├── CastingMoldRendererInfo.cs
│       └── BoxContentEntry.cs
├── 3-MonoBehaviours/
│   ├── Machine/
│   │   ├── CastingFurnace.cs               → "I smelt ore by majority type into output pieces"
│   │   ├── CastingFurnaceCoalInput.cs      → "I accept coal into the furnace"
│   │   ├── CastingFurnaceInteractionHandler.cs → "I handle furnace interact options"
│   │   ├── CastingFurnaceMoldArea.cs       → "I manage one mold slot on the furnace"
│   │   ├── BlastFurnace.cs                 → "I smelt with higher capacity (extends CastingFurnace)"
│   │   ├── CrusherMachine.cs               → "I crush ore into 2x smaller pieces"
│   │   ├── RollingMill.cs                  → "I flatten ingots into plates"
│   │   ├── PipeRoller.cs                   → "I roll ingots into pipes"
│   │   ├── RodExtruder.cs                  → "I extrude ingots into rods"
│   │   ├── ThreadingLathe.cs               → "I thread rods into threaded rods"
│   │   ├── PolishingMachine.cs             → "I gradually polish ore pieces to increase value"
│   │   ├── ClusterBreaker.cs               → "I break ore clusters into individual pieces"
│   │   ├── ShakerTable.cs                  → "I sieve crushed ore into refined outputs"
│   │   ├── SorterMachine.cs                → "I route ore to pass/fail outputs by filter"
│   │   ├── BulkSorter.cs                   → "I sort ore left/right/straight by dual filters"
│   │   ├── PackagerMachine.cs              → "I box loose ore into BoxObject containers"
│   │   ├── DepositBox.cs                   → "I animate a bucket elevator for selling"
│   │   ├── RapidAutoMiner.cs               → "I'm a faster auto-miner with drill bit"
│   │   └── OreAnalyzer.cs                  → "I display ore info on hover"
│   ├── Conveyor/
│   │   ├── ConveyorBlocker.cs              → "I stop ore on conveyor when closed"
│   │   ├── ConveyorSplitterT2.cs           → "I split conveyor flow into two outputs"
│   │   ├── RollerSplitter.cs               → "I split via roller direction"
│   │   └── RoutingConveyor.cs              → "I route ore to configurable output direction"
│   ├── BoxObject.cs                        → "I'm a box of packaged ore with manifest"
│   ├── BaseBasket.cs                       → "I track which ore pieces are inside my trigger"
│   ├── SorterFilterBasket.cs               → "I build a filter from ore placed in me"
│   └── Hopper.cs                           → "I funnel ore downward"
├── 4-Utils/
│   └── PhaseELOG.cs
└── 5-Tests/
    └── MachineTest.cs
```

### Phase F — 18 scripts

```
phase-f/
├── 0-Core/
│   └── GameEvents.cs                       (partial: OnQuestCompleted, OnQuestActivated, OnResearchCompleted)
├── 1-Managers/
│   ├── QuestManager.cs
│   ├── ResearchManager.cs
│   └── SubManager/
│       └── QuestTreeUI.cs
├── 2-Data/
│   ├── SO_QuestDefinition.cs
│   ├── SO_ResearchItemDefinition.cs        (abstract)
│   ├── SO_ShopItemResearchItemDef.cs
│   ├── SO_UpgradeDepositBoxResearchItemDef.cs
│   ├── Field_QuestItem.cs
│   ├── Field_ResearchItem.cs
│   ├── Field_QuestRequirement.cs
│   ├── Field_QuestInfo.cs
│   ├── DataWrapper/
│   │   └── WQuest.cs
│   ├── DataService/
│   │   ├── QuestDataService.cs
│   │   └── ResearchDataService.cs
│   └── Entities/
│       ├── QuestID.cs
│       ├── TriggeredQuestRequirementType.cs
│       ├── QuestRequirement.cs
│       ├── ResourceQuestRequirement.cs
│       ├── TriggeredQuestRequirement.cs
│       ├── TimedQuestRequirement.cs
│       ├── UnlockResearchQuestRequirement.cs
│       ├── ShopItemQuestRequirement.cs
│       ├── ActiveQuestEntry.cs
│       └── ResourceQuestRequirementEntry.cs
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   ├── QuestOrchestrator.cs
│   │   ├── ResearchOrchestrator.cs
│   │   └── QuestHudOrchestrator.cs
│   └── QuestHud.cs                         → "I show active quests on the HUD"
├── 4-Utils/
│   └── PhaseFLOG.cs
└── 5-Tests/
    ├── DEBUG_CheckF.cs
    └── QuestTest.cs
```

### Phase G — 8 scripts

```
phase-g/
├── 1-Managers/
│   ├── SavingLoadingManager.cs
│   └── AutoSaveManager.cs
├── 2-Data/
│   ├── Field_SaveFileButton.cs
│   ├── Interface/
│   │   ├── ISaveLoadableObject.cs          (expanded)
│   │   ├── ISaveLoadableBuildingObject.cs
│   │   ├── ISaveLoadableStaticBreakable.cs
│   │   ├── ISaveLoadableWorldEvent.cs
│   │   └── ICustomSaveDataProvider.cs
│   ├── DataService/
│   │   └── SaveDataService.cs
│   └── Entities/
│       ├── SaveFile.cs
│       ├── SaveEntry.cs
│       ├── SaveFileHeader.cs
│       ├── SaveFileHeaderFileCombo.cs
│       ├── SavableObjectID.cs              (expanded)
│       ├── SavableWorldEventType.cs
│       ├── WorldEventEntry.cs
│       ├── ShopPurchases.cs
│       ├── ShopObjectPurchaseEntry.cs
│       ├── BuildingObjectEntry.cs
│       └── OrePieceEntry.cs
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   └── SaveFileOrchestrator.cs
│   ├── SaveFileScreenshotCamera.cs         → "I capture a JPG screenshot for save files"
│   └── AutoSavingWarning.cs                → "I show auto-saving warning text briefly"
├── 4-Utils/
│   └── PhaseGLOG.cs
└── 5-Tests/
    └── SaveLoadTest.cs
```

### Phase H — 12 scripts

```
phase-h/
├── 1-Managers/
│   ├── SoundManager.cs
│   ├── SettingsManager.cs
│   ├── KeybindManager.cs
│   └── SubManager/
│       ├── PauseMenuUI.cs
│       └── SettingsUI.cs
├── 2-Data/
│   ├── SO_SoundDefinition.cs
│   ├── Field_SettingSlider.cs
│   ├── Field_SettingToggle.cs
│   ├── Field_SettingKeybind.cs
│   └── Entities/
│       ├── AudioClipDescription.cs
│       ├── KeybindEntry.cs
│       └── KeybindAction.cs
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   └── SettingsOrchestrator.cs
│   ├── SoundPlayer.cs                     → "I play one sound then return to pool"
│   ├── LoopingSoundPlayer.cs              → "I play/pause a looping AudioSource"
│   ├── LoopingSoundFader.cs               → "I fade a looping sound to target volume"
│   ├── ResolutionSetting.cs               → "I populate resolution dropdown"
│   ├── DisplayModeSetting.cs              → "I set fullscreen/windowed mode"
│   └── KeybindTokenText.cs               → "I replace [keybind] tokens in text with current bindings"
├── 4-Utils/
│   └── PhaseHLOG.cs
└── 5-Tests/
    └── SoundTest.cs
```

### Phase I — 18 scripts

```
phase-i/
├── 1-Managers/
│   ├── ContractsManager.cs
│   ├── MenuDataManager.cs
│   └── SubManager/
│       └── ContractsUI.cs
├── 2-Data/
│   ├── SO_ContractDefinition.cs
│   ├── Field_ContractInfo.cs
│   ├── DataWrapper/
│   │   └── WContractInstance.cs
│   ├── DataService/
│   │   └── ContractDataService.cs
│   └── Entities/
│       └── ContractInstance.cs
├── 3-MonoBehaviours/
│   ├── Orchestrator/
│   │   └── ContractOrchestrator.cs
│   ├── ContractsTerminal.cs               → "I'm an interactable that opens contracts panel"
│   ├── ContractSellTrigger.cs             → "I sell boxes deposited into my trigger"
│   ├── DetonatorExplosion.cs              → "I explode ore nodes in radius with physics force"
│   ├── DetonatorTrigger.cs                → "I arm and detonate on interact"
│   ├── DetonatorBuySign.cs                → "I sell detonator charges on interact"
│   ├── BreakableCrate.cs                  → "I break into gibs when hit + drop contents"
│   ├── EditableSign.cs                    → "I open text popup to edit my display text"
│   ├── ExtinguishableFire.cs              → "I can be extinguished by water"
│   ├── WaterVolume.cs                     → "I apply water effects to objects inside"
│   ├── MainMenu.cs                        → "I manage main menu buttons + elevator animation"
│   ├── LoadingMenu.cs                     → "I show save file browser + load/delete/new game"
│   ├── NewGameMenu.cs                     → "I show new game options + map select"
│   ├── MapSelectButton.cs                 → "I display one map option in new game menu"
│   └── EditTextPopup.cs                   → "I show a text input popup for signs"
├── 4-Utils/
│   └── PhaseILOG.cs
└── 5-Tests/
    └── ContractTest.cs
```

### Phase J — 9 scripts

```
phase-j/
├── 1-Managers/
│   ├── DebugManager.cs
│   ├── VersionManager.cs
│   ├── LevelManager.cs
│   └── DemoManager.cs
├── 2-Data/
│   └── Entities/
│       └── LevelInfo.cs
├── 3-MonoBehaviours/
│   ├── DebugOreSpawner.cs                 → "I spawn test ore on key press"
│   ├── ToolDebugSpawnTool.cs              → "I spawn any tool on key press"
│   ├── DisplacementMeshGenerator.cs       → "I generate displaced terrain meshes"
│   ├── VertexPainter.cs                   → "I paint vertex colors on terrain"
│   ├── DecalDestroyer.cs                  → "I clean up decals after time"
│   ├── ErrorMessagePopup.cs               → "I show error messages on exceptions"
│   └── InfoMessagePopup.cs                → "I show info messages to the player"
└── 5-Tests/
    └── DebugTest.cs
```

---

## Grand Totals

| Type | Count | Phases Present |
|------|-------|---------------|
| **SO_** | 12 | A, B, C, D, F, H, I |
| **Field_** | 13 | A, B, F, G, H, I |
| **DataService** | 8 | A, B, C, D, F(×2), G, I |
| **Orchestrator** | 8 | A, B, F(×3), G, H, I |
| **Managers** | 17 | A(2), C(1), D(1), F(2), G(2), H(3), I(2), J(4) |
| **SubManagers** | 6 | A(2), B(1), F(1), H(2), I(1) |
| **Vertical Slice Tests** | 15 | A(4), B(4), C(2), D(2), E(1), F(2), G(1), H(1), I(1), J(1) |

### All Managers (Singletons)

| # | Manager | Phase | One-Liner |
|---|---------|-------|-----------|
| 1 | `EconomyManager` | A | "I own money" |
| 2 | `UIManager` | A | "I report if any menu is open" (grows each phase) |
| 3 | `OreManager` | C | "I clean up invalid ore + hold resource descriptions via OreDataService" |
| 4 | `BuildingManager` | D | "I manage ghost preview, grid placement, materials" |
| 5 | `QuestManager` | F | "I manage quest lifecycle (activate, progress, complete)" |
| 6 | `ResearchManager` | F | "I manage research items (spend tickets, unlock)" |
| 7 | `SavingLoadingManager` | G | "I save/load game state to/from JSON" |
| 8 | `AutoSaveManager` | G | "I trigger auto-save on a timer" |
| 9 | `SoundManager` | H | "I pool AudioSources + distance culling" |
| 10 | `SettingsManager` | H | "I read/write PlayerPrefs for all settings" |
| 11 | `KeybindManager` | H | "I manage rebindable Input System keybinds" |
| 12 | `ContractsManager` | I | "I manage contract lifecycle (accept, fill, claim)" |
| 13 | `MenuDataManager` | I | "I manage main menu state + Steam news" |
| 14 | `DebugManager` | J | "I enable dev mode + debug keys" |
| 15 | `VersionManager` | J | "I hold the version string" |
| 16 | `LevelManager` | J | "I manage level list + scene lookup" |
| 17 | `DemoManager` | J | "I flag demo vs full version" |

### All Vertical Slice Tests

| # | Test | Phase | Type | Tests What | NOT Required |
|---|------|-------|------|-----------|-------------|
| 1 | `DEBUG_Check` | A | Data | ShopDataService (plain C#) | Everything |
| 2 | `ShopUITest` | A | UI | Full shop flow (keys) | Player, Interaction |
| 3 | `InteractionTest` | A | UI | Raycast + IInteractable + wheel | Shop, Player |
| 4 | `PlayerControllerTest` | A | UI | WASD + look + cursor | Shop, Interaction |
| 5 | `DEBUG_CheckB` | B | Data | InventoryDataService (plain C#) | Everything |
| 6 | `PlayerMovementTest` | B | UI | WASD + jump + sprint | Inventory, Tools |
| 7 | `PlayerGrabTest` | B | UI | SpringJoint grab on cubes | Inventory, Tools |
| 8 | `InventoryTest` | B | UI | Add/remove/switch tools | Player, Shop |
| 8b | `ToolActionTest` | B | UI | Pickaxe swing, magnet pull/launch/cycle, hammer, hat light | Shop, Ore |
| 9 | `DEBUG_CheckC` | C | Data | OreDataService (plain C#) | Everything |
| 10 | `OreTest` | C | UI | Spawn/mine/sell flow | Player |
| 11 | `BuildingTest` | D | UI | Place/rotate/snap | Player, Ore |
| 12 | `ConveyorTest` | D | UI | Ore flows along belt | Player, Shop |
| 13 | `MachineTest` | E | UI | Individual machine I/O | Player, Shop |
| 14 | `DEBUG_CheckF` | F | Data | QuestDataService + ResearchDataService | Everything |
| 15 | `QuestTest` | F | UI | Activate/progress/complete | Ore, Machines |

### All Manual Tests (`5-Tests/Manual/*.md`)

| # | File | Phase | Covers |
|---|------|-------|--------|
| 1 | `ShopUITest.md` | A | Cart add/remove, qty input, category tabs, purchase flow, Field_ prefab setup |
| 2 | `InteractionWheelTest.md` | A | Radial buttons spawn/destroy, single vs multi-option, E key flow |
| 3 | `ElevatorDescentTest.md` | A½ | Elevator lowers, Perlin shake, landing particles, view punch, roof collider |
| 4 | `InventoryUITest.md` | B | Drag-drop, hotbar↔extended, info panel, slot prefab, Canvas hierarchy |
| 5 | `ToolViewModelTest.md` | B | ViewModel equip/unequip swap, animation timing, pickaxe swing, magnet pull visual |
| 6 | `GrabRopeTest.md` | B | SpringJoint + LineRenderer visual — rope connects, follows, breaks, disappears |
| 7 | `FresnelHighlightTest.md` | B | Outline appears on hover (tools, grabbables), clears on look away |
| 8 | `MiningFlowTest.md` | C | Hit node → particles → health bar → shatter → ore pieces fly + bounce + settle |
| 9 | `AutoMinerVisualTest.md` | C | Rotator spins, ore spawns on timer, probability-based, rate adjustable |
| 10 | `SellerMachineTest.md` | C | Ore enters trigger → waits → money increases → ore returns to pool |
| 11 | `BuildingPlacementTest.md` | D | Ghost preview green/red, grid snap, rotation, mirror, conveyor auto-snap |
| 12 | `ConveyorFlowTest.md` | D | Belt texture scroll, ore moving along chain, splitter routing visual |
| 13 | `ScaffoldingTest.md` | D | Scaffolding legs raycast down, spawn dynamically, wrench toggle on/off |
| 14 | `FurnaceUITest.md` | E | Coal gauge needle, mold placement, liquid plane animation, output timing |
| 15 | `DepositBoxTest.md` | E | Bucket elevator animation, tier1/tier2 visual, belt renderer |
| 16 | `MachinePipelineTest.md` | E | Crusher→Furnace→RollingMill→Polish→Sort→Package end-to-end visual flow |
| 17 | `QuestTreeUITest.md` | F | Tree layout, connection lines, quest state colors, activate/pause, reward preview |
| 18 | `ResearchTreeUITest.md` | F | Research buttons, prerequisite lines, cost display, ticket count |
| 19 | `QuestHudTest.md` | F | Active quest cards on HUD, requirement progress updates, complete → remove |
| 20 | `SaveLoadUITest.md` | G | Save file list, screenshot thumbnail, load/delete/rename, auto-save warning |
| 21 | `SettingsUITest.md` | H | Slider drag, toggle click, keybind rebind popup, resolution dropdown |
| 22 | `PauseMenuTest.md` | H | Pause/unpause, time freeze, FPS cap, save/load/settings buttons |
| 23 | `ContractsUITest.md` | I | Contract cards, active/inactive swap, requirement fill, claim reward |
| 24 | `MainMenuTest.md` | I | New game → map select → load, elevator animation, Steam news panel |

---

> **This is the minimum confirmed structure** — derived from 100% main source analysis + GOAL.md architecture rules.
> Every file listed is required for 100% source fidelity. The hierarchy can only grow, not shrink.
> Files may be added, split, or merged as implementation reveals needs.

---

## phase-All — Shared Scripts

> Scripts that live outside any specific phase. Never duplicated. Grow as phases add entries.

```
phase-All/
├── 0-Core/
│   ├── Singleton.cs                    → generic singleton base — first instance wins
│   └── GameEvents.cs                   → core events (OnCloseAllSubManagers, LogSubscribersCount)
├── 1-Managers/
│   ├── UIManager.cs                    → menu state + close all + keyboard routing (grows 1-2 lines per phase)
│   └── EconomyManager.cs              → owns money (GetMoney, AddMoney, CanAfford)
├── 2-Data/
│   └── Enums/
│       └── GlobalEnumsAll.cs           → TagType enum (Grabbable, MarkedForDestruction, grows per phase)
└── 4-Utils/
    └── UtilsPhaseAll.cs                → HasTag/SetTag extensions (auto-available via C# extension methods)
```

**TagType** — all Unity inspector tags as an enum. Used via `HasTag(TagType.X)` / `SetTag(TagType.X)` extensions in UtilsPhaseAll — C# extensions, no class name needed. When adding a new tag value here, also add it in Unity: Edit → Project Settings → Tags and Layers.

---

## Gap Audit — Missing Files Per Phase (Minimal)

> Cross-referenced all ~270 original source files. Below are files missing from the hierarchy above.
> See `PhaseMap.md > Gap Audit` for full details + priority ratings.

### Phase B — add to existing folders
- `3-MonoBehaviours/Player/RigidbodyDraggerController.cs` → OnJointBreak auto-releases grab (new file)
- `3-MonoBehaviours/Tool/ToolHardHat.cs` → empty class extending ToolPickaxe (new file)
- `3-MonoBehaviours/UIRelay/UIEventRelay.cs` → generic EventSystem relay for drag-drop (new file)
- `BaseHeldTool.cs` → add `Equip()` / `UnEquip()` virtual methods (merge)
- `ToolMagnet.cs` → add `DetachBody(rb)` + `DroppedBodyInfo` cooldown + `_selectionModeText` (merge)
- `PlayerMovement.cs` → add noclip (V key) + mining hat dual-light system (merge)
- `InventoryOrchestrator.cs` → add selected item info panel + Equip/Drop buttons (merge)
- `UtilsPhaseB.cs` → add `SetLayerRecursively()` (merge)

### Phase C — add to existing folders
- `3-MonoBehaviours/DamageableOrePiece.cs` → OrePiece + IDamageable, breaks on collision
- `2-Data/Entities/OreLimitState.cs` → enum (Regular/SlightlyLimited/HighlyLimited/Blocked)

### Phase D — add to existing folders
- `3-MonoBehaviours/Building/ChuteHatch.cs` → IInteractable hatch toggle
- `3-MonoBehaviours/Building/ChuteTop.cs` → chute top piece
- `3-MonoBehaviours/Conveyor/ConveyorBatchRenderingComponent.cs` → batch rendering optimization
- `3-MonoBehaviours/Conveyor/ConveyorBeltShaker.cs` → visual shake
- `3-MonoBehaviours/Conveyor/ConveyorBeltShakerHorizontal.cs` → horizontal variant
- `3-MonoBehaviours/Conveyor/ConveyorBlockerT2.cs` → tier 2 blocker
- `3-MonoBehaviours/Building/RobotGrabberArm.cs` → automated ore grabber arm

### Phase E — add to existing folders
- `3-MonoBehaviours/Tool/ToolCastingMold.cs` → equippable mold tool for furnace
- `3-MonoBehaviours/Tool/RapidAutoMinerDrillBit.cs` → drill bit with durability
- `3-MonoBehaviours/Machine/CoalGaugeNeedle.cs` → visual gauge on furnace
- `3-MonoBehaviours/Machine/DepositBoxCrusher.cs` → crusher inside deposit box
- `3-MonoBehaviours/Machine/PackagerMachineInteractor.cs` → IInteractable for packager

### Phase F — add to existing folders
- `3-MonoBehaviours/Orchestrator/QuestTreeQuestInfoUI.cs` → quest info panel in tree (or Field_)
- `3-MonoBehaviours/Orchestrator/ResearchTreeSelectedResearchInfoUI.cs` → research info panel (or Field_)
- `2-Data/Entities/QuestPreviewRewardEntry.cs` → reward line item
- `2-Data/Entities/ShopItemQuestRequirementType.cs` → enum

### Phase G — add to 2-Data/Entities/
- `BaseHeldToolSaveData.cs`, `ToolMagnetSaveData.cs`, `ToolBuilderSaveData.cs`
- `RapidAutoMinerDrillBitToolSaveData.cs`, `AutoMinerSaveData.cs`, `BuildingCrateSaveData.cs`
- `CastingFurnaceSaveData.cs`, `RoutingConveyorSaveData.cs`
- `DetonatorExplosionSaveData.cs`, `EditableSignSaveData.cs`

### Phase H — add to existing folders
- `PlayerInputActions.cs` → auto-generated Input System action map (0-Core or 4-Utils)
- `BaseSettingOption.cs` → base class for setting UI (2-Data or 3-MonoBehaviours)
- `UIButtonSounds.cs` → hover/click sounds on UI buttons (3-MonoBehaviours)

### Phase I — add to existing folders
- `2-Data/Entities/DetonatorExplosionState.cs` → enum
- `2-Data/Entities/MenuData.cs` → main menu data container

### Utility — unassigned (suggest 0-Core or phase-specific Utils)
- `MathExtensions.cs` → math helpers (Phase B UtilsPhaseB or 0-Core)
- `TimeSince.cs` / `TimeUntil.cs` / `TimeUtil.cs` → time helper structs (0-Core)
- `CollisionDisabler.cs` / `TemporaryContinuousCollisionSetter.cs` → physics utilities (Phase B)