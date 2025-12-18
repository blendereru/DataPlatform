using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class DatasetWithLineageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DataWarehouseLayer Layer { get; set; }
    public TableType TableType { get; set; }
    public List<LineageConnection> UpstreamSources { get; set; } = new();
    public List<LineageConnection> DownstreamTargets { get; set; } = new();
}