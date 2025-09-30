# Sleep Confirmation Panel - Manual Setup Guide

## Step 1: Create the UI Hierarchy

1. **Create the Main Panel:**
   - Right-click in Hierarchy → UI → Panel
   - Rename to "SleepConfirmationPanel"
   - Set RectTransform: Anchor Min (0,0), Anchor Max (1,1), Left/Right/Top/Bottom all to 0
   - Set the Image color to semi-transparent black: (0, 0, 0, 150)

2. **Create Content Container:**
   - Right-click SleepConfirmationPanel → Create Empty
   - Rename to "ContentContainer"
   - Add Image component, set color to dark gray: (50, 50, 50, 255)
   - Set RectTransform: Anchor Min (0.5, 0.5), Anchor Max (0.5, 0.5), Width 400, Height 300
   - Add Vertical Layout Group component:
     - Padding: Left 20, Right 20, Top 20, Bottom 20
     - Spacing: 15
     - Child Force Expand Width: True
     - Child Force Expand Height: False

3. **Create Title Text:**
   - Right-click ContentContainer → UI → Text - TextMeshPro
   - Rename to "TitleText"
   - Set text: "Sleep Until Morning?"
   - Font Size: 24, Style: Bold, Alignment: Center
   - Content Size Fitter: Vertical Fit: Preferred Size

4. **Create Info Texts:**
   - Right-click ContentContainer → UI → Text - TextMeshPro (repeat 3 times)
   - Rename them: "TimeInfoText", "SellBoxInfoText", "SaveInfoText"
   - Set Font Size: 14, Alignment: Center
   - Content Size Fitter: Vertical Fit: Preferred Size
   - Set placeholder text for now

5. **Create Button Container:**
   - Right-click ContentContainer → Create Empty
   - Rename to "ButtonContainer"  
   - Add Horizontal Layout Group:
     - Spacing: 20
     - Child Force Expand Width: True
     - Child Force Expand Height: True
   - Set Layout Element: Preferred Height: 50

6. **Create Buttons:**
   - Right-click ButtonContainer → UI → Button - TextMeshPro (repeat twice)
   - Rename to "ConfirmButton" and "CancelButton"
   - Set ConfirmButton color to green-ish: (50, 150, 50, 255)
   - Set CancelButton color to red-ish: (150, 50, 50, 255)
   - Set button texts: "Sleep" and "Cancel"

## Step 2: Setup the Script Component

1. **Create Script Holder:**
   - Create Empty GameObject in scene
   - Rename to "SleepConfirmationManager"
   - Add the SleepConfirmationPanel script to it

2. **Assign References in Inspector:**
   - Panel Container: Drag the "SleepConfirmationPanel" GameObject
   - Confirm Button: Drag the "ConfirmButton"  
   - Cancel Button: Drag the "CancelButton"
   - Title Text: Drag "TitleText"
   - Time Info Text: Drag "TimeInfoText"
   - Sell Box Info Text: Drag "SellBoxInfoText"
   - Save Info Text: Drag "SaveInfoText"

## Step 3: Link to Bed

1. **Find your BedInteractable in the scene**
2. **In the BedInteractable inspector:**
   - Find the "Confirmation Panel" field
   - Drag the "SleepConfirmationManager" GameObject to it

## Step 4: Initial Setup

1. **Make sure you have EventSystem in scene:**
   - If not: Right-click Hierarchy → UI → Event System

2. **Set initial state:**
   - Disable the "SleepConfirmationPanel" GameObject (uncheck it)
   - The script will show/hide it when needed

## Step 5: Test

1. **Play the game**
2. **Interact with the bed (press E)**
3. **The confirmation panel should appear with working buttons**
4. **Enter key confirms, ESC key cancels, buttons work with mouse**

---

## Troubleshooting Tips:

- **Buttons not clicking?** Make sure EventSystem exists in scene
- **Panel not showing?** Check that BedInteractable has the script reference
- **Text not updating?** Verify all text components are assigned in inspector
- **ESC opens game menu?** The script should prevent this when panel is open

This manual approach avoids reflection and complex setup code that can cause issues.