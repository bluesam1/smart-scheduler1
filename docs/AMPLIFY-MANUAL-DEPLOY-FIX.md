# Fixing Amplify 404 Errors for Manual ZIP Deployments

If you're getting 404 errors (even for `/index.html`), the files aren't being served. Here's how to fix it:

## The Problem

When you manually upload a ZIP file to Amplify via the console, Amplify might not extract or serve the files correctly. This is a known limitation of manual ZIP uploads.

## Solution: Verify and Redeploy

### Step 1: Verify ZIP Structure

The ZIP should have files at the root:
- ✅ `index.html` (at root)
- ✅ `_next/` directory (at root)
- ✅ Other directories like `jobs/`, `contractors/`, etc. (at root)

**Check your ZIP:**
```bash
# On Windows (Git Bash), you can use Python:
python -c "import zipfile; z = zipfile.ZipFile('frontend-build.zip'); print('\n'.join([e for e in z.namelist() if '/' not in e or e == 'index.html'][:20]))"
```

### Step 2: Upload via Console

1. Go to: https://console.aws.amazon.com/amplify/home?region=us-east-2#/d3jy9zozo7c113
2. Click on **main** branch
3. Click **Deployments** tab
4. Click **Deploy without Git provider**
5. Upload `frontend-build.zip`
6. **Wait for deployment to complete** (check the status)

### Step 3: Verify Rewrite Rules

After deployment, check rewrite rules:

1. Go to **App settings** → **Rewrites and redirects**
2. You should see a rule: `/<*>` → `/index.html` (200 Rewrite)
3. If missing, add it:
   - **Source address:** `/<*>`
   - **Target address:** `/index.html`
   - **Type:** Rewrite (200)

### Step 4: Check Deployment Status

```bash
aws amplify list-jobs --app-id d3jy9zozo7c113 --branch-name main --max-results 1
```

Status should be `SUCCEED`.

### Step 5: Test

1. Wait 2-3 minutes after deployment completes
2. Try: `https://main.d3jy9zozo7c113.amplifyapp.com/`
3. Try: `https://main.d3jy9zozo7c113.amplifyapp.com/index.html`

## If Still Not Working

### Check Amplify Console Logs

1. Go to your app → Branch (main) → Latest deployment
2. Check the **Deploy** step logs
3. Look for errors about file extraction or serving

### Alternative: Use Git-Based Deployment

If manual ZIP uploads continue to fail, connect your GitHub repository:

1. Go to **App settings** → **General**
2. Click **Edit** under **Repository**
3. Connect your GitHub repository
4. Amplify will automatically build and deploy

This is more reliable than manual ZIP uploads.

## Why This Happens

Manual ZIP uploads to Amplify have limitations:
- Files might not be extracted correctly
- Rewrite rules might not be applied
- The deployment might not trigger file serving

Git-based deployments are more reliable because Amplify:
- Runs the build process
- Applies rewrite rules automatically
- Handles file serving correctly

## Quick Fix Script

Run this to rebuild and get a fresh ZIP:

```bash
./scripts/deploy-amplify.sh
```

Then upload the new ZIP file through the Amplify Console.

