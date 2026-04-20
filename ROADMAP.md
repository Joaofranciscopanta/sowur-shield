# Sowur Shield — Development Roadmap

> Last updated: 2026-04-19
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
The ATB auto-chess loop runs and is playable, but is shallow.

**Working:**
- 9×5 grid (cols 0–5 enemy, 6–8 player)
- Speed-based ATB turn gauge
- Basic melee attack with defense mitigation
- Victory/defeat detection
- Player team spawning from AnimalRoster
- Enemy spawning from StageData
- Health bars, turn order UI, battle results screen

**Missing:**
- Animal skills — `AnimalSkill.cs` exists with data structure but is never executed in `TurnManager`
- No skill cooldowns, targeting rules, or AoE
- No status effects (stun, poison, burn, shield)
- No animal happiness/growth bonuses applied during combat
- Reward distribution incomplete — `CombatRewardData` exists but rewards aren't granted to player
- No XP or persistent animal level-up from combat results
- Animations minimal — only flash on hit, no attack/idle animation states in CombatUnit

### World Map
`WorldMapUiController`, `StageButton`, `WorldMapTriggerZone` exist as stubs.

**Working:**
- CombatTriggerZone hooks into farm scene to enter CombatScene
- StageButton wired to StageManager

**Missing:**
- No map rendering — no visual world map exists
- No stage unlock / progression tracking
- No boss completion flags
- No biome exploration flow (5 biomes defined in assets: Meadow, Forest, Cave, Mountain, Volcano; 25 stages + bosses fully defined in ScriptableObjects — just not surfaced to player)

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

1. **More crops** — At least 1 crop per season (currently only Carrot and Cabbage)
2. **Animal expansion** — At least 1 more animal type; animal health/illness system
3. **Farm buildings** — Barn (more animal slots), Greenhouse (grow out of season)
4. **Weather** — Rain waters crops automatically; drought speeds wilting

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

- `AnimalSkill.cs` — data structure fully defined, never called in TurnManager
- `WorldMapTriggerZone.cs` / `WorldMapUiController.cs` / `StageButton.cs` — stubs only
- FeedingTrough: `DuckEgg_GroundItem` and `Feather_GroundItem` prefabs need to be created and assigned in Unity Editor
- AnimalInfoUI rename panel: UI elements need to be wired in Unity Editor (scripts ready)
- No NPC affection tracking despite dialogue system supporting it
- `CombatRewardData` never distributed to player inventory
- Debug.Log calls remain in many scripts — clean up per-feature when shipping
