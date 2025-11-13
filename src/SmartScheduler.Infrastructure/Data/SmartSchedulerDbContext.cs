using Microsoft.EntityFrameworkCore;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Infrastructure.Data.Configurations;

namespace SmartScheduler.Infrastructure.Data;

public class SmartSchedulerDbContext : DbContext
{
    public SmartSchedulerDbContext(DbContextOptions<SmartSchedulerDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditRecommendation> AuditRecommendations => Set<AuditRecommendation>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<WeightsConfig> WeightsConfigs => Set<WeightsConfig>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure PostGIS extension
        modelBuilder.HasPostgresExtension("postgis");

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ContractorConfiguration());
        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.ApplyConfiguration(new AssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new SystemConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new WeightsConfigConfiguration());
        
        // Configure AuditRecommendation
        modelBuilder.Entity<AuditRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestPayloadJson).IsRequired();
            entity.Property(e => e.CandidatesJson).IsRequired();
            entity.Property(e => e.SelectionActor).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure EventLog
        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.PublishedToJson).IsRequired();
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

