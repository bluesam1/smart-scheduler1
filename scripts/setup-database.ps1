# Script to set up the database (PostGIS extension and migrations) - PowerShell

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

Write-Host "Setting up database..." -ForegroundColor Green

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

# Check if psql is available
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $psqlPath) {
    Write-Host "psql not found. Skipping PostGIS extension setup." -ForegroundColor Yellow
    Write-Host "You can enable PostGIS manually by connecting to the database and running:" -ForegroundColor Yellow
    Write-Host "  CREATE EXTENSION IF NOT EXISTS postgis;" -ForegroundColor Yellow
    Write-Host "  CREATE EXTENSION IF NOT EXISTS postgis_topology;" -ForegroundColor Yellow
} else {
    # Enable PostGIS extension
    Write-Host "Enabling PostGIS extension..." -ForegroundColor Yellow
    $env:PGPASSWORD = $DBPassword
    try {
        psql -h $DBEndpoint -U dbadmin -d smartscheduler -c "CREATE EXTENSION IF NOT EXISTS postgis;" 2>$null
        Write-Host "PostGIS extension enabled." -ForegroundColor Green
        
        try {
            psql -h $DBEndpoint -U dbadmin -d smartscheduler -c "CREATE EXTENSION IF NOT EXISTS postgis_topology;" 2>$null
        } catch {
            Write-Host "Failed to enable postgis_topology extension (optional)." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Failed to enable PostGIS extension. Check database connection." -ForegroundColor Red
        exit 1
    }
}

# Get connection string
Write-Host "Getting connection string..." -ForegroundColor Yellow
$ConnectionString = aws secretsmanager get-secret-value `
    --secret-id smartscheduler/database/connection-string `
    --query SecretString --output text 2>$null

if (-not $ConnectionString) {
    Write-Host "Could not retrieve connection string. Run configure-secrets.ps1 first." -ForegroundColor Red
    exit 1
}

# Run database migrations
Write-Host "Running database migrations..." -ForegroundColor Yellow
Set-Location (Join-Path $ProjectRoot "src")

dotnet ef database update `
    --project SmartScheduler.Infrastructure `
    --startup-project SmartScheduler.Api `
    --connection $ConnectionString

Write-Host "Database setup complete!" -ForegroundColor Green

