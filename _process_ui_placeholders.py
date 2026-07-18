"""
One-off script: crop AI-generated UI placeholders to their content bounding box
(based on alpha channel) and resize to the exact target dimensions specified in
UI_ART_PLACEHOLDERS.md, saving results to _processed_ui/.
"""
from PIL import Image
import os

SRC_DIR = "_incoming_ui"
OUT_DIR = "_processed_ui"

# filename (without ext) -> (target_width, target_height)
TARGETS = {
    "panel_wood_generic":      (512, 512),
    "panel_team_assembler":    (768, 512),
    "panel_victory":           (768, 640),
    "panel_defeat":            (768, 640),
    "button_primary":          (600, 120),
    "button_secondary":        (600, 120),
    "button_danger":           (600, 120),
    "button_small_action":     (480, 96),
    "card_animal":             (1000, 200),
    "frame_portrait_circle":   (256, 256),
    "slot_grid_empty":         (160, 160),
    "slot_combat_empty":       (190, 190),
    "frame_turn_order":        (120, 120),
    "topbar_background":       (1920, 112),
    "turnorder_strip":         (800, 80),
    "frame_decorative_border": (512, 512),
    "icon_notification_bell":  (128, 128),
    "icon_continue_arrow":     (96, 96),
    "slot_inventory":          (160, 160),
}

PADDING = 6  # px of transparent padding to keep around cropped content

def autocrop(img: Image.Image) -> Image.Image:
    alpha = img.split()[-1]
    bbox = alpha.getbbox()
    if bbox is None:
        return img
    l, t, r, b = bbox
    l = max(0, l - PADDING)
    t = max(0, t - PADDING)
    r = min(img.width, r + PADDING)
    b = min(img.height, b + PADDING)
    return img.crop((l, t, r, b))

def process(name: str, target_w: int, target_h: int):
    src_path = os.path.join(SRC_DIR, f"{name}.png")
    out_path = os.path.join(OUT_DIR, f"{name}.png")

    img = Image.open(src_path).convert("RGBA")
    cropped = autocrop(img)

    src_ratio = cropped.width / cropped.height
    target_ratio = target_w / target_h

    # Crop-to-fill: scale so the smaller dimension matches target, then
    # center-crop the overflow on the larger dimension. Ensures the full
    # target rect is covered with no transparent padding (correct for both
    # filled buttons/slots/icons and 9-slice panel borders).
    scale = max(target_w / cropped.width, target_h / cropped.height)
    new_w = max(target_w, round(cropped.width * scale))
    new_h = max(target_h, round(cropped.height * scale))
    resized = cropped.resize((new_w, new_h), Image.LANCZOS)

    left = (new_w - target_w) // 2
    top = (new_h - target_h) // 2
    result = resized.crop((left, top, left + target_w, top + target_h))

    result.save(out_path)
    print(f"{name}: {img.size} -> cropped {cropped.size} -> {result.size}  (ratio src={src_ratio:.3f} target={target_ratio:.3f})")

if __name__ == "__main__":
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, (w, h) in TARGETS.items():
        process(name, w, h)
