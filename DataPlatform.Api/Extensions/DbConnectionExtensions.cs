using System.Data;

namespace DataPlatform.Api.Extensions;

public static class DbConnectionExtensions
{
    public static async Task<T?> ExecuteScalarAsync<T>(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await ((dynamic)cmd).ExecuteScalarAsync();
        return result is T value ? value : default;
    }

    public static async Task OpenAsync(this IDbConnection connection)
    {
        await ((dynamic)connection).OpenAsync();
    }
}