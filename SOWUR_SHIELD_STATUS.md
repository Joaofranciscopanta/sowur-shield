# Sowur Shield — Project Status

> Last updated: 2026-07-11
> Branch: `main`
> This is the **single source of truth for project state**. It supersedes ROADMAP.md,
> GAME_DEVELOPMENT_PLAN.md, COMBAT_PIPELINE_STATUS.md, DEVELOPMENT_LOG.md, COMBAT_SETUP_GUIDE.md
> (all deleted; content folded in here).

## Documentation Map

| Document | What it's for |
|---|---|
| **SOWUR_SHIELD_STATUS.md** (this file) | What's done, how it works, what's in progress, what's next |
| [KNOWN_BUGS.md](KNOWN_BUGS.md) | Open bugs and known quirks, with repro notes and where to look |
| [CLAUDE.md](CLAUDE.md) | Dev conventions, Unity setup requirements, bug-fix history, namespace rules |
| [review/01_ARCHITECTURE.md](review/01_ARCHITECTURE.md) | Deep architecture map (code-review ground truth, 2026-06-12) |
| [review/02_FINDINGS.md](review/02_FINDINGS.md) | Code-quality findings with file:line citations |
| [review/03_WORKLIST.md](review/03_WORKLIST.md) | Atomic task backlog (2 tasks remaining + follow-ups) |
| [review/PROGRESS.md](review/PROGRESS.md) | Log of completed review tasks |
| [UI_ART_PLACEHOLDERS.md](UI_ART_PLACEHOLDERS.md) | UI elements still needing custom art + generation prompts |
| [AI_Sprite_Prompts.md](AI_Sprite_Prompts.md) | Sprite-generation prompts matching the project art style |

---

## TL;DR Dashboard

| System | Status | Confidence |
|---|---|---|
| Farming (crops, soil, weather) | ✅ Complete | Well-tested |
| Economy (inventory, shops, selling) | ✅ Complete | Well-tested |
| Animal husbandry | ✅ Complete | 106 tests |
| Dialogue / Quests / Relationships | ✅ Complete | 45+ tests |
| Combat loop | ✅ Complete | **Pipeline confirmed end-to-end in-editor 2026-07-01** |
| Save/Load (multi-slot) | ✅ Complete | Dictionary-persistence fix landed Jun/26 |
| Localization EN/PT/ES | ✅ Complete | ~226 strings, WebGL-safe |
| Mobile touch + gamepad input | ✅ Complete | Manual device test recommended |
| Achievements | ✅ Complete | 8 achievements |
| Audio / Tutorial | ✅ Complete | Needs Editor asset wiring |
| WebGL demo | ✅ Live (Build #15) | https://joaofranciscopanta.github.io/sowur-shield/ |
| **UI polish pass** | 🔨 In progress | HUD/inventory done; combat consumable UI needs visual check |
| World/village map expansion | 💤 Deferred | Design decided (see Deferred section) |

**Project size**: 4 scenes, ~155 scripts (100% namespace-compliant `SowurShield.<System>`),
413+ test methods in 16+ test files, 3 asmdefs + tests.

---

## What's Complete (and how it works)

### Core Gameplay
- Player movement (WASD, dash, animations), new Input System (`PlayerControls.inputactions`)
- Interaction: E-key proximity (`InteractionManager`) + left-click sprite raycast
  (`CursorController`), priority: objects > tools. Cursor colors: green=object, yellow=tool
- Time & seasons: 28-day year (4×7-day seasons), `OnDayChanged`/`OnSeasonChanged` events
- Save/Load: multi-slot (AutoSave + Slot1-3) → `Saves/<Slot>/GameSave.json`, `ISaveable`
  registry, legacy flat-save migration, version-dispatch scaffolding in `MigrateSave()`.
  **Critical fix (Jun/26)**: `Dictionary<,>` fields were never persisted by `JsonUtility` —
  fixed in `SaveManager`/`GameData`

### Farming
- Soil state machine: Regular → Tilled → Watered → WithCrop (`SoilBlockInteractable`)
- Crop growth: multi-stage, per-crop `CropData` SO, water requirement with drought-death,
  regrowth, seasonal restrictions (Greenhouse bypasses)
- Crops: Carrot, Cabbage, Tomato (Summer), Winter Wheat (Winter)
- `WeatherController`: Rain auto-waters, Drought accelerates wilting, rolls on `OnDayChanged`
- DualGridTilemap: 16-tile rule-based rendering

### Economy
- Inventory: 36 slots (9 hotbar + 27 storage), drag/drop, stacking, tooltips.
  Storage grid opens over a themed wood window panel (`storagePanelBackground`, Jul/2)
- SellBox: auto-sell on sleep (80% via `GameBalance.sellMultiplier`), movement lock while open
- Shops: `ShopData`/`ShopNPC`/`ShopUI` with relationship discount (0.2%/pt, max 20%), limited
  stock persisted; `AnimalMarketUI` buy/sell animals; `BuildingShopUI` for farm buildings
- 30+ items via `ItemDatabase` (name-keyed lookup — names must match exactly)

### Animal Husbandry
- 4 animals: Chicken (Spring), Duck (Summer), Sparrow (Fall), Rabbit (Winter)
- Care loop: petting (+5 happiness, heart particle), feeding (+3, hand or `FeedingTrough`),
  daily decay if neglected (floor 20); illness after 3 neglect days (blocks production,
  -50% combat stats, cured by Medicine item)
- Production: daily, +50% bonus if petted AND fed; XP/leveling (level×100 XP, max 10,
  +5% growth stats/level); seasonal stat bonuses in preferred season
- Happiness → combat multiplier 0.5x–1.5x (`Animal.cs`, bounds via `GameBalance`)
- Market-purchased animals normalize sprite scale via `SpriteScaleUtility` (Jul/1) so they
  match hand-placed ones

### Dialogue, Quests, Relationships
- `DialogueTree`/`DialogueNode`/`DialogueChoice`: branching, conditions, effects, typewriter,
  portraits; all 6 ConditionType/EffectType variants wired to real game systems
- `ConversationMemory` (singleton, `ISaveable`): relationships (-100..100), quest statuses,
  custom variables — auto-saves every 30s
- `QuestManager` (singleton, `ISaveable`): objectives (CollectItem/TalkToNPC/HarvestCrop/
  CompleteBattle/Custom), rewards, `NotifyObjective()` auto-advance hooks live in
  Inventory/Conversation/Crop/Stage code. Quest assets in `Resources/Quests/`
- `QuestTrackerUI` corner HUD; `QuestsCanvas` full journal

### Combat (auto-battler)
- 9×5 grid: **columns 0-5 = enemy side, 6-8 = player side** (`GridManager`)
- Speed-based ATB turn gauge (`TurnManager`), skills with cooldowns/accuracy, status effects
  (Stun/Shield/Burn/Poison/Weakness — well-tested), immunities, crits (5%/×1.5), combo counter,
  melee/ranged positional targeting, in-battle consumables, battle modifiers (DoubleSpeed/
  LowVisibility/HealingRain/GlassCannon), behavior-aware enemy AI (Aggressive/Defensive/Support)
- Class synergy: 3+ units sharing `combatClass` get +10% buffs; passive skill unlocks via
  `AnimalSkill.CanUnlock` (Season-type conditions still N/A — no season singleton)
- Self-spawning VFX/UI: `HitStopController`, `CombatUnitVFX` (status icons + damage numbers),
  `TelegraphHighlighter`, `BattleHudOverlay` (modifier banner + combo, outlined text Jul/2),
  `ConsumableBattleUI` (restyled Jul/2 — gold button + wood panel)
- Rewards: gold, loot, happiness, XP; stage completion → `worldFlags`
- **Pipeline confirmed 2026-07-01** via full in-editor play-through: TeamAssembler → battle →
  spawns on correct sides → turn loop → results → return to farm, zero exceptions.
  Diagnostic log spam from the old investigation was removed the same day
- Unit sprite sizing centralized in `SpriteScaleUtility` (`SowurShield.Core`)

### World Map & Stages
- 5 biomes, 25 stages, 39 enemy SOs; `WorldMapBiomePanel` + `BiomeUnlockChecker`
  (boss-completion unlocks), `StageButton` → `TeamAssemblerUI`, `WorldMapTriggerZone` (E-key)
- `StageManager` (static) persists completion via save data; stage backgrounds render in combat

### Localization (EN / PT / ES)
- Full Unity Localization infra: 3 locales, 12 string-table collections, ~226 entries covering
  dialogue, items, animals, buildings, crops, enemies, stages, quests, UI
- Editor tooling: `Tools > Sowur Shield > Setup Localization (Full)` (idempotent),
  `Import Localization CSV`, `Auto-Wire Localized Fields` (reads `field_map.json`)
- WebGL-safe: `SafeGetLocalizedString()` returns empty until tables preload (avoids
  `WaitForCompletion` deadlock — tables preload during MainMenu). Demo has language switcher

### Mobile & Gamepad Input
- Virtual joystick + action button (touch, Safe-Area aware) via `MobileControlsManager`
  (DontDestroyOnLoad, lives in MainMenu scene)
- Xbox/PS5 gamepad: movement, interact (A/Cross), `GamepadVirtualCursor` reticle for tools
- Builder: `Tools > Sowur Shield > Rebuild Mobile Controls UI`

### Achievements
- `AchievementData`/`AchievementManager` + Steam-style toast; 8 achievements driven by global
  static events (stage completion, crop harvest, item sales)

### Audio & Tutorial
- `GameMusicManager`: seasonal farm tracks, combat/menu music, crossfades
- `SFXManager`: pooled AudioSources; wired to CombatHit/CombatDeath/PetAnimal
- `TutorialManager` (`ISaveable`): 6 steps, auto-starts on new game, skippable

### UI Theme
- `UITheme` SO (`Resources/UI/CozyUITheme.asset`): wood/cream/gold palette + spacing/typography
  tokens, consumed across combat and farm UI
- Shared sprite kit in `Assets/Resources/Sprites/UI/` (panels, buttons, bars, slots, frames) —
  runtime-loadable via `Resources.Load` for self-spawning UI
- All four popup canvases (TeamAssembler, BuildingShop, Quests, AnimalMarket) standardized to
  1920×1080 reference resolution matching the HUD (Jul/1)

---

## In Progress — UI Polish Pass (Jul/1–2)

**Done & committed** (`ec9d046`, `e03d748`):
- Main HUD: decorative frame on minimap, wood panels behind stamina bar and money/time/day,
  wood-tinted stamina backing
- Inventory: wood window panel behind storage grid, visibility tied to `ToggleInventory()`
- Combat: `ConsumableBattleUI` Items button (gold sprite) + list panel (wood), outlined
  floating HUD text

**Done (Jul/11, uncommitted — needs visual check in editor)**:
- New `UIThemeStyler` static helper (`Scripts/UI Systems/`, `SowurShield.UI`): runtime restyle
  of scene-wired UI with the sprite kit (sliced wood panels, primary/danger/small-action
  buttons, dark labels on gold art) + flat-tint fallback — same pattern as ConsumableBattleUI
- Themed via `ApplyTheme()` in Awake/Start: `ShopUI`, `BuildingShopUI`, `GameMenuUI` (all five
  panels + notification colors mapped to theme tokens; re-applied in `TransferReferencesFrom`),
  `QuestsUI` (panel + close button only — tabs stay on ShowTab's Button.colors tint),
  `BattleResultsUI` (uses the previously-unused `panel_victory`/`panel_defeat` sprites)
- `AnimalMarketUI` skipped intentionally — its builder already applies the theme at build time

**Remaining**:
- [ ] Visual verification of the combat Items button/panel in a live battle (code compiled
  clean; not yet seen on screen — needs editor-focused play session)
- [ ] Visual verification of the Jul/11 runtime theming above (shops, pause menu, quests,
  victory/defeat) in an editor play session
- [ ] Stamina bar has no icon (no energy icon exists in the sprite kit yet)

---

## Backlog

**From the code review** ([review/03_WORKLIST.md](review/03_WORKLIST.md)):
- TASK-011 — Extract `AnimalIllness` from `Animal.cs` (975 lines, 2nd-largest script)
- TASK-012 — Extract save-slot picker from `MainMenuUI.cs` (1019 lines, largest)
- Follow-up — Remove legacy `OpenPanel`/`ClosePanel` system from `UIManager` (needs one manual
  Editor check first, documented in PROGRESS.md)

**Art gaps**:
- Duck and Sparrow use chicken-baby placeholder sprites (`Assets/Resources/Animals/duck.asset`,
  `Sparrow.asset` point at `Chicken_Baby*.png`)
- Stamina/energy icon; see [UI_ART_PLACEHOLDERS.md](UI_ART_PLACEHOLDERS.md) for the full list
- AnimatorControllers with Crit/Poison/Weakness/Hurt/Attack/Die states don't exist yet for any
  animal/enemy — combat animation triggers are currently no-ops

**Feature ideas discussed (not started)**:
- Cooking/crafting; NPC romance/marriage; full 3-Passive combat system (PRD); farm land
  expansion; 6th biome + boss content

### Deferred: Village map + enterable houses (design decided 2026-07-01)
- Exterior village: extend `SampleScene`'s existing `DualGridTilemap` with new tile types —
  do NOT revive `Scripts/MapEditor/` (`RuntimeMapEditor`/`BrushTool`/`ExtendedDualGridTilemap`
  is an unwired skeleton: every scene reference is null, `MapEditorUI` script isn't even
  attached; finishing it ≈ building from scratch)
- Interiors: separate scenes per house + a new lightweight `HouseEntrance` trigger component,
  mirroring the proven CombatScene transition pattern (persist state, restore exit position)

---

## Unity Editor Wiring Checklist

Manual Editor-setup items still outstanding (verify against current scenes before working —
some may have been completed as side effects):

**Combat**: assign `AnimalSkill` SOs to `AnimalData.activeSkill`/`EnemyData.skills`; create and
assign AnimatorControllers per animal/enemy
**World Map**: WorldMap Canvas + `WorldMapUIController` in farm scene; one `WorldMapBiomePanel`
per biome wired to controller; `WorldMap` ref on `WorldMapTriggerZone`
**Animals**: sprites for `Rabbit.asset`/`RabbitFur.asset`; GroundItem prefabs (RabbitFur,
DuckEgg, Feather) in `Resources/Prefabs/GroundItems/`; `AnimalInfoUI` rename panel wiring
**Buildings/Shop/Tutorial**: `Resources/Buildings/Barn.asset`+`Greenhouse.asset` if missing;
row prefabs for `BuildingShopUI`/`ShopUI`; `Resources/Quests/` populated
**Audio**: `seasonalFarmTracks[4]`/`combatMusic`/`menuMusic` on `GameMusicManager`; SFX clips;
`OnEnterCombat()` wired from `SceneTransitionManager`
**Combat consumables known limitation**: player `Inventory` only exists in SampleScene, so
`ConsumableBattleUI.RefreshList()` shows "no inventory" during battle — fix requires persisting
Inventory into CombatScene or reading from `SaveManager`/`GameData` (deferred)

---

## Known Tech Debt

See [review/02_FINDINGS.md](review/02_FINDINGS.md) for the full diagnostic. Headlines:
- God classes: `MainMenuUI` 1019, `SellBox` 1123, `Inventory` ~1150, `InventorySlot` 907,
  `Animal` 975 lines (TASK-011/012 target the worst two)
- `UIManager` still has two coexisting window systems (legacy `OpenPanel` + `IUIWindow` stack)
- `SellBox` re-loads `GameBalance` via `Resources.Load` on every `sellMultiplier` access

**Working well**: namespace convention (100%), combat pipeline, status-effect tests,
GameBalance centralization (~80%), save scaffolding, animal husbandry tests.

---

## Dev-Environment Notes

- Unity **6000.3.3f1**; scenes: MainMenu (0), SampleScene (1), CombatScene (2), MapEditorScene
  (not in build)
- MCP editor automation via **CoplayDev unity-mcp** (`com.coplaydev.unity-mcp`, local HTTP
  server managed from `Window > MCP for Unity`) — connection quirks and workarounds are
  documented in the assistant's project memory, not here
- WebGL demo deploys via GitHub Actions (weekly + manual); see CLAUDE.md "WebGL Demo
  Deployment" for Brotli/CSS specifics. Live: https://joaofranciscopanta.github.io/sowur-shield/
