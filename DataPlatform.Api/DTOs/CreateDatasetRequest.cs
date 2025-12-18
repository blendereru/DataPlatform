using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class CreateDatasetRequest
{
    public Guid DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    
    // NEW: Layer information
    public DataWarehouseLayer Layer { get; set; } = DataWarehouseLayer.Source;
    public TableType TableType { get; set; } = TableType.Operational;
    public string? BusinessKey { get; set; }
    public string? GrainDescription { get; set; }
}