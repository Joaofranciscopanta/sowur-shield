# How to Build Sowur Shield Demo

## Quick Start

### In Unity Editor:

1. **Open Unity Project**
   - Open the project in Unity 6000.1.7f1 or compatible version

2. **Build Demo Versions**

   You have three options:

   **Option A: Build All (Recommended)**
   - Go to: `Build > Demo > Build All Demos`
   - This will build both Windows and WebGL versions

   **Option B: Build Individual Platforms**
   - `Build > Demo > Build Windows Demo` - For Windows only
   - `Build > Demo > Build WebGL Demo` - For WebGL only

3. **Find Your Builds**
   - Builds are created in: `Builds/` folder in your project root
   - Each build is timestamped: `Windows_Demo_2025-10-05_14-30/`

---

## What Happens During Build

### Automatic Configuration:

1. **Scripting Define Symbol Added**
   - `DEMO_BUILD` is added to Player Settings
   - This disables save/load functionality

2. **Platform-Specific Settings**

   **Windows:**
   - Windowed mode (1920x1080)
   - Compression enabled

   **WebGL:**
   - Brotli compression
   - Data caching disabled
   - Exception support minimal

3. **Clean Up**
   - After build completes, `DEMO_BUILD` symbol is removed
   - Original project settings restored

---

## Build Output Structure

### Windows Build:
```
Builds/
└── Windows_Demo_2025-10-05_14-30/
    ├── Sowur Shield Demo.exe
    ├── Sowur Shield Demo_Data/
    ├── UnityPlayer.dll
    └── UnityCrashHandler64.exe
```

### WebGL Build:
```
Builds/
└── WebGL_Demo_2025-10-05_14-30/
    ├── Build/
    │   ├── WebGL_Demo.data
    │   ├── WebGL_Demo.framework.js
    │   ├── WebGL_Demo.loader.js
    │   └── WebGL_Demo.wasm
    ├── TemplateData/
    └── index.html
```

---

## Adding README to Builds

After building, you can add the demo README:

1. Copy `BUILD_README_TEMPLATE.md` to your build folder
2. Rename it to `README.md`
3. Fill in the {VERSION} and {BUILD_DATE} placeholders
4. Add the `PATCH_NOTES.md` file for full changelog

**Example:**
```bash
# For Windows build
cp BUILD_README_TEMPLATE.md "Builds/Windows_Demo_2025-10-05_14-30/README.md"
cp PATCH_NOTES.md "Builds/Windows_Demo_2025-10-05_14-30/"

# For WebGL build
cp BUILD_README_TEMPLATE.md "Builds/WebGL_Demo_2025-10-05_14-30/README.md"
cp PATCH_NOTES.md "Builds/WebGL_Demo_2025-10-05_14-30/"
```

---

## Distribution

### Windows Distribution:

1. **Compress the folder**
   ```bash
   # Create ZIP archive
   cd Builds
   zip -r "SowurShield_Windows_Demo_v0.9.5.zip" Windows_Demo_2025-10-05_14-30/
   ```

2. **Upload to:**
   - GitHub Releases
   - Itch.io
   - Your website

### WebGL Distribution:

1. **Upload to Hosting**
   - Upload entire `WebGL_Demo_XXXX/` folder to web server
   - OR use platforms like:
     - Itch.io (has WebGL support)
     - GitHub Pages
     - Netlify / Vercel

2. **Itch.io Upload**
   - Create new project on itch.io
   - Select "HTML" for project type
   - Upload the entire WebGL build folder as a ZIP
   - Mark `index.html` as the main file

---

## Testing Your Builds

### Windows:
1. Extract the ZIP
2. Run the `.exe` file
3. Test all core features
4. Verify save/load is disabled (Continue button should be hidden)

### WebGL:
1. Test locally: Open `index.html` in a browser
   - Note: Some browsers may block local WebGL, use a local server:
   ```bash
   # Python 3
   cd Builds/WebGL_Demo_XXXX/
   python -m http.server 8000
   # Then open: http://localhost:8000
   ```

2. Test hosted version after upload
3. Check console for errors (F12 Developer Tools)

---

## Troubleshooting

### Build Fails:

**Error: No scenes in build settings**
- Go to: `File > Build Settings`
- Add your scenes (MainMenu, SampleScene, etc.)
- Make sure they're checked/enabled

**Error: WebGL not installed**
- Go to: `Unity Hub > Installs > Add Modules`
- Add WebGL Build Support

**Error: Missing assemblies**
- Close Unity
- Delete `Library/` folder
- Reopen project (let Unity reimport)

### Build Succeeds but Game Crashes:

**Windows:**
- Check `Sowur Shield Demo_Data/output_log.txt`
- Look for missing DLLs or assets

**WebGL:**
- Open browser console (F12)
- Look for JavaScript errors or missing assets

---

## Advanced: Command Line Building

You can also build from command line:

```bash
# Windows build
"C:\Program Files\Unity\Hub\Editor\6000.1.7f1\Editor\Unity.exe" ^
  -quit -batchmode ^
  -projectPath "C:\Users\YourUser\Sowur Shield\Sowur Shield" ^
  -executeMethod DemoBuildScript.BuildWindowsDemo ^
  -logFile build_log.txt

# WebGL build
"C:\Program Files\Unity\Hub\Editor\6000.1.7f1\Editor\Unity.exe" ^
  -quit -batchmode ^
  -projectPath "C:\Users\YourUser\Sowur Shield\Sowur Shield" ^
  -executeMethod DemoBuildScript.BuildWebGLDemo ^
  -logFile build_log.txt
```

---

## Notes

- **Build Times**: Windows ~5-10 min, WebGL ~10-20 min (depends on project size)
- **Disk Space**: Windows ~200-500 MB, WebGL ~100-300 MB per build
- **Version Control**: Add `Builds/` to `.gitignore` (already done)

---

## Questions?

- Check Unity Console for build errors
- Review build logs in project root
- Check `DemoBuildScript.cs` for build configuration

---

**Happy Building! 🎮**
