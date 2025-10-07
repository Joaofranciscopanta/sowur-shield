# GitHub Pages Setup for Sowur Shield WebGL Demo

## 📋 Overview

This guide will help you deploy your WebGL demo to GitHub Pages so anyone can play it directly in their browser at:
`https://yourusername.github.io/sowur-shield/`

---

## 🎯 Two Deployment Options

### Option A: Dedicated `gh-pages` Branch (Recommended)
- Keeps demo separate from source code
- Cleaner repository structure
- Faster loading (only demo files)

### Option B: Docs Folder in Main Branch
- Simpler setup
- All in one branch
- Good for smaller projects

**We'll use Option A (gh-pages branch) for best practices.**

---

## 🚀 Step-by-Step Setup

### Step 1: Prepare Your WebGL Build

1. **Build your WebGL demo** (if not already done):
   ```
   Unity Menu: Build > Demo > Build WebGL Demo
   ```

2. **Locate your build folder**:
   ```
   Builds/WebGL_Demo_[timestamp]/
   ```

3. **Important files in the build**:
   ```
   WebGL_Demo_XXXX/
   ├── index.html          ← Main entry point
   ├── Build/
   │   ├── *.data          ← Game assets
   │   ├── *.framework.js  ← Unity framework
   │   ├── *.loader.js     ← Loader
   │   └── *.wasm          ← WebAssembly code
   └── TemplateData/       ← UI assets
   ```

---

### Step 2: Create GitHub Repository (if needed)

If you don't have a GitHub repository yet:

```bash
# On GitHub.com:
# 1. Go to https://github.com/new
# 2. Name: sowur-shield (or your preferred name)
# 3. Description: "2D Farming Simulation Game - WebGL Demo"
# 4. Public repository
# 5. Don't add README/gitignore (you already have them)
# 6. Click "Create repository"
```

---

### Step 3: Set Up gh-pages Branch

#### Method 1: Using Git Commands (Recommended)

```bash
# Navigate to your project root
cd "C:\Users\joaof\Sowur Shield\Sowur Shield"

# Create a new orphan branch (no history from main)
git checkout --orphan gh-pages

# Remove all files from staging (we'll add WebGL build only)
git rm -rf .

# Copy WebGL build files to root
# Replace XXXX with your actual timestamp
cp -r "Builds/WebGL_Demo_XXXX/"* .

# Verify files are in root
ls
# Should show: index.html, Build/, TemplateData/

# Add all WebGL files
git add index.html Build/ TemplateData/

# Create .gitignore for gh-pages branch
echo "# Keep gh-pages clean
*.log
.DS_Store
Thumbs.db" > .gitignore

git add .gitignore

# Commit
git commit -m "Initial WebGL demo deployment"

# Push to GitHub
git push origin gh-pages

# Switch back to main branch
git checkout main
```

#### Method 2: Using GitHub Web Interface (Easier for beginners)

```bash
# 1. Create a temporary 'docs' folder
mkdir docs
cp -r "Builds/WebGL_Demo_XXXX/"* docs/

# 2. Commit and push
git add docs/
git commit -m "Add WebGL demo for GitHub Pages"
git push origin main

# 3. On GitHub.com:
#    - Go to repository Settings
#    - Scroll to "Pages" section
#    - Source: Deploy from branch
#    - Branch: main
#    - Folder: /docs
#    - Click Save

# 4. Wait 1-2 minutes, your demo will be live!
```

---

### Step 4: Enable GitHub Pages

#### If using gh-pages branch:

1. Go to your GitHub repository
2. Click **Settings** tab
3. Scroll to **Pages** section (left sidebar)
4. Configure:
   - **Source**: Deploy from a branch
   - **Branch**: `gh-pages`
   - **Folder**: `/ (root)`
5. Click **Save**

#### If using docs folder:

1. Go to **Settings** > **Pages**
2. Configure:
   - **Source**: Deploy from a branch
   - **Branch**: `main`
   - **Folder**: `/docs`
3. Click **Save**

---

### Step 5: Wait for Deployment

1. GitHub will show: **"Your site is ready to be published at..."**
2. Wait **1-5 minutes** for the build process
3. Refresh the Settings > Pages section
4. You'll see: **"Your site is live at https://yourusername.github.io/sowur-shield/"**

---

## ✅ Verify Deployment

### Check if it worked:

1. **Visit your URL**: `https://yourusername.github.io/sowur-shield/`
2. **Test the game**:
   - Does it load?
   - Can you move the player?
   - Does inventory work?
   - Is Continue button hidden? (demo mode verification)

### Common Issues:

**404 Error:**
- Wait a few more minutes (first deployment takes longer)
- Check branch name is correct in Settings > Pages
- Verify `index.html` is in the root of gh-pages branch

**Blank Page:**
- Open browser console (F12)
- Check for errors
- Verify all files in Build/ folder are present

**Game doesn't load:**
- Check console for CORS errors
- Verify compression is disabled (files should NOT have .gz extension)
- Try hard refresh: Ctrl+F5

---

## 🔄 Updating Your Demo

### When you make changes and rebuild:

```bash
# Build new WebGL version in Unity
# Unity Menu: Build > Demo > Build WebGL Demo

# Switch to gh-pages branch
git checkout gh-pages

# Remove old files
rm -rf Build/ TemplateData/ index.html

# Copy new build files
cp -r "Builds/WebGL_Demo_[NEW_TIMESTAMP]/"* .

# Commit and push
git add .
git commit -m "Update WebGL demo - [describe changes]"
git push origin gh-pages

# Switch back to main
git checkout main

# GitHub Pages will auto-deploy in 1-2 minutes
```

---

## 📦 Repository Structure

### Recommended Structure:

```
Your Repository (main branch):
├── Assets/
├── ProjectSettings/
├── Builds/               ← Excluded from git (.gitignore)
├── CLAUDE.md
├── PATCH_NOTES.md
├── README.md
└── ... (Unity project files)

gh-pages branch (separate, deployed to web):
├── index.html
├── Build/
│   ├── *.data
│   ├── *.framework.js
│   ├── *.loader.js
│   └── *.wasm
└── TemplateData/
```

---

## 🎨 Customizing Your Demo Page

### Add a README to gh-pages branch:

```bash
git checkout gh-pages

# Create a landing page (optional)
cat > README.md << 'EOF'
# Sowur Shield - WebGL Demo

Play the demo: [Launch Game](index.html)

## About
2D farming simulation game built in Unity.

## Features
- Farming mechanics
- Inventory system
- NPC interactions
- Day/night cycle

For source code, visit the [main repository](https://github.com/yourusername/sowur-shield).
EOF

git add README.md
git commit -m "Add README to gh-pages"
git push origin gh-pages

git checkout main
```

---

## 🔗 Sharing Your Demo

### Share this URL:
```
https://yourusername.github.io/sowur-shield/
```

### Embed in README (main branch):

Add this to your main branch `README.md`:

```markdown
# Sowur Shield

2D Farming Simulation Game

## 🎮 Play the Demo

**[Play Now!](https://yourusername.github.io/sowur-shield/)** - Browser demo (WebGL)

## Features
- Advanced farming system
- 36-slot inventory with drag & drop
- NPC dialogue system
- Day/night cycle
- And more!
```

---

## 🛠️ Advanced: Custom Domain (Optional)

If you want a custom domain like `demo.yourgame.com`:

1. Buy a domain from a registrar
2. Add CNAME file to gh-pages branch:
   ```bash
   git checkout gh-pages
   echo "demo.yourgame.com" > CNAME
   git add CNAME
   git commit -m "Add custom domain"
   git push origin gh-pages
   ```
3. Configure DNS at your registrar:
   - Type: CNAME
   - Name: demo (or www)
   - Value: yourusername.github.io

4. In GitHub Settings > Pages:
   - Custom domain: demo.yourgame.com
   - Enforce HTTPS: ✓

---

## 📊 Monitoring

### GitHub Pages Deployment Status:

1. Go to repository **Actions** tab
2. See deployment history
3. Check build logs if deployment fails

### Analytics (Optional):

Add Google Analytics to track visitors:

```html
<!-- Add to index.html in gh-pages branch, before </head> -->
<script async src="https://www.googletagmanager.com/gtag/js?id=YOUR_GA_ID"></script>
<script>
  window.dataLayer = window.dataLayer || [];
  function gtag(){dataLayer.push(arguments);}
  gtag('js', new Date());
  gtag('config', 'YOUR_GA_ID');
</script>
```

---

## 🚨 Troubleshooting

### Issue: Files are compressed (.gz)

**Solution:**
```bash
# Verify in build script that compression is disabled
# Assets/Scripts/Editor/DemoBuildScript.cs line 81:
PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

# Rebuild WebGL demo
```

### Issue: DEMO_BUILD symbol stays in project

**Solution:**
```bash
# Manually remove from Project Settings
# Edit > Project Settings > Player > Other Settings > Scripting Define Symbols
# Remove "DEMO_BUILD" from WebGL platform
```

### Issue: Large file warning on GitHub

GitHub has a 100 MB file limit. If your .wasm or .data files are huge:

**Solutions:**
1. Reduce texture quality in Unity
2. Enable code stripping
3. Use Build Size Analysis in Unity
4. Consider using itch.io for hosting instead (no size limit)

---

## 🎯 Alternative: Deploy to Itch.io

If GitHub Pages has issues, itch.io is excellent for WebGL:

1. Go to https://itch.io
2. Create account
3. Create new project
4. Upload WebGL build as ZIP
5. Set `index.html` as main file
6. Publish!

---

## ✅ Checklist

Before going live:

- [ ] WebGL build is uncompressed (no .gz files)
- [ ] Continue button is hidden (demo mode)
- [ ] Save/load is disabled
- [ ] Game loads and plays correctly
- [ ] README.md on gh-pages explains how to play
- [ ] PATCH_NOTES.md is accessible
- [ ] Main README links to live demo
- [ ] No console errors in browser
- [ ] Tested in Chrome, Firefox, Edge

---

## 🎉 You're Done!

Your demo is now live and accessible to anyone with the URL!

**Next Steps:**
- Share on social media
- Add to portfolio
- Get feedback from players
- Iterate and improve!

---

For questions or issues, check GitHub Pages documentation:
https://docs.github.com/en/pages
