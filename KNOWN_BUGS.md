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

## [OPEN] Dialogue UI — Choice button text clips out of button bounds

**Symptom:** Choice button labels overflow their button background at certain screen sizes
or with longer text. Text appears above/outside the wooden button frame.

**Where to look:** `DialogueTreeUI.cs` — `CreateChoiceButton()` method. The button
RectTransform `sizeDelta` and `ChoiceButton` label's RectTransform margins may need
adjustment, or the ChoicePanel needs a minimum height per button.

**Workaround:** None — cosmetic only, choices are still clickable.
