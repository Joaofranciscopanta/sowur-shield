# Findings — Sowur Shield Code Review

> Diagnostic findings from a fresh read of the codebase on 2026-06-12, building on
> 01_ARCHITECTURE.md. Three buckets: Critical, Important, Polish. Every item has a file:line
> citation. "Critical" = blocks shipping or risks data loss / broken pipeline. "Important" =
> real debt that will cause pain as the project grows. "Polish" = nice-to-have, low risk if
> skipped.

---

## Critical

### C1. Save data has no real migration path
- **System**: save
- **File**: `Assets/Scripts/SaveManager.cs:393-397`
- **Problem**: `MigrateSave()` exists as a method signature but contains no field-transformation
  logic. Right now this is silent (old saves still load because JSON deserialization fills
  missing fields with defaults), but the moment a future change needs to *transform* existing
  data (rename a key, recompute a derived stat, restructure a nested object), there is no hook
  to do it. Every system added so far (Quest, FarmBuilding, Tutorial, Weather-adjacent state)
  has been additive — this has masked the gap.
- **Why it matters now**: the project has 14 ISaveable implementers and is actively growing.
  The cost of adding versioned migration is much lower now (few schema versions) than later
  (many).
- **Risk if ignored**: a future schema change silently corrupts or drops player progress for
  existing saves, with no way to detect or repair it.

### C2. PRD's "3-Passive System" (Family/Class/Happiness passives + Combo Synergies) is not implemented — but the data model implies it should be
- **System**: combat / animals (scope)
- **Files**: `Assets/Scripts/Animals/AnimalData.cs` (`combatClass`, `availablePassiveSkills[]`,
  `canStack`/`maxStackSize`), `Assets/Scripts/Combat/CombatUnit.cs`, `Assets/Scripts/Combat/TurnManager.cs`
- **Problem**: `AnimalData.combatClass` and `AnimalData.availablePassiveSkills[]` are populated
  fields on the ScriptableObject, but grep across `Assets/Scripts/` shows **zero readers** of
  either field outside their own declaration. `CombatUnit`/`TurnManager` compute stats purely
  from `AnimalCombatStats` with no notion of family/class synergy or combo bonuses. The PRD
  (`PRD_Animals_Combat_System.md`, v2.0, 2025-10-21) describes this 3-passive system as the
  **core mechanic** of combat, but ROADMAP.md's "complete" combat system (status effects +
  XP/leveling) never mentions it.
- **This is the single biggest scope-vs-reality gap in the project.** Two outcomes are possible
  and both are valid — but the project needs to **pick one explicitly**:
  1. **Descope**: treat the PRD as historical/aspirational, remove or clearly mark
     `combatClass`/`availablePassiveSkills` as unused-for-now in AnimalData, and update the PRD
     to match the simpler shipped system (status effects + XP/leveling). Low effort, removes
     confusing dead fields.
  2. **Implement a minimal version**: pick ONE passive type (e.g., Class passive only —
     Tank/DPS/Support/Utility, since `combatClass` already exists on every AnimalData asset) and
     wire a small synergy bonus into `CombatTeamSpawner`'s stat pipeline. Defer Family passives
     and Combo Synergies entirely.
- **Risk if ignored**: every future combat balance discussion will be ambiguous about whether
  "the passive system" exists. New contributors reading the PRD will think it's implemented and
  waste time looking for it.

### C3. CombatScene.unity YAML health — VERIFIED CLEAN (no action needed, downgraded from Critical)
- **System**: combat
- **File**: `Assets/Scenes/CombatScene.unity`
- **Finding**: grep for `m_EditorClassIdentifier: ` (empty value) returns **0 matches**. The 24
  empty-ECI components documented as a "persisting" bug in COMBAT_PIPELINE_STATUS.md are fixed —
  this was resolved by one of the four commits that postdate that doc (16028e2, 407c426,
  306c529, 9d5773e). **No code action required.** Listed here only so the worklist doesn't
  re-open a closed investigation — see TASK list for the one remaining recommended action (a
  smoke-test play-through to get a second confirmation signal beyond YAML inspection).

---

## Important

### I1. God classes have grown, not shrunk
- **System**: inventory, UI, animals
- **Files & current sizes** (Feb 2026 audit → now):
  - `MainMenuUI.cs`: 862 → **1019 lines** (+157)
  - `SellBox.cs`: 1109 → **1123 lines** (+14)
  - `Inventory.cs`: 1146 → **~1150 lines** (+4, roughly flat)
  - `InventorySlot.cs`: 902 → **907 lines** (+5, despite partial split into
    `SlotVisualController`/`SlotDragHandler`)
  - `Animal.cs`: not previously flagged, now **975 lines** — 2nd largest script in the project
- **Problem**: `MainMenuUI.cs` at 1019 lines handles new game, continue, load, settings,
  credits, confirmation dialogs, loading screen, AND save-slot picking in one class. `Animal.cs`
  at 975 lines handles interaction, happiness, production, illness, XP/leveling, seasonal
  modifiers, and ISaveable — six responsibilities in one MonoBehaviour.
- **Why Important not Critical**: these classes work and are tested (AnimalHusbandryTests has
  64 tests against Animal.cs). The risk is velocity, not correctness — every new animal feature
  requires touching a 975-line file and re-running a 64-test suite to be safe.
- **Recommendation**: do NOT attempt a big-bang split. Extract ONE cohesive piece at a time
  (e.g., `AnimalIllness` component, or `MainMenuSaveSlotController`), each as its own small task
  with its own test pass.

### I2. UIManager has two coexisting window-management systems
- **System**: UI
- **File**: `Assets/Scripts/UIManager.cs:13-120` (legacy `allUIPanels`/`currentlyOpenPanel`/
  `OpenPanel`/`ClosePanel`) vs `Assets/Scripts/UIManager.cs:24-311` (current
  `openWindowStack`/`TryOpenWindow`/`TryCloseWindow`, used by all 10 `IUIWindow` implementers)
- **Problem**: the legacy panel API is still present in the file. If any code path still calls
  `OpenPanel`/`ClosePanel` directly (not confirmed either way by this pass), it could
  desynchronize from `openWindowStack`, causing two "open" windows to both think they're on top
  (e.g., ESC closing the wrong one, or a window failing to receive `OnWindowBlocked`).
- **What to do**: grep all call sites of `OpenPanel`/`ClosePanel`/`allUIPanels`/
  `currentlyOpenPanel`. If zero non-declaration call sites exist, delete the legacy block
  entirely (pure deletion, low risk). If call sites exist, each one is its own small migration
  task to `TryOpenWindow`/`TryCloseWindow`.

### I3. QuestManager.GrantRewards() does runtime FindFirstObjectByType on quest completion
- **System**: dialogue/quests
- **File**: `Assets/Scripts/Dialogue/QuestManager.cs:272-279`
- **Problem**: `FindFirstObjectByType<PlayerStats>()` and `FindFirstObjectByType<Inventory>()`
  are called inside `GrantRewards()`, which runs on quest completion — a player-facing moment
  where a missed reference (returns null) would silently drop gold/item rewards with no error
  surfaced to the player.
- **Why Important not Critical**: quest completion is infrequent (not a hot path), so the
  performance cost of `FindFirstObjectByType` is negligible. The real risk is **correctness**:
  if `PlayerStats`/`Inventory` aren't in the scene when a quest completes (e.g., a quest
  triggers during a scene transition), the reward is silently lost.
- **Recommendation**: cache `PlayerStats`/`Inventory` references in `QuestManager.Start()` (or
  have `GrantRewards()` log a warning if either is null) rather than re-finding on every
  completion.

### I4. Dialogue core logic has zero unit test coverage
- **System**: dialogue (testability)
- **Files**: `Assets/Scripts/Dialogue/Core/DialogueTree.cs`, `DialogueNode.cs`, `DialogueChoice.cs`,
  `DialogueCondition.cs`, `DialogueEffect.cs`, `Assets/Scripts/Dialogue/UI/DialogueTreeUI.cs`
- **Problem**: of 413 total test methods, **none** target `DialogueTree.ValidateTree()`,
  `GetReachableNodes()`, `DialogueCondition` evaluation (the `RelationshipLevel`/`QuestStatus`/
  `InventoryItem` condition types in particular have non-trivial comparison logic via
  `ConditionOperator`), or `DialogueEffect` execution. `NPCRelationshipTests.cs` (10 tests)
  covers `ConversationMemory` directly but not the `DialogueCondition`/`DialogueEffect` classes
  that read/write it.
- **Risk**: a bug in `ConditionOperator.GreaterOrEqual` vs `GreaterThan`, or in
  `GiveItem`/`TakeItem` effect execution, would only surface via manual playtesting of a
  specific dialogue branch — easy to miss.
- **Recommendation**: add an EditMode test file `DialogueConditionTests.cs` covering each
  `ConditionType` × `ConditionOperator` combination with a mocked/fake `ConversationMemory`.

### I5. Farming/Organization: "Farming" scripts live in `Scripts/` root under `SowurShield.Core`, not a `Scripts/Farming/` folder
- **System**: farming (organization)
- **Files**: `CropData.cs`, `CropGrowthManager.cs`, `SoilBlockInteractable.cs`,
  `FarmBuildingManager.cs`, `FarmBuildingData.cs`, `WeatherController.cs` — all in `Scripts/`
  root with `SowurShield.Core` namespace. Only `Scripts/DualGridTilemap/` uses
  `SowurShield.Farming`.
- **Problem**: CLAUDE.md's folder table implies a `Scripts/Farming/` folder exists for these
  files. It doesn't — they're in root. This is **not a namespace violation** (root → Core is the
  documented fallback) but it means CLAUDE.md's own architecture table is slightly out of sync
  with the actual layout, which could mislead a new contributor trying to find "the farming
  system".
- **Recommendation**: this is a documentation fix, not a code fix. Either (a) update CLAUDE.md's
  table to show these files under `Scripts/` root with `Core`, or (b) physically move them to
  `Scripts/Farming/` and re-namespace to `SowurShield.Farming` — but (b) touches 6 files'
  namespaces and any cross-references, so only do it if there's appetite for the churn. Default
  recommendation: (a), it's a 1-line table edit.

### I6. SellBox reads GameBalance from Resources on every property access
- **System**: economy
- **File**: `Assets/Scripts/SellBox.cs:67` (per farming exploration agent)
- **Problem**: `sellMultiplier` is a computed property that reads `GameBalance` (potentially via
  `Resources.Load` fallback) every time it's accessed, rather than caching the reference once in
  `Start()`/`Awake()`.
- **Why Important not Polish**: `Resources.Load` is not free, and if `sellMultiplier` is read
  per-item during a large sell-on-sleep batch (e.g., selling 20+ stacked items), that's 20+
  redundant loads for a value that never changes during a session.
- **Recommendation**: cache the `GameBalance` reference once (the pattern is already established
  elsewhere: `if (balance == null) balance = Resources.Load<GameBalance>("GameBalance")` in
  `Start()`), then have the `sellMultiplier` property read the cached field.

---

## Polish

### P1. GroundItem and heart particles are not pooled
- **System**: animals / performance
- **Files**: `Assets/Scripts/Animals/Animal.cs:388` (heart particle `Instantiate`),
  `Assets/Scripts/Animals/Animal.cs:566` (GroundItem `Instantiate`)
- **Problem**: both use `Instantiate`/implicit `Destroy` rather than an object pool, as flagged
  in the Feb 2026 audit.
- **Why Polish not Important**: production events are at most once-per-day per animal (per
  `productionIntervalDays`), and pet-heart particles are player-initiated and rate-limited by a
  5s cooldown. With a roster of, say, 10-20 animals, this is a handful of allocations per day —
  not a meaningful GC pressure source. Pooling would be premature optimization unless the animal
  roster grows by an order of magnitude.
- **Recommendation**: leave as-is unless profiling on a large roster (50+) shows a frame-time
  spike correlated with daily production resolution.

### P2. Time-of-day magic numbers not in GameBalance
- **System**: time
- **File**: `Assets/Scripts/TimeController.cs` (wake time ~0.25f and similar constants, per
  farming exploration agent)
- **Recommendation**: low priority — these are tuned once and rarely revisited, unlike
  happiness/economy values which benefit from live Inspector tweaking.

### P3. InteractionManager type-checks are now narrowly scoped to UI-display concerns, not range/eligibility
- **System**: interaction (re-verification of Feb audit finding)
- **File**: `Assets/Scripts/InteractionManager.cs:152-153, 168-176, 201-209`
- **Finding**: `GetInteractionRange()` (line 161-164) is now a pure delegate to
  `interactable.GetInteractionRange()` — the Open/Closed violation the Feb audit was most
  concerned about (range/eligibility logic branching on concrete type) **is gone**. The
  remaining `is NPCDialogueInteractable` / `is SellBox` checks are for:
  - `IsInteractableAvailable()` (152-153): NPC-specific "don't re-trigger while dialogue is
    active" guard.
  - `SetInteractablePromptVisibility()` (168-176): NPC has a prompt to show/hide; SellBox
    deliberately has a no-op branch with a comment "could add one in the future".
  - `GetCurrentInteractableName()` (201-209): NPC returns display name, SellBox returns literal
    "SellBox" string, everything else falls back to `gameObject.name`.
- **Assessment**: these three remaining checks are reasonable — they're genuinely
  type-specific UI behaviors (a prompt label, a display name), not core interaction-eligibility
  logic. **Downgrading from the Feb audit's "Open/Closed violation" framing.** Could be cleaned
  up by adding `string GetDisplayName()` and `bool HasPrompt`/`SetPromptVisibility` to
  `IInteractable` with default implementations, but the payoff is marginal (3 call sites, all
  working correctly). Polish-tier, optional.

### P4. SellBox no-op branch has a stale "could add later" comment
- **System**: economy (cleanup)
- **File**: `Assets/Scripts/InteractionManager.cs:173-176`
- **Problem**: `if (interactable is SellBox sellBox) { /* SellBox doesn't have a prompt, but we
  could add one in the future */ }` — `sellBox` variable is declared but unused, and the branch
  does nothing.
- **Recommendation**: either implement the prompt or delete the dead branch (`if (interactable
  is SellBox)` with empty body, or remove entirely). Trivial one-line cleanup.

---

## What's Confirmed Working Well (no action needed)

- Namespace convention: 100% compliant across 154 files.
- Combat pipeline (TeamAssembler → CombatScene → TurnManager → Results): structurally complete,
  CombatScene.unity YAML is healthy (C3 above).
- Status effects (Stun/Shield/Burn) + immunities: well-implemented AND well-tested (29 tests in
  CombatPhase1Tests.cs covering exactly these mechanics with edge cases like refresh-not-stack,
  shield cap, case-insensitive immunity matching).
- GameBalance ScriptableObject: ~80% of tunable values centralized, sensible fallback pattern.
- Save versioning infrastructure exists (slots, ISaveable registry) — only the *migration logic*
  (C1) is missing, not the scaffolding.
- Farming state machine, crop growth, weather: clean, no issues found.
- Animal husbandry (happiness, production, illness, XP/leveling): comprehensive and matches
  documented formulas exactly; 106 tests across 3 files.
