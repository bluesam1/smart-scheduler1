# Slot Generation Tests - Quick Start Guide

## Problem

Suggested time slots in recommendations are **always empty**, even though contractors have availability.

## Solution: Comprehensive Test Suite

I've created unit tests to identify and fix the logic issue.

## Quick Start (Windows)

```powershell
# Run all slot generation tests
.\scripts\run-slot-tests.ps1
```

## Quick Start (Linux/Mac)

```bash
# Make executable and run
chmod +x scripts/run-slot-tests.sh
./scripts/run-slot-tests.sh
```

## What Was Created

### 1. **Backend Tests** (C# / xUnit)

#### Domain Tests: `SlotGeneratorTests.cs`
- 10 test cases covering slot generation logic
- Tests edge cases: no availability, buffer time, fatigue limits, etc.
- Location: `src/SmartScheduler.Domain.Tests/Scheduling/SlotGeneratorTests.cs`

#### Application Tests: `GetRecommendationsQueryHandlerTests.cs`
- 7 test cases covering recommendations endpoint
- **Critical Test:** Verifies behavior when slots are empty but contractor has availability
- Location: `src/SmartScheduler.Application.Tests/Recommendations/GetRecommendationsQueryHandlerTests.cs`

### 2. **Frontend Tests** (TypeScript / Jest)

#### Transformation Tests: `recommendations-transformation.test.ts`
- 12 test cases for API response transformation
- Tests handling of empty/missing/null slots
- Location: `frontend/lib/api/__tests__/recommendations-transformation.test.ts`

### 3. **Documentation**

- **[TESTING.md](./docs/TESTING.md)** - Complete testing guide
- **[SLOT-GENERATION-TESTS.md](./docs/SLOT-GENERATION-TESTS.md)** - Detailed investigation guide
- **Test runner scripts** - Easy test execution

## Expected Test Results

### ✅ If Tests Pass

Tests passing means the logic is **theoretically correct**. The issue might be:
- **Data problem:** Real data has edge cases not covered by tests
- **Configuration issue:** Settings like fatigue limits are too restrictive
- **Integration issue:** Components work individually but not together

**Next Steps:**
1. Check real API responses in browser console
2. Compare with test expectations
3. Look for data/configuration differences

### ❌ If Tests Fail

Tests failing means the **logic has a bug**. The failing test will point to:
- Exact method that's broken
- Specific scenario that fails
- Expected vs actual behavior

**Next Steps:**
1. Read the failing test name (tells you what's wrong)
2. Check the "Root Cause Hypotheses" in `SLOT-GENERATION-TESTS.md`
3. Fix the identified issue
4. Re-run tests to verify

## Common Issues and Test Names

| If slots are empty because... | This test will fail |
|-------------------------------|---------------------|
| Fatigue limits too strict | `GenerateSlots_WhenFatigueCheckFails_ReturnsEmptyList` |
| Buffer time too large | `GenerateSlots_WithBufferTime_AccountsForTravel` |
| Job doesn't fit in windows | `GenerateSlots_WithShortWindow_ReturnsEmptyIfJobDoesNotFit` |
| Working hours not applied | `GenerateSlots_WithAvailableTime_ReturnsEarliestSlot` |
| Timezone conversion wrong | Multiple tests will fail |

## Running Specific Tests

### Run Just One Test (Windows)
```powershell
cd src
dotnet test --filter "FullyQualifiedName~GenerateSlots_WithAvailableTime_ReturnsEarliestSlot"
```

### Run Just One Test (Linux/Mac)
```bash
cd src
dotnet test --filter "FullyQualifiedName~GenerateSlots_WithAvailableTime_ReturnsEarliestSlot"
```

### Most Important Test (Tests the exact reported bug)
```bash
cd src
dotnet test --filter "FullyQualifiedName~Handle_SlotGeneratorReturnsEmpty_StillIncludesContractorWithNoSlots" --logger "console;verbosity=detailed"
```

## What the Tests Tell Us

The tests validate this flow:

```
1. AvailabilityEngine calculates available time windows
   ↓
2. ScoringService calculates contractor score
   ↓
3. SlotGenerator creates specific time slots ← THIS IS WHERE IT BREAKS
   ↓
4. Frontend displays slots (or "no available times")
```

**The tests will identify exactly where in Step 3 the logic fails.**

## Interpreting Results

### Example: Test Fails at Buffer Time

```
❌ GenerateSlots_WithBufferTime_AccountsForTravel FAILED
   Expected: Slot starts at 9:30 AM (after 60 min buffer)
   Actual: Empty list returned
```

**This tells you:**
- Buffer time calculation is the problem
- Check `TravelBufferService.CalculateBaseToFirstBuffer()`
- Buffer might be too large, pushing all slots outside available windows

### Example: All Tests Pass

```
✅ SlotGenerator Tests: PASSED (10/10)
✅ GetRecommendationsQueryHandler Tests: PASSED (7/7)
```

**This tells you:**
- Logic is correct
- Problem is likely with real-world data or configuration
- Check: Are real working hours set correctly? Is job duration realistic?

## Debug Workflow

1. **Run tests:**
   ```powershell
   .\scripts\run-slot-tests.ps1
   ```

2. **If tests fail:**
   - Note which test failed
   - Read test name (describes the issue)
   - Check the file mentioned in test
   - Fix the logic
   - Re-run tests

3. **If tests pass:**
   - Check real API in browser console
   - Add debug logging (see SLOT-GENERATION-TESTS.md)
   - Compare real data with test data
   - Look for edge cases

4. **Verify fix:**
   - All tests pass ✅
   - Frontend shows time slots ✅
   - Can schedule jobs successfully ✅

## Need More Help?

1. **Full Testing Guide:** [docs/TESTING.md](./docs/TESTING.md)
2. **Detailed Investigation:** [docs/SLOT-GENERATION-TESTS.md](./docs/SLOT-GENERATION-TESTS.md)
3. **Check Console Logs:** Frontend logs show raw API responses
4. **Run with Verbose:** Add `--logger "console;verbosity=detailed"` to see everything

## Files Changed

### Tests Added
- ✅ `src/SmartScheduler.Domain.Tests/Scheduling/SlotGeneratorTests.cs`
- ✅ `src/SmartScheduler.Application.Tests/Recommendations/GetRecommendationsQueryHandlerTests.cs`
- ✅ `frontend/lib/api/__tests__/recommendations-transformation.test.ts`

### Scripts Added
- ✅ `scripts/run-slot-tests.ps1` (Windows)
- ✅ `scripts/run-slot-tests.sh` (Linux/Mac)

### Documentation Added
- ✅ `docs/TESTING.md` - Complete testing guide
- ✅ `docs/SLOT-GENERATION-TESTS.md` - Investigation guide
- ✅ `TESTS-SUMMARY.md` - This file

### Frontend Changes
- ✅ Enhanced logging in `recommendations-sheet.tsx`
- ✅ Better error handling in `recommendation-card.tsx`
- ✅ New API utility: `frontend/lib/api/recommendations.ts`

## Ready to Start?

```powershell
# Windows
.\scripts\run-slot-tests.ps1

# Linux/Mac
chmod +x scripts/run-slot-tests.sh
./scripts/run-slot-tests.sh
```

The tests will tell you exactly what's wrong! 🎯

