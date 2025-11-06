# Sowur Shield - Unity Farming Game

## ⚠️ IMPORTANT: Git Branch Policy

**ALWAYS USE `main` BRANCH - NEVER USE `master`**

This project uses `main` as the primary branch. All references to "master" are deprecated.
- ✅ Use: `git push origin main`
- ❌ Never: `git push origin master`
- ✅ Pull requests target: `main`
- ✅ Default branch: `main`

## Project Overview

**Sowur Shield** is a sophisticated 2D farming simulation game built in Unity that demonstrates mature game development practices. The project combines traditional farming mechanics with modern Unity architecture, featuring extensive system integration and thoughtful design patterns.

### Core Features:
- **Advanced Farming System**: Multi-stage crop growth with soil states and seasonal mechanics
- **Comprehensive Inventory**: 36-slot system with drag/drop UI and item stacking
- **Interactive Dialogue**: Tree-based branching conversations with memory system
- **Tool-Based Interaction**: Distance-limited tool usage with visual feedback
- **Automatic Selling**: Sleep-triggered item sales through SellBox system
- **Time Management**: Day/night cycle with event-driven progression
- **Save/Load System**: Complete game state persistence
- **Dual-Grid Tilemap**: Sophisticated 2D world rendering system
- **Minimap System**: Three-state minimap with zoom/pan controls and icon support

### Technical Highlights:
- **Unity Input System**: Modern input handling with customizable bindings
- **Component-Based Architecture**: Modular, extensible design
- **Interface-Driven Development**: Consistent IInteractable implementation
- **Event System Integration**: Decoupled component communication
- **ScriptableObject Data**: Data-driven content creation
- **Performance Optimized**: Distance-based calculations and object pooling

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core Systems/
│   │   ├── PlayerMove.cs - Player movement, input handling, interaction
│   │   ├── InteractionManager.cs - Centralized interaction system
│   │   ├── UIManager.cs - UI panel management
│   │   ├── UIInput.cs - UI input handling and cursor management
│   │   └── IInteractable.cs - Interface for interactive objects
│   │
│   ├── Inventory System/
│   │   ├── Inventory.cs - Main inventory logic
│   │   ├── InventoryItem.cs - Item data structure
│   │   ├── ItemStack.cs - Item stacking system
│   │   ├── InventorySlot.cs - UI slot handling with drag/drop
│   │   └── ItemTooltip.cs - Item information display
│   │
│   ├── Selling System/
│   │   └── SellBox.cs - Selling container with sleep-based auto-sale
│   │
│   ├── Farming System/
│   │   ├── SoilBlockInteractable.cs - Soil interaction for planting
│   │   ├── CropData.cs - Crop information
│   │   ├── CropGrowthManager.cs - Crop growth logic
│   │   └── DualGridTilemap/
│   │       ├── DualGridTilemap.cs - Two-layer tilemap system
│   │       └── CursorController.cs - Tool-based tile interaction
│   │
│   ├── Dialogue System/
│   │   ├── Core/
│   │   │   ├── DialogueTree.cs - Branching dialogue system
│   │   │   ├── DialogueNode.cs - Individual dialogue pieces
│   │   │   ├── DialogueChoice.cs - Player dialogue options
│   │   │   ├── DialogueCondition.cs - Conditional dialogue
│   │   │   └── DialogueEffect.cs - Dialogue outcomes
│   │   ├── UI/
│   │   │   ├── DialogueTreeUI.cs - Dialogue display system
│   │   │   ├── ChoiceButton.cs - Choice button handling
│   │   │   └── PortraitManager.cs - Character portrait display
│   │   ├── Memory/
│   │   │   ├── ConversationMemory.cs - Conversation state tracking
│   │   │   └── ConversationData.cs - Conversation data storage
│   │   └── NPCDialogueInteractable.cs - NPC interaction handler
│   │
│   ├── Game Management/
│   │   ├── GameData.cs - Game state data
│   │   ├── PlayerDataManager.cs - Player data persistence
│   │   ├── SaveManager.cs - Save/load system
│   │   ├── PlayerStats.cs - Player statistics (money, etc.)
│   │   ├── TimeController.cs - Time/day cycle system
│   │   ├── SceneTransitionManager.cs - Scene management
│   │   └── MainMenuManager.cs - Main menu functionality
│   │
│   ├── UI Systems/
│   │   ├── GameMenuManager.cs - In-game menu
│   │   ├── GameMenuUI.cs - Menu UI components
│   │   ├── MainMenuUI.cs - Main menu UI
│   │   ├── SaveGameUI.cs - Save game interface
│   │   └── SleepConfirmationPanel.cs - Sleep confirmation dialog
│   │
│   ├── Minimap/
│   │   ├── MinimapController.cs - State management and input handling
│   │   ├── MinimapCamera.cs - Camera following and rendering
│   │   ├── MinimapUI.cs - UI display and transitions
│   │   ├── MinimapIcon.cs - Icon system for objects
│   │   └── MinimapSetupGuide.md - Complete setup instructions
│   │
│   └── Utility/
│       ├── FollowPlayer.cs - Camera following
│       ├── GroundItem.cs - World item drops
│       ├── ToolType.cs - Tool categorization
│       └── InventorySpacingFix.cs - UI layout fixes
```

## Architecture & Design Patterns

### Design Patterns Used:
- **Singleton Pattern**: Used extensively for managers (UIManager, InteractionManager, SaveManager, GameTimeController)
- **Component System**: Heavy reliance on Unity's component-based architecture
- **Interface-Driven Design**: Consistent use of IInteractable for all interactive objects
- **Observer Pattern**: Event-driven communication between systems
- **Strategy Pattern**: Different interaction behaviors based on tool types and item tags
- **State Machine Pattern**: Crop growth states, soil states, and UI states

### Code Organization:
The project follows excellent separation of concerns with modular script organization:
- **Core Systems**: Centralized management of player, interaction, and UI
- **Inventory System**: Complete item management with drag/drop UI
- **Farming System**: Sophisticated crop growth and soil management
- **Dialogue System**: Tree-based branching conversations
- **Save System**: Comprehensive data persistence with ISaveable interface
- **Minimap System**: Three-state minimap with zoom/pan and icon support

### Best Practices Observed
- Comprehensive null checking throughout codebase
- Debug logging for troubleshooting
- Modular script organization
- Clear separation of concerns
- Extensive inline documentation

### Performance Considerations
- Object pooling for UI elements
- Efficient collision detection using LayerMasks
- Conditional UI updates to avoid unnecessary redraws
- Distance-based interaction optimization

## Core Game Systems

### 1. Input System (Unity's New Input System)
- **Framework**: Modern Unity Input System with InputActions
- **Key Features**:
  - **Movement**: WASD/Arrow Keys for movement
  - **Interaction**: E key for interaction (fixed from Hold to Press)
  - **Sprint**: Sprint key toggle functionality
  - **Menu**: Escape key for UI management
  - **Inventory Slots**: Number keys 1-9 for inventory selection
  - **Mouse Controls**: Left-click for tool usage with distance limiting
- **Input Actions File**: `PlayerControls.inputactions` with generated C# class

### 2. Player Movement & Control (`PlayerMove.cs`)
- **Movement**: Smooth rigidbody-based movement with rotation
- **Dash System**: Cooldown-based dashing with visual effects
- **Animation Integration**: Parameter-based animator control
- **Interaction Support**: Dual-system interaction detection
- **Inventory Integration**: Direct access to player inventory

### 3. Interaction Management System
**Dual-System Architecture:**
- **Primary**: `InteractionManager.cs` - Priority-based, distance-calculated interaction
- **Fallback**: Collision-based detection in `PlayerMove.cs`

**Key Features:**
- Distance-based priority calculation
- Automatic prompt visibility management
- Registration/unregistration system for interactables
- Support for multiple interactable types (NPCs, SellBox, etc.)

### 4. Inventory System
**Comprehensive item management with multiple components:**

**Core Components:**
- `Inventory.cs`: Main inventory logic with 36-slot capacity (9 hotbar + 27 storage)
- `InventorySlot.cs`: Advanced UI slot with drag/drop, animations, and visual feedback
- `ItemStack.cs`: Efficient item stacking system
- `InventoryItem.cs` (Item.cs): ScriptableObject-based item definitions

**Features:**
- **Drag & Drop**: Full mouse-based item manipulation
- **Stacking System**: Intelligent item stacking with max stack sizes
- **Tool Integration**: Number key selection for tools/items
- **Visual Feedback**: Rarity-based glowing, tooltips, quantity display
- **Save/Load Support**: Complete persistence through ISaveable

### 5. Tool & Cursor System
**Components:**
- `CursorController.cs`: Mouse-based world interaction with priority-based system
- `ToolType.cs`: Tool categorization system

**Priority-Based Interaction System:**
1. **Objects in Hex** (HIGHEST PRIORITY): SellBox, NPCs, Soil blocks always take priority
2. **Tools in Hand** (LOWEST PRIORITY): Hoe, WateringCan, etc. used only when no objects present

**Features:**
- **Distance Limiting**: Tools limited to `maxDistance` from player
- **Tag-Based System**: Tools identified by itemTags (e.g., "Hoe", "WateringCan", "Shovel")
- **Visual Feedback**:
  - Green cursor: Interactable objects present
  - Yellow cursor: Tool can be used
  - White cursor: No interaction available
- **UI Integration**: Cursor hidden during inventory/dialogue states
- **Smart Tool Detection**: Automatically detects tool types and usage conditions

### 6. Farming System
**Multi-Component Architecture:**

**Soil Management (`SoilBlockInteractable.cs`):**
- **States**: Regular → Tilled → Watered → WithCrop
- **Tool Integration**: Hoe, WateringCan, Shovel interactions
- **Visual Feedback**: Sprite changes and highlight colors
- **Crop Integration**: Seamless connection to growth manager

**Crop Growth (`CropGrowthManager.cs`):**
- **Growth Stages**: Multi-stage visual progression
- **Water Requirements**: Death from lack of watering
- **Yield System**: Randomized harvest quantities
- **Regrowth Support**: Crops that produce multiple harvests
- **Time Integration**: Connected to day/night cycle

**Crop Data (`CropData.cs`):**
- **ScriptableObject-Based**: Data-driven crop definitions
- **Growth Configuration**: Customizable days per stage
- **Seasonal Restrictions**: Season-specific growing requirements
- **Visual Assets**: Growth stage sprites and death sprites

**Dual-Grid Tilemap (`DualGridTilemap.cs`):**
- **Two-Layer System**: Placeholder and display tilemaps
- **Rule-Based Display**: Automatic tile selection based on neighbors
- **16-Tile System**: Complete tile transition coverage

### 7. SellBox System
**Features:**
- **Automatic Sales**: Items sold during sleep cycle
- **Drag & Drop Integration**: Full inventory compatibility
- **Value Calculation**: Configurable sell multiplier (default 80%)
- **Visual Feedback**: Dynamic box sprites based on contents
- **UI Management**: Exclusive window management preventing conflicts
- **Movement Control**: Player movement disabled while SellBox is open
- **Auto-Close System**: Closes after 0.5s of movement/interaction attempts

**Auto-Close Behavior:**
- **Movement Detection**: WASD or Arrow keys held for 0.5+ seconds
- **Interaction Detection**: E key held for 0.5+ seconds
- **Smart Reset**: Brief key presses won't trigger close (allows responsive UI)
- **Automatic Recovery**: Movement immediately restored after auto-close

### 8. Dialogue System
**Tree-Based Architecture:**

**Core Components:**
- `DialogueTree.cs`: ScriptableObject-based branching conversations
- `DialogueNode.cs`: Individual dialogue pieces with conditions
- `DialogueTreeUI.cs`: UI management with typewriter effects
- `ConversationMemory.cs`: Persistent conversation state tracking

**Features:**
- **Branching Dialogue**: Choice-driven conversations
- **Conditional Logic**: Show/hide options based on game state
- **Memory System**: Remembers conversation progress
- **Portrait System**: Character visual representation
- **Audio Integration**: Sound effects for dialogue events

### 9. Time Management System
**Components:**
- `GameTimeController.cs` (TimeController.cs): Comprehensive day/night cycle

**Features:**
- **Real-Time Flow**: Configurable time passage rate
- **Day Advancement**: Sleep-triggered day progression
- **Event System**: OnDayChanged, OnTimeChanged events
- **UI Integration**: Time display and updates
- **Save/Load Support**: Persistent time state

### 10. Save/Load System
**Architecture:**
- `SaveManager.cs`: Centralized save management
- `GameData.cs`: Comprehensive data structures
- **ISaveable Interface**: Modular save/load implementation

**Data Categories:**
- Player data (position, stats, money)
- Inventory data (items, selected slot)
- World data (flags, counters, discoveries)
- Time data (day, progress, season)
- Farming data (crops, soil states)
- Relationship data (NPC interactions)

### 11. Minimap System
**Three-State Display System:**

**Core Components:**
- `MinimapController.cs`: State management and input handling
- `MinimapCamera.cs`: Camera following, zoom, and rendering
- `MinimapUI.cs`: UI display and smooth transitions
- `MinimapIcon.cs`: Icon system for objects (NPCs, SellBox, etc.)

**Features:**
- **Three Display States**: Normal (corner), Semi-Transparent (50% opacity), Fullscreen
- **Smart Camera Following**: Tracks player in corner modes, manual control in fullscreen
- **Zoom System**: Three levels (0.5x, 1x, 2x) with smooth transitions
- **Pan Controls**: Arrow keys or mouse drag in fullscreen mode
- **Player Movement Control**: Automatically disables movement in fullscreen
- **UIManager Integration**: Proper window management with ESC key support
- **Performance Optimized**: RenderTexture rendering on dedicated layer
- **Icon System**: Customizable icons for NPCs, buildings, quest markers, etc.

**State Behavior:**
- **Normal**: Top-right corner, 100% opacity, follows player, movement enabled
- **Semi-Transparent**: Top-right corner, 50% opacity, follows player, movement enabled
- **Fullscreen**: Center screen, 100% opacity, zoom/pan enabled, movement disabled

**Input Controls:**
- **M Key**: Cycle through states (Normal → Semi-Transparent → Fullscreen → Normal)
- **Mouse Scroll**: Zoom in/out (fullscreen only)
- **Arrow Keys**: Pan map (fullscreen only)
- **Mouse Drag**: Pan map (fullscreen only)
- **ESC Key**: Close fullscreen mode

**Technical Implementation:**
- DOTween transitions for professional animations
- RenderTexture for optimized rendering
- Layer-based visibility (Minimap layer)
- Integration with existing PlayerMove for movement control
- IUIWindow interface for proper UI coordination

## Bug Fixes Applied

### Bug #1: SellBox Interaction (E Key)
**Issue**: Could not interact with SellBox by pressing E when close
**Root Causes**:
1. Input action configured with `Hold(duration=0.2)` requiring 0.2s hold instead of press
2. PlayerControls.cs not regenerated after .inputactions file modification
3. Possible UI state blocking interactions

**Fixes Applied**:
1. Removed hold interaction from Interact action in `PlayerControls.inputactions:41`
2. Enhanced input callback to accept both `performed` and `started` phases in `PlayerMove.cs:147`
3. Added fallback Input.GetKeyDown(KeyCode.E) handling in `PlayerMove.cs:58-62`
4. Added comprehensive debug logging to track interaction flow
5. Enhanced InteractionManager to use SellBox's actual interaction range

**Files Modified**:
- `PlayerControls.inputactions`
- `PlayerMove.cs:58-62,144-151,152-197`
- `InteractionManager.cs:176-179,205-214`
- `SellBox.cs:252-256`

### Bug #2: Interaction Priority Issues
**Issue**: Hoe would create hex blocks even when clicking on SellBox, and player could move while SellBox was open
**Root Causes**:
1. Incorrect interaction priority - tools had precedence over objects
2. Insufficient object detection reliability (small radius, single method)
3. No movement restriction during UI interactions

**Fixes Applied**:
1. **Priority System Overhaul**: Objects now always take priority over tools
2. **Enhanced Detection**: Multiple detection methods with larger radius (0.6f vs 0.4f)
3. **Movement Control**: Player movement disabled when SellBox is open
4. **Cursor Management**: Tool cursor hidden when SellBox is active
5. **Comprehensive Logging**: Debug tracking for all interaction attempts

**Priority Order (NEW)**:
1. Objects in hex (SellBox, NPCs, Soil) - HIGHEST PRIORITY
2. Tools in hand (Hoe, WateringCan) - LOWEST PRIORITY

**Files Modified**:
- `CursorController.cs:115-175,217-256,303-312` - Priority system and detection
- `SellBox.cs:86,112-118,281-286,311-316` - Movement control integration

### Bug #3: Cursor-Based Interaction Improvement
**Issue**: SellBox and similar objects would respond to clicks anywhere in their hex tile, not requiring cursor to be over the actual sprite
**Root Cause**: Interaction system used grid-based hex detection instead of direct cursor collision

**Solution Applied**:
1. **Dual Interaction System**:
   - **Left-Click (Mouse)**: Direct cursor collision detection using raycasting - requires cursor to be over the sprite
   - **E Key**: Proximity-based interaction via InteractionManager - works within range regardless of cursor position

2. **New Priority System for Left-Click**:
   - **HIGHEST**: Direct mouse cursor collision with sprites (SellBox, NPCs)
   - **MEDIUM**: Hex-based interaction for grid objects (SoilBlocks, Beds)
   - **LOWEST**: Tool usage when no objects are present

3. **Visual Feedback Enhancement**:
   - Green cursor: Direct collision with interactable sprites
   - Green cursor: Grid-based objects in current hex
   - Yellow cursor: Tool can be used at current position
   - White cursor: No interactions available

**Files Modified**:
- `CursorController.cs:116-152,234-300` - Added CheckForDirectMouseHit() and improved priority system
- `PlayerMove.cs:169-170` - Added documentation clarifying E key vs left-click behavior

**Behavior Changes**:
- **SellBox**: Now requires cursor to be over the actual SellBox sprite to open with left-click
- **NPCs**: Same direct cursor requirement for left-click interaction
- **Soil Blocks**: Still use hex-based detection since they align to grid
- **Tools**: Only activate when no objects are present at cursor/hex position
- **E Key**: Unchanged - still works with proximity-based InteractionManager

### Bug #4: Fixed Proximity-Based SellBox Issue
**Issue**: SellBox was still triggering on left-clicks anywhere within collision radius, not requiring cursor to be over sprite
**Root Cause**: `ProcessHexInteraction()` was falling back to `CheckForInteractableAt()` which used large collision detection radius

**Solution Applied**:
1. **Separated Grid vs Direct Detection**:
   - Created `CheckForGridObjectsOnly()` method for soil blocks and beds only
   - Modified `ProcessHexInteraction()` to use precise detection methods
   - Removed SellBox/NPC detection from hex-based collision checking

2. **Refined Detection Hierarchy**:
   - **Direct Mouse Hit**: Uses raycasting and OverlapPoint for SellBox/NPCs
   - **Grid Detection**: Uses 0.3f radius only for SoilBlock/Bed components
   - **Tool Usage**: Only when no objects detected at cursor position

**Files Modified**:
- `CursorController.cs:137-146,320-342` - Added CheckForGridObjectsOnly() method
- `CursorController.cs:104,200-218` - Updated visual feedback and detection flow

### Debug Features Added
**Comprehensive logging system added to track**:
- Input action phases and states in PlayerMove
- InteractionManager registration and distance calculations
- UI state and mouse position relative to UI elements
- SellBox interaction calls and state changes
- Tool click processing and blocking conditions
- Cursor position and mouse-over-cursor detection

**Critical Unity Setup Note**: After modifying `.inputactions` files, you must regenerate the PlayerControls.cs class in Unity:
1. Select the PlayerControls.inputactions file in Project window
2. Click "Generate C# Class" button in the Inspector
3. Ensure the generated class is properly connected to PlayerMove component

## Development Notes

### Code Architecture
- **Component-Based**: Heavy use of Unity's component system
- **Interface-Driven**: IInteractable interface for consistent interaction
- **Singleton Pattern**: Used for managers (UIManager, InteractionManager)
- **Event System**: Actions and delegates for loose coupling

## Setup Requirements

### Scene Setup
1. **Player GameObject**: Must have "Player" tag and PlayerMove component
2. **InteractionManager**: GameObject with InteractionManager script for centralized interactions
3. **UIManager**: GameObject with UIManager script for UI management
4. **Main Camera**: Tagged as "MainCamera" for cursor world positioning

### SellBox Setup
1. GameObject with SellBox script
2. Collider2D with IsTrigger = true
3. Layer set to interactable layer
4. UI references assigned (panel, slots, text components)
5. Audio clips and particle effects (optional)

### Input System Setup
- PlayerControls.inputactions must be generated into PlayerControls.cs
- Input System package required
- Action callbacks properly connected in PlayerMove

## Testing Recommendations

### 1. **Priority-Based Interaction Testing**:
   - **SellBox + Hoe**: Click on SellBox hex with hoe in hand → Should open SellBox (NOT create soil)
   - **Empty Hex + Hoe**: Click on empty hex with hoe → Should create soil block
   - **NPC + Tool**: Click on NPC hex with any tool → Should start dialogue
   - **Multiple Objects**: Test priority when multiple objects overlap

### 2. **Movement Control Testing**:
   - **SellBox Open**: Player should NOT be able to move when SellBox is open
   - **SellBox Close**: Player movement should resume when SellBox is closed
   - **Distance Auto-Close**: Movement should resume if SellBox auto-closes due to distance
   - **Input Auto-Close**: Hold WASD for 0.5s → Should auto-close and restore movement
   - **E Key Auto-Close**: Hold E for 0.5s → Should auto-close SellBox
   - **Brief Key Presses**: Quick WASD/E taps should NOT trigger auto-close
   - **Multiple UI**: Test with inventory + SellBox interactions

### 3. **Visual Feedback Testing**:
   - **Green Cursor**: Should appear when hovering over interactable objects
   - **Yellow Cursor**: Should appear when tool can be used at empty hex
   - **White Cursor**: Default state when no interactions available
   - **Hidden Cursor**: Should hide when SellBox/inventory/dialogue is open

### 4. **Tool Usage Testing**:
   - **Hoe Detection**: Only use hoe when no objects present in hex
   - **Tool Validation**: Test with different tool types and tags
   - **Distance Limits**: Ensure tools respect maxDistance limitations

### 5. **Integration Testing**:
   - **E Key vs Left Click**: Both should respect same priority system
   - **UI State Management**: No conflicts between different UI systems
   - **Debug Logging**: Check console for proper interaction flow tracking

## Known Considerations

- Hold interactions removed from input actions - may affect other game mechanics if they relied on hold behavior
- Tool clicking now allowed when mouse over cursor area - may need fine-tuning based on gameplay feel
- InteractionManager system provides better interaction priority than collision-based fallback

## Main Menu System Implementation

### New Main Menu Components ✨

**MainMenuUI.cs** - Complete main menu interface:
- **New Game**: Starts fresh game with save overwrite confirmation
- **Continue**: Loads existing save (disabled when no save exists)
- **Settings**: Full audio/graphics settings with PlayerPrefs persistence
- **Credits**: Expandable credits/about section
- **Quit**: Application quit with confirmation dialog
- **Save Info Display**: Shows basic save file information when available
- **Loading Screen**: Integrated loading progress and tips

**MainMenuManager.cs** - Singleton coordinator:
- **Scene Integration**: Manages main menu initialization and cleanup
- **Audio Management**: Background music with volume control
- **Input Handling**: Navigation and ESC key support
- **Settings Integration**: Coordinates with save system and audio settings
- **Debug Support**: Comprehensive debug information for development

**SceneTransitionManager.cs** - Smooth scene loading system:
- **Async Loading**: Non-blocking scene transitions with progress tracking
- **Fade Effects**: Configurable fade in/out transitions
- **Loading Screens**: Customizable loading UI with tips and progress bars
- **Minimum Load Time**: Ensures loading screen shows long enough for good UX
- **Audio Feedback**: Transition sound effects
- **Scene-Specific Callbacks**: Custom initialization for different scenes

### Integration with Existing Systems ✅

**SaveManager Integration**:
- Continue button automatically disabled when no save exists
- New game confirmation when save file would be overwritten
- Save file info display (requires SaveManager.GetSaveFileInfo() implementation)

**GameMenuManager Compatibility**:
- "Quit to Main Menu" functionality already exists and works with new MainMenuUI
- Consistent singleton pattern and event-driven architecture
- Shared settings system through PlayerPrefs

**Audio System Integration**:
- Volume controls affect both menu and game audio
- Sound effect playback respects SFX and Master volume settings
- Background music with proper volume mixing

### Scene Flow Architecture 🔄

```
MainMenu Scene → MainMenuUI → SceneTransitionManager → MainGameScene
     ↑                                                        ↓
GameMenuManager ← "Quit to Main Menu" ← In-Game ESC Menu ←────┘
```

**New Game Flow**:
1. Check for existing save → Confirm overwrite if needed
2. Clear save data → Play start sound
3. Load MainGameScene via SceneTransitionManager
4. Initialize fresh game state

**Continue Game Flow**:
1. Verify save file exists → Load MainGameScene
2. SaveManager automatically loads save data in new scene
3. Player continues from last save point

### UI Architecture & Features 🎨

**Panel Management System**:
- Main Panel: Core navigation buttons
- Settings Panel: Reusable audio/graphics controls
- Credits Panel: Game information and credits
- Confirmation Dialogs: Yes/No confirmations for important actions
- Loading Panel: Progress bar, tips, and status text

**Visual Polish**:
- Button state management (disabled Continue when no save)
- Color-coded feedback (success/error states)
- Loading tips system with randomized helpful hints
- Progress tracking with descriptive loading messages

**Input Support**:
- Mouse/touch button interaction
- Keyboard navigation (ESC to cancel/back)
- Extensible input action system for controller support

### Files Added 📁
- `MainMenuUI.cs` - Core main menu functionality and UI management
- `MainMenuManager.cs` - Main menu coordinator and singleton manager
- `SceneTransitionManager.cs` - Advanced scene loading with transitions

### Setup Requirements 🔧

**MainMenu Scene Setup**:
1. Add MainMenuManager script to a manager GameObject
2. Add MainMenuUI script to UI Canvas
3. Add SceneTransitionManager to a persistent GameObject
4. Assign UI references (buttons, panels, sliders, etc.)
5. Configure audio sources and clips
6. Set up loading screen UI elements

**Integration Notes**:
- SceneTransitionManager persists between scenes (DontDestroyOnLoad)
- MainMenuManager is scene-specific (destroyed on scene change)
- SaveManager integration requires GetSaveFileInfo() method (currently placeholder)

## Advanced Features

### Performance Considerations:
- **Object Pooling**: UI elements reused efficiently
- **Distance-Based Calculations**: Interaction optimization
- **Conditional Updates**: UI updates only when necessary
- **LayerMask Usage**: Efficient collision detection
- **Async Scene Loading**: Non-blocking transitions with progress tracking

### Extensibility Features:
- **ScriptableObject-Based Data**: Easy content creation
- **Tag-Based Tool System**: Flexible tool categorization
- **Event-Driven Architecture**: Loose coupling between systems
- **Interface-Based Design**: Easy addition of new interactable types
- **Modular UI System**: Reusable settings and confirmation panels

### Unity Integration:
- **Unity Input System**: Modern input handling
- **Tilemap System**: 2D world rendering with dual-grid approach
- **Animator System**: Character animation integration
- **Audio System**: Sound effect management
- **UI System**: UGUI with TextMeshPro integration
- **Scene Management**: Async loading with proper lifecycle management

## Project Synchronization Strategy

### 🔧 **CRITICAL FOR NEW CLAUDE CODE USERS**:
**If you are joining this project with Claude Code, you MUST immediately replace/merge your CLAUDE.md with THIS one. This repository's CLAUDE.md takes PRIORITY - it contains 6 months of critical project knowledge, bug fixes, and architectural decisions that your Claude instance needs to function effectively on this codebase.**

**DO THIS IMMEDIATELY:**
1. **Backup your current CLAUDE.md** (if you want to preserve any custom instructions)
2. **Use THIS repository's CLAUDE.md as the primary source**
3. **Only add your custom instructions as additions, never replace existing content**

### Multi-Developer Workflow

#### **Recommended Git Flow for Team Development:**

1. **Feature Branch Workflow** (Recommended):
   ```bash
   # Always start with latest main
   git checkout main
   git pull origin main

   # Create feature branch
   git checkout -b feature/your-feature-name

   # Work on your changes
   git add specific-files
   git commit -m "Descriptive message"
   git push origin feature/your-feature-name

   # Create Pull Request for review
   gh pr create --title "Your Feature" --body "Description"
   ```

2. **Communication Protocol**:
   - **Before starting work**: Check for active branches and communicate with team
   - **Scene modifications**: Coordinate MainMenu.unity and SampleScene.unity changes
   - **Architecture changes**: Discuss modifications to core systems (GameMenuManager, SaveManager, etc.)
   - **CLAUDE.md updates**: Always merge documentation changes to maintain project knowledge

3. **Merge Conflict Prevention**:
   - **Frequent pulls**: `git pull origin main` before starting work
   - **Small commits**: Keep changes focused and atomic
   - **Scene coordination**: Only one person modifies Unity scenes at a time
   - **Script ownership**: Assign primary ownership of major systems

4. **File-Specific Guidelines**:
   ```bash
   # Safe to work on simultaneously (low conflict risk):
   - New scripts in Assets/Scripts/
   - Individual ScriptableObjects
   - Art assets and prefabs

   # Coordinate before modifying (high conflict risk):
   - MainMenu.unity, SampleScene.unity
   - GameMenuManager.cs, SaveManager.cs
   - PlayerControls.inputactions
   - CLAUDE.md (merge changes together)
   ```

### Keeping Claude Code Up-to-Date:

1. **Git Integration** (Recommended):
   ```bash
   # Set up persistent GitHub CLI access
   gh auth login
   gh repo set-default YOUR_USERNAME/sowur-shield

   # Claude can then always:
   git pull origin main  # Sync latest changes
   git status           # Check project state
   git log --oneline -10 # See recent commits
   ```

2. **Project State Monitoring**:
   ```bash
   # Claude will check project state with:
   git status                    # Uncommitted changes
   find Assets/Scripts -name "*.cs" -newer CLAUDE.md  # New scripts
   git diff HEAD~1 --name-only  # Recent file changes
   ```

3. **Regular Updates**:
   - **Automatic**: Claude checks git log for changes since last session
   - **Manual**: Use `@claude analyze recent changes` to trigger update
   - **File-Specific**: Claude monitors specific files for modifications

### Claude Code Workflow:

1. **Session Start**:
   - Pull latest changes: `git pull origin main`
   - Check for new scripts or modifications
   - Update CLAUDE.md if significant changes detected

2. **During Development**:
   - Create feature branches: `git checkout -b feature/new-system`
   - Make changes with proper commits
   - Update documentation as needed

3. **Session End**:
   - Commit any changes: `git add . && git commit -m "Description"`
   - Push changes: `git push origin branch-name`
   - Create PR if requested: `gh pr create --title "Title" --body "Description"`

### Monitoring Commands:
```bash
# Quick project status check
git log --oneline -5 && git status --porcelain

# Find recently modified scripts
find Assets/Scripts -name "*.cs" -mtime -7

# Check for new Unity assets
find Assets -name "*.meta" -newer .git/FETCH_HEAD
```

### Team Claude Code Synchronization

#### **🚨 CRITICAL: For New Claude Code Instances**

**When a new collaborator joins with Claude Code, they MUST:**

1. **Replace Your CLAUDE.md with This One**:
   ```bash
   # First, pull this repository (contains the authoritative CLAUDE.md)
   git pull origin main

   # IMPORTANT: Use THIS repository's CLAUDE.md as your primary file
   # This CLAUDE.md contains critical project context your Claude instance needs:
   # - All bug fixes and solutions (SellBox interaction, Input System issues)
   # - Complete architecture understanding
   # - Unity setup requirements and component configurations
   # - Performance optimizations and debugging workflows

   # If you have custom instructions, ADD them to this file, don't replace it
   ```

2. **Essential Information in This File**:
   - **Bug Fix History**: Solutions to known issues (SellBox interaction, Input System, etc.)
   - **Architecture Decisions**: Why certain patterns were chosen
   - **Unity Setup Requirements**: Scene configuration, component assignments
   - **Debug Workflows**: How to troubleshoot common problems
   - **Performance Optimizations**: Implemented solutions and rationale

3. **Collaboration Commands**:
   ```bash
   # Before starting any work session:
   git checkout main && git pull origin main

   # Check what teammates are working on:
   git branch -r
   gh pr list

   # Share your work frequently:
   git push origin your-branch
   gh pr create --draft  # For work-in-progress sharing
   ```

#### **File Ownership & Coordination**

**🔒 High-Conflict Files (One person at a time):**
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/SampleScene.unity`
- `PlayerControls.inputactions`
- `CLAUDE.md` (coordinate merges)

**✅ Safe for Parallel Work:**
- New scripts in `Assets/Scripts/`
- New prefabs and ScriptableObjects
- Art assets and materials
- Individual feature implementations

**📞 Communication Protocol:**
```bash
# Before modifying core systems, announce:
# "Working on GameMenuManager.cs - ETA 2 hours"

# When done:
# "GameMenuManager changes pushed to feature/menu-enhancements"
```

#### **Merge Conflict Resolution**

**Unity Scene Conflicts:**
- **Prevention**: Coordinate scene modifications in advance
- **Resolution**: Use Unity's conflict resolution or choose one version
- **Recovery**: Keep backup copies before major scene changes

**CLAUDE.md Conflicts:**
- **THIS repository's CLAUDE.md takes PRIORITY**
- **Preserve all technical knowledge from this version**
- **Only add new findings/solutions, never remove existing content**
- **When in doubt, keep the version that has more debugging information**

**Script Conflicts:**
- **Review both versions for improvements**
- **Test thoroughly after merging**
- **Document any breaking changes**

## Future Improvements

### Immediate Enhancements:
1. **Visual Feedback**: Add interaction range indicators
2. **Tool Durability**: Implement tool wear and repair system
3. **Advanced Crops**: Season-specific growing mechanics
4. **Weather System**: Environmental effects on farming
5. **NPC Relationships**: Friendship/reputation system

### System Expansions:
1. **Building System**: Constructible farm buildings
2. **Animal Husbandry**: Livestock management
3. **Market System**: Dynamic pricing and trade
4. **Quest System**: Task-based progression
5. **Multiplayer Support**: Cooperative farming

### Technical Improvements:
1. **Performance Profiling**: Identify and optimize bottlenecks
2. **Unit Testing**: Comprehensive test coverage
3. **Code Documentation**: API documentation generation
4. **Asset Management**: Addressable asset system
5. **Build Pipeline**: Automated build and deployment
6. **Delete debug logs after a feature is done**

## WebGL Demo Deployment (GitHub Pages)

### Automated Deployment System ✨

**The project now has an automated GitHub Actions workflow for WebGL demo deployment!**

**Live Demo**: https://joaofranciscopanta.github.io/sowur-shield/

### Deployment Architecture

```
Unity Cloud Build (WebGL Target)
    ↓ (REST API)
GitHub Actions Workflow (.github/workflows/deploy-webgl-demo.yml)
    ↓ (Download & Process)
Custom CSS Preservation (.github/templates/style.css)
    ↓ (Automated Deployment)
GitHub Pages (docs/ folder on main branch)
```

### Automated Workflow Features

**Schedule**: Weekly deployment every Sunday at 3 AM UTC
- **Automatic**: Downloads latest successful Unity Cloud Build
- **CSS Preservation**: Automatically restores custom sidebar styling
- **Build Verification**: Validates build integrity before deployment
- **Backup System**: Creates git tags before each deployment for rollback
- **Health Checks**: Verifies GitHub Pages deployment success
- **Manual Trigger**: Can be triggered manually via GitHub Actions UI
- **Build Number Display**: Injects Unity Cloud Build number into demo
- **Discord Notifications**: Sends rich embeds for deployment success/failure

**Workflow File**: `.github/workflows/deploy-webgl-demo.yml`

**Build Number Features**:
- Updates release notes sidebar version: "Build #X - Month Year"
- Adds build info badge in bottom-right corner of demo
- Shows deployment timestamp in HTML comments
- Dynamically generated from Unity Cloud Build API

**Discord Integration**:
- Success notifications with build number, date, and demo link
- Failure notifications with workflow logs link
- Color-coded rich embeds (green for success, red for failure)
- Optional feature - works without webhook configured
- Requires `DISCORD_WEBHOOK_URL` GitHub secret

### Supporting Scripts

**1. CSS Restoration Script** (`.github/scripts/restore-css.sh`)
- Automatically copies custom CSS from template
- Verifies sidebar styles are present
- Creates backup of Unity's default CSS

**2. Build Verification Script** (`.github/scripts/verify-build.sh`)
- Validates Unity build structure
- Checks for critical files (.data, .framework, .wasm)
- Confirms CSS preservation
- Verifies file sizes

**3. CSS Template** (`.github/templates/style.css`)
- Master copy of custom sidebar styling
- Source of truth for all deployments
- Preserved across Unity builds

### Deployment Triggers

**1. Scheduled Deployment** (Default)
```yaml
# Weekly on Sunday at 3 AM UTC
schedule:
  - cron: '0 3 * * 0'
```

**2. Manual Deployment**
- Go to GitHub Actions → "Deploy WebGL Demo to GitHub Pages"
- Click "Run workflow"
- Optionally specify build number or skip verification

**3. Webhook Trigger** (Optional)
- Unity Cloud Build can trigger deployment via webhook
- Event type: `repository_dispatch` with `unity-build-complete`

### Manual Deployment Steps

If you need to deploy manually without the workflow:

1. **Build WebGL demo in Unity Cloud Build**
2. **Download the build** from Unity Cloud Build dashboard
3. **Extract to docs/** folder in repository
4. **Restore custom CSS**:
   ```bash
   ./.github/scripts/restore-css.sh
   ```
5. **Verify build**:
   ```bash
   ./.github/scripts/verify-build.sh docs
   ```
6. **Commit and push** to main branch
7. **GitHub Pages** will auto-deploy in 2-5 minutes

### Important: Sidebar Styling Maintenance

**CRITICAL**: The `docs/TemplateData/style.css` file contains custom sidebar styling for the release notes panel. This styling MUST be preserved when rebuilding WebGL demos.

**Problem**: Unity's WebGL build process overwrites `style.css` with a minimal default version, removing all custom sidebar styles.

**Solution**: The automated workflow handles this automatically using `.github/templates/style.css` as the master copy.

**Required CSS Features**:
- Fixed left sidebar (`#release-notes`) with gradient background
- Responsive layout that adjusts Unity container position
- Styled release notes sections with color-coded headers
- Mobile-responsive design with collapsible sidebar
- Hover effects on footer links
- Custom scrollbar styling

**Manual CSS Restoration**:
```bash
# If deploying manually, restore CSS with:
./.github/scripts/restore-css.sh .github/templates/style.css docs/TemplateData/style.css
```

### Rollback Strategy

**Automatic Backups**: Every deployment creates a git tag:
```
backup/webgl-demo-20250106-030000
```

**To Rollback**:
1. Find latest backup tag: `git tag -l "backup/webgl-demo-*" | sort -r | head -1`
2. Restore docs folder: `git checkout <tag> -- docs/`
3. Commit and push to main

**Or use the workflow** with rollback flag (future enhancement).

### Required GitHub Secrets

For automated deployment to work, configure these secrets in repository settings:

**Required Secrets:**
1. **UNITY_API_KEY**: Your Unity Cloud Build API key
   - Get from: Unity Cloud Services → Cloud Build Preferences
2. **UNITY_ORG_ID**: Your Unity organization ID
3. **UNITY_PROJECT_ID**: Your Unity project ID
4. **UNITY_BUILD_TARGET_ID**: WebGL build target ID

**Optional Secrets:**
5. **DISCORD_WEBHOOK_URL**: Discord webhook URL for deployment notifications
   - Get from: Discord Server Settings → Integrations → Webhooks
   - If not configured, workflow continues without notifications

**To find Unity IDs**:
- Go to Unity Cloud Build dashboard
- URL format: `https://build.cloud.unity.com/orgs/{ORG_ID}/projects/{PROJECT_ID}/buildtargets/{BUILD_TARGET_ID}/`

### Monitoring Deployment

**GitHub Actions Dashboard**:
- View workflow runs: Repository → Actions → "Deploy WebGL Demo to GitHub Pages"
- Each run shows detailed logs and deployment summary
- Green checkmark = successful deployment
- Red X = failed deployment (check logs)

**Deployment Summary** includes:
- Unity Cloud Build number
- Download status
- Deployment status
- Links to live demo and repository
- Timestamp of deployment

### Troubleshooting

**Build Download Fails**:
- Verify GitHub secrets are configured correctly
- Check Unity Cloud Build has successful WebGL builds
- Ensure API key has proper permissions

**CSS Not Preserved**:
- Check `.github/templates/style.css` exists
- Verify restore-css.sh script ran successfully
- Manually inspect `docs/TemplateData/style.css` for `#release-notes`

**GitHub Pages Not Updating**:
- Wait 2-5 minutes for GitHub Pages to rebuild
- Check repository settings → Pages → Source is set to "main branch /docs folder"
- Verify workflow pushed changes to main branch

**Build Verification Fails**:
- Check build structure matches expected format
- Ensure Unity build target is WebGL (not other platform)
- Review verification script logs for specific errors

### Performance Considerations

**GitHub Actions Minutes**:
- Free tier: 2,000 minutes/month
- Each deployment uses ~5-10 minutes
- Weekly schedule = ~40 minutes/month (well within limits)

**Unity Cloud Build Minutes**:
- Separate from GitHub Actions
- Build in Unity Cloud Build according to your plan
- Workflow only downloads existing builds (doesn't trigger new builds)

**GitHub Pages Bandwidth**:
- Free tier: 100 GB/month
- Monitor if demo becomes very popular

### Future Enhancements

Potential improvements to the deployment system:
- **Automatic build triggering**: Trigger Unity Cloud Build from GitHub Actions
- **Multi-environment deploys**: Dev/staging/prod environments
- **Discord notifications**: Alert on deployment success/failure
- **Build comparison**: Show diff between deployments
- **Automatic release notes**: Generate from git commits
- **Performance monitoring**: Track build size and load times
