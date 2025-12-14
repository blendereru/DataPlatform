namespace DataPlatform.Api.Models;

/// <summary>
/// Ad-hoc query execution for data exploration
/// </summary>
public class Query
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public Guid DatasetId { get; set; }
    public QueryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public long? RowsReturned { get; set; }
    public double? ExecutionTimeMs { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation
    public Dataset Dataset { get; set; } = null!;
}