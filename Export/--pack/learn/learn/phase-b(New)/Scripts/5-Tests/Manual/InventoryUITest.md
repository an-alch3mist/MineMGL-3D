# Inventory UI — Manual Test Flow

> Step-by-step visual test. Follow in order. Each step says what to DO and what to EXPECT on screen.

---

## Prerequisites

- 4 tool prefabs in scene: Pickaxe, Magnet, Hammer, MiningHat (with WorldModel visible)
- ToolActionTest or InventoryTest script on a GO (for pickup keys)
- Canvas built per the UI Setup Guide below

---

## UI Setup Guide — Step by Step

> Build this ONCE. Save as scene. All tests reuse it.

### Step 1 — Canvas

1. GameObject → UI → Canvas
2. Canvas Scaler: **Scale With Screen Size**, Reference Resolution **1920×1080**, Match **0.5**
3. Add `EventSystem` (auto-created with Canvas, or GameObject → UI → EventSystem)

### Step 2 — HotbarPanel (bottom of screen)

1. Create Empty child of Canvas → name `HotbarPanel`
2. RectTransform: anchor **bottom-center**, pivot (0.5, 0), pos Y=10, width=800, height=70
3. Add `HorizontalLayoutGroup`:
   - Spacing: 4
   - Child Alignment: Middle Center
   - Control Child Size: Width ✓, Height ✓
   - Child Force Expand: Width ✓, Height ✗
4. Add `ContentSizeFitter`: Horizontal Fit = Preferred, Vertical Fit = Preferred (optional)

> This panel holds 10 hotbar slots. Slots are instantiated at runtime by InventoryOrchestrator.

### Step 3 — InventorySlot Prefab

Create this as a **prefab** (drag to Project after building):

```
InventorySlot (root GO)
│
│  Components on root:
│   - RectTransform: width=64, height=64
│   - Image (color: transparent or dark, raycastTarget = TRUE ← critical for drag-drop)
│   - Field_InventorySlot (wire all children below)
│
├── Background
│   - RectTransform: stretch to fill parent (anchor 0,0 to 1,1, offsets all 0)
│   - Image: color dark grey (0.2, 0.2, 0.2, 0.3)
│   - raycastTarget = false
│
├── Icon
│   - RectTransform: stretch to fill, padding 4px each side
│   - Image: default sprite = none, color white, enabled = FALSE (starts hidden)
│   - raycastTarget = false
│   - Preserve Aspect = true
│
├── NameText
│   - RectTransform: stretch to fill
│   - TMP_Text: text = "", font size 10, alignment center, color white
│   - raycastTarget = false
│
├── AmountText
│   - RectTransform: anchor bottom-right, pivot (1,0), width=30, height=20
│   - TMP_Text: text = "", font size 12, alignment right, color white, bold
│   - raycastTarget = false
│
├── OrangeBarThing
│   - RectTransform: anchor bottom-stretch, height=3, offset bottom=0
│   - Image: color orange (1, 0.6, 0, 1)
│   - raycastTarget = false
│   - Starts ACTIVE (hidden at runtime for extended slots via SetIsHotbar)
│
└── HideWhenDragged
    - RectTransform: stretch to fill
    - NO Image — just a wrapper GO
    - Parent Icon + NameText + AmountText inside this GO
    - (when dragging, this GO is SetActive(false) so slot looks empty)
```

**Wire Field_InventorySlot on root:**
- `_icon` → Icon/Image
- `_background` → Background/Image
- `_nameText` → NameText/TMP_Text
- `_amountText` → AmountText/TMP_Text
- `_orangeBarThing` → OrangeBarThing GO
- `_hideWhenDragged` → HideWhenDragged GO

**Save as prefab** → assign to InventoryOrchestrator `_pfInventorySlot`

### Step 4 — InventoryUI Panel (extended inventory)

1. Create Empty child of Canvas → name `InventoryUI`
2. RectTransform: anchor **center**, width=600, height=400
3. Add `Image`: dark background (0.1, 0.1, 0.1, 0.9)
4. Add `InventoryUI` component → wire `_orchestrator` (see Step 7)
5. Create child `ExtendedSlotsPanel`:
   - RectTransform: stretch to fill, padding 10
   - Add `GridLayoutGroup`: cell size 64×64, spacing 4×4, start corner Upper Left
   - This is `_extendedContainer` for InventoryOrchestrator

> InventoryUI starts **active** in scene. On play, `isFirstEnable` self-disables it.

### Step 5 — SelectedItemInfoPanel

1. Create Empty child of Canvas → name `SelectedItemInfoPanel`
2. RectTransform: anchor right-center, width=250, height=300
3. Add `Image`: panel background (0.15, 0.15, 0.15, 0.95)
4. Children:

```
SelectedItemInfoPanel
├── ItemIcon
│   - Image: width=80, height=80, top of panel
│   - Preserve Aspect = true
│
├── ItemNameText
│   - TMP_Text: font size 18, bold, top-center below icon
│
├── ItemDescText
│   - TMP_Text: font size 12, left-aligned, below name, word wrap
│
├── ItemAmountText
│   - TMP_Text: font size 14, below desc
│
├── EquipButton
│   - UI → Button (TMP), text "Equip"
│   - Child TMP_Text = _equipButtonText (changes to "Build" for ToolBuilder)
│
└── DropButton
    - UI → Button (TMP), text "Drop"
```

> Starts **inactive** — InventoryOrchestrator calls `UpdateSelectedItemInfo(null)` on init.

### Step 6 — DragGhostIcon

1. Create Empty child of Canvas → name `DragGhostIcon`
2. RectTransform: width=64, height=64 (same as slot)
3. Add `Image`: `_dragGhostImage` — shows dragged tool sprite
4. Add child `AmountText` → TMP_Text: `_dragGhostAmountText`
5. Set **inactive** in scene (starts hidden)
6. Move to **last sibling** in Canvas hierarchy (renders on top of everything)
7. On root Image: raycastTarget = **FALSE** (ghost must not block drop targets)

### Step 7 — InventoryOrchestrator GO

1. Create Empty in scene → name `InventoryOrchestrator`
2. Add `InventoryOrchestrator` component
3. Wire:

| Field | Drag From |
|-------|-----------|
| `_hotbarContainer` | HotbarPanel |
| `_extendedContainer` | InventoryUI/ExtendedSlotsPanel |
| `_pfInventorySlot` | InventorySlot prefab (from Project) |
| `_dragGhostIcon` | DragGhostIcon GO |
| `_dragGhostImage` | DragGhostIcon/Image |
| `_dragGhostAmountText` | DragGhostIcon/AmountText/TMP_Text |
| `_selectedItemInfo` | SelectedItemInfoPanel GO |
| `_selectedItemNameText` | SelectedItemInfoPanel/ItemNameText |
| `_selectedItemDescText` | SelectedItemInfoPanel/ItemDescText |
| `_selectedItemAmountText` | SelectedItemInfoPanel/ItemAmountText |
| `_selectedItemIcon` | SelectedItemInfoPanel/ItemIcon/Image |
| `_equipButtonText` | SelectedItemInfoPanel/EquipButton/TMP_Text |
| `_equipButton` | SelectedItemInfoPanel/EquipButton/Button |
| `_dropButton` | SelectedItemInfoPanel/DropButton/Button |

### Step 8 — Wire InventoryUI

1. Select `InventoryUI` GO
2. On `InventoryUI` component: `_orchestrator` → InventoryOrchestrator GO

### Step 9 — BgUI (blur/dim background)

1. Create Empty child of Canvas → name `BgUI`
2. RectTransform: stretch to fill entire screen
3. Add `Image`: color black (0, 0, 0, 0.5) — dims background when menu open
4. Add `bgUI` component (from Phase A)
5. Move **behind** all panels in hierarchy (renders first = behind)

### Final Canvas Hierarchy (top to bottom)

```
Canvas
├── BgUI                      — full-screen dim, behind everything
├── HotbarPanel               — 10 slots, always visible
├── InventoryUI               — extended panel, self-disables on start
│   └── ExtendedSlotsPanel    — 30 slots grid
├── SelectedItemInfoPanel     — tool details + equip/drop, starts inactive
├── DragGhostIcon             — follows cursor during drag, starts inactive, LAST = on top
└── EventSystem
```

> Order matters: later siblings render ON TOP of earlier siblings.

---

## How It Works (System Flow)

Before testing, understand how the inventory data flows:

**Scene loads:** `InventoryUI` GO starts active → `OnEnable` fires with `isFirstEnable = true` → it calls `_orchestrator.Init(dataService)`. Inside Init, `InventoryDataService.Build()` creates 40 `Slot` objects (10 hotbar + 30 extended). `InventoryOrchestrator.BuildSlotFields()` instantiates 40 `Field_InventorySlot` prefabs — first 10 go under `_hotbarContainer`, rest under `_extendedContainer`. For each slot, it calls `AddComponent<UIEventRelay>()` and wires drag/pointer callbacks. Then `InventoryUI` subscribes to `OnOpenInventoryView` and `OnCloseAllSubManagers`, **self-disables** (`SetActive(false)`), and returns. The extended panel is now hidden but hotbar is visible (it's a separate container that stays active).

**Tool pickup:** Player interacts with a tool → `BaseHeldTool.Interact(InteractionType.Take)` fires `GameEvents.RaiseToolPickupRequested(this)` → `InventoryOrchestrator.HandleToolPickup` receives it → `dataService.TryAdd(tool)` finds first empty slot → tool's GO is `SetActive(false)` (WorldModel disappears from ground). If `EquipWhenPickedUp`, it calls `SwitchToSlot(idx)` which sets the tool's GO active again → `BaseHeldTool.OnEnable` parents it to `ViewModelContainer` → `GameEvents.RaiseToolEquipped(tool)` fires → `PlayerGrab` sets `tool.Owner = _movement`. Finally `RefreshAllSlots()` loops all 40 `Field_InventorySlot` and calls `SetData(icon, name, qty)` or `SetEmpty()`.

**Drag-drop:** When you click-and-drag a slot, `UIEventRelay.OnBeginDrag` fires → orchestrator stores `dragFromIndex`, calls `FIELD_SLOT[i].SetDragVisible(false)` which **hides the `HideWhenDragged` child GO** (`SetActive(false)`) — the slot *looks empty*. The `DragGhostIcon` GO **activates** with the dragged item's sprite. Every frame during drag, `OnDrag` moves the ghost to cursor position. On drop over another slot, `UIEventRelay.OnDrop` fires → `dataService.Swap(from, to)` exchanges the two slots in data → `RefreshAllSlots()` updates all displays. `OnEndDrag` restores the source slot visual and **deactivates** the ghost.

**Open/close inventory:** `UIManager.Update()` detects Tab key → fires `GameEvents.RaiseOpenInventoryView()` → `InventoryUI` receives → `SetActive(true)` → `OnEnable` fires (not first time) → `GameEvents.RaiseMenuStateChanged(true)` → `UIManager` sets `isAnyMenuOpen = true`, `PlayerMovement` unlocks cursor + disables input. Closing: `InventoryUI.Update()` detects ESC/Tab → fires `RaiseCloseInventoryView()` → `SetActive(false)` → `OnDisable` → `RaiseMenuStateChanged(false)` → cursor locks, input re-enables.

---

## 1. Initial State (Scene Start)

**DO:** Press Play
**EXPECT:**
- Hotbar visible at bottom — 10 empty slots, each with `OrangeBarThing` visible
- Extended inventory panel is **hidden** (InventoryUI self-disabled on first enable)
- SelectedItemInfoPanel is **hidden**
- DragGhostIcon is **hidden**
- All slots: dark background (`_notSelectedColor`), no icon, no text
- Cursor is **locked** (FPS mode)

**Behind the scenes:** `InventoryUI.OnEnable()` ran with `isFirstEnable = true` → called `_orchestrator.Init()` → built 40 slots → subscribed to events → called `SetActive(false)` on itself. `OnDisable` fired → `RaiseMenuStateChanged(false)` → cursor locked.

---

## 2. Pick Up First Tool

**DO:** Walk near ToolPickaxe on ground → press `Space` (fires RaiseToolPickupRequested)
**EXPECT:**
- Pickaxe WorldModel **disappears** from ground
- Hotbar slot 0 shows **pickaxe icon** + highlight color (`_selectedColor`)
- Slots 1-9 remain empty/dark
- Pickaxe ViewModel **appears** in front of camera (parented to ViewModelContainer)
- Console: `[GameEvents] OnToolPickupRequested raised for -> 1 subscribers`
- Console: `[GameEvents] OnItemPickedUp raised for -> N subscribers`

**Behind the scenes:** `GameEvents.RaiseToolPickupRequested(tool)` → `InventoryOrchestrator.HandleToolPickup` → `dataService.TryAdd(tool)` returns index 0 → `tool.gameObject.SetActive(false)` (WorldModel gone) → `SwitchToSlot(0)` → `tool.gameObject.SetActive(true)` → `BaseHeldTool.OnEnable()` parents to `Owner.ViewModelContainer` → `RaiseToolEquipped` → `PlayerGrab` sets `tool.Owner` → `RefreshAllSlots()` → `FIELD_SLOT[0].SetData(icon, "Pickaxe", 1)` + `SetHighlighted(true)` → `_background.color = _selectedColor`.

---

## 3. Pick Up More Tools

**DO:** Press `U` (Magnet), `I` (Hammer), `O` (MiningHat)
**EXPECT:**
- Slot 0: Pickaxe icon (highlighted — active)
- Slot 1: Magnet icon (not highlighted)
- Slot 2: Hammer icon (not highlighted)
- Slot 3: MiningHat icon (not highlighted)
- Slots 4-9: empty
- Only Pickaxe ViewModel visible (it was first, auto-equipped)

---

## 4. Switch Tools via Number Keys

**DO:** Press `2`
**EXPECT:**
- Slot 0: Pickaxe icon → highlight **off**
- Slot 1: Magnet icon → highlight **on** (`_selectedColor`)
- Pickaxe ViewModel **hidden**
- Magnet ViewModel **visible**

**DO:** Press `1`
**EXPECT:**
- Slot 1 highlight off, slot 0 highlight on
- Magnet ViewModel hidden, Pickaxe ViewModel visible

**DO:** Press `1` again (same slot)
**EXPECT:**
- Slot 0 highlight **off** — tool deselected (toggle behavior)
- Pickaxe ViewModel **hidden**
- No tool active

---

## 5. Scroll Wheel Cycling

**DO:** Scroll wheel up
**EXPECT:**
- Cycles to next occupied slot (skips empty)
- Highlight moves, ViewModel swaps

**DO:** Scroll wheel down
**EXPECT:**
- Cycles to previous occupied slot
- Wraps around within hotbar (slot 3 → 0 → 3)

---

## 6. Tool Actions While Equipped

**DO:** Equip Pickaxe (press `1`) → hold left-click
**EXPECT:**
- Swing animation plays on ViewModel (Attack1 state)
- Repeats at cooldown rate while held

**DO:** Equip Magnet (press `2`) → hold right-click near Grabbable cubes
**EXPECT:**
- Nearby cubes fly toward camera via SpringJoints
- Cubes jitter near pull origin (spring physics)

**DO:** While magnet is holding cubes → press left-click
**EXPECT:**
- Cubes launch **forward** (push force)
- SpringJoints destroyed, cubes fly away

**DO:** Hold right-click again to grab cubes → press `R`
**EXPECT:**
- Cubes drop **gently** (low force)

**DO:** Press `Q`
**EXPECT:**
- Console: selection mode changes
- TMP text on magnet ViewModel updates: "Everything" → "ResourcesNotInFilter" → "ResourcesNotOnConveyors" → wraps

**DO:** Equip MiningHat (press `4`) → press left-click
**EXPECT:**
- Light on ViewModel toggles on/off

---

## 7. Drop Tool

**DO:** Equip Pickaxe (press `1`) → press `G`
**EXPECT:**
- Pickaxe ViewModel **disappears**
- Pickaxe WorldModel **appears** in front of camera, flies forward with velocity
- Slot 0 becomes **empty** (no icon, dark background)
- Slot 1 (Magnet) does NOT become active — no auto-switch
- Console: `OnItemDropped` fired

**Behind the scenes:** `InventoryOrchestrator.HandleDropActiveTool()` → `tool.DropItem()` → `SetActive(true)`, `HideWorldModel(false)` (WorldModel visible), `HideViewModel()` (ViewModel hidden), unparent from ViewModelContainer, `rb.linearVelocity = cam.forward * 5f` → `Owner = null` → `dataService.Remove(tool)` → slot[0].Tool = null → `RaiseItemDropped(tool)` → `RefreshAllSlots()` → `FIELD_SLOT[0].SetEmpty()`.

---

## 8. Open Extended Inventory (Tab)

**DO:** Press `Tab`
**EXPECT:**
- Extended inventory panel **appears** (30 slots, grid layout)
- Cursor **unlocks** (visible, menu mode)
- Player movement/look **disabled** (isAnyMenuOpen = true)
- Extended slots 0-29 all empty
- Hotbar slots still show tools (Magnet, Hammer, MiningHat in slots 1-3)

**Behind the scenes:** `UIManager.Update()` → `!isAnyMenuOpen && Tab pressed` → `GameEvents.RaiseOpenInventoryView()` → `InventoryUI` receives (subscribed in first OnEnable) → `this.gameObject.SetActive(true)` → `OnEnable` fires (isFirstEnable is false) → `GameEvents.RaiseMenuStateChanged(true)` → `UIManager.isAnyMenuOpen = true` + `PlayerMovement` receives → `isAnyMenuOpen = true` → cursor unlocks, WASD/look disabled. The `InventoryUI` GO becoming active makes the `ExtendedSlotsPanel` child visible (it's inside the InventoryUI GO).

---

## 9. Click Slot → Selected Item Info

**DO:** Click on hotbar slot 1 (Magnet)
**EXPECT:**
- SelectedItemInfoPanel **appears**
- Name text: "Magnet"
- Description text: tool description
- Icon: magnet sprite
- Amount text: empty (qty = 1)
- Equip button text: "Equip"

**DO:** Click on hotbar slot 3 (MiningHat)
**EXPECT:**
- Info panel updates to MiningHat data
- Previous selection (Magnet) no longer shown

**DO:** Click on empty slot
**EXPECT:**
- SelectedItemInfoPanel **hides**

**Behind the scenes:** `UIEventRelay.OnPointerDown` fires → `InventoryOrchestrator.UpdateSelectedItemInfo(slot[i].Tool)`. If tool != null: `_selectedItemInfo.SetActive(true)`, fills name/desc/icon/amount texts, sets equip button text to "Equip" (or "Build" for ToolBuilder). If tool == null: `_selectedItemInfo.SetActive(false)`.

---

## 10. Equip From Info Panel

**DO:** Click slot 1 (Magnet) → click "Equip" button
**EXPECT:**
- Magnet becomes active tool (slot 1 highlighted)
- Extended inventory **closes** (Tab auto-close)
- Cursor **re-locked** (FPS mode)
- Magnet ViewModel visible

**Behind the scenes:** `EquipSelectedTool()` → finds slot index for selected tool via `dataService.GetIndexFor(tool)` → `SwitchToSlot(idx)` → previous tool hides, Magnet `SetActive(true)` → `OnEnable` parents to ViewModelContainer → `RaiseToolEquipped` → `PlayerGrab` sets Owner → then `GameEvents.RaiseCloseInventoryView()` → `InventoryUI.SetActive(false)` → `OnDisable` → `RaiseMenuStateChanged(false)` → cursor locks.

---

## 11. Drop From Info Panel

**DO:** Press `Tab` → click slot 2 (Hammer) → click "Drop" button
**EXPECT:**
- Hammer WorldModel appears in world
- Slot 2 becomes empty
- SelectedItemInfoPanel **hides**
- Extended inventory stays open

**Behind the scenes:** `DropSelectedTool()` → `tool.DropItem()` (WorldModel visible, ViewModel hidden, rb velocity forward, Owner null) → `dataService.Remove(tool)` → `RaiseItemDropped(tool)` → `UpdateSelectedItemInfo(null)` → `_selectedItemInfo.SetActive(false)` → `RefreshAllSlots()`.

---

## 12. Drag-Drop: Hotbar ↔ Extended

**DO:** Press `Tab` to open extended → drag slot 1 (Magnet) onto extended slot 10
**EXPECT during drag:**
- Slot 1's `HideWhenDragged` content **disappears** (slot looks empty)
- DragGhostIcon **appears** at cursor with magnet sprite
- Ghost follows mouse movement

**EXPECT on drop:**
- Slot 1 becomes **empty**
- Extended slot 10 shows **Magnet icon**
- DragGhostIcon **hidden**
- Slot 1 `HideWhenDragged` content **restored**

**Behind the scenes (drag):** `UIEventRelay[1].OnBeginDrag` → `HandleBeginDrag(relay, e)` → `dragFromIndex = 1` → `FIELD_SLOT[1].SetDragVisible(false)` (→ `_hideWhenDragged.SetActive(false)` — slot content hidden) → `_dragGhostIcon.SetActive(true)` + `_dragGhostImage.sprite = magnet icon` + `SetAsLastSibling()` (renders on top). Every frame: `HandleDrag(e)` → `_dragGhostIcon.position = e.position`.
**Behind the scenes (drop):** `UIEventRelay[10].OnDrop` → `HandleDrop(relay, e)` → `dataService.Swap(1, 10)` (exchanges Slot objects in the list) → `RefreshAllSlots()`. Then `UIEventRelay[1].OnEndDrag` → `HandleEndDrag` → `FIELD_SLOT[1].SetDragVisible(true)` (→ `_hideWhenDragged.SetActive(true)`) → `_dragGhostIcon.SetActive(false)`.

**DO:** Drag extended slot 10 (Magnet) back to hotbar slot 1
**EXPECT:**
- Magnet returns to slot 1
- Extended slot 10 becomes empty

---

## 13. Drag-Drop: Swap Two Occupied Slots

**DO:** Drag slot 1 (Magnet) onto slot 3 (MiningHat)
**EXPECT:**
- Slot 1 now shows **MiningHat** icon
- Slot 3 now shows **Magnet** icon
- Tools swapped — no data lost

---

## 14. Drag Outside UI → Drop to World

**DO:** Drag slot 3 (Magnet, after swap) → release mouse outside any slot (over the 3D viewport)
**EXPECT:**
- Magnet WorldModel **appears** in world (dropped)
- Slot 3 becomes **empty**
- DragGhostIcon **hidden**
- Console: `OnItemDropped` fired

---

## 15. Close Extended Inventory

**DO:** Press `Tab` or `Escape`
**EXPECT:**
- Extended inventory panel **hides**
- Cursor **re-locks** (FPS mode)
- Player movement/look **re-enabled**
- SelectedItemInfoPanel **hides** if it was open
- Hotbar remains visible with remaining tools

---

## 16. Edge Cases

**DO:** Try to pick up tool when all 40 slots are full
**EXPECT:**
- Console: `"inventory full"` in red
- Tool stays on ground

**DO:** Scroll wheel with only 1 tool in hotbar
**EXPECT:**
- No change — can't cycle with one item

**DO:** Press `1` with no tool in slot 1
**EXPECT:**
- No crash, no ViewModel shown, slot stays empty

**DO:** Open extended inventory → press `Escape`
**EXPECT:**
- Inventory closes (same as Tab)

---

## Summary Checklist

- [ ] Initial state: 10 empty hotbar slots, no extended, no info panel
- [ ] Pickup: tool enters first empty slot, icon shows, ViewModel appears
- [ ] Switch: number keys swap highlight + ViewModel, same-key toggles off
- [ ] Scroll: cycles occupied slots, wraps in hotbar
- [ ] Pickaxe swing: animation plays on hold
- [ ] Magnet pull/launch/drop/cycle: all work
- [ ] MiningHat light toggle: on/off per click
- [ ] Drop (G): WorldModel appears, slot empties
- [ ] Tab: opens/closes extended panel, cursor locks/unlocks
- [ ] Click slot: info panel shows tool data
- [ ] Equip button: equips + closes inventory
- [ ] Drop button: drops from info panel
- [ ] Drag hotbar→extended: tools swap
- [ ] Drag extended→hotbar: tools swap back
- [ ] Drag two occupied: swap both
- [ ] Drag outside: drops tool to world
- [ ] Full inventory: shows error, tool stays
- [ ] Empty slot click: no crash