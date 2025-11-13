using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SystemConfiguration entity.
/// </summary>
public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.ToTable("SystemConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired();

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.UpdatedBy)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Configure Values (stored as JSON array in PostgreSQL)
        builder.Property(c => c.Values)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>(),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()))
            .HasColumnName("Values")
            .IsRequired();
        
        // Ignore the read-only property
        builder.Ignore(c => c.ValuesReadOnly);

        // Create unique index on Type (only one configuration per type)
        builder.HasIndex(c => c.Type)
            .IsUnique();
    }
}

