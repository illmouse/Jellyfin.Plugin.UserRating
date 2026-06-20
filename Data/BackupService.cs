using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Models;
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

        public string GetBackupDir()
        {
            var config = Plugin.Instance?.Configuration as PluginConfiguration;
            return !string.IsNullOrWhiteSpace(config?.BackupPath)
                ? config.BackupPath
                : GetDefaultBackupPath();
        }

        public (bool success, string backupPath, int totalBackups) CreateBackup()
        {
            var config = Plugin.Instance?.Configuration as PluginConfiguration;
            var backupDir = GetBackupDir();

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

        public List<BackupFileInfo> ListBackups()
        {
            var backupDir = GetBackupDir();
            var result = new List<BackupFileInfo>();

            try
            {
                if (!Directory.Exists(backupDir))
                {
                    return result;
                }

                var files = Directory.GetFiles(backupDir, "*.json")
                    .OrderByDescending(f => f);

                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    result.Add(new BackupFileInfo
                    {
                        FileName = Path.GetFileName(file),
                        FileSize = fi.Length,
                        LastModified = fi.LastWriteTimeUtc
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list backups from {BackupDir}", backupDir);
            }

            return result;
        }

        public string? GetBackupFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return null;
            }

            var backupDir = GetBackupDir();
            var fullPath = Path.Combine(backupDir, fileName);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            var fullDir = Path.GetFullPath(Path.GetDirectoryName(fullPath)!);
            var expectedDir = Path.GetFullPath(backupDir);
            if (!fullDir.StartsWith(expectedDir, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }

        public (bool success, string message) RestoreBackup(string fileName)
        {
            var fullPath = GetBackupFilePath(fileName);
            if (fullPath == null)
            {
                return (false, $"Backup file '{fileName}' not found.");
            }

            try
            {
                var dataPath = _repository.GetDataPath();

                if (File.Exists(dataPath))
                {
                    var restoreBackupDir = GetBackupDir();
                    Directory.CreateDirectory(restoreBackupDir);
                    var preRestoreName = $"pre_restore_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    var preRestorePath = Path.Combine(restoreBackupDir, preRestoreName);
                    File.Copy(dataPath, preRestorePath);
                    _logger.LogInformation("Pre-restore backup saved to {Path}", preRestorePath);
                }

                File.Copy(fullPath, _repository.GetDataPath(), overwrite: true);
                _repository.Reload();

                _logger.LogInformation("Ratings restored from backup {FileName}", fileName);
                return (true, $"Successfully restored from {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore backup {FileName}", fileName);
                return (false, $"Restore failed: {ex.Message}");
            }
        }

        public (bool success, string message) DeleteBackup(string fileName)
        {
            var fullPath = GetBackupFilePath(fileName);
            if (fullPath == null)
            {
                return (false, $"Backup file '{fileName}' not found.");
            }

            try
            {
                File.Delete(fullPath);
                _logger.LogInformation("Backup file deleted: {FileName}", fileName);
                return (true, $"Backup '{fileName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup {FileName}", fileName);
                return (false, $"Delete failed: {ex.Message}");
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