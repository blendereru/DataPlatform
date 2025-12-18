using DataPlatform.Api.Data;
using DataPlatform.Api.Models;
using DataPlatform.Api.Models.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Services;

/// <summary>
/// Hangfire-based pipeline scheduler for recurring jobs.
/// </summary>
public class PipelineSchedulerService
{
    private readonly ApplicationContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<PipelineSchedulerService> _logger;

    public PipelineSchedulerService(
        ApplicationContext db,
        IPublishEndpoint bus,
        ILogger<PipelineSchedulerService> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>
    /// Checks for pipelines that need to run based on their schedule.
    /// Called by Hangfire on a recurring basis (e.g., every minute).
    /// </summary>
    public async Task CheckScheduledPipelinesAsync()
    {
        _logger.LogInformation("Checking for scheduled pipelines");

        var activePipelines = await _db.Pipelines
            .Where(p => p.Status == PipelineStatus.Active && !string.IsNullOrEmpty(p.Schedule))
            .ToListAsync();

        foreach (var pipeline in activePipelines)
        {
            try
            {
                if (ShouldRunNow(pipeline))
                {
                    _logger.LogInformation(
                        "Triggering scheduled pipeline: {PipelineId} ({Name})",
                        pipeline.Id,
                        pipeline.Name
                    );

                    var run = new PipelineRun
                    {
                        PipelineId = pipeline.Id,
                        StartedAt = DateTime.UtcNow,
                        Status = PipelineRunStatus.Running,
                        RowsProcessed = 0,
                        RowsFailed = 0
                    };

                    _db.PipelineRuns.Add(run);
                    await _db.SaveChangesAsync();

                    await _bus.Publish(new PipelineExecutionMessage
                    {
                        PipelineId = pipeline.Id,
                        RunId = run.Id,
                        TriggeredBy = "scheduler"
                    });

                    pipeline.LastRunAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to schedule pipeline: {PipelineId}",
                    pipeline.Id
                );
            }
        }
    }

    /// <summary>
    /// Determines if a pipeline should run based on its schedule and last run time.
    /// Simplified cron evaluation - in production use a proper cron library.
    /// </summary>
    private bool ShouldRunNow(Pipeline pipeline)
    {
        if (string.IsNullOrWhiteSpace(pipeline.Schedule))
            return false;

        // For demo purposes, simple schedule format: "hourly", "daily", "weekly"
        // In production, use Cronos or NCrontab library for proper cron parsing

        if (!pipeline.LastRunAt.HasValue)
            return true; // Never run before

        var timeSinceLastRun = DateTime.UtcNow - pipeline.LastRunAt.Value;

        return pipeline.Schedule.ToLower() switch
        {
            "hourly" => timeSinceLastRun.TotalHours >= 1,
            "daily" => timeSinceLastRun.TotalDays >= 1,
            "weekly" => timeSinceLastRun.TotalDays >= 7,
            _ => false
        };
    }
}