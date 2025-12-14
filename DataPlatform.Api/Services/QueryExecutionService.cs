using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Services.Abstractions;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace DataPlatform.Api.Services;

public class QueryExecutionService : IQueryExecutionService
{
    private readonly IDataSourceConnectionService _connectionService;
    private readonly ILogger<QueryExecutionService> _logger;

    public QueryExecutionService(
        IDataSourceConnectionService connectionService,
        ILogger<QueryExecutionService> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a SQL query and returns results.
    /// </summary>
    public async Task<QueryResultResponse> ExecuteQueryAsync(Dataset dataset, string sqlQuery)
    {
        _logger.LogInformation(
            "Executing query on dataset {DatasetId} ({Type})",
            dataset.Id,
            dataset.DataSource.Type
        );

        var startTime = DateTime.UtcNow;

        try
        {
            var rows = dataset.DataSource.Type switch
            {
                DataSourceType.PostgreSQL => await ExecutePostgreSqlQueryAsync(dataset.DataSource, sqlQuery),
                DataSourceType.MySQL => await ExecuteMySqlQueryAsync(dataset.DataSource, sqlQuery),
                DataSourceType.SQLServer => await ExecuteSqlServerQueryAsync(dataset.DataSource, sqlQuery),
                DataSourceType.MongoDB => await ExecuteMongoDbQueryAsync(dataset.DataSource, dataset.TableName, sqlQuery),
                _ => throw new NotImplementedException($"Query execution not implemented for {dataset.DataSource.Type}")
            };

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var columns = rows.FirstOrDefault()?.Keys.ToList() ?? new List<string>();

            return new QueryResultResponse
            {
                QueryId = Guid.Empty, // Set by caller
                Rows = rows,
                TotalRows = rows.Count,
                ExecutionTimeMs = executionTime,
                Columns = columns
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed for dataset {DatasetId}", dataset.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets a preview of dataset rows.
    /// </summary>
    public async Task<DatasetPreviewResponse> GetPreviewAsync(Dataset dataset, int limit = 100)
    {
        _logger.LogInformation(
            "Generating preview for dataset {DatasetId}, limit: {Limit}",
            dataset.Id,
            limit
        );

        try
        {
            var query = dataset.DataSource.Type switch
            {
                DataSourceType.PostgreSQL => $"SELECT * FROM {dataset.TableName} LIMIT {limit}",
                DataSourceType.MySQL => $"SELECT * FROM {dataset.TableName} LIMIT {limit}",
                DataSourceType.SQLServer => $"SELECT TOP {limit} * FROM {dataset.TableName}",
                DataSourceType.MongoDB => "{}",  // Empty filter for MongoDB
                _ => throw new NotImplementedException($"Preview not implemented for {dataset.DataSource.Type}")
            };

            var rows = dataset.DataSource.Type == DataSourceType.MongoDB
                ? await GetMongoDbPreviewAsync(dataset.DataSource, dataset.TableName, limit)
                : (await ExecuteQueryAsync(dataset, query)).Rows;

            var totalRows = await GetRowCountAsync(dataset);

            return new DatasetPreviewResponse
            {
                DatasetId = dataset.Id,
                Rows = rows,
                Schema = dataset.Schema,
                RowsShown = rows.Count,
                TotalRows = totalRows
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview generation failed for dataset {DatasetId}", dataset.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets the total row count for a dataset.
    /// </summary>
    public async Task<long> GetRowCountAsync(Dataset dataset)
    {
        _logger.LogInformation("Getting row count for dataset {DatasetId}", dataset.Id);

        try
        {
            return dataset.DataSource.Type switch
            {
                DataSourceType.PostgreSQL => await GetPostgreSqlRowCountAsync(dataset.DataSource, dataset.TableName),
                DataSourceType.MySQL => await GetMySqlRowCountAsync(dataset.DataSource, dataset.TableName),
                DataSourceType.SQLServer => await GetSqlServerRowCountAsync(dataset.DataSource, dataset.TableName),
                DataSourceType.MongoDB => await GetMongoDbRowCountAsync(dataset.DataSource, dataset.TableName),
                _ => throw new NotImplementedException($"Row count not implemented for {dataset.DataSource.Type}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row count retrieval failed for dataset {DatasetId}", dataset.Id);
            return 0;
        }
    }

    // ============================================================================
    // PostgreSQL Implementation
    // ============================================================================

    private async Task<List<Dictionary<string, object>>> ExecutePostgreSqlQueryAsync(
        DataSource source,
        string sqlQuery)
    {
        await using var conn = new NpgsqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var rows = new List<Dictionary<string, object>>();

        await using var cmd = new NpgsqlCommand(sqlQuery, conn);
        cmd.CommandTimeout = 30; // 30 second timeout

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = value ?? DBNull.Value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private async Task<long> GetPostgreSqlRowCountAsync(DataSource source, string tableName)
    {
        await using var conn = new NpgsqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var query = $"SELECT COUNT(*) FROM {tableName}";

        await using var cmd = new NpgsqlCommand(query, conn);
        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt64(result);
    }

    // ============================================================================
    // MySQL Implementation
    // ============================================================================

    private async Task<List<Dictionary<string, object>>> ExecuteMySqlQueryAsync(
        DataSource source,
        string sqlQuery)
    {
        await using var conn = new MySqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var rows = new List<Dictionary<string, object>>();

        await using var cmd = new MySqlCommand(sqlQuery, conn);
        cmd.CommandTimeout = 30;

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = value ?? DBNull.Value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private async Task<long> GetMySqlRowCountAsync(DataSource source, string tableName)
    {
        await using var conn = new MySqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var query = $"SELECT COUNT(*) FROM {tableName}";

        await using var cmd = new MySqlCommand(query, conn);
        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt64(result);
    }

    // ============================================================================
    // SQL Server Implementation
    // ============================================================================

    private async Task<List<Dictionary<string, object>>> ExecuteSqlServerQueryAsync(
        DataSource source,
        string sqlQuery)
    {
        await using var conn = new SqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var rows = new List<Dictionary<string, object>>();

        await using var cmd = new SqlCommand(sqlQuery, conn);
        cmd.CommandTimeout = 30;

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = value ?? DBNull.Value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private async Task<long> GetSqlServerRowCountAsync(DataSource source, string tableName)
    {
        await using var conn = new SqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var query = $"SELECT COUNT(*) FROM {tableName}";

        await using var cmd = new SqlCommand(query, conn);
        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt64(result);
    }

    // ============================================================================
    // MongoDB Implementation
    // ============================================================================

    private async Task<List<Dictionary<string, object>>> ExecuteMongoDbQueryAsync(
        DataSource source,
        string collectionName,
        string filterJson)
    {
        var client = new MongoClient(source.ConnectionString);
        var database = client.GetDatabase(GetMongoDbName(source));
        var collection = database.GetCollection<BsonDocument>(collectionName);

        var filter = string.IsNullOrWhiteSpace(filterJson) || filterJson == "{}"
            ? new BsonDocument()
            : BsonDocument.Parse(filterJson);

        var documents = await collection.Find(filter).Limit(1000).ToListAsync();

        return documents.Select(doc =>
        {
            var row = new Dictionary<string, object>();

            foreach (var element in doc.Elements)
            {
                row[element.Name] = BsonValueToObject(element.Value);
            }

            return row;
        }).ToList();
    }

    private async Task<List<Dictionary<string, object>>> GetMongoDbPreviewAsync(
        DataSource source,
        string collectionName,
        int limit)
    {
        var client = new MongoClient(source.ConnectionString);
        var database = client.GetDatabase(GetMongoDbName(source));
        var collection = database.GetCollection<BsonDocument>(collectionName);

        var documents = await collection.Find(new BsonDocument()).Limit(limit).ToListAsync();

        return documents.Select(doc =>
        {
            var row = new Dictionary<string, object>();

            foreach (var element in doc.Elements)
            {
                row[element.Name] = BsonValueToObject(element.Value);
            }

            return row;
        }).ToList();
    }

    private async Task<long> GetMongoDbRowCountAsync(DataSource source, string collectionName)
    {
        var client = new MongoClient(source.ConnectionString);
        var database = client.GetDatabase(GetMongoDbName(source));
        var collection = database.GetCollection<BsonDocument>(collectionName);

        return await collection.CountDocumentsAsync(new BsonDocument());
    }

    private string GetMongoDbName(DataSource source)
    {
        if (source.Configuration.TryGetValue("Database", out var dbName))
        {
            return dbName;
        }

        var uri = new MongoUrl(source.ConnectionString);
        return uri.DatabaseName ?? "test";
    }

    private object BsonValueToObject(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.String => value.AsString,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.ObjectId => value.AsObjectId.ToString(),
            BsonType.Null => DBNull.Value,
            BsonType.Array => value.AsBsonArray.Select(BsonValueToObject).ToList(),
            BsonType.Document => value.AsBsonDocument.ToDictionary(
                e => e.Name,
                e => BsonValueToObject(e.Value)
            ),
            _ => value.ToString() ?? string.Empty
        };
    }
}