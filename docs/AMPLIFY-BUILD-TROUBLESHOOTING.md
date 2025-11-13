# Amplify Build Troubleshooting

If your Amplify app is still showing the "Welcome" page, follow these steps:

## 1. Check Amplify Build Logs

1. Go to **AWS Amplify Console** → Your App → Your Branch (main)
2. Click on the latest build
3. Check the build logs for errors

Common issues:
- Build failing during `npm ci` or `npm run build`
- Missing environment variables
- Wrong baseDirectory path

## 2. Verify Build Spec

The build spec should:
- Build in the `frontend` directory
- Output to `frontend/out` directory
- Include `index.html` in the artifacts

## 3. Manual Build Test

Test the build locally to ensure it works:

```bash
cd frontend
npm ci
npm run build
ls -la out/
```

You should see `out/index.html` and other static files.

## 4. Trigger a New Build

After fixing issues:

1. **Commit and push your changes** to trigger a new build
2. Or manually trigger in Amplify Console:
   - Go to your branch
   - Click "Redeploy this version" or push a new commit

## 5. Verify Build Artifacts

After a successful build, check:
- Build logs show "Build completed successfully"
- Artifacts section shows files in `frontend/out/`
- `index.html` is present in artifacts

## 6. Check Build Spec Format

The build spec uses the `frontend` format (not `version: 1.0` with `phases`). This is correct for Amplify Hosting.

If you need to debug, you can:
1. Add `echo` commands to see what's happening
2. Check if `out/` directory exists after build
3. Verify `index.html` is created

## Common Fixes

### Build Failing on Native Modules

If you see errors about `lightningcss` or `@tailwindcss/oxide`:

The build spec already includes:
```yaml
npm rebuild lightningcss @tailwindcss/oxide || true
npm install @tailwindcss/oxide lightningcss --force || true
```

### Missing index.html

If `index.html` is missing:
- Verify `output: 'export'` is in `next.config.mjs`
- Check that the build completes successfully
- Ensure no dynamic routes without `generateStaticParams()`

### Wrong baseDirectory

The baseDirectory should be `frontend/out` (relative to repository root).

If your repository structure is different, adjust accordingly.

## Next Steps

1. Check Amplify build logs
2. Verify the build completes successfully
3. Check that artifacts include `index.html`
4. If still not working, check Amplify Console → App settings → Build settings

