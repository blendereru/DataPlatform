namespace DataPlatform.Api.Models;

public enum PipelineType
{
    Batch,
    Streaming,
    Incremental,
    FullRefresh
}