using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.ScheduledTasks;

public class RatingsHealthTask(
HealthCheckService healthCheckService,
ILogger<RatingsHealthTask> logger) : IScheduledTask
{
public string Name => "Check Ratings Database Health";

public string Key => "RatingsHealthCheck";

public string Description => "Scans the ratings database for stale entries and heals ratings with outdated item IDs.";

public string Category => "User Ratings";

public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
{
    logger.LogInformation("Starting scheduled ratings database health check");

    try
    {
        progress.Report(10);

        var report = healthCheckService.RunHealthCheck(heal: true);

        progress.Report(100);

        logger.LogInformation(
            "Ratings health check complete: {Ok} ok, {Healed} healed, {Stale} stale",
            report.Ok, report.Healed, report.Stale);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during scheduled ratings health check");
        progress.Report(100);
    }

    await Task.CompletedTask.ConfigureAwait(false);
}

public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
{
    return new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(24).Ticks
        }
    };
}
}
