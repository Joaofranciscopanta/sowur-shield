# Minimap System - Step-by-Step Setup Guide
## Complete Beginner-Friendly Instructions

This guide assumes you're starting from scratch and will walk through **every single step** to get the minimap working in your game.

---

## 📋 Prerequisites Checklist

Before starting, make sure you have:
- [ ] Unity 2021.3 or newer
- [ ] DOTween package installed (already in your project)
- [ ] TextMeshPro package installed (already in your project)
- [ ] Your game scene open (SampleScene or your main game scene)
- [ ] A Player GameObject with the "Player" tag

---

## 🎯 Part 1: Create the Minimap Layer (2 minutes)

### Step 1.1: Open Project Settings
1. In Unity menu bar, click **Edit**
2. Click **Project Settings...**
3. In the Project Settings window, click **Tags and Layers** (left sidebar)

### Step 1.2: Add the Minimap Layer
1. Scroll down to **Layers** section
2. Find the first empty layer (usually Layer 6 or 7)
3. Click on the empty field
4. Type exactly: `Minimap`
5. Press Enter
6. **Remember this layer number!** (e.g., "Layer 6")

![Layer Setup Example]
```
Layers:
  User Layer 6: Minimap  ← Your layer should look like this
```

7. Close the Project Settings window

✅ **Checkpoint**: You should now have a layer named "Minimap" in your project.

---

## 🎥 Part 2: Create the Minimap Camera (5 minutes)

### Step 2.1: Create the Camera GameObject
1. In the **Hierarchy** window, **right-click** in empty space
2. Select **Create Empty**
3. Name it exactly: `MinimapCamera`
4. Select the MinimapCamera GameObject

### Step 2.2: Add Camera Component
1. With MinimapCamera selected, in the **Inspector**, click **Add Component**
2. Type: `Camera`
3. Click **Camera** to add it

### Step 2.3: Configure the Camera Component
With MinimapCamera still selected, configure these settings in Inspector:

**Camera Component Settings:**
1. **Projection**: Click dropdown, select **Orthographic**
2. **Size**: Set to `10`
3. **Culling Mask**:
   - Click **Everything** (or **Mixed...** if already set)
   - Click **Nothing** (unchecks all layers)
   - **ONLY** check the box next to **Minimap** layer
4. **Background**:
   - Type: **Solid Color**
   - Color: Click color box, choose a dark color (like dark gray or black)
5. **Depth**: Set to `10` (renders after main camera)
6. **Target Display**: Leave as **Display 1**

**Transform Settings (FOR 2D XY PLANE GAMES):**
1. **Position**: `(0, 0, -100)` (camera positioned IN FRONT of the world on Z axis)
2. **Rotation**: `(0, 0, 0)` (camera looks FORWARD along Z axis for 2D XY plane)
3. **Scale**: `(1, 1, 1)`

⚠️ **IMPORTANT FOR 2D GAMES**: Your game uses the XY plane (standard Unity 2D). The camera must look FORWARD (rotation 0,0,0), NOT down (rotation 90,0,0)!

### Step 2.4: Add MinimapCamera Script
1. Still with MinimapCamera selected, click **Add Component**
2. Type: `MinimapCamera`
3. Click the script to add it

### Step 2.5: Configure MinimapCamera Script
In the Inspector, you should now see **MinimapCamera (Script)**:

1. **Player Target**:
   - Click the circle/target icon
   - Select your **Player** GameObject from the scene
   - OR drag your Player from Hierarchy to this field

2. **Default Orthographic Size**: `10`
3. **Minimap Layers**:
   - Click dropdown
   - Select **Minimap** layer only
4. **Camera Distance**: `100` (distance in front of objects on Z axis)
5. **Follow Player**: ✓ **Checked**
6. **Follow Smoothness**: `5`
7. **Render Texture Size**: `1024`
8. **Background Color**: Same as Camera background (dark color)

✅ **Checkpoint**: MinimapCamera should be positioned IN FRONT of your world (negative Z), looking forward at the XY plane, only rendering the Minimap layer.

---

## 🖼️ Part 3: Create the Minimap UI (10 minutes)

### Step 3.1: Find or Create Canvas
**Option A: If you already have a Canvas in your scene:**
1. Find it in Hierarchy (usually named "Canvas")
2. Select it
3. Skip to Step 3.2

**Option B: If you need to create a Canvas:**
1. In Hierarchy, **right-click** in empty space
2. Go to **UI** → **Canvas**
3. A Canvas will be created (along with EventSystem - that's normal)
4. Select the Canvas

**Configure Canvas (important):**
1. Find **Canvas Scaler** component in Inspector
2. **UI Scale Mode**: Select **Scale With Screen Size**
3. **Reference Resolution**: `1920 x 1080`
4. **Match**: `0.5` (middle slider position)

### Step 3.2: Create Minimap Panel
1. In Hierarchy, **right-click** on **Canvas**
2. Go to **UI** → **Panel**
3. Name it exactly: `MinimapPanel`
4. Select MinimapPanel

### Step 3.3: Configure MinimapPanel - RectTransform
With MinimapPanel selected, configure **Rect Transform** component:

1. **Anchors** (click the square anchor icon in top-left of Rect Transform):
   - Hold **Shift + Alt** (Windows) or **Shift + Option** (Mac)
   - Click the **top-right** anchor preset (top-right corner of the grid)
   - This sets anchors to (1, 1, 1, 1) and pivot to (1, 1)

2. **Position**:
   - Pos X: `-100`
   - Pos Y: `-100`
   - Pos Z: `0`

3. **Size**:
   - Width: `200`
   - Height: `200`

4. **Scale**: `(1, 1, 1)`

Your minimap panel should now be in the top-right corner of the screen.

### Step 3.4: Add CanvasGroup Component
1. Still with MinimapPanel selected, click **Add Component**
2. Type: `Canvas Group`
3. Click **Canvas Group** to add it
4. Leave all settings at default (Alpha: 1, Interactable: checked, etc.)

### Step 3.5: Create Minimap Image (RawImage)
1. In Hierarchy, **right-click** on **MinimapPanel**
2. Go to **UI** → **Raw Image**
3. Name it exactly: `MinimapImage`
4. Select MinimapImage

### Step 3.6: Configure MinimapImage - RectTransform
With MinimapImage selected:

1. **Anchors** (click anchor icon):
   - Hold **Shift + Alt** (Windows) or **Shift + Option** (Mac)
   - Click **bottom-right** preset (stretches to fill parent)
   - This sets anchors to (0, 0, 1, 1)

2. **All position/offset values should now be**: `0`
   - Left: `0`
   - Top: `0`
   - Right: `0`
   - Bottom: `0`

3. **Raw Image Component**:
   - **Texture**: Leave empty for now (script will assign it)
   - **Color**: White
   - **Material**: None

### Step 3.7: Optional - Add Border
1. In Hierarchy, **right-click** on **MinimapPanel**
2. Go to **UI** → **Image**
3. Name it: `Border`
4. Drag it in Hierarchy to be **above** MinimapImage (first child)
5. Configure:
   - Stretch to fill parent (same as MinimapImage anchors)
   - **Color**: White or any border color you want
   - You can add a border sprite here later

### Step 3.8: Optional - Create Info Panel (for fullscreen mode)
1. **Right-click** on **MinimapPanel**
2. Go to **UI** → **Panel**
3. Name it: `InfoPanel`
4. Configure RectTransform:
   - Anchors: Bottom of parent
   - Position: Bottom of minimap
   - Width: Same as minimap (200)
   - Height: ~50
5. Set **Active** to **OFF** (uncheck the checkbox next to name in Inspector)

**Add Zoom Text:**
1. Right-click **InfoPanel** → **UI** → **Text - TextMeshPro**
2. Name: `ZoomText`
3. Set text to: `Zoom: 1.0x`
4. Configure position to top-left of InfoPanel

**Add State Text:**
1. Right-click **InfoPanel** → **UI** → **Text - TextMeshPro**
2. Name: `StateText`
3. Set text to: `Mode: Normal`
4. Configure position to bottom-left of InfoPanel

(You can skip this section if you don't want the info display)

### Step 3.9: Optional - Create Player Marker
1. **Right-click** on **MinimapPanel** (NOT InfoPanel)
2. Go to **UI** → **Image**
3. Name it: `PlayerMarker`
4. Configure:
   - **Anchors**: Center (0.5, 0.5, 0.5, 0.5)
   - **Position**: (0, 0, 0)
   - **Width**: `10`
   - **Height**: `10`
   - **Color**: Bright green (0, 255, 0)

✅ **Checkpoint**: Your Canvas should now have a MinimapPanel in the top-right corner with MinimapImage, optional InfoPanel, and optional PlayerMarker.

---

## 🎮 Part 4: Create the Minimap Controller (5 minutes)

### Step 4.1: Create MinimapController GameObject
1. In Hierarchy, **right-click** in empty space
2. Select **Create Empty**
3. Name it exactly: `MinimapController`
4. Select it

### Step 4.2: Add MinimapController Script
1. With MinimapController selected, click **Add Component**
2. Type: `MinimapController`
3. Click the script to add it

### Step 4.3: Add MinimapUI Script
1. Still with MinimapController selected, click **Add Component**
2. Type: `MinimapUI`
3. Click the script to add it

### Step 4.4: Configure MinimapController Script
In Inspector, find **MinimapController (Script)** and configure:

**References:**
1. **Minimap Camera**:
   - Drag **MinimapCamera** GameObject from Hierarchy to this field

2. **Minimap UI**:
   - This should auto-fill with the MinimapUI component
   - If not, drag the MinimapController itself (it will find the UI component)

3. **Player**:
   - Drag your **Player** GameObject from Hierarchy to this field

**Input Actions:**
- Leave all empty for now (we'll set these up in Part 5)
- OR you can skip Part 5 and use fallback keyboard input (M key works automatically)

**State Settings:**
1. **Current State**: `Normal`
2. **Semi Transparent Opacity**: `0.5`

**Fullscreen Settings:**
1. **Zoom Levels**:
   - Size: `3`
   - Element 0: `0.5`
   - Element 1: `1`
   - Element 2: `2`
2. **Current Zoom Index**: `1`
3. **Pan Speed**: `10`
4. **Mouse Pan Sensitivity**: `1`

**Transition Settings:**
1. **Transition Duration**: `0.3`
2. **Transition Ease**: `InOutQuad`

**Debug:**
1. **Enable Debug Logs**: ✓ Checked (for testing, uncheck later)

### Step 4.5: Configure MinimapUI Script
In Inspector, find **MinimapUI (Script)** and configure:

**UI References:**
1. **Minimap Panel**:
   - Drag **MinimapPanel** from Hierarchy to this field

2. **Minimap Image**:
   - Drag **MinimapImage** (child of MinimapPanel) to this field

3. **Canvas Group**:
   - This should auto-fill
   - If not, drag the CanvasGroup component from MinimapPanel

**Position Settings:**
1. **Normal Position**: `(-100, -100)`
2. **Normal Size**: `(200, 200)`
3. **Fullscreen Size**: `(800, 800)` (adjust based on your screen size)

**Info Display (if you created InfoPanel):**
1. **Info Panel**: Drag InfoPanel GameObject
2. **Zoom Text**: Drag ZoomText GameObject
3. **State Text**: Drag StateText GameObject
4. **Coordinates Text**: Leave empty (optional feature)

**Player Marker (if you created it):**
1. **Player Marker**: Drag PlayerMarker GameObject
2. **Player Marker Image**: Drag Image component from PlayerMarker
3. **Player Marker Color**: Green (0, 255, 0)
4. **Player Marker Size**: `10`

**Debug:**
1. **Enable Debug Logs**: ✓ Checked (for testing)

✅ **Checkpoint**: MinimapController is configured with all references assigned.

---

## ⌨️ Part 5: Setup Input Actions (10 minutes)

### Option A: Quick Test Without Input Actions (Skip to Part 6)
The system has fallback keyboard support:
- **M key** will work automatically for toggling
- **Arrow keys** work for panning
- **Mouse scroll** works for zooming

You can skip this part and test now if you want!

### Option B: Proper Input System Setup (Recommended)

### Step 5.1: Open PlayerControls Input Actions
1. In **Project** window, navigate to `Assets/` folder
2. Find **PlayerControls.inputactions** file
3. **Double-click** to open it in the Input Actions editor

### Step 5.2: Create Minimap Action Map (or use existing)
**Option 1: Create new Action Map**
1. In Input Actions window, click **+ (plus)** next to "Action Maps"
2. Name it: `Minimap`

**Option 2: Use existing Action Map (Player)**
1. Select your existing `Player` action map
2. We'll add actions here instead

### Step 5.3: Add Toggle Minimap Action
1. Select your action map (Minimap or Player)
2. Click **+ (plus)** next to "Actions"
3. Name the action: `ToggleMinimap`
4. Select the new action
5. In the right panel:
   - **Action Type**: Button
   - **Control Type**: Button

6. Click **+ (plus)** next to `ToggleMinimap` to add a binding
7. Click on **`<No Binding>`**
8. Press the **M** key on your keyboard
9. It should now show: `M [Keyboard]`

### Step 5.4: Add Zoom Actions
**Zoom In:**
1. Click **+ (plus)** next to "Actions"
2. Name: `ZoomIn`
3. Action Type: Button
4. Add binding → Press **Equals/Plus key** (`=` or `+`)

**Zoom Out:**
1. Click **+ (plus)** next to "Actions"
2. Name: `ZoomOut`
3. Action Type: Button
4. Add binding → Press **Minus key** (`-`)

### Step 5.5: Add Pan Actions
**Pan Up:**
1. Click **+ (plus)** next to "Actions"
2. Name: `PanUp`
3. Action Type: Button
4. Add binding → Press **Up Arrow**

**Pan Down:**
1. Name: `PanDown`
2. Add binding → Press **Down Arrow**

**Pan Left:**
1. Name: `PanLeft`
2. Add binding → Press **Left Arrow**

**Pan Right:**
1. Name: `PanRight`
2. Add binding → Press **Right Arrow**

### Step 5.6: Save and Generate C# Class
1. Click **Save Asset** button (top of Input Actions window)
2. Close the Input Actions window
3. Back in Unity, select **PlayerControls.inputactions** in Project window
4. In Inspector, find the **Generate C# Class** section
5. Make sure **Generate C# Class** is ✓ checked
6. Click **Apply** button
7. Wait for Unity to recompile

### Step 5.7: Assign Input Action References
1. Select **MinimapController** GameObject in Hierarchy
2. In Inspector, find **MinimapController (Script)**
3. Find the **Input** section:

**Toggle Minimap Action:**
1. Click the circle icon next to the field
2. In the popup, find your action map (Minimap or Player)
3. Select **ToggleMinimap**

**Zoom In Action:**
1. Click circle icon
2. Select **ZoomIn**

**Zoom Out Action:**
1. Click circle icon
2. Select **ZoomOut**

**Pan Actions:**
1. Repeat for **PanUp**, **PanDown**, **PanLeft**, **PanRight**

✅ **Checkpoint**: All input actions are created and assigned.

---

## 🎨 Part 6: Make Objects Appear on Minimap (5 minutes)

Currently, your minimap will be blank because nothing is on the Minimap layer. Let's fix that!

### Step 6.1: Add Icon to Player
1. Select your **Player** GameObject
2. Click **Add Component**
3. Type: `MinimapIcon`
4. Click to add it

**Configure Player Icon:**
1. **Icon Type**: Select `Player` from dropdown
2. **Icon Color**: Green (auto-set by type)
3. **Icon Size**: `1.5` (make player marker bigger)
4. **Always Visible**: ✓ Checked
5. **Minimap Layer Name**: `Minimap` (should match your layer)

### Step 6.2: Add Icons to NPCs (if you have any)
For each NPC in your scene:
1. Select the NPC GameObject
2. Add **MinimapIcon** component
3. Configure:
   - **Icon Type**: `NPC`
   - **Icon Color**: Blue (auto-set)
   - **Icon Size**: `1`
   - **Always Visible**: ✓ Checked

### Step 6.3: Add Icon to SellBox (if you have one)
1. Select your **SellBox** GameObject
2. Add **MinimapIcon** component
3. Configure:
   - **Icon Type**: `SellBox`
   - **Icon Color**: Yellow (auto-set)
   - **Icon Size**: `1.2`
   - **Always Visible**: ✓ Checked

### Step 6.4: Add Icons to Other Objects
Repeat for any other objects you want on the minimap:
- Beds: Icon Type = `Bed`, Color = Orange
- Buildings: Icon Type = `Building`, Color = Gray
- Quest markers: Icon Type = `Quest`, Color = Magenta

### Step 6.5: Alternative - Add Tilemap to Minimap Layer
If you want the actual game world visible on minimap:

1. Find your **Tilemap** GameObject(s) in Hierarchy
2. You have two options:

**Option A: Duplicate the Tilemap**
1. Right-click Tilemap → **Duplicate**
2. Rename duplicate: `Tilemap_Minimap`
3. Select it
4. In Inspector, find **Layer** dropdown (top-right)
5. Change to **Minimap** layer
6. Optionally: Simplify visuals, reduce sprite quality for performance

**Option B: Create a Simplified Minimap Version**
1. Create a new Tilemap for minimap only
2. Paint with simpler tiles
3. Set layer to **Minimap**

✅ **Checkpoint**: Player and other objects now have MinimapIcon components.

---

## 🧪 Part 7: Testing (5 minutes)

### Step 7.1: Enter Play Mode
1. Click the **Play** button (top center of Unity)
2. Wait for game to start

### Step 7.2: Check Initial State
You should see:
- Minimap in **top-right corner** of screen
- Minimap shows your game world from above
- If you added icons, you should see colored dots/sprites
- Minimap camera following your player

**If minimap is BLACK:**
- Check that MinimapCamera Culling Mask is set to Minimap layer only
- Check that Player has MinimapIcon component
- Check that MinimapIcon created a child object on Minimap layer
- See troubleshooting section below

### Step 7.3: Test State Transitions
**Press M key:**
1. First press: Minimap should fade to **50% opacity** (semi-transparent)
2. Second press: Minimap should **expand to center of screen** (fullscreen)
3. Third press: Minimap should **return to top-right corner** at 100% opacity

**Watch the Console** for debug logs showing state changes.

### Step 7.4: Test Fullscreen Controls
When in fullscreen mode (after pressing M twice):

**Test Zoom:**
1. Scroll mouse wheel up → Map should zoom in (smaller area, bigger objects)
2. Scroll mouse wheel down → Map should zoom out (larger area, smaller objects)

**Test Pan with Arrow Keys:**
1. Press **Up Arrow** → Map should pan upward
2. Press **Down Arrow** → Map should pan downward
3. Press **Left Arrow** → Map should pan left
4. Press **Right Arrow** → Map should pan right

**Test Pan with Mouse:**
1. Click and hold **left mouse button** on minimap
2. Drag mouse → Map should pan in the direction you drag

**Test Player Movement Lock:**
1. Try moving your player with WASD
2. Player should **NOT move** (movement is locked in fullscreen)

**Exit Fullscreen:**
1. Press **M** key → Returns to normal
2. OR press **ESC** key → Returns to normal

### Step 7.5: Test Integration with Other UI
**Test with Inventory:**
1. Open your inventory (if you have one)
2. Try pressing M to open minimap fullscreen
3. Minimap should be blocked (or inventory should close first)

**Test with Game Menu:**
1. Press ESC to open game menu
2. Minimap should remain in corner mode (not interfere)

### Step 7.6: Performance Check
1. Open **Window → Analysis → Profiler** (if you want detailed stats)
2. Play the game
3. Toggle minimap states
4. CPU usage should not spike significantly
5. FPS should remain stable

✅ **Checkpoint**: All features work as expected!

---

## 🐛 Troubleshooting

### Problem: Minimap is completely black/empty

**Solution 1: Check Camera Culling Mask**
1. Select **MinimapCamera** in Hierarchy
2. Check **Camera** component → **Culling Mask**
3. Should ONLY have **Minimap** layer checked
4. If wrong, click dropdown, select Nothing, then check only Minimap

**Solution 2: Check RenderTexture Connection**
1. Select **MinimapCamera**
2. In **MinimapCamera (Script)**, check **Render Texture** field
3. Should show a texture named "MinimapRenderTexture"
4. If not, click Play, then Stop, then Play again (it creates on Awake)

**Solution 3: Check MinimapImage Texture**
1. Enter Play Mode
2. Select **MinimapImage** in Hierarchy
3. In **Raw Image** component, check **Texture** field
4. Should show "MinimapRenderTexture"
5. If empty, check MinimapUI script references

**Solution 4: Add Visible Objects**
1. Make sure Player has **MinimapIcon** component
2. Check that MinimapIcon created a child object
3. Select Player → expand in Hierarchy → should see "Player_MinimapIcon"
4. Select that child → check **Layer** is set to **Minimap**

**Solution 5: Check Camera Position**
1. Select **MinimapCamera**
2. Check Transform → Position should be `(0, 0, -100)` or similar
3. Check Rotation should be `(90, 0, 0)` (pointing down)
4. Camera should be high above the game world

---

### Problem: M key doesn't toggle minimap

**Solution 1: Check Console for Errors**
1. Open Console window (Window → General → Console)
2. Look for red error messages
3. Fix any errors shown

**Solution 2: Use Fallback Keyboard**
1. The system should work without Input Actions
2. Try pressing M key multiple times
3. Check Console for "[MinimapController]" debug messages

**Solution 3: Check Input Action Assignment**
1. Select **MinimapController** in Hierarchy
2. Scroll to **Input** section in Inspector
3. **Toggle Minimap Action** should show an action reference
4. If empty, click circle icon and select ToggleMinimap action

**Solution 4: Regenerate Input Actions**
1. Select **PlayerControls.inputactions** in Project window
2. In Inspector, click **Generate C# Class**
3. Click **Apply**
4. Wait for Unity to recompile
5. Re-assign action references in MinimapController

**Solution 5: Check Action is Enabled**
1. Open **PlayerControls.inputactions**
2. Find ToggleMinimap action
3. Make sure it's not disabled
4. Make sure it has a binding (M key)

---

### Problem: Minimap doesn't follow player

**Solution 1: Check Player Reference**
1. Select **MinimapCamera** in Hierarchy
2. In **MinimapCamera (Script)**, check **Player Target** field
3. Should reference your Player GameObject
4. If empty, drag Player from Hierarchy to this field

**Solution 2: Check Follow Player Setting**
1. Select **MinimapCamera**
2. In **MinimapCamera (Script)**
3. Make sure **Follow Player** is ✓ Checked

**Solution 3: Check Player Tag**
1. Select your **Player** GameObject
2. In Inspector, check **Tag** dropdown (top)
3. Should be set to **Player**
4. If not, set it to Player

**Solution 4: Check Camera Update**
1. Enter Play Mode
2. Select **MinimapCamera** in Hierarchy
3. Watch its **Position** in Inspector as you move player
4. X and Y should change as player moves
5. If not changing, check script is enabled

---

### Problem: Player can still move in fullscreen mode

**Solution 1: Check Player Reference**
1. Select **MinimapController** in Hierarchy
2. Check **Player** field in Inspector
3. Should reference your Player GameObject
4. If empty, drag Player to this field

**Solution 2: Check PlayerMove Component**
1. Select your **Player** GameObject
2. Make sure it has **PlayerMove** component
3. The script must have `DisableMovement()` and `EnableMovement()` methods
4. These methods already exist in your PlayerMove.cs

**Solution 3: Check Console Messages**
1. Enter Play Mode
2. Press M twice to enter fullscreen
3. Check Console for "[MinimapController] Player movement disabled"
4. If not showing, script isn't finding PlayerMove component

---

### Problem: Transitions are instant (not smooth)

**Solution 1: Check DOTween Installation**
1. In Project window, search for "DOTween"
2. Should find DOTween folders
3. If not found, DOTween is missing (but it's in your project)

**Solution 2: Check Transition Duration**
1. Select **MinimapController** in Hierarchy
2. Find **Transition Duration** setting
3. Should be `0.3` (not 0)
4. If 0, change to 0.3

**Solution 3: Check for Errors**
1. Look in Console for DOTween errors
2. Fix any errors shown

---

### Problem: Zoom/Pan doesn't work in fullscreen

**Solution 1: Verify in Fullscreen Mode**
1. Make sure you pressed M **twice** (not once)
2. Minimap should be in center of screen, not corner
3. Only then will zoom/pan work

**Solution 2: Check Mouse Scroll**
1. In Play Mode, fullscreen minimap
2. Scroll mouse wheel
3. Watch Console for zoom messages
4. If no messages, scroll isn't being detected

**Solution 3: Check Arrow Key Input**
1. Hold arrow key for 1 second
2. Map should pan
3. If not, check Input Actions are assigned

---

### Problem: Icons don't appear on minimap

**Solution 1: Check MinimapIcon Component**
1. Select object that should appear (e.g., Player)
2. Make sure **MinimapIcon** component is attached
3. Check **Minimap Layer Name** field = "Minimap"

**Solution 2: Check Icon Child Object**
1. Select object with MinimapIcon
2. Expand it in Hierarchy
3. Should see child object like "Player_MinimapIcon"
4. Select child → check **Layer** = Minimap

**Solution 3: Check Icon Visibility**
1. Select object with MinimapIcon
2. Check **Always Visible** is ✓ Checked
3. Check **Icon Size** is > 0 (try 1.5)
4. Check **Icon Color** is not black

**Solution 4: Check Icon Sprite**
1. Enter Play Mode
2. Select the "_MinimapIcon" child object
3. Check **Sprite Renderer** component
4. Should have a sprite assigned (even if null, shows white square)
5. Check **Enabled** is ✓ Checked

---

### Problem: ESC key doesn't close fullscreen

**Solution 1: Check UIManager**
1. Make sure you have **UIManager** GameObject in scene
2. UIManager script should be enabled
3. MinimapController should auto-register with UIManager

**Solution 2: Check Console**
1. Enter Play Mode
2. Press M twice to fullscreen
3. Check Console for "[MinimapController] Registered with UIManager"
4. If not showing, UIManager isn't found

**Solution 3: Try M Key Instead**
1. Press M key to cycle through states
2. Should return to normal mode
3. ESC is optional, M always works

---

### Problem: Minimap shows wrong area of map

**Solution 1: Check Camera Position**
1. Select **MinimapCamera** in Hierarchy
2. Check Transform Position
3. Should be above your player/world
4. Adjust X and Y to center on your play area

**Solution 2: Check Camera Rotation**
1. Select **MinimapCamera**
2. Rotation should be `(90, 0, 0)` for top-down view
3. If different, set to 90, 0, 0

**Solution 3: Adjust Orthographic Size**
1. Select **MinimapCamera**
2. Increase **Size** in Camera component to see more area
3. Or decrease to see less area
4. Try values: 5 (close), 10 (normal), 20 (far)

---

## ✅ Final Checklist

After setup is complete, verify all these work:

- [ ] Minimap visible in top-right corner
- [ ] Press M → Semi-transparent (50% opacity)
- [ ] Press M again → Fullscreen (center of screen)
- [ ] Press M again → Back to normal
- [ ] In fullscreen: Mouse scroll zooms in/out
- [ ] In fullscreen: Arrow keys pan the map
- [ ] In fullscreen: Mouse drag pans the map
- [ ] In fullscreen: Player cannot move (WASD does nothing)
- [ ] ESC key closes fullscreen (or M key)
- [ ] Camera follows player in normal/semi-transparent modes
- [ ] Player icon visible on minimap
- [ ] Other icons visible (NPCs, SellBox, etc.)
- [ ] Smooth transitions between states
- [ ] No errors in Console
- [ ] No performance issues

---

## 🎨 Next Steps - Customization

Once everything works, you can customize:

### Change Minimap Size
1. Select **MinimapController** in Hierarchy
2. Find **MinimapUI (Script)** component
3. Change **Normal Size**: Try (250, 250) for bigger corner minimap
4. Change **Fullscreen Size**: Try (1000, 1000) for bigger fullscreen

### Change Corner Position to Top-Left
1. Select **MinimapPanel** in Hierarchy
2. In Rect Transform, change **Anchors** to top-left
3. In MinimapUI script, change **Normal Position** to (100, -100)

### Add More Zoom Levels
1. Select **MinimapController**
2. Find **Zoom Levels** array
3. Change **Size** to 5
4. Add values: 0.25, 0.5, 1, 1.5, 2

### Change Semi-Transparent Opacity
1. Select **MinimapController**
2. Change **Semi Transparent Opacity** to 0.3 (more transparent) or 0.7 (less transparent)

### Add Custom Icons
See MinimapSetupGuide.md for details on:
- Custom icon sprites
- Icon animations (flash, pulse)
- Conditional visibility
- Icon colors and sizes

---

## 📞 Still Need Help?

If you're still having issues:

1. **Check Console** for error messages (Window → General → Console)
2. **Enable Debug Logs** in MinimapController and MinimapUI
3. **Take a screenshot** of your setup and compare to this guide
4. **Check all references** are assigned (no "None" or "Missing" fields)
5. **Try the test scene** provided (if available)

Common mistakes:
- Forgot to create Minimap layer
- Minimap layer not selected in Camera Culling Mask
- Player not assigned in MinimapCamera or MinimapController
- Canvas not set to "Scale With Screen Size"
- MinimapIcon component not added to Player

---

## 🎉 Success!

If you've followed all steps and everything works:

**Congratulations!** 🎊

Your minimap system is now fully functional and ready to use!

You now have:
- ✅ A professional three-state minimap
- ✅ Smooth DOTween transitions
- ✅ Zoom and pan controls
- ✅ Player movement locking
- ✅ Icon system for objects
- ✅ Full UIManager integration

**Enjoy your new minimap system!** 🗺️

---

## 📚 Additional Documentation

For more advanced features and customization:
- **MinimapSetupGuide.md** - Original comprehensive guide
- **README.md** - Quick reference and API docs
- **claude.md** - Full project documentation (section 11)

For scripting API and extending the system:
- See inline code documentation in all 4 scripts
- Check README.md for API reference
- Look at MinimapIcon.cs for icon system examples
