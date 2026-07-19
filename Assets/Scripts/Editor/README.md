# Unity Editor Tools - Scene Cleanup Suite

These custom Unity Editor tools help fix the issues identified in the senior game designer review.

## 📁 Files

### SceneCleanupTools.cs
General scene cleanup utilities.

**Menu Location**: `Tools > Scene Cleanup`

**Features**:
- Fix Manager Positions - Resets all managers to (0,0,0)
- Fix Rotation Values - Snaps rotations to clean angles
- Report Scene Health - Analyzes scene issues

**Usage**:
```
Tools > Scene Cleanup > Fix Manager Positions
Tools > Scene Cleanup > Fix Rotation Values
Tools > Scene Cleanup > Report Scene Health
```

---

### InventoryUIConverter.cs
Converts manual inventory slots to prefab-based system.

**Menu Location**: `Tools > Inventory`

**Features**:
- Just Create Prefab - Creates prefab without deleting slots
- Convert to Prefab System - Full conversion (irreversible!)

**Usage**:
```
1. Tools > Inventory > Just Create Prefab (Don't Delete)
2. Review the prefab created
3. Tools > Inventory > Convert to Prefab System
4. Follow on-screen instructions
```

**⚠️ Warning**: "Convert to Prefab System" deletes all 36 manual slots. Commit your scene first!

---

### CanvasConsolidationTool.cs
Helps consolidate multiple Canvas objects into efficient structure.

**Menu Location**: `Tools > Canvas`

**Features**:
- Analyze Canvas Structure - Reports current canvas setup
- Create Recommended Canvas Structure - Creates proper hierarchy
- Show Canvas Consolidation Guide - Opens detailed walkthrough

**Usage**:
```
1. Tools > Canvas > Analyze Canvas Structure (see what you have)
2. Tools > Canvas > Show Canvas Consolidation Guide (read the plan)
3. Tools > Canvas > Create Recommended Canvas Structure (create new structure)
4. Manually move UI elements
5. Update scripts to use CanvasGroups
6. Delete old canvases
```

---

### MapEditorCleanup.cs
Handles MapEditor scene separation and cleanup.

**Menu Location**: `Tools > MapEditor`

**Features**:
- Analyze MapEditor Issue - Reports current state
- Disable MapEditor in Scene - Quick fix
- Create MapEditor Scene - **RECOMMENDED** solution
- Wrap Scripts in #ifdef - Alternative solution

**Usage (Recommended)**:
```
Tools > MapEditor > Create MapEditor Scene (RECOMMENDED)
```

This creates a separate MapEditorScene.unity and moves all editor tools there.

**Usage (Alternative)**:
```
Tools > MapEditor > Disable MapEditor in Scene
```

---

## 🚀 Quick Start

### First Time Setup

1. **Open Unity**
2. **Open SampleScene.unity**
3. **Run Scene Health Check**:
   ```
   Tools > Scene Cleanup > Report Scene Health
   ```
4. **Review the Console** to see issues

### Apply Quick Fixes (15 minutes)

Run these in order:
```
1. Tools > Scene Cleanup > Fix Manager Positions
2. Tools > Scene Cleanup > Fix Rotation Values
3. Tools > MapEditor > Create MapEditor Scene (RECOMMENDED)
```

### Major Improvements (Hours)

Follow the **SCENE_CLEANUP_MASTER_GUIDE.md** in the root folder for detailed step-by-step instructions.

---

## 📋 Tool Output

All tools output to the Unity Console with color-coded messages:
- 🟢 **Green**: Success/Completion
- 🟡 **Yellow**: Warnings/Important Notes
- 🔴 **Red**: Errors

---

## 🔄 Undo Support

Most tools support Unity's Undo system:
- **Ctrl+Z** (Windows) or **Cmd+Z** (Mac) to undo changes
- Works for:
  - Manager position fixes
  - Rotation fixes
  - Canvas creation (before save)
- **Does NOT work for**:
  - Inventory conversion (destructive)
  - MapEditor scene creation (creates new file)

**Always commit your scene to git before using destructive tools!**

---

## 🐛 Troubleshooting

### "Tools menu not showing!"
**Solution**:
1. Check Console for compilation errors
2. Ensure files are in `Assets/Scripts/Editor/` folder
3. Reimport scripts: Right-click folder → Reimport

### "Script compilation errors!"
**Solution**:
1. Check that you have TextMeshPro installed
2. Verify Unity version is 2020.3+ (uses newer UI system)
3. Check Console for specific errors

### "Tools run but nothing happens!"
**Solution**:
1. Check Console output
2. Verify you're in the correct scene (SampleScene.unity)
3. Ensure objects exist (e.g., "Managers" parent, "Hotbar", etc.)

---

## 📖 Additional Documentation

- **SCENE_CLEANUP_MASTER_GUIDE.md** - Complete walkthrough
- **INVENTORY_CONVERSION_INSTRUCTIONS.txt** - Created after inventory conversion
- **CANVAS_CONSOLIDATION_GUIDE.txt** - Created by Canvas tools

---

## 🎯 Best Practices

### Before Using Any Tool:
1. **Commit your scene** to git
2. **Run Scene Health Report** to understand current state
3. **Read the relevant guide** first
4. **Test in a backup branch** if unsure

### After Using Tools:
1. **Check Console** for success messages
2. **Test the game** to verify functionality
3. **Save the scene** if satisfied
4. **Run Scene Health Report** again to verify improvements

---

## 🔧 Extending These Tools

Want to add your own cleanup tools?

**Template**:
```csharp
using UnityEngine;
using UnityEditor;

namespace SowurShield.Editor
{
    public class MyCustomTool : EditorWindow
    {
        [MenuItem("Tools/MyCategory/My Tool Name")]
        public static void MyToolFunction()
        {
            // Your tool logic here
            Debug.Log("<color=green>✓ Tool completed!</color>");
        }
    }
}
```

**Best Practices**:
- Use `Undo.RecordObject()` for undoable changes
- Color-code console output (green/yellow/red)
- Show confirmation dialogs for destructive operations
- Create instruction files for complex workflows
- Always validate that required objects exist

---

## 📝 Notes

These tools were created in response to a senior game designer review that identified several scene architecture issues. They are designed to:
- Automate tedious fixes
- Enforce Unity best practices
- Improve scene maintainability
- Teach better workflows

**Author**: Claude (AI Assistant)
**Date**: December 2025
**Purpose**: Scene Architecture Cleanup
**Status**: Production Ready

---

## ⚠️ Important Warnings

### Irreversible Operations
These tools perform **irreversible** operations:
- InventoryUIConverter > Convert to Prefab System (deletes 36 slots)
- MapEditorCleanup > Create MapEditor Scene (moves objects to new scene)

**Always commit to git first!**

### Scene-Specific
Most tools expect specific scene structure:
- "Managers" parent object
- "Hotbar" object with inventory slots
- "MapEditor" object (if present)

If your scene structure differs, tools may not work correctly.

### Unity Version
Tested on Unity 2022.3.x LTS
Should work on Unity 2020.3+ (uses standard UI)

---

## 🆘 Need Help?

1. Check Console output (usually very descriptive)
2. Read SCENE_CLEANUP_MASTER_GUIDE.md
3. Review Unity documentation for concepts
4. Search Unity forums for specific errors
5. Ask on Unity Discord/Subreddit

Remember: These are helpers, not magic! Understanding WHY they work is more important than just running them.

---

**Happy cleaning! 🧹✨**
