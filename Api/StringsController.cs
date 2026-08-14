using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api;

[ApiController]
[Route("api/UserRatings")]
public class StringsController : ControllerBase
{
    [HttpGet("Strings")]
    [Produces("application/json")]
    public ActionResult GetStrings()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Jellyfin.Plugin.UserRatings.Strings.en.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return NotFound(new { error = "Strings resource not found" });
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return Content(json, "application/json");
    }
}