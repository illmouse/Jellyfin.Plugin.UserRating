using System;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Data
{
    public class BackupService
    {
        private readonly RatingRepository _repository;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<BackupService> _logger;

        public BackupService(
            RatingRepository repository,
            IApplicationPaths appPaths,
            ILogger<BackupService> logger)
        {
            _repository = repository;
            _appPaths = appPaths;
            _logger = logger;
        }

        private string GetDefaultBackupPath()
        {
            return Path.Combine(_appPaths.DataPath, "backups", "UserRatings");
        }

        public (bool success, string backupPath, int totalBackups) CreateBackup()
        {
            var config = Plugin.Instance?.Configuration as PluginConfiguration;
            var backupDir = !string.IsNullOrWhiteSpace(config?.BackupPath)
                ? config.BackupPath
                : GetDefaultBackupPath();

            try
            {
                Directory.CreateDirectory(backupDir);

                var dataPath = _repository.GetDataPath();
                if (!File.Exists(dataPath))
                {
                    _logger.LogWarning("No ratings file found at {Path} to backup", dataPath);
                    return (false, string.Empty, 0);
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"ratings_{timestamp}.json";
                var backupPath = Path.Combine(backupDir, backupFileName);

                File.Copy(dataPath, backupPath);
                _logger.LogInformation("Ratings backup created at {BackupPath}", backupPath);

                var maxBackups = config?.MaxBackups ?? 7;
                RotateBackups(backupDir, maxBackups);

                var totalBackups = Directory.GetFiles(backupDir, "ratings_*.json").Length;
                return (true, backupPath, totalBackups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ratings backup");
                return (false, string.Empty, 0);
            }
        }

        private void RotateBackups(string backupDir, int maxBackups)
        {
            try
            {
                var backups = Directory.GetFiles(backupDir, "ratings_*.json")
                    .OrderByDescending(f => f)
                    .ToList();

                if (backups.Count <= maxBackups) return;

                var toDelete = backups.Skip(maxBackups).ToList();
                foreach (var file in toDelete)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation("Deleted old backup {FilePath}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old backup {FilePath}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to rotate backups in {BackupDir}", backupDir);
            }
        }
    }
}