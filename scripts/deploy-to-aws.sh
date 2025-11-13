#!/bin/bash

# SmartScheduler AWS Deployment Script
# This script automates the deployment of SmartScheduler to AWS

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
INFRASTRUCTURE_DIR="${PROJECT_ROOT}/infrastructure"

# Check prerequisites
echo -e "${GREEN}Checking prerequisites...${NC}"

command -v aws >/dev/null 2>&1 || { echo -e "${RED}AWS CLI is required but not installed.${NC}" >&2; exit 1; }
command -v node >/dev/null 2>&1 || { echo -e "${RED}Node.js is required but not installed.${NC}" >&2; exit 1; }
command -v cdk >/dev/null 2>&1 || { echo -e "${RED}AWS CDK CLI is required but not installed.${NC}" >&2; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo -e "${RED}.NET SDK is required but not installed.${NC}" >&2; exit 1; }

# Check AWS credentials
if ! aws sts get-caller-identity >/dev/null 2>&1; then
    echo -e "${RED}AWS credentials not configured. Run 'aws configure' first.${NC}" >&2
    exit 1
fi

# Get AWS account and region
AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
AWS_REGION=${AWS_REGION:-${CDK_DEFAULT_REGION:-us-east-1}}

echo -e "${GREEN}AWS Account: ${AWS_ACCOUNT}${NC}"
echo -e "${GREEN}AWS Region: ${AWS_REGION}${NC}"

# Prompt for confirmation
read -p "Continue with deployment? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Deployment cancelled."
    exit 1
fi

# Change to infrastructure directory
cd "${INFRASTRUCTURE_DIR}"

# Install dependencies
echo -e "${GREEN}Installing CDK dependencies...${NC}"
npm install

# Build CDK code
echo -e "${GREEN}Building CDK code...${NC}"
npm run build

# Check if CDK is bootstrapped
echo -e "${GREEN}Checking CDK bootstrap status...${NC}"
if ! aws cloudformation describe-stacks --stack-name CDKToolkit --region ${AWS_REGION} >/dev/null 2>&1; then
    echo -e "${YELLOW}CDK not bootstrapped. Bootstrapping...${NC}"
    cdk bootstrap aws://${AWS_ACCOUNT}/${AWS_REGION}
else
    echo -e "${GREEN}CDK already bootstrapped.${NC}"
fi

# Deploy stacks in order
echo -e "${GREEN}Deploying infrastructure stacks...${NC}"

# 1. Database Stack
echo -e "${GREEN}Deploying Database Stack...${NC}"
npm run deploy:database

# 2. Secrets Stack
echo -e "${GREEN}Deploying Secrets Stack...${NC}"
npm run deploy:secrets

# 3. Storage Stack
echo -e "${GREEN}Deploying Storage Stack...${NC}"
npm run deploy:storage

# 4. Cognito Stack
echo -e "${GREEN}Deploying Cognito Stack...${NC}"
npm run deploy:cognito

# 5. API Stack
echo -e "${GREEN}Deploying API Stack...${NC}"
npm run deploy:api

# 6. Frontend Stack
echo -e "${GREEN}Deploying Frontend Stack...${NC}"
npm run deploy:frontend

echo -e "${GREEN}Infrastructure deployment complete!${NC}"
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Update secrets in AWS Secrets Manager (API keys, database connection string)"
echo "2. Enable PostGIS extension in the database"
echo "3. Run database migrations"
echo "4. Deploy application code to Elastic Beanstalk"
echo "5. Configure frontend environment variables in Amplify"

