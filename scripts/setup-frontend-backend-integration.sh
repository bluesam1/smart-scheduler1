#!/bin/bash

# Script to help set up frontend-backend integration

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Setting up Frontend-Backend Integration${NC}"
echo ""

# Get API URL
echo -e "${YELLOW}Getting Elastic Beanstalk API URL...${NC}"
API_URL=$(aws elasticbeanstalk describe-environments \
  --environment-names production \
  --query 'Environments[0].EndpointURL' \
  --output text 2>/dev/null || echo "")

if [ -z "${API_URL}" ]; then
    echo -e "${RED}Could not get API URL. Is the API stack deployed?${NC}"
    exit 1
fi

echo -e "${GREEN}API URL: https://${API_URL}${NC}"

# Get Amplify App URL
echo -e "${YELLOW}Getting Amplify App URL...${NC}"
AMPLIFY_APP_ID=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Frontend \
  --query 'Stacks[0].Outputs[?OutputKey==`AppId`].OutputValue' \
  --output text 2>/dev/null || echo "")

if [ -z "${AMPLIFY_APP_ID}" ]; then
    echo -e "${YELLOW}Could not get Amplify App ID from CloudFormation.${NC}"
    echo -e "${YELLOW}Using default: main.d3jy9zozo7c113.amplifyapp.com${NC}"
    AMPLIFY_DOMAIN="main.d3jy9zozo7c113.amplifyapp.com"
else
    AMPLIFY_DOMAIN=$(aws amplify get-app \
      --app-id ${AMPLIFY_APP_ID} \
      --query 'app.defaultDomain' \
      --output text 2>/dev/null || echo "main.d3jy9zozo7c113.amplifyapp.com")
fi

echo -e "${GREEN}Amplify URL: https://${AMPLIFY_DOMAIN}${NC}"
echo ""

# Display configuration steps
echo -e "${YELLOW}=== Configuration Steps ===${NC}"
echo ""
echo -e "${GREEN}1. Update Amplify Environment Variables:${NC}"
echo "   Go to: AWS Amplify Console → Your App → Environment variables"
echo ""
echo "   Add/Update these variables:"
echo "   ${YELLOW}NEXT_PUBLIC_API_URL${NC}=https://${API_URL}"
echo "   ${YELLOW}NEXT_PUBLIC_SIGNALR_URL${NC}=https://${API_URL}/hubs"
echo "   ${YELLOW}NEXT_PUBLIC_COGNITO_USER_POOL_ID${NC}=us-east-2_oGumIWt36"
echo "   ${YELLOW}NEXT_PUBLIC_COGNITO_CLIENT_ID${NC}=4rps8b0oldpuan0qs2dnk37odd"
echo "   ${YELLOW}NEXT_PUBLIC_COGNITO_REGION${NC}=us-east-2"
echo ""
echo -e "${GREEN}2. Update Cognito App Client Callback URLs:${NC}"
echo "   Go to: AWS Cognito Console → User Pools → us-east-2_oGumIWt36 → App integration → App clients"
echo "   Edit app client: 4rps8b0oldpuan0qs2dnk37odd"
echo ""
echo "   Add these Callback URLs:"
echo "   - https://${AMPLIFY_DOMAIN}/auth/callback"
echo "   - https://*.amplifyapp.com/auth/callback"
echo ""
echo "   Add these Sign-out URLs:"
echo "   - https://${AMPLIFY_DOMAIN}/auth/signout"
echo "   - https://*.amplifyapp.com/auth/signout"
echo ""
echo -e "${GREEN}3. Update Backend CORS Configuration:${NC}"
echo "   The backend CORS has been updated to allow *.amplifyapp.com"
echo "   If you need to add a specific domain, update:"
echo "   src/SmartScheduler.Api/appsettings.json"
echo ""
echo -e "${GREEN}4. Redeploy Frontend:${NC}"
echo "   After updating environment variables, redeploy the frontend:"
echo "   - Go to Amplify Console → Your App → Your Branch"
echo "   - Click 'Redeploy this version'"
echo ""
echo -e "${YELLOW}=== Testing ===${NC}"
echo ""
echo "Test API health:"
echo "  curl https://${API_URL}/health"
echo ""
echo "Test frontend:"
echo "  Open https://${AMPLIFY_DOMAIN} in your browser"
echo ""

