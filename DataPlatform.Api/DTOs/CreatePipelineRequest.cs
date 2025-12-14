using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class CreatePipelineRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PipelineType Type { get; set; }
    public Guid SourceDatasetId { get; set; }
    public Guid? TargetDatasetId { get; set; }
    public string SourceQuery { get; set; } = string.Empty;
    public string? Schedule { get; set; }
}