namespace DataPlatform.Api.Models;

/// <summary>
/// Tracks data flow between layers (data lineage)
/// </summary>
public class DataLineage
{
    public Guid Id { get; set; }
    public Guid SourceDatasetId { get; set; }
    public Guid TargetDatasetId { get; set; }
    public Guid? PipelineId { get; set; }
    public string TransformationDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public Dataset SourceDataset { get; set; } = null!;
    public Dataset TargetDataset { get; set; } = null!;
    public Pipeline? Pipeline { get; set; }
}