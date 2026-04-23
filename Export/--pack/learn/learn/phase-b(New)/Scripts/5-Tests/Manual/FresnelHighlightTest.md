# Fresnel Highlight — Manual Test Flow

> Verifies URP fresnel outlines appear on hover, correct color per object type, clears on look away.
> Temporary URP approach — will be replaced with Highlight Plus later.

---

## Prerequisites

- URP project with Shader Graph support
- Player GO with PlayerMovement + PlayerCamera
- FresnelHighlighter on Camera GO (or separate GO)
- Shader Graph + Renderer Feature configured (see setup below)

---

## Setup Guide

### Step 1 — "Highlighted" Layer

1. Edit → Project Settings → Tags and Layers
2. Add layer at slot 31 → name `"Highlighted"`

### Step 2 — Shader Graph: `Highlight_Fresnel_Additive`

1. Create → Shader Graph → URP → Unlit Shader Graph → name `Highlight_Fresnel_Additive`
2. Blackboard properties:
   - `_Color` (Color, default cyan `0.25, 0.85, 1, 1`)
   - `_Power` (Float, default `2`, range 0.5–8)
   - `_Intensity` (Float, default `1.2`, range 0–3)
3. Graph:
   ```
   Fresnel Effect (Power = _Power) → Multiply (_Intensity) → Multiply (_Color) → Emission
   ```
4. Graph Inspector: Surface = Transparent, Blend = Additive, Render Face = Both, ZWrite = Off
5. Save

### Step 3 — Material: `M_Highlight_Fresnel`

1. Create → Material → name `M_Highlight_Fresnel`
2. Shader: `Highlight_Fresnel_Additive`
3. Defaults: Color = cyan, Power = 2, Intensity = 1.2

### Step 4 — URP Renderer Feature

1. Project Settings → Graphics → click URP Renderer Data asset
2. Add Renderer Feature → **Render Objects**
3. Name: `FresnelHighlight`
4. Event: **AfterRenderingOpaques**
5. Filters → Layer Mask: **Highlighted**
6. Overrides → Material: `M_Highlight_Fresnel`
7. Overrides → Depth → Write: Off, Test: LessEqual

### Step 5 — FresnelHighlighter Component

1. Select Camera GO (or create separate GO)
2. Add `FresnelHighlighter` component
3. Wire:

| Field | Assign |
|-------|--------|
| `_cam` | Camera component |
| `_interactRange` | 2 |
| `_interactLayerMask` | "Interact" layer |
| `_highlightLayer` | 31 (or whichever slot "Highlighted" is at) |
| `_toolColor` | Cyan (0.25, 0.85, 1) |
| `_grabbableColor` | Cyan (0.25, 0.85, 1) |

### Step 6 — Player GO (if not already set up)

Use the player setup from GrabRopeTest.md Steps 1-6. Key requirement: `PlayerMovement` with `_playerCam` wired to Camera child. FresnelHighlighter needs the Camera for raycasting.

### Step 7 — Test Objects in Scene

Create 4 test objects. Place all in front of player spawn, within 2m reach:

**Object A — Tool on ground:**
1. Use any BaseHeldTool prefab (e.g. ToolPickaxe from ToolViewModelTest)
2. Must have: WorldModel with `Collider`, layer `"Interact"`
3. Place on ground

**Object B — Grabbable cube:**
1. GameObject → 3D Object → Cube
2. Add `Rigidbody` (mass 1)
3. Tag: `"Grabbable"`
4. Layer: `"Interact"`

**Object C — Non-interactable wall:**
1. GameObject → 3D Object → Cube (scale up to 3,3,1)
2. Layer: `"Default"` ← NOT "Interact"
3. No tag, no Rigidbody
4. Tests: FresnelHighlighter raycast won't hit this (wrong layer mask)

**Object D — Interactable but no matching type:**
1. GameObject → 3D Object → Cube
2. Layer: `"Interact"`
3. No `"Grabbable"` tag, no `BaseHeldTool` component
4. Tests: raycast hits but no highlight applied (no matching type check)

### Final Scene Hierarchy

```
Scene Root
├── Player (PlayerMovement, PlayerCamera)
│   └── Camera (Camera, FresnelHighlighter)
├── UIManager (phase-All)
├── ToolPickaxe_01 (on ground, layer Interact — Object A)
├── GrabbableCube (Rigidbody, tag Grabbable, layer Interact — Object B)
├── Wall (layer Default, no tag — Object C)
├── InteractCube (layer Interact, no tag, no BaseHeldTool — Object D)
├── Floor (Plane, layer Ground)
└── PlayerSpawnPoint
```

---

## How It Works (System Flow)

**Every frame:** `FresnelHighlighter.Update()` runs. First it calls `ClearAll()` — loops through all previously highlighted objects, **restores their original layer** via `UtilsPhaseB.SetLayerRecursively(go, originalLayer)`, and clears `MaterialPropertyBlock` on all renderers. Then `OutlineLookedAtThing()` raycasts from `_cam` forward using `_interactLayerMask`.

**Tool detected:** If the raycast hits a collider, it checks `hit.collider.GetComponentInParent<BaseHeldTool>()`. If found → calls `HighlightObject(hit.collider.gameObject, _toolColor)`. Inside: the object's **layer is swapped** to `_highlightLayer` (layer 31 = "Highlighted") via `UtilsPhaseB.SetLayerRecursively()`. The URP Renderer Feature is configured to render this layer with the **additive fresnel material** (`M_Highlight_Fresnel`) — so the object gets a second render pass with cyan rim glow. A `MaterialPropertyBlock` sets `_Color` on each renderer for per-object color control. The entry (GO + original layer) is stored for cleanup next frame.

**Grabbable detected:** If no tool found, checks `hit.collider.HasTag(TagType.Grabbable)`. If true → `HighlightObject(go, _grabbableColor)` — same layer-swap process, same or different color.

**No match:** If raycast misses or hits something with no matching type → no highlight applied. `ClearAll()` already restored last frame's layers, so nothing is on the "Highlighted" layer — no outline.

**Result:** Exactly one object is outlined at a time (or zero). Layer swap + restore happens within 1 frame — no fade, instant.

> **Note:** This is a **temporary** URP-native approach using Shader Graph + Renderer Feature. When Highlight Plus is imported, replace the layer-swap logic with `HighlightEffect.SetHighlighted(true/false)` per object.

---

## 1. Initial State

**DO:** Press Play, look at empty space (floor/sky)
**EXPECT:**
- No outlines visible anywhere
- No console errors from FresnelHighlighter

**Behind the scenes:** `FresnelHighlighter.Update()` raycasts but hits nothing (floor is layer "Default") → `OutlineLookedAtThing` returns early. `ClearAll()` has nothing to clear (empty list).

---

## 2. Look At Tool

**DO:** Aim crosshair at ToolPickaxe on ground (within 2m)
**EXPECT:**
- Cyan **outline appears** around pickaxe mesh (HP_Tool profile)
- Outline follows mesh shape — visible on all child renderers
- Outline is solid cyan, no glow, no see-through

**DO:** Move crosshair slightly off the tool (still nearby but not hitting collider)
**EXPECT:**
- Outline **disappears immediately** — no fade, instant clear

---

## 3. Look At Grabbable Cube

**DO:** Aim at Grabbable cube (within 2m)
**EXPECT:**
- Cyan outline appears (HP_Grabbable profile)
- Slightly thinner than tool outline (width 0.8 vs 1.0)

---

## 4. Look Away → Clear

**DO:** Look at Grabbable → quickly look at sky
**EXPECT:**
- Outline gone **within 1 frame** — ClearAll() runs every Update before OutlineLookedAtThing

**DO:** Look at Tool → look at Grabbable (switch between two objects)
**EXPECT:**
- Previous outline clears, new outline appears — only ONE object highlighted at a time

---

## 5. Out of Range

**DO:** Stand 5m away from tool → aim at it
**EXPECT:**
- No outline — `_interactRange` is 2m, raycast doesn't reach

**DO:** Walk closer until within 2m → aim at it
**EXPECT:**
- Outline appears as soon as raycast reaches

---

## 6. Non-Interactable Object (Wrong Layer)

**DO:** Aim at wall (layer "Default")
**EXPECT:**
- No outline — raycast uses `_interactLayerMask` which only hits "Interact" layer
- No console errors

---

## 7. Interactable But No Matching Type

**DO:** Aim at non-grabbable, non-tool cube (layer "Interact" but no tag, no BaseHeldTool)
**EXPECT:**
- No outline — FresnelHighlighter checks for `BaseHeldTool` component and "Grabbable" tag, neither matches
- Raycast hits but no highlight applied

---

## 8. Multiple Renderers (Child Meshes)

**DO:** Aim at a tool prefab that has multiple child mesh renderers in WorldModel
**EXPECT:**
- ALL child renderers get outlined — `GetComponentsInChildren<Renderer>()` catches all
- ParticleSystemRenderers are **excluded** (if any exist on the tool)

---

## 9. Rapid Look Switching

**DO:** Quickly alternate looking between Tool and Grabbable (wiggle mouse between them)
**EXPECT:**
- Outlines switch cleanly — no double-highlight, no stuck outlines
- No console errors or performance stutter
- ClearAll + OutlineLookedAtThing runs every frame

---

## 10. HighlightEffect Component Lifecycle

**DO:** Look at a cube that has never been highlighted → aim at it
**EXPECT:**
- `HighlightEffect` component **added at runtime** to the cube (check Inspector during Play mode)
- `highlighted = true`

**DO:** Look away
**EXPECT:**
- `HighlightEffect` component still on the cube but `highlighted = false`
- Component is reused next time you look at it (not re-added)

---

## Summary Checklist

- [ ] Tool on ground → cyan outline (HP_Tool profile)
- [ ] Grabbable cube → cyan outline (HP_Grabbable profile, slightly thinner)
- [ ] Look away → outline clears within 1 frame
- [ ] Only ONE object highlighted at a time
- [ ] Out of range (>2m) → no outline
- [ ] Wrong layer → no outline, no error
- [ ] Correct layer but no matching type → no outline
- [ ] Multiple child renderers → all outlined
- [ ] Rapid switching → no stuck outlines
- [ ] HighlightEffect added at runtime, reused on re-look
- [ ] Zero console errors throughout