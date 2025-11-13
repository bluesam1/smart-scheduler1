using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.Events;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Domain.Tests.Contracts.Entities;

public class JobTests
{
    private GeoLocation CreateValidLocation() => new GeoLocation(
        40.7128, 
        -74.0060, 
        "123 Main St", 
        "New York", 
        "NY", 
        "10001", 
        "US", 
        "123 Main St, New York, NY 10001, USA",
        "ChIJexample");

    private TimeWindow CreateValidServiceWindow() => new TimeWindow(
        DateTime.UtcNow.AddDays(1),
        DateTime.UtcNow.AddDays(1).AddHours(4));

    [Fact]
    public void Constructor_WithValidValues_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var location = CreateValidLocation();
        var serviceWindow = CreateValidServiceWindow();
        var requiredSkills = new List<string> { "Hardwood Installation", "Tile" };
        var desiredDate = DateTime.UtcNow.AddDays(2);

        // Act
        var job = new Job(
            id,
            "Hardwood Installation",
            240, // 4 hours
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            desiredDate,
            requiredSkills);

        // Assert
        Assert.Equal(id, job.Id);
        Assert.Equal("Hardwood Installation", job.Type);
        Assert.Equal(240, job.Duration);
        Assert.Equal(location, job.Location);
        Assert.Equal("America/New_York", job.Timezone);
        Assert.Equal(serviceWindow, job.ServiceWindow);
        Assert.Equal(Priority.Normal, job.Priority);
        Assert.Equal(JobStatus.Scheduled, job.Status);
        Assert.Equal(2, job.RequiredSkills.Count);
        Assert.Equal("hardwood installation", job.RequiredSkills[0]); // Normalized
        Assert.Equal("tile", job.RequiredSkills[1]); // Normalized
        Assert.Equal(AssignmentStatus.Unassigned, job.AssignmentStatus);
        Assert.Single(job.DomainEvents);
        Assert.IsType<JobCreated>(job.DomainEvents[0]);
    }

    [Fact]
    public void Constructor_WithAllProperties_ShouldCreateInstance()
    {
        // Arrange
        var id = Guid.NewGuid();
        var location = CreateValidLocation();
        var serviceWindow = CreateValidServiceWindow();
        var requiredSkills = new List<string> { "Hardwood Installation" };
        var desiredDate = DateTime.UtcNow.AddDays(2);
        var tools = new List<string> { "Hammer", "Saw" };

        // Act
        var job = new Job(
            id,
            "Hardwood Installation",
            240,
            location,
            "America/New_York",
            serviceWindow,
            Priority.High,
            desiredDate,
            requiredSkills,
            description: "Install hardwood flooring in living room",
            accessNotes: "Use side entrance",
            tools: tools);

        // Assert
        Assert.Equal("Install hardwood flooring in living room", job.Description);
        Assert.Equal("Use side entrance", job.AccessNotes);
        Assert.Equal(2, job.Tools?.Count);
        Assert.Equal(Priority.High, job.Priority);
    }

    [Fact]
    public void Constructor_WithEmptyType_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();
        var serviceWindow = CreateValidServiceWindow();
        var requiredSkills = new List<string> { "Hardwood Installation" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Job(
            Guid.NewGuid(),
            "",
            240,
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            requiredSkills));
    }

    [Fact]
    public void Constructor_WithInvalidDuration_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();
        var serviceWindow = CreateValidServiceWindow();
        var requiredSkills = new List<string> { "Hardwood Installation" };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            0,
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            requiredSkills));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            -10,
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            requiredSkills));
    }

    [Fact]
    public void Constructor_WithNullLocation_ShouldThrowException()
    {
        // Arrange
        var serviceWindow = CreateValidServiceWindow();
        var requiredSkills = new List<string> { "Hardwood Installation" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            240,
            null!,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            requiredSkills));
    }

    [Fact]
    public void Constructor_WithEmptyTimezone_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();
        var serviceWindow = CreateValidServiceWindow();
        var requiredSkills = new List<string> { "Hardwood Installation" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            240,
            location,
            "",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            requiredSkills));
    }

    [Fact]
    public void Constructor_WithInvalidServiceWindow_ShouldThrowException()
    {
        // Arrange
        var location = CreateValidLocation();
        var requiredSkills = new List<string> { "Hardwood Installation" };
        
        // Act & Assert
        // TimeWindow validates itself, so we can't create an invalid one
        // Instead, we test that null service window is rejected
        Assert.Throws<ArgumentNullException>(() => new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            240,
            location,
            "America/New_York",
            null!,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            requiredSkills));
    }

    [Fact]
    public void Constructor_WithEmptyRequiredSkills_ShouldSucceed()
    {
        // Arrange
        var location = CreateValidLocation();
        var serviceWindow = CreateValidServiceWindow();

        // Act
        var job = new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            240,
            location,
            "America/New_York",
            serviceWindow,
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            Array.Empty<string>());

        // Assert
        Assert.NotNull(job);
        Assert.Equal(0, job.RequiredSkills.Count);
    }

    [Fact]
    public void UpdateType_WithValidType_ShouldUpdate()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateType("Tile Installation");

        // Assert
        Assert.Equal("Tile Installation", job.Type);
    }

    [Fact]
    public void UpdateType_WithEmptyType_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => job.UpdateType(""));
    }

    [Fact]
    public void UpdateDuration_WithValidDuration_ShouldUpdate()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateDuration(360);

        // Assert
        Assert.Equal(360, job.Duration);
    }

    [Fact]
    public void UpdateDuration_WithInvalidDuration_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => job.UpdateDuration(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.UpdateDuration(-10));
    }

    [Fact]
    public void Reschedule_WithValidWindow_ShouldUpdateAndRaiseEvent()
    {
        // Arrange
        var job = CreateValidJob();
        var previousStart = job.ServiceWindow.Start;
        var previousEnd = job.ServiceWindow.End;
        var newWindow = new TimeWindow(
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(3).AddHours(4));

        // Act
        job.Reschedule(newWindow);

        // Assert
        Assert.Equal(newWindow, job.ServiceWindow);
        var rescheduledEvent = job.DomainEvents.OfType<JobRescheduled>().First();
        Assert.Equal(previousStart, rescheduledEvent.PreviousServiceWindowStart);
        Assert.Equal(previousEnd, rescheduledEvent.PreviousServiceWindowEnd);
        Assert.Equal(newWindow.Start, rescheduledEvent.NewServiceWindowStart);
        Assert.Equal(newWindow.End, rescheduledEvent.NewServiceWindowEnd);
    }

    [Fact]
    public void Reschedule_WithNullWindow_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => job.Reschedule(null!));
    }

    [Fact]
    public void UpdateStatus_WithValidTransition_ShouldUpdate()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateStatus(JobStatus.InProgress);

        // Assert
        Assert.Equal(JobStatus.InProgress, job.Status);
    }

    [Fact]
    public void UpdateStatus_WithInvalidTransition_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();

        // Act & Assert
        // Cannot go directly from Scheduled to Completed
        Assert.Throws<InvalidOperationException>(() => job.UpdateStatus(JobStatus.Completed));
    }

    [Fact]
    public void AssignContractor_ShouldAddAssignmentAndRaiseEvent()
    {
        // Arrange
        var job = CreateValidJob();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act
        job.AssignContractor(contractorId, startUtc, endUtc);

        // Assert
        Assert.Single(job.AssignedContractors);
        Assert.Equal(contractorId, job.AssignedContractors[0].ContractorId);
        Assert.Equal(startUtc, job.AssignedContractors[0].StartUtc);
        Assert.Equal(endUtc, job.AssignedContractors[0].EndUtc);
        Assert.Equal(JobStatus.Scheduled, job.Status); // Status remains Scheduled
        Assert.Contains(job.DomainEvents, e => e is JobAssigned);
        var assignedEvent = job.DomainEvents.OfType<JobAssigned>().First();
        Assert.Equal(contractorId, assignedEvent.ContractorId);
    }

    [Fact]
    public void AssignContractor_WithInvalidTimes_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(-1); // End before start

        // Act & Assert
        Assert.Throws<ArgumentException>(() => job.AssignContractor(contractorId, startUtc, endUtc));
    }

    [Fact]
    public void Cancel_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.Cancel("Customer cancelled");

        // Assert
        Assert.Equal(JobStatus.Canceled, job.Status);
        Assert.Contains(job.DomainEvents, e => e is JobCancelled);
        var cancelledEvent = job.DomainEvents.OfType<JobCancelled>().First();
        Assert.Equal("Customer cancelled", cancelledEvent.Reason);
    }

    [Fact]
    public void Cancel_CompletedJob_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();
        job.UpdateStatus(JobStatus.InProgress);
        job.UpdateStatus(JobStatus.Completed);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => job.Cancel("Cannot cancel"));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldThrowException()
    {
        // Arrange
        var job = CreateValidJob();
        job.Cancel("First cancellation");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => job.Cancel("Second cancellation"));
    }

    [Fact]
    public void UpdateRequiredSkills_ShouldNormalizeAndUpdate()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateRequiredSkills(new List<string> { "  Hardwood Installation  ", "TILE", "carpet" });

        // Assert
        Assert.Equal(3, job.RequiredSkills.Count);
        Assert.Equal("hardwood installation", job.RequiredSkills[0]);
        Assert.Equal("tile", job.RequiredSkills[1]);
        Assert.Equal("carpet", job.RequiredSkills[2]);
    }

    [Fact]
    public void UpdateRequiredSkills_WithEmptyList_ShouldSucceed()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateRequiredSkills(Array.Empty<string>());

        // Assert
        Assert.Equal(0, job.RequiredSkills.Count);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var job = CreateValidJob();
        job.Reschedule(CreateValidServiceWindow());
        job.UpdatePriority(Priority.High);

        // Act
        job.ClearDomainEvents();

        // Assert
        Assert.Empty(job.DomainEvents);
    }

    [Fact]
    public void AssignmentStatus_WithNoAssignments_ShouldBeUnassigned()
    {
        // Arrange
        var job = CreateValidJob();

        // Assert
        Assert.Equal(AssignmentStatus.Unassigned, job.AssignmentStatus);
    }

    [Fact]
    public void AssignmentStatus_WithAssignments_ShouldBeAssigned()
    {
        // Arrange
        var job = CreateValidJob();
        var contractorId = Guid.NewGuid();
        var startUtc = DateTime.UtcNow.AddDays(1);
        var endUtc = startUtc.AddHours(4);

        // Act
        job.AssignContractor(contractorId, startUtc, endUtc);

        // Assert
        Assert.Equal(AssignmentStatus.Assigned, job.AssignmentStatus);
    }

    private Job CreateValidJob()
    {
        return new Job(
            Guid.NewGuid(),
            "Hardwood Installation",
            240,
            CreateValidLocation(),
            "America/New_York",
            CreateValidServiceWindow(),
            Priority.Normal,
            DateTime.UtcNow.AddDays(2),
            new List<string> { "Hardwood Installation" });
    }
}

