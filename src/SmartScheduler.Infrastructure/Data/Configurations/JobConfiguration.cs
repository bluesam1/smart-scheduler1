using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Job entity.
/// </summary>
public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .IsRequired();

        builder.Property(j => j.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .HasMaxLength(2000);

        builder.Property(j => j.Duration)
            .IsRequired();

        builder.Property(j => j.Timezone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(j => j.AccessNotes)
            .HasMaxLength(1000);

        builder.Property(j => j.DesiredDate)
            .IsRequired();

        builder.Property(j => j.CreatedAt)
            .IsRequired();

        builder.Property(j => j.UpdatedAt)
            .IsRequired();

        // Configure Location value object (owned entity)
        builder.OwnsOne(j => j.Location, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("Location_Latitude").IsRequired();
            location.Property(l => l.Longitude).HasColumnName("Location_Longitude").IsRequired();
            location.Property(l => l.Address).HasColumnName("Location_Address").HasMaxLength(500);
            location.Property(l => l.City).HasColumnName("Location_City").HasMaxLength(100);
            location.Property(l => l.State).HasColumnName("Location_State").HasMaxLength(50);
            location.Property(l => l.PostalCode).HasColumnName("Location_PostalCode").HasMaxLength(20);
            location.Property(l => l.Country).HasColumnName("Location_Country").HasMaxLength(50);
            location.Property(l => l.FormattedAddress).HasColumnName("Location_FormattedAddress").HasMaxLength(1000);
            location.Property(l => l.PlaceId).HasColumnName("Location_PlaceId").HasMaxLength(200);
        });

        // Configure ServiceWindow value object (owned entity)
        builder.OwnsOne(j => j.ServiceWindow, serviceWindow =>
        {
            serviceWindow.Property(sw => sw.Start).HasColumnName("ServiceWindow_Start").IsRequired();
            serviceWindow.Property(sw => sw.End).HasColumnName("ServiceWindow_End").IsRequired();
        });

        // Configure RequiredSkills (stored as comma-separated string in PostgreSQL)
        builder.Property(j => j.RequiredSkills)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()))
            .HasColumnName("RequiredSkills");

        // Configure Tools (stored as comma-separated string in PostgreSQL)
        builder.Property(j => j.Tools)
            .HasConversion(
                v => v == null ? null : string.Join(',', v),
                v => string.IsNullOrWhiteSpace(v) ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>?>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c == null ? null : c.ToList()))
            .HasColumnName("Tools");

        // Ignore the AssignedContractors property (we'll use the backing field)
        builder.Ignore(j => j.AssignedContractors);

        // Configure ContractorAssignment value objects (owned collection)
        // Use backing field for owned collection
        builder.OwnsMany<ContractorAssignment>("_assignments", assignments =>
        {
            assignments.ToTable("JobAssignments");
            assignments.WithOwner().HasForeignKey("JobId");
            assignments.Property<int>("Id").ValueGeneratedOnAdd();
            assignments.HasKey("Id", "JobId");

            assignments.Property(a => a.ContractorId).IsRequired().HasColumnName("ContractorId");
            assignments.Property(a => a.StartUtc).IsRequired().HasColumnName("StartUtc");
            assignments.Property(a => a.EndUtc).IsRequired().HasColumnName("EndUtc");
        });

        // Map the property to use the backing field
        builder.Navigation("_assignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ignore computed properties (they're not stored in database)
        builder.Ignore(j => j.AssignmentStatus);
        builder.Ignore(j => j.DomainEvents);
    }
}

