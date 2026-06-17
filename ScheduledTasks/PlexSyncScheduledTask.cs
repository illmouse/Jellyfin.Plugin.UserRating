using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.ScheduledTasks
{
    public class PlexSyncScheduledTask : IScheduledTask
    {
        private readonly PlexImportService _importService;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<PlexSyncScheduledTask> _logger;

        public PlexSyncScheduledTask(
            PlexImportService importService,
            ILibraryManager libraryManager,
            ILogger<PlexSyncScheduledTask> logger)
        {
            _importService = importService;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public string Name => "Import Plex Ratings";

        public string Key => "PlexRatingsImport";

        public string Description => "Automatically imports ratings from a configured Plex Media Server.";

        public string Category => "User Ratings";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance?.Configuration as PluginConfiguration;

            if (config == null || !config.EnableAutoSync)
            {
                _logger.LogInformation("Plex auto-sync is disabled, skipping");
                progress.Report(100);
                return;
            }

            if (string.IsNullOrEmpty(config.PlexServerUrl) || string.IsNullOrEmpty(config.PlexToken))
            {
                _logger.LogWarning("Plex server URL or token not configured, skipping auto-sync");
                progress.Report(100);
                return;
            }

            var userIdStr = config.SyncUserId;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogWarning("Sync user ID not configured or invalid, skipping auto-sync");
                progress.Report(100);
                return;
            }

            _logger.LogInformation("Starting scheduled Plex rating import for user {UserId}", userId);

            var operationId = Guid.NewGuid().ToString("N");

            try
            {
                progress.Report(5);

                var result = await _importService.ImportFromPlexAsync(userId, operationId, cancellationToken).ConfigureAwait(false);

                progress.Report(100);

                if (result.Success)
                {
                    _logger.LogInformation("Plex auto-sync completed: {Imported} imported, {Skipped} skipped, {Unmatched} unmatched", result.Imported, result.Skipped, result.Unmatched);
                }
                else
                {
                    _logger.LogWarning("Plex auto-sync failed: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled Plex rating import");
                progress.Report(100);
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var config = Plugin.Instance?.Configuration as PluginConfiguration;
            var intervalHours = config?.SyncIntervalHours ?? 24;

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
}