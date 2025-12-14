using DataPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Data;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
        
    }

    public DbSet<Pipeline> Pipelines { get; set; } = null!;
    public DbSet<PipelineRun> PipelineRuns { get; set; } = null!;
    public DbSet<DataSource> DataSources { get; set; } = null!;
    public DbSet<Dataset> Datasets { get; set; } = null!;
    public DbSet<DataQualityCheck> DataQualityChecks { get; set; } = null!;
    public DbSet<DataQualityRule> DataQualityRules { get; set; } = null!;
    public DbSet<DatasetColumn> DatasetColumns { get; set; } = null!;
    public DbSet<Query> Queries { get; set; } = null!;
}