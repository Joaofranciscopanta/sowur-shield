# Sowur Shield — UI Art Placeholder Spec (AI-Generated)

> Purpose: list every UI element that currently uses Unity's default white/gray sprite (or no
> sprite at all) and needs a custom "cozy" 2D art asset. Each entry has a ready-to-paste AI
> image-generation prompt, target pixel size, file format, and the exact import folder/settings.
>
> **Style baseline** (apply to every prompt below): warm hand-painted/painterly cozy farming-sim
> UI, flat colors with soft shading, 2-4px rounded corners, visible wood-grain texture on frames,
> palette = cream `#F7F2E8`, tan `#EFE3C0`, wood light `#A66A3F`, wood mid `#8B5A2B`, wood dark
> `#6B4423`, gold accent `#F4D35E`/`#FFD166`, positive green `#81C784`, warning orange `#FFB74D`,
> negative red `#E57373`, text `#2D2A26`. No photorealism, no gradients/noise that clash with flat
> game art, transparent background (PNG, alpha channel) unless noted "opaque background ok".

---

## How to use this doc

1. Generate each image with the prompt given (Midjourney / DALL-E / Stable Diffusion / etc.),
   at the **Generate size** (or the nearest supported size, then resize down — never up).
2. Export as **PNG-32 (RGBA)** unless stated otherwise.
3. Drop the file into the **Import path** listed.
4. In Unity, select the file → Inspector → set:
   - `Texture Type: Sprite (2D and UI)`
   - `Sprite Mode`: `Single` (unless noted `9-slice` — then also set `Border` values as given)
   - `Pixels Per Unit`: `100` (UI sprites are scaled by RectTransform, PPU mostly irrelevant but
     keep consistent)
   - `Filter Mode: Bilinear` (or `Point` only if you want pixel-art style — this game is painterly,
     so Bilinear)
   - `Compression: None` or `High Quality` for UI crispness
5. Assign to the `Image` component's `Source Image` field on the GameObject named in each entry.
6. For panels marked **9-slice**, also set `Image.Image Type = Sliced` after assigning the sprite,
   so it stretches without distorting corners.

---

## Group 1 — Panel Backgrounds (9-slice frames)

These are large background frames for full panels/windows. All should be **9-slice** so they
stretch cleanly to any panel size.

### 1.1 Generic Wood Panel (reusable — main menu, pause menu, settings, confirmation, grid panel)
- **Used by**: `SettingsPanel` (1366×628), `ConfirmationPanel` (200×130), `MenuPanel`,
  `SlotPickerPanel`, `GridPanel`, `SaveSlotPanel`
- **Generate size**: 512×512 px (square, will be 9-sliced and stretched to any panel size)
- **Border (9-slice)**: 32px on all 4 sides
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI panel background for a cozy farming game, square frame, light cream parchment
  > center (#F7F2E8), thick rounded wood-plank border (#8B5A2B mid-tone wood with visible grain
  > lines, #6B4423 darker wood shading at the inner edge for depth), 4px gold (#F4D35E) inner
  > highlight line just inside the wood border, soft drop shadow on outer edge, no characters, no
  > icons, flat painterly shading, square canvas, transparent corners outside the rounded frame"
- **Import path**: `Assets/Sprites/UI/Panels/panel_wood_generic.png`
- **Apply to**: `SettingsPanel`, `ConfirmationPanel`, `MenuPanel`, `SlotPickerPanel`, `GridPanel`,
  `SaveSlotPanel`, `AnimalSelectionPanel` (set `Image Type = Sliced`, border 32px)

### 1.2 Team Assembler Panel (larger, slightly fancier — has grid + roster inside)
- **Used by**: `AssemblerPanel` (Team Assembler UI in SampleScene)
- **Generate size**: 768×512 px landscape
- **Border (9-slice)**: 40px sides/top, 56px bottom (extra room for button row)
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI background panel for a cozy farming-game team-builder screen, wide landscape
  > rectangle, warm tan parchment center (#EFE3C0), thick decorative wood-plank border (#8B5A2B
  > with visible grain), gold (#F4D35E) corner ornament accents in all four corners (small carved
  > leaf/acorn motif), darker wood shadow (#6B4423) along the bottom edge, no characters, no text,
  > flat painterly style, transparent outside the frame"
- **Import path**: `Assets/Sprites/UI/Panels/panel_team_assembler.png`
- **Apply to**: `AssemblerPanel` (set `Image Type = Sliced`, border 40/40/40/56)

### 1.3 Victory / Defeat Result Panel
- **Used by**: `VictoryPanel`, `DefeatPanel` (CombatScene)
- **Generate size**: 768×640 px
- **Border (9-slice)**: 40px all sides
- **Format**: PNG-32, transparent background
- **Two variants needed** (same composition, different accent color):
  - **Victory**: gold/green accent
  - **Defeat**: muted red/gray accent
- **Prompt (Victory)**:
  > "Flat 2D UI background frame for a 'Victory' results screen in a cozy farming auto-battler,
  > tall rectangle, warm cream parchment center (#F7F2E8), ornate wood-plank border (#A66A3F)
  > with gold (#F4D35E) ribbon banner motif across the top edge, small laurel-leaf accents in
  > gold at top corners, soft warm glow at center, flat painterly shading, no text, no
  > characters, transparent outside the frame"
- **Prompt (Defeat)**:
  > "Flat 2D UI background frame for a 'Defeat' results screen in a cozy farming auto-battler,
  > tall rectangle, muted gray-tan parchment center (#E3DCCB), wood-plank border (#6B4423,
  > slightly desaturated/darker than normal), tattered cloth ribbon motif in muted red (#E57373)
  > across the top edge, subtle dark vignette at edges, flat painterly shading, no text, no
  > characters, transparent outside the frame"
- **Import paths**:
  - `Assets/Sprites/UI/Panels/panel_victory.png`
  - `Assets/Sprites/UI/Panels/panel_defeat.png`
- **Apply to**: `VictoryPanel` → `panel_victory.png`, `DefeatPanel` → `panel_defeat.png`
  (both `Image Type = Sliced`, border 40px)

---

## Group 2 — Buttons (3-state: normal / hover / pressed, OR single sprite + Unity color tint)

> Recommendation: generate **one** button sprite per shape/size below, then use Unity's Button
> component `Colors` tint (not separate sprites) for hover/pressed states — simpler and keeps
> visual consistency. If you want hand-painted hover/pressed art instead, generate 3 variants per
> prompt by appending "(slightly lighter/raised)" for hover and "(slightly darker/pressed in)"
> for pressed.

### 2.1 Primary Action Button (large — New Game, Start Battle, Continue, Confirm/Yes)
- **Used by**: `NewGameButton`, `ContinueButton`, `LoadGameButton`, `StartBattleButton`,
  `YesButton` (×2), `ApplyButton`, `ConfirmButton`
- **Generate size**: 600×120 px (will be displayed at 300×60, generate 2x for crispness)
- **Border (9-slice)**: 16px all sides
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI button for a cozy farming game, wide rounded rectangle, warm gold gradient-free
  > fill (#F4D35E center, #FFD166 edges blended flat — not a smooth gradient, more like two flat
  > bands), thick rounded wood-brown border (#8B5A2B), subtle 2px darker bottom edge for a raised
  > 3D button look (#6B4423), no text, no icons, flat painterly shading, transparent background
  > outside the button shape"
- **Import path**: `Assets/Sprites/UI/Buttons/button_primary.png`
- **Apply to**: all primary buttons listed above (`Image Type = Sliced`, border 16px)

### 2.2 Secondary/Neutral Button (Back, Cancel, Settings, Quit, No)
- **Used by**: `BackButton` (×2 in MainMenu), `CancelButton` (multiple), `NoButton`,
  `SettingsButton` (×2), `QuitButton`, `QuitToMenuButton`
- **Generate size**: 600×120 px (displayed at ~300×60 or 160×30/45, generate 2x largest)
- **Border (9-slice)**: 16px all sides
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI button for a cozy farming game, wide rounded rectangle, light cream fill
  > (#F7F2E8), thin wood-brown border (#A66A3F), subtle 2px darker bottom edge (#8B5A2B) for a
  > raised button look, no text, no icons, flat painterly shading, transparent background outside
  > the button shape — visually a 'lighter/neutral' counterpart to a gold primary button"
- **Import path**: `Assets/Sprites/UI/Buttons/button_secondary.png`
- **Apply to**: all secondary buttons listed above (`Image Type = Sliced`, border 16px)

### 2.3 Danger/Destructive Button (Clear Grid, Delete Slot)
- **Used by**: `ClearGridButton`, any "Delete Save" button
- **Generate size**: 600×120 px
- **Border (9-slice)**: 16px all sides
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI button for a cozy farming game, wide rounded rectangle, warm muted red fill
  > (#E57373), thin darker red-brown border (#A65252), subtle 2px darker bottom edge for a raised
  > button look, no text, no icons, flat painterly shading, transparent background outside the
  > button shape"
- **Import path**: `Assets/Sprites/UI/Buttons/button_danger.png`
- **Apply to**: `ClearGridButton` (`Image Type = Sliced`, border 16px)

### 2.4 Small Action Button (Retry, Return to Farm, Feed All — combat/results context)
- **Used by**: `RetryBattleButton`, `VictoryRetryButton`, `ReturnToFarmButton` (×2),
  `FeedAllButton`
- **Generate size**: 480×96 px (displayed at 160×30)
- **Border (9-slice)**: 12px all sides
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI button for a cozy farming game, compact rounded rectangle, warm gold fill
  > (#FFD166), thin wood-brown border (#8B5A2B), subtle darker bottom edge for raised look, no
  > text, flat painterly shading, transparent background outside the shape — same family as a
  > larger primary gold button but more compact/squat proportions"
- **Import path**: `Assets/Sprites/UI/Buttons/button_small_action.png`
- **Apply to**: `RetryBattleButton`, `VictoryRetryButton`, `ReturnToFarmButton` (×2),
  `FeedAllButton` (`Image Type = Sliced`, border 12px)

---

## Group 3 — Cards & Slots

### 3.1 Animal Card Frame (Farm roster view)
- **Used by**: `AnimalCard.prefab` root (500×100)
- **Generate size**: 1000×200 px (2x)
- **Border (9-slice)**: 20px all sides
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI card background for a cozy farming game, wide horizontal rounded rectangle,
  > cream fill (#F7F2E8), thin gold border (#F4D35E) with a slightly thicker wood-brown outer
  > edge (#A66A3F), small decorative notch/tab on the left side sized for a circular portrait to
  > sit in, flat painterly shading, no text, no icons, transparent background outside the card
  > shape"
- **Import path**: `Assets/Sprites/UI/Cards/card_animal.png`
- **Apply to**: `AnimalCard` root Image (`Image Type = Sliced`, border 20px)

### 3.2 Animal Portrait Frame (circular, inside AnimalCard)
- **Used by**: `Portrait` inside `AnimalCard.prefab` (80×80) and combat `AnimalCardPrefab`
  Portrait (200×100 area — use the circular frame at 80×80 size, centered)
- **Generate size**: 256×256 px
- **Border (9-slice)**: not needed (single sprite, circular)
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI circular portrait frame for a cozy farming game, simple ring shape, gold
  > (#F4D35E) outer ring with thin wood-brown (#8B5A2B) inner ring, transparent center (so an
  > animal sprite can show through), subtle drop shadow on outer edge, flat painterly style,
  > transparent background"
- **Import path**: `Assets/Sprites/UI/Cards/frame_portrait_circle.png`
- **Apply to**: `Portrait` (in `AnimalCard.prefab` and `Combat/AnimalCardPrefab.prefab`) — place
  as a sibling Image **behind or in front of** the actual animal sprite Image (frame should be a
  separate Image layered over/under the portrait, not replacing it)

### 3.3 Farm Grid Slot (empty cell background)
- **Used by**: `GridSlot.prefab` root (80×80)
- **Generate size**: 160×160 px (2x)
- **Border**: not 9-slice (square, fixed aspect — single sprite ok, or 9-slice with 8px border if
  you want it reusable at other sizes)
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI square slot background for a cozy farming game, rounded square, light tan fill
  > (#EFE3C0), thin wood-brown border (#A66A3F) all around, subtle inner shadow for a recessed
  > 'slot' look, flat painterly shading, no icons, transparent background outside the rounded
  > square"
- **Import path**: `Assets/Sprites/UI/Slots/slot_grid_empty.png`
- **Apply to**: `GridSlot` root Image (`Image Type = Simple` or `Sliced` with border 8px)

### 3.4 Combat Grid Slot (empty cell background — slightly different tone for battle)
- **Used by**: `Combat/GridSlotPrefab.prefab` root (95×95)
- **Generate size**: 190×190 px (2x)
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI square slot background for a cozy farming game's battle grid, rounded square,
  > slightly desaturated tan fill (#E3DCC8), thin wood-brown border (#8B5A2B), small subtle
  > crosshair/target corner marks in each corner (faint gold #F4D35E, very subtle), flat
  > painterly shading, transparent background outside the rounded square"
- **Import path**: `Assets/Sprites/UI/Slots/slot_combat_empty.png`
- **Apply to**: `GridSlotPrefab` root Image (`Image Type = Simple`)

### 3.5 Turn Order Icon Frame
- **Used by**: `TurnOrderIcon.prefab` (30×30)
- **Generate size**: 120×120 px (4x — small icon needs to stay crisp)
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI small circular icon frame for a cozy farming game's turn-order tracker, simple
  > thin gold ring (#F4D35E) on transparent background, minimal, no text, no inner content —
  > just the ring frame that an animal/enemy portrait will be placed inside"
- **Import path**: `Assets/Sprites/UI/Icons/frame_turn_order.png`
- **Apply to**: `TurnOrderIcon` root Image (`Image Type = Simple`)

---

## Group 4 — Bars, Borders, Small Icons

### 4.1 TopBar Background (Farm + Combat)
- **Used by**: `TopBar` (SampleScene) and `TopBar` (CombatScene, currently 0×56 default gray
  `fileID: 10907`)
- **Generate size**: 1920×112 px (2x, full-width banner — will stretch horizontally via 9-slice)
- **Border (9-slice)**: 0px top/bottom, 64px left/right (only horizontal stretch needed)
- **Format**: PNG-32, transparent background OR opaque cream if TopBar should be fully solid
  (recommend transparent, since the canvas background already provides the scene backdrop)
- **Prompt**:
  > "Flat 2D UI horizontal top bar banner for a cozy farming game, wide thin rectangle, warm
  > wood-brown fill (#8B5A2B) with a subtle horizontal wood-grain texture, thin gold (#F4D35E)
  > highlight line along the bottom edge, flat painterly shading, no text, no icons, seamless
  > horizontally tileable left-right, transparent above/below the bar shape if any"
- **Import path**: `Assets/Sprites/UI/Bars/topbar_background.png`
- **Apply to**: `TopBar` (SampleScene and CombatScene) — `Image Type = Sliced`, border
  `(64, 0, 64, 0)` (left, bottom, right, top — Unity order: left/bottom/right/top in the Sprite
  Editor border tool)

### 4.2 Turn Order Strip Background
- **Used by**: `TurnOrderPanel` (CombatScene, currently 0×40 default gray)
- **Generate size**: 800×80 px
- **Border (9-slice)**: 40px left/right, 0px top/bottom
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI horizontal strip background for a turn-order tracker in a cozy farming
  > auto-battler, thin rounded rectangle, light cream fill (#F7F2E8) at 80% opacity feel (slightly
  > translucent look achieved with a pale tone), thin gold border (#F4D35E) top and bottom edges
  > only, flat painterly shading, no icons, transparent background outside the strip"
- **Import path**: `Assets/Sprites/UI/Bars/turnorder_strip.png`
- **Apply to**: `TurnOrderPanel` (`Image Type = Sliced`, border `(40,0,40,0)`)

### 4.3 Generic Decorative Border/Frame Overlay
- **Used by**: `Border` element (Inventory panel), and any panel needing an extra decorative
  inner-frame overlay
- **Generate size**: 512×512 px
- **Border (9-slice)**: 24px all sides
- **Format**: PNG-32, transparent center AND transparent outside — only the frame ring itself is
  opaque
- **Prompt**:
  > "Flat 2D UI decorative inner border frame for a cozy farming game, square ring shape only
  > (fully transparent center and fully transparent outside), thin gold (#F4D35E) inner line,
  > wood-brown (#A66A3F) outer line, small carved corner flourishes (tiny leaf motifs) in all
  > four corners, flat painterly style"
- **Import path**: `Assets/Sprites/UI/Frames/frame_decorative_border.png`
- **Apply to**: `Border` (Inventory), and optionally as an extra Image layered on top of any
  Group 1 panel for extra detail (`Image Type = Sliced`, border 24px)

### 4.4 Notification Icon (bell/exclamation badge)
- **Used by**: `NotificationIcon`
- **Generate size**: 128×128 px
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI notification icon for a cozy farming game, small bell shape, warm gold fill
  > (#F4D35E) with wood-brown outline (#8B5A2B), simple flat painterly style, centered, no
  > background, transparent PNG, slight drop shadow for a 'badge' pop-up feel"
- **Import path**: `Assets/Sprites/UI/Icons/icon_notification_bell.png`
- **Apply to**: `NotificationIcon` (`Image Type = Simple`)

### 4.5 Dialogue "Continue" Indicator
- **Used by**: `ContinueIndicator`
- **Generate size**: 96×96 px
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI small downward-pointing chevron/arrow icon for a cozy farming game's dialogue
  > box, indicates 'press to continue', warm gold fill (#F4D35E), thin wood-brown outline
  > (#8B5A2B), flat painterly style, centered, transparent background — simple and minimal,
  > suitable for a subtle pulsing/bouncing animation"
- **Import path**: `Assets/Sprites/UI/Icons/icon_continue_arrow.png`
- **Apply to**: `ContinueIndicator` (`Image Type = Simple`)

### 4.6 Inventory Item Slot Background (MainMenu "Item Background")
- **Used by**: `Item Background` (MainMenu), and reusable for any generic inventory slot not
  already covered by `EnhancedInventorySlot`
- **Generate size**: 160×160 px (2x for a 64×64 slot, matches `EnhancedInventorySlot` size)
- **Border**: not 9-slice needed (fixed square)
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "Flat 2D UI square inventory slot background for a cozy farming game, rounded square, tan
  > fill (#EFE3C0), thin wood-brown border (#A66A3F), subtle inner shadow for a recessed slot
  > look, flat painterly shading, no icons, transparent background outside the rounded square —
  > visually consistent with the farm grid slot but slightly smaller/simpler"
- **Import path**: `Assets/Sprites/UI/Slots/slot_inventory.png`
- **Apply to**: `Item Background` (MainMenu) and `EnhancedInventorySlot.prefab` background Image
  (`Image Type = Simple`)

---

## Group 5 — Logo / Branding (only if you want to refresh the existing one)

> The current `Logo` in MainMenu already has a custom asset (guid `7a18c45077fcc864aa621cc19bec3f21`).
> Skip this unless you want a redesign — included for completeness.

### 5.1 Game Logo
- **Generate size**: 1024×512 px
- **Format**: PNG-32, transparent background
- **Prompt**:
  > "2D game logo for 'Sowur Shield', a cozy farming simulation game with light auto-battler
  > elements, warm hand-painted style, title text 'Sowur Shield' in a rounded rustic serif font,
  > cream/gold color scheme (#F7F2E8 background tone, #F4D35E accent, #8B5A2B wood-brown text
  > outline), small decorative icon combining a wheat stalk and a small shield, flat painterly
  > illustration style, transparent background"
- **Import path**: `Assets/Sprites/UI/Branding/logo.png` (replace existing Logo sprite reference
  if redesigning)

---

## Summary Table

| # | Asset | Generate Size | Format | 9-Slice Border | Import Path |
|---|-------|---------------|--------|-----------------|--------------|
| 1.1 | Generic Wood Panel | 512×512 | PNG-32 | 32px all | `Sprites/UI/Panels/panel_wood_generic.png` |
| 1.2 | Team Assembler Panel | 768×512 | PNG-32 | 40/40/40/56 | `Sprites/UI/Panels/panel_team_assembler.png` |
| 1.3a | Victory Panel | 768×640 | PNG-32 | 40px all | `Sprites/UI/Panels/panel_victory.png` |
| 1.3b | Defeat Panel | 768×640 | PNG-32 | 40px all | `Sprites/UI/Panels/panel_defeat.png` |
| 2.1 | Primary Button | 600×120 | PNG-32 | 16px all | `Sprites/UI/Buttons/button_primary.png` |
| 2.2 | Secondary Button | 600×120 | PNG-32 | 16px all | `Sprites/UI/Buttons/button_secondary.png` |
| 2.3 | Danger Button | 600×120 | PNG-32 | 16px all | `Sprites/UI/Buttons/button_danger.png` |
| 2.4 | Small Action Button | 480×96 | PNG-32 | 12px all | `Sprites/UI/Buttons/button_small_action.png` |
| 3.1 | Animal Card Frame | 1000×200 | PNG-32 | 20px all | `Sprites/UI/Cards/card_animal.png` |
| 3.2 | Portrait Ring Frame | 256×256 | PNG-32 | n/a | `Sprites/UI/Cards/frame_portrait_circle.png` |
| 3.3 | Farm Grid Slot | 160×160 | PNG-32 | 8px (opt) | `Sprites/UI/Slots/slot_grid_empty.png` |
| 3.4 | Combat Grid Slot | 190×190 | PNG-32 | n/a | `Sprites/UI/Slots/slot_combat_empty.png` |
| 3.5 | Turn Order Icon Frame | 120×120 | PNG-32 | n/a | `Sprites/UI/Icons/frame_turn_order.png` |
| 4.1 | TopBar Background | 1920×112 | PNG-32 | (64,0,64,0) | `Sprites/UI/Bars/topbar_background.png` |
| 4.2 | Turn Order Strip | 800×80 | PNG-32 | (40,0,40,0) | `Sprites/UI/Bars/turnorder_strip.png` |
| 4.3 | Decorative Border | 512×512 | PNG-32 | 24px all | `Sprites/UI/Frames/frame_decorative_border.png` |
| 4.4 | Notification Bell Icon | 128×128 | PNG-32 | n/a | `Sprites/UI/Icons/icon_notification_bell.png` |
| 4.5 | Continue Arrow Icon | 96×96 | PNG-32 | n/a | `Sprites/UI/Icons/icon_continue_arrow.png` |
| 4.6 | Inventory Slot Bg | 160×160 | PNG-32 | n/a | `Sprites/UI/Slots/slot_inventory.png` |
| 5.1 | Logo (optional redesign) | 1024×512 | PNG-32 | n/a | `Sprites/UI/Branding/logo.png` |

**Total: 18 distinct assets** cover all ~42 UI elements identified (many share the same sprite —
e.g. all primary buttons reuse `button_primary.png`).

---

## Folder structure to create

```
Assets/Sprites/UI/
├── Panels/
│   ├── panel_wood_generic.png
│   ├── panel_team_assembler.png
│   ├── panel_victory.png
│   └── panel_defeat.png
├── Buttons/
│   ├── button_primary.png
│   ├── button_secondary.png
│   ├── button_danger.png
│   └── button_small_action.png
├── Cards/
│   ├── card_animal.png
│   └── frame_portrait_circle.png
├── Slots/
│   ├── slot_grid_empty.png
│   ├── slot_combat_empty.png
│   └── slot_inventory.png
├── Icons/
│   ├── frame_turn_order.png
│   ├── icon_notification_bell.png
│   └── icon_continue_arrow.png
├── Bars/
│   ├── topbar_background.png
│   └── turnorder_strip.png
├── Frames/
│   └── frame_decorative_border.png
└── Branding/
    └── logo.png
```

---

## Recommended order of work

1. **Group 2 (Buttons)** — used everywhere, highest visual impact for least effort (4 sprites
   cover ~15 buttons).
2. **Group 1 (Panels)** — second highest impact, makes every screen feel finished (4 sprites).
3. **Group 3 (Cards & Slots)** — directly tied to the Cozy UI Redesign Pass 2 work
   (AnimalMarket/BoardView panels in SampleScene).
4. **Group 4 (Bars/Borders/Icons)** — polish pass, smaller visual impact individually.
5. **Group 5 (Logo)** — optional, only if a full rebrand is wanted.
