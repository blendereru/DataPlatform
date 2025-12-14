using DataPlatform.Api.Models;

namespace DataPlatform.Api.DTOs;

public class CreateDataSourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string>? Configuration { get; set; }
}