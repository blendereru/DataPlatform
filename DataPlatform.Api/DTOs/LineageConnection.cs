using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class LineageConnection
{
    public Guid DatasetId { get; set; }
    public string DatasetName { get; set; } = string.Empty;
    public DataWarehouseLayer Layer { get; set; }
    public string? PipelineName { get; set; }
    public string TransformationDescription { get; set; } = string.Empty;
}