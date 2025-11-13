using Moq;
using Microsoft.Extensions.Logging;
using SmartScheduler.Application.Scheduling.Services;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Repositories;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Tests.Scheduling.Services;

public class CalendarConsistencyCheckerTests
{
    private readonly Mock<IAssignmentRepository> _assignmentRepositoryMock;
    private readonly Mock<IContractorRepository> _contractorRepositoryMock;
    private readonly Mock<ILogger<CalendarConsistencyChecker>> _loggerMock;
    private readonly CalendarConsistencyChecker _checker;

    public CalendarConsistencyCheckerTests()
    {
        _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
        _contractorRepositoryMock = new Mock<IContractorRepository>();
        _loggerMock = new Mock<ILogger<CalendarConsistencyChecker>>();

        _checker = new CalendarConsistencyChecker(
            _assignmentRepositoryMock.Object,
            _contractorRepositoryMock.Object,
            _loggerMock.Object);
    }

    private Assignment CreateTestAssignment(Guid id, Guid jobId, Guid contractorId, DateTime startUtc, DateTime endUtc, AssignmentEntityStatus status = AssignmentEntityStatus.Confirmed)
    {
        var assignment = new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Manual);
        
        // Set status if not default
        if (status == AssignmentEntityStatus.Confirmed)
        {
            assignment.Confirm();
        }
        else if (status == AssignmentEntityStatus.Completed)
        {
            assignment.Confirm();
            assignment.MarkInProgress();
            assignment.MarkCompleted();
        }
        else if (status == AssignmentEntityStatus.Cancelled)
        {
            assignment.Cancel("Test");
        }
        
        return assignment;
    }

    [Fact]
    public async Task CheckConsistencyAsync_WithNoAssignments_ReturnsConsistent()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act
        var result = await _checker.CheckConsistencyAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConsistent);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task CheckConsistencyAsync_WithNonOverlappingAssignments_ReturnsConsistent()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var start1 = DateTime.UtcNow.AddDays(1);
        var end1 = start1.AddHours(2);
        var start2 = start1.AddHours(3); // Gap of 1 hour
        var end2 = start2.AddHours(2);
        
        var assignment1 = CreateTestAssignment(assignmentId1, jobId1, contractorId, start1, end1);
        var assignment2 = CreateTestAssignment(assignmentId2, jobId2, contractorId, start2, end2);
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _checker.CheckConsistencyAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConsistent);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task CheckConsistencyAsync_WithOverlappingAssignments_ReturnsInconsistent()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var start1 = DateTime.UtcNow.AddDays(1);
        var end1 = start1.AddHours(2);
        var start2 = start1.AddHours(1); // Overlaps with assignment1
        var end2 = start2.AddHours(2);
        
        var assignment1 = CreateTestAssignment(assignmentId1, jobId1, contractorId, start1, end1);
        var assignment2 = CreateTestAssignment(assignmentId2, jobId2, contractorId, start2, end2);
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _checker.CheckConsistencyAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsConsistent);
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Type == ConsistencyIssueType.Overlap);
    }

    [Fact]
    public async Task CheckConsistencyAsync_WithInvalidGap_ReturnsInconsistent()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var start1 = DateTime.UtcNow.AddDays(1);
        var end1 = start1.AddHours(2);
        var start2 = end1.AddMinutes(10); // Only 10 minutes gap (less than 15 minimum)
        var end2 = start2.AddHours(2);
        
        var assignment1 = CreateTestAssignment(assignmentId1, jobId1, contractorId, start1, end1);
        var assignment2 = CreateTestAssignment(assignmentId2, jobId2, contractorId, start2, end2);
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _checker.CheckConsistencyAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsConsistent);
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Type == ConsistencyIssueType.InvalidGap);
    }

    [Fact]
    public async Task CheckConsistencyAsync_IgnoresCancelledAssignments()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var start1 = DateTime.UtcNow.AddDays(1);
        var end1 = start1.AddHours(2);
        var start2 = start1.AddHours(1); // Would overlap, but assignment2 is cancelled
        var end2 = start2.AddHours(2);
        
        var assignment1 = CreateTestAssignment(assignmentId1, jobId1, contractorId, start1, end1);
        var assignment2 = CreateTestAssignment(assignmentId2, jobId2, contractorId, start2, end2, AssignmentEntityStatus.Cancelled);
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _checker.CheckConsistencyAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConsistent);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task CheckConsistencyAsync_IgnoresCompletedAssignments()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var start1 = DateTime.UtcNow.AddDays(1);
        var end1 = start1.AddHours(2);
        var start2 = start1.AddHours(1); // Would overlap, but assignment2 is completed
        var end2 = start2.AddHours(2);
        
        var assignment1 = CreateTestAssignment(assignmentId1, jobId1, contractorId, start1, end1);
        var assignment2 = CreateTestAssignment(assignmentId2, jobId2, contractorId, start2, end2, AssignmentEntityStatus.Completed);
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _checker.CheckConsistencyAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConsistent);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task AttemptCorrectionAsync_WithConsistentCalendar_ReturnsNoCorrections()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment>());

        // Act
        var result = await _checker.AttemptCorrectionAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CorrectionsMade);
        Assert.Empty(result.CorrectionDetails);
    }

    [Fact]
    public async Task AttemptCorrectionAsync_WithInconsistentCalendar_ReturnsRemainingIssues()
    {
        // Arrange
        var contractorId = Guid.NewGuid();
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var assignmentId1 = Guid.NewGuid();
        var assignmentId2 = Guid.NewGuid();
        
        var start1 = DateTime.UtcNow.AddDays(1);
        var end1 = start1.AddHours(2);
        var start2 = start1.AddHours(1); // Overlaps
        var end2 = start2.AddHours(2);
        
        var assignment1 = CreateTestAssignment(assignmentId1, jobId1, contractorId, start1, end1);
        var assignment2 = CreateTestAssignment(assignmentId2, jobId2, contractorId, start2, end2);
        
        _assignmentRepositoryMock.Setup(r => r.GetByContractorIdAsync(contractorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assignment> { assignment1, assignment2 });

        // Act
        var result = await _checker.AttemptCorrectionAsync(contractorId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // MVP doesn't implement automatic corrections, so should return remaining issues
        // Note: WithCorrections is used even when no corrections are made, just to return remaining issues
        Assert.NotEmpty(result.RemainingIssues);
    }
}

