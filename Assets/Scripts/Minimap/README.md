# Minimap System - Quick Reference

## 📁 Files Created

- **MinimapController.cs** - Main state management and input handling (~380 lines)
- **MinimapCamera.cs** - Camera following, zoom, and rendering (~330 lines)
- **MinimapUI.cs** - UI display and smooth transitions (~380 lines)
- **MinimapIcon.cs** - Icon system for marking objects (~300 lines)
- **MinimapSetupGuide.md** - Complete setup instructions
- **README.md** - This quick reference

## 🎯 Quick Start

### Minimum Setup (5 minutes)

1. **Create "Minimap" layer** in Project Settings
2. **Create MinimapCamera GameObject** with Camera + MinimapCamera script
3. **Create MinimapPanel UI** in Canvas with RawImage + CanvasGroup
4. **Create MinimapController GameObject** with MinimapController + MinimapUI scripts
5. **Assign references** between components
6. **Add Input Actions** for M key toggle

**See MinimapSetupGuide.md for detailed step-by-step instructions**

## 🎮 User Controls

| Key/Action | Function | Available In |
|------------|----------|--------------|
| M | Toggle state | All states |
| Mouse Scroll | Zoom in/out | Fullscreen only |
| Arrow Keys | Pan map | Fullscreen only |
| Mouse Drag | Pan map | Fullscreen only |
| ESC | Close fullscreen | Fullscreen only |

## 🔄 State Flow

```
Normal (Corner, 100%)
    ↓ Press M
Semi-Transparent (Corner, 50%)
    ↓ Press M
Fullscreen (Center, 100%, Zoom/Pan)
    ↓ Press M or ESC
Normal (back to start)
```

## 🏗️ Architecture

### Component Hierarchy
```
MinimapController (Manager)
├── MinimapCamera (Camera system)
│   └── RenderTexture
└── MinimapUI (Display)
    └── Canvas → MinimapPanel → RawImage
```

### Integration Points
- **UIManager**: Registered as IUIWindow (priority 5)
- **PlayerMove**: Movement control via DisableMovement()/EnableMovement()
- **Input System**: Uses InputActionReferences or fallback keyboard
- **DOTween**: Smooth transitions and animations

## 📊 Key Settings

### MinimapController
- **Zoom Levels**: [0.5, 1, 2] - Customize as needed
- **Pan Speed**: 10 - Keyboard pan speed
- **Transition Duration**: 0.3s - State change animation time

### MinimapCamera
- **Orthographic Size**: 10 - Default zoom level
- **Render Texture Size**: 1024x1024 - Balance quality/performance
- **Camera Distance**: 100 - Distance in front of world (Z axis for 2D XY plane)
- **Rotation**: (0, 0, 0) - Essential for 2D games on XY plane

### MinimapUI
- **Normal Position**: (-100, -100) - Top-right corner offset
- **Normal Size**: (200, 200) - Corner minimap dimensions
- **Fullscreen Size**: (800, 800) - Fullscreen dimensions

## 🎨 Customization Examples

### Change Corner Position to Top-Left
```csharp
// In MinimapUI Inspector
normalPosition = new Vector2(100, -100); // Top-left

// And update anchors
minimapPanel.anchorMin = new Vector2(0, 1); // Top-left anchor
minimapPanel.anchorMax = new Vector2(0, 1);
minimapPanel.pivot = new Vector2(0, 1);
```

### Add More Zoom Levels
```csharp
// In MinimapController Inspector
zoomLevels = [0.25f, 0.5f, 1f, 1.5f, 2f, 3f]; // 6 levels
```

### Change Semi-Transparent Opacity
```csharp
// In MinimapController Inspector
semiTransparentOpacity = 0.3f; // More transparent
```

### Add Custom Icon to GameObject
```csharp
// In code
MinimapIcon icon = gameObject.AddComponent<MinimapIcon>();
icon.SetIconType(MinimapIconType.Quest);
icon.SetIconColor(Color.cyan);
icon.SetIconSize(2f);
icon.Flash(3f); // Draw attention
```

## 🔧 Common Issues & Solutions

### Issue: Minimap is black
**Solution**: Ensure MinimapCamera Culling Mask is set to "Minimap" layer only, and objects have MinimapIcon components with children on Minimap layer.

### Issue: M key doesn't work
**Solution**: Assign Toggle Minimap input action reference, or use fallback keyboard (script uses Keyboard.current).

### Issue: Player can move in fullscreen
**Solution**: Verify Player reference is assigned in MinimapController and PlayerMove has DisableMovement() method.

### Issue: Transitions are instant/jerky
**Solution**: Check DOTween is installed and Transition Duration > 0 in MinimapController.

## 📚 API Reference

### MinimapController
```csharp
// Public methods
ToggleMinimapState()                    // Cycle to next state
SetMinimapState(MinimapState state)     // Set specific state
ZoomIn() / ZoomOut()                    // Manual zoom control
IsFullscreen()                          // Check current state

// Static access
MinimapController.Instance.SetMinimapState(MinimapState.Fullscreen);
```

### MinimapCamera
```csharp
// Public methods
SetFollowPlayer(bool follow)            // Enable/disable following
SetZoomLevel(float zoom)                // Set zoom (0.5-2)
SetPanOffset(Vector3 offset)            // Manual pan
GetRenderTexture()                      // Get texture for UI
ForceUpdate()                           // Immediate position sync
```

### MinimapUI
```csharp
// Public methods
SetOpacity(float opacity)               // Change opacity
UpdateZoomIndicator(float zoom)         // Update zoom display
SetPlayerMarkerVisible(bool visible)    // Show/hide player marker
SetRenderTexture(RenderTexture tex)     // Change display texture
```

### MinimapIcon
```csharp
// Public methods
SetIconType(MinimapIconType type)       // Change icon type
SetIconColor(Color color)               // Change color
SetIconSize(float size)                 // Change size
Flash(float duration)                   // Flash animation
Pulse(float duration)                   // Pulse animation
SetVisible(bool visible)                // Manual visibility
```

## 🎯 Performance Tips

- Use 512x512 or 1024x1024 RenderTexture (not higher)
- Limit number of always-visible icons
- Use visibility range for distant objects
- Keep icon sprites simple (low complexity)
- Consider disabling minimap updates when paused

## 📦 Dependencies

- **Unity 2021.3+** (or equivalent)
- **DOTween** (already in project)
- **Unity Input System** (already in project)
- **TextMeshPro** (for info display, optional)

## 🔗 Related Documentation

- **MinimapSetupGuide.md** - Complete setup instructions
- **claude.md** - Full project documentation (section 11)
- **IUIWindow.cs** - Window management interface

## ✅ Feature Checklist

Core Features:
- [x] Three-state toggle system
- [x] Smooth DOTween transitions
- [x] Player following in corner modes
- [x] Zoom system (3 levels)
- [x] Pan controls (keyboard + mouse)
- [x] Player movement control
- [x] UIManager integration
- [x] ESC key support
- [x] Icon system
- [x] RenderTexture rendering
- [x] Performance optimized

Optional Features (can be extended):
- [ ] Waypoint system
- [ ] Fog of war
- [ ] Terrain height colors
- [ ] Icon ping system
- [ ] Minimap rotation
- [ ] Multiple minimap cameras
- [ ] Save minimap state

---

**For detailed setup instructions, see MinimapSetupGuide.md**

**For project-wide documentation, see claude.md**
