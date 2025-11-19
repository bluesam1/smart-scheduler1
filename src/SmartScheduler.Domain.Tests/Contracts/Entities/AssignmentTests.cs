using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Events;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.Entities;

public class AssignmentTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act
        var assignment = new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Auto);

        // Assert
        Assert.Equal(id, assignment.Id);
        Assert.Equal(jobId, assignment.JobId);
        Assert.Equal(contractorId, assignment.ContractorId);
        Assert.Equal(startUtc, assignment.StartUtc);
        Assert.Equal(endUtc, assignment.EndUtc);
        Assert.Equal(AssignmentSource.Auto, assignment.Source);
        Assert.Null(assignment.AuditId);
        Assert.Equal(AssignmentEntityStatus.Pending, assignment.Status);
        Assert.Single(assignment.DomainEvents);
        Assert.IsType<AssignmentCreated>(assignment.DomainEvents[0]);
    }

    [Fact]
    public void Constructor_WithAuditId_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var auditId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act
        var assignment = new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Auto,
            auditId);

        // Assert
        Assert.Equal(auditId, assignment.AuditId);
    }

    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Assignment(
            Guid.Empty,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Auto));
    }

    [Fact]
    public void Constructor_WithEmptyJobId_ShouldThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Assignment(
            id,
            Guid.Empty,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Auto));
    }

    [Fact]
    public void Constructor_WithEmptyContractorId_ShouldThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Assignment(
            id,
            jobId,
            Guid.Empty,
            startUtc,
            endUtc,
            AssignmentSource.Auto));
    }

    [Fact]
    public void Constructor_WithInvalidTimeSlot_ShouldThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(-1); // End before start

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Auto));
    }

    [Fact]
    public void Constructor_WithEqualStartAndEnd_ShouldThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc; // Equal times

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Assignment(
            id,
            jobId,
            contractorId,
            startUtc,
            endUtc,
            AssignmentSource.Auto));
    }

    [Fact]
    public void Confirm_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var assignment = CreateValidAssignment();

        // Act
        assignment.Confirm();

        // Assert
        Assert.Equal(AssignmentEntityStatus.Confirmed, assignment.Status);
        Assert.Contains(assignment.DomainEvents, e => e is AssignmentConfirmed);
        var confirmedEvent = assignment.DomainEvents.OfType<AssignmentConfirmed>().First();
        Assert.Equal(assignment.Id, confirmedEvent.AssignmentId);
        Assert.Equal(assignment.JobId, confirmedEvent.JobId);
        Assert.Equal(assignment.ContractorId, confirmedEvent.ContractorId);
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Cancel("Test cancellation");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.Confirm());
    }

    [Fact]
    public void Confirm_WhenCompleted_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();
        assignment.MarkInProgress();
        assignment.MarkCompleted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.Confirm());
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.Confirm());
    }

    [Fact]
    public void MarkInProgress_WhenConfirmed_ShouldUpdateStatus()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();

        // Act
        assignment.MarkInProgress();

        // Assert
        Assert.Equal(AssignmentEntityStatus.InProgress, assignment.Status);
    }

    [Fact]
    public void MarkInProgress_WhenNotConfirmed_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.MarkInProgress());
    }

    [Fact]
    public void MarkCompleted_WhenInProgress_ShouldUpdateStatus()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();
        assignment.MarkInProgress();

        // Act
        assignment.MarkCompleted();

        // Assert
        Assert.Equal(AssignmentEntityStatus.Completed, assignment.Status);
    }

    [Fact]
    public void MarkCompleted_WhenNotInProgress_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.MarkCompleted());
    }

    [Fact]
    public void Cancel_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        var reason = "Customer cancelled";

        // Act
        assignment.Cancel(reason);

        // Assert
        Assert.Equal(AssignmentEntityStatus.Cancelled, assignment.Status);
        Assert.Contains(assignment.DomainEvents, e => e is AssignmentCancelled);
        var cancelledEvent = assignment.DomainEvents.OfType<AssignmentCancelled>().First();
        Assert.Equal(assignment.Id, cancelledEvent.AssignmentId);
        Assert.Equal(reason, cancelledEvent.Reason);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();
        assignment.MarkInProgress();
        assignment.MarkCompleted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.Cancel("Cannot cancel"));
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Cancel("First cancellation");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.Cancel("Second cancellation"));
    }

    [Fact]
    public void Cancel_WithNullReason_ShouldUseDefaultReason()
    {
        // Arrange
        var assignment = CreateValidAssignment();

        // Act
        assignment.Cancel(null);

        // Assert
        var cancelledEvent = assignment.DomainEvents.OfType<AssignmentCancelled>().First();
        Assert.Equal("No reason provided", cancelledEvent.Reason);
    }

    [Fact]
    public void UpdateTimeSlot_WithValidTimes_ShouldUpdate()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = newStartUtc.AddHours(5);

        // Act
        assignment.UpdateTimeSlot(newStartUtc, newEndUtc);

        // Assert
        Assert.Equal(newStartUtc, assignment.StartUtc);
        Assert.Equal(newEndUtc, assignment.EndUtc);
    }

    [Fact]
    public void UpdateTimeSlot_WithInvalidTimes_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        var newStartUtc = DateTime.UtcNow.AddDays(2);
        var newEndUtc = newStartUtc.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => assignment.UpdateTimeSlot(newStartUtc, newEndUtc));
    }

    [Fact]
    public void UpdateTimeSlot_WhenCompleted_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();
        assignment.MarkInProgress();
        assignment.MarkCompleted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.UpdateTimeSlot(
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(4)));
    }

    [Fact]
    public void UpdateTimeSlot_WhenCancelled_ShouldThrowException()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Cancel("Test");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => assignment.UpdateTimeSlot(
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(4)));
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var assignment = CreateValidAssignment();
        assignment.Confirm();
        assignment.Cancel("Test");

        // Act
        assignment.ClearDomainEvents();

        // Assert
        Assert.Empty(assignment.DomainEvents);
    }

    private Assignment CreateValidAssignment()
    {
        return new Assignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(4),
            AssignmentSource.Auto);
    }
}




