# Sowur Shield — AI Sprite Generation Prompts

## Style Reference
Match the style of the existing Sprout Lands assets already in the project:
- **Pixel art**, top-down 2D farming game perspective
- **Tile size**: 16×16 pixels (or 32×32 — match the existing `Tilled_Dirt_v2.png`)
- **Color palette**: earthy browns, warm tans, desaturated muted tones
- **Clean outlines**, no dithering, limited palette per tile
- **No background** — transparent PNG, each tile self-contained
- These tiles use the **dual-grid bitmask system** — each sprite sheet must be a 4×16 grid (64 tiles total, 16px each), or a flat set of individual tiles if you prefer

---

## Sprites Needed

### 1. Watered Tilled Soil (PRIORITY — missing from game)
**File to create:** `Tilled_Dirt_Watered.png`
**Where to drop:** `Assets/Sprites/DirtGround/` (or create `Assets/Sprites/WateredDirt/`)
**What to wire in Unity:** `SoilBlockInteractable.wateredSprite` on each soil tile prefab

**Prompt:**
```
Pixel art top-down farm soil tile, 16x16 pixels, watered/wet tilled dirt.
Dark moist brown earth with shallow horizontal furrow lines (tilled rows),
small dark water droplets or dark wet patches visible on the soil surface,
slightly darker and more saturated than dry tilled dirt.
Transparent background. Sprout Lands style. Clean, no dithering.
Tile must tile seamlessly.
```

**Spritesheet variant (if making a bitmask tileset):**
```
Pixel art 16x16 top-down watered tilled soil tileset, 4 columns x 16 rows
(64 tiles total, 256x256 PNG). Bitmask autotile for Unity dual-grid system.
Same layout as the Sprout Lands Tilled_Dirt_v2.png but darker and wetter —
moist dark earth, visible water sheen, same furrow lines. Muted earthy palette.
Transparent background where no tile is present.
```

---

### 2. Regular / Untilled Soil (plain dirt, before hoeing)
**File to create:** `Plain_Dirt.png`
**Where to drop:** `Assets/Sprites/DirtGround/`
**What to wire in Unity:** `SoilBlockInteractable.regularSprite` on each soil tile prefab

**Prompt:**
```
Pixel art top-down farm soil tile, 16x16 pixels, plain untilled dirt.
Medium brown earth, smooth surface, no furrows, no crops.
Small subtle texture variation (pebbles, slight color variation).
Transparent background. Sprout Lands style. Clean pixel art, no dithering.
Seamlessly tileable.
```

---

### 3. Crop Bed / Planted Soil (tilled with seed, pre-sprout)
**File to create:** `Tilled_Dirt_Planted.png` (optional — used as the "with crop" background before first growth stage)
**Where to drop:** `Assets/Sprites/DirtGround/`

**Prompt:**
```
Pixel art top-down farm soil tile, 16x16 pixels, tilled dirt with a small
planted mound visible — a tiny raised bump of soil in the center indicating
a seed was just buried. Same brown tilled earth with horizontal furrow lines
as Sprout Lands style. Transparent background. Clean, no dithering.
```

---

### 4. Dead / Wilted Crop (crop that died from lack of water)
**File to create:** `Crop_Dead.png`
**Where to drop:** `Assets/Sprites/Crops/` (or `Assets/Sprites/DirtGround/`)
**What to wire in Unity:** `CropData.deadSprite` (if that field exists, otherwise use as growth stage override)

**Prompt:**
```
Pixel art top-down farm, 16x16 pixels, dead wilted seedling on tilled dirt.
Small drooping brown plant stem, dried curled leaves, yellowed and dead.
Sitting on dark tilled soil background. Sprout Lands pixel art style.
Transparent background. Muted brown/yellow palette. Clean pixels, no dithering.
```

---

### 5. Fertilized Soil (bonus — for future fertilizer feature)
**File to create:** `Tilled_Dirt_Fertilized.png`
**Where to drop:** `Assets/Sprites/DirtGround/`

**Prompt:**
```
Pixel art top-down farm soil tile, 16x16 pixels, tilled dirt with light
visible fertilizer — small greenish or dark compost specks on the surface,
slightly richer/darker earth color than plain tilled soil. Same horizontal
furrow lines, Sprout Lands style. Transparent background. Clean pixel art.
```

---

## After Generating — How to Set Up in Unity

1. **Drop the PNG** into the correct `Assets/Sprites/` subfolder
2. In Unity **Inspector**:
   - `Texture Type`: **Sprite (2D and UI)**
   - `Sprite Mode`: **Single** (for individual tiles) or **Multiple** (for spritesheets)
   - `Filter Mode`: **Point (no filter)** — critical for pixel art
   - `Compression`: **None**
   - `Pixels Per Unit`: **16** (match existing tiles)
3. If spritesheet: click **Sprite Editor → Slice → Grid by Cell Size → 16x16**
4. Hit **Apply**
5. **Wire sprites** in Unity:
   - Select a `SoilBlock` prefab or scene object
   - Drag the new sprite into `SoilBlockInteractable → regularSprite / tilledSprite / wateredSprite`
6. If using a bitmask tileset: create a new `Tile` asset in the DirtGround folder, set the sprite, and assign to the DualGridTilemap rule tiles

---

## Existing Sprites (already in project — do NOT regenerate)
- `Assets/Sprites/DirtGround/Tilled_Dirt_v2_*.asset` — tilled (dry) dirt, 71 bitmask slices from `Tilled_Dirt_v2.png`
- `Assets/Sprites/GrassGround/Grass.png` — base grass ground
- `Assets/Assets Importados/Sprout Lands - Sprites - premium pack/Tilesets/ground tiles/` — full Sprout Lands tileset for style reference
