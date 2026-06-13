# Progress

- 2026-06-13: TASK-002 done. Decision: Option A (descope). Added clarifying comments to
  `AnimalData.cs` (`combatClass`, `availablePassiveSkills`) and updated
  `SOWUR_SHIELD_STATUS.md` "Combat Scope" section to "Resolved (Option A, descope)".
- 2026-06-13: TASK-003 marked N/A — descoped per TASK-002.
- 2026-06-13: World map progression wired. All 25 stage `.asset` files now form one
  continuous `prerequisiteStage` chain (Meadow 001 -> 005 -> Forest 006 -> 010 ->
  Cave 011 -> 015 -> Mountain 016 -> 020 -> Volcano 021 -> 025), done via direct YAML
  edits (Unity batch mode unavailable - license error). Added
  `Assets/Scripts/Editor/StageProgressionLinker.cs` as an idempotent Editor-menu tool
  that does the same thing programmatically if a re-run is ever needed.
- 2026-06-13: Combat AI targeting overhauled in `TurnManager.SelectTarget()` -
  lethal-first targeting (kills prioritized over chip damage, shield-adjusted),
  front-column fallback now tiebreaks by HP% instead of raw HP. Added
  `EstimateAttackDamage()` helper shared with `ExecuteAttack`. Removed dead
  `verboseLogging` field and empty if-blocks. New EditMode test file
  `Assets/Tests/EditMode/CombatTargetingTests.cs` (7 tests, all logic verified by
  hand against production formulas).
- 2026-06-13: Team Assembler UI overhaul - `AnimalSelectionCard.cs`,
  `GridPositionSlot.cs`, `TeamAssemblerUI.cs` rewritten to remove debug/placeholder
  cruft (runtime texture generation, viewport-mask hacks, debug borders) and
  redesign the fed/hungry indicator as color-coded status (green/yellow/gray) with
  matching grid-slot states. Wired previously-unused `FixPanelLayout()` into
  `OpenAssembler()`. `TeamAssemblerUISetup.cs` updated for the `selectedColor` ->
  `hoverColor` field rename.
- 2026-06-13: Independent reviewer agent audited all of the above (TurnManager,
  CombatTargetingTests, UI overhaul, stage chain) - no blocking issues. Only note:
  `CombatScene.unity`/`SampleScene.unity` still have stale serialized fields for
  removed script members (`verboseLogging`, `disableViewportMask`, etc.) - harmless,
  Unity will strip them on next scene save in the Editor.
- 2026-06-13: TASK-013 done (verification only, no changes needed). Both
  `Assets/Tests/EditMode/SowurShield.Tests.EditMode.asmdef` and
  `Assets/Tests/PlayMode/SowurShield.Tests.PlayMode.asmdef` exist, both have
  `includePlatforms: ["Editor"]` and reference `SowurShield.Runtime` correctly.
- 2026-06-13: TASK-009 done. Removed the dead `if (interactable is SellBox sellBox) { }`
  no-op branch from `InteractionManager.SetInteractablePromptVisibility()`.
- 2026-06-13: TASK-010 done. Fixed CLAUDE.md "Project Structure" tree - farming scripts
  (`SoilBlockInteractable.cs`, `CropData.cs`, `CropGrowthManager.cs`,
  `FarmBuildingManager.cs`, `FarmBuildingData.cs`, `WeatherController.cs`) actually live
  in `Assets/Scripts/` root with `SowurShield.Core` namespace, not in a `Farming/`
  subfolder. Only `DualGridTilemap/` uses `SowurShield.Farming`. Tree updated to reflect
  this; no files moved, no namespaces changed.
- 2026-06-13: TASK-005 done. `QuestManager` now caches `PlayerStats`/`Inventory`
  references in `Start()` instead of calling `FindFirstObjectByType` on every
  `GrantRewards()`. Added lazy re-resolve (if cached field is still null) plus
  `Debug.LogWarning` when a reward target is missing, so a dropped reward is now
  visible in the console instead of silently swallowed.
- 2026-06-13: TASK-004 done. `SaveManager.MigrateSave()` now walks
  `data.saveVersion -> GameData.CURRENT_SAVE_VERSION` via a while/switch dispatch
  (currently all no-op cases), logging a `LogWarning` if any migration step ran. Added
  new `Assets/Tests/EditMode/SaveManagerTests.cs` (2 tests, via reflection on the
  private method) covering "below current version" and "at current version" cases.
  Also fixed a pre-existing broken test in `GameDataTests.cs`
  (`Constructor_SetsSaveVersionToDefault` was asserting `data.saveVersion` (an `int`,
  default `1`) equals the string `"1.0.0"` - corrected to `Assert.AreEqual(1,
  data.saveVersion)`). `CURRENT_SAVE_VERSION` value (1) was NOT changed.
- 2026-06-13: TASK-007 done. New `Assets/Tests/EditMode/DialogueConditionTests.cs`
  (11 tests) covering `Always` (incl. inverted), `RelationshipLevel`
  (GreaterOrEqual/LessThan), `QuestStatus` (Equals, case-insensitive), `VariableCheck`
  (Equals/NotEquals), `ConversationCompleted`/`ChoiceMade` (false-when-never-recorded),
  and `InventoryItem` (false when no Inventory in scene + unknown item). Follows the
  `NPCRelationshipTests.cs` pattern: fresh `ConversationMemory` via `AddComponent` per
  test (sets `ConversationMemory.Instance` for the condition's internal lookups).
- 2026-06-13: TASK-008 done. New `Assets/Tests/EditMode/DialogueEffectTests.cs`
  (8 tests) covering `SetVariable` (round-trip via `VariableCheck` condition),
  `ModifyRelationship` (delta + clamp to both +100 and -100), `SetQuestStatus`
  (round-trip via `GetQuestStatus`), and `GiveItem`/`TakeItem` with an unknown item
  name (logs warning, does not throw — verified via `Assert.DoesNotThrow`).
- 2026-06-13: TASK-006 investigated, NOT removed (option b — documented, follow-up
  needed). Findings:
  - `SellBox.OpenSellBox()`/`CloseWindow()` call BOTH the current window-stack API
    (`TryOpenWindow`/`TryCloseWindow`, via `IUIWindow`) AND the legacy panel API
    (`UIManager.OpenPanel(sellBoxMainPanel)` at `SellBox.cs:429`,
    `UIManager.ClosePanel(sellBoxMainPanel)` at `SellBox.cs:509`) on the same panel.
  - `UIManager.HandleEscapeKey()` (ESC handling) reads ONLY `openWindowStack` — the
    legacy `currentlyOpenPanel`/`allUIPanels` are NOT involved in ESC. Safe to remove
    from that angle.
  - HOWEVER, `UIManager.IsAnyPanelOpen()` (reads legacy `currentlyOpenPanel`, set by
    `OpenPanel`/cleared by `ClosePanel`) IS consumed by two real call sites:
    `PlayerMove.cs:169` (gates E-key `TryInteract` while "any panel" is open) and
    `UIInput.cs:90` (cursor-visibility fallback in `EnsureCursorVisibleDelayed`).
  - `UIInput.cs` already has its own SellBox-specific fallback (`sellBox.IsOpen` check
    at `UIInput.cs:95-99`), so removing the legacy calls would NOT break cursor
    visibility.
  - `PlayerMove.cs:169` is the risk: if `OpenPanel`/`ClosePanel` are removed from
    SellBox, `IsAnyPanelOpen()` would no longer return `true` while SellBox is open,
    so `TryInteract()` would no longer be gated by "any panel open" for SellBox.
    SellBox already calls `playerMove.DisableMovement()` while open, but movement-
    disable and the E-key interact gate in `PlayerMove.cs:144-197` are separate code
    paths — it is NOT proven from static reading alone that `DisableMovement()` also
    blocks `TryInteract()`'s E-key path.
  - Also noted: `UIManager.inventoryPanel`/`sellBoxPanel`/`gameMenuPanel` (distinct
    serialized fields from `SellBox.sellBoxMainPanel`) feed `allUIPanels` at `Awake()`
    — if `sellBoxPanel` is wired in the Inspector to the same/another GameObject,
    `CloseAllPanels()` could affect a different object than `sellBoxMainPanel`. Not
    confirmed either way without checking scene serialization.
  - **Follow-up (new task candidate)**: before removing `OpenPanel`/`ClosePanel` calls
    from `SellBox.cs:429,509`, do a manual test in Unity Editor — open SellBox, press
    E while it's open, confirm `TryInteract()` is already blocked by
    `DisableMovement()` (or some other guard) independent of `IsAnyPanelOpen()`. If
    confirmed blocked, the legacy calls + the dead `OpenPanel`/`ClosePanel`/
    `CloseAllPanels`/`IsAnyPanelOpen`/`currentlyOpenPanel`/`allUIPanels` machinery in
    `UIManager.cs` can be removed in one pass (update `PlayerMove.cs:169` and
    `UIInput.cs:90` to drop the `IsAnyPanelOpen()` checks too). If NOT blocked, keep
    the legacy calls and instead document this as an intentional dual-system
    dependency in CLAUDE.md/SOWUR_SHIELD_STATUS.md.
- 2026-06-13: Test run (`TestResults_20260613_110149.xml`, 440 tests, 100 failed)
  triaged. Two pre-existing root causes found and fixed (both unrelated to TASK-001
  through TASK-013 above, present in committed code before this session):
  - **`CombatUnit.CreateSphereVisual()` (`Assets/Scripts/Combat/CombatUnit.cs:301`)**
    called `Destroy(visualObject.GetComponent<Collider>())` unconditionally. In
    EditMode, `Destroy()` logs `[Error] Destroy may not be called from edit mode!`,
    which NUnit treats as a test failure via `LogAssert`. This single line caused
    ALL 28 `CombatPhase1Tests` and ALL 7 `CombatTargetingTests` to fail (every test
    goes through `CreateUnit()` -> `InitializeAsEnemy()` -> `SetupVisuals()` ->
    `CreateSphereVisual()`). Fixed with an `Application.isPlaying` guard
    (`Destroy` in play mode, `DestroyImmediate` in edit mode) — 35 tests affected.
  - **Singleton `Instance` not cleared in `OnDestroy()`** for `ConversationMemory`
    (`Assets/Scripts/Dialogue/Memory/ConversationMemory.cs`) and `QuestManager`
    (`Assets/Scripts/Dialogue/QuestManager.cs`). Both follow the
    `if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);`
    pattern but never reset `Instance` on destroy. Across EditMode test fixtures
    (`DialogueConditionTests`, `DialogueEffectTests`, `NPCRelationshipTests`,
    `QuestSystemTests`), each test's `[SetUp]` creates a fresh instance via
    `AddComponent`, but the *first* test's `DestroyImmediate` in `[TearDown]` left
    `Instance` pointing at a destroyed object. The second+ test's `Awake()` then saw
    `Instance != null && Instance != this`, called `Destroy(gameObject)` (another
    EditMode log error) and left the new instance's data unused — so
    `ConversationMemory.Instance`/`QuestManager.Instance` (read internally by
    `DialogueCondition.IsConditionMet()` etc.) pointed at a destroyed (Unity
    fake-null) object for every test after the first. Fixed by adding
    `if (Instance == this) Instance = null;` to both classes' `OnDestroy()` —
    27 tests affected (7 + 5 + 7 + 8).
  - Total addressed: 62/100 failures. Remaining 38 are pre-existing and unrelated:
    `AnimalHusbandryTests` (4, `AnimalRoster` singleton — same class of bug but not
    yet root-caused, `AnimalRoster.cs` already has the `OnDestroy` reset so the
    cause is different) and PlayMode tests (34, `CropGrowthPlayModeTests` +
    `PlayerStatsPlayModeTests` — likely environment/PlayMode-runner related, not
    investigated). Re-run the full suite after these fixes to confirm the 62 and
    re-scope the remaining 38.
- 2026-06-13: Combat UI refactor (`Assets/Scenes/CombatScene.unity`, full scope incl.
  Turn Order visual, chosen by user). Direct YAML edits:
  - **TopBar**: `TurnCounterText`/`PlayerTeamText`/`EnemyTeamText` font size 14 -> 24
    (was unreadably small at 1920x1080/1280x720 reference resolutions). `TopBar`
    RectTransform height 42 -> 56px (`m_AnchoredPosition.y` -28, `m_SizeDelta.y` 56)
    to give the larger text room within its `HorizontalLayoutGroup` padding.
  - **Victory/Defeat panels**: `VictoryStatsText`, `VictoryRewardsText`,
    `DefeatStatsText` were all 200x50px boxes at fontSize 36, used to display the
    5-line `GetBattleStatsText()` output — text was massively overflowing/clipped.
    Repositioned all three to center-anchored 480x320px boxes (`VictoryStatsText` at
    x=-260, `VictoryRewardsText` at x=+260, both y=-50, side-by-side either side of
    panel center; `DefeatStatsText` centered at x=0 since DefeatPanel has no rewards
    text) and reduced fontSize 36 -> 28 on all three. `VictoryPanel` background color
    `(0,0.8,0,0.4)` -> `(0,0.6,0,0.8)` — was too transparent for white text contrast
    (DefeatPanel's `(0.8,0,0,0.8)` was already fine, used as reference).
  - **Turn Order panel** (previously unwired — `BattleStatusUI.turnOrderPanel` and
    `turnOrderIconPrefab` were both `{fileID: 0}`, so `InitializeTurnOrderIcons()`
    early-returned and the feature was invisible despite `TurnManager` calling
    `UpdateTurnOrder`/`HighlightActingUnit` correctly every turn). Added new
    `TurnOrderPanel` GameObject (fileIDs 700100001-700100005) as a sibling of
    `TopBar` under `BattleStatusCanvas`: top-anchored full-width strip, 40px tall,
    positioned directly below TopBar (`m_AnchoredPosition.y` -76), semi-transparent
    black background (`0,0,0,0.5`), `HorizontalLayoutGroup` (left-aligned, 6px
    spacing, `m_ChildForceExpandWidth: 0` so the 10 pooled 30x30 icons don't stretch).
    Wired `BattleStatusUI.turnOrderPanel` -> `{fileID: 700100001}` and
    `turnOrderIconPrefab` -> the existing (previously-unused) `TurnOrderIcon.prefab`
    (`{fileID: 8453758217787325226, guid: ff7477731212de44090be5e095a9aea6, type: 3}`).
    `InitializeTurnOrderIcons()` will now instantiate 10 icon pool entries at runtime;
    `UpdateTurnOrder` colors them by team (blue/red) with opacity scaled by
    `turnGauge`, `HighlightActingUnit` flashes icon 0 white briefly each turn.
  - All edits verified via Python YAML-document-count check (130 unique fileIDs,
    no duplicates) — `pyyaml` unavailable in this environment so full YAML-syntax
    parse was not possible; structure cross-checked manually against existing
    `TopBar`/`VictoryPanel` blocks for indentation/format consistency.
  - **Not yet done**: open in Unity Editor to visually confirm layout (no Editor
    access in this session — Unity license error blocked batch mode per earlier
    entries). Recommend a manual smoke-test of a full combat encounter to confirm
    TopBar/Victory/Defeat readability and that the Turn Order strip renders/updates
    correctly under the TopBar without overlapping the battlefield.
- 2026-06-13: Victory/Defeat results-screen bug fixes (`Assets/Scenes/CombatScene.unity`),
  direct consequence of the Combat UI refactor above (repositioned
  `VictoryStatsText`/`VictoryRewardsText` now span the panel center, exposing a
  previously-harmless center-anchored stray button). Direct YAML edits:
  - **Part A — stray unlabeled "Button" on Victory panel (the reported bug)**:
    - `955836958` (`Text (TMP)` child of `VictoryRetryButton`, line ~1694): `m_text`
      `Button` -> `Retry` (was the unedited Unity default placeholder label — the
      button itself was always correctly wired via
      `BattleResultsUI.victoryRetryButton: {fileID: 526299551}` and
      `SetupButtons()` -> `RetryBattle()`, only the label was wrong).
    - `526299554` (`VictoryRetryButton` RectTransform, line ~1084-1086): was
      dead-center (`m_AnchorMin/Max: {x:0.5,y:0.5}`, `m_AnchoredPosition: {x:0,y:0}`),
      directly overlapping the refactor's new `VictoryRewardsText` (480x320 box at
      x=260,y=-50, spanning x:[20,500] y:[-210,110]). Moved to bottom-right corner,
      mirroring `ReturnToFarmButton`'s bottom-left placement:
      `m_AnchorMin/Max: {x:1,y:0}` (was `{x:0.5,y:0.5}`),
      `m_AnchoredPosition: {x:-80,y:15}` (was `{x:0,y:0}`), `m_SizeDelta` unchanged
      (160x30). New button spans x:[800,960] y:[-540,-510] in panel-center-relative
      coordinates (1920x1080 reference resolution) — verified no overlap with
      `VictoryStatsText` (x:[-500,-20] y:[-210,110]), `VictoryRewardsText`
      (x:[20,500] y:[-210,110]), or `ReturnToFarmButton`
      (x:[-960,-800] y:[-540,-510]).
    - Checked the Defeat panel for the same overlap class: `DefeatRetryButton`/
      `DefeatReturnButton` (`1114062463`/`393331997`) were already correctly
      anchored to bottom-left/bottom-right corners (same `{x:80,y:15}`/`{x:-80,y:15}`
      pattern as Victory), well clear of `DefeatStatsText` (centered, 480x320 at
      x=0,y=-50). No stray center button existed on the Defeat panel — no
      reposition needed there.
  - **Part B — "To Farm"/"Retry" not transitioning scenes**:
    - Confirmed both `SampleScene` and `CombatScene` are present and `enabled: 1` in
      `ProjectSettings/EditorBuildSettings.asset` — not a missing-scene issue.
    - Confirmed via `grep -rn "Application.Quit" Assets/Scripts/` — only hits are in
      `GameMenuManager.cs:484`, `MainMenuManager.cs:245`, `MainMenuUI.cs:712` (main
      menu/pause-menu quit buttons), none reachable from `BattleResultsUI`. Also
      confirmed every `m_OnClick.m_PersistentCalls.m_Calls` in `CombatScene.unity`
      is `[]` (grep on `m_Calls:` across the whole file) — no stale Inspector-wired
      calls anywhere, all button wiring is code-only via `SetupButtons()`.
    - **Found the real Part B bug on the Defeat panel**: `BattleResultsUI`'s
      `defeatReturnButton`/`defeatRetryButton` fields (MonoBehaviour `872830841`,
      line ~1425-1426) were **swapped relative to their on-screen labels**:
      - `defeatRetryButton` pointed at `{fileID: 1114062460}`, the Button on
        GameObject `1114062459` ("ReturnToFarmButton", bottom-left, child text
        `2063833796` = `"To Farm"`). So clicking the button labeled **"To Farm"**
        on the Defeat screen actually ran `RetryBattle()` -> reloads CombatScene
        (matches the user's "buggy button just stops/resets the simulation"
        complaint, applied to the Defeat screen).
      - `defeatReturnButton` pointed at `{fileID: 393332000}`, the Button on
        GameObject `393331996` ("RetryBattleButton", bottom-right, child text
        `1826602260` = `"Retry"`). So clicking the button labeled **"Retry"**
        actually ran `ReturnToFarm()` -> loads SampleScene.
    - **Fix**: swapped the two field assignments only (no text/position/GameObject
      changes needed — labels and positions were already correct and consistent
      with the Victory panel's "To Farm" bottom-left / "Retry" bottom-right
      layout): `defeatReturnButton: {fileID: 393332000}` -> `{fileID: 1114062460}`,
      `defeatRetryButton: {fileID: 1114062460}` -> `{fileID: 393332000}`. Now
      "To Farm" (bottom-left, `1114062460`) -> `ReturnToFarm()` and "Retry"
      (bottom-right, `393332000`) -> `RetryBattle()`, matching both the Victory
      panel's wiring pattern and the visible labels.
    - Also fixed a stale `m_EditorClassIdentifier: TMPro.TextMeshProUGUI` ->
      `UnityEngine.UI.Button` on MonoBehaviour `1114062460` (cosmetic
      Editor-Inspector-only field; the actual component type is the `Button` script
      via `m_Script` guid `4e29b1a8efbd4b44bb3f3716e73f07ff`, unaffected either way —
      fixed for consistency since `393332000`, the equivalent Button on the other
      Defeat button, already had the correct identifier).
    - Victory panel's button wiring/labels were already internally consistent
      (`victoryReturnButton` -> "To Farm" bottom-left, `victoryRetryButton` -> now
      "Retry" bottom-right after the Part A relabel) — no Part B issue on Victory.
    - Could not reproduce "game closes entirely" directly (no Editor/Play-mode
      access this session). Best-supported explanation: the Defeat-panel "To Farm"
      button silently reloading CombatScene (Part B fix above) plus the Victory
      panel's stray center button intercepting clicks meant for
      `VictoryRewardsText`'s area (Part A fix above) together account for "click a
      button, simulation just stops/resets, no scene change" on both result
      screens. Recommend a manual Editor smoke-test of both Victory and Defeat
      outcomes, clicking both buttons on each, to confirm `SampleScene`/
      `CombatScene` now load correctly.
  - Validation: extracted all `--- !u!<id> &<fileID>` headers via grep + a small
    Python regex pass (no `pyyaml` available) — **130 unique fileIDs, no
    duplicates**, same count as before this session's edits (no objects
    added/removed, only field values changed). File remains LF-only
    (`grep -c $'\r'` = 0); pre-existing UTF-8 BOM on line 1 (present before this
    session's edits, from the prior Combat UI refactor pass) left untouched.
- 2026-06-13: Investigated "Combat doesn't load in WebGL build (works in Editor)" -
  build shows `Turn: 1/30, Your Team: 0/0, Enemies: 0/0` and a stuck/empty battle;
  Editor shows `Turn: 0/500, Your Team: 1/1, Enemies: 1/1`. Code-only investigation
  (no `.unity` files touched, per task constraints).
  - **Root cause of the "1/30, 0/0, 0/0" text - CONFIRMED**: these are the
    design-time placeholder strings baked into `CombatScene.unity`'s TopBar TMP
    texts by `Assets/Scripts/Editor/CombatSceneSetup.cs:88-90`
    (`"Turn: 1/30"`, `"Your Team: 0/0"`, `"Enemies: 0/0"`). They are only
    overwritten by `BattleStatusUI.UpdateAll()`, called once from
    `TurnManager.InitializeCombat()` (`Assets/Scripts/Combat/TurnManager.cs:134-141`).
    The Editor's `Turn: 0/500` is the real post-init state (`currentTurn=0`,
    `maxActions=500` default at `TurnManager.cs:32`) - "500" never appeared in the
    build because `InitializeCombat()` was hitting its `allUnits.Count == 0`
    early-return (old code) before calling `UpdateAll()`, leaving the placeholders
    on screen. This fully explains the "1/30 vs 0/500" discrepancy flagged in the
    bug report - it is two different UI states (never-initialized vs initialized),
    not a numeric/config mismatch.
  - **Remaining question - NOT yet definitively confirmed**: *why*
    `GridManager.GetAllUnits().Count == 0` in the build, i.e. why
    `CombatTeamSpawner`/`EnemySpawner` produce zero placed units there. Ranked
    hypotheses (most to least likely), each now both mitigated AND instrumented:
    1. **Silent exception during a per-unit/per-enemy spawn** silently aborting the
       `foreach` loop (IL2CPP-only failure, e.g. reflection-based stat injection on
       `Animal` fields without `[SerializeField]` being stripped, or a null
       `Shader.Find` result). Mitigated with try/catch around every per-unit spawn
       call; any such exception now logs `[CombatTeamSpawner] Exception spawning
       '<name>': <ex>` or `[EnemySpawner] Exception spawning '<name>' at <pos>: <ex>`.
    2. **Timing/race condition**: WebGL's first-frame load is slower than the
       Editor, so the fixed `Invoke` delays (CombatTeamSpawner 0.5s, EnemySpawner
       0.6s, TurnManager.InitializeCombat 1.0s) might not hold their relative order,
       or `Time.timeScale` could still be 0 when `Start()` runs (which would
       permanently block all three `Invoke`s, since `Invoke` delays are scaled by
       `Time.timeScale`). Mitigated: all three `Start()` methods now detect
       `Time.timeScale == 0f`, log an error, and force it to `1f`
       (`CombatTeamSpawner.cs:54-58`, `EnemySpawner.cs:39-43`,
       `TurnManager.cs:85-89`). Additionally, `TurnManager.InitializeCombat()`
       (`TurnManager.cs:112-126`) now retries up to 5 times at 0.5s intervals
       instead of giving up immediately on `allUnits.Count == 0`, covering any
       residual spawn-order slack.
    3. **`TeamAssemblerData.Instance.team` empty / PlayerPrefs round-trip failing**
       in the build (e.g. a fresh `TeamAssemblerData` singleton instance created on
       `CombatScene` load with an empty `team` list, and `LoadFromPrefs()` not
       restoring it). Instrumented: `CombatTeamSpawner.SpawnPlayerTeam()`
       (`CombatTeamSpawner.cs:83-94`) now logs the singleton's `GetInstanceID()`,
       initial `team.Count`, `selectedStageName`, and whether
       `PlayerPrefs.HasKey("Combat_TeamCount")`/its value, then calls
       `LoadFromPrefs()` if the team is empty and logs the team size again
       afterward. Similarly `EnemySpawner.SpawnEnemies()`
       (`EnemySpawner.cs:58-75`) logs `StageManager.GetSelectedStage()`'s result,
       and on null falls back to `TeamAssemblerData.Instance.selectedStageName` +
       `StageManager.GetStageByName()`, logging each step.
  - **Ruled out** (confirmed via code/config inspection, not just assumption):
    - `Resources.LoadAll<StageData>("Stages")` path casing -
      `Assets/Resources/Stages/{Cave,Forest,Mountain,Volcano}/...` folder names and
      casing match `StageManager.LoadAllStages()`'s `"Stages"` argument exactly.
    - `CombatScene` missing from Build Settings - confirmed present (index 2, guid
      `8123d2775d645ea438ba40e24f9543ec`) in `ProjectSettings/EditorBuildSettings.asset`.
    - `Time.timeScale = 1f` missing before `SceneManager.LoadScene("CombatScene")` -
      already present in `TeamAssemblerUI.OnStartBattleClicked` (pre-existing fix
      from commit `16028e2`, predates Build #6); added a confirming log line at
      `TeamAssemblerUI.cs` (`OnStartBattleClicked`) showing `teamSize`,
      `selectedStageName`, and the post-assignment `Time.timeScale` value right
      before the scene load, to cross-check against what the spawners see after
      load.
    - `Sprites/Default` shader stripped from WebGL build - confirmed present in
      `m_AlwaysIncludedShaders` in `ProjectSettings/GraphicsSettings.asset`
      (used by `CombatUnit.CreateSpriteVisual()`).
  - **Files changed** (diagnostics + defensive fixes, all `.cs`, no `.unity` edits):
    - `Assets/Scripts/Combat/TurnManager.cs` - `Start()` (lines 81-92) and
      `InitializeCombat()` (lines 97-149ish) rewritten with timing diagnostics,
      `Time.timeScale==0` auto-fix, and a 5x/0.5s retry loop on
      `allUnits.Count == 0` instead of an immediate silent return.
    - `Assets/Scripts/Combat/CombatTeamSpawner.cs` - `Awake()`/`OnDestroy()`/`Start()`
      (lines 38-63) gained `Time.timeScale`/`Time.time` logging and the same
      `Time.timeScale==0` auto-fix; `SpawnPlayerTeam()` (lines 81-94) gained
      `TeamAssemblerData`/PlayerPrefs diagnostic logging + `LoadFromPrefs()` retry
      when the team is empty; the per-unit spawn call inside the team-building
      `foreach` loop and the `SpawnFallbackUnit()` call are now wrapped in
      try/catch with `Debug.LogError`.
    - `Assets/Scripts/Combat/EnemySpawner.cs` - `Start()` (lines 35-46) gained the
      same timing/`Time.timeScale` diagnostics+autofix; `SpawnEnemies()`
      (lines 48-108) now logs `StageManager.GetSelectedStage()`, and on null logs
      `TeamAssemblerData.selectedStageName` + `StageManager.GetTotalStages()` and
      whether `GetStageByName(savedName)` succeeded before retrying; `SpawnFromStage`
      /`SpawnBackground`/`SpawnFallbackEnemies` calls each wrapped in try/catch;
      `SpawnFromStage()` (lines 137-172) additionally logs `pool.Count` and wraps
      each per-enemy `SpawnEnemy()` call in the pool-iteration `foreach` loop in
      try/catch.
    - `Assets/Scripts/Combat/TeamAssemblerUI.cs` - `OnStartBattleClicked()` gained a
      log line immediately after `SaveToPrefs()`/`Time.timeScale = 1f`, showing
      `teamSize`, `selectedStageName`, and the resulting `Time.timeScale` right
      before `SceneManager.LoadScene("CombatScene")`.
  - **What to check in the next WebGL build's browser console**: filter for the
    `[TeamAssembler]`, `[CombatTeamSpawner]`, `[EnemySpawner]`, and `[TurnManager]`
    prefixes, in that rough chronological order. Specifically:
    - Does `[TeamAssembler] OnStartBattleClicked` show a non-zero `teamSize` and a
      non-empty `selectedStage`?
    - Does `[CombatTeamSpawner] SpawnPlayerTeam()` show a non-zero initial
      `team.Count` (or does `LoadFromPrefs()` need to kick in - check the
      "after LoadFromPrefs, team size" follow-up line)?
    - Does `[EnemySpawner] StageManager.GetSelectedStage()` return a stage name, or
      `"null"` - and if null, does the `TeamAssemblerData.selectedStageName`
      fallback succeed?
    - Are there any `[CombatTeamSpawner] Exception spawning ...` or
      `[EnemySpawner] Exception spawning ... / Exception in SpawnFromStage(): ...`
      lines - these would be the smoking gun for hypothesis 1.
    - Does any `Start()` log show `Time.timeScale=0` (hypothesis 2 confirmed if so -
      now auto-corrected, but worth knowing why it was 0)?
    - Does `[TurnManager] InitializeCombat` eventually report a non-zero unit count
      (possibly after 1-2 retries), and does `BattleStatusUI.Instance` resolve
      (no `"BattleStatusUI.Instance is null"` error)?
  - No files committed - per task instructions, only `.cs` files and this
    `PROGRESS.md` entry were edited; no `.unity` scene files touched.
- 2026-06-13: Fixed two UI bugs in `SampleScene.unity`.
  - **World Map only showing "Sunny Fields"**: `WorldMapUIController.biomePanels`
    (the intended per-biome panel list) is empty in the scene, so
    `RefreshBiomePanels()` previously early-returned and only the one hardcoded
    `StageButton_SunnyFields` was ever visible. `WorldMapUiController.cs` now
    always computes the full `theme -> List<StageData>` grouping (25 stages across
    Meadow/Forest/Cave/Mountain/Volcano from `Resources.LoadAll<StageData>("Stages")`)
    and, when `biomePanels` is empty, calls a new fallback
    `RefreshFlatStageButtons()` that clones `StageButton_SunnyFields` (found via
    `GetComponentInChildren<StageButton>(true)`, or an optional new
    `flatLayoutTemplate` field) once per stage, arranges them in a 5-row
    (one row per biome theme, alphabetical) x 5-column grid under `MapImage`, sets
    each clone's `stageName`/`WorldMap`/label text, and re-triggers
    `StageButton.OnEnable()` (toggle inactive/active) so lock-state visuals match
    the new `stageName` instead of the template's stale "Sunny Fields". Previously
    spawned clones (prefixed `StageButton_Generated_`) are destroyed and
    regenerated on every `OpenWindow()`. New serialized fields on
    `WorldMapUIController`: `flatLayoutTemplate`, `flatButtonCellSize` (140x60),
    `flatButtonSpacing` (20x20), `flatLayoutOrigin` (40,-40) - all optional with
    sensible defaults, no Unity wiring required for the fallback to work. If
    `biomePanels` is populated later with real `WorldMapBiomePanel`s, the new
    per-stage grouping/sorting is reused and the fallback is skipped entirely.
    File: `Assets/Scripts/Worldmap/WorldMapUiController.cs` (`RefreshBiomePanels()`
    rewritten, `RefreshFlatStageButtons()`, `ConfigureFlatStageButton()`,
    `ResolveFlatLayoutTemplate()` added).
  - Also removed a dead "Missing Script" `MonoBehaviour` (fileID `1771585767`,
    script guid `bb6c8d87be1af622d8a5e305d0c4c011` - no corresponding `.cs`/`.meta`
    anywhere in the project) from the `WorldMap` GameObject's `m_Component` list in
    `SampleScene.unity` (fileID `1771585762`). The real `WorldMapUIController`
    (fileID `1771585768`, guid `627deec244b56383f89bf4cf187aa68d`, matches
    `WorldMapUiController.cs.meta`) is untouched. Scene object count: 824 -> 823,
    all fileIDs still unique (`grep -c "^--- !u!"` == unique `&<number>` count).
  - **Team Assembler "Available Animals" card overlap**: in
    `Assets/Prefabs/AnimalCard.prefab`, the `HappinessText` (fileID
    `6371327513841963786`) and `FoodStatusText` (fileID `8559207285585469064`)
    RectTransforms were byte-identical (`anchorMin/Max={0.5,0.5}`,
    `anchoredPosition={0,0}`, `sizeDelta={180,25}`, `pivot={0.5,0.5}`), so
    "Happiness: NN%" and the food-requirement text ("Needs: 1x CarrotSeed" /
    "Fed") rendered centered on top of each other - this is the overlap visible
    in the build screenshots. Repositioned into two non-overlapping rows in the
    card's right column (card is 380x120, portrait occupies the left 140px):
    `HappinessText` -> `anchoredPosition={35,10}`, `sizeDelta={220,25}`;
    `FoodStatusText` -> `anchoredPosition={35,-20}`, `sizeDelta={220,25}`.
    `NameText` (top strip, fileID `1891834458420850715`) was already correctly
    positioned and untouched. Also fixed the stale `AnimalSelectionCard`
    MonoBehaviour (fileID `5556013670678334846`) serialized fields: renamed
    `selectedColor` -> `hoverColor` (preserving its `{1,1,0.5,1}` value, matching
    the rename already applied to `TeamAssemblerUISetup.cs` in the prior
    2026-06-13 entry) and added the previously-missing
    `foodStatusIcon`/`happinessFillBar` (null) and color fields
    (`fedColor`/`hungryColor`/`notInTeamColor`/`happinessLowColor`/
    `happinessMidColor`/`happinessHighColor`) with values matching the C# defaults
    in `AnimalSelectionCard.cs`, so the Inspector now reflects what the code
    actually uses.
  - Investigated "Available Animals panel empty": confirmed one `Animal`
    (Clucky, via `Assets/Prefabs/Clucky.prefab` PrefabInstance fileID
    `3024074362188938321`) is placed active under the root "Animals" GameObject
    (fileID `2082245845`, `m_IsActive: 1`) with no `m_IsActive` override in its
    modifications - `FindObjectsByType<Animal>()` should find it normally. No
    code/scene change made for this sub-issue; the reported "empty" panel is most
    likely explained by the HappinessText/FoodStatusText overlap above making the
    single Clucky card look like unreadable overlapping boxes rather than a
    recognizable animal card. If the panel is still empty after this fix, the
    next suspect is `AnimalCard.prefab`'s fixed `sizeDelta={380,120}` vs the
    `AnimalSelectionPanel`'s ~40%-width viewport (potential horizontal clip since
    the `Content` VerticalLayoutGroup has `m_ChildControlWidth: 0`).
  - Files changed: `Assets/Scripts/Worldmap/WorldMapUiController.cs`,
    `Assets/Scenes/SampleScene.unity`, `Assets/Prefabs/AnimalCard.prefab`. No
    commits made.
