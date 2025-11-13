# Script to seed the database with development data
# Usage: .\scripts\seed-development-data.ps1

$ErrorActionPreference = "Stop"

Write-Host "Seeding database with development data..." -ForegroundColor Green

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $scriptPath ".."
$apiPath = Join-Path $projectRoot "src\SmartScheduler.Api"

Set-Location $apiPath

# Run the seeder (this will be implemented as a CLI command or endpoint)
# For now, this is a placeholder that will be implemented once domain entities exist
dotnet run -- seed-development

Write-Host "Development data seeded successfully!" -ForegroundColor Green

