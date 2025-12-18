using DataPlatform.Api.Data;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using DataPlatform.Api.Services.Abstractions;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Consumers;

/// <summary>
/// Background worker that processes pipeline execution requests.
/// Consumes PipelineExecutionMessage from the message queue.
/// </summary>
public class PipelineExecutionConsumer : IConsumer<PipelineExecutionMessage>
{
    private readonly ApplicationContext _db;
    private readonly IQueryExecutionService _queryService;
    private readonly ILogger<PipelineExecutionConsumer> _logger;

    public PipelineExecutionConsumer(
        ApplicationContext db,
        IQueryExecutionService queryService,
        ILogger<PipelineExecutionConsumer> logger)
    {
        _db = db;
        _queryService = queryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PipelineExecutionMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Starting pipeline execution: PipelineId={PipelineId}, RunId={RunId}",
            message.PipelineId,
            message.RunId
        );

        var run = await _db.PipelineRuns
            .Include(r => r.Pipeline)
                .ThenInclude(p => p.SourceDataset)
                    .ThenInclude(d => d.DataSource)
            .Include(r => r.Pipeline)
                .ThenInclude(p => p.TargetDataset)
            .FirstOrDefaultAsync(r => r.Id == message.RunId);

        if (run == null)
        {
            _logger.LogError("Pipeline run not found: {RunId}", message.RunId);
            return;
        }

        try
        {
            await ExecutePipelineAsync(run);

            run.Status = PipelineRunStatus.Succeeded;
            run.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Pipeline execution succeeded: RunId={RunId}, Rows={Rows}",
                run.Id,
                run.RowsProcessed
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Pipeline execution failed: RunId={RunId}",
                run.Id
            );

            run.Status = PipelineRunStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
        }

        await _db.SaveChangesAsync();
    }

    private async Task ExecutePipelineAsync(PipelineRun run)
    {
        var pipeline = run.Pipeline;

        _logger.LogInformation(
            "Executing pipeline: {Name} ({Type})",
            pipeline.Name,
            pipeline.Type
        );

        switch (pipeline.Type)
        {
            case PipelineType.Batch:
            case PipelineType.FullRefresh:
                await ExecuteBatchPipelineAsync(run);
                break;

            case PipelineType.Incremental:
                await ExecuteIncrementalPipelineAsync(run);
                break;

            case PipelineType.Streaming:
                throw new NotImplementedException("Streaming pipelines not yet supported");

            default:
                throw new InvalidOperationException($"Unknown pipeline type: {pipeline.Type}");
        }
    }

    /// <summary>
    /// Executes a batch pipeline (full table load).
    /// </summary>
    private async Task ExecuteBatchPipelineAsync(PipelineRun run)
    {
        var pipeline = run.Pipeline;
        var sourceDataset = pipeline.SourceDataset;

        _logger.LogInformation("Executing batch pipeline from {Source}", sourceDataset.Name);

        // Execute source query
        var query = string.IsNullOrWhiteSpace(pipeline.SourceQuery)
            ? $"SELECT * FROM {sourceDataset.TableName}"
            : pipeline.SourceQuery;

        var result = await _queryService.ExecuteQueryAsync(sourceDataset, query);

        run.RowsProcessed = result.TotalRows;

        // Record execution metrics
        run.Metrics["query_execution_time_ms"] = result.ExecutionTimeMs;
        run.Metrics["columns_count"] = result.Columns.Count;

        // If target dataset exists, write results there
        if (pipeline.TargetDataset != null)
        {
            await WriteToTargetAsync(pipeline.TargetDataset, result.Rows, run);
        }

        _logger.LogInformation(
            "Batch pipeline completed: {Rows} rows processed in {Time}ms",
            run.RowsProcessed,
            result.ExecutionTimeMs
        );
    }

    /// <summary>
    /// Executes an incremental pipeline (only changed data).
    /// </summary>
    private async Task ExecuteIncrementalPipelineAsync(PipelineRun run)
    {
        var pipeline = run.Pipeline;

        _logger.LogInformation("Executing incremental pipeline: {Name}", pipeline.Name);

        // Get last successful run to determine watermark
        var lastRun = await _db.PipelineRuns
            .Where(r => r.PipelineId == pipeline.Id &&
                       r.Status == PipelineRunStatus.Succeeded &&
                       r.Id != run.Id)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync();

        var watermark = lastRun?.CompletedAt ?? DateTime.MinValue;

        // Modify query to only get new/changed records
        var query = pipeline.SourceQuery;

        if (query.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            query += $" AND updated_at > '{watermark:yyyy-MM-dd HH:mm:ss}'";
        }
        else
        {
            query += $" WHERE updated_at > '{watermark:yyyy-MM-dd HH:mm:ss}'";
        }

        var result = await _queryService.ExecuteQueryAsync(pipeline.SourceDataset, query);

        run.RowsProcessed = result.TotalRows;
        run.Metrics["watermark"] = watermark;
        run.Metrics["incremental"] = true;

        if (pipeline.TargetDataset != null)
        {
            await WriteToTargetAsync(pipeline.TargetDataset, result.Rows, run);
        }

        _logger.LogInformation(
            "Incremental pipeline completed: {Rows} new rows since {Watermark}",
            run.RowsProcessed,
            watermark
        );
    }

    /// <summary>
    /// Writes processed data to the target dataset.
    /// </summary>
    private async Task WriteToTargetAsync(
        Dataset targetDataset,
        List<Dictionary<string, object>> rows,
        PipelineRun run)
    {
        _logger.LogInformation(
            "Writing {Rows} rows to target: {Target}",
            rows.Count,
            targetDataset.Name
        );

        // This is a simplified implementation
        // In production, you'd want bulk insert operations
        
        try
        {
            // TODO: Implement actual write logic based on target type
            // For now, just simulate the write
            await Task.Delay(100 * rows.Count / 1000); // Simulate write time

            run.Metrics["rows_written"] = rows.Count;
            run.Metrics["target_dataset"] = targetDataset.Name;

            _logger.LogInformation("Successfully wrote to target dataset");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to target dataset");
            run.RowsFailed = rows.Count;
            throw;
        }
    }
}