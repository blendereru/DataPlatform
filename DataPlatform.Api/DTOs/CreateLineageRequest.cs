namespace DataPlatform.Api.DTOs;

public class CreateLineageRequest
{
    public Guid SourceDatasetId { get; set; }
    public Guid TargetDatasetId { get; set; }
    public Guid? PipelineId { get; set; }
    public string TransformationDescription { get; set; } = string.Empty;
}