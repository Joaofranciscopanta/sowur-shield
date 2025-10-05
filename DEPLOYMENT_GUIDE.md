# WebGL Demo Deployment Guide

## Quick Deployment Workflow

### 1. Build WebGL Demo in Unity
```
Build → Demo → Build WebGL Demo
```

This creates: `Builds/WebGL_Demo/` with fixed filenames:
- `Build/WebGL_Demo.data`
- `Build/WebGL_Demo.framework.js`
- `Build/WebGL_Demo.loader.js`
- `Build/WebGL_Demo.wasm`

### 2. Copy Build to docs/ Folder
```bash
# Delete old build files
rm -rf docs/Build/

# Copy new build
cp -r "Builds/WebGL_Demo/Build" docs/
cp "Builds/WebGL_Demo/index.html" docs/index_unity.html  # Optional: backup Unity's generated index
```

**IMPORTANT:** Do NOT overwrite `docs/index.html` - it contains your custom release notes sidebar!

### 3. Commit and Push
```bash
git add docs/Build/
git commit -m "Update WebGL demo build"
git push origin main
```

### 4. Wait 1-2 Minutes
GitHub Pages will automatically deploy at:
https://joaofranciscopanta.github.io/sowur-shield/

## File Structure

```
docs/
├── index.html           ← Custom page with release notes (DON'T OVERWRITE!)
├── Build/
│   ├── WebGL_Demo.data
│   ├── WebGL_Demo.framework.js
│   ├── WebGL_Demo.loader.js
│   └── WebGL_Demo.wasm
└── TemplateData/
    ├── style.css        ← Custom styles for release notes
    ├── favicon.ico
    ├── fullscreen-button.png
    ├── progress-bar-*.png
    └── unity-logo-*.png
```

## Important Notes

✅ **Build outputs now use FIXED filenames** - no more timestamps!
✅ **index.html never needs updating** - filenames stay the same
✅ **Just copy Build folder and push** - that's it!

❌ **Don't overwrite docs/index.html** - it has custom release notes
❌ **Don't commit Builds/ folder** - only copy to docs/

## Troubleshooting

### Build folder files have wrong names
- Rebuild using Unity menu: `Build → Demo → Build WebGL Demo`
- Check `DemoBuildScript.cs` uses fixed names (no timestamps)

### Demo won't load on GitHub Pages
- Check `docs/Build/` folder is committed (not ignored by .gitignore)
- Verify all 4 files present: .data, .framework.js, .loader.js, .wasm
- Check browser console for errors

### Changes not appearing
- Wait 1-2 minutes for GitHub Pages to rebuild
- Hard refresh: Ctrl+Shift+R (Windows) or Cmd+Shift+R (Mac)
- Clear browser cache if needed
