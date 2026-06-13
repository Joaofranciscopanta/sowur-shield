# Sowur Shield - Unity Farming Game

## ⚠️ IMPORTANT: Git Branch Policy

**ALWAYS USE `main` BRANCH - NEVER USE `master`**

- ✅ `git push origin main` | ❌ Never `git push origin master`
- Pull requests target: `main` | Default branch: `main`

## Project Overview

2D farming simulation game in Unity. Core systems: farming (multi-stage crops, soil states), inventory (36-slot drag/drop), dialogue (tree-based branching), minimap (3-state), animals (husbandry + feeding trough), combat, save/load, time/day cycle.

## Project Structure

```
Assets/Scripts/
├── Core Systems/       PlayerMove.cs, InteractionManager.cs, UIManager.cs, UIInput.cs, IInteractable.cs
├── Inventory/          Inventory.cs, InventoryItem.cs, ItemStack.cs, InventorySlot.cs, ItemTooltip.cs
├── Selling/            SellBox.cs
├── Farming (root, SowurShield.Core): SoilBlockInteractable.cs, CropData.cs, CropGrowthManager.cs,
│                       FarmBuildingManager.cs, FarmBuildingData.cs, WeatherController.cs
│   DualGridTilemap/    DualGridTilemap.cs, CursorController.cs  (SowurShield.Farming)
├── Dialogue/Core/      DialogueTree.cs, DialogueNode.cs, DialogueChoice.cs, DialogueCondition.cs, DialogueEffect.cs
│   Dialogue/UI/        DialogueTreeUI.cs, ChoiceButton.cs, PortraitManager.cs
│   Dialogue/Memory/    ConversationMemory.cs, ConversationData.cs
│   NPCDialogueInteractable.cs
├── Game Management/    GameData.cs, PlayerDataManager.cs, SaveManager.cs, PlayerStats.cs
│                       TimeController.cs, SceneTransitionManager.cs, MainMenuManager.cs
├── UI Systems/         GameMenuManager.cs, GameMenuUI.cs, MainMenuUI.cs, SaveGameUI.cs, SleepConfirmationPanel.cs
├── Minimap/            MinimapController.cs, MinimapCamera.cs, MinimapUI.cs, MinimapIcon.cs
├── Animals/            Animal.cs, AnimalData.cs, AnimalRoster.cs, AnimalRosterUI.cs, AnimalInfoUI.cs,
│                       AnimalHappinessIcon.cs, FeedingTrough.cs
└── Utility/            FollowPlayer.cs, GroundItem.cs, ToolType.cs, InventorySpacingFix.cs
```

## Core Game Systems

### Input System
- Framework: Unity New Input System — `PlayerControls.inputactions` → generated `PlayerControls.cs`
- WASD/Arrows: movement | E: interact (Press, NOT Hold) | Escape: menu | 1–9: inventory slots | Left-click: tool/interact
- **After editing `.inputactions`: must click "Generate C# Class" in Unity Inspector**

### Interaction System (Dual Architecture)
- **Primary**: `InteractionManager.cs` — priority-based, distance-calculated
- **Fallback**: collision-based in `PlayerMove.cs`
- **Left-click**: direct raycast collision with sprite (requires cursor over sprite)
- **E key**: proximity-based via InteractionManager (works within range)
- **Priority order**: Objects in hex (SellBox, NPCs, Soil) > Tools in hand (ALWAYS)
- Cursor: green=interactable object, yellow=tool usable, white=none

### Inventory System
- `Inventory.cs`: 36 slots (9 hotbar + 27 storage)
- `InventorySlot.cs`: drag/drop, animations, visual feedback
- `ItemStack.cs`: stacking with max sizes
- Items are ScriptableObjects; looked up via `ItemDatabase.GetItem(itemName)` — names must match exactly
- Save/load via ISaveable

### Tool & Cursor System (`CursorController.cs`)
- Detection priority for left-click:
  1. **Direct mouse hit** — raycasting + OverlapPoint for SellBox/NPCs
  2. **Grid detection** — 0.3f radius for SoilBlock/Bed only (`CheckForGridObjectsOnly()`)
  3. **Tool usage** — only when no objects detected
- Tools identified by itemTags: "Hoe", "WateringCan", "Shovel"
- Max distance enforced from player
- Cursor hidden during SellBox/inventory/dialogue open states

### Farming System
- **Soil states**: Regular → Tilled → Watered → WithCrop (`SoilBlockInteractable.cs`)
- **Crop growth**: multi-stage progression, water requirements, randomized yield, regrowth support (`CropGrowthManager.cs`)
- **CropData**: ScriptableObject — days per stage, seasonal restrictions, growth sprites
- **DualGridTilemap**: placeholder + display tilemaps, 16-tile rule-based display

### SellBox System (`SellBox.cs`)
- Auto-sells items during sleep cycle; sell multiplier default 80%
- Player movement disabled while open; cursor hidden
- Auto-close: WASD/E held 0.5+ seconds (brief presses don't trigger)
- Left-click requires cursor over actual SellBox sprite (not just hex tile)
- `SellBox.cs:86,112-118,281-286,311-316` — movement control integration

### Dialogue System
- `DialogueTree.cs` (ScriptableObject): branching conversations with conditions/effects
- `ConversationMemory.cs`: persistent conversation state tracking
- `DialogueTreeUI.cs`: typewriter effects, choice buttons, portrait display

### Time Management (`GameTimeController.cs` / `TimeController.cs`)
- Events: `OnDayChanged`, `OnTimeChanged`
- Sleep triggers day advancement; save/load supported

### Save/Load System
- `SaveManager.cs` + `GameData.cs` + ISaveable interface
- Save slots: `AutoSave`, `Slot1`, `Slot2`, `Slot3` → `Saves/<SlotName>/GameSave.json` + `SlotMeta.json`
- `SaveManager.activeSlotName` controls active slot; `TriggerAutoSave()` forces AutoSave then restores slot
- Data: player (position, stats, money), inventory, world (flags, counters, strings), time, farming, relationships
- `PlayerDataManager.LoadData` uses `position` directly — no bed-spawn override
- GroundItem: ISaveable, key = `gameObject.name` (must be unique per scene)
- Legacy flat `Saves/GameSave.json` auto-migrated to `AutoSave/` on first boot
- Public API: `SetActiveSlot`, `SaveToSlot`, `LoadFromSlot`, `DeleteSlot`, `GetSlotInfo`, `GetAllSlotInfos`, `GetMostRecentSlotName`
- `SaveSlotInfo.cs`: `Assets/Scripts/SaveSlotInfo.cs` | `SaveSlotButton.cs`: `Assets/Scripts/UI Systems/SaveSlotButton.cs`

### Minimap System
- Three states: Normal (corner, 100%), Semi-Transparent (corner, 50%), Fullscreen (center, zoom/pan, movement disabled)
- M key: cycle states | Mouse scroll: zoom (fullscreen only) | Arrow keys/drag: pan (fullscreen only) | ESC: close fullscreen
- DOTween transitions; RenderTexture on Minimap layer; IUIWindow integration
- Components: `MinimapController.cs`, `MinimapCamera.cs`, `MinimapUI.cs`, `MinimapIcon.cs`

### Animal Husbandry System
- `AnimalData.cs` (ScriptableObject): stats, feeding requirements, heart particle prefab
- `Animal.cs`: petting (+5 happiness, first pet/day spawns heart), feeding (+3 happiness), production (spawns GroundItem)
- Happiness: 0–100, starts 50; decay if not petted (-0.5/day) or not fed (-1.0/day); floor = 20
- Second pet of day opens `AnimalInfoUI`; save/load via ISaveable
- Animal ScriptableObjects: `Assets/Resources/Animals/<name>.asset`

**Heart Particle Setup:**
1. Import sprite frames — `Texture Type: Sprite`, `Sprite Mode: Multiple`, slice in Sprite Editor
2. Create Particle System; Renderer module: **`Default-Particle` material** (NOT `Default-ParticleSystem` — causes invisible particles)
3. Sorting Layer: match scene sprites; Order in Layer: higher than animal (e.g. 10)
4. Texture Sheet Animation: enable, Mode=Sprites, add frames in order; Loop: off; Play On Awake: on
5. Save as Prefab from **Project window** (never drag from Hierarchy — causes Type Mismatch)
6. Assign prefab to `heartParticlePrefab` on **AnimalData ScriptableObject** (not the Animal GameObject)
- Sorting overridden at runtime: `psr.sortingLayerName = "Default"`, `psr.sortingOrder = 9999`
- Files: `Assets/Scripts/Animals/Animal.cs` (`SpawnHeartParticle()`), `Assets/Prefabs/Heart_particule.prefab`

### FeedingTrough System (`FeedingTrough.cs`)
- Implements IInteractable (E key), IUIWindow, ISaveable; food stored in `InventoryContainer`
- On `OnDayChanged`: iterates zone animals, looks up `AnimalData.dailyFoodRequirements` via `ItemDatabase`, calls `Animal.AutoFeed(amount)`
- `itemName` in `FoodRequirement` must match `ItemDatabase` key exactly (logs warning + skips on mismatch)
- **Drag-drop (TroughMode)**: `EnableTroughMode(container)` on slots; inventory↔trough transfers; trough→ground cancelled (item restored); item removed from container only in `OnEndDrag` after `wasDroppedOnSlot == true`
- Sprite: empty (count==0), partial, full (occupiedSlots ≥ slotCount/2)
- Status text: `"Food stored: X items\nCan feed: Y/Z animals tomorrow"` — `GetFeedableAnimalCount()` uses `Dictionary<Item, int>` (not strings)
- Save keys: `feedingtrough_{gameObject.name}_slot{i}_item` / `_qty` in `worldData`
- Files: `FeedingTrough.cs`, `InventorySlot.cs` (`EnableTroughMode()`, `IsTroughMode`, `OnDrop`/`OnEndDrag`)

## Bug Fixes Applied

### Bug #1: SellBox E Key Interaction
**Problem**: Hold(duration=0.2) on Interact action; PlayerControls.cs not regenerated after .inputactions change.
**Fixes**: Removed hold from `PlayerControls.inputactions:41`; accept `performed`+`started` phases in `PlayerMove.cs:147`; fallback `Input.GetKeyDown(KeyCode.E)` in `PlayerMove.cs:58-62`; SellBox uses actual interaction range in `InteractionManager.cs:176-179,205-214`.
**Files**: `PlayerControls.inputactions`, `PlayerMove.cs:58-62,144-197`, `InteractionManager.cs:176-179,205-214`, `SellBox.cs:252-256`

### Bug #2: Interaction Priority (Hoe vs SellBox)
**Problem**: Tools had precedence over objects; no movement restriction during SellBox open.
**Fixes**: Objects always > tools; enlarged detection radius (0.6f); movement disabled when SellBox open; tool cursor hidden when SellBox active.
**Files**: `CursorController.cs:115-175,217-256,303-312`, `SellBox.cs:86,112-118,281-286,311-316`

### Bug #3: Hex-Based vs Direct Cursor Detection
**Problem**: SellBox triggered on any click in hex tile, not requiring cursor over sprite.
**Fix**: Left-click uses direct raycast on sprite; E key stays proximity-based. Added `CheckForDirectMouseHit()`.
**Files**: `CursorController.cs:116-152,234-300`, `PlayerMove.cs:169-170`

### Bug #4: Grid Objects Contaminating Direct Detection
**Problem**: `ProcessHexInteraction()` fell back to large-radius `CheckForInteractableAt()` catching SellBox/NPCs.
**Fix**: Created `CheckForGridObjectsOnly()` for soil/beds only (0.3f radius); removed SellBox/NPC from hex path.
**Files**: `CursorController.cs:137-146,320-342`, `CursorController.cs:104,200-218`

**⚠️ Unity Input System Note**: After modifying `.inputactions`, select the file in Project window → click "Generate C# Class" in Inspector → ensure generated class connected to PlayerMove.

## Development Notes

### Architecture
- Singletons: UIManager, InteractionManager, SaveManager, GameTimeController
- Interfaces: IInteractable, IUIWindow (all UI windows), ISaveable
- UI windows: register with UIManager, open via `TryOpenWindow(this)`, close via `TryCloseWindow(this)`
- Player movement disabled via `FindObjectOfType<PlayerMove>()?.DisableMovement()`
- Assembly defs: `SowurShield.Runtime.asmdef` (all gameplay), `SowurShield.Tests.PlayMode.asmdef` (`includePlatforms: ["Editor"]`), `Assets/Scripts/Dialogue/Editor/SowurShield.Dialogue.Editor.asmdef` (`includePlatforms: ["Editor"]`)

### Namespace Convention (v0.5+, MANDATORY)

**ALL new scripts MUST declare a namespace** using `SowurShield.<System>`:

| Folder | Namespace |
|--------|-----------|
| `Scripts/` (core, managers) | `SowurShield.Core` |
| `Scripts/Inventory/` | `SowurShield.Inventory` |
| `Scripts/Animals/` | `SowurShield.Animals` |
| `Scripts/Combat/` | `SowurShield.Combat` |
| `Scripts/Dialogue/` | `SowurShield.Dialogue` |
| `Scripts/Dialogue/Editor/` | `SowurShield.Dialogue.Editor` |
| `Scripts/Editor/` | `SowurShield.Editor` |
| `Scripts/DualGridTilemap/`, `Scripts/Farming/` | `SowurShield.Farming` |
| `Scripts/MapEditor/` | `SowurShield.MapEditor` |
| `Scripts/Minimap/` | `SowurShield.Minimap` |
| `Scripts/Worldmap/` | `SowurShield.Worldmap` |
| `Scripts/UI Systems/` | `SowurShield.UI` |
| `Scripts/Debugging/` | `SowurShield.Debugging` |

Fallback for uncategorized: `SowurShield.Core`. Do NOT create namespaces outside this pattern.

```csharp
using UnityEngine;
using SowurShield.Inventory;   // cross-namespace refs need using directives

namespace SowurShield.<System>
{
    public class MyClass : MonoBehaviour { }
}
```

### Common Pitfalls
- After editing `.inputactions` → regenerate PlayerControls.cs in Unity Inspector
- Debug.Log: clean up after feature is done (use LogWarning/LogError for production)
- Empty `if (!condition) {}` blocks after removing logs should also be cleaned up
- GroundItem names in scene must be unique — ISaveable key is `gameObject.name`
- `itemName` in FoodRequirement must match ItemDatabase key exactly

## Scene Setup Requirements

**Required GameObjects:**
- Player: "Player" tag + PlayerMove component
- InteractionManager: InteractionManager script
- UIManager: UIManager script
- Main Camera: "MainCamera" tag

**SellBox**: SellBox script + Collider2D (IsTrigger=true) + interactable layer + UI refs assigned

**Main Menu Scene**: MainMenuManager (manager GO), MainMenuUI (UI Canvas), SceneTransitionManager (persistent GO, DontDestroyOnLoad)

**Scene flow:**
```
MainMenu → MainMenuUI → SceneTransitionManager → MainGameScene
     ↑                                                  ↓
GameMenuManager ← "Quit to Main Menu" ← In-Game ESC ←──┘
```

**Save slot UI** (needs Unity wiring):
- Main menu: `slotPickerPanel`, `slotListParent`, `saveSlotButtonPrefab`, `slotPickerBackButton`, `slotPickerTitleText` on `MainMenuUI`
- Pause menu: `saveSlotPanel`, `saveSlotListParent`, `saveSlotButtonPrefab`, `saveSlotPanelTitle`, `saveSlotBackButton` on `GameMenuUI`
- AutoSave: locked/hidden for manual saves; empty slots locked for Load

## WebGL Demo Deployment (GitHub Pages)

**Live Demo**: https://joaofranciscopanta.github.io/sowur-shield/

**Architecture:**
```
Unity Cloud Build (WebGL) → GitHub Actions (.github/workflows/deploy-webgl-demo.yml)
→ CSS Preservation (.github/templates/style.css) → GitHub Pages (docs/ on main)
```

**Workflow**: Weekly Sunday 3AM UTC; also manual trigger via Actions UI.
- Downloads latest Unity Cloud Build, decompresses Brotli (.br) files, restores custom CSS, verifies build, deploys

**Required GitHub Secrets**: `UNITY_API_KEY`, `UNITY_ORG_ID`, `UNITY_PROJECT_ID`, `UNITY_BUILD_TARGET_ID`
**Optional**: `DISCORD_WEBHOOK_URL` for deployment notifications

**Critical — Brotli decompression**: Unity 6 builds use `.br` compression; GitHub Pages can't serve these correctly → workflow decompresses them during deployment to prevent "Unable to parse Build/file.br" errors.

**CSS**: `docs/TemplateData/style.css` contains custom sidebar styling (Unity builds overwrite it). Master copy: `.github/templates/style.css`. Manual restore: `./.github/scripts/restore-css.sh .github/templates/style.css docs/TemplateData/style.css`

**Rollback**: Each deployment creates tag `backup/webgl-demo-YYYYMMDD-HHMMSS`; restore with `git checkout <tag> -- docs/`

**Troubleshooting:**
- Build download fails → verify GitHub secrets + Unity Cloud Build has successful WebGL builds
- CSS missing → check `.github/templates/style.css` exists + restore-css.sh ran
- Pages not updating → wait 2-5min; check Source = "main branch /docs folder"
- `.br` parse error → check "Decompress Brotli Files" step in workflow logs; re-run if needed

## Git Workflow

```bash
git checkout main && git pull origin main
git checkout -b feature/your-feature-name
# ... make changes ...
git add specific-files && git commit -m "Descriptive message"
git push origin feature/your-feature-name
gh pr create --title "Your Feature" --body "Description"
```

**High-conflict files (coordinate before modifying):** `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/SampleScene.unity`, `PlayerControls.inputactions`, `CLAUDE.md`

**This CLAUDE.md takes PRIORITY** — it contains critical bug fix history, architecture decisions, and Unity setup requirements. New Claude instances: use this as primary, only add to it.
