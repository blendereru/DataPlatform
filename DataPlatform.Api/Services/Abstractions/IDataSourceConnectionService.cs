using System.Data;
using DataPlatform.Api.Models;

namespace DataPlatform.Api.Services.Abstractions;

/// <summary>
/// Service for testing connections and discovering schemas from various data sources.
/// </summary>
public interface IDataSourceConnectionService
{
    Task<ConnectionTestResult> TestConnectionAsync(DataSource source);
    Task<List<string>> DiscoverTablesAsync(DataSource source);
    Task<List<DatasetColumn>> DiscoverSchemaAsync(DataSource source, string tableName);
    Task<IDbConnection> GetConnectionAsync(DataSource source);
}

public class ConnectionTestResult
{
    /// <summary>
    /// Whether the connection test succeeded.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Human-readable message about the test result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional details about the connection (e.g., database version, server info).
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Error message if the connection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Time taken to establish the connection in milliseconds.
    /// </summary>
    public double? ConnectionTimeMs { get; set; }
}