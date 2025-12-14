using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/datasets")]
public class DatasetsController : ControllerBase
{
    private readonly ApplicationContext _db;
    private readonly IDataSourceConnectionService _connectionService;
    private readonly IQueryExecutionService _queryService;
    private readonly ILogger<DatasetsController> _logger;

    public DatasetsController(
        ApplicationContext db,
        IDataSourceConnectionService connectionService,
        IQueryExecutionService queryService,
        ILogger<DatasetsController> logger)
    {
        _db = db;
        _connectionService = connectionService;
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Dataset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] Guid? dataSourceId = null)
    {
        var query = _db.Datasets.Include(d => d.DataSource).AsQueryable();

        if (dataSourceId.HasValue)
        {
            query = query.Where(d => d.DataSourceId == dataSourceId.Value);
        }

        var datasets = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

        return Ok(datasets);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Dataset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var dataset = await _db.Datasets
            .Include(d => d.DataSource)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dataset == null)
        {
            return NotFound();
        }

        return Ok(dataset);
    }

    /// <summary>
    /// UPDATED: Now actually discovers the schema from the source!
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Dataset), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDatasetRequest request)
    {
        var source = await _db.DataSources.FindAsync(request.DataSourceId);
        if (source == null)
        {
            return BadRequest("Data source not found.");
        }

        try
        {
            // Discover actual schema from the data source
            var schema = await _connectionService.DiscoverSchemaAsync(source, request.TableName);

            var dataset = new Dataset
            {
                DataSourceId = request.DataSourceId,
                Name = request.Name,
                Description = request.Description,
                TableName = request.TableName,
                Schema = schema,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "unknown"
            };

            _db.Datasets.Add(dataset);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Dataset created: {Id} with {Columns} columns", 
                dataset.Id, schema.Count);

            return CreatedAtAction(nameof(Get), new { id = dataset.Id }, dataset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create dataset");
            return StatusCode(500, new { message = "Failed to create dataset", error = ex.Message });
        }
    }

    /// <summary>
    /// UPDATED: Now returns actual data from the source!
    /// </summary>
    [HttpGet("{id}/preview")]
    [ProducesResponseType(typeof(DatasetPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(Guid id, [FromQuery] int limit = 100)
    {
        var dataset = await _db.Datasets
            .Include(d => d.DataSource)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dataset == null)
        {
            return NotFound();
        }

        try
        {
            var preview = await _queryService.GetPreviewAsync(dataset, limit);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview for dataset: {Id}", id);
            return StatusCode(500, new { message = "Failed to generate preview", error = ex.Message });
        }
    }

    /// <summary>
    /// UPDATED: Now syncs real metadata from the source!
    /// </summary>
    [HttpPost("{id}/sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sync(Guid id)
    {
        var dataset = await _db.Datasets
            .Include(d => d.DataSource)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dataset == null)
        {
            return NotFound();
        }

        try
        {
            // Refresh schema
            dataset.Schema = await _connectionService.DiscoverSchemaAsync(
                dataset.DataSource, 
                dataset.TableName
            );

            // Get row count
            dataset.RowCount = await _queryService.GetRowCountAsync(dataset);
            dataset.LastSyncedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Dataset synced: {Id}, rows: {Rows}",
                id,
                dataset.RowCount
            );

            return Ok(new 
            { 
                success = true, 
                lastSyncedAt = dataset.LastSyncedAt,
                rowCount = dataset.RowCount,
                columns = dataset.Schema.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync dataset: {Id}", id);
            return StatusCode(500, new { message = "Sync failed", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var dataset = await _db.Datasets.FindAsync(id);

        if (dataset == null)
        {
            return NotFound();
        }

        _db.Datasets.Remove(dataset);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}