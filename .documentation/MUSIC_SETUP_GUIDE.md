# Music System Setup Guide

## Overview
This guide explains how to configure the music system for Sowur Shield, including menu music and gameplay music with volume controls.

## Files Created
- **GameMusicManager.cs**: Manages in-game background music with fade transitions
- **Assets/Audio/Music/**: Folder containing all game music tracks

## Music Tracks
1. **OST-The Fields Will Grow.mp3**: Main menu background music
2. **OST-Whispers of the Wandering.mp3**: Gameplay background music
3. **OST-Chud Battle.mp3**: Battle/combat music (ready for future implementation)

## Volume Control System

The game has a three-tier volume control system:

### 1. Master Volume
- Controls the overall volume of ALL audio in the game
- Affects both music and sound effects
- Range: 0.0 to 1.0 (0% to 100%)

### 2. Music Volume
- Controls only background music volume
- Multiplied by Master Volume
- Range: 0.0 to 1.0 (0% to 100%)

### 3. SFX Volume (Sound Effects)
- Controls only game sound effects (clicks, actions, etc.)
- Multiplied by Master Volume
- Range: 0.0 to 1.0 (0% to 100%)

**Formula**: `Final Volume = Base Volume × Master Volume × Category Volume`

Example:
- Base Volume: 0.7
- Master Volume: 0.8 (80%)
- Music Volume: 0.9 (90%)
- **Result**: 0.7 × 0.8 × 0.9 = 0.504 (50.4%)

## Unity Setup Instructions

### Step 1: Import Music Files

The music files are already copied to `Assets/Audio/Music/`. Unity will automatically import them.

**Configure Import Settings** (in Unity Inspector):
1. Select each .mp3 file in `Assets/Audio/Music/`
2. In Inspector, configure:
   - **Load Type**: Streaming (for long music tracks)
   - **Preload Audio Data**: Unchecked
   - **Compression Format**: Vorbis
   - **Quality**: 70-100 (adjust based on file size needs)
3. Click "Apply"

### Step 2: Setup Main Menu Music

**In MainMenu Scene:**

1. **Find or Create MainMenuManager GameObject**:
   - In Hierarchy, locate the GameObject with `MainMenuManager` component
   - If it doesn't exist, create one: `GameObject` → `Create Empty` → Name: "MainMenuManager"

2. **Add Audio Source**:
   - With MainMenuManager selected, click `Add Component`
   - Add `Audio Source` component
   - Configure:
     - ☐ Play On Awake: **Unchecked**
     - ☑ Loop: **Checked**
     - Spatial Blend: **2D** (fully 2D)
     - Volume: 1.0

3. **Configure MainMenuManager Component**:
   - Locate the `MainMenuManager` script component
   - In Inspector, find **Audio** section:
     - **Menu Music Clip**: Drag `OST-The Fields Will Grow` from Assets/Audio/Music
     - **Menu Music Source**: Drag the Audio Source component (or it will auto-find it)
     - **Play Music On Start**: ☑ Checked
     - **Music Volume**: 0.7 (adjust to taste)

### Step 3: Setup Gameplay Music

**In MainGameScene (or SampleScene):**

1. **Create GameMusicManager GameObject**:
   - In Hierarchy: Right-click → `Create Empty`
   - Name: "GameMusicManager"
   - Add Component → Search for `GameMusicManager`

2. **Add Audio Source**:
   - With GameMusicManager selected, click `Add Component`
   - Add `Audio Source` component
   - Configure:
     - ☐ Play On Awake: **Unchecked**
     - ☑ Loop: **Checked**
     - Spatial Blend: **2D** (fully 2D)
     - Volume: 1.0

3. **Configure GameMusicManager Component**:
   - In Inspector, find **Music Settings**:
     - **Gameplay Music**: Drag `OST-Whispers of the Wandering` from Assets/Audio/Music
     - **Play On Start**: ☑ Checked
     - **Music Volume**: 0.7 (adjust to taste)
     - **Fade In Duration**: 1.5 seconds
     - **Fade Out Duration**: 1.0 seconds
   - In **Audio Source** section:
     - **Music Source**: Drag the Audio Source component (or it will auto-find it)

### Step 4: Configure Volume Sliders (Already Done)

The volume control UI is already implemented in:
- **MainMenuUI.cs**: Settings panel with volume sliders
- **GameMenuUI.cs**: In-game settings with volume sliders

Both support:
- Master Volume slider
- Music Volume slider
- SFX Volume slider

All sliders save to PlayerPrefs and update in real-time.

### Step 5: Test the Music System

**Test Main Menu Music:**
1. Play the MainMenu scene
2. Music should start automatically: "The Fields Will Grow"
3. Open Settings → Adjust Music Volume slider → Music should respond
4. Adjust Master Volume slider → Music should respond
5. SFX slider should NOT affect music

**Test Gameplay Music:**
1. Start a new game or continue
2. Music should fade in: "Whispers of the Wandering"
3. Menu music should stop automatically
4. Open in-game menu (ESC) → Settings → Test volume controls
5. Return to main menu → Gameplay music should fade out

**Test Volume Controls:**
- Master = 100%, Music = 100% → Full volume
- Master = 50%, Music = 100% → Half volume
- Master = 100%, Music = 50% → Half volume
- Master = 0% → Complete silence (all audio)
- Music = 0% → No music, SFX still play

## Advanced Features

### Fade Transitions
The GameMusicManager supports smooth fade in/out:
- Fade in when game starts (default: 1.5s)
- Fade out when returning to menu (default: 1.0s)
- Prevents abrupt audio cuts

### DontDestroyOnLoad
GameMusicManager persists between scenes:
- Music continues seamlessly during scene transitions
- Singleton pattern prevents duplicate instances
- Automatically stops when returning to main menu

### Volume Integration
Both music managers integrate with PlayerPrefs:
```csharp
float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
```

Settings persist between sessions.

## Troubleshooting

### Music Not Playing

**Check 1: Audio Source Configuration**
- Ensure Audio Source is attached to the manager GameObject
- Verify "Mute" is unchecked
- Check "Volume" is > 0

**Check 2: Script Configuration**
- Verify music clip is assigned in Inspector
- Check "Play On Start" is enabled
- Ensure volume settings are not 0

**Check 3: Volume Settings**
- Open Settings in-game
- Check Master Volume is not 0%
- Check Music Volume is not 0%

**Check 4: Audio Listener**
- MainCamera should have Audio Listener component
- Only ONE Audio Listener should exist in scene

### Music Too Loud/Quiet

Adjust in this order:
1. **Base Volume** in script (0.7 default)
2. **Audio Source Volume** in Inspector
3. **In-game sliders** (Master/Music)

### Music Doesn't Stop When Changing Scenes

- Ensure GameMusicManager has `DontDestroyOnLoad` (already configured)
- Check `OnReturnToMainMenu()` is called when quitting to menu
- Verify only one instance of GameMusicManager exists

### Multiple Music Sources Playing

- Check for duplicate GameMusicManager GameObjects
- Verify Singleton pattern is working (only one instance)
- Look for other AudioSources with music clips

## Future Enhancements

### Battle Music (OST-Chud Battle.mp3)
Ready for implementation when combat system is added:
```csharp
// Example usage
public void StartBattle()
{
    GameMusicManager.Instance.PlayMusic(battleMusicClip, 0.5f);
}

public void EndBattle()
{
    GameMusicManager.Instance.PlayMusic(gameplayMusicClip, 1.0f);
}
```

### Ambient Sounds
Add environmental sounds:
- Day/night ambient loops
- Weather sounds (rain, wind)
- Farm animal sounds
- Tool sounds (already implemented)

### Music Playlist System
Future enhancement for variety:
- Multiple gameplay tracks
- Random selection or sequential play
- Smooth crossfade between tracks

## API Reference

### GameMusicManager

**Public Methods:**
```csharp
// Play music with optional fade
void PlayMusic(AudioClip clip, float fadeTime = 0f)

// Stop music with optional fade
void StopMusic(float fadeTime = 0f)

// Pause/Resume
void PauseMusic()
void ResumeMusic()

// Update volume from settings
void UpdateVolume()

// Change music clip
void SetGameplayMusic(AudioClip clip)
```

**Usage Examples:**
```csharp
// Play with 2-second fade in
GameMusicManager.Instance.PlayMusic(newTrack, 2f);

// Stop with 1-second fade out
GameMusicManager.Instance.StopMusic(1f);

// Update volume after settings change
GameMusicManager.Instance.UpdateVolume();
```

### MainMenuManager (Music-Related)

**Public Methods:**
```csharp
// Update audio volumes from settings
void UpdateAudioVolumes()
```

Both managers automatically handle volume changes from the settings UI.

## Credits

All music tracks composed by Lucas Quintanilha (luqui):
- "The Fields Will Grow" - Main Menu Theme
- "Whispers of the Wandering" - Gameplay Theme
- "Chud Battle" - Battle Theme (future use)

Location: `Assets/Audio/Music/`
