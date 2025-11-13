#!/bin/bash

# Script to run slot generation tests and identify the root cause

echo "=================================="
echo "  Slot Generation Test Suite"
echo "=================================="
echo ""

# Navigate to src directory
cd "$(dirname "$0")/../src" || exit 1

echo "Running SlotGenerator Domain Tests..."
echo "--------------------------------------"
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests" --logger "console;verbosity=normal"
SLOT_RESULT=$?

echo ""
echo "Running GetRecommendationsQueryHandler Tests..."
echo "--------------------------------------"
dotnet test --filter "FullyQualifiedName~GetRecommendationsQueryHandlerTests" --logger "console;verbosity=normal"
HANDLER_RESULT=$?

echo ""
echo "=================================="
echo "  Test Results Summary"
echo "=================================="

if [ $SLOT_RESULT -eq 0 ]; then
    echo "✅ SlotGenerator Tests: PASSED"
else
    echo "❌ SlotGenerator Tests: FAILED"
    echo "   → Check slot generation logic in Domain/Scheduling/Services/SlotGenerator.cs"
fi

if [ $HANDLER_RESULT -eq 0 ]; then
    echo "✅ GetRecommendationsQueryHandler Tests: PASSED"
else
    echo "❌ GetRecommendationsQueryHandler Tests: FAILED"
    echo "   → Check recommendations handler logic in Application/Recommendations/Handlers/GetRecommendationsQueryHandler.cs"
fi

echo ""
echo "=================================="
echo "  Key Test to Debug Empty Slots"
echo "=================================="
echo "Run this specific test to see why slots are empty:"
echo ""
echo "  dotnet test --filter \"FullyQualifiedName~Handle_SlotGeneratorReturnsEmpty_StillIncludesContractorWithNoSlots\" --logger \"console;verbosity=detailed\""
echo ""

# Exit with error if any tests failed
if [ $SLOT_RESULT -ne 0 ] || [ $HANDLER_RESULT -ne 0 ]; then
    exit 1
fi

exit 0

