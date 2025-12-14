namespace DataPlatform.Api.DTOs;

public class CreateDatasetRequest
{
    public Guid DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}