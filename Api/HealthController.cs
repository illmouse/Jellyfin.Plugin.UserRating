using System.Net.Mime;
using Jellyfin.Plugin.UserRatings.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api
{
    [ApiController]
    [Route("api/UserRatings")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
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
                    healed = report.Healed,
                    stale = report.Stale,
                    staleItems = report.StaleItems.Select(s => new
                    {
                        s.ItemId,
                        s.UserId,
                        s.Rating,
                        s.Note,
                        s.ProviderIds,
                        s.Timestamp
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
                    healed = report.Healed,
                    stale = report.Stale,
                    message = $"Healed {report.Healed} ratings. {report.Stale} stale entries remain."
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
    }
}