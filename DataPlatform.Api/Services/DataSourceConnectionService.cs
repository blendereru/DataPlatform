using System.Data;
using DataPlatform.Api.Models;
using DataPlatform.Api.Services.Abstractions;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace DataPlatform.Api.Services;

public class DataSourceConnectionService : IDataSourceConnectionService
{
    private readonly ILogger<DataSourceConnectionService> _logger;

    public DataSourceConnectionService(ILogger<DataSourceConnectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tests if a data source connection is valid and accessible.
    /// </summary>
    public async Task<ConnectionTestResult> TestConnectionAsync(DataSource source)
    {
        _logger.LogInformation("Testing connection to {Type} source: {Name}", source.Type, source.Name);

        try
        {
            switch (source.Type)
            {
                case DataSourceType.PostgreSQL:
                    return await TestPostgreSqlAsync(source);
                
                case DataSourceType.MySQL:
                    return await TestMySqlAsync(source);
                
                case DataSourceType.SQLServer:
                    return await TestSqlServerAsync(source);
                
                case DataSourceType.MongoDB:
                    return await TestMongoDbAsync(source);
                
                default:
                    return new ConnectionTestResult
                    {
                        Success = false,
                        Message = $"Connection testing not implemented for {source.Type}"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for {Name}", source.Name);
            return new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                Details = ex.StackTrace
            };
        }
    }

    /// <summary>
    /// Discovers all tables/collections in a data source.
    /// </summary>
    public async Task<List<string>> DiscoverTablesAsync(DataSource source)
    {
        _logger.LogInformation("Discovering tables in {Type} source: {Name}", source.Type, source.Name);

        switch (source.Type)
        {
            case DataSourceType.PostgreSQL:
                return await DiscoverPostgreSqlTablesAsync(source);
            
            case DataSourceType.MySQL:
                return await DiscoverMySqlTablesAsync(source);
            
            case DataSourceType.SQLServer:
                return await DiscoverSqlServerTablesAsync(source);
            
            case DataSourceType.MongoDB:
                return await DiscoverMongoDbCollectionsAsync(source);
            
            default:
                throw new NotImplementedException($"Table discovery not implemented for {source.Type}");
        }
    }

    /// <summary>
    /// Discovers the schema (columns) of a specific table.
    /// </summary>
    public async Task<List<DatasetColumn>> DiscoverSchemaAsync(DataSource source, string tableName)
    {
        _logger.LogInformation(
            "Discovering schema for {Table} in {Type} source: {Name}",
            tableName,
            source.Type,
            source.Name
        );

        switch (source.Type)
        {
            case DataSourceType.PostgreSQL:
                return await DiscoverPostgreSqlSchemaAsync(source, tableName);
            
            case DataSourceType.MySQL:
                return await DiscoverMySqlSchemaAsync(source, tableName);
            
            case DataSourceType.SQLServer:
                return await DiscoverSqlServerSchemaAsync(source, tableName);
            
            case DataSourceType.MongoDB:
                return await DiscoverMongoDbSchemaAsync(source, tableName);
            
            default:
                throw new NotImplementedException($"Schema discovery not implemented for {source.Type}");
        }
    }

    /// <summary>
    /// Gets an open database connection.
    /// </summary>
    public async Task<IDbConnection> GetConnectionAsync(DataSource source)
    {
        IDbConnection connection = source.Type switch
        {
            DataSourceType.PostgreSQL => new NpgsqlConnection(source.ConnectionString),
            DataSourceType.MySQL => new MySqlConnection(source.ConnectionString),
            DataSourceType.SQLServer => new SqlConnection(source.ConnectionString),
            _ => throw new NotImplementedException($"Connection not implemented for {source.Type}")
        };

        await connection.OpenAsync();
        return connection;
    }

    // ============================================================================
    // PostgreSQL Implementation
    // ============================================================================

    private async Task<ConnectionTestResult> TestPostgreSqlAsync(DataSource source)
    {
        await using var conn = new NpgsqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var version = await conn.ExecuteScalarAsync<string>("SELECT version()");

        return new ConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            Details = version
        };
    }

    private async Task<List<string>> DiscoverPostgreSqlTablesAsync(DataSource source)
    {
        await using var conn = new NpgsqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        const string query = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
              AND table_type = 'BASE TABLE'
            ORDER BY table_name";

        var tables = new List<string>();
        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<List<DatasetColumn>> DiscoverPostgreSqlSchemaAsync(DataSource source, string tableName)
    {
        await using var conn = new NpgsqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        const string query = @"
            SELECT 
                column_name,
                data_type,
                is_nullable,
                column_default,
                (SELECT COUNT(*) > 0 
                 FROM information_schema.key_column_usage kcu
                 JOIN information_schema.table_constraints tc 
                   ON kcu.constraint_name = tc.constraint_name
                 WHERE tc.constraint_type = 'PRIMARY KEY'
                   AND kcu.table_name = c.table_name
                   AND kcu.column_name = c.column_name) as is_primary_key
            FROM information_schema.columns c
            WHERE table_name = @tableName
              AND table_schema = 'public'
            ORDER BY ordinal_position";

        var columns = new List<DatasetColumn>();
        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("tableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(new DatasetColumn
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES",
                IsPrimaryKey = reader.GetBoolean(4),
                Description = null
            });
        }

        return columns;
    }

    // ============================================================================
    // MySQL Implementation
    // ============================================================================

    private async Task<ConnectionTestResult> TestMySqlAsync(DataSource source)
    {
        await using var conn = new MySqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var version = await conn.ExecuteScalarAsync<string>("SELECT VERSION()");

        return new ConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            Details = version
        };
    }

    private async Task<List<string>> DiscoverMySqlTablesAsync(DataSource source)
    {
        await using var conn = new MySqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        const string query = "SHOW TABLES";

        var tables = new List<string>();
        await using var cmd = new MySqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<List<DatasetColumn>> DiscoverMySqlSchemaAsync(DataSource source, string tableName)
    {
        await using var conn = new MySqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        const string query = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                COLUMN_KEY
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName
              AND TABLE_SCHEMA = DATABASE()
            ORDER BY ORDINAL_POSITION";

        var columns = new List<DatasetColumn>();
        await using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(new DatasetColumn
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES",
                IsPrimaryKey = reader.GetString(3) == "PRI",
                Description = null
            });
        }

        return columns;
    }

    // ============================================================================
    // SQL Server Implementation
    // ============================================================================

    private async Task<ConnectionTestResult> TestSqlServerAsync(DataSource source)
    {
        await using var conn = new SqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        var version = await conn.ExecuteScalarAsync<string>("SELECT @@VERSION");

        return new ConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            Details = version
        };
    }

    private async Task<List<string>> DiscoverSqlServerTablesAsync(DataSource source)
    {
        await using var conn = new SqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        const string query = @"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME";

        var tables = new List<string>();
        await using var cmd = new SqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task<List<DatasetColumn>> DiscoverSqlServerSchemaAsync(DataSource source, string tableName)
    {
        await using var conn = new SqlConnection(source.ConnectionString);
        await conn.OpenAsync();

        const string query = @"
            SELECT 
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_PRIMARY_KEY
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
                  ON ku.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON c.TABLE_NAME = pk.TABLE_NAME 
                AND c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_NAME = @tableName
            ORDER BY c.ORDINAL_POSITION";

        var columns = new List<DatasetColumn>();
        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(new DatasetColumn
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES",
                IsPrimaryKey = reader.GetInt32(3) == 1,
                Description = null
            });
        }

        return columns;
    }

    // ============================================================================
    // MongoDB Implementation
    // ============================================================================

    private async Task<ConnectionTestResult> TestMongoDbAsync(DataSource source)
    {
        var client = new MongoClient(source.ConnectionString);
        var database = client.GetDatabase(GetMongoDbName(source));

        // Try to list collections to verify connection
        await database.ListCollectionNamesAsync();

        return new ConnectionTestResult
        {
            Success = true,
            Message = "Connection successful",
            Details = "MongoDB connection verified"
        };
    }

    private async Task<List<string>> DiscoverMongoDbCollectionsAsync(DataSource source)
    {
        var client = new MongoClient(source.ConnectionString);
        var database = client.GetDatabase(GetMongoDbName(source));

        var collections = await database.ListCollectionNamesAsync();
        return await collections.ToListAsync();
    }

    private async Task<List<DatasetColumn>> DiscoverMongoDbSchemaAsync(DataSource source, string collectionName)
    {
        var client = new MongoClient(source.ConnectionString);
        var database = client.GetDatabase(GetMongoDbName(source));
        var collection = database.GetCollection<BsonDocument>(collectionName);

        // Sample first document to infer schema
        var sample = await collection.Find(new BsonDocument()).Limit(1).FirstOrDefaultAsync();

        if (sample == null)
        {
            return new List<DatasetColumn>();
        }

        var columns = new List<DatasetColumn>();

        foreach (var element in sample.Elements)
        {
            columns.Add(new DatasetColumn
            {
                Name = element.Name,
                DataType = element.Value.BsonType.ToString(),
                IsNullable = true,
                IsPrimaryKey = element.Name == "_id",
                Description = null
            });
        }

        return columns;
    }

    private string GetMongoDbName(DataSource source)
    {
        // Extract database name from connection string or configuration
        if (source.Configuration.TryGetValue("Database", out var dbName))
        {
            return dbName;
        }

        // Parse from connection string
        var uri = new MongoUrl(source.ConnectionString);
        return uri.DatabaseName ?? "test";
    }
}