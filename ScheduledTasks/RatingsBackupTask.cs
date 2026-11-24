using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.ScheduledTasks;

public class RatingsBackupTask(
BackupService backupService,
ILogger<RatingsBackupTask> logger) : IScheduledTask
{
public string Name => Localizer.Get("taskBackupName");

public string Key => "RatingsBackup";

public string Description => Localizer.Get("taskBackupDescription");

public string Category => Localizer.Get("taskCategory");

public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
{
    var config = Plugin.Instance?.Configuration as PluginConfiguration;

    if (config == null || !config.EnableAutoBackup)
    {
        logger.LogInformation("Ratings auto-backup is disabled, skipping");
        progress.Report(100);
        return;
    }

    logger.LogInformation("Starting scheduled ratings database backup");

    try
    {
        progress.Report(10);
        var (success, backupPath, totalBackups) = backupService.CreateBackup();
        progress.Report(100);

        if (success)
        {
            logger.LogInformation("Ratings backup complete: {BackupPath} ({TotalBackups} backups retained)", backupPath, totalBackups);
        }
        else
        {
            logger.LogWarning("Ratings backup failed");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during scheduled ratings backup");
        progress.Report(100);
    }

    await Task.CompletedTask.ConfigureAwait(false);
}

public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
{
    var config = Plugin.Instance?.Configuration as PluginConfiguration;
    var intervalHours = config?.BackupIntervalHours ?? 24;

    return new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(intervalHours).Ticks
        }
    };
}
}
