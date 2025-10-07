# NPC Interaction Prompt Canvas Fix Guide

## Problem
The "Press E to talk" canvas is being activated/deactivated correctly (visible in Hierarchy), but the text doesn't appear on screen.

## Root Cause
**Canvas rendering configuration issue** - The canvas is not properly configured for World Space rendering.

---

## ✅ Solution 1: Automatic Fix (Recommended)

### Step 1: Add Debug Script
1. Select your NPC in the Hierarchy
2. Add Component → `NPCCanvasDebugger`
3. Make sure `Auto Fix On Start` is checked (enabled by default)
4. Press Play

The script will automatically diagnose and fix common canvas issues.

### Step 2: Check Console
Look at the Console for diagnostic messages:
- ✅ Green checkmarks = Issues fixed
- ⚠️ Warnings = Things to review
- ❌ Errors = Manual intervention needed

### Step 3: Remove Script (Optional)
Once fixed, you can remove the `NPCCanvasDebugger` script from the NPC.

---

## 🔧 Solution 2: Manual Fix

### Step 1: Select the Canvas
1. In Hierarchy, expand your NPC GameObject
2. Find the child Canvas (usually named "InteractionPrompt" or similar)
3. Select it

### Step 2: Configure Canvas Component
In the Inspector, set:

```
Canvas Component:
├── Render Mode: World Space
├── World Camera: Main Camera (drag from Hierarchy)
├── Sorting Layer: Default (or UI)
└── Order in Layer: 100
```

### Step 3: Configure RectTransform
```
RectTransform:
├── Pos X: 0
├── Pos Y: 2 (above NPC's head)
├── Pos Z: 0
├── Width: 200
├── Height: 100
├── Scale X: 0.01
├── Scale Y: 0.01
└── Scale Z: 0.01
```

### Step 4: Configure Canvas Scaler (if present)
```
Canvas Scaler:
├── UI Scale Mode: Constant Pixel Size
└── Dynamic Pixels Per Unit: 10
```

### Step 5: Check Text Component
Make sure the Text/TextMeshPro component has:
```
TextMeshPro - Text (UI):
├── Text: "Press E to Talk"
├── Font Size: 24 (or higher)
├── Color: White (255, 255, 255, 255)
├── Alpha: 255 (fully opaque)
└── Alignment: Center
```

---

## 🎯 Common Issues & Fixes

### Issue 1: Canvas Scale Too Small/Large
**Symptom**: Text is invisible or gigantic
**Fix**: Set Canvas RectTransform scale to (0.01, 0.01, 0.01)

### Issue 2: Canvas Behind Other Elements
**Symptom**: Text appears but is covered by other UI
**Fix**: Increase Canvas `Order in Layer` to 100+

### Issue 3: No Camera Reference
**Symptom**: Canvas doesn't render at all in World Space
**Fix**: Assign Main Camera to Canvas → World Camera field

### Issue 4: Canvas Position Too Low
**Symptom**: Text appears at NPC's feet or underground
**Fix**: Set Canvas RectTransform Y position to 2 or higher

### Issue 5: Text Alpha/Color Issue
**Symptom**: Text exists but is invisible
**Fix**: Set text color to white with alpha = 255

---

## 🧪 Testing

After applying fixes:

1. **Enter Play Mode**
2. **Walk towards NPC** (within interaction range)
3. **Check Hierarchy**: Canvas should activate ✅
4. **Check Scene View**: Canvas should be visible above NPC ✅
5. **Check Game View**: "Press E to Talk" should appear ✅

---

## 📋 Recommended Canvas Hierarchy

```
NPC GameObject
├── Sprite Renderer (NPC visual)
├── Collider2D (for interactions)
├── NPCDialogueInteractable (script)
└── InteractionPrompt (Canvas - World Space)
    └── Panel (optional background)
        └── Text - "Press E to Talk" (TextMeshPro)
```

---

## 🔍 Debug Commands

With `NPCCanvasDebugger` attached, you can:

1. **Right-click** the script in Inspector
2. Select **"Diagnose Canvas Issues"**
3. Check Console for detailed diagnostic report

---

## 💡 Prevention Tips

When creating new NPCs:

1. **Use a Prefab**: Create one working NPC and duplicate it
2. **World Space Canvas**: Always use World Space for 3D world prompts
3. **Proper Scale**: World Space canvases need small scale (0.01)
4. **High Sorting Order**: Keep interaction prompts above other UI (100+)
5. **Camera Reference**: Always assign the camera in World Space mode

---

## 🆘 Still Not Working?

If the canvas still doesn't appear after trying both solutions:

1. **Check the script references** in NPCDialogueInteractable:
   - Is `interactionPrompt` assigned in Inspector?
   - Is it referencing the correct GameObject?

2. **Check active state**:
   - Is the NPC GameObject active?
   - Is the Canvas GameObject active in Hierarchy when close?

3. **Check layers**:
   - Is the Canvas on a visible layer?
   - Is the layer enabled in the Camera's culling mask?

4. **Check if InteractionManager is working**:
   - Open Console
   - Walk near NPC
   - Look for "Setting prompt visible: true" messages

5. **Try the other NPC script**:
   - You have two NPC scripts: `NPCDialogueInteractable` and `NpcInteractableN`
   - Try switching to see if one works better

---

## 📝 Notes

- The `NPCCanvasDebugger` script is safe to leave attached (it only runs diagnostic checks)
- You can disable `Auto Fix On Start` and use the context menu option instead
- The script shows cyan gizmos in Scene View connecting NPC to canvas position

---

Good luck! 🎮
