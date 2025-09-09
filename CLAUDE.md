# Sowur Shield - Unity Farming Game

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

### Bug #2: Tool Clicking
**Issue**: Could not click to use tools in certain situations
**Root Causes**:
1. Mouse clicks blocked when `IsMouseOverUI()` returned true, preventing legitimate tool usage
2. Unity's Input System mouse detection potentially failing

**Fixes Applied**:
1. Added `IsMouseOverCursor()` method allowing tool usage when mouse within cursor area
2. Enhanced click logic to bypass UI blocking when over cursor position
3. Added fallback Input.GetMouseButtonDown(0) for reliable mouse detection
4. Added comprehensive debug logging for click detection and processing

**Files Modified**: `CursorController.cs:102-129,190-194`

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

1. **Interaction Testing**:
   - Test E key interaction with SellBox at various distances
   - Verify interaction priority when multiple objects nearby
   - Test interaction during UI states (inventory open, etc.)

2. **Tool Usage Testing**:
   - Test left-click tool usage at cursor position
   - Verify tool usage works when mouse over UI elements
   - Test hoe soil creation functionality

3. **UI Integration Testing**:
   - Test drag-and-drop between inventory and sellbox
   - Verify cursor visibility in different UI states
   - Test Escape key UI closing functionality

## Known Considerations

- Hold interactions removed from input actions - may affect other game mechanics if they relied on hold behavior
- Tool clicking now allowed when mouse over cursor area - may need fine-tuning based on gameplay feel
- InteractionManager system provides better interaction priority than collision-based fallback

## Advanced Features

### Performance Considerations:
- **Object Pooling**: UI elements reused efficiently
- **Distance-Based Calculations**: Interaction optimization
- **Conditional Updates**: UI updates only when necessary
- **LayerMask Usage**: Efficient collision detection

### Extensibility Features:
- **ScriptableObject-Based Data**: Easy content creation
- **Tag-Based Tool System**: Flexible tool categorization
- **Event-Driven Architecture**: Loose coupling between systems
- **Interface-Based Design**: Easy addition of new interactable types

### Unity Integration:
- **Unity Input System**: Modern input handling
- **Tilemap System**: 2D world rendering with dual-grid approach
- **Animator System**: Character animation integration
- **Audio System**: Sound effect management
- **UI System**: UGUI with TextMeshPro integration

## Project Synchronization Strategy

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