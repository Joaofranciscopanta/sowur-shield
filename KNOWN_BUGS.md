# Known Bugs

> Companion to [SOWUR_SHIELD_STATUS.md](SOWUR_SHIELD_STATUS.md). Sections: **Bugs** (broken
> behavior) and **Quirks** (surprising-but-intended or environment-specific behavior worth
> knowing before debugging "ghosts").

## [FIXED 2026-07-05] Purchased animals vanish on returning from combat

**Symptom:** An animal bought from AnimalMarketUI disappears the moment the player returns
from CombatScene to SampleScene (or after any disk load), even though hand-placed animals
survive fine. No error in the console.

**Root cause:** Purchased animals are plain runtime GameObjects (`new GameObject(...)` in
`AnimalMarketUI.SpawnPurchasedAnimal`) with no scene-file entry. Only their per-attribute
state (happiness, growth) was ever saved, keyed off `gameObject.name` — nothing recorded
that the GameObject itself needed to exist. On any scene reload the object is gone and
nothing recreates it.

**Underlying discovery:** `SceneTransitionManager` — the class meant to own "farm scene
(re)loaded" bookkeeping (re-apply save data, restore inventory snapshot, switch music) — is
**never instantiated anywhere in the project**. No scene, prefab, or bootstrap creates it;
every call site's `if (SceneTransitionManager.Instance != null)` falls through to a plain
`SceneManager.LoadScene(...)`, in the real game too, not just in test sessions. Its
`OnGameSceneLoaded()` callback therefore never ran.

**Fix applied (2026-07-05):**
- `GameData.worldData.purchasedAnimals` (new) records which animals were bought, from which
  `AnimalData`, and into which `AnimalZone`.
- `Animal.SaveData()`/`OnDestroy()` add/remove their own entry, guarded by
  `gameObject.scene.isLoaded` so a scene teardown (which also fires `OnDestroy`) is never
  mistaken for a sale.
- `AnimalPurchaseLoader` (new) re-instantiates purchased animals from that list, listening to
  `SceneManager.sceneLoaded` directly instead of the dead `SceneTransitionManager` path.
- `SaveManager.CaptureRegisteredObjectsIntoCurrentGameData()` (new) snapshots every
  `ISaveable` into memory right before combat starts, so a purchase survives the round-trip
  even without an explicit disk save.
- `GameSceneReloadHandler` (new) independently drives
  `SaveManager.ReapplyLoadedDataToRegisteredObjects()` on scene load, since
  `SceneTransitionManager` can't be relied on for that either.

Verified in Play Mode via the real MainMenu → New Game → buy animal → set custom happiness →
battle → return flow: the animal reappears with its exact saved happiness intact.

---

## [FIXED 2026-07-05] SellBox and FeedingTrough contents lost on returning from combat

**Symptom:** Items placed in the SellBox (queued for the next sleep) or in a FeedingTrough
silently disappear after any trip through CombatScene, even though the fix above already
made `ReapplyLoadedDataToRegisteredObjects()` run on scene load.

**Root cause:** Both `SellBox` and `FeedingTrough` register themselves with `SaveManager` in
`Start()`, not `Awake()`. `SceneManager.sceneLoaded` fires after every object's `Awake()` in
the new scene but **before** any of their `Start()` methods — so the very first version of
`GameSceneReloadHandler` called `ReapplyLoadedDataToRegisteredObjects()` one frame too early,
before SellBox/FeedingTrough had registered, and both were silently skipped.

Separately, `SellBox` didn't implement `ISaveable` at all — its container was pure runtime
state with no save path whatsoever, on top of the timing issue above.

**Fix applied (2026-07-05):**
- `SellBox` now implements `ISaveable` (mirrors `FeedingTrough`'s per-slot
  item-name/quantity persistence pattern), registers/unregisters with `SaveManager` in
  `Start()`/`OnDestroy()`.
- `GameSceneReloadHandler.OnAnySceneLoaded()` now waits one frame (`yield return null`)
  before calling `ReapplyLoadedDataToRegisteredObjects()`, so every `Start()` in the reloaded
  scene has run first. `AnimalPurchaseLoader.OnAnySceneLoaded()` got the same one-frame delay
  for consistency, though it wasn't strictly required there.

Verified in Play Mode: 3 Carrots placed in the SellBox and 5 CarrotSeed in a FeedingTrough
both survive a full battle round-trip (confirmed via `HasItemsToSell`/`GetFeedableAnimalCount`
before and after).

---

## [FIXED 2026-07-05] Combat music never plays; farm music keeps playing through battles

**Symptom:** Entering CombatScene, the farm's seasonal music keeps playing instead of
switching to combat music; returning to the farm doesn't switch back either.

**Root cause:** Same dead `SceneTransitionManager` as above — `GameMusicManager.OnStartGame()`
/ `OnEnterCombat()` / `OnExitCombat()` all existed and were correctly implemented, but nothing
ever called them in the real game; the class comment even said "we rely on
SceneTransitionManager callbacks."

**Fix applied (2026-07-05):** `GameMusicManager` now listens to `SceneManager.sceneLoaded`
directly and calls `OnStartGame()`/`OnEnterCombat()` for SampleScene/CombatScene respectively
(MainMenu is intentionally left alone — `MainMenuManager` already plays its own menu music and
explicitly stops `GameMusicManager`'s track to avoid overlap).

**Still needed (pre-existing content gap, not part of this fix):** `combatMusic` (and
`menuMusic`) `AudioClip` fields on the `GameMusicManager` Inspector are unassigned, so even
though `OnEnterCombat()` now fires at the right time, there's no clip to switch to yet
(confirmed by assigning a throwaway test clip at runtime — the switch mechanism works). See
SOWUR_SHIELD_STATUS.md's Audio wiring checklist.

---

## [FIXED 2026-07-05] Maren Beloved — Can't re-interact after first conversation

**Symptom:** After talking to Maren (rel >= 75) and closing the dialogue (ESC or finishing),
the player can no longer interact with her. The "Press E to Talk" prompt disappears and E
key does nothing.

**Root cause (suspected):** `InteractionManager` drives `playerInRange` via
`SetPromptVisibility(bool)` on `NPCDialogueInteractable`. After dialogue ends,
`isDialogueActive` is reset to false correctly, but `playerInRange` is not re-evaluated
unless the player moves out and back into range. `CheckPlayerDistance()` only runs in
fallback mode (no InteractionManager). So `CanInteract()` returns true, but the prompt
never re-appears and `InteractionManager` may skip this interactable.

**Where to look:**
- `NPCDialogueInteractable.OnDialogueEndedCallback()` (line ~437) — should call
  `SetPromptVisibility(true)` or force-trigger an InteractionManager re-scan after
  dialogue ends, if the player is still in range.
- `InteractionManager` — check if it re-evaluates interactables after a dialogue closes
  or if it needs a nudge (e.g. subscribe to `NPCDialogueInteractable.OnDialogueEnded`).

**Workaround:** Walk away from the NPC and back into range.

**Fix applied (2026-07-05, QA audit session):** re-interaction itself had already recovered
(InteractionManager re-scans within seconds), but the "Press E" prompt stayed hidden because
the manager only pushes `SetPromptVisibility` on interactable *transitions*. Fixed in
`NPCDialogueInteractable.OnDialogueEndedCallback()` — it now calls `SetPromptVisibility(true)`
when the player is still within `GetInteractionRange()`. Verified in Play Mode: prompt is
active immediately after `EndDialogue` with the player in range.

---

## [FIXED 2026-07-05] Dialogue UI — Choice button text clips out of button bounds

**Symptom (as originally reported):** Choice button labels overflow their button background
at certain screen sizes or with longer text. Text appears above/outside the wooden button
frame.

**Status:** The code this bug pointed to no longer exists. `DialogueTreeUI.CreateChoiceButton()`
was removed during the CozyUITheme pass — choice buttons are now instantiated from
`choiceButtonPrefab` (`DialogueTreeUI.cs:435`) and driven by `ChoiceButton.cs`
(`Assets/Scripts/Dialogue/UI/ChoiceButton.cs`), which only sets `choiceText.text` and applies
theme colors; it has no dynamic resize logic. Whether the clipping still reproduces now depends
entirely on the prefab's `RectTransform`/`ContentSizeFitter`/TMP auto-size settings, which can't
be checked from source — needs a manual Editor/Play-mode check with a long choice string.

**Where to look (if still reproducing):** the `choiceButtonPrefab` asset itself (likely under
`Assets/Prefabs/` or `Resources/`) — check the label's `ContentSizeFitter`/auto-size and whether
the button `RectTransform` grows with a `LayoutElement`/`VerticalLayoutGroup` on the choice
container.

**Workaround:** None confirmed — cosmetic only if it still reproduces, choices remain clickable.

**Fix applied (2026-07-05, QA audit session):** re-verified and confirmed — the prefab
(`Assets/Prefabs/UI/ChoiceButton.prefab`) is a fixed 160×30 rect with TMP `overflowMode=Overflow`,
no auto-size and no ContentSizeFitter, so a 150-char choice measured 1422×252 and spilled over
the world. Fixed in code: `ChoiceButton.Initialize()` now calls `FitHeightToText()`, which grows
the button's rect (and publishes min/preferredHeight through a `LayoutElement`) to fit the label.
Verified in Play Mode: same long string now yields a 218px-tall button with no overflow.

---

## [HARDENED 2026-07-05] ItemDatabase lookup came back empty after an in-editor domain reload

**Symptom (observed 2026-07-01, Editor via MCP):** after a script recompile mid-session,
`ItemDatabase.GetItem("...")` returned null for every item and the static `itemLookup`
dictionary had count 0, even after touching `ItemDatabase.Instance` (which should trigger
`Initialize()`). Earlier in the same session the same lookups worked (count 20).

**Notes:** `Resources.Load<ItemDatabase>("ItemDatabase")` returned null at the time — there is
no `ItemDatabase.asset` at `Resources/` root, so `Instance` falls back to
`CreateInstance` + auto-load from `Resources/Items` (only 6 items live there; the other ~14
load from other Resources folders via the initialized path). Suspicion: the static
`isInitialized` flag and the static `itemLookup` can get out of sync across domain reloads
(`Initialize()` early-returns on `isInitialized` without checking the dictionary is populated).

**Where to look:** `Assets/Scripts/Inventory/ItemDatabase.cs` — `Initialize()` guard (consider
`if (isInitialized && itemLookup.Count > 0) return;`) and/or `[RuntimeInitializeOnLoadMethod]`
reset. Also consider actually creating the `ItemDatabase.asset` in `Resources/`.

**Impact if it fires in a build:** items silently fail to resolve (quests rewards, feeding,
shops). Not yet reproduced in Play Mode from a cold start — may be editor-domain-reload only.

**Hardening applied (2026-07-05, QA audit session):** `Initialize()` now re-runs when
`itemLookup` is empty or holds destroyed assets even if `isInitialized` is true, and a
`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset clears all statics at every
play session start. The originally suspected repro (mid-session editor domain reload) was not
forced during the session, so this stays listed until someone re-observes it in the wild.

---

# Quirks (not bugs)

## HUD money/time/day text is empty when playing directly in SampleScene

`SafeGetLocalizedString()` intentionally returns `""` until the Localization tables finish
preloading, which happens during the MainMenu scene (guards against a WebGL
`WaitForCompletion` deadlock). Entering Play Mode directly in SampleScene skips that preload,
so labels stay blank. Normal flow (MainMenu → game) is unaffected.

## Unity Play Mode freezes while the Editor window is unfocused

The Editor throttles background playback: `Time.time` barely advances and `Invoke()` timers
never fire while another window has focus. Any automated/scripted play-mode test (combat
spawns, timers) requires the Unity window to stay focused. This is Editor behavior, not a
game bug.

## Duck and Sparrow use chicken-baby placeholder sprites

`duck.asset` / `Sparrow.asset` point at `Chicken_Baby*.png` — art gap, tracked in
SOWUR_SHIELD_STATUS.md "Art gaps", listed here so nobody debugs it as a sprite-assignment bug.
