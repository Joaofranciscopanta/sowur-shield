# Sowur Shield — Project Status

> Last updated: 2026-06-13
> Branch: `main`
> This document supersedes and replaces: ROADMAP.md, GAME_DEVELOPMENT_PLAN.md,
> COMBAT_PIPELINE_STATUS.md, DEVELOPMENT_LOG.md, COMBAT_SETUP_GUIDE.md.
> For a deep code-quality review (architecture map, findings, atomic task worklist), see `/review/`.
> For product/design ideas and a full design-oriented audit, see `FULL_GAME_PROJECT_AUDIT.md`
> (kept separate — design/feature brainstorming, not technical status).

---

## Current State Overview

Sowur Shield is a 2D farming sim + auto-battler hybrid. The farming/economy/animal/dialogue/quest
side is production-ready and well-tested. The combat loop is mechanically complete and its
end-to-end pipeline (farm → team select → battle → rewards → back to farm) is structurally
sound based on a fresh source read.

- **4 scenes**: MainMenu, SampleScene (farm), CombatScene, MapEditorScene
- **154 scripts** under `Assets/Scripts/`, **100% namespace-compliant** (`SowurShield.<System>`)
- **413 test methods** across **16 test files** (EditMode + PlayMode)
- **3 assembly definitions**: `SowurShield.Runtime`, `SowurShield.Editor`,
  `SowurShield.Dialogue.Editor`

---

## What's Complete

### Core Gameplay
- Player movement (WASD, dash, animations), new Input System (`PlayerControls.inputactions`)
- Interaction system: E-key proximity (`InteractionManager`) + left-click sprite collision
  (`CursorController`), priority-based (objects > tools)
- Time & season cycle: 28-day year (4×7-day seasons), `OnDayChanged`/`OnSeasonChanged` events
- Save/Load: multi-slot (AutoSave + Slot1-3), `ISaveable` registry (14 implementers), legacy
  flat-save migration to AutoSave on first boot

### Farming
- Soil state machine: Regular → Tilled → Watered → WithCrop, event-driven, no issues found
- Crop growth: multi-stage, per-crop config via `CropData` ScriptableObject, water requirement
  with drought-death, regrowth, seasonal restrictions (Greenhouse bypasses restriction)
- Crops: Carrot, Cabbage, Tomato (Summer), Winter Wheat (Winter)
- DualGridTilemap: 16-tile rule-based rendering

### Economy
- Inventory: 36 slots (9 hotbar + 27 storage), drag/drop, stacking, tooltips
- SellBox: auto-sell on sleep (80% via `GameBalance.sellMultiplier`), drag/drop, movement lock
  while open, auto-close on walk-away
- Shop system: `ShopData`/`ShopNPC`/`ShopUI`, relationship-based discount (0.2%/point, max 20%),
  limited stock persisted via `ISaveable`
- 30+ items: produce, seeds, tools, food

### Animal Husbandry
- 4 animals: Chicken (Spring), Duck (Summer), Sparrow (Fall), Rabbit (Winter)
- Care: petting (+5 happiness, heart particle, 5s cooldown), feeding (+3 happiness, via hand or
  FeedingTrough), daily decay if neglected (-0.5/no pet, -1.0/no feed, floor 20)
- Happiness → combat multiplier: `0.5x` at 0 happiness → `1.5x` at 100 (formula in
  `Animal.cs:675-680`, bounds configurable via `GameBalance`)
- Illness: 3-day neglect threshold → ill, blocks production, -50% combat stats, cured by
  "Medicine" item
- Production: daily, +50% bonus if petted AND fed same day
- XP/leveling: level × 100 XP per level, max level 10, +5% growth-stat boost per level (cap 3x)
- Seasonal stat bonuses in preferred season
- `AnimalRosterUI`/`AnimalInfoUI`: inspection + rename (max 20 chars)
- `FeedingTrough`: stores food in `InventoryContainer`, auto-feeds registered animals on
  `OnDayChanged`

### Dialogue, Quests, Relationships
- `DialogueTree`/`DialogueNode`/`DialogueChoice`: branching, conditions, effects, typewriter,
  portraits
- `ConversationMemory` (singleton, `ISaveable`, 444 lines): relationship levels (-100..100),
  quest statuses, custom variables, conversation completion — auto-saves every 30s
- `DialogueCondition.ConditionType` includes `RelationshipLevel`, `QuestStatus`, `InventoryItem`,
  `VariableCheck` — all wired to real game state
- `DialogueEffect.EffectType` includes `ModifyRelationship`, `GiveItem`/`TakeItem` (real
  `Inventory` via `ItemDatabase`), `SetQuestStatus`
- `QuestManager` (singleton, `ISaveable`, 369 lines): objectives (CollectItem/TalkToNPC/
  HarvestCrop/CompleteBattle/Custom), rewards (gold/items/relationship), `NotifyObjective()`
  auto-advances from dialogue/farming/combat hooks
- `QuestTrackerUI`: corner HUD showing active quest + current objective

### Farm Buildings & Weather
- `FarmBuildingManager` (`ISaveable` singleton): Barn doubles `AnimalZone` capacity (5→10),
  Greenhouse bypasses seasonal planting restriction
- `BuildingShopUI`: loads `Resources/Buildings/` assets
- `WeatherController`: Rain (auto-waters tilled/watered soil), Drought (accelerates wilting),
  once per day on `OnDayChanged`

### World Map
- `WorldMapBiomePanel`: expandable per-biome panel with stage buttons + lock icon
- `BiomeUnlockChecker`: boss-completion → biome unlock
- `WorldMapUiController`: syncs save → `StageManager`, refreshes panels on open
- `StageButton`: lock/unlock from `worldFlags`, routes to `TeamAssemblerUI`
- `WorldMapTriggerZone`: E-key entry point from farm scene
- 25 stage ScriptableObjects across 5 biomes, 39 enemy ScriptableObjects

### Combat
- 9×5 grid: **columns 0-5 = enemy side, columns 6-8 = player side** (confirmed in
  `GridManager.cs:73-126` — this is the ground truth; ignore any doc describing a row-based
  enemy/player split, that was always aspirational/incorrect)
- Speed-based ATB turn gauge, allocation-free `Update()` loop (`TurnManager.cs:133-148`),
  `gaugeFilLRate=10f`, overflow-preserving gauge reset
- Skill execution: cooldowns, accuracy, damage multiplier, self-heal, player skill from
  `AnimalData.activeSkill`, enemy skills from `EnemyData.skills` + `skillUseChance`
- Status effects: Stun (skip turn), Shield (0-90% damage reduction, capped), Burn (DoT) —
  **well-tested**, 29 dedicated tests in `CombatPhase1Tests.cs` covering edge cases (refresh vs.
  stack, shield cap, case-insensitive immunity matching)
- Status immunities via `EnemyData.immunities` (string list)
- Rewards: gold → `PlayerStats`, loot → `Inventory`, happiness → animals, XP/level-up to
  survivors; stage completion marked in `worldFlags["stage_completed_{stageName}"]`
- Combat animations: Attack/Hurt/Die triggers via `AnimalData.animatorController`

### Audio & Tutorial
- `GameMusicManager`: seasonal farm tracks, combat/menu music, crossfade on season change
- `SFXManager`: pooled AudioSources (5 slots, round-robin)
- `TutorialManager` (`ISaveable` singleton): 6 steps (till → plant → water → pet → sleep →
  harvest), persists progress, skip button

---

## Combat Pipeline — Resolved

`COMBAT_PIPELINE_STATUS.md` previously documented a bug where `SpawnTeams()` never fired via the
TeamAssembler flow, caused by 24 MonoBehaviour components in `CombatScene.unity` with empty
`m_EditorClassIdentifier` entries (Unity serialization issue, not a code bug).

**Status: fixed.** A static check of the current `CombatScene.unity`
(`grep -c "m_EditorClassIdentifier: $"`) returns **0** — no empty ECI entries remain. This lines
up with 4 commits that postdate the original bug report and specifically address related issues
(timeScale=0 blocking Invoke, enemy sprite rendering, turn speed/health bar scale, and "save
CombatScene/SampleScene with combat pipeline setup"). A fresh read of
`CombatTeamSpawner`/`EnemySpawner`/`TurnManager`/`GridManager` shows a complete, coherent
Invoke-chain (0.5s spawn players → 0.6s spawn enemies → 1.0s init combat) with no missing-script
issues.

**One remaining recommended action** (tracked as `/review/03_WORKLIST.md` TASK-001): a manual
play-through smoke test (farm → trigger zone → team assembler → start battle → win/lose →
return to farm) to get a second confirmation signal beyond static YAML inspection. This is the
only piece of `COMBAT_SETUP_GUIDE.md`'s troubleshooting content still potentially relevant — if
the smoke test passes, the rest of that guide's manual prefab/scene-setup steps describe work
that is now baked into the saved scenes and can be considered historical.

---

## Combat Scope — Resolved (Option A, descope)

`PRD_Animals_Combat_System.md` (v2.0, 2025-10-21) describes an elaborate "3-Passive System"
(Family passives, Class passives, Happiness passives, plus team-wide "Combo Synergies") as the
core combat mechanic. **This is not implemented.** `AnimalData.combatClass` and
`AnimalData.availablePassiveSkills[]` are populated ScriptableObject fields with **zero readers**
anywhere in `Assets/Scripts/Combat/` — `CombatUnit`/`TurnManager` compute stats purely from
`AnimalCombatStats` with no synergy/family/class logic.

The combat system that IS shipped (status effects + XP/leveling, happiness→stat multiplier, see
above) is solid and well-tested, but it is a **simpler system than the PRD describes**. This gap
was tracked as `/review/03_WORKLIST.md` TASK-002, with two options:

- **Option A — Descope**: mark the PRD's 3-passive system as historical/aspirational, annotate
  `combatClass`/`availablePassiveSkills` as currently-unused fields.
- **Option B — Minimal implementation**: implement just the Class-passive synergy (3+ units of
  the same `combatClass` grant a small stat bonus), using the field that already exists on every
  `AnimalData` asset. Family passives and Combo Synergies stay descoped.

**Decision: Option A.** `AnimalData.combatClass` and `AnimalData.availablePassiveSkills[]` are
annotated in code as currently-unused (PRD-descoped) fields. The shipped status-effect +
happiness-multiplier combat system is the source of truth; the PRD's 3-passive content should be
treated as historical/aspirational and not representative of the current game. TASK-003
(conditional Class-passive synergy) is marked N/A — descoped per this decision.

---

## Known Tech Debt

See `/review/02_FINDINGS.md` for the full diagnostic with file:line citations, and
`/review/03_WORKLIST.md` for atomic, ready-to-execute tasks. Headlines:

- **God classes grew, didn't shrink**: `MainMenuUI.cs` 862→1019 lines, `SellBox.cs` 1109→1123,
  `Inventory.cs` ~1150, `InventorySlot.cs` 907, `Animal.cs` 975 (new entrant — 2nd largest script
  in the project). (TASK-011, TASK-012)
- **UIManager has two coexisting window systems** — legacy `OpenPanel`/`ClosePanel` (still
  called by `SellBox.cs:429,509`) alongside the documented `TryOpenWindow`/`TryCloseWindow` stack
  used by 10 `IUIWindow` implementers. Investigated (TASK-006): ESC handling only uses the
  window stack (safe), but `IsAnyPanelOpen()` (legacy) is read by `PlayerMove.cs:169` and
  `UIInput.cs:90`. Removal needs a manual Editor check first — see `/review/PROGRESS.md`.

**Recently resolved**: save migration dispatch scaffolding added to `SaveManager.MigrateSave()`
(TASK-004); `QuestManager.GrantRewards()` now caches `PlayerStats`/`Inventory` and logs a warning
on miss instead of failing silently (TASK-005); dead `SellBox` branch removed from
`InteractionManager.cs` (TASK-009); CLAUDE.md folder table corrected for farming scripts
(TASK-010); test asmdefs verified (TASK-013); `DialogueCondition`/`DialogueEffect` now have
19 unit tests total (TASK-007, TASK-008).

**What's confirmed working well**: namespace convention (100%), combat pipeline structure,
status-effect tests, GameBalance centralization (~80%), farming/weather/save scaffolding,
animal husbandry (106 tests).

---

## Unity Editor Wiring Checklist

Carried forward from ROADMAP.md's "Known Gaps" — these are manual Editor-setup items independent
of the code review above. Re-verify each against the current scenes before assuming still
outstanding (the combat pipeline fixes may have completed some of these as a side effect of
"save CombatScene/SampleScene with combat pipeline setup").

**Combat:**
- Assign `AnimalSkill` ScriptableObjects to `AnimalData.activeSkill` / `EnemyData.skills`
- Assign `AnimatorController` to `AnimalData.animatorController` per animal

**World Map:**
- Create WorldMap Canvas + `WorldMapUIController` in farm scene (if not already present)
- Create one `WorldMapBiomePanel` per biome, wire to controller's `biomePanels` list
- Assign `WorldMap` reference on `WorldMapTriggerZone`

**Animals & Farm:**
- Assign sprites to `Rabbit.asset` + `RabbitFur.asset` (via `Tools > Sowur Shield > Create Animal
  Assets` if not yet run)
- Create `RabbitFur_GroundItem`, `DuckEgg_GroundItem`, `Feather_GroundItem` prefabs in
  `Resources/Prefabs/GroundItems/`
- Wire `AnimalInfoUI` rename panel UI elements

**Buildings / Shop / Tutorial:**
- Create `Resources/Buildings/Barn.asset` + `Greenhouse.asset` (FarmBuildingData) if missing
- Building row prefab wired to `BuildingShopUI`
- `ShopItemRow` prefab wired to `ShopUI.shopItemRowPrefab`
- `Resources/Quests/` folder populated with `QuestData` assets

**Audio:**
- Assign `seasonalFarmTracks[4]`, `combatMusic`, `menuMusic` on `GameMusicManager`
- SFX clips for `CombatHit`, `CombatDeath`, `PetAnimal`
- `GameMusicManager.Instance.OnEnterCombat()` wired from `SceneTransitionManager`

---

## WebGL Demo Deployment

Unchanged — see CLAUDE.md "WebGL Demo Deployment (GitHub Pages)" section for the full
GitHub Actions / Brotli-decompression / CSS-preservation workflow. Live demo:
https://joaofranciscopanta.github.io/sowur-shield/
