# PowerShell script to run slot generation tests and identify the root cause

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  Slot Generation Test Suite" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to src directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcPath = Join-Path (Split-Path -Parent $scriptPath) "src"
Set-Location $srcPath

Write-Host "Running SlotGenerator Domain Tests..." -ForegroundColor Yellow
Write-Host "--------------------------------------"
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests" --logger "console;verbosity=normal"
$slotResult = $LASTEXITCODE

Write-Host ""
Write-Host "Running GetRecommendationsQueryHandler Tests..." -ForegroundColor Yellow
Write-Host "--------------------------------------"
dotnet test --filter "FullyQualifiedName~GetRecommendationsQueryHandlerTests" --logger "console;verbosity=normal"
$handlerResult = $LASTEXITCODE

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  Test Results Summary" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

if ($slotResult -eq 0) {
    Write-Host "✅ SlotGenerator Tests: PASSED" -ForegroundColor Green
} else {
    Write-Host "❌ SlotGenerator Tests: FAILED" -ForegroundColor Red
    Write-Host "   → Check slot generation logic in Domain/Scheduling/Services/SlotGenerator.cs" -ForegroundColor Yellow
}

if ($handlerResult -eq 0) {
    Write-Host "✅ GetRecommendationsQueryHandler Tests: PASSED" -ForegroundColor Green
} else {
    Write-Host "❌ GetRecommendationsQueryHandler Tests: FAILED" -ForegroundColor Red
    Write-Host "   → Check recommendations handler logic in Application/Recommendations/Handlers/GetRecommendationsQueryHandler.cs" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  Key Test to Debug Empty Slots" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Run this specific test to see why slots are empty:" -ForegroundColor Yellow
Write-Host ""
Write-Host '  dotnet test --filter "FullyQualifiedName~Handle_SlotGeneratorReturnsEmpty_StillIncludesContractorWithNoSlots" --logger "console;verbosity=detailed"' -ForegroundColor White
Write-Host ""

# Exit with error if any tests failed
if ($slotResult -ne 0 -or $handlerResult -ne 0) {
    exit 1
}

exit 0

