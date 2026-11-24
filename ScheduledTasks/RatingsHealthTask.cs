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
public string Name => Localizer.Get("taskHealthName");

public string Key => "RatingsHealthCheck";

public string Description => Localizer.Get("taskHealthDescription");

public string Category => Localizer.Get("taskCategory");

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration as Configuration.PluginConfiguration;
        if (config == null || !config.EnableAutoHealthCheck)
        {
            logger.LogInformation("Ratings health check is disabled, skipping");
            progress.Report(100);
            await Task.CompletedTask.ConfigureAwait(false);
            return;
        }

        logger.LogInformation("Starting scheduled ratings database health check");

    try
    {
        progress.Report(10);

        var report = healthCheckService.RunHealthCheck(heal: true);

        progress.Report(100);

        logger.LogInformation(
            "Ratings health check complete: {Ok} ok, {Healed} healed, {Conflicts} conflicts, {Stale} stale",
            report.Ok, report.Healed, report.Conflicts, report.Stale);
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
    var config = Plugin.Instance?.Configuration as Configuration.PluginConfiguration;
    var intervalMinutes = config?.HealthCheckIntervalMinutes ?? 30;
    if (intervalMinutes < 1) intervalMinutes = 30;

    return new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks
        }
    };
}
}
