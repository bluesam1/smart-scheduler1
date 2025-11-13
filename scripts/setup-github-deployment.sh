#!/bin/bash

# Script to set up Amplify frontend deployment from GitHub
# Usage: ./scripts/setup-github-deployment.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  GitHub Deployment Setup              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Step 1: Get GitHub repository (try to auto-detect from git remote)
if [ -z "${GITHUB_REPOSITORY}" ]; then
    GIT_REMOTE=$(git remote get-url origin 2>/dev/null || echo "")
    if [[ "${GIT_REMOTE}" =~ github.com[:/]([^/]+)/([^/]+)\.git ]]; then
        DETECTED_REPO="${BASH_REMATCH[1]}/${BASH_REMATCH[2]}"
        echo -e "${GREEN}      ✓ Detected repository: ${DETECTED_REPO}${NC}"
        read -p "Use this repository? (y/n) [y]: " USE_DETECTED
        if [[ "${USE_DETECTED:-y}" =~ ^[Yy]$ ]]; then
            GITHUB_REPOSITORY="${DETECTED_REPO}"
        else
            echo -e "${YELLOW}Enter your GitHub repository (format: owner/repo):${NC}"
            read -p "Repository: " GITHUB_REPOSITORY
        fi
    else
        echo -e "${YELLOW}Enter your GitHub repository (format: owner/repo):${NC}"
        echo -e "${YELLOW}Example: samsmith/smart-scheduler1${NC}"
        read -p "Repository: " GITHUB_REPOSITORY
    fi
    export GITHUB_REPOSITORY
fi

# Step 2: Get GitHub branch
if [ -z "${GITHUB_BRANCH}" ]; then
    read -p "Branch name [main]: " GITHUB_BRANCH
    export GITHUB_BRANCH="${GITHUB_BRANCH:-main}"
fi

# Step 3: Check for existing Cognito
if [ -z "${COGNITO_USER_POOL_ID}" ] || [ -z "${COGNITO_APP_CLIENT_ID}" ]; then
    echo ""
    echo -e "${YELLOW}Do you already have a Cognito User Pool? (y/n)${NC}"
    read -p "Use existing Cognito: " USE_EXISTING_COGNITO
    if [[ "${USE_EXISTING_COGNITO}" =~ ^[Yy]$ ]]; then
        echo ""
        echo -e "${YELLOW}Enter your existing Cognito details:${NC}"
        read -p "User Pool ID: " COGNITO_USER_POOL_ID
        read -p "App Client ID: " COGNITO_APP_CLIENT_ID
        read -p "Cognito Region [us-east-1]: " COGNITO_REGION
        COGNITO_REGION="${COGNITO_REGION:-us-east-1}"
        export COGNITO_USER_POOL_ID
        export COGNITO_APP_CLIENT_ID
        export COGNITO_REGION
        echo -e "${GREEN}      ✓ Using existing Cognito User Pool${NC}"
    else
        echo -e "${YELLOW}      Will create new Cognito User Pool${NC}"
    fi
fi

# Step 4: Get GitHub token
if [ -z "${GITHUB_TOKEN}" ]; then
    echo ""
    echo -e "${YELLOW}You need a GitHub Personal Access Token with 'repo' scope.${NC}"
    echo -e "${BLUE}To create one:${NC}"
    echo "  1. Go to: https://github.com/settings/tokens"
    echo "  2. Click 'Generate new token (classic)'"
    echo "  3. Name it: AWS Amplify"
    echo "  4. Select scope: repo (Full control of private repositories)"
    echo "  5. Generate and copy the token"
    echo ""
    read -p "Enter your GitHub token: " GITHUB_TOKEN
    export GITHUB_TOKEN
fi

# Step 5: Get API URL (optional, can be updated later)
if [ -z "${API_URL}" ]; then
    echo ""
    echo -e "${YELLOW}Enter your API URL (or press Enter to set later):${NC}"
    echo -e "${YELLOW}Example: https://your-api.elasticbeanstalk.com${NC}"
    read -p "API URL: " API_URL
    if [ -n "${API_URL}" ]; then
        export API_URL
    fi
fi

# Step 6: Get Amplify App URL for callback URLs
echo ""
echo -e "${YELLOW}Getting Amplify App URL for Cognito callback configuration...${NC}"
APP_ID=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Frontend \
  --query 'Stacks[0].Outputs[?OutputKey==`AppId`].OutputValue' \
  --output text 2>/dev/null || echo "")

REGION=$(aws configure get region 2>/dev/null || echo "us-east-2")

if [ -n "${APP_ID}" ]; then
    AMPLIFY_URL="https://${GITHUB_BRANCH}.${APP_ID}.amplifyapp.com"
    echo -e "${GREEN}      ✓ Amplify URL: ${AMPLIFY_URL}${NC}"
    
    # Update callback URLs
    export FRONTEND_CALLBACK_URLS="${AMPLIFY_URL}/auth/callback,http://localhost:3000/auth/callback"
    export FRONTEND_SIGNOUT_URLS="${AMPLIFY_URL}/auth/signout,http://localhost:3000/auth/signout"
else
    echo -e "${YELLOW}      ⚠ Could not get App ID, using defaults${NC}"
    export FRONTEND_CALLBACK_URLS="http://localhost:3000/auth/callback"
    export FRONTEND_SIGNOUT_URLS="http://localhost:3000/auth/signout"
fi

echo ""
echo -e "${BLUE}Configuration:${NC}"
echo "  Repository: ${GITHUB_REPOSITORY}"
echo "  Branch: ${GITHUB_BRANCH}"
echo "  Token: ${GITHUB_TOKEN:0:10}... (hidden)"
if [ -n "${API_URL}" ]; then
    echo "  API URL: ${API_URL}"
fi
echo "  Callback URLs: ${FRONTEND_CALLBACK_URLS}"
echo ""

read -p "Continue with deployment? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Cancelled.${NC}"
    exit 1
fi

cd infrastructure

# Only deploy Cognito if we're creating a new one or updating callback URLs
if [ -z "${COGNITO_USER_POOL_ID}" ]; then
    echo ""
    echo -e "${YELLOW}[1/2] Deploying Cognito stack...${NC}"
    npm run deploy:cognito
    echo ""
    echo -e "${YELLOW}[2/2] Deploying Frontend Stack with GitHub connection...${NC}"
    npm run deploy:frontend
else
    # If using existing Cognito, we still need to deploy the stack to import it
    # But we can skip if the stack already exists and callback URLs haven't changed
    echo ""
    echo -e "${YELLOW}[1/2] Deploying Cognito stack (importing existing User Pool)...${NC}"
    npm run deploy:cognito
    echo ""
    echo -e "${YELLOW}[2/2] Deploying Frontend Stack with GitHub connection...${NC}"
    npm run deploy:frontend
fi

echo ""
echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  Setup Complete!                     ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "  1. Push your code to GitHub:"
echo "     git push origin ${GITHUB_BRANCH}"
echo ""
echo "  2. Amplify will automatically build and deploy"
echo ""
echo "  3. Check deployment status:"
echo "     aws amplify list-jobs --app-id ${APP_ID} --branch-name ${GITHUB_BRANCH}"
echo ""
if [ -n "${AMPLIFY_URL}" ]; then
    echo -e "${BLUE}Your app will be available at:${NC} ${AMPLIFY_URL}"
    echo ""
fi
echo -e "${GREEN}Done!${NC}"

