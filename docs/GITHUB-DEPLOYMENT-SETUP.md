# Setting Up GitHub Deployment for Amplify

This guide will help you set up your Amplify frontend to deploy automatically from GitHub.

## Quick Setup

Run the setup script:

```bash
./scripts/setup-github-deployment.sh
```

The script will:
1. Ask for your GitHub repository (format: `owner/repo`)
2. Ask for your GitHub branch (default: `main`)
3. Ask for your GitHub Personal Access Token
4. Optionally ask for your API URL
5. Update Cognito callback URLs
6. Deploy the frontend stack with GitHub connection

## Manual Setup

If you prefer to set it up manually:

### Step 1: Get GitHub Personal Access Token

1. Go to: https://github.com/settings/tokens
2. Click "Generate new token (classic)"
3. Name it: `AWS Amplify`
4. Select scope: **`repo`** (Full control of private repositories)
5. Click "Generate token"
6. **Copy the token** (you won't see it again!)

### Step 2: Set Environment Variables

```bash
export GITHUB_REPOSITORY="YOUR_USERNAME/smart-scheduler1"
export GITHUB_BRANCH="main"
export GITHUB_TOKEN="ghp_YOUR_TOKEN_HERE"
export API_URL="https://your-api.elasticbeanstalk.com"  # Optional
```

### Step 3: Get Amplify URL for Callback URLs

After the frontend is deployed, you'll need to update Cognito callback URLs:

```bash
# Get your Amplify App ID
APP_ID=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Frontend \
  --query 'Stacks[0].Outputs[?OutputKey==`AppId`].OutputValue' \
  --output text)

# Your Amplify URL will be:
# https://main.${APP_ID}.amplifyapp.com

# Set callback URLs
export FRONTEND_CALLBACK_URLS="https://main.${APP_ID}.amplifyapp.com/auth/callback,http://localhost:3000/auth/callback"
export FRONTEND_SIGNOUT_URLS="https://main.${APP_ID}.amplifyapp.com/auth/signout,http://localhost:3000/auth/signout"
```

### Step 4: Deploy Frontend Stack

```bash
cd infrastructure
npm run deploy -- SmartScheduler-Frontend
```

## After Setup

Once deployed:

1. **Push your code to GitHub:**
   ```bash
   git push origin main
   ```

2. **Amplify will automatically:**
   - Detect the push
   - Build your frontend
   - Deploy to Amplify hosting

3. **Check deployment status:**
   ```bash
   aws amplify list-jobs --app-id YOUR_APP_ID --branch-name main
   ```

## Updating Configuration

If you need to update the GitHub repository or token:

1. Set the environment variables again
2. Redeploy the frontend stack:
   ```bash
   cd infrastructure
   npm run deploy -- SmartScheduler-Frontend
   ```

## Troubleshooting

### Deployment Fails

Check the Amplify build logs:
1. Go to AWS Amplify Console
2. Select your app
3. Click on the failed deployment
4. Check the build logs for errors

### GitHub Connection Fails

1. Verify your GitHub token has `repo` scope
2. Verify the repository name is correct (format: `owner/repo`)
3. Verify the branch exists in your repository

### Build Fails

Common issues:
- Missing dependencies in `package.json`
- Build errors in your code
- Missing environment variables

Check the build logs in Amplify Console for specific errors.

## Benefits of GitHub Deployment

- ✅ Automatic deployments on every push
- ✅ Build history and logs
- ✅ Easy rollback to previous versions
- ✅ No manual ZIP uploads needed
- ✅ Better CI/CD integration

