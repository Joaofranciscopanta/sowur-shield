# Custom Art — Drop Folder

Drop generated sprites here. Claude will organize and wire them.

## Folders

| Folder | What goes here |
|---|---|
| `SoilTiles/` | Watered dirt, plain dirt, planted soil, fertilized soil, dead crop |
| `UI/` | Buttons, panels, icons, frames, any HUD art |
| `Crops/` | New crop growth stages, seed icons, harvest items |
| `Characters/` | NPC portraits, player sprites, animal sprites |

## Import settings (set in Unity after dropping)
- Texture Type: **Sprite (2D and UI)**
- Filter Mode: **Point (no filter)** — critical for pixel art
- Compression: **None**
- Pixels Per Unit: **16** (tiles) or **100** (UI)
- If spritesheet: Sprite Mode = Multiple → Sprite Editor → Slice by cell size

## After dropping files here, tell Claude:
> "Organize the art in SowurShield_Custom"

Claude will move sprites to the correct `Assets/Sprites/` subfolders and wire them.
