using System.Linq;
using System.Net.Mime;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Services;
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
                    staleItems = report.StaleItems.Select(s => new
                    {
                        itemId = s.ItemId,
                        userId = s.UserId,
                        rating = s.Rating,
                        note = s.Note,
                        providerIds = s.ProviderIds,
                        timestamp = s.Timestamp
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
    }
}