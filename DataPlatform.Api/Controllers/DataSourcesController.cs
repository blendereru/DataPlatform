using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/datasources")]
public class DataSourcesController : ControllerBase
{
    private readonly ApplicationContext _db;
    private readonly IDataSourceConnectionService _connectionService;
    private readonly ILogger<DataSourcesController> _logger;

    public DataSourcesController(
        ApplicationContext db,
        IDataSourceConnectionService connectionService,
        ILogger<DataSourcesController> logger)
    {
        _db = db;
        _connectionService = connectionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DataSource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var sources = await _db.DataSources
            .OrderByDescending(ds => ds.CreatedAt)
            .ToListAsync();

        return Ok(sources);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DataSource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var source = await _db.DataSources.FindAsync(id);

        if (source == null)
        {
            return NotFound();
        }

        return Ok(source);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DataSource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDataSourceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest("Name and connection string are required.");
        }

        var source = new DataSource
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            ConnectionString = request.ConnectionString,
            Configuration = request.Configuration ?? new(),
            Status = DataSourceStatus.Testing,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "unknown"
        };

        _db.DataSources.Add(source);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Data source created: {Id}", source.Id);

        return CreatedAtAction(nameof(Get), new { id = source.Id }, source);
    }

    /// <summary>
    /// UPDATED: Now actually tests the connection!
    /// </summary>
    [HttpPost("{id}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        var source = await _db.DataSources.FindAsync(id);

        if (source == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Testing connection for data source: {Id}", id);

        var result = await _connectionService.TestConnectionAsync(source);

        source.Status = result.Success ? DataSourceStatus.Active : DataSourceStatus.Failed;
        source.LastTestedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(result);
    }

    /// <summary>
    /// NEW: Discovers available tables in the data source
    /// </summary>
    [HttpGet("{id}/tables")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DiscoverTables(Guid id)
    {
        var source = await _db.DataSources.FindAsync(id);

        if (source == null)
        {
            return NotFound();
        }

        try
        {
            var tables = await _connectionService.DiscoverTablesAsync(source);
            return Ok(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover tables for source: {Id}", id);
            return StatusCode(500, new { message = "Failed to discover tables", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var source = await _db.DataSources.FindAsync(id);

        if (source == null)
        {
            return NotFound();
        }

        _db.DataSources.Remove(source);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}