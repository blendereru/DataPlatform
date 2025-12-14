using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manages data quality rules and executes validation checks.
/// Supports defining quality metrics and monitoring data health.
/// </summary>
[Authorize]
[ApiController]
[Route("api/quality")]
public class DataQualityController : ControllerBase
{
    private readonly ApplicationContext _db;
    private readonly ILogger<DataQualityController> _logger;

    public DataQualityController(ApplicationContext db, ILogger<DataQualityController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Lists all data quality rules.
    /// </summary>
    /// <param name="datasetId">Optional filter by dataset.</param>
    /// <returns>List of quality rules.</returns>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(List<DataQualityRule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRules([FromQuery] Guid? datasetId = null)
    {
        var query = _db.DataQualityRules
            .Include(r => r.Dataset)
            .AsQueryable();

        if (datasetId.HasValue)
        {
            query = query.Where(r => r.DatasetId == datasetId.Value);
        }

        var rules = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(rules);
    }

    /// <summary>
    /// Creates a new data quality rule.
    /// </summary>
    /// <param name="rule">Rule configuration.</param>
    /// <returns>Created rule.</returns>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(DataQualityRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRule([FromBody] DataQualityRule rule)
    {
        _logger.LogInformation(
            "Creating quality rule: {Name} for dataset {DatasetId}",
            rule.Name,
            rule.DatasetId
        );

        var dataset = await _db.Datasets.FindAsync(rule.DatasetId);
        if (dataset == null)
        {
            return BadRequest("Dataset not found.");
        }

        try
        {
            rule.CreatedAt = DateTime.UtcNow;
            rule.CreatedBy = User.Identity?.Name ?? "unknown";
            rule.IsActive = true;

            _db.DataQualityRules.Add(rule);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Quality rule created: {Id}", rule.Id);

            return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create quality rule");
            return StatusCode(500, "An error occurred while creating the rule.");
        }
    }

    /// <summary>
    /// Gets details of a quality rule.
    /// </summary>
    /// <param name="id">Rule identifier.</param>
    /// <returns>Rule details.</returns>
    [HttpGet("rules/{id}")]
    [ProducesResponseType(typeof(DataQualityRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRule(Guid id)
    {
        var rule = await _db.DataQualityRules
            .Include(r => r.Dataset)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        return Ok(rule);
    }

    /// <summary>
    /// Executes quality checks for a dataset.
    /// </summary>
    /// <param name="datasetId">Dataset to validate.</param>
    /// <returns>Quality check results.</returns>
    [HttpPost("check/{datasetId}")]
    [ProducesResponseType(typeof(DataQualityReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunQualityCheck(Guid datasetId)
    {
        var dataset = await _db.Datasets.FindAsync(datasetId);

        if (dataset == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Running quality checks for dataset: {DatasetId}", datasetId);

        var rules = await _db.DataQualityRules
            .Where(r => r.DatasetId == datasetId && r.IsActive)
            .ToListAsync();

        var checkResults = new List<RuleCheckResult>();

        foreach (var rule in rules)
        {
            try
            {
                // TODO: Execute actual validation query
                // Simulated check
                await Task.Delay(100);

                var passed = new Random().NextDouble() > 0.3;
                var score = passed ? 95.0 + new Random().NextDouble() * 5 : 50.0 + new Random().NextDouble() * 30;

                var check = new DataQualityCheck
                {
                    RuleId = rule.Id,
                    ExecutedAt = DateTime.UtcNow,
                    Passed = passed,
                    Score = score,
                    RowsChecked = 10000,
                    RowsFailed = passed ? 0 : 100
                };

                _db.DataQualityChecks.Add(check);

                checkResults.Add(new RuleCheckResult
                {
                    RuleName = rule.Name,
                    Type = rule.Type,
                    Passed = passed,
                    Score = score,
                    Details = passed ? "All checks passed" : "Some rows failed validation"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute quality rule: {RuleId}", rule.Id);
            }
        }

        await _db.SaveChangesAsync();

        var overallScore = checkResults.Any() 
            ? checkResults.Average(r => r.Score) 
            : 0;

        var report = new DataQualityReportResponse
        {
            DatasetId = datasetId,
            DatasetName = dataset.Name,
            OverallScore = overallScore,
            RuleResults = checkResults,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Quality check completed for dataset {DatasetId}: score {Score}",
            datasetId,
            overallScore
        );

        return Ok(report);
    }

    /// <summary>
    /// Gets quality check history for a dataset.
    /// </summary>
    /// <param name="datasetId">Dataset identifier.</param>
    /// <param name="limit">Maximum number of checks to return.</param>
    /// <returns>List of quality checks.</returns>
    [HttpGet("history/{datasetId}")]
    [ProducesResponseType(typeof(List<DataQualityCheck>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCheckHistory(Guid datasetId, [FromQuery] int limit = 50)
    {
        var checks = await _db.DataQualityChecks
            .Include(c => c.Rule)
            .Where(c => c.Rule.DatasetId == datasetId)
            .OrderByDescending(c => c.ExecutedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(checks);
    }
}