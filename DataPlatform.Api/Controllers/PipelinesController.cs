using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataPlatform.Api.Controllers;

/// <summary>
/// Manages ETL/ELT pipelines for data transformation and movement.
/// Supports scheduling, execution, and monitoring of data workflows.
/// </summary>
[Authorize]
[ApiController]
[Route("api/pipelines")]
public class PipelinesController : ControllerBase
{
    private readonly ApplicationContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<PipelinesController> _logger;

    public PipelinesController(
        ApplicationContext db,
        IPublishEndpoint bus,
        ILogger<PipelinesController> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Lists all pipelines.
    /// </summary>
    /// <returns>List of pipelines with their current status.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Pipeline>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        _logger.LogInformation("Fetching pipelines for user {User}", User.Identity?.Name);

        var pipelines = await _db.Pipelines
            .Include(p => p.SourceDataset)
            .Include(p => p.TargetDataset)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(pipelines);
    }

    /// <summary>
    /// Gets details of a specific pipeline.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <returns>Pipeline details including configuration.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Pipeline), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var pipeline = await _db.Pipelines
            .Include(p => p.SourceDataset)
            .Include(p => p.TargetDataset)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pipeline == null)
        {
            _logger.LogWarning("Pipeline {Id} not found", id);
            return NotFound();
        }

        return Ok(pipeline);
    }

    /// <summary>
    /// Creates a new data pipeline.
    /// </summary>
    /// <param name="request">Pipeline configuration.</param>
    /// <returns>Created pipeline.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Pipeline), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePipelineRequest request)
    {
        _logger.LogInformation("Creating pipeline: {Name}", request.Name);

        var sourceDataset = await _db.Datasets.FindAsync(request.SourceDatasetId);
        if (sourceDataset == null)
        {
            return BadRequest("Source dataset not found.");
        }

        if (request.TargetDatasetId.HasValue)
        {
            var targetDataset = await _db.Datasets.FindAsync(request.TargetDatasetId.Value);
            if (targetDataset == null)
            {
                return BadRequest("Target dataset not found.");
            }
        }

        try
        {
            var pipeline = new Pipeline
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                SourceDatasetId = request.SourceDatasetId,
                TargetDatasetId = request.TargetDatasetId,
                SourceQuery = request.SourceQuery,
                Schedule = request.Schedule ?? string.Empty,
                Status = PipelineStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "unknown"
            };

            _db.Pipelines.Add(pipeline);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Pipeline created: {Id}", pipeline.Id);

            return CreatedAtAction(nameof(Get), new { id = pipeline.Id }, pipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pipeline");
            return StatusCode(500, "An error occurred while creating the pipeline.");
        }
    }

    /// <summary>
    /// Executes a pipeline manually (triggers a run).
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <returns>Created pipeline run.</returns>
    [HttpPost("{id}/run")]
    [ProducesResponseType(typeof(PipelineRun), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Run(Guid id)
    {
        var pipeline = await _db.Pipelines.FindAsync(id);

        if (pipeline == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Triggering pipeline run: {PipelineId}", id);

        try
        {
            var run = new PipelineRun
            {
                PipelineId = id,
                StartedAt = DateTime.UtcNow,
                Status = PipelineRunStatus.Running,
                RowsProcessed = 0,
                RowsFailed = 0
            };

            _db.PipelineRuns.Add(run);
            await _db.SaveChangesAsync();

            // Publish message to background worker
            await _bus.Publish(new PipelineExecutionMessage
            {
                PipelineId = id,
                RunId = run.Id,
                TriggeredBy = User.Identity?.Name ?? "system"
            });

            pipeline.LastRunAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Pipeline run created: {RunId}", run.Id);

            return AcceptedAtAction(nameof(GetRun), new { id, runId = run.Id }, run);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger pipeline run");
            return StatusCode(500, "An error occurred while triggering the pipeline.");
        }
    }

    /// <summary>
    /// Gets execution history for a pipeline.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="limit">Maximum number of runs to return.</param>
    /// <returns>List of pipeline runs.</returns>
    [HttpGet("{id}/runs")]
    [ProducesResponseType(typeof(List<PipelineRun>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRuns(Guid id, [FromQuery] int limit = 50)
    {
        var pipeline = await _db.Pipelines.FindAsync(id);

        if (pipeline == null)
        {
            return NotFound();
        }

        var runs = await _db.PipelineRuns
            .Where(r => r.PipelineId == id)
            .OrderByDescending(r => r.StartedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(runs);
    }

    /// <summary>
    /// Gets details of a specific pipeline run.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="runId">Run identifier.</param>
    /// <returns>Pipeline run details with metrics.</returns>
    [HttpGet("{id}/runs/{runId}")]
    [ProducesResponseType(typeof(PipelineRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRun(Guid id, Guid runId)
    {
        var run = await _db.PipelineRuns
            .Include(r => r.Pipeline)
            .FirstOrDefaultAsync(r => r.Id == runId && r.PipelineId == id);

        if (run == null)
        {
            return NotFound();
        }

        return Ok(run);
    }

    /// <summary>
    /// Gets pipeline execution summary statistics.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <returns>Summary of pipeline runs.</returns>
    [HttpGet("{id}/summary")]
    [ProducesResponseType(typeof(PipelineRunSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(Guid id)
    {
        var pipeline = await _db.Pipelines.FindAsync(id);

        if (pipeline == null)
        {
            return NotFound();
        }

        var runs = await _db.PipelineRuns
            .Where(r => r.PipelineId == id)
            .ToListAsync();

        var summary = new PipelineRunSummaryResponse
        {
            PipelineId = id,
            PipelineName = pipeline.Name,
            TotalRuns = runs.Count,
            SuccessfulRuns = runs.Count(r => r.Status == PipelineRunStatus.Succeeded),
            FailedRuns = runs.Count(r => r.Status == PipelineRunStatus.Failed),
            LastRunAt = runs.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.StartedAt,
            LastRunStatus = runs.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.Status
        };

        return Ok(summary);
    }

    /// <summary>
    /// Updates pipeline configuration.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="request">Updated pipeline configuration.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePipelineRequest request)
    {
        var pipeline = await _db.Pipelines.FindAsync(id);

        if (pipeline == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Updating pipeline: {Id}", id);

        pipeline.Name = request.Name;
        pipeline.Description = request.Description;
        pipeline.SourceQuery = request.SourceQuery;
        pipeline.Schedule = request.Schedule ?? string.Empty;

        await _db.SaveChangesAsync();

        return Ok(pipeline);
    }

    /// <summary>
    /// Deletes a pipeline and its execution history.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var pipeline = await _db.Pipelines.FindAsync(id);

        if (pipeline == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Deleting pipeline: {Id}", id);

        _db.Pipelines.Remove(pipeline);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}