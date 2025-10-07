# WebGL Build Setup Guide

## Error: "Build target was unsupported"

This error occurs when the **WebGL Build Support module is not installed** in Unity.

---

## ✅ Solution: Install WebGL Build Support

### Method 1: Through Unity Hub (Recommended)

1. **Open Unity Hub**
2. **Go to Installs tab**
3. **Find your Unity version** (2022.3.46f1)
4. **Click the gear icon** (⚙️) next to it
5. **Select "Add Modules"**
6. **Check "WebGL Build Support"**
   - Should also check "WebGL Build Support (IL2CPP)" if available
7. **Click "Install"**
8. **Wait for download and installation** (may take 5-15 minutes)

### Method 2: Through Unity Editor

1. **Open Unity Editor**
2. **Go to** `File > Build Settings`
3. **Select "WebGL"** in the platform list
4. **Click "Open Download Page"** (if module is missing)
5. **Follow Unity Hub instructions** to install the module

### Method 3: Manual Installation

1. **Download Unity Hub** if not installed
2. **Run**: `UnityHub.exe`
3. **Navigate to**: Installs → 2022.3.46f1 → Add Modules
4. **Select**:
   - ✅ WebGL Build Support
   - ✅ WebGL Build Support (IL2CPP)
5. **Install**

---

## 🔍 Verify Installation

After installing, verify the module is available:

1. **Open your project** in Unity
2. **Go to** `File > Build Settings`
3. **Check if "WebGL" appears** in the Platform list
4. **Select "WebGL"** and click "Switch Platform"
5. **If successful**, WebGL is now ready to build

---

## 🚀 Building WebGL Demo

Once WebGL module is installed:

### Option 1: Unity Menu (Recommended)
```
Unity Editor → Build → Demo → Build WebGL Demo
```

### Option 2: Unity Build Settings
1. `File > Build Settings`
2. Select "WebGL"
3. Click "Switch Platform" (if not already selected)
4. Click "Build" and choose output folder

### Option 3: Command Line
```bash
"C:\Program Files\Unity\Hub\Editor\2022.3.46f1\Editor\Unity.exe" \
  -quit \
  -batchmode \
  -projectPath . \
  -executeMethod DemoBuildScript.BuildWebGLDemo \
  -logFile build_log.txt
```

---

## 📦 Expected Build Output

After successful build:

```
Builds/
└── WebGL_Demo/
    ├── Build/
    │   ├── WebGL_Demo.data.unityweb
    │   ├── WebGL_Demo.framework.js.unityweb
    │   ├── WebGL_Demo.loader.js
    │   └── WebGL_Demo.wasm.unityweb
    ├── TemplateData/
    │   ├── favicon.ico
    │   ├── style.css
    │   └── ...
    └── index.html
```

---

## ⚙️ Build Settings (Auto-Configured by Script)

The `DemoBuildScript.cs` automatically configures:

- ✅ **Compression**: Disabled (for local testing)
- ✅ **Exception Support**: None (smaller build size)
- ✅ **Data Caching**: Disabled
- ✅ **Fullscreen Mode**: Windowed
- ✅ **DEMO_BUILD**: Define symbol added

---

## 🐛 Common Issues

### Issue 1: "Scripts are still compiling"
**Solution**: Wait for Unity to finish compiling, then try again.

### Issue 2: "No scenes enabled in Build Settings"
**Solution**:
1. Go to `File > Build Settings`
2. Click "Add Open Scenes"
3. Ensure MainMenu and SampleScene are checked

### Issue 3: Build takes very long
**Cause**: First WebGL build always takes longer (10-30 minutes)
**Solution**: Be patient. Subsequent builds are faster (~5-10 minutes)

### Issue 4: Out of memory during build
**Solution**:
- Close other applications
- Increase virtual memory in Windows
- Build on a machine with more RAM (8GB+ recommended)

### Issue 5: Module installed but still shows error
**Solution**:
1. Close Unity Editor completely
2. Close Unity Hub
3. Reopen Unity Hub
4. Reopen project
5. Try building again

---

## 📊 Build Size Expectations

- **Development Build**: ~100-150 MB
- **Release Build**: ~50-80 MB
- **Compressed (Gzip)**: ~20-30 MB

---

## 🌐 Testing WebGL Build

### Local Testing (No Server Required)

Since the build script disables compression:

1. Navigate to `Builds/WebGL_Demo/`
2. **Double-click** `index.html`
3. Build should open in your default browser

### Local Server Testing (Recommended)

For more accurate testing:

```bash
# Python 3
cd Builds/WebGL_Demo
python -m http.server 8000

# Then open: http://localhost:8000
```

```bash
# Node.js (with http-server)
cd Builds/WebGL_Demo
npx http-server -p 8000

# Then open: http://localhost:8000
```

---

## 📝 GitHub Pages Deployment

After building, deploy to GitHub Pages:

1. **Copy build contents** to `docs/` folder
2. **Restore custom CSS** (see DEPLOYMENT_GUIDE.md)
3. **Commit and push** to GitHub
4. **Enable GitHub Pages** in repository settings

---

## 💡 Tips

- **First build**: Takes 10-30 minutes (be patient!)
- **Incremental builds**: Much faster (5-10 minutes)
- **Clean build**: Delete `Library/` folder if build issues persist
- **Build location**: Check Unity Console for exact output path

---

## 🆘 Still Having Issues?

1. **Check Unity version**: Ensure you're using 2022.3.46f1 LTS
2. **Check disk space**: WebGL builds need ~5GB free space
3. **Check Unity installation**: Repair installation through Unity Hub
4. **Check build log**: Look at Console for specific error messages
5. **Ask for help**: Create an issue on GitHub with error details

---

## ✅ Checklist

Before building WebGL:

- [ ] Unity 2022.3.46f1 LTS installed
- [ ] WebGL Build Support module installed
- [ ] At least 5GB free disk space
- [ ] Scenes added to Build Settings
- [ ] Project compiles without errors
- [ ] No active UI windows blocking build

---

Good luck with your WebGL build! 🚀
