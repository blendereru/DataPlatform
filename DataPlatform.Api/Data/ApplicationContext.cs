using DataPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Data;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
    }

    // ============================================================================
    // Authentication
    // ============================================================================
    public DbSet<User> Users { get; set; }

    // ============================================================================
    // Data Platform Entities
    // ============================================================================
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<Dataset> Datasets { get; set; }
    public DbSet<Pipeline> Pipelines { get; set; }
    public DbSet<PipelineRun> PipelineRuns { get; set; }
    public DbSet<Query> Queries { get; set; }
    public DbSet<DataQualityRule> DataQualityRules { get; set; }
    public DbSet<DataQualityCheck> DataQualityChecks { get; set; }
    
    public DbSet<DataLineage> DataLineages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========================================================================
        // Dataset Relationships
        // ========================================================================
        
        modelBuilder.Entity<Dataset>()
            .HasOne(d => d.DataSource)
            .WithMany()
            .HasForeignKey(d => d.DataSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Store Schema as JSON
        modelBuilder.Entity<Dataset>()
            .Property(d => d.Schema)
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        // ========================================================================
        // Pipeline Relationships
        // ========================================================================
        
        modelBuilder.Entity<Pipeline>()
            .HasOne(p => p.SourceDataset)
            .WithMany()
            .HasForeignKey(p => p.SourceDatasetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pipeline>()
            .HasOne(p => p.TargetDataset)
            .WithMany()
            .HasForeignKey(p => p.TargetDatasetId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========================================================================
        // Pipeline Run Relationships
        // ========================================================================
        
        modelBuilder.Entity<PipelineRun>()
            .HasOne(r => r.Pipeline)
            .WithMany()
            .HasForeignKey(r => r.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store Metrics as JSON
        modelBuilder.Entity<PipelineRun>()
            .Property(r => r.Metrics)
            .HasColumnType("jsonb");

        // ========================================================================
        // Query Relationships
        // ========================================================================
        
        modelBuilder.Entity<Query>()
            .HasOne(q => q.Dataset)
            .WithMany()
            .HasForeignKey(q => q.DatasetId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========================================================================
        // Data Quality Relationships
        // ========================================================================
        
        modelBuilder.Entity<DataQualityRule>()
            .HasOne(r => r.Dataset)
            .WithMany()
            .HasForeignKey(r => r.DatasetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DataQualityCheck>()
            .HasOne(c => c.Rule)
            .WithMany()
            .HasForeignKey(c => c.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // ========================================================================
        // DataSource Configuration
        // ========================================================================
        
        // Store Configuration as JSON
        modelBuilder.Entity<DataSource>()
            .Property(ds => ds.Configuration)
            .HasColumnType("jsonb");

        // ========================================================================
        // Indexes for Performance
        // ========================================================================
        
        // DataSources
        modelBuilder.Entity<DataSource>()
            .HasIndex(ds => ds.Type);

        modelBuilder.Entity<DataSource>()
            .HasIndex(ds => ds.Status);

        // Datasets
        modelBuilder.Entity<Dataset>()
            .HasIndex(d => d.DataSourceId);

        modelBuilder.Entity<Dataset>()
            .HasIndex(d => d.Name);
        
        modelBuilder.Entity<Dataset>()
            .Property(d => d.Layer)
            .HasDefaultValue(DataWarehouseLayer.Source);
    
        modelBuilder.Entity<Dataset>()
            .Property(d => d.TableType)
            .HasDefaultValue(TableType.Operational);
    
        // Data Lineage relationships
        modelBuilder.Entity<DataLineage>()
            .HasOne(l => l.SourceDataset)
            .WithMany()
            .HasForeignKey(l => l.SourceDatasetId)
            .OnDelete(DeleteBehavior.Restrict);
    
        modelBuilder.Entity<DataLineage>()
            .HasOne(l => l.TargetDataset)
            .WithMany()
            .HasForeignKey(l => l.TargetDatasetId)
            .OnDelete(DeleteBehavior.Restrict);
    
        modelBuilder.Entity<DataLineage>()
            .HasOne(l => l.Pipeline)
            .WithMany()
            .HasForeignKey(l => l.PipelineId)
            .OnDelete(DeleteBehavior.SetNull);
    
        // Indexes
        modelBuilder.Entity<Dataset>()
            .HasIndex(d => d.Layer);
    
        modelBuilder.Entity<Dataset>()
            .HasIndex(d => d.TableType);
    
        modelBuilder.Entity<DataLineage>()
            .HasIndex(l => l.SourceDatasetId);
    
        modelBuilder.Entity<DataLineage>()
            .HasIndex(l => l.TargetDatasetId);

        // Pipelines
        modelBuilder.Entity<Pipeline>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<Pipeline>()
            .HasIndex(p => p.SourceDatasetId);

        modelBuilder.Entity<Pipeline>()
            .HasIndex(p => p.LastRunAt);

        // Pipeline Runs
        modelBuilder.Entity<PipelineRun>()
            .HasIndex(r => r.PipelineId);

        modelBuilder.Entity<PipelineRun>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<PipelineRun>()
            .HasIndex(r => r.StartedAt);

        // Queries
        modelBuilder.Entity<Query>()
            .HasIndex(q => q.DatasetId);

        modelBuilder.Entity<Query>()
            .HasIndex(q => q.CreatedBy);

        modelBuilder.Entity<Query>()
            .HasIndex(q => q.CreatedAt);

        // Data Quality Rules
        modelBuilder.Entity<DataQualityRule>()
            .HasIndex(r => r.DatasetId);

        modelBuilder.Entity<DataQualityRule>()
            .HasIndex(r => r.IsActive);

        // Data Quality Checks
        modelBuilder.Entity<DataQualityCheck>()
            .HasIndex(c => c.RuleId);

        modelBuilder.Entity<DataQualityCheck>()
            .HasIndex(c => c.ExecutedAt);
    }
}