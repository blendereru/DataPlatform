namespace DataPlatform.Api.DTOs;

public class ExecuteQueryRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid DatasetId { get; set; }
    public string SqlQuery { get; set; } = string.Empty;
}