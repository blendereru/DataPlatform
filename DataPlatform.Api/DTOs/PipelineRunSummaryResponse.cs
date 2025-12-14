using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class PipelineRunSummaryResponse
{
    public Guid PipelineId { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public int TotalRuns { get; set; }
    public int SuccessfulRuns { get; set; }
    public int FailedRuns { get; set; }
    public DateTime? LastRunAt { get; set; }
    public PipelineRunStatus? LastRunStatus { get; set; }
}