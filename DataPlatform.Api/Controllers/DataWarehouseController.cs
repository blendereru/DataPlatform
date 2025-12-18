using DataPlatform.Api.Data;
using DataPlatform.Api.DTOs;
using DataPlatform.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Controllers;

/// <summary>
/// Manages data warehouse layers and data lineage visualization.
/// </summary>
[Authorize]
[ApiController]
[Route("api/warehouse")]
public class DataWarehouseController : ControllerBase
{
    private readonly ApplicationContext _db;
    private readonly ILogger<DataWarehouseController> _logger;

    public DataWarehouseController(ApplicationContext db, ILogger<DataWarehouseController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Gets all datasets grouped by warehouse layer.
    /// </summary>
    [HttpGet("layers")]
    [ProducesResponseType(typeof(Dictionary<string, List<Dataset>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLayerStructure()
    {
        _logger.LogInformation("Fetching warehouse layer structure");

        var datasets = await _db.Datasets
            .Include(d => d.DataSource)
            .OrderBy(d => d.Layer)
            .ThenBy(d => d.Name)
            .ToListAsync();

        var layerGroups = datasets
            .GroupBy(d => d.Layer.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        return Ok(layerGroups);
    }

    /// <summary>
    /// Gets datasets in a specific layer.
    /// </summary>
    [HttpGet("layers/{layer}")]
    [ProducesResponseType(typeof(List<Dataset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatasetsByLayer(DataWarehouseLayer layer)
    {
        var datasets = await _db.Datasets
            .Include(d => d.DataSource)
            .Where(d => d.Layer == layer)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(datasets);
    }

    /// <summary>
    /// Gets complete lineage (upstream and downstream) for a dataset.
    /// </summary>
    [HttpGet("lineage/{datasetId}")]
    [ProducesResponseType(typeof(DatasetWithLineageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLineage(Guid datasetId)
    {
        var dataset = await _db.Datasets.FindAsync(datasetId);
        
        if (dataset == null)
        {
            return NotFound();
        }

        // Get upstream sources (what feeds this dataset)
        var upstreamLineage = await _db.DataLineages
            .Include(l => l.SourceDataset)
            .Include(l => l.Pipeline)
            .Where(l => l.TargetDatasetId == datasetId)
            .ToListAsync();

        // Get downstream targets (what this dataset feeds)
        var downstreamLineage = await _db.DataLineages
            .Include(l => l.TargetDataset)
            .Include(l => l.Pipeline)
            .Where(l => l.SourceDatasetId == datasetId)
            .ToListAsync();

        var response = new DatasetWithLineageResponse
        {
            Id = dataset.Id,
            Name = dataset.Name,
            Layer = dataset.Layer,
            TableType = dataset.TableType,
            UpstreamSources = upstreamLineage.Select(l => new LineageConnection
            {
                DatasetId = l.SourceDatasetId,
                DatasetName = l.SourceDataset.Name,
                Layer = l.SourceDataset.Layer,
                PipelineName = l.Pipeline?.Name,
                TransformationDescription = l.TransformationDescription
            }).ToList(),
            DownstreamTargets = downstreamLineage.Select(l => new LineageConnection
            {
                DatasetId = l.TargetDatasetId,
                DatasetName = l.TargetDataset.Name,
                Layer = l.TargetDataset.Layer,
                PipelineName = l.Pipeline?.Name,
                TransformationDescription = l.TransformationDescription
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a lineage relationship between datasets.
    /// </summary>
    [HttpPost("lineage")]
    [ProducesResponseType(typeof(DataLineage), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLineage([FromBody] CreateLineageRequest request)
    {
        var source = await _db.Datasets.FindAsync(request.SourceDatasetId);
        var target = await _db.Datasets.FindAsync(request.TargetDatasetId);

        if (source == null || target == null)
        {
            return BadRequest("Source or target dataset not found.");
        }

        // Validate layer progression (can't go backwards)
        if (target.Layer < source.Layer)
        {
            return BadRequest($"Invalid layer progression: cannot move from {source.Layer} to {target.Layer}");
        }

        var lineage = new DataLineage
        {
            SourceDatasetId = request.SourceDatasetId,
            TargetDatasetId = request.TargetDatasetId,
            PipelineId = request.PipelineId,
            TransformationDescription = request.TransformationDescription,
            CreatedAt = DateTime.UtcNow
        };

        _db.DataLineages.Add(lineage);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLineage), new { datasetId = target.Id }, lineage);
    }

    /// <summary>
    /// Gets warehouse architecture visualization data.
    /// </summary>
    [HttpGet("visualization")]
    [ProducesResponseType(typeof(WarehouseVisualization), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisualization()
    {
        var datasets = await _db.Datasets
            .Include(d => d.DataSource)
            .ToListAsync();

        var lineages = await _db.DataLineages
            .Include(l => l.Pipeline)
            .ToListAsync();

        var visualization = new WarehouseVisualization
        {
            Nodes = datasets.Select(d => new VisualizationNode
            {
                Id = d.Id.ToString(),
                Name = d.Name,
                Layer = d.Layer.ToString(),
                TableType = d.TableType.ToString(),
                RowCount = d.RowCount
            }).ToList(),
            Edges = lineages.Select(l => new VisualizationEdge
            {
                Source = l.SourceDatasetId.ToString(),
                Target = l.TargetDatasetId.ToString(),
                PipelineName = l.Pipeline?.Name,
                Description = l.TransformationDescription
            }).ToList()
        };

        return Ok(visualization);
    }
}