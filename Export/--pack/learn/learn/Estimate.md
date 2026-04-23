# Estimate — Hand-Typing Timeline

> Calibrated from actual data: Phase A = 14h (22 scripts + architecture design), Phase A½ = 3h (4 scripts + scene work).
> Architecture is locked (GOAL.md). Patterns proven across Phase A + B.
> Script counts include all Gap Audit Critical + Important items from PhaseMap.md + StructureMap.md.
> Schedule: 4 hours/weekday, 8 hours/weekend = ~36 hours/week.

---

## Current Status

| Asset | Status | Details |
|-------|--------|---------|
| `handTyped(latest)/phase-a` | ✅ **Typed** | 26 scripts + 1 DEBUG_Check. User's ground-truth code. |
| `handTyped(latest)/phase-a-1` | ✅ **Typed** | 4 scripts (GameEvents, CameraShaker, StartingElevator, ElevatorTest). |
| `phase-All/` | ✅ **Typed** | 4 shared scripts (Singleton, GameEvents, EconomyManager, UIManager). |
| `phase-a-1(New)/` | ✅ **Generated** | Agent output: 4 .cs + GUIDE.md. User hand-typed from this. |
| `phase-b(New)/` | ✅ **Generated** | Agent output: 32 .cs + GUIDE.md + FLOW.md + 4 Manual/*.md. **Not yet hand-typed.** |
| Phase C–J | ❌ **Not generated** | Need agent generation → then hand-typing. |

**Completed: ~17h (Phase A 14h + Phase A½ 3h). Next: hand-type Phase B from generated code.**

---

## Per Phase — Actual Script Counts

> Counts from StructureMap per-phase hierarchy + Gap Audit Critical/Important additions.
> "Scripts" = .cs files to hand-type (includes tests). Manual test .md files listed separately.

| Phase | Base | +Gap | **Total .cs** | Manual .md | Difficulty | What's New / Hard | Est. Hours | Est. Days |
|-------|------|------|--------------|-----------|-----------|-------------------|-----------|-----------|
| **A** | 22 | — | **26** | 2 | Easy | Architecture design (one-time cost) | 14h | 3 ✅ Done |
| **A½** | 2 | — | **4** | 1 | Easy | Scene work (mine environment) | 3h | <1 ✅ Done |
| **B** | 24 | +8 | **32** | 4 | Hard | 888→4 split, tool inheritance (8 tools), InventoryDataService, drag-drop, SpringJoint grab | 24h | 6 days |
| **C** | 14 | +2 | **16** | 3 | Medium | Object pooling, weighted drops, IDamageable, DamageableOrePiece | 12h | 3 days |
| **D** | 15 | +7 | **22** | 3 | Hard | Ghost preview, grid snap, conveyor snap, chute system, robot arm | 20h | 5 days |
| **E** | 27 | +5 | **32** | 3 | Medium | Repetitive trigger I/O pattern, but high count. ToolCastingMold + DrillBit are new tools | 16h | 4 days |
| **F** | 18 | +4 | **22** | 3 | Medium | 2 DataServices, 3 Orchestrators, polymorphic quest requirements | 16h | 4 days |
| **G** | 8 | +10 | **18** | 1 | Hard | Save/load touches ALL phases. 10 save-data entities. ISaveLoadable wiring | 22h | 5-6 days |
| **H** | 12 | +3 | **15** | 2 | Easy | SoundManager pool, SettingsOrchestrator (15+ callbacks), Input System rebinding | 10h | 2-3 days |
| **I** | 18 | +2 | **20** | 2 | Easy | ContractDataService (follows quest pattern). Main menu + world events | 12h | 3 days |
| **J** | 9 | — | **9** | 0 | Easy | Debug singletons, demo mode, mesh generators | 5h | 1-2 days |
| | | | **216** | **24** | | | **154h** | |

### Gap Audit Additions (included above)

| Phase | +Scripts | Key Additions |
|-------|---------|--------------|
| B (+8) | RigidbodyDraggerController, ToolHardHat, UIEventRelay, + merges into BaseHeldTool (Equip/UnEquip), ToolMagnet (DetachBody+cooldown), InventoryOrchestrator (info panel), PlayerMovement (noclip), UtilsPhaseB (SetLayerRecursively) |
| C (+2) | DamageableOrePiece, OreLimitState enum |
| D (+7) | ChuteHatch, ChuteTop, RobotGrabberArm, ConveyorBatchRendering, ConveyorBeltShaker, ConveyorBeltShakerHorizontal, ConveyorBlockerT2 |
| E (+5) | ToolCastingMold, RapidAutoMinerDrillBit, CoalGaugeNeedle, DepositBoxCrusher, PackagerMachineInteractor |
| F (+4) | QuestTreeQuestInfoUI, ResearchTreeSelectedResearchInfoUI, QuestPreviewRewardEntry, ShopItemQuestRequirementType |
| G (+10) | BaseHeldToolSaveData, ToolMagnetSaveData, ToolBuilderSaveData, RapidAutoMinerDrillBitToolSaveData, AutoMinerSaveData, BuildingCrateSaveData, CastingFurnaceSaveData, RoutingConveyorSaveData, DetonatorExplosionSaveData, EditableSignSaveData |
| H (+3) | PlayerInputActions, BaseSettingOption, UIButtonSounds |
| I (+2) | DetonatorExplosionState enum, MenuData |

---

## Calibration — Why These Numbers

**Data point:** Phase A = 26 scripts in 14h (includes ~4h architecture design). Pure typing+testing rate = ~10h / 26 scripts = **~23 min/script average**.

| Script Complexity | Examples | Avg Time |
|-------------------|---------|----------|
| **Simple** (enums, stubs, interfaces, SOs, entities) | GlobalEnumsB, SavableObjectID, SO_FootstepSoundDef, OreLimitState | ~15 min |
| **Medium** (DataService, DataWrapper, Field_, Utils, tests) | InventoryDataService, WShopItem, Field_InventorySlot, PhaseBLOG | ~30 min |
| **Complex** (MonoBehaviours, Orchestrators, Managers, Player*) | PlayerMovement, ToolMagnet, InventoryOrchestrator, CastingFurnace | ~60 min |

Phase B breakdown: ~10 simple × 15min + ~12 medium × 30min + ~10 complex × 60min = 2.5 + 6 + 10 = 18.5h + ~5h scene/testing = **~24h**

**Agent generation saves ~30% vs designing from scratch** — code is ready to reference, GUIDE.md has typing order, FLOW.md explains connections. But you still read every line, type it, and test it.

---

## Why It's Faster Now

| Advantage | Impact |
|-----------|--------|
| **Architecture locked** | No more multi-day design sessions. GOAL.md defines everything. |
| **Agent generates first** | Full code + GUIDE + FLOW ready before you type. ~30% faster than Phase A's design-while-typing. |
| **Patterns repeat** | Phase E machines = same trigger I/O as Phase C. Phase I contracts = same as Phase F quests. |
| **DataService first** | Type DataService → test with `DEBUG_Check` → then wire UI. Bugs caught early. |
| **Orchestrator pattern** | SubManager (toggle) → Orchestrator (wire Field_) → DataService (data). Copy shape, change content. |
| **partial GameEvents** | Each phase extends in its own file. No merge conflicts. |
| **Tool inheritance** | Base class does heavy lifting. Concrete tools are ~10-30 lines each. |
| **Vertical slice tests** | Each system testable independently. Never wonder "did I break something else?" |

---

## Weekly Schedule

```
Weekday:  ~4 hours/day × 5 = 20 hours
Weekend:  ~8 hours/day × 2 = 16 hours
Weekly total: ~36 hours
```

## Timeline

| Week | Phases | Scripts | Hours | Milestone |
|------|--------|---------|-------|-----------|
| — | A + A½ | 30 | 17h | ✅ Done. Shop, interaction, economy, elevator, mine environment. |
| 1 | B (start) | 20 | 36h | Player movement + camera + grab + inventory working |
| 2 | B (finish) | 12 | 24h | All 8 tools + highlighting + tests. Full player system. |
| 3 | C | 16 | 12h | Mining + ore nodes + pooling + seller machine |
| 3-4 | D (start) | 12 | 20h | Building placement + ghost preview + grid snap |
| 4 | D (finish) | 10 | — | Conveyors carry ore end-to-end |
| 5 | E | 32 | 16h | Full factory pipeline (repetitive pattern — fastest scripts-per-hour) |
| 5-6 | F | 22 | 16h | Quest + research system |
| 6-7 | G | 18 | 22h | Save/load working — game persists across sessions |
| 7-8 | H | 15 | 10h | Full audio + settings + keybinds |
| 8 | I | 20 | 12h | Contracts + world events + main menu |
| 8 | J | 9 | 5h | Debug + demo + polish — **feature complete** |

**Typing: ~8 weeks. ~154h of hand-typing + testing.**
**Manual testing: 24 .md test guides, ~1h each = ~24h additional.**
**Grand total: ~178h to feature-complete.**

---

## Risk Factors

| Risk | Phase | Impact | Mitigation |
|------|-------|--------|------------|
| **Phase B complexity** | B | Hardest phase: 32 scripts, 888-line split, SpringJoint physics, drag-drop | Agent generated full code + 4 manual test guides |
| **Phase G cross-cutting** | G | Save/load touches every earlier phase (ISaveLoadable interfaces, save-data entities) | Interface stubs planted in Phase B. Each system adds its own ISaveLoadable. |
| **Phase E volume** | E | Most scripts (32) but lowest complexity-per-script | Repetitive trigger I/O pattern. Fastest phase by scripts/hour. |
| **Phase D new patterns** | D | Ghost preview + grid snap + conveyor snap = no prior pattern to copy | BuildingDataService isolates pure math. BuildingManager handles Unity lifecycle. |
| **Scene setup time** | All | Not captured in script hours — prefab hierarchies, inspector wiring, layers/tags | Manual test .md guides have step-by-step setup instructions |

---

## Script Counts — Grand Summary

| Category | Count | Source |
|----------|-------|--------|
| **Total .cs scripts** | 216 | StructureMap base (169) + Gap Audit Critical+Important (+41) + phase-All (4) + root DEBUG_Check (2) |
| **Manual test guides** | 24 | StructureMap "All Manual Tests" table |
| **SO_ types** | 12 | Across A, B, C, D, F, H, I |
| **Field_ types** | 13 | Across A, B, F, G, H, I |
| **DataServices** | 8 | A, B, C, D, F(×2), G, I |
| **Orchestrators** | 8 | A, B, F(×3), G, H, I |
| **Managers (singletons)** | 17 | Across all phases |
| **SubManagers** | 6 | A(2), B(1), F(1), H(2), I(1) |
| **Vertical Slice Tests (.cs)** | 15+ | Data-level (DEBUG_Check) + UI-level per system |
| **Original source files** | ~270 | Scripts/Assembly-CSharp/ — 100% fidelity target |

---

## The Payoff

```
Week 4-5: fully playable mining/factory simulation
  → mine ore nodes with pickaxe
  → ore flows through conveyors automatically
  → furnaces smelt by majority type
  → machines shape, polish, sort, package
  → seller machine converts to money
  → quest system guides progression

Week 8: feature-complete game
  → save/load persists everything
  → full audio + configurable settings
  → contracts loop + main menu
  → debug tools for testing
  → 216 scripts, 24 manual tests, zero tight coupling
  → every system tested independently via vertical slice
```

That's when it clicks — and the architecture proves itself.