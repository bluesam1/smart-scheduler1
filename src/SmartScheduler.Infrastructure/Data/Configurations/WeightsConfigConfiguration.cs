using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartScheduler.Domain.Contracts.Entities;

namespace SmartScheduler.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for WeightsConfig entity.
/// </summary>
public class WeightsConfigConfiguration : IEntityTypeConfiguration<WeightsConfig>
{
    public void Configure(EntityTypeBuilder<WeightsConfig> builder)
    {
        builder.ToTable("WeightsConfigs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired();

        builder.Property(c => c.Version)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.ConfigJson)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSONB type

        builder.Property(c => c.ChangeNotes)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedBy)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Create unique index on Version
        builder.HasIndex(c => c.Version)
            .IsUnique();

        // Create index on IsActive for quick lookup of active config
        builder.HasIndex(c => c.IsActive);
    }
}


