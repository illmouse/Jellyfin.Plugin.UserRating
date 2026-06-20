using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api
{
    [ApiController]
    [Route("api/UserRatings")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly BackupService _backupService;

        public HealthController(HealthCheckService healthCheckService, BackupService backupService)
        {
            _healthCheckService = healthCheckService;
            _backupService = backupService;
        }

        [HttpGet("HealthReport")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetHealthReport([FromQuery] bool heal = false)
        {
            try
            {
                var report = _healthCheckService.RunHealthCheck(heal);
                return Ok(new
                {
                    success = true,
                    ok = report.Ok,
                    recoverable = report.Recoverable,
                    healed = report.Healed,
                    updated = report.Updated,
                    stale = report.Stale,
                    recoverableItems = report.RecoverableItems.Select(r => new
                    {
                        oldItemId = r.OldItemId,
                        newItemId = r.NewItemId,
                        itemName = r.ItemName,
                        userId = r.UserId,
                        rating = r.Rating,
                        providerIds = r.ProviderIds
                    }),
                    staleItems = report.StaleItems.Select(s => new
                    {
                        itemId = s.ItemId,
                        userId = s.UserId,
                        rating = s.Rating,
                        note = s.Note,
                        providerIds = s.ProviderIds,
                        timestamp = s.Timestamp
                    }),
                    healedItems = report.HealedItems.Select(h => new
                    {
                        oldItemId = h.OldItemId,
                        newItemId = h.NewItemId,
                        itemName = h.ItemName,
                        userId = h.UserId,
                        rating = h.Rating
                    })
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("HealRatings")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult HealRatings()
        {
            try
            {
                var report = _healthCheckService.RunHealthCheck(heal: true);
                return Ok(new
                {
                    success = true,
                    ok = report.Ok,
                    recoverable = report.Recoverable,
                    healed = report.Healed,
                    updated = report.Updated,
                    stale = report.Stale,
                    healedItems = report.HealedItems.Select(h => new
                    {
                        oldItemId = h.OldItemId,
                        newItemId = h.NewItemId,
                        itemName = h.ItemName,
                        userId = h.UserId,
                        rating = h.Rating
                    }),
                    staleItems = report.StaleItems.Select(s => new
                    {
                        itemId = s.ItemId,
                        userId = s.UserId,
                        rating = s.Rating,
                        note = s.Note,
                        providerIds = s.ProviderIds,
                        timestamp = s.Timestamp
                    }),
                    message = $"Healed {report.Healed} ratings, updated {report.Updated} provider IDs. {report.Stale} stale entries remain."
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("ClearStale")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult ClearStale()
        {
            try
            {
                var removed = _healthCheckService.ClearStale();
                return Ok(new { success = true, message = $"Cleared {removed} stale ratings", removed });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Backup")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult CreateBackup()
        {
            try
            {
                var (success, backupPath, totalBackups) = _backupService.CreateBackup();
                if (success)
                {
                    return Ok(new { success = true, message = $"Backup created at {backupPath}", backupPath, totalBackups });
                }
                else
                {
                    return Ok(new { success = false, message = "Failed to create backup. No ratings file found." });
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Backups")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult ListBackups()
        {
            try
            {
                var backups = _backupService.ListBackups();
                return Ok(new { success = true, backups = backups.Select(b => new
                {
                    fileName = b.FileName,
                    fileSize = b.FileSize,
                    lastModified = b.LastModified
                })});
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RestoreBackup")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult RestoreBackup([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { success = false, message = "fileName is required" });
            }

            try
            {
                var (success, message) = _backupService.RestoreBackup(fileName);
                return Ok(new { success, message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("DownloadBackup")]
        public IActionResult DownloadBackup([FromQuery] string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("fileName is required");
            }

            var filePath = _backupService.GetBackupFilePath(fileName);
            if (filePath == null)
            {
                return NotFound($"Backup file '{fileName}' not found.");
            }

            var contentType = "application/json";
            return PhysicalFile(filePath, contentType, fileName);
        }

        [HttpPost("UploadBackup")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult UploadBackup(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded." });
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Only JSON files are accepted." });
            }

            try
            {
                var backupDir = _backupService.GetBackupDir();
                Directory.CreateDirectory(backupDir);

                var safeName = file.FileName.Contains("/")  || file.FileName.Contains("\\")
                    ? $"uploaded_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
                    : $"uploaded_{file.FileName}";

                var destPath = Path.Combine(backupDir, safeName);

                using (var stream = new FileStream(destPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                return Ok(new { success = true, message = $"Backup uploaded as {safeName}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}