using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api;

[ApiController]
[Route("api/UserRatings")]
[Authorize]
public class ConfigController : ControllerBase
{
    [HttpGet("DisplayConfig")]
    [Produces("application/json")]
    public ActionResult<DisplayConfigDto> GetDisplayConfig()
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            return NotFound(new { error = "Plugin configuration not available" });
        }

        return Ok(new DisplayConfigDto
        {
            ShowAverageRatingBadge = config.ShowAverageRatingBadge,
            ShowPersonalRatingBadge = config.ShowPersonalRatingBadge,
            AverageBadgePosition = config.AverageBadgePosition,
            PersonalBadgePosition = config.PersonalBadgePosition,
            FavoriteThreshold = config.FavoriteThreshold,
            RecentlyRatedItemsCount = config.RecentlyRatedItemsCount
        });
    }
}

public class DisplayConfigDto
{
    public bool ShowAverageRatingBadge { get; set; }
    public bool ShowPersonalRatingBadge { get; set; }
    public string AverageBadgePosition { get; set; } = "top-left";
    public string PersonalBadgePosition { get; set; } = "top-left";
    public int FavoriteThreshold { get; set; } = 9;
    public int RecentlyRatedItemsCount { get; set; } = 10;
}