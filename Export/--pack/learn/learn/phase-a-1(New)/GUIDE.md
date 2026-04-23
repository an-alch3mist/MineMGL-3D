# Phase A½ — The Mine: Environment & Elevator

> **Goal:** Replace the flat plane with an enclosed mine room. Elevator lowers player on scene start. All Phase A systems still work — just inside a mine.

---

## What Phase A½ Looks Like When Running

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   You hit Play. Screen is dark — you're underground.        │
│   Standing on a platform high above the mine floor.         │
│   Elevator starts descending immediately.                   │
│                                                             │
│   During descent:                                           │
│   - Platform shakes side-to-side (Perlin noise X/Z)         │
│   - Shake is strong at top, fades near bottom               │
│   - Speed is fast at top, decelerates smoothly              │
│   - Roof collider prevents jumping off                      │
│   - Camera has subtle ambient sway (barely noticeable)      │
│                                                             │
│   Near bottom (~1m above floor):                            │
│   - Dust/smoke particle burst plays                         │
│                                                             │
│   Elevator settles at floor level:                          │
│   - Shake stops completely                                  │
│   - Roof collider disables — look up freely                 │
│   - GameEvents.OnElevatorLanded fires                       │
│                                                             │
│   You're in an enclosed underground mine room:              │
│   - Rocky walls, dim point lights                           │
│   - Tunnel openings (empty, for future mining)              │
│   - Shop terminal against a wall — press E, still works     │
│   - ShopSpawnPoints near elevator shaft base                │
│                                                             │
│   Everything from Phase A works identically.                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Script Purpose

```
StartingElevator → "I lower the player into the mine on scene start"
CameraShaker     → "I add ambient Perlin noise sway + view punch to camera"
ElevatorTest     → "I test elevator descent independently"
```

---

## Folder Structure

```
Scripts/

0-Core/
└── GameEvents.cs           — Modify: add OnElevatorLanded, OnGamePaused, OnGameUnpaused

3-MonoBehaviours/
├── StartingElevator.cs     — code-driven elevator descent with Perlin shake
└── CameraShaker.cs         — ambient sway + one-shot view punch

5-Tests/
└── ElevatorTest.cs         — vertical slice test (R=restart, P=pause, U=unpause, V=punch)
```

---

## Hand-Typing Order

### Group 1: GameEvents modification
1. Add to existing `GameEvents.cs`: `OnElevatorLanded`, `OnGamePaused`, `OnGameUnpaused` + their Raise methods

### Group 2: CameraShaker → compile
2. `CameraShaker.cs` — 67 lines. No dependencies beyond Unity.

### Group 3: StartingElevator → compile
3. `StartingElevator.cs` — 107 lines. Depends on GameEvents + SimplePlayerController.

### Group 4: Test → **STOP & TEST**
4. `ElevatorTest.cs` — 50 lines.

**→ Vertical Slice Test: Elevator**

---

## Vertical Slice Test: Elevator

**Internal prerequisites:**
Singleton, GameEvents (with OnElevatorLanded, OnGamePaused, OnGameUnpaused), StartingElevator, CameraShaker

**External prerequisites:**
1. ElevatorPlatform GO — StartingElevator component, child cube (platform), child RoofCollider, child LandingParticle, child PlayerTeleportPosition
2. Camera with CameraShaker (low values: posAmplitude=0.01, rotAmplitude=0.1)
3. Ground plane
4. ElevatorTest on any GO — assign refs

**NOT required:** ShopUI, EconomyManager, InteractionSystem, UIManager

**Controls:** R=restart elevator, P=pause, U=unpause, V=view punch

**Checklist:**
- [ ] Elevator starts at height 15, descends to 0
- [ ] Shake visible during descent (X/Z Perlin noise)
- [ ] Shake fades as elevator approaches bottom
- [ ] Speed decelerates near bottom
- [ ] Roof collider active during descent
- [ ] Landing particle activates near bottom
- [ ] Elevator settles — shake stops, roof collider off
- [ ] Console shows: `[ElevatorTest] Elevator landed!`
- [ ] Camera has subtle ambient sway
- [ ] R key restarts descent
- [ ] V key applies view punch (camera kicks and recovers)
- [ ] P/U keys simulate pause/unpause (Phase H will wire sound)

---

## Scene Setup

| Name | Components | Notes |
|------|-----------|-------|
| `ElevatorPlatform` | StartingElevator | StartingHeight=15, EndHeight=0 |
| → `Platform` (child) | Cube, scale (3,0.2,3) | The platform player stands on |
| → `RoofCollider` (child) | Cube | Above platform, covers shaft. Initially inactive. |
| → `LandingParticle` (child) | ParticleSystem (optional) | At floor level. Initially inactive. |
| → `PlayerTeleportPos` (child) | Empty Transform | On the platform, pos (0,0.5,0) |
| `Camera` | Camera, CameraShaker | posAmplitude=0.01, rotAmplitude=0.1 |
| `Ground` | Plane | Floor of the mine |
| `[ElevatorTest]` | ElevatorTest | Assign elevator + shaker refs |

### Lighting (mine feel)
- Delete default Directional Light
- Add 3-4 Point Lights: range 8-12, intensity 0.5-1.0, warm orange or cool blue
- Ambient Color: very dark grey (0.05, 0.05, 0.08)

---

## Source vs Phase A½ — What Changed

| Area | Original Source | Phase A½ | Why |
|------|----------------|----------|-----|
| **Pause/resume** | Subscribes to `GameManager.GamePaused` singleton event | Subscribes to `GameEvents.OnGamePaused` | Decoupling |
| **Player teleport** | `FindObjectOfType<PlayerController>().TeleportPlayer()` | Finds `SimplePlayerController`, disable/set/enable CharacterController | Phase A uses SimplePlayerController, not full PlayerController |
| **New game check** | Checks `SavingLoadingManager.SceneWasLoadedFromNewGame` | Always lowers — no save system yet | Save/load is Phase G |
| **Sound** | `SoundPlayer.PlaySound(LoweringSoundDefinition)` | Placeholder stubs | Sound system is Phase H |
| **Landing event** | None — just sets `_isLowering = false` | `GameEvents.RaiseElevatorLanded()` | Decoupling — future systems can react |
| **Camera shaker** | Named `MainMenuCameraShaker` (menu-only) | Named `CameraShaker` (generic, reusable) | Works on any camera |

### What Stayed the Same (100% source behavior)
- Elevator movement math — identical `Mathf.Lerp` speed, `InverseLerp` progress, Perlin shake
- Roof collider pattern — enabled during descent, disabled on landing
- Landing particle — activates when close to bottom
- CameraShaker math — identical Perlin noise + SmoothDamp view punch
- `DefaultExecutionOrder(1000)` — elevator runs after all other scripts