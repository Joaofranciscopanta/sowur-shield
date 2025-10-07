# Unity Setup Instructions for Bug Fixes

## Critical Steps Required in Unity Editor

### 1. Regenerate PlayerControls Input Actions
**MOST IMPORTANT**: The PlayerControls.inputactions file was modified but the C# class needs regeneration:

1. In Unity Project window, navigate to `Assets/PlayerControls.inputactions`
2. Click on the PlayerControls.inputactions file to select it
3. In the Inspector window, you should see "Generate C# Class" button
4. Click "Generate C# Class" button
5. Unity will regenerate `Assets/PlayerControls.cs` with the updated interaction settings

### 2. Verify PlayerMove Component Setup
1. Select the Player GameObject in the scene
2. Check the PlayerMove component in Inspector
3. Ensure the PlayerControls asset is assigned to the component
4. The OnInteract event should be connected to PlayerMove.OnInteract

### 3. Check SellBox Setup
Ensure your SellBox GameObject has:
1. **SellBox script** attached
2. **Collider2D component** with `Is Trigger = true`
3. **Correct Layer**: Set to a layer that's included in PlayerMove's `interactableLayer` mask
4. **UI References**: All UI panel, text, and slot references assigned
5. **Distance**: Within `maxInteractionDistance` (default 3 units) of player

### 4. Check InteractionManager Setup
1. Ensure there's a GameObject with `InteractionManager` script in the scene
2. The InteractionManager should initialize on Start and register the SellBox
3. Check Console for "InteractionManager initialized successfully" message

## Testing the Fixes

### Debug Console Output
With the debug logging added, you should see these messages when testing:

**When pressing E key**:
```
PlayerMove: E key pressed (Input System keyboard)
PlayerMove: OnInteract called - phase: [Started/Performed], performed: [true/false], started: [true/false]
PlayerMove: DetectAndInteract called
PlayerMove: InteractionManager available. CanInteract: [true/false]
```

**When near SellBox**:
```
InteractionManager: Showing prompt for '[SellBox GameObject Name]'
PlayerMove: Triggering interaction with [SellBox Name]
InteractionManager: Triggering interaction with '[SellBox Name]'
SellBox: Interact() called on [GameObject Name]. Currently open: [true/false]
```

**When clicking with tools**:
```
CursorController: MousePressed=true, IsDragging=false, OverUI=[true/false], OverCursor=[true/false]
CursorController: Processing click at [Vector3Int position]
CursorController: Creating soil block with hoe at [position] (if hoe equipped)
```

### Manual Testing Options
If input still doesn't work, you can test manually:

1. **In Play Mode**: Select the Player GameObject, find the PlayerMove component, and click the "Test Interaction" button in the Inspector (only visible in editor)

2. **Check Input System**: Go to Window → Analysis → Input Debugger to see if the E key input is being registered

### Common Issues and Solutions

**Issue**: E key still requires holding instead of pressing
**Solution**: Make sure you clicked "Generate C# Class" after modifying the .inputactions file

**Issue**: InteractionManager shows "No interactable available"
**Solution**: 
- Check SellBox is on correct layer (matches PlayerMove's interactableLayer)
- Verify SellBox has Collider2D with IsTrigger=true
- Check player is within interaction range (default 3 units)

**Issue**: "Mouse over UI" blocking tool clicks
**Solution**: Move mouse closer to the cursor icon (within 1 unit) to bypass UI blocking

**Issue**: Tool clicks not working
**Solution**: 
- Ensure you have a tool equipped (item with "Hoe" tag for soil creation)
- Check mouse is not over UI elements
- Verify CursorController has proper references assigned

### Performance Notes
- The debug logging is extensive and should be removed/disabled in production builds
- Debug messages can be filtered in Console using "PlayerMove", "SellBox", "InteractionManager", or "CursorController"

## Expected Behavior After Fixes

1. **E Key Interaction**: Single press (no holding required) should open/close SellBox when near
2. **Tool Clicking**: Left mouse click should use equipped tools or interact with objects at cursor position
3. **Distance-based**: SellBox interaction available within configured range
4. **UI Integration**: Cursor remains visible for UI interactions, proper panel management

## If Issues Persist

Check these components in order:
1. Input Actions properly regenerated (PlayerControls.cs updated)
2. Player GameObject has correct components and references
3. SellBox GameObject properly configured with collider and layer
4. No UI elements blocking interactions unexpectedly
5. Console debug messages showing where the interaction flow stops