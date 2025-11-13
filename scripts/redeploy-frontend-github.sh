#!/bin/bash

# Script to destroy and redeploy frontend stack with GitHub connection
# Usage: ./scripts/redeploy-frontend-github.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Redeploy Frontend with GitHub        ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Get required info
if [ -z "${GITHUB_REPOSITORY}" ]; then
    GIT_REMOTE=$(git remote get-url origin 2>/dev/null || echo "")
    if [[ "${GIT_REMOTE}" =~ github.com[:/]([^/]+)/([^/]+)\.git ]]; then
        DETECTED_REPO="${BASH_REMATCH[1]}/${BASH_REMATCH[2]}"
        echo -e "${GREEN}      ✓ Detected repository: ${DETECTED_REPO}${NC}"
        read -p "Use this repository? (y/n) [y]: " USE_DETECTED
        if [[ "${USE_DETECTED:-y}" =~ ^[Yy]$ ]]; then
            GITHUB_REPOSITORY="${DETECTED_REPO}"
        else
            read -p "Enter GitHub repository (format: owner/repo): " GITHUB_REPOSITORY
        fi
    else
        read -p "Enter GitHub repository (format: owner/repo): " GITHUB_REPOSITORY
    fi
    export GITHUB_REPOSITORY
fi

if [ -z "${GITHUB_BRANCH}" ]; then
    read -p "Branch name [main]: " GITHUB_BRANCH
    export GITHUB_BRANCH="${GITHUB_BRANCH:-main}"
fi

if [ -z "${GITHUB_TOKEN}" ]; then
    echo ""
    echo -e "${YELLOW}Enter your GitHub Personal Access Token:${NC}"
    read -p "Token: " GITHUB_TOKEN
    export GITHUB_TOKEN
fi

# Get Cognito info if using existing
if [ -z "${COGNITO_USER_POOL_ID}" ] || [ -z "${COGNITO_APP_CLIENT_ID}" ]; then
    echo ""
    echo -e "${YELLOW}Do you have an existing Cognito User Pool? (y/n)${NC}"
    read -p "Use existing Cognito: " USE_EXISTING_COGNITO
    if [[ "${USE_EXISTING_COGNITO}" =~ ^[Yy]$ ]]; then
        read -p "User Pool ID: " COGNITO_USER_POOL_ID
        read -p "App Client ID: " COGNITO_APP_CLIENT_ID
        read -p "Cognito Region [us-east-1]: " COGNITO_REGION
        COGNITO_REGION="${COGNITO_REGION:-us-east-1}"
        export COGNITO_USER_POOL_ID
        export COGNITO_APP_CLIENT_ID
        export COGNITO_REGION
    fi
fi

# Get API URL if needed
if [ -z "${API_URL}" ]; then
    read -p "API URL (or press Enter to set later): " API_URL
    if [ -n "${API_URL}" ]; then
        export API_URL
    fi
fi

echo ""
echo -e "${BLUE}Configuration:${NC}"
echo "  Repository: ${GITHUB_REPOSITORY}"
echo "  Branch: ${GITHUB_BRANCH}"
echo "  Token: ${GITHUB_TOKEN:0:10}... (hidden)"
if [ -n "${COGNITO_USER_POOL_ID}" ]; then
    echo "  Cognito: Using existing (${COGNITO_USER_POOL_ID})"
fi
if [ -n "${API_URL}" ]; then
    echo "  API URL: ${API_URL}"
fi
echo ""

read -p "This will DESTROY the existing frontend stack. Continue? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Cancelled.${NC}"
    exit 1
fi

cd infrastructure

echo ""
echo -e "${YELLOW}[1/3] Destroying existing frontend stack...${NC}"
npm run destroy:frontend -- --force || {
    echo -e "${YELLOW}      ⚠ Stack may not exist or already destroyed${NC}"
}

# Wait a bit for resources to be cleaned up
echo -e "${YELLOW}      Waiting for resources to be cleaned up...${NC}"
sleep 10

echo ""
echo -e "${YELLOW}[2/3] Deploying Cognito stack...${NC}"
if [ -n "${COGNITO_USER_POOL_ID}" ]; then
    echo -e "${YELLOW}      (Importing existing User Pool)${NC}"
fi
npm run deploy:cognito

echo ""
echo -e "${YELLOW}[3/3] Creating Frontend Stack with GitHub connection...${NC}"
npm run deploy:frontend

echo ""
echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  Redeployment Complete!               ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
echo ""

# Get the new App ID
APP_ID=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Frontend \
  --query 'Stacks[0].Outputs[?OutputKey==`AppId`].OutputValue' \
  --output text 2>/dev/null || echo "")

if [ -n "${APP_ID}" ]; then
    REGION=$(aws configure get region 2>/dev/null || echo "us-east-2")
    AMPLIFY_URL="https://${GITHUB_BRANCH}.${APP_ID}.amplifyapp.com"
    echo -e "${BLUE}Next steps:${NC}"
    echo "  1. Push your code to GitHub:"
    echo "     git push origin ${GITHUB_BRANCH}"
    echo ""
    echo "  2. Amplify will automatically build and deploy"
    echo ""
    echo "  3. Check deployment status:"
    echo "     aws amplify list-jobs --app-id ${APP_ID} --branch-name ${GITHUB_BRANCH}"
    echo ""
    echo -e "${BLUE}Your app will be available at:${NC} ${AMPLIFY_URL}"
    echo ""
fi

echo -e "${GREEN}Done!${NC}"

