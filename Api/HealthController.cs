using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api;

[ApiController]
[Route("api/UserRatings")]
[Authorize]
public class HealthController(
    HealthCheckService healthCheckService,
    BackupService backupService) : ControllerBase
{

    [HttpGet("HealthReport")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult GetHealthReport([FromQuery] bool heal = false)
    {
        var report = healthCheckService.RunHealthCheck(heal);
        return Ok(new HealthReportResponse(
            true,
            report.Ok,
            report.Recoverable,
            report.Healed,
            report.Updated,
            report.Stale,
            report.Conflicts,
            report.RecoverableItems.Select(r => new RecoverableItemDto(
                r.OldItemId, r.NewItemId, r.ItemName, r.UserId, r.Rating, r.Note, r.Timestamp, r.ProviderIds, r.MatchType, r.MatchedProviderIds
            )).ToList(),
            report.StaleItems.Select(s => new StaleItemDto(
                s.ItemId, s.UserId, s.Rating, s.Note, s.ProviderIds, s.Timestamp
            )).ToList(),
            report.HealedItems.Select(h => new HealedItemDto(
                h.OldItemId, h.NewItemId, h.ItemName, h.UserId, h.Rating, h.Note, h.Timestamp, h.ProviderIds
            )).ToList(),
            report.ConflictItems.Select(c => new ConflictItemDto(
                c.OldItemId, c.NewItemId, c.ItemName, c.UserId,
                c.IncomingRating, c.ExistingRating, c.IncomingNote, c.ExistingNote,
                c.IncomingTimestamp, c.ExistingTimestamp, c.ConflictReason, c.ProviderIds
            )).ToList()
        ));
    }

    [HttpPost("HealRatings")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult HealRatings()
    {
        var report = healthCheckService.RunHealthCheck(heal: true);
        return Ok(new HealResponse(
            true,
            report.Ok,
            report.Recoverable,
            report.Healed,
            report.Updated,
            report.Stale,
            report.Conflicts,
            report.HealedItems.Select(h => new HealedItemDto(
                h.OldItemId, h.NewItemId, h.ItemName, h.UserId, h.Rating, h.Note, h.Timestamp, h.ProviderIds
            )).ToList(),
            report.StaleItems.Select(s => new StaleItemDto(
                s.ItemId, s.UserId, s.Rating, s.Note, s.ProviderIds, s.Timestamp
            )).ToList(),
            report.ConflictItems.Select(c => new ConflictItemDto(
                c.OldItemId, c.NewItemId, c.ItemName, c.UserId,
                c.IncomingRating, c.ExistingRating, c.IncomingNote, c.ExistingNote,
                c.IncomingTimestamp, c.ExistingTimestamp, c.ConflictReason, c.ProviderIds
            )).ToList(),
            $"Healed {report.Healed} ratings, updated {report.Updated} provider IDs. {report.Conflicts} conflicts, {report.Stale} stale entries remain."
        ));
    }

    [HttpPost("HealSingleItem")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult HealSingleItem([FromQuery] Guid oldItemId, [FromQuery] Guid newItemId, [FromQuery] Guid userId)
    {
        if (oldItemId == Guid.Empty || newItemId == Guid.Empty || userId == Guid.Empty)
        {
            return BadRequest(new ApiResponse(false, "oldItemId, newItemId, and userId are required"));
        }

        var result = healthCheckService.HealSingleItem(oldItemId, newItemId, userId);
        return result switch
        {
            HealConflictResult.Replaced => Ok(new HealSingleItemResponse(true, "Rating healed successfully", "replaced")),
            HealConflictResult.ConflictSkipped => Ok(new HealSingleItemResponse(true, "Heal skipped: conflict with existing rating", "conflict", "existing-rating-kept")),
            _ => Ok(new HealSingleItemResponse(false, "Rating not found or could not be healed", "not-found"))
        };
    }

    [HttpDelete("ClearStale")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult ClearStale()
    {
        var removed = healthCheckService.ClearStale();
        return Ok(new ClearStaleResponse(true, $"Cleared {removed} stale ratings", removed));
    }

    [HttpPost("Backup")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult CreateBackup()
    {
        var (success, backupPath, totalBackups) = backupService.CreateBackup();
        if (success)
        {
            return Ok(new CreateBackupResponse(true, $"Backup created at {backupPath}", backupPath, totalBackups));
        }

        return Ok(new CreateBackupResponse(false, "Failed to create backup. No ratings file found.", null, null));
    }

    [HttpGet("Backups")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult ListBackups()
    {
        var backups = backupService.ListBackups();
        return Ok(new BackupListResponse(
            true,
            backups.Select(b => new BackupInfoDto(b.FileName, b.FileSize, b.LastModified, b.ParsedTimestamp)).ToList()
        ));
    }

    [HttpPost("RestoreBackup")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult RestoreBackup([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new ApiResponse(false, "fileName is required"));
        }

        var (success, message) = backupService.RestoreBackup(fileName);
        return Ok(new ApiResponse(success, message));
    }

    [HttpGet("DownloadBackup")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public IActionResult DownloadBackup([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest("fileName is required");
        }

        var filePath = backupService.GetBackupFilePath(fileName);
        if (filePath == null)
        {
            return NotFound($"Backup file '{fileName}' not found.");
        }

        var contentType = "application/json";
        return PhysicalFile(filePath, contentType, fileName);
    }

    [HttpPost("UploadBackup")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult UploadBackup(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse(false, "No file uploaded."));
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiResponse(false, "Only JSON files are accepted."));
        }

        var backupDir = backupService.GetBackupDir();
        Directory.CreateDirectory(backupDir);

        var safeName = file.FileName.Contains("/") || file.FileName.Contains("\\")
            ? $"uploaded_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
            : $"uploaded_{file.FileName}";

        var destPath = Path.Combine(backupDir, safeName);

        using (var stream = new FileStream(destPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return Ok(new ApiResponse(true, $"Backup uploaded as {safeName}"));
    }

    [HttpDelete("DeleteBackup")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Policy = Policies.RequiresElevation)]
    public ActionResult DeleteBackup([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new ApiResponse(false, "fileName is required"));
        }

        var (success, message) = backupService.DeleteBackup(fileName);
        return Ok(new ApiResponse(success, message));
    }
}