# Sowur Shield — Development Roadmap

> Last updated: 2026-04-20
> Branch: `main`

---

## Current State Overview

Sowur Shield is a 2D farming + auto-chess combat game. The farming side is production-ready. The combat loop is mechanically functional but shallow. The world map is a stub.

**4 scenes**: MainMenu, SampleScene (farm), CombatScene, MapEditorScene
**135 scripts**, 12 test files, 100+ ScriptableObject assets

---

## What's Complete ✅

### Core Gameplay
- **Player movement** — WASD, dash with cooldown, animation integration
- **Interaction system** — E-key proximity + left-click sprite collision, priority-based (objects > tools)
- **Time & Day cycle** — Real season system (28-day year: Spring → Summer → Fall → Winter), `OnDayChanged`/`OnSeasonChanged` events
- **Save / Load** — Multi-slot (AutoSave + 3 manual slots), full persistence via ISaveable, legacy migration

### Farming
- **Soil states** — Regular → Tilled → Watered → WithCrop
- **Crop growth** — Multi-stage progression, watering requirement (death on drought), regrowth support, seasonal restrictions
- **Crops available**: Carrot, Cabbage
- **DualGridTilemap** — 16-tile rule-based rendering

### Economy
- **Inventory** — 36 slots (9 hotbar + 27 storage), drag/drop, stacking, rarity glowing, tooltips, number key selection
- **SellBox** — Auto-sells on sleep (80% price), drag/drop integration, movement lock while open, auto-close on walk-away
- **Items** — 30+ items: produce (Egg, DuckEgg, Feather, Carrot…), seeds, tools (Hoe, WateringCan, Shovel), food

### Animal Husbandry
- **3 animals** — Chicken (Spring), Duck (Summer), Sparrow (Fall)
- **Care mechanics** — Petting (+happiness, heart particle), feeding (via hand or FeedingTrough), daily decay if neglected
- **Production** — Egg (daily), DuckEgg (daily), Feather (every 2 days); happiness bonus on full care
- **Combat growth** — Persistent stat multipliers: petting +0.01, full feeding +0.02 per day, capped 3×, 4 stats (Attack/Defense/Speed/Health)
- **Seasonal bonuses** — Each animal gets stat multipliers in their preferred season
- **Naming** — Player can rename any animal (max 20 chars), persisted
- **AnimalRosterUI + AnimalInfoUI** — Full inspection and rename UI
- **FeedingTrough** — Stores food, auto-feeds registered animals at day change

### UI & Meta
- **Minimap** — 3 states (corner → semi-transparent → fullscreen), zoom 3 levels, pan, DOTween transitions
- **Dialogue** — Branching tree, conditions, effects, typewriter, portraits, persistent conversation memory
- **Main Menu** — New game, continue, settings (volume/resolution), credits, quit; async scene transitions with loading screen
- **GameMenu (pause)** — Save, load slot picker, quit to main menu
- **UIManager** — Priority-based window stacking, ESC closes top window

---

## In Progress 🔶

### Combat System
Combat loop is functionally complete. Needs Unity Editor wiring to be playable.

**Working:**
- 9×5 grid (cols 0–5 enemy, 6–8 player)
- Speed-based ATB turn gauge (allocation-free update loop)
- Skill execution with cooldowns, accuracy, damage multiplier, self-heal
- Status effects: Stun (skip turn), Shield (0–0.9 damage reduction), Burn (DoT)
- Status immunities: enemies with `immunities` list block specific effects
- Reward delivery: gold → PlayerStats, loot → Inventory, happiness → animals
- XP + level-up (1–10): surviving animals gain XP, level up every level×100 XP, +5% all stats per level
- Happiness decay on defeat/draw (−5 all), dead units in victory (−3)
- Combat animations: Attack/Hurt/Die triggers on AnimatorController

**Needs Unity Editor wiring:**
- Assign `AnimalSkill` ScriptableObjects to `AnimalData.activeSkill` fields
- Assign `AnimatorController` to `AnimalData.animatorController` fields
- Populate `EnemyData.skills` and `EnemyData.immunities` on enemy assets

### World Map
Fully implemented in code. Needs Unity Editor wiring to be playable.

**Working:**
- `WorldMapBiomePanel`: expandable biome panels with stage buttons + lock icon
- `BiomeUnlockChecker`: boss completion, biome unlock, stage count queries
- `WorldMapUIController`: syncs save → StageManager, refreshes panels on open
- `StageButton`: lock/unlock state from worldFlags, routes to TeamAssemblerUI
- `WorldMapTriggerZone`: E-key entry point in farm scene

**Needs Unity Editor wiring:**
- Create WorldMap Canvas in farm scene, add `WorldMapUIController`
- Create one `WorldMapBiomePanel` GameObject per biome, wire to controller's `biomePanels` list
- Assign `WorldMap` reference on `WorldMapTriggerZone` in farm scene

---

## Planned ❌

### Phase 1 — Close the Combat Loop
Priority: ship a complete combat experience before adding new systems.

1. [x] **Skill execution** — `TurnManager.ExecuteUnitTurn` checks `CombatUnit.GetReadySkill()` first; `ExecuteSkill()` applies damage multiplier, healing, and status effects; player skill from `AnimalData.activeSkill`, enemies use `EnemyData.skills` + `skillUseChance`
2. [x] **Reward delivery** — `BattleResultsUI.AwardRewards()` grants gold → `PlayerStats.AddMoney()`, items → `Inventory.AddItem()`, happiness → `ModifyHappiness()` on surviving units; triggered on "Return to Farm" button
3. [x] **Animal combat feedback** — Victory: dead units −3 happiness; Defeat/Draw: all player units −5 happiness (applied in `TurnManager.EndBattle`)
4. [x] **Basic status effects** — `CombatStatusEffect.cs` + `CombatUnit` API: Stun (skip turn, auto-expire), Shield (damage reduction 0–0.9 cap), Burn (DoT per tick); applied via `AnimalSkill.statusEffect` field
5. [x] **Combat animations** — `CombatUnit` wires `AnimalData.animatorController`; triggers `Attack`, `Hurt`, `Die` hashes on respective events

### Phase 2 — World Map
Unlock the 25 stages and 5 biomes already defined in assets.

1. [x] **Map UI** — `WorldMapBiomePanel.cs`: expandable panel per biome with stage buttons, completion counter, lock icon; populated via `WorldMapUIController.RefreshBiomePanels()`
2. [x] **Stage progression** — `StageButton` already reads `StageManager` + worldFlags; `TurnManager.ComputeRewards` marks `stage_completed_{name}` in worldFlags on victory; `WorldMapUIController.SyncStageProgressFromSave()` hydrates StageManager from save on map open
3. [x] **Boss completion / biome unlock** — `BiomeUnlockChecker.IsBiomeComplete()` checks boss stage completion; `WorldMapBiomePanel` shows lock icon when biome has no unlocked stages
4. [x] **Map entry from farm** — `WorldMapTriggerZone` (E key) → `WorldMapUIController.OpenMap()` → `StageButton.OnClick()` → `TeamAssemblerUI.OpenAssembler()` → load CombatScene

### Phase 3 — Farm Expansion
Deepen the farming loop.

1. [x] **More crops** — `CropCreatorWindow` editor tool creates Tomato (Summer) + Winter Wheat (Winter); run via `Tools > Sowur Shield > Create Crop Assets` in Unity
2. **Animal expansion** — At least 1 more animal type; animal health/illness system
3. **Farm buildings** — Barn (more animal slots), Greenhouse (grow out of season)
4. [x] **Weather** — `WeatherController.cs`: Rain (auto-waters all tilled/watered soil), Drought (accelerates crop wilting); subscribe to `OnDayChanged`; place in SampleScene

### Phase 4 — NPC & World
1. **NPC relationships** — Track affection per NPC; unlock dialogue branches and gifts at thresholds; save via worldCounters
2. **Quests** — Simple task-based quest system using existing dialogue infrastructure
3. **Shop NPC** — Buy seeds, tools, and animal feed; prices affected by relationship level

### Phase 5 — Polish & Release
1. **Audio** — Background music per scene and season; SFX for all interactions (currently partial)
2. **Tutorial** — Guided first day: till soil → plant → water → sleep → harvest
3. **WebGL build** — Monthly deploy via existing GitHub Actions workflow
4. **Delete all debug logs** — Final cleanup pass per CLAUDE.md policy
5. **Achievements / completion** — Optional stretch goal

---

## Asset Inventory Reference

| Type | Count | Notes |
|------|-------|-------|
| Enemy ScriptableObjects | 39 | 5 biomes fully defined |
| Stage ScriptableObjects | 25 | 5 per biome, ready to use |
| Animal types | 3 | chicken, duck, sparrow |
| Crop types | 2 | carrot, cabbage |
| Items | 30+ | produce, seeds, tools, food |
| Prefabs | 20 | combat, UI, interactables |
| Scenes | 4 | menu, farm, combat, map editor |
| Test files | 12 | EditMode + PlayMode |

---

## Known Gaps / Tech Debt

- AnimalSkill assets need to be created in Unity Editor and assigned to AnimalData.activeSkill / EnemyData.skills
- WorldMap Canvas + BiomePanels need to be created and wired in Unity Editor (all code done)
- AnimalInfoUI rename panel: UI elements need to be wired in Unity Editor (scripts complete)
- WeatherController: needs to be placed as a GameObject in SampleScene
- CropCreatorWindow: run `Tools > Sowur Shield > Create Crop Assets` once in Unity Editor
- FeedingTrough: `DuckEgg_GroundItem` and `Feather_GroundItem` prefabs in `Resources/Prefabs/GroundItems/` need sprites assigned in Editor
- No NPC affection tracking despite dialogue system supporting it
- Debug.Log calls remain in many scripts — clean up per-feature when shipping
