# WebGL Deployment Setup Guide

This guide will walk you through setting up automated WebGL deployment from Unity Cloud Build to GitHub Pages.

## 🎯 What You'll Accomplish

After following this guide:
- ✅ Automated weekly deployments of your Unity WebGL builds to GitHub Pages
- ✅ Custom CSS preservation (sidebar styling maintained automatically)
- ✅ Build verification before deployment
- ✅ Automatic backup system for rollbacks
- ✅ Manual deployment capability when needed

---

## 📋 Prerequisites

Before starting, ensure you have:

1. **Unity Cloud Build** configured with a WebGL build target
2. **GitHub Pages** enabled for this repository
3. **Admin access** to this GitHub repository (to add secrets)
4. **Unity Cloud Build API key** (we'll get this in Step 1)

---

## 🚀 Setup Instructions

### Step 1: Get Unity Cloud Build API Key

1. Go to [Unity Cloud Services Dashboard](https://build.cloud.unity.com/)
2. Sign in with your Unity account
3. Click on your profile/organization in the top right
4. Select **"Cloud Build Preferences"** or **"API Keys"**
5. Generate a new API key if you don't have one
6. **Copy the API key** (you'll need it in Step 3)

### Step 2: Get Unity Cloud Build IDs

You need three IDs from your Unity Cloud Build URL:

1. Go to your Unity Cloud Build dashboard
2. Navigate to your WebGL build target
3. Look at the URL - it will be in this format:
   ```
   https://build.cloud.unity.com/orgs/{ORG_ID}/projects/{PROJECT_ID}/buildtargets/{BUILD_TARGET_ID}/
   ```

4. **Write down these values**:
   - `ORG_ID` - Your organization ID
   - `PROJECT_ID` - Your project ID
   - `BUILD_TARGET_ID` - Your WebGL build target ID

**Example URL**:
```
https://build.cloud.unity.com/orgs/my-studio-123/projects/sowur-shield-456/buildtargets/webgl-789/
```

From this URL:
- ORG_ID = `my-studio-123`
- PROJECT_ID = `sowur-shield-456`
- BUILD_TARGET_ID = `webgl-789`

### Step 3: Add GitHub Secrets

Now add these secrets to your GitHub repository:

1. Go to your repository on GitHub
2. Click **Settings** (top right)
3. In the left sidebar, click **Secrets and variables** → **Actions**
4. Click **New repository secret** button
5. Add each of these secrets:

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `UNITY_API_KEY` | Your API key from Step 1 | Unity Cloud Build API authentication |
| `UNITY_ORG_ID` | Your org ID from Step 2 | Unity organization identifier |
| `UNITY_PROJECT_ID` | Your project ID from Step 2 | Unity project identifier |
| `UNITY_BUILD_TARGET_ID` | Your build target ID from Step 2 | WebGL build target identifier |

**For each secret**:
- Click "New repository secret"
- Enter the **Name** (exactly as shown above)
- Paste the **Value**
- Click "Add secret"

### Step 4: Verify GitHub Pages Settings

1. Go to repository **Settings** → **Pages**
2. Under **Source**, ensure it's set to:
   - **Branch**: `main`
   - **Folder**: `/docs`
3. Click **Save** if you made changes

Your GitHub Pages site should be at:
```
https://joaofranciscopanta.github.io/sowur-shield/
```

### Step 5: Commit and Push Deployment Files

All the deployment files have been created. Now commit them:

```bash
# Stage all new deployment files
git add .github/

# Add updated CLAUDE.md
git add CLAUDE.md

# Commit
git commit -m "feat: add automated WebGL deployment workflow

- Automated weekly deployment from Unity Cloud Build
- Custom CSS preservation system
- Build verification and health checks
- Backup and rollback capability
- Manual deployment trigger support"

# Push to main branch
git push origin main
```

### Step 6: Test the Workflow

Let's test the deployment manually before relying on the schedule:

1. Go to your repository on GitHub
2. Click **Actions** tab (top menu)
3. In the left sidebar, click **"Deploy WebGL Demo to GitHub Pages"**
4. Click the **"Run workflow"** button (right side)
5. Select branch: `main`
6. Leave build number empty (will use latest)
7. Click **"Run workflow"**

The workflow will:
- ✅ Download latest Unity Cloud Build
- ✅ Extract the WebGL build
- ✅ Restore custom CSS
- ✅ Verify build integrity
- ✅ Create backup tag
- ✅ Deploy to docs/ folder
- ✅ Push to GitHub
- ✅ Verify GitHub Pages deployment

### Step 7: Monitor the Deployment

1. Click on the workflow run that just started
2. Watch the real-time logs as each step executes
3. **Green checkmarks** = success ✅
4. **Red X's** = failure ❌ (check logs for details)

If successful, you'll see a summary with:
- Unity Cloud Build number used
- Download status
- Deployment status
- Link to your live demo

### Step 8: Verify Live Demo

After workflow completes:

1. Wait 2-5 minutes for GitHub Pages to rebuild
2. Visit your demo: https://joaofranciscopanta.github.io/sowur-shield/
3. Verify the game loads correctly
4. Check that the **release notes sidebar** is visible (left side)
5. Confirm custom CSS styling is present

---

## ✅ Setup Complete!

Your automated deployment system is now active!

### What Happens Next?

**Automatic Deployments**:
- Every **Sunday at 3 AM UTC**, the workflow automatically runs
- Downloads latest Unity Cloud Build
- Deploys to GitHub Pages
- No manual intervention needed

**Manual Deployments**:
- Trigger anytime via GitHub Actions → "Run workflow"
- Useful for immediate deployments after important builds

**Monitoring**:
- Check **Actions** tab for deployment history
- Each run creates a detailed summary
- Backup tags created for every deployment

---

## 🔧 Customizing the Schedule

To change the deployment frequency, edit `.github/workflows/deploy-webgl-demo.yml`:

**Daily deployments** (3 AM UTC every day):
```yaml
schedule:
  - cron: '0 3 * * *'
```

**Twice weekly** (Sunday and Wednesday at 3 AM):
```yaml
schedule:
  - cron: '0 3 * * 0'  # Sunday
  - cron: '0 3 * * 3'  # Wednesday
```

**Monthly** (1st of month at 3 AM):
```yaml
schedule:
  - cron: '0 3 1 * *'
```

**Manual only** (disable automatic deployments):
```yaml
# Comment out or remove the schedule section
# schedule:
#   - cron: '0 3 * * 0'
```

---

## 🛠️ Troubleshooting

### Workflow Fails at "Download Unity Cloud Build"

**Problem**: Cannot authenticate with Unity API

**Solutions**:
- ✅ Verify `UNITY_API_KEY` secret is correct
- ✅ Check API key has access to the organization
- ✅ Ensure API key hasn't expired
- ✅ Verify ORG_ID, PROJECT_ID, BUILD_TARGET_ID are correct

### Workflow Fails at "Verify Build"

**Problem**: Build structure doesn't match expected format

**Solutions**:
- ✅ Ensure Unity Cloud Build target is WebGL (not other platform)
- ✅ Check Unity build completed successfully
- ✅ Try downloading build manually to verify structure
- ✅ If needed, skip verification: Run workflow with "skip_verification" checked

### CSS Not Preserved

**Problem**: Demo loads but sidebar styling is missing

**Solutions**:
- ✅ Verify `.github/templates/style.css` exists and contains sidebar CSS
- ✅ Check workflow logs for "Restore Custom CSS" step
- ✅ Manually run: `./.github/scripts/restore-css.sh`
- ✅ Commit and push if CSS was fixed

### GitHub Pages Not Updating

**Problem**: Workflow succeeds but demo doesn't update

**Solutions**:
- ✅ Wait 5 minutes (GitHub Pages rebuild takes time)
- ✅ Hard refresh browser (Ctrl+Shift+R / Cmd+Shift+R)
- ✅ Check Settings → Pages → Source is "main branch /docs folder"
- ✅ Verify workflow pushed changes (check commits)

### No Successful Unity Builds Found

**Problem**: Workflow can't find a successful WebGL build

**Solutions**:
- ✅ Check Unity Cloud Build has completed at least one successful WebGL build
- ✅ Ensure build status is "success" (not "failed" or "canceled")
- ✅ Try triggering a new Unity Cloud Build manually
- ✅ Verify BUILD_TARGET_ID matches your WebGL target (not iOS/Android)

---

## 📚 Additional Resources

- **GitHub Actions Documentation**: https://docs.github.com/actions
- **Unity Cloud Build API**: https://build-api.cloud.unity3d.com/docs/
- **GitHub Pages Documentation**: https://docs.github.com/pages
- **Cron Schedule Examples**: https://crontab.guru/

---

## 🎉 Success Checklist

Before considering setup complete, verify:

- [ ] All 4 GitHub secrets added correctly
- [ ] Manual workflow run succeeded
- [ ] Live demo accessible at GitHub Pages URL
- [ ] Release notes sidebar visible with custom styling
- [ ] Game loads and plays correctly
- [ ] Workflow logs show all green checkmarks
- [ ] Backup tag created (`backup/webgl-demo-*`)

---

## 💡 Pro Tips

1. **Test deployments on Friday**: Gives you the weekend to fix issues before Monday
2. **Monitor GitHub Actions minutes**: Free tier has 2,000 minutes/month
3. **Use manual triggers for urgent deploys**: Don't wait for scheduled run
4. **Check backup tags regularly**: Ensure rollback capability is maintained
5. **Review deployment summaries**: Quick way to verify everything worked

---

## 🚨 Need Help?

If you encounter issues:

1. Check workflow logs in GitHub Actions
2. Review CLAUDE.md troubleshooting section
3. Test scripts locally:
   ```bash
   ./.github/scripts/verify-build.sh docs
   ./.github/scripts/restore-css.sh
   ```
4. Create a GitHub issue with:
   - Workflow run link
   - Error messages from logs
   - Steps you've tried

---

**Deployment system created with ❤️ for Sowur Shield**
