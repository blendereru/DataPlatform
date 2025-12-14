namespace DataPlatform.Api.Models;

/// <summary>
/// ETL/ELT pipeline for data transformations
/// </summary>
public class Pipeline
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PipelineType Type { get; set; }
    public string SourceQuery { get; set; } = string.Empty; // SQL or transformation logic
    public Guid SourceDatasetId { get; set; }
    public Guid? TargetDatasetId { get; set; }
    public string Schedule { get; set; } = string.Empty; // Cron expression
    public PipelineStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation
    public Dataset SourceDataset { get; set; } = null!;
    public Dataset? TargetDataset { get; set; }
}