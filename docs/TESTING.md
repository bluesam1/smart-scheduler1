# Testing Guide

This document describes how to run and write tests for the SmartScheduler project.

## Backend Tests (C# / xUnit)

### Running All Backend Tests

```bash
# From the project root
cd src
dotnet test

# Or run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Running Specific Test Projects

```bash
# Domain tests only
cd src/SmartScheduler.Domain.Tests
dotnet test

# Application tests only
cd src/SmartScheduler.Application.Tests
dotnet test

# Specific test class
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests.GenerateSlots_WithNoWorkingHours_ReturnsEmptyList"
```

### New Test Files Created

#### 1. **SlotGeneratorTests** (`src/SmartScheduler.Domain.Tests/Scheduling/SlotGeneratorTests.cs`)
Tests the core slot generation logic that creates suggested time slots for recommendations.

**Key Test Cases:**
- ✅ Empty working hours returns empty slots
- ✅ No available slots returns empty list
- ✅ With available time, returns earliest slot
- ✅ Accounts for travel buffer time
- ✅ Avoids conflicts with existing assignments
- ✅ Respects fatigue limits
- ✅ Handles multiple days
- ✅ Returns empty if job duration doesn't fit
- ✅ Prioritizes earlier slots for rush jobs

**To Run:**
```bash
cd src
dotnet test --filter "FullyQualifiedName~SlotGeneratorTests"
```

#### 2. **GetRecommendationsQueryHandlerTests** (`src/SmartScheduler.Application.Tests/Recommendations/GetRecommendationsQueryHandlerTests.cs`)
Tests the main recommendations endpoint handler that orchestrates slot generation and scoring.

**Key Test Cases:**
- ✅ Throws exception when job not found
- ✅ Returns empty when no contractors available
- ✅ Excludes contractors with no availability
- ✅ Includes contractors with availability
- ✅ Still includes contractor even if slot generator returns empty (THIS IS THE KEY TEST!)
- ✅ Ranks contractors by score

**To Run:**
```bash
cd src
dotnet test --filter "FullyQualifiedName~GetRecommendationsQueryHandlerTests"
```

**Critical Test:**
```bash
# This test verifies the bug - contractor has availability but no suggested slots
dotnet test --filter "FullyQualifiedName~Handle_SlotGeneratorReturnsEmpty_StillIncludesContractorWithNoSlots"
```

## Frontend Tests (TypeScript / Jest)

### Setup (First Time)

The frontend tests need Jest to be configured. To set up:

```bash
cd frontend
npm install --save-dev jest @types/jest ts-jest
npx ts-jest config:init
```

### Running Frontend Tests

```bash
cd frontend
npm test

# Watch mode
npm test -- --watch

# Coverage
npm test -- --coverage
```

### Test Files Created

#### 1. **Recommendations Transformation Tests** (`frontend/lib/api/__tests__/recommendations-transformation.test.ts`)
Tests the transformation of API responses to component format.

**Key Test Cases:**
- ✅ Transforms recommendations with suggested slots correctly
- ✅ Handles empty suggested slots array
- ✅ Handles missing suggested slots property
- ✅ Handles null suggested slots
- ✅ Correctly converts slot types
- ✅ Correctly converts confidence levels
- ✅ Converts distance from meters to miles
- ✅ Formats travel time correctly
- ✅ Handles missing contractor base location
- ✅ Rounds scores correctly
- ✅ Handles missing rotation score

## Debugging Failed Tests

### Backend Tests

1. **View detailed output:**
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

2. **Run single test with debugging:**
   - Open test file in Visual Studio or VS Code
   - Set breakpoint
   - Right-click test method → "Debug Test"

3. **Check logs:**
   - Tests use `Mock<ILogger>` - check mock setup for log verification
   - Look for console output in test results

### Frontend Tests

1. **Run with verbose output:**
   ```bash
   npm test -- --verbose
   ```

2. **Debug in VS Code:**
   - Add launch configuration:
   ```json
   {
     "type": "node",
     "request": "launch",
     "name": "Jest Debug",
     "program": "${workspaceFolder}/frontend/node_modules/.bin/jest",
     "args": ["--runInBand", "--no-cache"],
     "console": "integratedTerminal",
     "internalConsoleOptions": "neverOpen"
   }
   ```

3. **Check console logs:**
   - Tests include `console.log` statements
   - Review test output for transformation details

## Understanding the Slot Generation Bug

Based on the tests, the issue is likely:

### Problem Flow:
1. **AvailabilityEngine** calculates available time windows ✅
2. **ScoringService** calculates contractor score ✅  
3. **SlotGenerator** tries to generate specific time slots ❌ **RETURNS EMPTY**
4. **Frontend** shows "No available times" even though contractor has availability

### Possible Causes (Tests Will Reveal):

1. **Fatigue Calculator Issue:**
   - Test: `GenerateSlots_WhenFatigueCheckFails_ReturnsEmptyList`
   - Check: Are fatigue limits too restrictive?

2. **Buffer Time Issue:**
   - Test: `GenerateSlots_WithBufferTime_AccountsForTravel`
   - Check: Is buffer + job duration exceeding available windows?

3. **Duration Mismatch:**
   - Test: `GenerateSlots_WithShortWindow_ReturnsEmptyIfJobDoesNotFit`
   - Check: Are available windows too short for job duration?

4. **Timezone Issues:**
   - Check: Are time conversions causing windows to disappear?

5. **Rounding Issues:**
   - Check: Is quarter-hour rounding pushing slots outside windows?

## Running Specific Bug Investigation Tests

```bash
# Test if fatigue limits are causing empty slots
cd src
dotnet test --filter "FullyQualifiedName~GenerateSlots_WhenFatigueCheckFails"

# Test if buffer time is the issue
dotnet test --filter "FullyQualifiedName~GenerateSlots_WithBufferTime"

# Test if duration doesn't fit
dotnet test --filter "FullyQualifiedName~GenerateSlots_WithShortWindow"

# Test the exact scenario: availability but no slots
dotnet test --filter "FullyQualifiedName~Handle_SlotGeneratorReturnsEmpty"
```

## Next Steps

1. **Run the backend tests:**
   ```bash
   cd src
   dotnet test --logger "console;verbosity=detailed"
   ```

2. **Look for failures** - any failing test reveals the logic issue

3. **Check test output** for console logs showing:
   - What time windows are available
   - What the slot generator receives
   - Why it returns empty

4. **Fix the identified issue** in the production code

5. **Verify fix** by running tests again

## Writing New Tests

### Backend (xUnit + Moq)

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var mock = new Mock<IDependency>();
    mock.Setup(m => m.Method(It.IsAny<string>())).Returns("result");
    var sut = new SystemUnderTest(mock.Object);

    // Act
    var result = sut.DoSomething();

    // Assert
    Assert.Equal("expected", result);
}
```

### Frontend (Jest)

```typescript
test('describes what is being tested', () => {
    // Arrange
    const input = { /* test data */ };

    // Act
    const result = functionUnderTest(input);

    // Assert
    expect(result).toBe('expected');
});
```

## Continuous Integration

### Running Tests in CI/CD

```yaml
# .github/workflows/tests.yml
- name: Run Backend Tests
  run: |
    cd src
    dotnet test --logger "trx;LogFileName=test-results.trx" --no-build

- name: Run Frontend Tests  
  run: |
    cd frontend
    npm test -- --ci --coverage
```

## Test Coverage Goals

- **Backend:** Aim for 80%+ coverage on core business logic
- **Frontend:** Aim for 70%+ coverage on data transformation and business logic
- **Integration Tests:** Cover critical user flows end-to-end

## Troubleshooting

### "Test not found"
- Ensure test project references the project under test
- Check namespace matches folder structure
- Rebuild solution: `dotnet build`

### "Mock setup not called"
- Verify mock setup matches actual call signature
- Check `It.IsAny<T>()` types match
- Use `.Verifiable()` and `.Verify()` to debug

### "Tests hang or timeout"
- Check for infinite loops
- Verify async/await usage
- Increase timeout: `[Fact(Timeout = 5000)]`

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Jest Documentation](https://jestjs.io/)
- [Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)

