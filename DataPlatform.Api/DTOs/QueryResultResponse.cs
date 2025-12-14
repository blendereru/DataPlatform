namespace DataPlatform.Api.DTOs;

public class QueryResultResponse
{
    public Guid QueryId { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public long TotalRows { get; set; }
    public double ExecutionTimeMs { get; set; }
    public List<string> Columns { get; set; } = new();
}