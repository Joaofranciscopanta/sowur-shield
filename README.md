# 🌾 Sowur Shield

[![Unity](https://img.shields.io/badge/Unity-2022.3.46f1-black?logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/Joaofranciscopanta/sowur-shield?style=social)](https://github.com/Joaofranciscopanta/sowur-shield)
[![Last Commit](https://img.shields.io/github/last-commit/Joaofranciscopanta/sowur-shield)](https://github.com/Joaofranciscopanta/sowur-shield/commits/main)

A sophisticated 2D farming simulation game built in Unity that combines traditional farming mechanics with modern game development architecture. Plant crops, tend to animals, interact with NPCs, manage your inventory, and build your dream farm!

## 🎮 Play the Demo

**[➡️ Play Sowur Shield on GitHub Pages](https://joaofranciscopanta.github.io/sowur-shield/)**

Experience the game directly in your browser! The WebGL demo includes:
- Full farming system with crop growth mechanics
- Interactive inventory and selling system
- NPC dialogue and interactions
- Save/Load with multiple save slots
- Day/night cycle
- Animal husbandry system

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
- **Configurable Pricing**: Adjustable sell multiplier via GameBalance ScriptableObject
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
- **Multiple Save Slots**: `AutoSave`, `Slot 1`, `Slot 2`, `Slot 3`
- **Play Time Tracking**: Each slot displays total play time
- **Auto-Save on Sleep**: Never lose your progress
- **Slot Management**: Delete, overwrite, and load individual slots
- **Complete State Persistence**: Crops, inventory, animals, time — everything saved

### 🐔 Animal Husbandry System
- **Petting & Feeding**: Interact with animals daily for happiness bonuses
- **Happiness System**: 0–100 scale with daily decay and production bonuses
- **Feeding Trough**: World-placed trough auto-feeds animals each day
- **Heart Particle Effects**: Visual feedback when petting or feeding
- **Production System**: Happy animals produce items at configurable intervals
- **Animal Roster UI**: Track all your animals and their status at a glance

### ⚖️ Game Balance (ScriptableObject)
- **Centralized Tuning**: All magic numbers in one `GameBalance` asset
- **Economy**: Sell multiplier, pricing configuration
- **Animal Stats**: Happiness bonuses, decay rates, production multipliers
- **Interaction Ranges**: Per-system distance tuning without touching code

---

## 🎯 Technical Highlights

### Modern Unity Architecture
- **Unity Input System**: Modern input handling with customizable bindings
- **Component-Based Design**: Modular, extensible architecture
- **Interface-Driven Development**: Full `IInteractable` contract — `Interact()`, `CanInteract()`, `GetInteractionRange()`, `GetInteractionPrompt()` — on all 9 interactive types
- **Event System Integration**: Decoupled component communication via Actions/delegates
- **ScriptableObject Data**: Data-driven content — crops, animals, items, game balance

### Design Patterns
- **Singleton Pattern**: Centralized managers (`UIManager`, `SaveManager`, `InteractionManager`, `GameTimeController`)
- **Observer Pattern**: Event-driven system updates (`OnDayChanged`, `OnClosestInteractableChanged`)
- **Strategy Pattern**: Context-specific interaction behaviors per interactable type
- **State Machine Pattern**: Crop growth, soil states, UI states

### Performance Optimizations
- **Cached References**: `FindObjectOfType` called once in `Start()`, never in `Update()`
- **Interval-Based Checks**: `InteractionManager` polls at 10Hz, not every frame
- **Object Pooling**: Efficient UI element reuse
- **Distance-Based Culling**: Interaction checks only within range
- **Unscaled Time**: Play time tracking uses `Time.unscaledDeltaTime` — unaffected by game pause

---

## 📂 Project Structure

```
Assets/
├── Scripts/
│   ├── Core Systems/
│   │   ├── PlayerMove.cs              # Player movement & input
│   │   ├── InteractionManager.cs      # Centralized interaction (distance-based, 10Hz)
│   │   ├── UIManager.cs               # UI panel management & IUIWindow registry
│   │   ├── IInteractable.cs           # Full interaction interface (4 members)
│   │   └── GameBalance.cs             # ScriptableObject — all game constants
│   │
│   ├── Inventory System/
│   │   ├── Inventory.cs               # Main inventory logic (36 slots)
│   │   ├── InventorySlot.cs           # UI slot with drag/drop + TroughMode
│   │   ├── InventoryContainer.cs      # Generic item container (used by trough)
│   │   └── ItemStack.cs               # Item stacking system
│   │
│   ├── Farming System/
│   │   ├── SoilBlockInteractable.cs   # Soil interaction (IInteractable + ISaveable)
│   │   ├── CropGrowthManager.cs       # Crop growth logic
│   │   ├── CropData.cs                # Crop ScriptableObject definitions
│   │   └── DualGridTilemap/           # Dual-layer tilemap system
│   │
│   ├── Dialogue System/
│   │   ├── Core/
│   │   │   ├── DialogueTree.cs        # Branching dialogue ScriptableObject
│   │   │   ├── DialogueNode.cs        # Individual dialogue pieces
│   │   │   └── DialogueChoice.cs      # Player choice options
│   │   ├── UI/
│   │   │   └── DialogueTreeUI.cs      # Typewriter dialogue display
│   │   ├── Memory/
│   │   │   └── ConversationMemory.cs  # Persistent conversation state
│   │   └── NPCDialogueInteractable.cs # NPC interaction handler
│   │
│   ├── Game Management/
│   │   ├── SaveManager.cs             # Save/load — 4 slots, auto-save, play time
│   │   ├── GameData.cs                # Game state data structures
│   │   ├── SaveSlotInfo.cs            # Slot metadata (day, season, money, playtime)
│   │   ├── TimeController.cs          # Day/night cycle with events
│   │   └── PlayerStats.cs             # Player statistics (money, etc.)
│   │
│   ├── UI Systems/
│   │   ├── MainMenuUI.cs              # Main menu — New Game, Continue, Settings
│   │   ├── GameMenuUI.cs              # In-game menu — Save/Load slot panels
│   │   ├── GameMenuManager.cs         # In-game menu coordinator
│   │   └── SaveSlotButton.cs          # Slot row UI (name, day, playtime, delete)
│   │
│   ├── Minimap/
│   │   ├── MinimapController.cs       # State machine (Normal/Semi/Fullscreen)
│   │   ├── MinimapCamera.cs           # Camera following & zoom
│   │   ├── MinimapUI.cs               # DOTween UI transitions
│   │   └── MinimapIcon.cs             # NPC/building icon system
│   │
│   └── Animals/
│       ├── Animal.cs                  # Petting, feeding, happiness, production
│       ├── AnimalData.cs              # Animal ScriptableObject (stats, food, particle)
│       ├── AnimalZone.cs              # Zone tracking registered animals
│       ├── AnimalRoster.cs            # Scene-wide animal registry
│       ├── AnimalRosterUI.cs          # Roster info panel
│       └── FeedingTrough.cs           # World trough — stores food, auto-feeds daily
│
├── Scenes/
│   ├── MainMenu.unity                 # Main menu scene
│   └── SampleScene.unity              # Main game scene
│
├── Prefabs/                           # Reusable game objects
├── Sprites/                           # 2D artwork
└── Resources/
    ├── GameBalance.asset              # Central game balance tuning
    └── Animals/                       # Animal ScriptableObject assets
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
   - Open `MainMenu.unity` to start from the menu, or `SampleScene.unity` directly

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

### WebGL Build (GitHub Pages)
1. Switch platform to WebGL in Build Settings
2. Build to `docs/` folder
3. The GitHub Actions workflow (`.github/workflows/deploy-webgl-demo.yml`) auto-deploys on push or manual trigger

---

## 📖 Documentation

- **[CLAUDE.md](CLAUDE.md)** — Comprehensive project architecture, system breakdowns, and bug fix history
- **[GitHub Issues](https://github.com/Joaofranciscopanta/sowur-shield/issues)** — Bug reports and feature requests

---

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit your changes** (`git commit -m 'Add amazing feature'`)
4. **Push to the branch** (`git push origin feature/amazing-feature`)
5. **Open a Pull Request**

Please follow the existing architecture patterns (interface-driven, ScriptableObject data, singleton managers).

---

## 📝 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## 👥 Team

### Core Team
- **[João Francisco Pantaleão](https://www.linkedin.com/in/joaofranciscopantaleao/)** — Owner & Lead Developer
- **[Lucas Daniel](https://www.linkedin.com/in/enf-lucas-daniel/)** — Co-Owner & Main Developer
- **[Isabella Freitas](https://www.linkedin.com/in/isabellafferreira03/)** — Art Director, Dialogue & Character Design

### Technical Stack
- **Engine**: Unity 2022.3.46f1 LTS
- **Language**: C#
- **Development Period**: 2025 – Present

---

## 🌟 Planned Features

- 🏠 **Building System**: Construct barns, silos, and other structures
- 🐄 **Expanded Animals**: Cows, pigs, sheep with unique mechanics
- 🌦️ **Weather System**: Dynamic weather affecting crop growth
- 💍 **NPC Relationships**: Friendship levels and gift-giving
- 🎯 **Quest System**: Task-based progression and rewards
- 🎣 **Fishing System**: Rivers, lakes, and ocean fishing
- 🍳 **Cooking System**: Combine ingredients to create meals
- 🌐 **Multiplayer Support**: Cooperative farming with friends

---

## 📧 Support

- **Issues**: [GitHub Issues](https://github.com/Joaofranciscopanta/sowur-shield/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Joaofranciscopanta/sowur-shield/discussions)

---

## 🙏 Acknowledgments

- Unity Technologies for the game engine
- TextMesh Pro for advanced text rendering
- DOTween for smooth animations
- The indie game development community for inspiration

---

<div align="center">

**[⬆ Back to Top](#-sowur-shield)**

Made with passion by [João Francisco](https://www.linkedin.com/in/joaofranciscopantaleao/), [Lucas Daniel](https://www.linkedin.com/in/enf-lucas-daniel/) & [Isabella Freitas](https://www.linkedin.com/in/isabellafferreira03/)

</div>
