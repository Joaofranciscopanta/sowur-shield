# Known Bugs

## [OPEN] Maren Beloved — Can't re-interact after first conversation

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

---

## [NEEDS RE-VERIFICATION] Dialogue UI — Choice button text clips out of button bounds

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
