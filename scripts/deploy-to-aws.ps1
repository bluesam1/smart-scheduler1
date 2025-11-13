# SmartScheduler AWS Deployment Script (PowerShell)
# This script automates the deployment of SmartScheduler to AWS

$ErrorActionPreference = "Stop"

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$InfrastructureDir = Join-Path $ProjectRoot "infrastructure"

Write-Host "Checking prerequisites..." -ForegroundColor Green

# Check prerequisites
$commands = @("aws", "node", "cdk", "dotnet")
foreach ($cmd in $commands) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Host "$cmd is required but not installed." -ForegroundColor Red
        exit 1
    }
}

# Check AWS credentials
try {
    $null = aws sts get-caller-identity 2>&1
} catch {
    Write-Host "AWS credentials not configured. Run 'aws configure' first." -ForegroundColor Red
    exit 1
}

# Get AWS account and region
$AWSAccount = (aws sts get-caller-identity --query Account --output text)
$AWSRegion = if ($env:AWS_REGION) { $env:AWS_REGION } elseif ($env:CDK_DEFAULT_REGION) { $env:CDK_DEFAULT_REGION } else { "us-east-1" }

Write-Host "AWS Account: $AWSAccount" -ForegroundColor Green
Write-Host "AWS Region: $AWSRegion" -ForegroundColor Green

# Prompt for confirmation
$confirmation = Read-Host "Continue with deployment? (y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "Deployment cancelled."
    exit 1
}

# Change to infrastructure directory
Set-Location $InfrastructureDir

# Install dependencies
Write-Host "Installing CDK dependencies..." -ForegroundColor Green
npm install

# Build CDK code
Write-Host "Building CDK code..." -ForegroundColor Green
npm run build

# Check if CDK is bootstrapped
Write-Host "Checking CDK bootstrap status..." -ForegroundColor Green
try {
    $null = aws cloudformation describe-stacks --stack-name CDKToolkit --region $AWSRegion 2>&1
    Write-Host "CDK already bootstrapped." -ForegroundColor Green
} catch {
    Write-Host "CDK not bootstrapped. Bootstrapping..." -ForegroundColor Yellow
    cdk bootstrap "aws://$AWSAccount/$AWSRegion"
}

# Deploy stacks in order
Write-Host "Deploying infrastructure stacks..." -ForegroundColor Green

# 1. Database Stack
Write-Host "Deploying Database Stack..." -ForegroundColor Green
npm run deploy:database

# 2. Secrets Stack
Write-Host "Deploying Secrets Stack..." -ForegroundColor Green
npm run deploy:secrets

# 3. Storage Stack
Write-Host "Deploying Storage Stack..." -ForegroundColor Green
npm run deploy:storage

# 4. Cognito Stack
Write-Host "Deploying Cognito Stack..." -ForegroundColor Green
npm run deploy:cognito

# 5. API Stack
Write-Host "Deploying API Stack..." -ForegroundColor Green
npm run deploy:api

# 6. Frontend Stack
Write-Host "Deploying Frontend Stack..." -ForegroundColor Green
npm run deploy:frontend

Write-Host "Infrastructure deployment complete!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update secrets in AWS Secrets Manager (API keys, database connection string)"
Write-Host "2. Enable PostGIS extension in the database"
Write-Host "3. Run database migrations"
Write-Host "4. Deploy application code to Elastic Beanstalk"
Write-Host "5. Configure frontend environment variables in Amplify"

