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
}