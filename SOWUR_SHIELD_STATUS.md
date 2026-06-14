# Sowur Shield — Project Status

> Last updated: 2026-06-14
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

## Combat Scope — Implemented (minimal synergy)

`PRD_Animals_Combat_System.md` (v2.0, 2025-10-21) describes an elaborate "3-Passive System"
(Family passives, Class passives, Happiness passives, plus team-wide "Combo Synergies") as the
core combat mechanic. The full system is still not implemented, but as of the Combat Phase 2
initiative (see below), `AnimalData.combatClass` and `AnimalData.availablePassiveSkills[]` are no
longer dead fields:

- **Class synergy** (`TurnManager.ApplyClassSynergies`, called from `StartCombat`): player units
  sharing a `combatClass` with 2+ teammates (3+ total) each get a permanent +10% attack/defense
  buff via `CombatUnit.ApplyStatBuff`.
- **Passive skill unlocks** (`CombatTeamSpawner.ApplyUnlockedPassiveSkills`, called per unit at
  spawn): each `AnimalSkill` in `availablePassiveSkills` with `skillType == Passive` is checked
  via `AnimalSkill.CanUnlock` (existing `SkillUnlockCondition` logic — CombatClass, AnimalFamily,
  FamilyCount via `AnimalRoster.GetFamilyCount`, HappinessThreshold, Combined). If unlocked and
  its `attackMultiplier`/`defenseMultiplier`/`speedMultiplier` differ from `1f`, a permanent
  `ApplyStatBuff` is applied.
- **Season-based unlock conditions are still N/A** — no season-singleton accessor exists yet, so
  `currentSeason` is passed as `""` and `Season`-type `SkillUnlockCondition`s never trigger. Family
  passives beyond `FamilyCount` and the PRD's "Combo Synergies" (distinct from the Phase 2 combo
  counter below) remain descoped.

This was previously tracked as `/review/03_WORKLIST.md` TASK-002/TASK-003 (Option A descope,
Option B minimal implementation). **Option B has now shipped** — TASK-003 is resolved, not N/A.

---

## Combat Phase 2 — Innovation Pass (AI, Strategy, Mechanics, Event Hooks)

A 23-step CD initiative (small change → EditMode tests → commit, repeated) extended combat with
new status effects, smarter enemy AI, new player-facing mechanics, and event hooks for future
VFX/animation work. All logic is pure C# on `CombatUnit`/`TurnManager`/`AnimalSkill`/`EnemyData`/
`CombatTeamSpawner`, covered by new EditMode test files under `Assets/Tests/EditMode/`
(`CombatPhase2StatusTests.cs`, `CombatPhase2AccuracyTests.cs`, `CombatPhase3CritTests.cs`,
`CombatPhase3ComboTests.cs`, `CombatPhase3ConsumableTests.cs`, `CombatPhase3BattleModifierTests.cs`,
`CombatPhase4SynergyTests.cs`, `CombatPhase4PassiveSkillTests.cs`, `CombatPhase5TelegraphTests.cs`,
`CombatPhase6BigHitTests.cs`, `CombatPhase6StatusEventTests.cs`, `CombatPhase6DamageHealEventTests.cs`,
`CombatPhase7CritEventIntegrationTests.cs`).

- **New status effects**: `Poison` (stacks independently, ticks alongside Burn) and `Weakness`
  (refreshes like Burn/Shield/Stun, reduces `GetAttack()`/`GetDefense()` via
  `GetWeaknessReduction()`, capped at 75%). Both wired into `TurnManager.ExecuteSkill` via
  `AnimalSkillEffect.Poison`/`Weakness`, respecting `IsImmuneTo`.
- **Temporary stat buffs**: `CombatUnit.ApplyStatBuff(atkMult, defMult, spdMult, duration)` —
  buffs stack multiplicatively and expire independently via `TickStatusEffects()`. Activates the
  previously-dormant `AnimalSkill.attackMultiplier`/`defenseMultiplier`/`speedMultiplier` fields.
  `GetAttack()`/`GetDefense()`/`GetSpeed()` apply Weakness reduction × buff product; turn-gauge
  fill now uses `GetSpeed()`.
- **Smarter AI** (`EnemyData.aiBehavior`, previously unused — now wired at spawn via
  `EnemySpawner`): `SelectTarget()` branches per-behavior for non-player attackers —
  `"Defensive"` targets the highest-`GetAttack()` player unit, `"Support"` targets the
  lowest-`GetDefense()` player unit, `"Aggressive"`/`"Random"` keep the existing lethal-first →
  front-column logic (regression-safe). `SelectSkillTarget()` is similarly behavior-aware:
  Aggressive prefers offensive (Burn/Poison/Weakness) skills on the highest-HP enemy; Support
  prefers Shield/heal skills on the lowest-HP% ally.
- **Difficulty-scaled accuracy/skill-use**: `EnemyData.GetScaledAccuracy(difficultyLevel)` and
  `GetScaledSkillUseChance(difficultyLevel)` mirror the existing `GetScaledStats` `Mathf.Pow`
  pattern (capped at 0.95 / 0.6). `CombatUnit.InitializeAsEnemy` gained an optional `acc`
  parameter (default `1.0f`, all existing call sites unaffected).
- **Critical hits**: `CombatUnit.GetCritChance()` (5% base), `CombatUnit.ApplyCrit(damage, isCrit)`
  (×1.5 multiplier), rolled in both basic-attack and skill-damage resolution in `TurnManager`.
- **Combo counter**: `TurnManager` tracks consecutive player hits on the same target
  (`GetComboCount()`, cap 5), granting up to +20% damage (+4%/stack above 1). Any enemy action or
  target switch resets the combo.
- **Melee/Ranged positional targeting**: new `AttackRange` enum (`Melee`/`Ranged`, default
  `Ranged` preserves prior behavior) on `AnimalData`/`EnemyData` → `CombatUnit.GetAttackRange()`.
  Melee attackers in `SelectTarget()` restrict to the front column, falling back to the back
  column only if the front column has no living targets.
- **In-battle consumables**: `TurnManager.UseConsumableOnUnit(Item, CombatUnit)` — heals the
  target via `item.healthRestore`, removes one unit from the player `Inventory`, free action (no
  gauge cost). Rejects non-consumables, dead/null targets.
- **Battle modifiers** (`BattleModifier.cs`): `BattleModifierType` enum (`None`/`DoubleSpeed`/
  `LowVisibility`/`HealingRain`/`GlassCannon`), rolled once per battle in `StartCombat`
  (60% `None`, 10% each other). `DoubleSpeed` doubles turn-gauge fill; `LowVisibility` subtracts a
  flat accuracy penalty via `GetEffectiveAccuracy()`; `HealingRain` heals all living units at the
  start of each round (`currentTurn % allUnits.Count == 1`); `GlassCannon` doubles both dealt and
  received damage. `SetBattleModifierForTesting()`/`GetActiveModifier()` for deterministic tests.
- **Event hooks for future VFX/animation** (no behavior change, pure data plumbing):
  - `TurnManager.OnTelegraph` (instance event, `TelegraphInfo { actor, target, skill }`) fires
    immediately before an attack/skill resolves (`skill = null` for basic attacks).
  - `TurnManager.OnBigHit` (static event, `Action<float>`) fires with a hit-stop duration —
    `0.08f` on crit, `0.05f` if damage ≥ 25% of target max HP, otherwise not fired.
  - `CombatUnit.OnStatusApplied`/`OnStatusExpired` (`Action<StatusEffectType>`) fire on
    application and expiry via `ApplyStatusEffect`/`TickStatusEffects`.
  - `CombatUnit.OnDamageTaken` (`Action<float, bool>`, amount + isCrit) and `OnHealed`
    (`Action<float>`) fire from `TakeDamage`/`TakeDamageWithShield`/`Heal`. The crit flag is
    threaded end-to-end: `TurnManager`'s crit roll → `TakeDamageWithShield(damage, isCrit)` →
    `OnDamageTaken(amount, true)` + correlated `OnBigHit(0.08f)`.
  - All of the above are now consumed by self-spawning VFX/UI controllers — see "Combat Phase 2
    Wiring — Completed" below.

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

**Combat Phase 2 Wiring — Completed** (all 12 items from the deferred checklist):
- New `AnimalSkill` assets under `Assets/Resources/AnimalSkills/`: `PeckOfWeakening` (Chicken,
  Weakness), `ToxicQuack` (Duck, Poison), `FeatherShield` (Sparrow, Shield), `FlockInstinct`
  (Chicken passive, FamilyCount unlock), `SupportersBlessing` (Sparrow passive, CombatClass
  unlock), `VenomousBite` (enemy Poison, spiders/frogs), `DrainingHowl` (enemy Weakness, wolves)
- `EnemyData.aiBehavior` set per enemy across all 33 enemy assets (Aggressive/Defensive/Support
  mix by role — tanks Defensive, casters/support Support, attackers Aggressive)
- `EnemyData.baseAccuracy` set per enemy (0.80–0.95 range, tougher/slower enemies less accurate)
- `AnimalData.attackRange`/`EnemyData.attackRange` set per unit (Melee/Ranged) across animals and
  all 33 enemy assets
- `AnimalData.availablePassiveSkills[]` populated for Chicken/Sparrow; `animalFamily` set
  (Chicken=Galliformes, Duck=Anatidae, Sparrow=Passeridae); Sparrow `combatClass` changed to
  `Support` so `SupportersBlessing` (CombatClass-gated) and class synergies have real data
- `HitStopController.cs` — self-spawning (`RuntimeInitializeOnLoadMethod`), subscribes to
  `TurnManager.OnBigHit`, applies brief `Time.timeScale` dip + camera shake on `Camera.main`
- `CombatUnitVFX.cs` — auto-attached to every `CombatUnit` (`SetupVFX()` in both
  `InitializeFromAnimal`/`InitializeAsEnemy`); procedural `TextMeshPro` status icons
  (PSN/WKN/BRN/SHD/STN) driven by `OnStatusApplied`/`OnStatusExpired`
- `CombatUnitVFX.cs` — same component also spawns floating damage/heal numbers (red/gold-crit/
  green-heal, rise + fade) driven by `OnDamageTaken`/`OnHealed`
- `TelegraphHighlighter.cs` — self-spawning, subscribes to `TurnManager.OnTelegraph`, spawns a
  temporary glow sprite behind the acting unit and target
- `BattleHudOverlay.cs` — self-spawning screen-space overlay showing the active
  `BattleModifier.description` banner and `Combo x{N}!` counter, polling `TurnManager.Instance`
- `ConsumableBattleUI.cs` — self-spawning "Items" button + list of consumables from `Inventory`;
  clicking one calls `TurnManager.UseConsumableOnUnit` on the most-injured living player unit.
  The button is now hidden outside of an active battle (`TurnManager.Instance == null`) — it
  previously persisted across all scenes via `DontDestroyOnLoad` with no visibility gating.
  **Known limitation**: the player's `Inventory` MonoBehaviour only exists in `SampleScene`
  (the main game scene), not in `CombatScene` — so even during battle, `RefreshList()` currently
  shows "No inventory found" because `FindFirstObjectByType<Inventory>()` returns null.
  Fixing this requires either persisting the `Inventory` GameObject into `CombatScene` or
  having `ConsumableBattleUI`/`TurnManager.UseConsumableOnUnit` read consumables from
  `SaveManager`/`GameData` instead of a live `Inventory` instance — deferred to a future session.
- `CombatUnit` animator triggers: `Crit` (fires alongside `Hurt` when `TakeDamage(_, isCrit: true)`),
  `Poison`/`Weakness` (fire via new `TriggerStatusAnimation(StatusEffectType)`, called from
  `CombatUnitVFX.HandleStatusApplied`) — all guarded by `unitAnimator != null`, no-op if no
  `AnimatorController` is assigned

All new VFX/UI controllers are self-spawning (`RuntimeInitializeOnLoadMethod` +
`DontDestroyOnLoad`) and built procedurally — no `CombatScene.unity`/prefab edits were required.
Still outstanding: actual `AnimatorController` assets with `Crit`/`Poison`/`Weakness`/`Hurt`/
`Attack`/`Die` states+transitions don't exist yet for any animal/enemy, so the new triggers are
currently no-ops in practice (see "Assign `AnimatorController`..." item above).

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
