# Slot Generation Tests - Investigation Summary

## Problem Statement

**Issue:** Suggested time slots in recommendations are always empty, even when contractors have availability in their working hours.

**User Report:** "The suggested slots are now always empty" - Frontend always shows "Click to view calendar and schedule" message.

## Test Files Created

### 1. Backend Domain Tests
**File:** `src/SmartScheduler.Domain.Tests/Scheduling/SlotGeneratorTests.cs`

Comprehensive tests for the `SlotGenerator` service that creates suggested time slots.

**Test Coverage:**
- ✅ Returns empty list when no working hours defined
- ✅ Returns empty list when no available slots exist
- ✅ Returns earliest slot when time is available
- ✅ Accounts for travel buffer time
- ✅ Avoids conflicts with existing assignments
- ✅ Returns empty when fatigue check fails
- ✅ Handles multiple days correctly
- ✅ Returns empty if job duration doesn't fit in window
- ✅ Prioritizes earlier slots for rush jobs

### 2. Backend Application Tests
**File:** `src/SmartScheduler.Application.Tests/Recommendations/GetRecommendationsQueryHandlerTests.cs`

Tests for the recommendations query handler that orchestrates the slot generation process.

**Test Coverage:**
- ✅ Throws exception when job not found
- ✅ Returns empty when no contractors available
- ✅ Excludes contractors with no availability
- ✅ Includes contractors with availability and slots
- ✅ **CRITICAL:** Still includes contractor when slot generator returns empty (tests the reported bug)
- ✅ Ranks multiple contractors by score correctly

### 3. Frontend Transformation Tests
**File:** `frontend/lib/api/__tests__/recommendations-transformation.test.ts`

Tests for transforming API responses to component format.

**Test Coverage:**
- ✅ Transforms recommendations with suggested slots
- ✅ Handles empty suggested slots array
- ✅ Handles missing/null suggested slots property
- ✅ Converts slot types correctly
- ✅ Converts confidence levels correctly
- ✅ Converts distance and travel time
- ✅ Handles missing data gracefully

## Running the Tests

### Quick Test Run (Windows)
```powershell
.\scripts\run-slot-tests.ps1
```

### Quick Test Run (Linux/Mac)
```bash
chmod +x scripts/run-slot-tests.sh
./scripts/run-slot-tests.sh
```

### Manual Test Runs

#### All Slot Generator Tests
```bash
cd src
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests" --logger "console;verbosity=detailed"
```

#### All Recommendations Handler Tests
```bash
cd src
dotnet test --filter "FullyQualifiedName~GetRecommendationsQueryHandlerTests" --logger "console;verbosity=detailed"
```

#### Critical Bug Test (Most Important)
```bash
cd src
dotnet test --filter "FullyQualifiedName~Handle_SlotGeneratorReturnsEmpty_StillIncludesContractorWithNoSlots" --logger "console;verbosity=detailed"
```

This test specifically validates the scenario where:
1. Contractor has available time windows (from AvailabilityEngine)
2. But SlotGenerator returns empty list
3. Contractor should still be included in recommendations with empty `suggestedSlots` array

## Expected vs Actual Behavior

### Expected (Correct) Behavior

```json
{
  "recommendations": [
    {
      "contractorId": "...",
      "contractorName": "John Doe",
      "score": 85.5,
      "suggestedSlots": [
        {
          "startUtc": "2025-01-20T14:30:00Z",
          "endUtc": "2025-01-20T16:30:00Z",
          "type": "earliest",
          "confidence": 85
        }
      ]
    }
  ]
}
```

### Actual (Bug) Behavior

```json
{
  "recommendations": [
    {
      "contractorId": "...",
      "contractorName": "John Doe",
      "score": 85.5,
      "suggestedSlots": []  // ❌ ALWAYS EMPTY
    }
  ]
}
```

## Root Cause Hypotheses

Based on the test structure, the issue is likely in one of these areas:

### Hypothesis 1: Fatigue Calculator Too Restrictive
**Test:** `GenerateSlots_WhenFatigueCheckFails_ReturnsEmptyList`

**Check:**
- Are daily hour limits too low?
- Is the fatigue check logic incorrect?
- Are existing assignments being double-counted?

**File to Investigate:** `src/SmartScheduler.Domain/Scheduling/Services/FatigueCalculator.cs`

### Hypothesis 2: Buffer Time Calculation Issue
**Test:** `GenerateSlots_WithBufferTime_AccountsForTravel`

**Check:**
- Is buffer time being calculated correctly?
- Is buffer + job duration exceeding available windows?
- Are timezone conversions affecting buffer calculations?

**File to Investigate:** `src/SmartScheduler.Domain/Scheduling/Services/TravelBufferService.cs`

### Hypothesis 3: Duration Doesn't Fit
**Test:** `GenerateSlots_WithShortWindow_ReturnsEmptyIfJobDoesNotFit`

**Check:**
- Are available windows being calculated incorrectly?
- Is job duration being inflated somewhere?
- Are windows being fragmented by existing assignments?

**File to Investigate:** `src/SmartScheduler.Domain/Scheduling/Services/AvailabilityEngine.cs`

### Hypothesis 4: Timezone Conversion Bug
**Check:**
- Are service window times being converted correctly?
- Is contractor timezone vs job timezone causing windows to disappear?
- Are working hours being applied in the wrong timezone?

**Files to Investigate:**
- `src/SmartScheduler.Domain/Scheduling/Services/WorkingHoursCalculator.cs`
- `src/SmartScheduler.Domain/Scheduling/Services/SlotGenerator.cs` (timezone handling)

### Hypothesis 5: Quarter-Hour Rounding Issue
**Check:**
- Is the rounding logic pushing slots outside available windows?
- Are slots being rounded down when they should round up?

**File to Investigate:** `src/SmartScheduler.Domain/Scheduling/Services/SlotGenerator.cs` (lines with `RoundToNearestQuarterHour`)

## Debugging Steps

### Step 1: Run Tests and Identify Failures
```bash
cd src
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests" --logger "console;verbosity=detailed" > slot-test-results.txt
```

Review `slot-test-results.txt` for failures.

### Step 2: Add Debug Logging
Add temporary console logging to `SlotGenerator.GenerateSlots()`:

```csharp
public List<GeneratedSlot> GenerateSlots(...)
{
    Console.WriteLine($"[DEBUG] GenerateSlots called with:");
    Console.WriteLine($"  - Working Hours: {workingHours.Count} entries");
    Console.WriteLine($"  - Service Window: {serviceWindow.Start} to {serviceWindow.End}");
    Console.WriteLine($"  - Job Duration: {jobDurationMinutes} min");
    Console.WriteLine($"  - Contractor TZ: {contractorTimezone}");
    Console.WriteLine($"  - Job TZ: {jobTimezone}");
    
    // ... rest of method
    
    var result = new List<GeneratedSlot>();
    Console.WriteLine($"[DEBUG] Returning {result.Count} slots");
    return result;
}
```

### Step 3: Run Real Recommendations Request
1. Start the backend API
2. Use the frontend console logs to see the raw API response
3. Compare with test expectations

### Step 4: Fix and Verify
1. Identify root cause from test failures or debug logs
2. Fix the issue in production code
3. Run tests again to verify fix
4. Check frontend to confirm slots appear

## Success Criteria

Tests will pass when:
- ✅ SlotGenerator returns non-empty list when contractor has availability
- ✅ SlotGenerator correctly handles timezone conversions
- ✅ SlotGenerator accounts for buffer time without making all slots invalid
- ✅ Fatigue calculator doesn't incorrectly reject valid slots
- ✅ Quarter-hour rounding doesn't push slots outside windows
- ✅ Frontend transformation correctly handles both empty and populated slots

## Related Documentation

- [Testing Guide](./TESTING.md) - Full testing documentation
- [Architecture: Scheduling Services](./architecture/high-level-architecture.md#scheduling-services)
- [Slot Generation Algorithm](./architecture/api-specification.md#slot-generation)

## Next Steps

1. **Run the tests:** `.\scripts\run-slot-tests.ps1`
2. **Review failures:** Look for which specific test is failing
3. **Check logs:** Review console output for hints
4. **Fix the bug:** Update the production code based on test results
5. **Verify fix:** Run tests again and check frontend behavior
6. **Remove debug logging:** Clean up any temporary logging added

## Contact

If tests reveal an issue you're unsure how to fix:
1. Review the test that's failing
2. Check the "Root Cause Hypotheses" section above
3. Add debug logging to the suspected file
4. Review the test expectations vs actual behavior

