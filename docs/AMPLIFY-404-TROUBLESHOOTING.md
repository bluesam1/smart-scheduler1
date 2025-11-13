# Amplify 404 Error Troubleshooting

If you're getting a 404 error after deploying to Amplify, follow these steps:

## Quick Checks

1. **Verify the deployment succeeded:**
   ```bash
   aws amplify list-jobs --app-id d3jy9zozo7c113 --branch-name main --max-results 1
   ```
   Status should be `SUCCEED`.

2. **Check rewrite rules are configured:**
   ```bash
   aws amplify get-app --app-id d3jy9zozo7c113 --query "app.customRules"
   ```
   Should show: `[{"source": "/<*>", "target": "/index.html", "status": "200"}]`

3. **Try accessing index.html directly:**
   - Visit: `https://main.d3jy9zozo7c113.amplifyapp.com/index.html`
   - If this works, the rewrite rules aren't being applied

## Common Issues

### Issue 1: Rewrite Rules Not Applied to Manual Deployments

**Problem:** When you manually upload a ZIP file, Amplify might not apply the rewrite rules configured in CDK.

**Solution:** Configure rewrite rules manually in Amplify Console:

1. Go to **AWS Amplify Console** → Your App → **App settings** → **Rewrites and redirects**
2. Add a rewrite rule:
   - **Source address:** `/<*>`
   - **Target address:** `/index.html`
   - **Type:** Rewrite (200)
3. Save and redeploy

### Issue 2: ZIP File Structure

**Problem:** The ZIP file might have files in a subdirectory instead of at the root.

**Solution:** Ensure the ZIP contains files at the root level:
- ✅ Correct: `index.html`, `_next/`, `jobs/`, etc. (all at root)
- ❌ Wrong: `out/index.html`, `out/_next/`, etc. (files in `out/` subdirectory)

The `deploy-amplify.sh` script should create the ZIP correctly, but verify:
```bash
unzip -l frontend-build.zip | head -20
```
You should see `index.html` at the root, not `out/index.html`.

### Issue 3: Missing index.html

**Problem:** The `index.html` file might not be in the deployment.

**Solution:** Verify the build output:
```bash
ls -la frontend/out/index.html
```
If missing, rebuild:
```bash
cd frontend
npm run build
```

### Issue 4: Cache Issues

**Problem:** Browser or CDN cache might be serving old content.

**Solution:**
1. Hard refresh: `Ctrl+Shift+R` (Windows) or `Cmd+Shift+R` (Mac)
2. Clear browser cache
3. Try incognito/private browsing mode
4. Wait a few minutes for CDN cache to clear

## Step-by-Step Fix

1. **Rebuild and redeploy:**
   ```bash
   ./scripts/deploy-amplify.sh
   ```

2. **Upload the new ZIP:**
   - Go to Amplify Console → Your App → Branch (main)
   - Click "Deployments" → "Deploy without Git provider"
   - Upload `frontend-build.zip`

3. **Verify rewrite rules:**
   - Go to App settings → Rewrites and redirects
   - Ensure there's a rule: `/<*>` → `/index.html` (Rewrite, 200)

4. **Test:**
   - Wait 1-2 minutes for deployment to complete
   - Try: `https://main.d3jy9zozo7c113.amplifyapp.com/`
   - Try: `https://main.d3jy9zozo7c113.amplifyapp.com/index.html`

## Still Not Working?

1. **Check Amplify Console logs:**
   - Go to your app → Branch → Latest deployment
   - Check build logs for errors

2. **Verify files are deployed:**
   - In Amplify Console, check the "Artifacts" section
   - Should show `index.html` and other files

3. **Check Amplify app configuration:**
   ```bash
   aws amplify get-app --app-id d3jy9zozo7c113
   ```

4. **Try a fresh deployment:**
   - Delete the current deployment
   - Create a new deployment with the ZIP file

## Alternative: Use Git-Based Deployment

If manual ZIP uploads continue to have issues, consider connecting your GitHub repository:

1. Go to Amplify Console → App settings → General
2. Click "Edit" under "Repository"
3. Connect your GitHub repository
4. Amplify will automatically build and deploy on each push

This ensures rewrite rules and build settings are applied correctly.

