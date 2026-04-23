# Shader & Material Guide

> Centralized reference for ALL shaders, materials, and URP settings across all phases.
> Every material the game needs is listed here with: Shader Graph setup, property values,
> which phase introduced it, which scripts reference it, and what URP Renderer settings are needed.
>
> **Target:** Unity 6000.3+ LTS with Universal Render Pipeline (URP).
> All Shader Graphs use the URP target. All materials use URP shaders (not Built-In).

---

## URP Project Setup (One-Time)

> Do this ONCE when setting up the project. All materials and Shader Graphs depend on this.

### Step 1 — Verify URP is Active

1. Edit → Project Settings → **Graphics**
2. **Scriptable Render Pipeline Settings** should show a `UniversalRenderPipelineAsset` (e.g. `URP-HighFidelity`)
3. If empty → you're on Built-In RP. Switch: Window → Package Manager → install `Universal RP`, then assign the asset.

### Step 2 — URP Renderer Data Asset

1. In Project Settings → Graphics → click the **Default Renderer** field (or click the URP asset → find `Renderer List`)
2. This opens the **URP Renderer Data** asset (e.g. `URP-HighFidelity-Renderer`)
3. This is where you add **Renderer Features** (like Render Objects for fresnel highlight)
4. **Remember this asset** — you'll come back here for each phase that needs a Renderer Feature

### Step 3 — Depth/Stencil Settings

On the URP Renderer Data asset:
- **Depth Texture**: ✅ enabled (needed for some effects)
- **Opaque Texture**: ✅ enabled (needed for Scene Color node if used)
- **Rendering Path**: Forward+ (default in Unity 6000.3)

### Step 4 — Shader Graph Compatibility

In Unity 6000.3+, Shader Graph uses the **URP** target by default:
- Create → Shader Graph → URP → Unlit Shader Graph (for additive/transparent effects)
- Create → Shader Graph → URP → Lit Shader Graph (for standard lit materials)
- **Do NOT use** "Built-In" target — it won't work with URP

---

## How to Use This Guide

1. **Find the material** you need in the tables below
2. **Create the Shader Graph** if it's a custom shader (step-by-step instructions provided)
3. **Create the Material** from that shader and set the listed property values
4. **Configure URP Renderer** if the material needs a Renderer Feature (Render Objects, Full Screen Pass, etc.)
5. **Assign in Inspector** — the "Used By" column tells you which script field to drag it into

---

## Phase B — Fresnel Highlight

### Shader Graph: `Highlight_Fresnel_Additive`

> Creates a cyan rim glow on object edges. Used by FresnelHighlighter to outline
> whatever the player looks at (tools, grabbables, buildings).

**Create the Shader Graph:**
1. Project panel → Create → Shader Graph → URP → **Unlit Shader Graph**
2. Name it `Highlight_Fresnel_Additive`
3. Open the graph → add these **Properties** (Blackboard, left panel):

| Property | Type | Default | Range |
|----------|------|---------|-------|
| `_Color` | Color | Cyan `(0.25, 0.85, 1, 1)` | — |
| `_Power` | Float | `2` | 0.5 – 8 |
| `_Intensity` | Float | `1.2` | 0 – 3 |

4. **Build the node graph:**
```
[_Power property] ──────► Fresnel Effect (Power input)
                                │
                          Fresnel output
                                │
                                ▼
                          Multiply (A)
[_Intensity property] ──► Multiply (B)
                                │
                          result
                                │
                                ▼
                          Multiply (A)
[_Color property] ────► Multiply (B)
                                │
                          final color
                                │
                                ▼
                     Fragment → Emission
```

5. **Graph Inspector** (gear icon, top-right):
   - Surface Type: **Transparent**
   - Blending Mode: **Additive**
   - Render Face: **Both**
   - ZWrite: **Off**
   - ZTest: **LessEqual**
6. Save the Shader Graph

### Material: `M_Highlight_Fresnel`

| Property | Value |
|----------|-------|
| Shader | `Highlight_Fresnel_Additive` |
| `_Color` | Cyan `(0.25, 0.85, 1, 1)` |
| `_Power` | `2` |
| `_Intensity` | `1.2` |

### URP Renderer Feature Setup (Unity 6000.3+)

> **Render Objects** Renderer Feature is confirmed available in Unity 6000.3 LTS.
> It renders objects on a specific layer with an override material — perfect for our highlight.

1. Project Settings → Graphics → click your **URP Renderer Data** asset (e.g. `URP-HighFidelity-Renderer`)
2. Scroll to bottom → **Add Renderer Feature** → select **Render Objects** (NOT "Full Screen Pass")
3. A new "Render Objects" entry appears. Configure it:

| Setting | Where to find it | Value |
|---------|-----------------|-------|
| Name | Top of the feature | `FresnelHighlight` |
| Event | Event dropdown | **AfterRenderingOpaques** |
| Filters → Queue | Filters section | **Opaque** |
| Filters → Layer Mask | Filters section | **Highlighted** only (uncheck everything else) |
| Overrides → Material | Overrides section, check "Material" | `M_Highlight_Fresnel` (drag from Project) |
| Overrides → Depth | Overrides section, check "Depth" | Write = **Off**, Test = **LessEqual** |

> **Important:** The Layer Mask in Filters must ONLY have "Highlighted" checked. If you leave "Default" or other layers checked, ALL objects will render with the fresnel material.

4. **Verify it works:** Enter Play mode with FresnelHighlighter active. Look at a Grabbable object. You should see cyan rim glow. If not:
   - Check the "Highlighted" layer exists at slot 31
   - Check `FresnelHighlighter._highlightLayer` is set to 31
   - Check `M_Highlight_Fresnel` material is assigned in the Render Objects feature
   - Check the URP Renderer Data asset is assigned in Project Settings → Graphics

### URP Renderer Data — Opaque Layer Mask Note

> **Critical:** On the same URP Renderer Data asset, find the **Opaque Layer Mask** setting (near the top). Make sure "Highlighted" is **UNCHECKED** here. This prevents the highlighted objects from rendering TWICE in the normal opaque pass. The Render Objects feature handles their rendering with the fresnel material instead.

### Layer Required

| Layer Name | Slot | Purpose |
|-----------|------|---------|
| `Highlighted` | 31 (or any free) | FresnelHighlighter swaps objects to this layer on raycast hit. URP Renderer Feature renders them with fresnel material. |

**Create:** Edit → Project Settings → Tags and Layers → User Layer 31 → type `Highlighted`

### Used By

| Script | Field | Phase |
|--------|-------|-------|
| `FresnelHighlighter.cs` | `_highlightLayer` = 31 | B |

---

## Phase D — Building Ghost Materials

> Transparent materials that show placement validity. BuildingManager swaps ALL renderers
> on the ghost building to one of these materials every frame during placement preview.
> **No custom Shader Graph needed** — use the built-in `Universal Render Pipeline/Unlit` shader.

**How to create each ghost material in Unity 6000.3:**
1. Project panel → Create → Material
2. In Inspector, change Shader dropdown to: **Universal Render Pipeline → Unlit**
3. Set **Surface Type** = Transparent (dropdown at top of material inspector)
4. Set **Base Color** including alpha (the A channel controls transparency)
5. Set **Render Face** = Both

### Material: `M_Ghost_Valid`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Unlit** |
| Surface Type | **Transparent** |
| Base Color | Green `(0.2, 1, 0.2, 0.3)` ← alpha 0.3 = 30% visible |
| Render Face | Both |

### Material: `M_Ghost_Invalid`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Unlit** |
| Surface Type | **Transparent** |
| Base Color | Red `(1, 0.2, 0.2, 0.3)` |
| Render Face | Both |

### Material: `M_Ghost_Requirement`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Unlit** |
| Surface Type | **Transparent** |
| Base Color | Yellow `(1, 1, 0.2, 0.3)` |
| Render Face | Both |

### Material: `M_BuildingNodeGhost`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Unlit** |
| Surface Type | **Transparent** |
| Base Color | Cyan `(0.2, 0.8, 1, 0.3)` |

> Shows on BuildingPlacementNode when the player is in build mode with a matching requirement type.

### Material: `M_BuildingNodeGhost_WrongType`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Unlit** |
| Surface Type | **Transparent** |
| Base Color | Grey `(0.5, 0.5, 0.5, 0.3)` |

> Shows on BuildingPlacementNode when the requirement type doesn't match.

### Material: `M_GreenLight`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Lit** |
| Surface Type | Opaque |
| Base Color | Green `(0.1, 0.9, 0.1)` |
| Emission | Green `(0.1, 0.9, 0.1)` × 2 |

> Used on AutoMiner and machine light indicators when ON/enabled.

### Material: `M_RedLight`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Lit** |
| Surface Type | Opaque |
| Base Color | Red `(0.9, 0.1, 0.1)` |
| Emission | Red `(0.9, 0.1, 0.1)` × 2 |

> Used on AutoMiner and machine light indicators when OFF/disabled.

### Material: `M_OrangeLight`

| Property | Value |
|----------|-------|
| Shader | **Universal Render Pipeline/Lit** |
| Surface Type | Opaque |
| Base Color | Orange `(1, 0.6, 0)` |
| Emission | Orange `(1, 0.6, 0)` × 2 |

> Used on machines in warning/transitional state.

**Emission Note (Unity 6000.3):** To enable emission on URP/Lit materials:
1. In the material inspector, scroll to **Emission**
2. Check the **Emission** checkbox to enable it
3. Set the **Emission Color** (click the color picker → set to the color above)
4. Set **Emission Intensity** by clicking the HDR color → use the Intensity slider (set to 2)
5. If emission doesn't glow in-game: ensure **Post Processing** is enabled on the Camera with **Bloom** effect in a Volume

### Layers Required

| Layer Name | Purpose | Used By |
|-----------|---------|--------|
| `BuildingGhost` | Ghost object during placement preview | BuildingManager.SetupGhostObject |
| `BuildingObject` | Placed buildings' physical colliders | BuildingObject.Start |

### Used By

| Script | Fields | Phase |
|--------|--------|-------|
| `BuildingManager.cs` | `GhostMaterial`, `InvalidGhostMaterial`, `RequirementGhostMaterial`, `BuildingNodeGhost`, `BuildingNodeGhost_WrongType`, `GreenLightMaterial`, `RedLightMaterial`, `OrangeLightMaterial` | D |
| `BuildingPlacementNode.cs` | reads from `BuildingManager.Ins.BuildingNodeGhost` / `BuildingNodeGhost_WrongType` | D |
| `AutoMiner.cs` | reads from `BuildingManager.Ins.GreenLightMaterial` / `RedLightMaterial` | C/D |
| `ChuteHatch.cs` | reads from `BuildingManager.Ins.GreenLightMaterial` / `RedLightMaterial` | D |

---

## All Layers Summary

| Layer | Slot | Introduced | Purpose |
|-------|------|-----------|---------|
| `Ground` | (default or custom) | B | PlayerMovement ground check |
| `Interact` | (custom) | B | FresnelHighlighter + InteractionSystem raycast |
| `Highlighted` | 31 | B | URP Renderer Feature renders fresnel outline |
| `BuildingGhost` | (custom) | D | Ghost preview during building placement |
| `BuildingObject` | (custom) | D | Placed buildings' physical colliders |

---

## All Tags Summary

| Tag | Introduced | Purpose |
|-----|-----------|---------|
| `Grabbable` / `TagType.grabbable` | B | PlayerGrab + FresnelHighlighter + SellerMachine |
| `MarkedForDestruction` / `TagType.markedForDestruction` | C | OrePiece during sell delay — prevents double-sell |

---

## Future Phases — Placeholder

### Phase E — Machine Materials
- Liquid plane materials (molten metal visual)
- Coal gauge needle material
- Polish brush material

### Phase H — UI/Sound Materials
- Settings panel materials
- Button hover/click materials

> These will be filled in when those phases are generated.