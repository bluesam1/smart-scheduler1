using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Contractor entity.
/// </summary>
public class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("Contractors");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Rating)
            .IsRequired();

        builder.Property(c => c.MaxJobsPerDay)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Configure BaseLocation value object (owned entity)
        builder.OwnsOne(c => c.BaseLocation, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("BaseLocation_Latitude").IsRequired();
            location.Property(l => l.Longitude).HasColumnName("BaseLocation_Longitude").IsRequired();
            location.Property(l => l.Address).HasColumnName("BaseLocation_Address").HasMaxLength(500);
            location.Property(l => l.City).HasColumnName("BaseLocation_City").HasMaxLength(100);
            location.Property(l => l.State).HasColumnName("BaseLocation_State").HasMaxLength(50);
            location.Property(l => l.PostalCode).HasColumnName("BaseLocation_PostalCode").HasMaxLength(20);
            location.Property(l => l.Country).HasColumnName("BaseLocation_Country").HasMaxLength(50);
            location.Property(l => l.FormattedAddress).HasColumnName("BaseLocation_FormattedAddress").HasMaxLength(1000);
            location.Property(l => l.PlaceId).HasColumnName("BaseLocation_PlaceId").HasMaxLength(200);
        });

        // Ignore the WorkingHours property (we'll use the backing field)
        builder.Ignore(c => c.WorkingHours);

        // Configure WorkingHours value objects (owned collection)
        // Use backing field for owned collection
        builder.OwnsMany<WorkingHours>("_workingHours", workingHours =>
        {
            workingHours.ToTable("ContractorWorkingHours");
            workingHours.WithOwner().HasForeignKey("ContractorId");
            workingHours.Property<int>("Id").ValueGeneratedOnAdd();
            workingHours.HasKey("Id", "ContractorId");

            workingHours.Property(wh => wh.DayOfWeek).IsRequired();
            workingHours.Property(wh => wh.StartTime).IsRequired().HasColumnName("StartTime");
            workingHours.Property(wh => wh.EndTime).IsRequired().HasColumnName("EndTime");
            workingHours.Property(wh => wh.TimeZone).IsRequired().HasMaxLength(100).HasColumnName("TimeZone");
        });
        
        // Map the property to use the backing field and auto-include
        builder.Navigation("_workingHours")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        // Configure Skills (stored as comma-separated string in PostgreSQL)
        builder.Property(c => c.Skills)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()))
            .HasColumnName("Skills");

        // Configure Calendar value object (owned entity, nullable)
        builder.OwnsOne(c => c.Calendar, calendar =>
        {
            calendar.Property(c => c.DailyBreakMinutes).HasColumnName("Calendar_DailyBreakMinutes").IsRequired();

            // Configure Holidays (stored as comma-separated dates)
            calendar.Property(c => c.Holidays)
                .HasConversion(
                    v => string.Join(',', v.Select(d => d.ToString("yyyy-MM-dd"))),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(d => DateOnly.Parse(d))
                        .ToList(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<DateOnly>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnName("Calendar_Holidays");

            // Configure Exceptions (stored as JSON array in PostgreSQL)
            // Note: Storing as JSON to avoid complex nested owned entity configuration
            calendar.Property(c => c.Exceptions)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<CalendarException>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? Array.Empty<CalendarException>(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<CalendarException>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()))
                .HasColumnName("Calendar_Exceptions");
        });

        // Ignore computed properties (they're not stored in database)
        builder.Ignore(c => c.Availability);
        builder.Ignore(c => c.JobsToday);
        builder.Ignore(c => c.CurrentUtilization);
        builder.Ignore(c => c.Timezone);
        builder.Ignore(c => c.DomainEvents);
    }
}

