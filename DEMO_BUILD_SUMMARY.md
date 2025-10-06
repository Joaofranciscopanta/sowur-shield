# Sowur Shield Demo Build - Setup Complete ✅

## Overview

Your project is now fully configured to build **Windows** and **WebGL** demo versions with save/load functionality disabled.

---

## ✅ What's Been Configured

### 1. **Build Script Created**
   - **File**: `Assets/Scripts/Editor/DemoBuildScript.cs`
   - **Menu Location**: `Build > Demo > [Build Options]`
   - **Features**:
     - Automatic `DEMO_BUILD` define symbol management
     - Platform-specific optimization
     - Timestamped build folders
     - Auto-restoration of project settings

### 2. **Save/Load System Modified**
   - **Files Modified**:
     - `Assets/Scripts/SaveManager.cs`
     - `Assets/Scripts/MainMenuUI.cs`

   - **Changes**:
     - `SaveGame()` - Disabled in demo builds
     - `LoadGame()` - Disabled in demo builds
     - `HasSaveFile()` - Always returns false in demo
     - Continue button - Hidden in main menu for demo

### 3. **Documentation Created**
   - `HOW_TO_BUILD.md` - Complete build instructions
   - `BUILD_README_TEMPLATE.md` - README template for distribution
   - `DEMO_BUILD_SUMMARY.md` - This file

### 4. **Project Settings**
   - Input System backend: Enabled (Both)
   - `.gitignore`: Builds folder excluded
   - Build folders: Configured for `Builds/` directory

---

## 🎮 How to Build

### Option 1: Build Everything (Recommended)
```
Unity Menu: Build > Demo > Build All Demos
```

### Option 2: Build Individual Platforms
```
Windows: Build > Demo > Build Windows Demo
WebGL:   Build > Demo > Build WebGL Demo
```

### Builds will be created in:
```
Sowur Shield/Builds/
├── Windows_Demo_[timestamp]/
└── WebGL_Demo_[timestamp]/
```

---

## 🔧 Technical Details

### Scripting Define Symbol: `DEMO_BUILD`

When this symbol is defined (during demo builds), the following happens:

**SaveManager.cs:**
```csharp
#if DEMO_BUILD
    public void SaveGame() → Returns immediately, logs "DISABLED in demo build"
    public void LoadGame() → Returns immediately, logs "DISABLED in demo build"
    public bool HasSaveFile() → Always returns false
#endif
```

**MainMenuUI.cs:**
```csharp
#if DEMO_BUILD
    continueButton.gameObject.SetActive(false); // Hide Continue button
#endif
```

### Build Process:

1. **Pre-Build**:
   - Add `DEMO_BUILD` to scripting defines
   - Configure platform-specific settings
   - Refresh assets

2. **Build**:
   - Compile with `DEMO_BUILD` active
   - Save/load code is disabled
   - Continue button is hidden

3. **Post-Build**:
   - Remove `DEMO_BUILD` from scripting defines
   - Restore original settings
   - Open build folder

---

## 📦 Distribution Checklist

### Before Distributing:

- [ ] Build both Windows and WebGL versions
- [ ] Test Windows build (run .exe, verify no save/load)
- [ ] Test WebGL build (open index.html, verify no save/load)
- [ ] Copy `BUILD_README_TEMPLATE.md` to build folder as `README.md`
- [ ] Update README with version and date
- [ ] Copy `PATCH_NOTES.md` to build folder
- [ ] Create ZIP archive for Windows build
- [ ] Upload WebGL to hosting platform

### Windows ZIP Structure:
```
SowurShield_Windows_Demo_v0.9.5.zip
└── Sowur Shield Demo/
    ├── README.md (from template)
    ├── PATCH_NOTES.md
    ├── Sowur Shield Demo.exe
    ├── Sowur Shield Demo_Data/
    ├── UnityPlayer.dll
    └── UnityCrashHandler64.exe
```

### WebGL Upload:
```
Upload entire WebGL_Demo_XXXX/ folder to:
- Itch.io (HTML5 game)
- GitHub Pages
- Your web server
```

---

## 🧪 Testing Your Demo Builds

### Verification Checklist:

**Windows:**
- [ ] Game launches without errors
- [ ] Main menu shows only "New Game" (Continue hidden)
- [ ] Gameplay works normally
- [ ] No save files created in AppData
- [ ] Game resets when closed and reopened

**WebGL:**
- [ ] Game loads in browser
- [ ] Main menu shows only "New Game"
- [ ] Controls work properly
- [ ] Performance is acceptable
- [ ] No console errors

---

## 🎯 Demo Features

### ✅ Enabled in Demo:
- All gameplay mechanics
- Inventory system
- Farming (plant, water, harvest)
- NPC dialogue
- Selling items
- Day/night cycle
- All UI functionality

### ❌ Disabled in Demo:
- Save game functionality
- Load game functionality
- Continue button in main menu
- Persistent progress

---

## 📝 Known Limitations

1. **No Save Persistence** - By design for demo
2. **WebGL Performance** - May be slower than native builds
3. **Browser Compatibility** - Chrome/Firefox/Edge recommended for WebGL

---

## 🐛 Troubleshooting

### "Build menu doesn't appear"
- Ensure `DemoBuildScript.cs` is in `Assets/Scripts/Editor/` folder
- Restart Unity Editor

### "DEMO_BUILD symbol not removed after build"
- Manually remove from: `Edit > Project Settings > Player > Other Settings > Scripting Define Symbols`

### "Save system still works in demo"
- Verify `DEMO_BUILD` was added during build (check build logs)
- Ensure code conditional compilation is correct

### "WebGL build is huge"
- Normal! WebGL builds are larger than Windows
- Compression is enabled (Brotli)
- Consider code stripping if size is critical

---

## 🚀 Next Steps

1. **Build your demos**: Use the Build menu
2. **Test thoroughly**: Verify save/load is disabled
3. **Prepare distribution**: Add README and patch notes
4. **Upload**: Share your demo with the world!

---

## 📚 Additional Resources

- **Full Build Instructions**: `HOW_TO_BUILD.md`
- **README Template**: `BUILD_README_TEMPLATE.md`
- **Patch Notes**: `PATCH_NOTES.md`
- **Project Documentation**: `CLAUDE.md`

---

## ✨ Features to Highlight in Demo

When promoting your demo, emphasize:

1. **Recent Refactoring** - 47% code reduction in InventorySlot
2. **Improved Performance** - Better component architecture
3. **Enhanced UI** - Smooth drag & drop, visual feedback
4. **Professional Code** - Clean, maintainable structure

See `PATCH_NOTES.md` for marketing-friendly descriptions!

---

**You're all set to build and distribute your demo! 🎮**

For questions or issues with the build process, check the troubleshooting sections in this document and `HOW_TO_BUILD.md`.

Good luck with your demo release! 🚀
