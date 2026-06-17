using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Api
{
    [ApiController]
    [Route("api/UserRatings")]
    public class PlexImportController : ControllerBase
    {
        private readonly PlexImportService _importService;
        private readonly ProgressTracker _progressTracker;
        private readonly ILogger<PlexImportController> _logger;

        public PlexImportController(
            PlexImportService importService,
            ProgressTracker progressTracker,
            ILogger<PlexImportController> logger)
        {
            _importService = importService;
            _progressTracker = progressTracker;
            _logger = logger;
        }

        private PluginConfiguration GetConfig()
        {
            return Plugin.Instance?.Configuration ?? new PluginConfiguration();
        }

        [HttpPost("ImportFromPlex")]
        [Produces("application/json")]
        public ActionResult StartImport([FromQuery] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { success = false, message = "userId is required" });
            }

            var config = GetConfig();
            if (string.IsNullOrEmpty(config.PlexServerUrl) || string.IsNullOrEmpty(config.PlexToken))
            {
                return BadRequest(new { success = false, message = "Plex server URL and token must be configured in plugin settings" });
            }

            var operationId = _progressTracker.StartOperation();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _importService.ImportFromPlexAsync(userId, operationId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in Plex import task");
                    _progressTracker.FailOperation(operationId, $"Unhandled error: {ex.Message}");
                }
            });

            return Ok(new { success = true, operationId });
        }

        [HttpGet("ImportProgress/{operationId}")]
        public async Task StreamProgress(string operationId, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                var progress = _progressTracker.GetProgress(operationId);

                if (progress == null)
                {
                    var errorJson = JsonSerializer.Serialize(new { status = "not_found", message = "Operation not found" }, options);
                    await Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken).ConfigureAwait(false);
                    await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                    break;
                }

                var json = JsonSerializer.Serialize(progress, options);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

                if (progress.Status == "completed" || progress.Status == "failed" || progress.Status == "cancelled")
                {
                    _progressTracker.RemoveOperation(operationId);
                    break;
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        [HttpGet("PlexStatus")]
        [Produces("application/json")]
        public async Task<ActionResult> CheckPlexStatus()
        {
            var config = GetConfig();
            var plexUrl = config.PlexServerUrl?.TrimEnd('/') ?? string.Empty;
            var plexToken = config.PlexToken;

            if (string.IsNullOrEmpty(plexUrl) || string.IsNullOrEmpty(plexToken))
            {
                return Ok(new { success = false, message = "Plex server URL and token not configured" });
            }

            var (success, message, libraryCount) = await _importService.ValidatePlexConnectionAsync(plexUrl, plexToken, HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(new { success, message, libraryCount });
        }
    }
}