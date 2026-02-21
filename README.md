# 🌾 Sowur Shield

[![Unity](https://img.shields.io/badge/Unity-2022.3.46f1-black?logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/Joaofranciscopanta/sowur-shield?style=social)](https://github.com/Joaofranciscopanta/sowur-shield)
[![Last Commit](https://img.shields.io/github/last-commit/Joaofranciscopanta/sowur-shield)](https://github.com/Joaofranciscopanta/sowur-shield/commits/main)

A sophisticated 2D farming simulation game built in Unity that combines traditional farming mechanics with modern game development architecture. Plant crops, interact with NPCs, manage your inventory, and build your dream farm!

## 🎮 Play the Demo

**[➡️ Play Sowur Shield on GitHub Pages](https://joaofranciscopanta.github.io/sowur-shield/)**

Experience the game directly in your browser! The WebGL demo includes:
- Full farming system with crop growth mechanics
- Interactive inventory and selling system
- NPC dialogue and interactions
- Save/Load functionality
- Day/night cycle

---

## ✨ Features

### 🌱 Advanced Farming System
- **Multi-Stage Crop Growth**: Watch your crops evolve through distinct growth phases
- **Soil State Management**: Till, water, and maintain soil health
- **Seasonal Mechanics**: Different crops thrive in different seasons
- **Harvest System**: Randomized yields and regrowth support for recurring harvests

### 🎒 Comprehensive Inventory
- **36-Slot System**: 9 hotbar slots + 27 storage slots
- **Drag & Drop UI**: Intuitive item management with visual feedback
- **Item Stacking**: Automatic stacking with configurable max stack sizes
- **Rarity System**: Color-coded item rarity with glow effects
- **Tool Quickbar**: Number keys (1-9) for instant tool switching

### 💬 Interactive Dialogue System
- **Branching Conversations**: Choice-driven dialogue trees
- **Memory System**: NPCs remember your previous conversations
- **Conditional Logic**: Dynamic dialogue based on game state
- **Portrait Display**: Character visual representation during conversations

### 🛠️ Tool-Based Interaction
- **Distance-Limited Usage**: Realistic tool range mechanics
- **Visual Feedback**: Color-coded cursor system (green/yellow/white)
- **Priority System**: Smart interaction detection (objects > tools)
- **Multiple Tools**: Hoe, Watering Can, Shovel, and more

### 💰 Automatic Selling System
- **SellBox Container**: Drag items to sell
- **Sleep-Triggered Sales**: Items automatically sold when you sleep
- **Configurable Pricing**: Adjustable sell multiplier (default 80%)
- **Visual Feedback**: Dynamic box sprites based on contents

### ⏰ Time Management
- **Day/Night Cycle**: Realistic time progression
- **Event System**: OnDayChanged and OnTimeChanged events
- **Sleep Mechanics**: Advance days and trigger sales
- **Seasonal Calendar**: Track seasons and plan your crops

### 🗺️ Minimap System
- **Three Display Modes**: Normal, Semi-Transparent, Fullscreen
- **Zoom Controls**: Three levels (0.5x, 1x, 2x)
- **Pan Navigation**: Arrow keys or mouse drag in fullscreen
- **Icon Support**: Customizable icons for NPCs, buildings, and quest markers
- **Smart Movement Control**: Auto-disables player movement in fullscreen mode

### 💾 Save/Load System
- **Complete State Persistence**: Save everything from player position to crop growth
- **Multiple Save Slots**: Manage different playthroughs
- **Auto-Save Support**: Never lose your progress
- **Modular Architecture**: Easy to extend with new saveable systems

### 🐔 Animal System
- **Animal Companions**: Chickens and other farm animals
- **AI Behavior**: Autonomous movement and interactions
- **Animal Zones**: Designated areas for animal management
- **Information UI**: View animal stats and status

---

## 🎯 Technical Highlights

### Modern Unity Architecture
- **Unity Input System**: Modern input handling with customizable bindings
- **Component-Based Design**: Modular, extensible architecture
- **Interface-Driven Development**: Consistent `IInteractable` implementation
- **Event System Integration**: Decoupled component communication
- **ScriptableObject Data**: Data-driven content creation

### Design Patterns
- **Singleton Pattern**: Centralized managers (UIManager, SaveManager, etc.)
- **Observer Pattern**: Event-driven system updates
- **Strategy Pattern**: Context-specific interaction behaviors
- **State Machine Pattern**: Crop growth, soil states, UI states

### Performance Optimizations
- **Object Pooling**: Efficient UI element reuse
- **Distance-Based Calculations**: Optimized interaction checks
- **Conditional Updates**: UI updates only when necessary
- **LayerMask Optimization**: Efficient collision detection

---

## 📂 Project Structure

```
Assets/
├── Scripts/
│   ├── Core Systems/
│   │   ├── PlayerMove.cs              # Player movement & input
│   │   ├── InteractionManager.cs      # Centralized interactions
│   │   ├── UIManager.cs                # UI panel management
│   │   └── IInteractable.cs            # Interaction interface
│   │
│   ├── Inventory System/
│   │   ├── Inventory.cs                # Main inventory logic
│   │   ├── InventorySlot.cs            # UI slot with drag/drop
│   │   ├── SlotVisualController.cs     # Visual effects & animations
│   │   ├── SlotDragHandler.cs          # Drag & drop functionality
│   │   └── SlotSellBoxAdapter.cs       # SellBox integration
│   │
│   ├── Farming System/
│   │   ├── SoilBlockInteractable.cs    # Soil interaction
│   │   ├── CropGrowthManager.cs        # Crop growth logic
│   │   ├── CropData.cs                 # Crop definitions
│   │   └── DualGridTilemap/            # Tilemap system
│   │
│   ├── Dialogue System/
│   │   ├── Core/
│   │   │   ├── DialogueTree.cs         # Branching dialogue
│   │   │   ├── DialogueNode.cs         # Dialogue pieces
│   │   │   └── DialogueChoice.cs       # Player options
│   │   ├── UI/
│   │   │   └── DialogueTreeUI.cs       # Dialogue display
│   │   └── Memory/
│   │       └── ConversationMemory.cs   # Conversation tracking
│   │
│   ├── Game Management/
│   │   ├── SaveManager.cs              # Save/load system
│   │   ├── TimeController.cs           # Day/night cycle
│   │   ├── PlayerStats.cs              # Player statistics
│   │   └── GameData.cs                 # Game state data
│   │
│   ├── Minimap/
│   │   ├── MinimapController.cs        # State & input handling
│   │   ├── MinimapCamera.cs            # Camera & rendering
│   │   ├── MinimapUI.cs                # UI display
│   │   └── MinimapIcon.cs              # Icon system
│   │
│   └── Animals/
│       ├── Animal.cs                   # Animal base class
│       ├── AnimalAI.cs                 # AI behavior
│       └── AnimalData.cs               # Animal definitions
│
├── Scenes/
│   ├── MainMenu.unity                  # Main menu scene
│   └── SampleScene.unity               # Main game scene
│
├── Prefabs/                            # Reusable game objects
├── Sprites/                            # 2D artwork
└── ScriptableObjects/                  # Data assets
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity 2022.3.46f1** (LTS)
- **TextMesh Pro** (included)
- **Unity Input System** (included)
- **DOTween** (for animations)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Joaofranciscopanta/sowur-shield.git
   cd sowur-shield
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Click "Open" and select the `Sowur Shield` folder
   - Unity will import all assets (may take a few minutes)

3. **Open Main Scene**
   - Navigate to `Assets/Scenes/`
   - Open `MainMenu.unity` or `SampleScene.unity`

4. **Play!**
   - Press the Play button in Unity Editor
   - Use WASD to move, E to interact, Mouse to use tools

### Controls

| Action | Key/Button |
|--------|-----------|
| Movement | WASD / Arrow Keys |
| Interact | E |
| Use Tool | Left Mouse Click |
| Open Inventory | I |
| Hotbar Select | 1-9 (Number Keys) |
| Open Menu | ESC |
| Toggle Minimap | M |
| Sprint | Shift (Hold/Toggle) |

---

## 🏗️ Building the Game

### Desktop Build (Windows/Mac/Linux)
1. Open Unity Editor
2. Go to `File > Build Settings`
3. Select your target platform
4. Click "Build" and choose output folder
5. Run the executable

### WebGL Build
1. Switch platform to WebGL in Build Settings
2. Build to `docs/` folder for GitHub Pages deployment
3. **Important**: Restore custom CSS after build (see `.documentation/DEPLOYMENT_GUIDE.md`)

For detailed build instructions, see [.documentation/HOW_TO_BUILD.md](.documentation/HOW_TO_BUILD.md).

---

## 📖 Documentation

- **[CLAUDE.md](CLAUDE.md)** - Comprehensive project documentation and architecture guide
- **[PATCH_NOTES.md](.documentation/PATCH_NOTES.md)** - Version history and changelog
- **[NPC Canvas Fix Guide](.documentation/NPC_CANVAS_FIX_GUIDE.md)** - Troubleshooting NPC interaction prompts
- **[Additional Documentation](.documentation/)** - Build guides, deployment instructions, and more

---

## 🤝 Contributing

We welcome contributions! To contribute:

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit your changes** (`git commit -m 'Add amazing feature'`)
4. **Push to the branch** (`git push origin feature/amazing-feature`)
5. **Open a Pull Request**

Please ensure your code follows the existing architecture patterns and includes appropriate documentation.


## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👥 Team

### Core Team
- **[João Francisco Pantaleão](https://www.linkedin.com/in/joaofranciscopantaleao/)** - Owner & Lead Developer
- **[Lucas Daniel](https://www.linkedin.com/in/enf-lucas-daniel/)** - Co-Owner & Main Developer
- **[Isabella Freitas](https://www.linkedin.com/in/isabellafferreira03/)** - Art Director, Dialogue & Character Design

### Technical Stack
- **Unity Version**: 2022.3.46f1 LTS
- **Development Period**: 2025 - Present

---

## 🌟 Future Improvements

### Planned Features
- 🏠 **Building System**: Construct barns, silos, and other structures
- 🐄 **Expanded Animal Husbandry**: Cows, pigs, sheep, and more
- 🌦️ **Weather System**: Dynamic weather affecting crop growth
- 💍 **NPC Relationships**: Friendship levels and gift-giving
- 🎯 **Quest System**: Task-based progression and rewards
- 🎣 **Fishing System**: Rivers, lakes, and ocean fishing
- 🍳 **Cooking System**: Combine ingredients to create meals
- 🌐 **Multiplayer Support**: Cooperative farming with friends

### Technical Roadmap
- Performance profiling and optimization
- Unit testing framework
- Enhanced save system with cloud sync
- Mobile platform support

---

## 📧 Support

For bugs, feature requests, or questions:
- **Issues**: [GitHub Issues](https://github.com/Joaofranciscopanta/sowur-shield/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Joaofranciscopanta/sowur-shield/discussions)

---

## 🙏 Acknowledgments

- Unity Technologies for the game engine
- TextMesh Pro for advanced text rendering
- DOTween for smooth animations
- The game development community for inspiration and support

---

<div align="center">

**[⬆ Back to Top](#-sowur-shield)**

Made with passion by [João Francisco](https://www.linkedin.com/in/joaofranciscopantaleao/), [Lucas Daniel](https://www.linkedin.com/in/enf-lucas-daniel/) & [Isabella Freitas](https://www.linkedin.com/in/isabellafferreira03/)
</div>
