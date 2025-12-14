using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/queries")]
public class QueriesController : ControllerBase
{
    private readonly ApplicationContext _db;
    private readonly IQueryExecutionService _queryService;
    private readonly ILogger<QueriesController> _logger;

    public QueriesController(
        ApplicationContext db,
        IQueryExecutionService queryService,
        ILogger<QueriesController> logger)
    {
        _db = db;
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Query>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int limit = 50)
    {
        var queries = await _db.Queries
            .Where(q => q.CreatedBy == User.Identity!.Name)
            .Include(q => q.Dataset)
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(queries);
    }

    /// <summary>
    /// UPDATED: Now actually executes SQL queries!
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(QueryResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Execute([FromBody] ExecuteQueryRequest request)
    {
        var dataset = await _db.Datasets
            .Include(d => d.DataSource)
            .FirstOrDefaultAsync(d => d.Id == request.DatasetId);

        if (dataset == null)
        {
            return BadRequest("Dataset not found.");
        }

        if (string.IsNullOrWhiteSpace(request.SqlQuery))
        {
            return BadRequest("SQL query is required.");
        }

        var query = new Query
        {
            Name = request.Name,
            SqlQuery = request.SqlQuery,
            DatasetId = request.DatasetId,
            Status = QueryStatus.Running,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "unknown"
        };

        _db.Queries.Add(query);
        await _db.SaveChangesAsync();

        try
        {
            var result = await _queryService.ExecuteQueryAsync(dataset, request.SqlQuery);

            query.Status = QueryStatus.Completed;
            query.ExecutedAt = DateTime.UtcNow;
            query.RowsReturned = result.TotalRows;
            query.ExecutionTimeMs = result.ExecutionTimeMs;

            result.QueryId = query.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Query executed: {QueryId}, rows: {Rows}, time: {Time}ms",
                query.Id,
                result.TotalRows,
                result.ExecutionTimeMs
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed: {QueryId}", query.Id);

            query.Status = QueryStatus.Failed;
            query.ExecutedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return StatusCode(500, new { message = "Query execution failed", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Query), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var query = await _db.Queries
            .Include(q => q.Dataset)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (query == null)
        {
            return NotFound();
        }

        return Ok(query);
    }
}