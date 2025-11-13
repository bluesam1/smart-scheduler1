using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Assignment entity.
/// </summary>
public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .IsRequired();

        builder.Property(a => a.JobId)
            .IsRequired();

        builder.Property(a => a.ContractorId)
            .IsRequired();

        builder.Property(a => a.StartUtc)
            .IsRequired();

        builder.Property(a => a.EndUtc)
            .IsRequired();

        builder.Property(a => a.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.AuditId)
            .IsRequired(false);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Configure relationships
        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Contractor>()
            .WithMany()
            .HasForeignKey(a => a.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AuditRecommendation>()
            .WithMany()
            .HasForeignKey(a => a.AuditId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes for common queries
        builder.HasIndex(a => a.JobId);
        builder.HasIndex(a => a.ContractorId);
        builder.HasIndex(a => a.StartUtc);
        builder.HasIndex(a => a.EndUtc);
        builder.HasIndex(a => new { a.ContractorId, a.StartUtc, a.EndUtc });

        // Ignore domain events (not persisted)
        builder.Ignore(a => a.DomainEvents);
    }
}

