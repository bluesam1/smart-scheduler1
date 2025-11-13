# Script to configure AWS Secrets Manager secrets for SmartScheduler (PowerShell)

$ErrorActionPreference = "Stop"

Write-Host "Configuring AWS Secrets Manager secrets..." -ForegroundColor Green

# Get database endpoint
Write-Host "Getting database endpoint..." -ForegroundColor Yellow
$DBEndpoint = aws cloudformation describe-stacks `
    --stack-name SmartScheduler-Database `
    --query "Stacks[0].Outputs[?OutputKey=='DatabaseEndpoint'].OutputValue" `
    --output text 2>$null

if (-not $DBEndpoint) {
    Write-Host "Database stack not found. Deploy database stack first." -ForegroundColor Red
    exit 1
}

Write-Host "Database endpoint: $DBEndpoint" -ForegroundColor Green

# Get database password
Write-Host "Getting database password..." -ForegroundColor Yellow
$DBPasswordJson = aws secretsmanager get-secret-value `
    --secret-id smartscheduler/database/master-credentials `
    --query SecretString --output text 2>$null

if (-not $DBPasswordJson) {
    Write-Host "Could not retrieve database password. Check Secrets Manager." -ForegroundColor Red
    exit 1
}

$DBPassword = ($DBPasswordJson | ConvertFrom-Json).password

# Update database connection string
Write-Host "Updating database connection string secret..." -ForegroundColor Yellow
$ConnectionString = "Host=$DBEndpoint;Port=5432;Database=smartscheduler;Username=dbadmin;Password=$DBPassword"

aws secretsmanager update-secret `
    --secret-id smartscheduler/database/connection-string `
    --secret-string $ConnectionString | Out-Null

Write-Host "Database connection string updated." -ForegroundColor Green

# Prompt for API keys
Write-Host "API Keys Configuration" -ForegroundColor Yellow

# OpenRouteService API key
$ORSKey = Read-Host "Enter OpenRouteService API key (or press Enter to skip)"
if ($ORSKey) {
    Write-Host "Updating OpenRouteService API key..." -ForegroundColor Yellow
    $ORSSecret = @{ ApiKey = $ORSKey } | ConvertTo-Json -Compress
    aws secretsmanager update-secret `
        --secret-id smartscheduler/api-keys/openrouteservice `
        --secret-string $ORSSecret | Out-Null
    Write-Host "OpenRouteService API key updated." -ForegroundColor Green
} else {
    Write-Host "Skipping OpenRouteService API key." -ForegroundColor Yellow
}

# Google Places API key
$GooglePlacesKey = Read-Host "Enter Google Places API key (or press Enter to skip)"
if ($GooglePlacesKey) {
    Write-Host "Updating Google Places API key..." -ForegroundColor Yellow
    $GooglePlacesSecret = @{ ApiKey = $GooglePlacesKey } | ConvertTo-Json -Compress
    aws secretsmanager update-secret `
        --secret-id smartscheduler/api-keys/google-places `
        --secret-string $GooglePlacesSecret | Out-Null
    Write-Host "Google Places API key updated." -ForegroundColor Green
} else {
    Write-Host "Skipping Google Places API key." -ForegroundColor Yellow
}

Write-Host "Secrets configuration complete!" -ForegroundColor Green

