# Worklist — Sowur Shield Code Review

> Ordered, dependency-aware queue of atomic tasks. Critical first. Each task is sized to fit in
> a few minutes of focused work — losing a session mid-task costs at most one task. Pick the
> first unmarked task whose dependencies are all `[x]`.

---

## TASK-001 — Confirm CombatScene pipeline fix with a smoke-test play-through
- [ ] status
- priority: Critical
- system: combat
- files: `Assets/Scenes/CombatScene.unity`, `Assets/Scenes/SampleScene.unity`
- problem: COMBAT_PIPELINE_STATUS.md documented a bug where `SpawnTeams()` never fired via the
  TeamAssembler flow due to 24 empty `m_EditorClassIdentifier` entries in CombatScene.unity. A
  static grep of the current CombatScene.unity shows **0** empty ECI entries (`grep -c
  "m_EditorClassIdentifier: $"` returns 0), and 4 commits postdating that doc claim to fix
  related issues (timeScale=0 blocking Invoke, enemy sprites, turn speed, "save CombatScene with
  combat pipeline setup"). This strongly suggests the bug is fixed, but no one has confirmed it
  with an actual play-through since.
- what to do:
  1. Open the project in Unity Editor.
  2. Play from MainMenu → New Game → SampleScene.
  3. Walk into the `CombatTriggerZone`, open TeamAssembler, select at least 1 animal, click
     "Start Battle".
  4. Confirm CombatScene loads, player unit(s) spawn on columns 6-8, enemy unit(s) spawn on
     columns 0-5, and the turn loop begins (gauges fill, units act).
  5. Play to victory or defeat once, confirm BattleResultsUI shows and "Return to Farm" works.
- change sketch: none — this is a verification task, no code changes expected. If everything
  works, the only output is updating PROGRESS.md and (optionally) deleting
  `COMBAT_PIPELINE_STATUS.md` since its content is now folded into 01_ARCHITECTURE.md /
  02_FINDINGS.md (C3) and `SOWUR_SHIELD_STATUS.md` (see TASK-014).
- tests: manual play-through as described above.
- done criteria: a full combat encounter (spawn → turns → victory/defeat → return to farm)
  completes without errors in the Console.
- edge cases / risks: if the bug is NOT fixed, do not attempt to fix it as part of this task —
  instead, append a new TASK-0XX to this file describing the exact symptom (what step failed,
  what the Console shows) so it can be triaged separately. Do not block other tasks on this.
- depends on: none

---

## TASK-002 — Decide and document the fate of AnimalData.combatClass / availablePassiveSkills
- [ ] status
- priority: Critical
- system: combat / animals (scope decision)
- files: `Assets/Scripts/Animals/AnimalData.cs`, `PRD_Animals_Combat_System.md`,
  `Assets/Scripts/Combat/CombatUnit.cs`, `Assets/Scripts/Combat/TurnManager.cs`
- problem: `AnimalData.combatClass` (Tank/DPS/Support/Utility enum) and
  `availablePassiveSkills[]` are populated ScriptableObject fields with zero readers anywhere in
  `Assets/Scripts/Combat/`. The PRD describes an elaborate "3-Passive System" (Family + Class +
  Happiness passives, plus Combo Synergies) as the core combat mechanic, but the shipped combat
  system (status effects + XP/leveling) never implements it. This is a scope decision, not a
  bug — but it needs to be MADE, not left ambiguous.
- what to do: this task is a DECISION + DOCUMENTATION task, not an implementation task (the
  implementation, if chosen, becomes TASK-003 which depends on this one).
  1. Read `PRD_Animals_Combat_System.md` section on the 3-Passive System (Family/Class/Happiness
     passives, Combo Synergies) — already summarized in `/review/01_ARCHITECTURE.md` Section 10
     and `/review/02_FINDINGS.md` C2.
  2. Decide: (A) descope — mark PRD as historical/aspirational and leave `combatClass`/
     `availablePassiveSkills` as unused-for-now fields (add a `// Not currently read by combat
     system — see PRD history` comment on each field); or (B) implement a minimal version
     (recommended minimal scope: Class passive only, using the existing `combatClass` field,
     e.g. "3+ units of the same combatClass in the active team grant +X% to one stat").
  3. Whichever is chosen, add a short "Combat Scope" section to whatever consolidated status doc
     replaces ROADMAP.md (see TASK-014) stating the decision and why.
- change sketch:
  - If (A): add a one-line comment above `combatClass` and `availablePassiveSkills` declarations
    in AnimalData.cs noting they are currently unused by the combat system.
  - If (B): no code change in this task — TASK-003 (depends on this) implements it.
- tests: none for this task (decision + doc only).
- done criteria: the consolidated status doc has an explicit "Combat Scope" statement, and if
  (A) was chosen, AnimalData.cs has the clarifying comments.
- edge cases / risks: if (B) is chosen but TASK-003 is never picked up, the comment-vs-no-comment
  difference is cosmetic — either way the fields stay inert until implemented. Don't let this
  decision block unrelated combat tasks (TASK-005+).
- depends on: none

---

## TASK-003 — (CONDITIONAL) Implement minimal Class-passive synergy bonus
- [ ] status
- priority: Important
- system: combat
- files: `Assets/Scripts/Combat/CombatTeamSpawner.cs`, `Assets/Scripts/Animals/AnimalData.cs`,
  `Assets/Scripts/Combat/CombatUnit.cs`
- problem: see TASK-002. This task only applies if TASK-002 chose option (B) — implement a
  minimal Class-passive synergy. If TASK-002 chose (A), mark this task `[x]` with result "N/A —
  descoped per TASK-002" in PROGRESS.md and move on.
- what to do (only if applicable):
  1. In `CombatTeamSpawner.SpawnPlayerTeam()` (around `CombatTeamSpawner.cs:73-115`), after all
     player `Animal` references are collected but before `CreateAnimalUnit()` is called for
     each, count occurrences of each `AnimalData.combatClass` value across the team.
  2. For any `combatClass` with count >= 3, apply a small flat multiplier (e.g., +10%) to the
     relevant stat for all units of that class: Tank → defense, DPS → attack, Support → no
     direct combat stat (skip for minimal version), Utility → speed.
  3. Apply via the existing `ApplyStatMultiplier()` pattern on `CombatUnit`
     (`CombatUnit.cs:560-567`), called once per affected unit during spawn, same place illness
     penalty is applied (`CombatTeamSpawner.cs:254-256`).
- change sketch:
  ```csharp
  // In CombatTeamSpawner.SpawnPlayerTeam(), after building the list of (Animal, CombatUnit) pairs:
  var classCounts = team.GroupBy(a => a.AnimalData.combatClass)
                         .ToDictionary(g => g.Key, g => g.Count());

  foreach (var (animal, unit) in spawnedUnits)
  {
      if (classCounts.TryGetValue(animal.AnimalData.combatClass, out int count) && count >= 3)
      {
          switch (animal.AnimalData.combatClass)
          {
              case CombatClass.Tank:    unit.ApplyStatMultiplier(defenseMult: 1.1f); break;
              case CombatClass.DPS:     unit.ApplyStatMultiplier(attackMult: 1.1f); break;
              case CombatClass.Utility: unit.ApplyStatMultiplier(speedMult: 1.1f); break;
          }
      }
  }
  ```
  Note: `ApplyStatMultiplier()` currently takes a single scalar applied to attack/defense/health
  (`CombatUnit.cs:560-567`) — it will need an overload or parameter change to target individual
  stats. Keep the change additive (new overload), don't break the illness-penalty call site.
- tests: add `CombatTeamSpawnerTests` (new EditMode test file) with a test that spawns 3 Tank
  animals and asserts each has +10% defense vs. a baseline spawn of 2 Tanks (no bonus).
- done criteria: compiles; new test passes; existing CombatPhase1Tests still pass (illness
  penalty path unaffected).
- edge cases / risks: don't apply the bonus to enemy units (EnemySpawner path is separate and
  untouched). Watch for double-application if `SpawnPlayerTeam()` is ever called twice in one
  session (TeamAssemblerData persistence) — guard with a "synergy already applied" flag on
  CombatUnit if needed.
- depends on: TASK-002

---

## TASK-004 — Implement save migration scaffolding (GameData.saveVersion-based dispatch)
- [ ] status
- priority: Critical
- system: save
- files: `Assets/Scripts/SaveManager.cs:393-397`, `Assets/Scripts/GameData.cs`
- problem: `MigrateSave(GameData data)` at `SaveManager.cs:393-397` only stamps
  `data.saveVersion = GameData.CURRENT_SAVE_VERSION` — it does not run any version-specific
  transformation. There is currently no need for a transformation (no schema change has shipped
  that requires one), but the DISPATCH MECHANISM should exist now so that the NEXT schema change
  has somewhere to plug in.
- what to do:
  1. Read `GameData.cs` to find `CURRENT_SAVE_VERSION` and `saveVersion` field definitions.
  2. In `MigrateSave()`, replace the single-line stamp with a loop/switch that walks from
     `data.saveVersion` to `CURRENT_SAVE_VERSION`, calling a per-version migration method for
     each step (even if, today, every method body is empty / a no-op).
  3. Add a log line (LogWarning, not Log) when a migration step runs, so a future developer can
     see in the console that migration occurred.
- change sketch:
  ```csharp
  private GameData MigrateSave(GameData data)
  {
      int fromVersion = data.saveVersion;
      while (data.saveVersion < GameData.CURRENT_SAVE_VERSION)
      {
          switch (data.saveVersion)
          {
              // case 1: MigrateV1ToV2(data); break; // example for the future
              default:
                  break; // no-op for versions with no transformation needed
          }
          data.saveVersion++;
      }
      if (fromVersion != data.saveVersion)
          Debug.LogWarning($"[SaveManager] Migrated save from v{fromVersion} to v{data.saveVersion}");
      return data;
  }
  ```
- tests: extend `GameDataTests.cs` with a test that constructs a `GameData` with
  `saveVersion = CURRENT_SAVE_VERSION - 1` (or 0), runs it through `MigrateSave()`, and asserts
  `saveVersion == CURRENT_SAVE_VERSION` afterward. If `CURRENT_SAVE_VERSION` is currently 1 (no
  prior versions), use a temporary local constant in the test to simulate "version 0 → 1" so the
  loop logic itself is exercised.
- done criteria: compiles; new GameDataTests test passes; existing 35 GameDataTests tests still
  pass; loading an existing save file from `Saves/` still works (manual check: load AutoSave in
  Editor, confirm no errors).
- edge cases / risks: do not change `CURRENT_SAVE_VERSION`'s current value as part of this task
  — that would force every existing save through the (currently empty) migration path on next
  load, which is fine functionally but should be a deliberate separate decision tied to an actual
  schema change, not bundled here.
- depends on: none

---

## TASK-005 — Cache PlayerStats/Inventory references in QuestManager instead of FindFirstObjectByType in GrantRewards
- [ ] status
- priority: Important
- system: dialogue/quests
- files: `Assets/Scripts/Dialogue/QuestManager.cs:19-60` (Awake/Start), `:267-290` (GrantRewards)
- problem: `GrantRewards(QuestData data)` calls `Object.FindFirstObjectByType<PlayerStats>()`
  (line 272) and `Object.FindFirstObjectByType<Inventory.Inventory>()` (line 279) every time a
  quest completes. If either returns null (e.g., scene transition timing), the reward (gold or
  items) is silently dropped — `stats?.AddMoney(...)` at line 273 uses null-conditional, so no
  error is logged.
- what to do:
  1. Add private fields `private PlayerStats _playerStats;` and
     `private Inventory.Inventory _inventory;` to `QuestManager`.
  2. In `Start()` (or wherever QuestManager already initializes — check near line 47-59),
     populate both via `FindFirstObjectByType` ONCE.
  3. In `GrantRewards()`, replace the two `FindFirstObjectByType` calls with the cached fields.
  4. If a cached field is null when `GrantRewards()` runs, add `Debug.LogWarning($"[QuestManager]
     Cannot grant {rewardType} reward for quest '{data.questId}' — reference not found")` so a
     dropped reward is at least visible in the console.
- change sketch:
  ```csharp
  // fields near top of class
  private PlayerStats _playerStats;
  private Inventory.Inventory _inventory;

  // in Start()
  _playerStats = Object.FindFirstObjectByType<PlayerStats>();
  _inventory = Object.FindFirstObjectByType<Inventory.Inventory>();

  // in GrantRewards()
  if (data.rewardGold > 0)
  {
      if (_playerStats != null) _playerStats.AddMoney(data.rewardGold);
      else Debug.LogWarning($"[QuestManager] Cannot grant gold reward for quest '{data.questId}' — PlayerStats not found");
  }
  if (data.rewardItems != null && data.rewardItems.Count > 0)
  {
      if (_inventory != null) { /* existing foreach using _inventory */ }
      else Debug.LogWarning($"[QuestManager] Cannot grant item rewards for quest '{data.questId}' — Inventory not found");
  }
  ```
- tests: existing `QuestSystemTests.cs` (16 tests) should still pass unchanged. If feasible, add
  one test that completes a quest with `rewardGold > 0` in an EditMode test scene without a
  PlayerStats object present, and assert no exception is thrown (just a warning).
- done criteria: compiles; existing QuestSystemTests pass; manual check — complete a quest with
  gold reward in-game, confirm gold is still added.
- edge cases / risks: QuestManager's `Start()` order relative to PlayerStats/Inventory
  initialization matters — if QuestManager.Start() runs BEFORE PlayerStats exists in the scene,
  the cached reference will be null forever (the original code at least re-checked every time).
  Mitigate by also re-attempting the cache lazily inside `GrantRewards()` if the cached field is
  still null: `if (_playerStats == null) _playerStats = Object.FindFirstObjectByType<PlayerStats>();`
  — this keeps the "retry" behavior for the rare cold-start case while caching for the common case.
- depends on: none

---

## TASK-006 — Audit and resolve UIManager's dual panel systems (OpenPanel/ClosePanel vs TryOpenWindow/TryCloseWindow)
- [ ] status
- priority: Important
- system: UI
- files: `Assets/Scripts/UIManager.cs:13-120` (legacy), `Assets/Scripts/UIManager.cs:24-311`
  (current), `Assets/Scripts/SellBox.cs:429,509`
- problem: `UIManager` has two parallel window-tracking systems. The "legacy" one
  (`allUIPanels`/`currentlyOpenPanel`/`OpenPanel`/`ClosePanel`) is NOT dead — `SellBox.cs:429`
  calls `UIManager.Instance.OpenPanel(sellBoxMainPanel)` and `SellBox.cs:509` calls
  `UIManager.Instance.ClosePanel(sellBoxMainPanel)`, even though `SellBox` ALSO implements
  `IUIWindow` (per 01_ARCHITECTURE.md Section 4) and presumably also calls
  `TryOpenWindow`/`TryCloseWindow`. This means SellBox may be tracked by BOTH systems
  simultaneously, which could cause ESC-key handling (`HandleEscapeKey()`,
  `UIManager.cs:246`-ish, operates on `openWindowStack`) to be unaware that the legacy
  `currentlyOpenPanel` still thinks the SellBox panel is open.
- what to do:
  1. Read `SellBox.cs` around lines 420-435 and 500-515 to see the full context of the
     `OpenPanel`/`ClosePanel` calls — are they ALSO calling `TryOpenWindow`/`TryCloseWindow`
     elsewhere in the same open/close methods?
  2. If SellBox calls BOTH APIs: this is redundant but likely harmless IF `currentlyOpenPanel`
     is never read by `HandleEscapeKey()` or other ESC logic (confirm by checking what reads
     `currentlyOpenPanel` / `IsAnyPanelOpen()` at `UIManager.cs:114`, and whether ESC handling
     uses that or `openWindowStack`).
  3. If the legacy calls are truly redundant (current window-stack system already handles
     SellBox correctly), remove the two `OpenPanel`/`ClosePanel` calls from SellBox.cs and the
     now-fully-dead legacy block from UIManager.cs (lines ~13-120) in one commit.
  4. If removing them breaks something (e.g., some other script reads `IsAnyPanelOpen()` and
     relies on SellBox setting `currentlyOpenPanel`), do NOT remove — instead, document in
     PROGRESS.md why the dual system must stay, and split a follow-up task to migrate that other
     reader.
- change sketch: N/A until investigation in step 1-2 is done — this task is investigation-first,
  removal-second, within the same atomic task (should still fit in a few minutes since it's a
  bounded grep + read).
- tests: after any removal, run the full EditMode suite (`dotnet test` or Unity Test Runner) —
  specifically confirm SellBox-related tests (if any) and a manual check that opening/closing
  SellBox via E-key still works and ESC still closes it.
- done criteria: either (a) legacy panel system removed and all tests pass, or (b) investigation
  documented in PROGRESS.md with a concrete reason it can't be removed yet + a follow-up task
  number for whoever picks it up next.
- edge cases / risks: SellBox is one of the higher-traffic interaction surfaces in the game
  (CLAUDE.md documents 4 prior bug fixes specifically around SellBox interaction). Be
  conservative — if in doubt, choose option (b) and don't risk re-breaking a previously-fixed
  interaction flow.
- depends on: none

---

## TASK-007 — Add DialogueCondition evaluation unit tests
- [ ] status
- priority: Important
- system: dialogue (testability)
- files: new `Assets/Tests/EditMode/DialogueConditionTests.cs`,
  `Assets/Scripts/Dialogue/Core/DialogueCondition.cs`
- problem: zero unit tests exist for `DialogueCondition` evaluation. `ConditionType` includes
  `Always, ConversationCompleted, ChoiceMade, RelationshipLevel, QuestStatus, VariableCheck,
  InventoryItem` and `ConditionOperator` includes `Equals, NotEquals, GreaterThan, LessThan,
  GreaterOrEqual, LessOrEqual, Contains` — a 7×7 matrix of meaningful combinations, none tested.
- what to do:
  1. Read `DialogueCondition.cs` in full to find the evaluation method signature(s) (likely
     something like `bool Evaluate(ConversationMemory memory, Inventory inventory)` based on
     01_ARCHITECTURE.md Section 8).
  2. Look at `NPCRelationshipTests.cs` (10 tests, `Assets/Tests/EditMode/` or similar — confirm
     path) for the existing pattern of constructing a `ConversationMemory` instance / fake for
     testing — reuse that setup pattern.
  3. Write `DialogueConditionTests.cs` with tests for at minimum:
     - `RelationshipLevel` with `GreaterOrEqual`/`LessThan` against a memory with a known
       relationship value.
     - `QuestStatus` with `Equals` against a memory with a known quest status string.
     - `VariableCheck` with `Equals`/`NotEquals` against a custom variable.
     - `InventoryItem` with `GreaterOrEqual` against a fake/real inventory with a known item
       count.
     - `Always` returns true unconditionally.
- change sketch: (test file skeleton)
  ```csharp
  using NUnit.Framework;
  using SowurShield.Dialogue;

  public class DialogueConditionTests
  {
      [Test]
      public void RelationshipLevel_GreaterOrEqual_TrueWhenAboveThreshold()
      {
          var memory = /* construct or reset ConversationMemory singleton */;
          memory.ModifyRelationship("npc1", 50f);

          var condition = new DialogueCondition
          {
              conditionType = ConditionType.RelationshipLevel,
              targetId = "npc1",
              op = ConditionOperator.GreaterOrEqual,
              compareValue = "40"
          };

          Assert.IsTrue(condition.Evaluate(/* args per actual signature */));
      }
      // ... more tests per the matrix above
  }
  ```
  Exact field names (`targetId`, `compareValue`, etc.) must be read from the real
  `DialogueCondition.cs` — this sketch is illustrative, not literal.
- tests: this task IS the test addition. Run the new file via Unity Test Runner (EditMode) and
  confirm all new tests pass.
- done criteria: new test file compiles and all new tests pass; existing 413 tests unaffected.
- edge cases / risks: `ConversationMemory` is a singleton (per 01_ARCHITECTURE.md Section 8) —
  tests must reset its state between runs (look for a `[SetUp]`/`[TearDown]` pattern in
  `NPCRelationshipTests.cs` and replicate it) to avoid test-order-dependent failures.
- depends on: none

---

## TASK-008 — Add DialogueEffect execution unit tests
- [ ] status
- priority: Important
- system: dialogue (testability)
- files: new `Assets/Tests/EditMode/DialogueEffectTests.cs`,
  `Assets/Scripts/Dialogue/Core/DialogueEffect.cs`
- problem: zero unit tests for `DialogueEffect` execution. `EffectType` includes `SetVariable,
  ModifyRelationship, SetQuestStatus, GiveItem, TakeItem, PlaySound, TriggerEvent`. The
  `GiveItem`/`TakeItem` effects in particular touch real `Inventory` via `ItemDatabase` —
  exactly the kind of cross-system glue that's easy to break silently.
- what to do:
  1. Read `DialogueEffect.cs` in full (141 lines per 01_ARCHITECTURE.md) to find the execution
     method signature and the `GiveItemToInventory()`/`TakeItemFromInventory()` implementations
     (lines ~65 and ~87 per the earlier exploration).
  2. Write tests for:
     - `SetVariable` followed by a `VariableCheck` condition reading it back (round-trip).
     - `ModifyRelationship` changes `ConversationMemory.GetRelationshipLevel()` by the expected
       amount, and is clamped to ±100 (per `ConversationData.cs:130`).
     - `SetQuestStatus` updates `ConversationMemory.GetQuestStatus()`.
     - `GiveItem` with a valid item name adds the item to a real/fake `Inventory`; with an
       INVALID item name (not in ItemDatabase), does not throw and logs a warning (mirror the
       pattern from `QuestManager.GrantRewards` at line 287-288).
     - `TakeItem` removes an item if present; if the item isn't present, does not throw (decide
       expected behavior by reading the implementation — document what it actually does if
       different from "no-op").
- change sketch: same structural pattern as TASK-007 — read real signatures first, write tests
  matching them. No illustrative code provided here since `DialogueEffect.cs` hasn't been read
  yet in this review pass; reading it is step 1 of this task.
- tests: this task IS the test addition.
- done criteria: new test file compiles, all new tests pass, existing 413 tests unaffected.
- edge cases / risks: same `ConversationMemory` singleton reset concern as TASK-007 — if both
  tasks are done in the same session, consider sharing a test-utility helper for resetting
  memory state (but don't over-engineer; a copy-pasted `[SetUp]` is fine for now).
- depends on: none (independent of TASK-007, but if both are picked up, doing TASK-007 first
  means the singleton-reset pattern is already proven)

---

## TASK-009 — Clean up dead SellBox branch in InteractionManager.SetInteractablePromptVisibility
- [ ] status
- priority: Polish
- system: interaction
- files: `Assets/Scripts/InteractionManager.cs:166-177`
- problem: `SetInteractablePromptVisibility()` has:
  ```csharp
  if (interactable is SellBox sellBox)
  {
      // SellBox doesn't have a prompt, but we could add one in the future
  }
  ```
  The `sellBox` variable is declared and unused; the branch is a no-op.
- what to do: either (a) delete the entire `if (interactable is SellBox sellBox) { ... }` block
  (3 lines + comment), since `NPCDialogueInteractable` is the only type that currently needs
  prompt-visibility handling and the method works fine without an explicit "do nothing for
  SellBox" branch; or (b) if there's a near-term plan to add a SellBox prompt, change to
  `if (interactable is SellBox)` (drop unused variable) and leave the comment.
- change sketch:
  ```csharp
  // before
  if (interactable is NPCDialogueInteractable npc)
  {
      npc.SetPromptVisibility(visible);
  }

  if (interactable is SellBox sellBox)
  {
      // SellBox doesn't have a prompt, but we could add one in the future
  }

  // after (option a — recommended)
  if (interactable is NPCDialogueInteractable npc)
  {
      npc.SetPromptVisibility(visible);
  }
  ```
- tests: none needed — pure dead-code removal. Compile check is sufficient.
- done criteria: compiles; no behavior change (verified by the fact that the removed branch was
  a no-op).
- edge cases / risks: none — this is the lowest-risk task in the entire worklist.
- depends on: none

---

## TASK-010 — Fix CLAUDE.md folder table to reflect actual Farming script locations
- [ ] status
- priority: Polish
- system: documentation
- files: `CLAUDE.md` (Project Structure section near top)
- problem: CLAUDE.md's folder table implies a `Scripts/Farming/` folder containing
  `SoilBlockInteractable.cs, CropData.cs, CropGrowthManager.cs`. In reality these files (plus
  `FarmBuildingManager.cs`, `FarmBuildingData.cs`, `WeatherController.cs`) live in `Scripts/`
  root with `SowurShield.Core` namespace. Only `Scripts/DualGridTilemap/` (CursorController,
  DualGridTilemap) actually uses `SowurShield.Farming`.
- what to do: edit the "Project Structure" tree in CLAUDE.md so the farming-related files are
  listed under the root `Scripts/` Core Systems section (or a clearly-marked
  "Farming (lives in Scripts/ root, SowurShield.Core)" sub-note), and keep `DualGridTilemap/` as
  the only `SowurShield.Farming` entry. Do not move any actual files or change any namespaces —
  this is a documentation-only correction.
- change sketch: in the existing tree, move `SoilBlockInteractable.cs, CropData.cs,
  CropGrowthManager.cs` (and optionally `FarmBuildingManager.cs`, `FarmBuildingData.cs`,
  `WeatherController.cs` if they're listed elsewhere incorrectly too) out of any implied
  `Farming/` heading and into the `Scripts/` root listing, e.g.:
  ```
  Assets/Scripts/
  ├── Core Systems/       PlayerMove.cs, InteractionManager.cs, UIManager.cs, ...
  ├── Farming (root, SowurShield.Core): SoilBlockInteractable.cs, CropData.cs, CropGrowthManager.cs,
  │                       FarmBuildingManager.cs, FarmBuildingData.cs, WeatherController.cs
  │   DualGridTilemap/    DualGridTilemap.cs, CursorController.cs  (SowurShield.Farming)
  ```
- tests: none — documentation only.
- done criteria: CLAUDE.md tree accurately reflects `Assets/Scripts/` layout for farming-related
  files (spot-check with a `Glob` for the listed files' actual paths).
- edge cases / risks: none. Keep the edit minimal — do not restructure the entire CLAUDE.md tree,
  only the farming-related rows.
- depends on: none

---

## TASK-011 — Extract AnimalIllness logic from Animal.cs into a focused component
- [ ] status
- priority: Important
- system: animals
- files: `Assets/Scripts/Animals/Animal.cs` (975 lines; illness logic around lines 900-917 per
  01_ARCHITECTURE.md, plus `illnessThresholdDays`/`illnessCureItemName`/`illnessStatPenalty`
  fields read from `AnimalData`), new `Assets/Scripts/Animals/AnimalIllness.cs`
- problem: `Animal.cs` at 975 lines is the 2nd-largest script in the project and handles 6
  distinct concerns (interaction, happiness, production, illness, XP/leveling, seasonal
  modifiers, save/load). Illness (`UpdateNeglectAndIllness()`, neglect-day tracking, ill-state
  flag, cure-on-medicine-feed) is one of the more self-contained sub-systems — a good first
  candidate for extraction since `AnimalHusbandryTests.cs` already has dedicated illness test
  cases that can validate the extraction didn't change behavior.
- what to do:
  1. Read `Animal.cs` in full, identify every field/method related to illness: neglect-day
     counter, `IsIll` flag, `UpdateNeglectAndIllness()`, the cure check (medicine item feeding),
     and the `illnessStatPenalty` application point (`ApplyStatMultiplier` call in
     `CombatTeamSpawner.cs:254-256` reads `animal.IsIll` — this cross-reference must keep
     working).
  2. Create `Assets/Scripts/Animals/AnimalIllness.cs` (namespace `SowurShield.Animals`) as a
     plain C# class (NOT a MonoBehaviour — it doesn't need its own GameObject lifecycle) that
     owns: neglect-day counter, `IsIll` property, `UpdateNeglect(bool wasCaredForToday)`,
     `TryCure(string fedItemName)`.
  3. In `Animal.cs`, replace the inline illness fields/logic with a `private AnimalIllness
     illness = new AnimalIllness(...)` instance, delegating `IsIll` to `illness.IsIll`, and
     calling `illness.UpdateNeglect(...)`/`illness.TryCure(...)` from the existing call sites
     (`UpdateNeglectAndIllness()` becomes a thin wrapper, or is replaced entirely with direct
     calls from the day-change handler).
  4. Update `ISaveable.SaveData`/`LoadData` in `Animal.cs` to persist/restore
     `illness.neglectDays` and `illness.IsIll` (same save-key names as before, to avoid breaking
     existing saves).
- change sketch:
  ```csharp
  // AnimalIllness.cs
  namespace SowurShield.Animals
  {
      public class AnimalIllness
      {
          private readonly int thresholdDays;
          private readonly string cureItemName;
          public int NeglectDays { get; private set; }
          public bool IsIll { get; private set; }

          public AnimalIllness(int thresholdDays, string cureItemName)
          {
              this.thresholdDays = thresholdDays;
              this.cureItemName = cureItemName;
          }

          public void UpdateNeglect(bool wasCaredForToday)
          {
              if (wasCaredForToday) { NeglectDays = 0; return; }
              NeglectDays++;
              if (NeglectDays >= thresholdDays) IsIll = true;
          }

          public bool TryCure(string fedItemName)
          {
              if (!IsIll || fedItemName != cureItemName) return false;
              IsIll = false;
              NeglectDays = 0;
              return true;
          }

          // RestoreState for load
          public void RestoreState(int neglectDays, bool isIll)
          {
              NeglectDays = neglectDays;
              IsIll = isIll;
          }
      }
  }
  ```
  `Animal.cs` changes are additive-replace: construct `illness` in `Awake()`/`Start()` using
  `animalData.illnessThresholdDays` and `animalData.illnessCureItemName`, then route existing
  call sites through it.
- tests: run `AnimalHusbandryTests.cs` (64 tests) and `AnimalTests.cs` (27 tests) — these MUST
  still pass unchanged, since the extraction should be behavior-preserving. If illness-specific
  tests reach into private fields via reflection, they may need updating to go through
  `animal.illness.IsIll` etc. — check this first before starting the extraction (if tests use
  reflection on private fields, this task gets noticeably harder; if they call public
  methods/properties like `animal.IsIll`, the extraction is safe as long as `Animal.IsIll`
  remains a public property delegating to `illness.IsIll`).
- done criteria: compiles; all 91 (64+27) existing animal tests pass unchanged; `Animal.cs` line
  count drops by roughly the size of the extracted logic (~40-60 lines expected, based on the
  900-917 range cited plus related fields).
- edge cases / risks: `CombatTeamSpawner.cs:254-256` reads `animal.IsIll` and
  `animalData.illnessStatPenalty` — confirm this still compiles and behaves identically after
  extraction (it should, if `Animal.IsIll` stays a public property). Tutorial system
  (`TutorialManager`) may also reference animal state — grep for `IsIll` usages project-wide
  before starting to catch all call sites.
- depends on: none

---

## TASK-012 — Extract SaveSlot UI logic from MainMenuUI.cs into MainMenuSaveSlotController
- [ ] status
- priority: Important
- system: UI
- files: `Assets/Scripts/MainMenuUI.cs` (1019 lines), new
  `Assets/Scripts/UI Systems/MainMenuSaveSlotController.cs`
- problem: `MainMenuUI.cs` grew from 862 to 1019 lines (+157) since the Feb 2026 audit, and
  handles new game, continue, load, settings, credits, confirmation dialogs, loading screen, AND
  save-slot picking (per `slotPickerPanel`, `slotListParent`, `saveSlotButtonPrefab`,
  `slotPickerBackButton`, `slotPickerTitleText` fields documented in CLAUDE.md). The save-slot
  picker is the most self-contained of these (it has its own panel, its own button prefab, and a
  narrow API surface: populate list, handle slot click, handle back button).
- what to do:
  1. Read `MainMenuUI.cs` in full, identify every method/field related to the slot picker:
     `slotPickerPanel`, `slotListParent`, `saveSlotButtonPrefab`, `slotPickerBackButton`,
     `slotPickerTitleText`, plus methods like `PopulateSlotPicker()`,
     `OnSlotPickerBackClicked()`, `OnSlotSelected()`, and whatever opens the picker (likely from
     "New Game" and "Continue"/"Load" button handlers).
  2. Create `Assets/Scripts/UI Systems/MainMenuSaveSlotController.cs` (namespace `SowurShield.UI`)
     as a `MonoBehaviour` owning the slot-picker panel and its fields/methods.
  3. In `MainMenuUI.cs`, replace the extracted fields with a single
     `[SerializeField] private MainMenuSaveSlotController saveSlotController;` reference, and
     replace direct calls to the extracted methods with calls on `saveSlotController` (e.g.,
     `saveSlotController.OpenForNewGame(callback)`, `saveSlotController.OpenForLoad(callback)`).
  4. The "what happens when a slot is picked" logic (start new game in that slot / load that
     slot) stays in `MainMenuUI` — `MainMenuSaveSlotController` should expose an event/callback
     (`Action<string> OnSlotChosen`) rather than knowing about `SaveManager` directly, to keep
     the new component focused purely on UI.
- change sketch: this is a larger extraction than TASK-011 — expect this to be the largest task
  in this worklist. If it doesn't fit in "a few minutes", it's acceptable to split further: do
  the read + field/method inventory as TASK-012a (documentation of what to extract, written to
  PROGRESS.md as a comment), and the actual extraction as TASK-012b. Use judgment — if reading
  `MainMenuUI.cs` reveals the slot-picker code is more tangled with other UI state than expected
  (e.g., shares private fields with the settings panel), STOP and document the tangle in
  PROGRESS.md rather than forcing an extraction that risks breaking the main menu.
- tests: MainMenuUI likely has no dedicated automated tests (not listed in the 16 test files in
  01_ARCHITECTURE.md Section 9) — validation is MANUAL: open the main menu in Play mode, click
  "New Game" (slot picker should appear, choosing a slot starts a new game in it), click
  "Continue"/"Load" (slot picker should appear in load mode, AutoSave hidden per CLAUDE.md Save
  System notes), click back button (returns to main menu without picking).
- done criteria: compiles; manual main-menu walkthrough (new game, continue, load, back) all
  work identically to before extraction; `MainMenuUI.cs` line count drops by roughly the size of
  the extracted slot-picker code.
- edge cases / risks: main menu scene wiring (`MainMenuUI.unity` / scene file) needs the new
  `MainMenuSaveSlotController` component added to a GameObject and its SerializeField references
  wired in the Unity Inspector — this is a CODE-side task, but it produces an INCOMPLETE result
  until someone does the Editor wiring. Document this clearly in PROGRESS.md so the Unity-wiring
  step isn't missed (similar to how ROADMAP.md tracked "Needs Unity Editor wiring" items).
- depends on: none

---

## TASK-013 — Verify SowurShield.Tests.PlayMode.asmdef / EditMode.asmdef exist and reference Runtime correctly
- [ ] status
- priority: Polish
- system: testing (verification)
- files: `Assets/Tests/**/*.asmdef`
- problem: CLAUDE.md and project memory reference `SowurShield.Tests.PlayMode.asmdef`
  (`includePlatforms: ["Editor"]`) and an EditMode equivalent, but 01_ARCHITECTURE.md's Section 3
  (Assembly Definitions) only directly confirmed the 3 non-test asmdefs
  (`SowurShield.Runtime.asmdef`, `SowurShield.Editor.asmdef`,
  `SowurShield.Dialogue.Editor.asmdef`) — the test asmdefs were not independently verified in
  this review pass.
- what to do:
  1. `Glob` for `Assets/Tests/**/*.asmdef`.
  2. For each found, confirm: `name` matches `SowurShield.Tests.PlayMode` /
     `SowurShield.Tests.EditMode` (or similar), `includePlatforms` is set appropriately
     (`["Editor"]` for PlayMode per CLAUDE.md), and `references` includes
     `SowurShield.Runtime`.
  3. If anything is missing or misconfigured, note it — but do NOT fix it as part of this task
     unless the fix is a one-line `references` addition. If it requires restructuring (e.g., the
     16 test files are split across folders not covered by any asmdef, causing them to fall back
     to `Assembly-CSharp` implicitly), document the finding in PROGRESS.md as a new task rather
     than attempting a fix blind.
- change sketch: none expected — verification task.
- tests: none — this task verifies test INFRASTRUCTURE, doesn't add tests itself.
- done criteria: PROGRESS.md records what was found (asmdef names, platforms, references) and
  whether anything needs follow-up.
- edge cases / risks: none — read-only investigation.
- depends on: none

---

## TASK-014 — Consolidate stale root-level docs into one current status doc
- [ ] status
- priority: Important
- system: documentation
- files: `ROADMAP.md`, `GAME_DEVELOPMENT_PLAN.md`, `COMBAT_PIPELINE_STATUS.md`,
  `DEVELOPMENT_LOG.md`, `COMBAT_SETUP_GUIDE.md`, `FULL_GAME_PROJECT_AUDIT.md`, new
  `SOWUR_SHIELD_STATUS.md` (or similar single name)
- problem: 6 root-level .md docs (dated Oct 2025 - Apr 2026) overlap heavily and are now stale
  relative to `/review/01_ARCHITECTURE.md` and `/review/02_FINDINGS.md` (this review's fresh
  ground truth). Keeping 6 separate docs makes it unclear which is authoritative.
- what to do:
  1. Create ONE new root-level doc (suggested name `SOWUR_SHIELD_STATUS.md`) that becomes the
     single source of truth for "what's the current state of the project". Structure suggestion:
     - **Current State Overview** (from ROADMAP.md, updated with 01_ARCHITECTURE.md's stats:
       154 scripts, 413 tests, 16 test files, namespace/asmdef summary)
     - **What's Complete** (condensed from ROADMAP.md's checkmarked sections — verify each
       against 01_ARCHITECTURE.md before carrying forward)
     - **Combat Scope Decision** (the outcome of TASK-002 — Family/Class/Happiness passives
       descoped or minimally implemented)
     - **Known Tech Debt** (pointer to `/review/02_FINDINGS.md` rather than duplicating it)
     - **Unity Editor Wiring Checklist** (carry forward ROADMAP.md's "Needs Unity Editor wiring"
       items — these are still likely valid since they're manual-setup items independent of code
       review)
  2. Once the new doc is written and the user has reviewed it, DELETE the 6 old docs (or move
     them to an `archive/` subfolder if the user prefers history preserved — ask if unsure, but
     default to delete since git history preserves them anyway).
  3. Update any cross-references to the deleted docs (grep for their filenames across the repo,
     especially in CLAUDE.md and `.github/` workflows — `COMBAT_SETUP_GUIDE.md` in particular may
     be linked from somewhere).
- change sketch: N/A — this is a content-synthesis task. The synthesis should prioritize
  CURRENT ACCURACY over completeness: it's better for `SOWUR_SHIELD_STATUS.md` to be shorter and
  100% accurate than to carry forward every detail from 6 docs with unclear staleness.
- tests: none — documentation only.
- done criteria: `SOWUR_SHIELD_STATUS.md` exists at repo root, the 6 old docs are removed (or
  archived), and no remaining file references a deleted doc by path (grep check).
- edge cases / risks: `COMBAT_SETUP_GUIDE.md` (606 lines) contains detailed manual Unity Editor
  setup steps for prefabs/scenes that may STILL be needed even if the combat pipeline code works
  (TASK-001) — don't lose this content. If TASK-001's smoke test passes, much of
  COMBAT_SETUP_GUIDE.md's content describes steps that are now DONE (baked into the saved
  scenes) — but verify against TASK-001's result before discarding, since some manual asset
  assignments (AnimalSkill ScriptableObjects, AnimatorControllers per ROADMAP.md's "Needs Unity
  Editor wiring") may still be outstanding and worth keeping as a checklist in
  `SOWUR_SHIELD_STATUS.md`.
- depends on: TASK-001 (need to know if combat pipeline truly works before writing the new
  status doc's combat section), TASK-002 (need the scope decision for the "Combat Scope" section)

---

## Summary

**14 tasks generated.**

**Critical priority (3 tasks)** — recommended to run first, in this order:
1. **TASK-001** — Smoke-test the combat pipeline. This is pure verification (no code change) and
   either closes out the project's longest-standing open bug investigation, or surfaces a real
   remaining issue early — either way, everything else benefits from knowing this answer.
2. **TASK-004** — Add save-migration dispatch scaffolding. Small, isolated, no dependencies, and
   the kind of thing that gets exponentially more annoying to retrofit the longer it's deferred.
3. **TASK-002** — Decide the fate of the PRD's 3-Passive combat system. This is a
   decision-and-documentation task (not code), but it's blocking (TASK-003 depends on it, and
   TASK-014's final status doc depends on it too) and resolves the single biggest
   ambiguity in the project's combat scope.

**Important (7 tasks)**: TASK-003 (conditional on TASK-002), TASK-005, TASK-006, TASK-007,
TASK-008, TASK-011, TASK-012, TASK-014.

**Polish (4 tasks)**: TASK-009, TASK-010, TASK-013.

All tasks are designed to be independently resumable — see `00_README.md` for the execution
protocol.
