"""
One-off script: copy processed UI placeholder PNGs into Assets/Sprites/UI/...
and generate matching .meta files (Texture2D, Sprite, Single mode, with
9-slice border where specified per UI_ART_PLACEHOLDERS.md).
"""
import os
import shutil
import uuid

SRC_DIR = "_processed_ui"
DEST_ROOT = "Assets/Sprites/UI"

# name -> (dest_subfolder, border (left, bottom, right, top))
# border = (0,0,0,0) means no 9-slice (Image Type = Simple)
ASSETS = {
    "panel_wood_generic":      ("Panels",  (32, 32, 32, 32)),
    "panel_team_assembler":    ("Panels",  (40, 56, 40, 40)),
    "panel_victory":           ("Panels",  (40, 40, 40, 40)),
    "panel_defeat":            ("Panels",  (40, 40, 40, 40)),
    "button_primary":          ("Buttons", (16, 16, 16, 16)),
    "button_secondary":        ("Buttons", (16, 16, 16, 16)),
    "button_danger":           ("Buttons", (16, 16, 16, 16)),
    "button_small_action":     ("Buttons", (12, 12, 12, 12)),
    "card_animal":             ("Cards",   (20, 20, 20, 20)),
    "frame_portrait_circle":   ("Cards",   (0, 0, 0, 0)),
    "slot_grid_empty":         ("Slots",   (8, 8, 8, 8)),
    "slot_combat_empty":       ("Slots",   (0, 0, 0, 0)),
    "frame_turn_order":        ("Icons",   (0, 0, 0, 0)),
    "topbar_background":       ("Bars",    (64, 0, 64, 0)),
    "turnorder_strip":         ("Bars",    (40, 0, 40, 0)),
    "frame_decorative_border": ("Frames",  (24, 24, 24, 24)),
    "icon_notification_bell":  ("Icons",   (0, 0, 0, 0)),
    "icon_continue_arrow":     ("Icons",   (0, 0, 0, 0)),
    "slot_inventory":          ("Slots",   (0, 0, 0, 0)),
}

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: {internal_id}
    second: {name}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {border_l}, y: {border_b}, z: {border_r}, w: {border_t}}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: {width}
        height: {height}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: {border_l}, y: {border_b}, z: {border_r}, w: {border_t}}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: {internal_id}
      vertices: []
      indices:
      edges: []
      weights: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      {name}: {internal_id}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def get_png_size(path):
    with open(path, "rb") as f:
        data = f.read(24)
    # PNG IHDR chunk: width/height are big-endian 4-byte ints at offset 16/20
    width = int.from_bytes(data[16:20], "big")
    height = int.from_bytes(data[20:24], "big")
    return width, height


def main():
    for name, (subfolder, border) in ASSETS.items():
        src = os.path.join(SRC_DIR, f"{name}.png")
        dest_dir = os.path.join(DEST_ROOT, subfolder)
        dest_png = os.path.join(dest_dir, f"{name}.png")
        dest_meta = dest_png + ".meta"

        os.makedirs(dest_dir, exist_ok=True)
        shutil.copy2(src, dest_png)

        width, height = get_png_size(dest_png)

        guid = uuid.uuid4().hex
        internal_id = uuid.uuid4().int % (2**63)
        sprite_id = uuid.uuid4().hex

        meta = META_TEMPLATE.format(
            guid=guid,
            name=name,
            internal_id=internal_id,
            sprite_id=sprite_id,
            width=width,
            height=height,
            border_l=border[0],
            border_b=border[1],
            border_r=border[2],
            border_t=border[3],
        )

        with open(dest_meta, "w", newline="\n") as f:
            f.write(meta)

        print(f"{name}: {width}x{height} -> {dest_png}  border={border}  guid={guid}")


if __name__ == "__main__":
    main()
