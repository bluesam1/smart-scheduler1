#!/bin/bash

# Fully automated Amplify deployment script
# Usage: ./scripts/deploy-amplify.sh

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
FRONTEND_DIR="${PROJECT_ROOT}/frontend"
ZIP_FILE="${PROJECT_ROOT}/frontend-build.zip"

echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Amplify Deployment Script            ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"
echo ""

# Step 1: Build
echo -e "${YELLOW}[1/4]${NC} Building frontend..."
cd "${FRONTEND_DIR}"
if [ ! -f "out/index.html" ]; then
    echo -e "${YELLOW}      Installing dependencies...${NC}"
    npm install --legacy-peer-deps 2>/dev/null || npm install --force 2>/dev/null || true
    echo -e "${YELLOW}      Building...${NC}"
    npm run build
else
    echo -e "${YELLOW}      Rebuilding...${NC}"
    npm run build
fi
echo -e "${GREEN}      ✓ Build complete${NC}"

# Step 2: Create ZIP
echo -e "${YELLOW}[2/4]${NC} Creating deployment package..."
OUT_DIR="${FRONTEND_DIR}/out"
if [ ! -d "${OUT_DIR}" ]; then
    echo -e "${RED}      ✗ Build output directory not found${NC}"
    exit 1
fi

rm -f "${ZIP_FILE}" 2>/dev/null || true

OUT_DIR_ABS=$(cd "${OUT_DIR}" && pwd)
ZIP_FILE_ABS=$(cd "${PROJECT_ROOT}" && pwd)/frontend-build.zip

if ! command -v node >/dev/null 2>&1; then
    echo -e "${RED}      ✗ Node.js not found${NC}"
    exit 1
fi

cd "${FRONTEND_DIR}" || exit 1
if [ ! -d "node_modules/archiver" ]; then
    echo -e "${YELLOW}      Installing archiver...${NC}"
    npm install archiver --no-save --silent 2>/dev/null || true
fi

# Create ZIP using Node.js (run from frontend dir so archiver can be found)
# Use relative paths from frontend directory
node -e "
const fs = require('fs');
const path = require('path');
const archiver = require('archiver');

// Use relative paths from frontend directory
const outDir = path.resolve('./out');
const zipFile = path.resolve('../frontend-build.zip');

if (!fs.existsSync(outDir)) {
    console.error('Error: out directory not found:', outDir);
    process.exit(1);
}

(async () => {
    try {
        await new Promise((resolve, reject) => {
            const output = fs.createWriteStream(zipFile);
            const archive = archiver('zip', { zlib: { level: 9 } });

            output.on('close', () => {
                // Wait a bit for file system to sync
                setTimeout(() => {
                    if (fs.existsSync(zipFile)) {
                        const stats = fs.statSync(zipFile);
                        if (stats.size > 0) {
                            resolve();
                        } else {
                            reject(new Error('ZIP file is empty'));
                        }
                    } else {
                        reject(new Error('ZIP file not found after creation'));
                    }
                }, 100);
            });

            output.on('error', (err) => {
                reject(err);
            });

            archive.on('error', (err) => {
                reject(err);
            });

            archive.pipe(output);
            archive.directory(outDir, false);
            archive.finalize();
        });
        process.exit(0);
    } catch (err) {
        console.error('Error:', err.message);
        process.exit(1);
    }
})();
" 2>&1

ZIP_EXIT_CODE=$?

# Change back to project root
cd "${PROJECT_ROOT}" || exit 1

# Wait a moment for file system
sleep 1

if [ $ZIP_EXIT_CODE -ne 0 ] || [ ! -f "${ZIP_FILE}" ]; then
    echo -e "${RED}      ✗ Failed to create ZIP${NC}"
    if [ $ZIP_EXIT_CODE -ne 0 ]; then
        echo -e "${YELLOW}      Node.js script exited with code: ${ZIP_EXIT_CODE}${NC}"
    fi
    if [ ! -f "${ZIP_FILE}" ]; then
        echo -e "${YELLOW}      ZIP file not found at: ${ZIP_FILE}${NC}"
        echo -e "${YELLOW}      Checked path: $(cd "${PROJECT_ROOT}" && pwd)/frontend-build.zip${NC}"
    fi
    exit 1
fi

ZIP_SIZE=$(du -h "${ZIP_FILE}" | cut -f1)
echo -e "${GREEN}      ✓ Package created: frontend-build.zip (${ZIP_SIZE})${NC}"

# Step 3: Get App ID
echo -e "${YELLOW}[3/4]${NC} Getting Amplify App info..."
APP_ID=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Frontend \
  --query 'Stacks[0].Outputs[?OutputKey==`AppId`].OutputValue' \
  --output text 2>/dev/null || echo "")

if [ -z "${APP_ID}" ]; then
    echo -e "${RED}      ✗ Could not find Amplify App ID${NC}"
    echo -e "${YELLOW}      Make sure the Frontend stack is deployed${NC}"
    exit 1
fi

REGION=$(aws configure get region 2>/dev/null || echo "us-east-2")
BRANCH_NAME="main"

echo -e "${GREEN}      ✓ App ID: ${APP_ID}${NC}"

# Step 4: Deploy via AWS CLI
echo -e "${YELLOW}[4/4]${NC} Deploying to Amplify..."

# Try to get deployment bucket from CDK stack, otherwise create temp bucket name
BUCKET_NAME=$(aws cloudformation describe-stacks \
  --stack-name SmartScheduler-Storage \
  --query 'Stacks[0].Outputs[?OutputKey==`DeploymentBucketName`].OutputValue' \
  --output text 2>/dev/null || echo "")

if [ -z "${BUCKET_NAME}" ]; then
    # Fallback: use account-based bucket name
    ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text 2>/dev/null || echo 'temp')
    BUCKET_NAME="smartscheduler-deployments-${ACCOUNT_ID}-${REGION}"
    echo -e "${YELLOW}      Creating S3 bucket if needed...${NC}"
    aws s3 mb "s3://${BUCKET_NAME}" --region "${REGION}" 2>/dev/null || true
fi

S3_KEY="frontend-build-$(date +%s).zip"

echo -e "${YELLOW}      Uploading to S3 (${BUCKET_NAME})...${NC}"

# Upload ZIP and capture output
UPLOAD_OUTPUT=$(aws s3 cp "${ZIP_FILE}" "s3://${BUCKET_NAME}/${S3_KEY}" --region "${REGION}" 2>&1)
UPLOAD_EXIT_CODE=$?

if [ $UPLOAD_EXIT_CODE -ne 0 ]; then
    echo -e "${RED}      ✗ Failed to upload to S3${NC}"
    echo -e "${YELLOW}      Error: ${UPLOAD_OUTPUT}${NC}"
    echo -e "${YELLOW}      Falling back to manual upload instructions...${NC}"
    echo ""
    echo -e "${BLUE}Manual upload:${NC}"
    echo "  1. Go to: https://console.aws.amazon.com/amplify/home?region=${REGION}#/${APP_ID}"
    echo "  2. Click branch: ${BRANCH_NAME}"
    echo "  3. Click: 'Deployments' → 'Deploy without Git provider'"
    echo "  4. Upload: ${ZIP_FILE}"
    exit 1
fi

# Verify the file exists in S3
if ! aws s3 ls "s3://${BUCKET_NAME}/${S3_KEY}" --region "${REGION}" >/dev/null 2>&1; then
    echo -e "${RED}      ✗ File not found in S3 after upload${NC}"
    echo -e "${YELLOW}      Falling back to manual upload...${NC}"
    echo ""
    echo -e "${BLUE}Manual upload:${NC}"
    echo "  1. Go to: https://console.aws.amazon.com/amplify/home?region=${REGION}#/${APP_ID}"
    echo "  2. Click branch: ${BRANCH_NAME}"
    echo "  3. Click: 'Deployments' → 'Deploy without Git provider'"
    echo "  4. Upload: ${ZIP_FILE}"
    exit 1
fi

echo -e "${GREEN}      ✓ Uploaded to S3${NC}"

# Start deployment
echo -e "${YELLOW}      Starting deployment...${NC}"

DEPLOY_OUTPUT=$(aws amplify start-deployment \
  --app-id "${APP_ID}" \
  --branch-name "${BRANCH_NAME}" \
  --source-url "s3://${BUCKET_NAME}/${S3_KEY}" \
  --region "${REGION}" \
  --query 'jobSummary.jobId' \
  --output text 2>&1)
DEPLOY_EXIT_CODE=$?

if [ $DEPLOY_EXIT_CODE -ne 0 ] || [ -z "${DEPLOY_OUTPUT}" ] || [ "${DEPLOY_OUTPUT}" = "None" ]; then
    echo -e "${RED}      ✗ Failed to start deployment via CLI${NC}"
    if [ -n "${DEPLOY_OUTPUT}" ] && [ "${DEPLOY_OUTPUT}" != "None" ]; then
        echo -e "${YELLOW}      Error: ${DEPLOY_OUTPUT}${NC}"
    fi
    echo ""
    echo -e "${YELLOW}      Note: AWS Amplify CLI may not support manual ZIP deployments.${NC}"
    echo -e "${YELLOW}      Use the console upload method below.${NC}"
    echo ""
    echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║  Manual Upload Required               ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "${BLUE}Steps:${NC}"
    echo "  1. Open: https://console.aws.amazon.com/amplify/home?region=${REGION}#/${APP_ID}"
    echo "  2. Click branch: ${BRANCH_NAME}"
    echo "  3. Click: 'Deployments' → 'Deploy without Git provider'"
    echo "  4. Upload: ${ZIP_FILE}"
    echo ""
    echo -e "${BLUE}ZIP file:${NC} ${ZIP_FILE}"
    echo -e "${BLUE}File size:${NC} ${ZIP_SIZE}"
    echo ""
    echo -e "${GREEN}The ZIP file is ready for upload!${NC}"
    exit 0
fi

JOB_ID="${DEPLOY_OUTPUT}"

echo -e "${GREEN}      ✓ Deployment started (Job ID: ${JOB_ID})${NC}"

# Wait for deployment
echo ""
echo -e "${YELLOW}Waiting for deployment to complete...${NC}"
echo -e "${YELLOW}(This may take 1-2 minutes)${NC}"

MAX_WAIT=120
ELAPSED=0
while [ $ELAPSED -lt $MAX_WAIT ]; do
    STATUS=$(aws amplify get-job \
      --app-id "${APP_ID}" \
      --branch-name "${BRANCH_NAME}" \
      --job-id "${JOB_ID}" \
      --query 'job.summary.status' \
      --output text 2>/dev/null || echo "PENDING")
    
    if [ "${STATUS}" = "SUCCEED" ]; then
        echo -e "${GREEN}      ✓ Deployment succeeded!${NC}"
        break
    elif [ "${STATUS}" = "FAILED" ] || [ "${STATUS}" = "CANCELLED" ]; then
        echo -e "${RED}      ✗ Deployment ${STATUS}${NC}"
        echo -e "${YELLOW}      Check logs in Amplify Console${NC}"
        exit 1
    fi
    
    sleep 5
    ELAPSED=$((ELAPSED + 5))
    echo -n "."
done

echo ""

if [ "${STATUS}" != "SUCCEED" ]; then
    echo -e "${YELLOW}      Deployment still in progress...${NC}"
    echo -e "${YELLOW}      Check status: aws amplify get-job --app-id ${APP_ID} --branch-name ${BRANCH_NAME} --job-id ${JOB_ID}${NC}"
fi

# Cleanup S3 file
aws s3 rm "s3://${BUCKET_NAME}/${S3_KEY}" --region "${REGION}" >/dev/null 2>&1 || true

# Output results
echo ""
echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  Deployment Complete!                 ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}App URL:${NC} https://${BRANCH_NAME}.${APP_ID}.amplifyapp.com"
echo -e "${BLUE}Console:${NC} https://console.aws.amazon.com/amplify/home?region=${REGION}#/${APP_ID}"
echo ""
echo -e "${GREEN}Done!${NC}"
