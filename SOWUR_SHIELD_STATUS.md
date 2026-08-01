# Sowur Shield — Project Status

> Last updated: 2026-07-25 (tech-debt sweep; the Jul/12–19 animation & scene work is tracked in
> `ANIMACAO_STATUS.md` and not yet folded in here)
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
| [review/03_WORKLIST.md](review/03_WORKLIST.md) | Atomic task backlog (14/14 done Jul/25) |
| [review/04_CONTAINER_REFACTOR_PLAN.md](review/04_CONTAINER_REFACTOR_PLAN.md) | Inventory/SellBox/trough container architecture — **Etapas 0–5 done + verified Jul/26**; Etapa 6 (lojas) registrada como continuação |
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
| **UI polish pass** | 🔨 In progress | Combat results, quests, building shop + consumables verified Aug/1; `ShopUI` + settings widgets left |
| World/village map expansion | 💤 Deferred | Design decided (see Deferred section) |

**Project size**: 4 scenes, ~157 scripts (100% namespace-compliant `SowurShield.<System>`),
**786 tests across 44 files** — 752 EditMode (42 files) + 34 PlayMode (2 files); all passing as
of Aug/1. 3 asmdefs + 2 test asmdefs.

> The "430+ test methods" figure this line used to carry was an undercount, and the 34 PlayMode
> tests had **never executed once**: their asmdef was marked Editor-only, which excludes it from
> a PlayMode run, so the runner silently reported 0 total. Fixed Aug/1 (`f11008f`).

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
- Inventory: 45 slots (9 hotbar + 36 storage), drag/drop, stacking, tooltips.
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
  (`AnimalMarketUIBuilder` reads `CozyUITheme` and bakes flat palette tints into the scene
  objects; the UI class itself has no `UIThemeStyler` call and no theme field, so nothing is
  re-applied at runtime — sprite-kit art is absent here, unlike the panels above)

**Verified in Play Mode (Jul/26)** — pause menu + building shop. Three contrast bugs found and
fixed; every colour decision was measured (WCAG ratio), not eyeballed:
- **Pause menu title was invisible** — "Game Menu" kept the scene's old brown on the panel
  sprite's dark border: a **1.06** contrast ratio. New `UIThemeStyler.StylePanelTitle` recolours
  headings that have no serialized field, found by name so no Editor wiring is needed. Cream,
  not the gold used elsewhere: a heading can land on any wood tone, and gold only clears the
  bar on the darkest (3.01 on woodLight vs cream's 3.96/5.24/7.59 across the range).
- **Building shop rows ignored the theme** — a hardcoded 0.15 grey row with a flat green button
  on a cream panel. New `UIThemeStyler.StyleListRow`, applied at row instantiation since the
  prefab is Editor-owned. Row labels flip dark to match the new light background.
- **`BuildingRow`'s state colours were tuned for light-on-dark** — green/red/grey scored
  **1.88 / 3.01 / 3.11** against the new tan row, all under the 4.5 wanted for body text (the
  green "affordable" was nearly unreadable). Deepened to 4.62 / 6.12 / 4.46, same meanings.
  The buy button's tint was split off: it multiplies the gold sprite, so it stays a near-white
  dim rather than a deep hue that would just look dirty.

**`BattleResultsUI` verified in CombatScene (Jul/26)** — two more bugs, one of them not a
colour problem at all:
- **The victory title was hidden behind the battle HUD.** `BattleResultsCanvas` and
  `BattleStatusCanvas` both sat at sortingOrder 10, so which drew on top was arbitrary — and
  the HUD's `TurnOrderPanel` (y 936–996) covered the "Victory!" heading (y 930–1030) exactly.
  The results screen genuinely looked like it had no title. Now set from code to 150: above
  `BattleHudOverlay`/`ConsumableBattleUI` (100), below the achievement toast (200).
- **Both panels' text was tuned for a dark background they don't have.** `panel_victory` /
  `panel_defeat` are light art, but the title got gold and the body cream: **1.20** and
  **1.03** contrast — invisible. Body text and the victory heading are now dark (8.10 on the
  gold ribbon, 12.48 on the cream field).
- The defeat heading needed the **opposite** treatment: its ribbon is red, where nothing dark
  works (textDark only reaches 2.40). Cream, at 5.32. The ribbon already carries the "defeat"
  colour.

**Remaining**:
- [x] ~~Stamina bar has no icon~~ **done Aug/1** — drew `icon_stamina_bolt.png` procedurally
  (gold bolt, dark outline, theme palette, kit import settings). Fixing it exposed that the bar
  itself had no room for an icon (backing flush to the screen top, icon rendered off-screen at
  x −16) and that its fill was white-on-light, reading as empty even at full energy; both fixed,
  and the drag handle hidden since it is a read-only stat bar
- [ ] **`ShopUI` is blocked on content, not on verification.** Checked Aug/1: there is no
  `ShopUI` in any scene, no `ShopNPC` placed, **and zero `ShopData` assets anywhere** — and
  unlike `QuestsUI`, no builder exists to create one (`Tools > Sowur Shield` has builders for
  the animal market, building shop, quests and language UI, but not this). Nothing can be seen
  running until a `ShopData` exists, so this is a content task, not a UI-polish one

**Also done (Aug/1, same branch)**:
- **Settings panel themed** (`67ba609`) — sliders/dropdowns/toggle were stock Unity visuals.
  Panel grown 496×488 → 536×638 because the content genuinely did not fit (buttons landed on
  the bottom frame at every smaller size). Tracks `woodMid` (~3.6 on cream, over the 3.0
  non-text bar), fills gold, handles `woodDark`; labels lifted above their track instead of
  across it. **The heading needed a code fix, not a scene fix**: `ApplyTheme` called
  `StylePanelTitle(settingsPanel)`, which unconditionally forces cream — right for a heading on
  wood, wrong once this one moved onto the cream field (cream ~1.1 vs `textDark` ~12.1). It
  silently repainted the scene's colour on every `Awake`, so the panel looked fine until opened
  through the real `OpenMenu` flow. Now uses a `GetSettingsHeading()` lookup; `mainMenuPanel`
  and `saveInfoPanel` keep `StylePanelTitle` and their cream headings.
- **Audio wiring** (`11f93e6`) — see the Audio line in the wiring checklist below.

**Done (Aug/1, branch `feature/ui-layout-fixes-combat-quests-shop`, commit `82f4505`)** — five
items above closed, each verified in Play Mode with a screenshot rather than by reading code.
The common root cause: these layouts were authored when the panels were plain rects, and the
cozy sprite kit later gave them a painted frame roughly **90px** thick on every side. The
9-slice `border` field (32px) is *not* the usable inset — measure against the Viewport or the
render, not the sprite metadata.
- **`BattleResultsUI` buttons** — fixed, but *not* with the values this file previously
  suggested. Both panels are full-screen (1920×1080) with no smaller sprite child, so the
  proposed y −260 would have crowded the stats block (which spans y −210…+110). Used y −300,
  x ±170. The buttons also had to grow **160×30 → 300×70**: at the old size the "To Farm" label
  rendered clipped as "to Farm". Titles moved to y −148 so they sit on the ribbon instead of
  under the battle HUD.
- **Building shop title** — moved to y −165 (inside the cream field, not merely nudged within
  the border) and recoloured. `ApplyTheme` was tinting `playerGoldText` `highlightGold`, which
  reads **~1.4** on cream; both header labels are now `textDark` (**~12.1**). Two knock-on fixes
  the old note didn't anticipate: the list had to move down or it covered the relocated header,
  and its `VerticalLayoutGroup` lacked `childControlWidth`, so rows ignored `flexibleWidth` and
  stayed 600px wide inside a 1038px panel.
- **`QuestsUI`** — seen running for the first time. **Retracted: the claim that it is "absent
  from SampleScene" was false** — `QuestsCanvas` is in the scene and opens fine; no builder
  invocation was needed. Title, tabs and both tab panels all sat outside the frame, and the two
  tabs were ~400px apart. Fixed, and see the layout bug below.
- **Quest completed-row alpha** — 0.3 → 0.5 in both `QuestsUIBuilder` and the prefab on disk
  (the builder only runs on demand, so the prefab needed the same edit).
- **Combat Items panel** — verified populated, which required a battle with live enemies plus
  `Fish`/`RareFish` (the project's *only* two consumables — Carrot/Cabbage/Tomato are crops with
  `isConsumable=false`, so seeding those shows the empty-state row and looks like a bug).

**New bug found and fixed while verifying: 0-width rows from point anchors.** A fresh
`RectTransform` defaults to anchors `(0.5, 0.5)`, where `sizeDelta.x = 0` means a literal
0-wide rect — no `childControlWidth` on the parent can rescue it, because the layout group
writes `sizeDelta`, which stays 0. This bit two places independently:
`QuestObjectiveLine` (objective counters wrapped one character per line) and
`ConsumableBattleUI.CreateRow` (rows measured 0 → now 720px). Both now anchor `(0,1)-(1,1)`.
Worth grepping for `new Vector2(0,` on a `sizeDelta` next to a runtime-created RectTransform.

**Testing note:** entering Play Mode directly in SampleScene or CombatScene leaves every
localized label blank — the documented `SafeGetLocalizedString` quirk (tables preload during
MainMenu). Quest titles, building names and item rows all render empty this way. Confirm
against the HUD money/day text before treating a blank label as a UI bug.

**Theming audit (Jul/26)** — every claim in this section re-checked against the code, because the
list had accumulated two contradictory notes about `QuestsUI`:
- **Retracted: the "`QuestsUI` contains no `ApplyTheme` call at all" claim was false.**
  `Scripts/Dialogue/QuestsUI.cs:91-92` calls `StylePanel` + `StyleButton` in `Awake`, added
  Jul/19 in `c2f247e` — six days *before* the Jul/26 note that denied it. The Jul/11 note (panel
  + close button only, tabs left on `ShowTab`'s `Button.colors` tint) was the accurate one all
  along. The error looks like a grep for the *method name* `ApplyTheme`, which `QuestsUI` genuinely
  lacks because it inlines the calls in `Awake` instead of wrapping them — a reminder to grep for
  `UIThemeStyler` rather than the helper method when auditing this.
- Confirmed accurate: `ShopUI` (`ShopUI.cs:117-130`), `BuildingShopUI` (`:145-157`, rows `:202-211`),
  `GameMenuUI` (`:126-162`), `BattleResultsUI` (`:158-184`) all call `UIThemeStyler` as described.
- **Real gap found and fixed in `QuestsUI`.** `StylePanel` replaces the builder's cream background
  with the wood sprite, but the panel's own text was authored `textDark` to suit that cream —
  leaving the "Quests" heading and both empty-state labels at **1.68 / 2.44 / 3.23** on
  wood dark/mid/light: failing on all three, invisible on the darkest. Now cream via
  `StylePanelTitle` + `TintText` (**7.60 / 5.24 / 3.96**), cream over gold for the established
  reason — the wood tone under a panel sprite isn't fixed, and gold falls to 3.01 on woodLight.
  The quest rows were deliberately left alone: they carry their own white backing, so their dark
  text is already measured against the row, not the wood.

---

## Backlog

**From the code review** ([review/03_WORKLIST.md](review/03_WORKLIST.md)): **all 14 tasks done**
as of Jul/25 — TASK-011, TASK-012 and both outstanding follow-ups closed in one sweep. New
follow-up logged there — add a `.gitattributes` and renormalize line endings — **done Jul/26**
(see Known Tech Debt).

**Art gaps**:

> **AI image generation is unavailable in this project as configured** (checked Aug/1). The MCP
> `generate_image` tool exposes two providers, `fal` and `openrouter`, and both report
> `configured: false` — no API key in the editor's secure store, and no `MCPFORUNITY_FAL_API_KEY`
> / `MCPFORUNITY_OPENROUTER_API_KEY` in the environment. Add a key via
> *MCP for Unity → Asset Generation* before planning any work that assumes generated art.
> Simple geometric assets can still be drawn procedurally (that is how the stamina bolt was
> made); sprite sheets and animation frames cannot.

- Duck and Sparrow use chicken-baby placeholder sprites (`Assets/Resources/Animals/duck.asset`,
  `Sparrow.asset` point at `Chicken_Baby*.png`). Note these are also the only two `AnimalData`
  assets with a null `animatorController` — the same two gaps travel together
- Stamina/energy icon; see [UI_ART_PLACEHOLDERS.md](UI_ART_PLACEHOLDERS.md) for the full list
- **Combat animation clips** — no animal has attack/hurt/death art. Checked Aug/1: all 289
  AnimationClips in the project are Idle/Walk/Eat (plus player, NPC, tree, egg-hatch); not one
  is combat. This is the real gap and it needs art.
  **Correcting the old entry here**, which claimed AnimatorControllers "don't exist yet for any
  animal/enemy": **25 of 27 `AnimalData` assets have a controller**. Only `duck` and `Sparrow`
  are null — the same two on placeholder sprites. The controllers are farm controllers
  (`IsWalking`/`IsIdle`/`IsEating`); the combat trigger parameters were added Aug/1 (`7623020`)
  so `CombatUnit`'s `SetTrigger` calls are safe, but they drive no states yet — firing them is
  a no-op until clips exist

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
**Audio**: ~~`combatMusic`/`menuMusic` on `GameMusicManager`~~ **done Aug/1** (`11f93e6`) —
the clips were never missing, only unassigned; see KNOWN_BUGS.md for the `MusicContext` bug
that filling them in exposed. `seasonalFarmTracks[4]` intentionally left null (falls back to
`gameplayMusic`; only 3 OST tracks exist). Still open: SFX clips, and `OnEnterCombat()` is
driven by `SceneManager.sceneLoaded` rather than the never-instantiated `SceneTransitionManager`
**Combat consumables known limitation**: player `Inventory` only exists in SampleScene, so
`ConsumableBattleUI.RefreshList()` shows "no inventory" during battle — fix requires persisting
Inventory into CombatScene or reading from `SaveManager`/`GameData` (deferred).
Note when testing: the project's only two consumables are `Fish` and `RareFish`
(`isConsumable=true`); Carrot/Cabbage/Tomato are crops, so seeding those shows the empty-state
row and looks like a broken panel

---

## Known Tech Debt

See [review/02_FINDINGS.md](review/02_FINDINGS.md) for the full diagnostic. Headlines:
- God classes, after the Jul/25–26 sweeps: `Inventory` 1056, `MainMenuUI` 1036, `Animal` 1048,
  `SellBox` 998, `InventorySlot` 844. `SellBox` lost 176 lines, `Inventory` 91 and
  `InventorySlot` 63 to the container refactor
  ([review/04_CONTAINER_REFACTOR_PLAN.md](review/04_CONTAINER_REFACTOR_PLAN.md)), which also
  gave every container one shared transfer path instead of four hand-written copies, and one
  shared slot-building path (`ContainerView`) instead of three
- ~~`UIManager` has two coexisting window systems~~ — **fixed Jul/25**: the legacy
  `OpenPanel`/`ClosePanel` system is gone (`UIManager` 321 → 212 lines) and the `IUIWindow`
  stack is the single source of truth. `IsAnyPanelOpen()` callers moved to `IsAnyWindowOpen()`
- `SellBox` re-loads `GameBalance` via `Resources.Load` on every `sellMultiplier` access
- ~~**No `.gitattributes`**~~ — **fixed Jul/26**: `* text=auto` plus `eol=lf` on the Unity YAML
  types (`.unity`/`.prefab`/`.asset`/`.meta`/`.anim`/…). `.sh`/`.py` pinned `eol=lf` because
  `.github/scripts/*.sh` run on Linux in Actions; `.unity`/`.prefab` get `merge=binary` so a bad
  auto-merge fails loudly instead of silently corrupting a scene.
  **The old entry was wrong about the scale.** It predicted ~2800 files needing renormalization;
  the real number is **zero** — `git add --renormalize .` staged nothing, because every tracked
  file already stored LF (`core.autocrlf` was **`true`** locally, not unset as claimed). ~4200
  files are *governed* by the new rules, but none needed rewriting. The `.gitattributes` still
  earns its place by pinning that behaviour in the repo instead of leaving it dependent on one
  machine's local git config — which is what would have broken a Linux/CI checkout.
  Caveat for whoever reads this next: marking generated files `binary` is the wrong tool here.
  Tried it for `*.private.0` and it made Git store the working tree's CRLF verbatim, baking
  Windows endings into the repo (fixed in `ace4328`). The local symptom all of this cures is a
  file showing as modified with a completely empty diff

- **`??` and `?.` are unsafe on Unity objects.** `GetComponent<T>() ?? AddComponent<T>()` reads
  correctly and is silently broken: a missing component returns Unity's "fake null" (a live C#
  object with a destroyed native side), which `??` treats as non-null, so the fallback never
  runs. This threw a `MissingComponentException` on every combat spawn until Aug/1 (`7623020`).
  Use an explicit `== null`, which Unity's operator overload handles. Only one occurrence
  existed; worth re-grepping `\?\?` near `GetComponent` if this pattern reappears.
- **`UIThemeStyler.StylePanelTitle` hardcodes cream, which makes it a trap.** It forces every
  heading it finds to `backgroundCream` — correct for a heading on wood, silently wrong for one
  on a cream field, and it re-applies on every `Awake`, so a scene-authored colour looks right
  in the Editor and gets repainted at runtime. Bit the settings panel Aug/1. If a heading needs
  a different colour, look it up and `TintText` it rather than extending this method.
- **Runtime-built UI keeps re-inventing the same two layout bugs.** Code that builds UI at
  runtime (`ConsumableBattleUI.CreateRow`, `QuestsUIBuilder`) has now twice shipped a
  `RectTransform` left on default point anchors with `sizeDelta.x = 0`, and twice shipped a
  `VerticalLayoutGroup` with `childForceExpandWidth` but no `childControlWidth` (which does
  nothing on its own). Both were fixed Aug/1; a small shared helper for "stretch this new
  RectTransform across its parent" would stop the third occurrence.

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
