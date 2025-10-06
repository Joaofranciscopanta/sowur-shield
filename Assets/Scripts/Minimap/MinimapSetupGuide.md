# Minimap System - Complete Setup Guide

## 📋 Overview

This minimap system provides a professional, feature-rich minimap for your 2D Unity game with three display states:
- **Normal**: Top-right corner, 100% opacity
- **Semi-Transparent**: Top-right corner, 50% opacity
- **Fullscreen**: Center screen with zoom/pan controls

## 🎯 Features Implemented

✅ **Three-State Toggle System** (M key)
✅ **Smooth Transitions** with DOTween
✅ **Fullscreen Zoom** (Mouse scroll or +/- keys)
✅ **Fullscreen Pan** (Arrow keys or mouse drag)
✅ **Player Following** in corner modes
✅ **Player Movement Control** (disabled in fullscreen)
✅ **UIManager Integration** (ESC key support)
✅ **Performance Optimized** (RenderTexture rendering)
✅ **Icon System** for NPCs, SellBox, Beds, etc.

---

## 🔧 Step-by-Step Setup

### Step 1: Create Minimap Layer

1. Go to **Edit → Project Settings → Tags and Layers**
2. Add a new layer called **"Minimap"**
3. Note the layer number (e.g., Layer 8)

### Step 2: Setup Minimap Camera

1. Create a new GameObject: **Right-click in Hierarchy → Create Empty**
2. Name it: **"MinimapCamera"**
3. Add Camera component: **Add Component → Camera**
4. Add MinimapCamera script: **Add Component → MinimapCamera**

**Configure the Camera:**
- Projection: **Orthographic**
- Orthographic Size: **10** (adjustable)
- Culling Mask: **Select "Minimap" layer only**
- Background: **Solid Color** (dark gray/black)
- Depth: **10** (render after main camera)

**Configure MinimapCamera Script:**
- Player Target: **Drag your Player GameObject here**
- Default Orthographic Size: **10**
- Minimap Layers: **Minimap layer**
- Camera Distance: **100** (distance in front of objects on Z axis for 2D)
- Follow Player: **✓ Checked**
- Render Texture Size: **1024**

### Step 3: Create Minimap UI

1. **Create Canvas** (if you don't have one):
   - Right-click Hierarchy → **UI → Canvas**
   - Canvas Scaler: **Scale with Screen Size**
   - Reference Resolution: **1920x1080**

2. **Create Minimap Panel:**
   - Right-click Canvas → **UI → Panel**
   - Name it: **"MinimapPanel"**
   - Add **CanvasGroup** component

3. **Configure MinimapPanel RectTransform:**
   - Anchors: **Top-Right** (1, 1, 1, 1)
   - Pivot: **Top-Right** (1, 1)
   - Position: **(-100, -100, 0)**
   - Size: **(200, 200)**

4. **Add RawImage for Minimap Display:**
   - Right-click MinimapPanel → **UI → Raw Image**
   - Name it: **"MinimapImage"**
   - Stretch to fill parent:
     - Anchors: **(0, 0, 1, 1)**
     - Offsets: **All 0**

5. **Optional: Add Border:**
   - Right-click MinimapPanel → **UI → Image**
   - Name: **"Border"**
   - Set as first child (render behind RawImage)
   - Color: White or desired border color
   - Use Outline or create custom border sprite

6. **Optional: Add Info Panel (for fullscreen mode):**
   - Right-click MinimapPanel → **UI → Panel**
   - Name: **"InfoPanel"**
   - Add TextMeshProUGUI elements for:
     - **ZoomText**: Shows current zoom level
     - **CoordinatesText**: Shows current map position
     - **StateText**: Shows current mode
   - Initially set InfoPanel to **inactive**

7. **Optional: Add Player Marker:**
   - Right-click MinimapPanel → **UI → Image**
   - Name: **"PlayerMarker"**
   - Size: **(10, 10)**
   - Color: **Green**
   - Use a dot/arrow sprite

### Step 4: Setup Minimap Controller

1. **Create MinimapController GameObject:**
   - Right-click Hierarchy → **Create Empty**
   - Name: **"MinimapController"**
   - Add **MinimapController** script
   - Add **MinimapUI** script

2. **Configure MinimapController:**
   - Minimap Camera: **Drag MinimapCamera GameObject**
   - Minimap UI: **Drag MinimapUI component (or auto-finds)**
   - Player: **Drag Player GameObject**
   - Semi Transparent Opacity: **0.5**
   - Zoom Levels: **0.5, 1, 2** (can customize)
   - Pan Speed: **10**
   - Mouse Pan Sensitivity: **1**
   - Transition Duration: **0.3**
   - Transition Ease: **InOutQuad**

3. **Configure MinimapUI:**
   - Minimap Panel: **Drag MinimapPanel RectTransform**
   - Minimap Image: **Drag MinimapImage RawImage**
   - Canvas Group: **Auto-assigned or drag CanvasGroup**
   - Normal Position: **(-100, -100)**
   - Normal Size: **(200, 200)**
   - Fullscreen Size: **(800, 800)** or 80% of your screen
   - Info Panel: **Drag InfoPanel** (if created)
   - Zoom Text: **Drag ZoomText** (if created)
   - Coordinates Text: **Drag CoordinatesText** (if created)
   - State Text: **Drag StateText** (if created)
   - Player Marker: **Drag PlayerMarker** (if created)
   - Player Marker Color: **Green**

### Step 5: Configure Input Actions

#### Option A: Manual Input Actions Setup

1. Open your **PlayerControls.inputactions** file
2. Create a new Action Map called **"Minimap"** or add to existing map
3. Add the following actions:

**Toggle Minimap:**
- Name: `ToggleMinimap`
- Binding: Keyboard **M**
- Action Type: Button

**Zoom In:**
- Name: `ZoomIn`
- Binding: Keyboard **Equals (=)** or **Mouse Scroll Up**
- Action Type: Button

**Zoom Out:**
- Name: `ZoomOut`
- Binding: Keyboard **Minus (-)** or **Mouse Scroll Down**
- Action Type: Button

**Pan Controls:**
- Name: `PanUp`
- Binding: Keyboard **Up Arrow**
- Action Type: Button

- Name: `PanDown`
- Binding: Keyboard **Down Arrow**
- Action Type: Button

- Name: `PanLeft`
- Binding: Keyboard **Left Arrow**
- Action Type: Button

- Name: `PanRight`
- Binding: Keyboard **Right Arrow**
- Action Type: Button

4. **Generate C# Class** from the PlayerControls.inputactions file
5. **Assign Input Action References** in MinimapController:
   - Toggle Minimap Action: **Select ToggleMinimap**
   - Zoom In Action: **Select ZoomIn**
   - Zoom Out Action: **Select ZoomOut**
   - Pan Up/Down/Left/Right Actions: **Select respective actions**

#### Option B: Fallback Keyboard Input

If you prefer not to use Input Actions, the MinimapController has fallback keyboard support:
- **M key**: Toggle minimap state
- **Arrow keys**: Pan in fullscreen mode
- **Mouse scroll**: Zoom in fullscreen mode
- **+/- keys**: Zoom in fullscreen mode (if Input Actions configured)

### Step 6: Add Minimap Icons to GameObjects (Optional)

For objects you want to appear on the minimap (NPCs, SellBox, Beds, etc.):

1. Select the GameObject (e.g., SellBox)
2. Add **MinimapIcon** component
3. Configure:
   - Icon Type: **Select appropriate type** (SellBox, NPC, Bed, etc.)
   - Icon Color: **Auto-set by type or customize**
   - Icon Size: **1** (adjust for visibility)
   - Always Visible: **✓ Checked** (or set visibility range)
4. **Important**: The MinimapIcon will automatically create a child object on the Minimap layer

**Recommended Icon Setup:**
- **Player**: Add to Player GameObject, Icon Type: Player, Color: Green
- **NPCs**: Add to each NPC, Icon Type: NPC, Color: Blue
- **SellBox**: Add to SellBox, Icon Type: SellBox, Color: Yellow
- **Beds**: Add to bed objects, Icon Type: Bed, Color: Orange
- **Crop Fields**: Add to farm areas, Icon Type: CropField, Color: Light Green

### Step 7: Layer Configuration for Visibility

**Objects that should appear on minimap:**
1. Make sure the actual game objects are on their normal layers (Default, Player, etc.)
2. The MinimapIcon component will create a child object on the Minimap layer
3. Only objects with children on the Minimap layer will be visible on the minimap

**Alternative approach (for terrain/backgrounds):**
1. Duplicate your tilemap/background objects
2. Set duplicates to the Minimap layer
3. Simplify visuals for performance (lower resolution sprites)
4. Position at same location as originals

---

## 🎮 Usage & Controls

### Keyboard Controls

**M Key**: Cycle through minimap states
- First press: **Semi-Transparent** (50% opacity)
- Second press: **Fullscreen** (zoom/pan enabled)
- Third press: **Back to Normal**

**In Fullscreen Mode:**
- **Arrow Keys**: Pan the map
- **Mouse Scroll**: Zoom in/out
- **Mouse Drag** (left-click): Pan the map
- **+/- Keys**: Zoom in/out (if Input Actions configured)
- **ESC or M**: Exit fullscreen mode

### State Behaviors

| State | Opacity | Position | Size | Player Movement | Zoom/Pan |
|-------|---------|----------|------|----------------|----------|
| Normal | 100% | Top-Right | 200x200 | Enabled | Disabled |
| Semi-Transparent | 50% | Top-Right | 200x200 | Enabled | Disabled |
| Fullscreen | 100% | Center | 800x800 | **Disabled** | Enabled |

---

## 🔍 Testing Checklist

After setup, test the following:

- [ ] Minimap visible in top-right corner on game start
- [ ] Press M → Minimap becomes semi-transparent
- [ ] Press M again → Minimap goes fullscreen (center screen)
- [ ] Press M again → Returns to normal
- [ ] In fullscreen: Mouse scroll zooms in/out smoothly
- [ ] In fullscreen: Arrow keys pan the map
- [ ] In fullscreen: Mouse drag pans the map
- [ ] In fullscreen: Player cannot move
- [ ] Press ESC in fullscreen → Closes minimap to normal mode
- [ ] Camera follows player in normal/semi-transparent modes
- [ ] Icons appear for objects with MinimapIcon component
- [ ] Player marker shows on minimap (if configured)
- [ ] Smooth transitions between all states
- [ ] No performance issues

---

## 🐛 Troubleshooting

### Minimap is black/not showing anything
**Solution**:
- Check that MinimapCamera has Minimap layer in Culling Mask
- Ensure objects have MinimapIcon components with child on Minimap layer
- Verify RenderTexture is assigned to MinimapImage

### Minimap doesn't follow player
**Solution**:
- Check Player reference is assigned in MinimapCamera
- Verify "Follow Player" is checked in MinimapCamera
- Ensure player GameObject has the "Player" tag

### M key doesn't toggle
**Solution**:
- Verify Input Action Reference is assigned in MinimapController
- Check Input Actions are enabled in PlayerControls.inputactions
- Regenerate C# class from .inputactions file
- Fallback: Script uses Keyboard.current, should work without Input Actions

### Player can still move in fullscreen
**Solution**:
- Ensure PlayerMove component has public DisableMovement()/EnableMovement() methods
- Check Player reference is assigned in MinimapController

### Icons not appearing
**Solution**:
- Verify Minimap layer exists and is assigned in MinimapCamera
- Check MinimapIcon created child object on correct layer
- Ensure icon sprites are assigned or default sprites are visible

### Transitions are jerky
**Solution**:
- Install DOTween (should already be in project)
- Check Transition Duration in MinimapController (0.3 recommended)
- Verify no other scripts are modifying minimap panel

### ESC key doesn't close fullscreen
**Solution**:
- Verify MinimapController is registered with UIManager
- Check UIManager.Instance is not null
- Ensure IUIWindow interface is properly implemented

---

## ⚙️ Customization Options

### Changing Minimap Size
```csharp
// In MinimapUI component
normalSize = new Vector2(250, 250); // Larger corner minimap
fullscreenSize = new Vector2(1000, 1000); // Larger fullscreen
```

### Changing Zoom Levels
```csharp
// In MinimapController component
zoomLevels = new float[] { 0.25f, 0.5f, 1f, 1.5f, 2f }; // 5 zoom levels
```

### Changing Transition Speed
```csharp
// In MinimapController component
transitionDuration = 0.5f; // Slower, more dramatic
transitionEase = Ease.OutElastic; // Bouncy effect
```

### Custom Icon Colors
```csharp
// On MinimapIcon component via script
minimapIcon.SetIconColor(Color.red);
minimapIcon.SetIconSize(2f); // Larger icon
minimapIcon.Flash(2f); // Draw attention
```

### Different Opacity Levels
```csharp
// In MinimapController component
semiTransparentOpacity = 0.3f; // More transparent
// or 0.7f for less transparent
```

---

## 🎨 Visual Customization

### Border Styling
1. Select Border Image in MinimapPanel
2. Assign custom border sprite with 9-slice
3. Adjust border width and color

### Background Styling
1. Adjust MinimapCamera background color
2. Add gradient overlay to MinimapImage
3. Add vignette effect for professional look

### Player Marker Customization
1. Replace circle sprite with arrow/triangle
2. Add rotation to point in player's facing direction
3. Add glow/outline for better visibility

---

## 📊 Performance Notes

### Optimizations Included:
- **RenderTexture caching** - Renders once per frame
- **Culling mask** - Only renders Minimap layer
- **Conditional updates** - Player marker updates only when visible
- **Tween pooling** - DOTween reuses tween instances
- **Layer-based rendering** - Minimal draw calls

### Performance Tips:
- Keep minimap icons simple (low-poly sprites)
- Use 512x512 or 1024x1024 RenderTexture (not 4K)
- Limit number of always-visible icons
- Use visibility range for distant objects
- Consider disabling minimap updates when game is paused

---

## 🔗 Integration with Existing Systems

### UIManager Integration
The minimap automatically registers as an `IUIWindow`:
- Window Name: "Minimap"
- Priority: 5 (between GameMenu and Inventory)
- Can close with ESC: Yes
- Won't conflict with SellBox, Inventory, or Game Menu

### PlayerMove Integration
Automatically disables player movement in fullscreen mode:
- Calls `PlayerMove.DisableMovement()` on fullscreen enter
- Calls `PlayerMove.EnableMovement()` on fullscreen exit
- No changes needed to existing PlayerMove script

### Input System Integration
Works with your existing PlayerControls.inputactions:
- Add minimap actions to existing action map
- Or create separate "Minimap" action map
- Fallback keyboard input if Input Actions not configured

---

## 📝 Script Reference

### MinimapController.cs
**Main controller managing states and input**
- Public Methods:
  - `ToggleMinimapState()` - Cycle to next state
  - `SetMinimapState(MinimapState state)` - Set specific state
  - `ZoomIn()` / `ZoomOut()` - Manual zoom control
  - `IsFullscreen()` - Check if in fullscreen mode

### MinimapCamera.cs
**Camera following and rendering**
- Public Methods:
  - `SetFollowPlayer(bool follow)` - Enable/disable following
  - `SetZoomLevel(float zoomLevel)` - Set zoom (0.5 = close, 2 = far)
  - `SetPanOffset(Vector3 offset)` - Manual pan in fullscreen
  - `GetRenderTexture()` - Get the minimap texture
  - `ForceUpdate()` - Immediate position update

### MinimapUI.cs
**UI display and transitions**
- Public Methods:
  - `TransitionToNormal/SemiTransparent/Fullscreen()` - State transitions
  - `UpdateZoomIndicator(float zoom)` - Update zoom display
  - `UpdateCoordinates(Vector3 pos)` - Update position display
  - `SetPlayerMarkerVisible(bool visible)` - Show/hide player marker

### MinimapIcon.cs
**Icon system for objects**
- Public Methods:
  - `SetIconType(MinimapIconType type)` - Change icon type
  - `SetIconColor(Color color)` - Change color
  - `SetIconSize(float size)` - Change size
  - `SetVisible(bool visible)` - Manual visibility control
  - `Flash(float duration)` - Attention effect
  - `Pulse(float duration)` - Size pulse effect

---

## 🎯 Example Usage Scenarios

### Scenario 1: Show Quest Location
```csharp
// Add to quest marker GameObject
MinimapIcon icon = questMarker.AddComponent<MinimapIcon>();
icon.SetIconType(MinimapIconType.Quest);
icon.SetAlwaysVisible(true);
icon.Flash(3f); // Draw attention
```

### Scenario 2: Temporarily Highlight NPC
```csharp
// When NPC has important dialogue
npcMinimapIcon.SetIconColor(Color.yellow);
npcMinimapIcon.Pulse(2f);
```

### Scenario 3: Open Minimap to Fullscreen Programmatically
```csharp
// From another script
MinimapController.Instance.SetMinimapState(MinimapState.Fullscreen);
```

### Scenario 4: Change Zoom Level via Script
```csharp
// Set specific zoom level
var minimapCamera = FindObjectOfType<MinimapCamera>();
minimapCamera.SetZoomLevel(0.5f); // Zoom in close
```

---

## 📚 Additional Resources

- **DOTween Documentation**: http://dotween.demigiant.com/documentation.php
- **Unity Input System**: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- **RenderTexture Guide**: https://docs.unity3d.com/Manual/class-RenderTexture.html

---

## ✅ Completion Checklist

Setup is complete when:
- [x] All 4 scripts created in `Assets/Scripts/Minimap/`
- [ ] Minimap layer created in project
- [ ] MinimapCamera GameObject configured
- [ ] MinimapPanel UI created in Canvas
- [ ] MinimapController GameObject setup
- [ ] Input Actions added and assigned
- [ ] At least one test icon added (e.g., to Player)
- [ ] All functionality tested and working
- [ ] Performance verified (no lag)

---

**🎉 Congratulations!** Your minimap system is now fully integrated and ready to use!

For issues or questions, refer to the Troubleshooting section or check the inline code documentation.
