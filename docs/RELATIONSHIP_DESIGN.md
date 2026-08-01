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
2. Gift preferences (§1) + codex display of them (§4) — **done 2026-08-01** (`60a445f`)
3. Daily-conversation affinity (§2) — **done 2026-08-01** (`60a445f`)
4. Visual pass (§5) — **done 2026-08-01** (`af66b87`): wood panel sprite, gold NPC name, framed
   portrait, themed close button, plus the tier ticks and dimmed locked rows from the earlier
   commit. Padding went 12px → 40/36 and the panel 520 → 600 wide: the frame art is ~40px per
   side at this width, and the old padding laid content on the border.
   **Not yet verified in Play Mode with a screenshot** — the layout numbers are reasoned from
   the frame thickness, not measured against a render
5. Mechanical unlocks (§3) — **not started**; content-heavy, needs shop/quest assets that
   partly do not exist yet

### What shipped in `60a445f`

- Multipliers 2.5x / 1.5x / 1x / −1x, with Neutral pinned at exactly 1x by test so existing
  gift content is not silently rebalanced
- Preferences matched on `itemName`, never `GetDisplayName()` — the latter is localized, and
  matching on it would break every preference the moment the player switched language
- Discovery record: the codex shows what the player *learned*, and `GetDiscoveredReaction`
  deliberately has no fallback to the real answer
- Locked lore tiers rendered dimmed with their requirement, plus a "Codex (2/4)" count
- `TierThresholds` is now the single source for both the labels and the bar ticks
- **Giftable items 2 → 16 of 28.** The gift panel was nearly empty before this; it was the
  quiet reason the whole system felt inert

### Testing note worth keeping

`GiftPreferenceTests` initially passed against a *deliberately broken* `GetDiscoveredReaction`,
because a bare test NPC has a null `conversationMemory` and the method returned before reaching
the bug. The fixture now builds a real `ConversationMemory`, bypassing `Awake` (which calls
`DontDestroyOnLoad` and throws outside Play Mode). Re-breaking the method after that fix did
produce a red test. **A green test against known-broken code is a broken test**, and the only
way to notice is to break the code on purpose.

## Content status (updated 2026-08-01)

- ~~**More NPCs.** One is not a relationship system.~~ **Done**: 9 NPCs now, via
  `Tools > NPC > Populate Village (Placeholders)`. Adding another means adding one entry to the
  table in `VillagerPopulationTool.BuildCast()` and re-running it — it is idempotent.
- ~~**More giftable items.**~~ **Done**: 16 of 28 items are giftable.
- **Portraits are still missing for every NPC**, including Maren. `RelationshipUI` falls back to
  a flat wood rectangle, so all nine codex entries show the same blank portrait. This is the
  most visible remaining gap and it needs art.
- **Villager sprites are all identical** by design of the placeholder pass — eight villagers
  render as the same 32×32 figure.

## Known limitation of the placeholder cast

Each villager has one dialogue tree with two lines (greeting → farewell). That is enough to
exercise the dialogue UI, the injected gift/relationship choices and the daily-talk award, but
it is not enough for the relationship *tiers* to feel different: Maren is still the only NPC
whose dialogue changes with affinity (she has Friend/Beloved/Seasonal trees). Giving the
villagers tier-specific trees is the natural next content step.
