# Relationship & Codex — deepening plan

> Written 2026-08-01. Status: **proposal, not yet implemented** (except the clamped-event fix,
> which shipped with this document). Supersedes nothing; the existing system is kept and
> extended.

## What exists today (verified, not assumed)

The logic is in better shape than the content. Working and tested:

- `ConversationMemory` (singleton, `ISaveable`) — `SetRelationship` / `ModifyRelationship` /
  `GetRelationshipLevel`, clamped to −100..100 in `ConversationData:134`, persisted as int×100,
  auto-saves every 30s, fires `OnRelationshipChanged`
- One gift per NPC per in-game day (`CanGiftToday` / `GiveGift`, keyed on `lastGiftDay`)
- `NpcLoreEntry[]` per NPC with a `requiredRelationship` gate; `GetUnlockedLore()` filters and
  sorts by tier
- `RelationshipUI` — self-spawning codex panel: portrait, bio, 6-tier label, affinity bar, lore
  list. No scene wiring needed
- Quest rewards and dialogue effects can both move affinity

**The measured gap is content, not architecture:**

| | Count |
|---|---|
| NPCs in the entire game | **1** (Maren) |
| Items with `giftAffinityValue > 0` | **2** of 28 (Bread 15, Fish 5) |
| Ways to gain affinity | 2 (dialogue effect, quest reward) |
| Maren's lore tiers | 4 (at 0 / 10 / 40 / 75) |

A codex about one person, filled by two items, is going to feel thin no matter how good the
code is. **Content is the bottleneck; the mechanics below exist to give that content something
to do.**

---

## Bug fixed alongside this document

`ConversationMemory.SetRelationship` fired `OnRelationshipChanged` with its **raw parameter**
rather than the stored value. Since storage clamps to ±100, a gift pushing affinity to 110
stored 100 and announced 110. Nothing subscribes to the event today, so there was no symptom —
but every feature below is a subscriber, and each would have inherited the discrepancy. Also
removed a computed-and-never-used `oldLevel` local.

This is the same failure shape as the Aug/1 bugs: silently wrong, no console error, no failing
test.

---

## Proposed mechanics

Ordered by value-per-unit-of-work. Each is independently shippable — none blocks another.

### 1. Gift preferences (loved / liked / disliked) — highest value

Today a gift is worth `item.giftAffinityValue` to **every** NPC. Bread is worth 15 to everyone,
forever. There is no reason to learn anything about a character.

Proposal: per-NPC preference lists on `NPCDialogueInteractable`.

```
lovedGifts[]     → ×2.5 affinity, unique reaction line
likedGifts[]     → ×1.5
(unlisted)       → ×1.0   ← current behaviour, so existing content still works
dislikedGifts[]  → ×−1.0  (a real mistake, not merely neutral)
```

Why this first: it is the cheapest change that makes the *existing* 2 giftable items and the
1 existing NPC more interesting, and it turns "which item has the highest number" into "what
does Maren actually like". It also gives the codex something worth revealing (see §4).

### 2. More affinity sources

Two sources is too few for a −100..100 range. Add:

- **First conversation of the day**: +1, once per NPC per day. Rewards simply visiting.
- **Quest completion**: already exists via `QuestManager` — keep.
- **Seasonal/birthday event**: a `birthdayDay` per NPC; gifting on that day multiplies ×3 and
  is remembered in the codex.

Deliberately *not* proposing affinity decay. Decay punishes the player for playing other parts
of the game and, with one NPC, would be pure friction. Revisit only if the NPC count grows.

### 3. Affinity unlocks something mechanical

Right now affinity buys lore text and a shop discount. Add at least one tangible unlock so the
bar reads as progression:

- **40+**: NPC occasionally leaves a gift at the farm (a `GroundItem` on day change)
- **75+**: unlocks a unique recipe/seed in their shop stock

### 4. Codex depth

- Show **locked** tiers as greyed rows with their requirement ("Requires: Close Friend"), rather
  than hiding them. A codex that visibly has more to reveal is the whole point of a codex.
- Record **discovered gift preferences** in the codex as they are found — this is what makes §1
  legible instead of guesswork.
- Show `lastGiftDay` state ("Already gifted today") so the player is not guessing.

### 5. Visual pass

`RelationshipUI` is built procedurally with flat theme colours and never received the cozy
sprite-kit pass the other panels got on Jul/26–Aug/1. It needs:

- Wood panel sprite + gold heading, per the established `UIThemeStyler` pattern
- **Contrast check on every label** — this panel puts cream text on `woodDark` today, which is
  fine, but §4's greyed rows and any new backing must be measured (see the Jul/26 findings, where
  three separate labels scored under 2.0 and were invisible)
- The affinity bar is a plain `Image.Filled` with no tier markers — add ticks at the six label
  thresholds so the bar communicates *distance to the next tier*
- Beware the two runtime-UI traps this project has now hit three times: point-anchored
  `RectTransform`s with `sizeDelta.x = 0`, and `childForceExpandWidth` without `childControlWidth`

---

## Suggested order

1. Clamped-event fix — **done**
2. Gift preferences (§1) + codex display of them (§4)
3. Daily-conversation affinity (§2)
4. Visual pass (§5)
5. Mechanical unlocks (§3) — content-heavy, needs shop/quest assets that partly do not exist yet

## Content still needed regardless

- **More NPCs.** One is not a relationship system. This is the single highest-impact item and
  it is authoring work, not code.
- **More giftable items.** 26 of 28 items have `giftAffinityValue = 0`, including every crop.
- Portraits and bios for any NPC added.
