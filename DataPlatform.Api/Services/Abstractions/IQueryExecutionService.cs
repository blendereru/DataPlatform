using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;

namespace DataPlatform.Api.Services.Abstractions;

/// <summary>
/// Service for executing SQL queries against registered datasets.
/// </summary>
public interface IQueryExecutionService
{
    Task<QueryResultResponse> ExecuteQueryAsync(Dataset dataset, string sqlQuery);
    Task<DatasetPreviewResponse> GetPreviewAsync(Dataset dataset, int limit = 100);
    Task<long> GetRowCountAsync(Dataset dataset);
}