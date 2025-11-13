#!/bin/bash

# Script to build and deploy the .NET API to Elastic Beanstalk

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
API_DIR="${PROJECT_ROOT}/src/SmartScheduler.Api"

echo -e "${GREEN}Building and deploying .NET API...${NC}"

# Check if .NET SDK is installed
if ! command -v dotnet >/dev/null 2>&1; then
    echo -e "${RED}.NET SDK is required but not installed.${NC}" >&2
    exit 1
fi

# Build the application
echo -e "${YELLOW}Building .NET application...${NC}"
cd "${API_DIR}"
dotnet publish -c Release -o ./publish

# Create deployment package
echo -e "${YELLOW}Creating deployment package...${NC}"
cd publish
zip -r ../SmartScheduler.Api.zip . >/dev/null
cd ..

# Get deployment bucket name
echo -e "${YELLOW}Getting deployment bucket name...${NC}"
BUCKET_NAME=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Storage \
  --query "Stacks[0].Outputs[?OutputKey=='DeploymentBucketName'].OutputValue" \
  --output text 2>/dev/null || echo "")

if [ -z "${BUCKET_NAME}" ]; then
    echo -e "${RED}Storage stack not found. Deploy storage stack first.${NC}"
    exit 1
fi

echo -e "${GREEN}Deployment bucket: ${BUCKET_NAME}${NC}"

# Upload to S3
echo -e "${YELLOW}Uploading to S3...${NC}"
aws s3 cp SmartScheduler.Api.zip s3://${BUCKET_NAME}/SmartScheduler.Api.zip

# Get application name
echo -e "${YELLOW}Getting Elastic Beanstalk application name...${NC}"
APP_NAME=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Api \
  --query "Stacks[0].Outputs[?OutputKey=='ApplicationName'].OutputValue" \
  --output text 2>/dev/null || echo "")

if [ -z "${APP_NAME}" ]; then
    echo -e "${RED}API stack not found. Deploy API stack first.${NC}"
    exit 1
fi

# Create application version
VERSION_LABEL="v$(date +%Y%m%d%H%M%S)"
echo -e "${YELLOW}Creating application version: ${VERSION_LABEL}...${NC}"
aws elasticbeanstalk create-application-version \
  --application-name ${APP_NAME} \
  --version-label ${VERSION_LABEL} \
  --source-bundle S3Bucket=${BUCKET_NAME},S3Key=SmartScheduler.Api.zip >/dev/null

# Deploy to environment
echo -e "${YELLOW}Deploying to Elastic Beanstalk environment...${NC}"
aws elasticbeanstalk update-environment \
  --environment-name production \
  --version-label ${VERSION_LABEL}

echo -e "${GREEN}API deployment initiated!${NC}"
echo -e "${YELLOW}Monitor deployment progress in Elastic Beanstalk console.${NC}"

# Cleanup
rm -f SmartScheduler.Api.zip
rm -rf publish

echo -e "${GREEN}Deployment complete!${NC}"

