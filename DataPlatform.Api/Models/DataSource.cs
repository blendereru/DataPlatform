namespace DataPlatform.Api.Models;

/// <summary>
/// Represents an external data source (database, API, file system, etc.)
/// </summary>
public class DataSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty; // Encrypted in practice
    public Dictionary<string, string> Configuration { get; set; } = new();
    public DataSourceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}