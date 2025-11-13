# Deploy to Amplify - Fully Automated

## One Command Deployment

Run this from the project root:

```bash
./scripts/deploy-amplify.sh
```

That's it! The script will:
1. ✅ Build your frontend (if needed)
2. ✅ Create deployment ZIP package
3. ✅ Upload to S3
4. ✅ Deploy to Amplify via AWS CLI
5. ✅ Wait for deployment to complete
6. ✅ Show you the URL

## What It Does

The script fully automates the deployment process:

1. **Builds** your frontend using `npm run build`
2. **Creates** `frontend-build.zip` with all static files
3. **Uploads** the ZIP to S3 (uses existing deployment bucket from CDK stack)
4. **Deploys** to Amplify using AWS CLI
5. **Waits** for deployment to complete (up to 2 minutes)
6. **Shows** the deployment URL and status

## Requirements

- AWS CLI configured with credentials
- Node.js installed
- Frontend stack deployed via CDK (for App ID)

## Troubleshooting

### Deployment Fails

If the deployment fails, check:
1. Amplify Console logs: https://console.aws.amazon.com/amplify/home
2. S3 bucket permissions (should be handled by CDK)
3. Amplify app exists and is accessible

### S3 Upload Fails

The script will fall back to manual upload instructions if S3 upload fails.

### Build Fails

Make sure:
- `frontend/package.json` exists
- Dependencies can be installed
- `npm run build` works locally

## Manual Fallback

If the automated deployment fails, you can still:
1. Run the script to create the ZIP
2. Upload `frontend-build.zip` manually via Amplify Console
3. Go to: Deployments → Deploy without Git provider
