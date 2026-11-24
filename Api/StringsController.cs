using Jellyfin.Plugin.UserRatings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api;

/// <summary>
/// Serves localized UI strings. The client passes the Jellyfin UI language (?lang=ru-RU);
/// resolution is done by <see cref="Localizer"/>.
/// </summary>
[ApiController]
[Route("api/UserRatings")]
[AllowAnonymous]
public class StringsController : ControllerBase
{
    [HttpGet("Strings")]
    [Produces("application/json")]
    public ActionResult GetStrings([FromQuery] string? lang = null)
    {
        var resolved = Localizer.Resolve(lang);
        if (resolved == null)
        {
            return NotFound(new { error = "Strings resource not found" });
        }

        Response.Headers["Content-Language"] = resolved.Value.Culture;
        Response.Headers["Cache-Control"] = "private, max-age=300";
        return new JsonResult(resolved.Value.Strings);
    }
}
