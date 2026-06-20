using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Api;

[ApiController]
[Route("api/UserRatings")]
public class PlexImportController(
PlexImportService importService,
ProgressTracker progressTracker,
ILogger<PlexImportController> logger) : ControllerBase
{

    private PluginConfiguration GetConfig()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    [HttpPost("ImportFromPlex")]
    [Produces("application/json")]
    public ActionResult StartImport([FromQuery] Guid userId, [FromQuery] string? conflictMode = null)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest(new ApiResponse(false, "userId is required"));
        }

        var config = GetConfig();
        if (string.IsNullOrEmpty(config.PlexServerUrl) || string.IsNullOrEmpty(config.PlexToken))
        {
            return BadRequest(new ApiResponse(false, "Plex server URL and token must be configured in plugin settings"));
        }

        var operationId = progressTracker.StartOperation();

        _ = Task.Run(async () =>
        {
            try
            {
                await importService.ImportFromPlexAsync(userId, operationId, CancellationToken.None, conflictMode).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in Plex import task");
                progressTracker.FailOperation(operationId, $"Unhandled error: {ex.Message}");
            }
        });

        return Ok(new StartImportResponse(true, operationId));
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
            var progress = progressTracker.GetProgress(operationId);

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
                progressTracker.RemoveOperation(operationId);
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
            return Ok(new CheckPlexStatusResponse(false, "Plex server URL and token not configured", 0));
        }

        var (success, message, libraryCount) = await importService.ValidatePlexConnectionAsync(plexUrl, plexToken, HttpContext.RequestAborted).ConfigureAwait(false);

        return Ok(new CheckPlexStatusResponse(success, message, libraryCount));
    }
}