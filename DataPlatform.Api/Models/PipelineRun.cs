namespace DataPlatform.Api.Models;

/// <summary>
/// Individual execution of a pipeline
/// </summary>
public class PipelineRun
{
    public Guid Id { get; set; }
    public Guid PipelineId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public PipelineRunStatus Status { get; set; }
    public long RowsProcessed { get; set; }
    public long RowsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    
    // Navigation
    public Pipeline Pipeline { get; set; } = null!;
}