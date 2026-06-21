using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UserRatings.Api;

[ApiController]
[Route("api/UserRatings")]
public class RatingsController(
RatingRepository repository,
ILibraryManager libraryManager,
IUserManager userManager,
RatingResolver resolver,
BackupService backupService) : ControllerBase
{

    [HttpPost("Rate")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult RateItem([FromQuery] Guid itemId, [FromQuery] Guid userId, [FromQuery] int rating, [FromQuery] string? note, [FromQuery] string? userName)
    {
        if (rating < 1 || rating > 10)
        {
            return BadRequest(new ApiResponse(false, "Rating must be between 1 and 10"));
        }

        var providerIds = resolver.GetProviderIdsForItem(itemId);
        var userRating = new UserRating
        {
            ItemId = itemId,
            UserId = userId,
            Rating = rating,
            Note = note,
            Timestamp = DateTime.UtcNow,
            UserName = userName ?? "Unknown",
            ProviderIds = providerIds ?? new Dictionary<string, string>()
        };

        repository.SaveRating(userRating);

        return Ok(new ApiResponse(true, "Rating saved successfully"));
    }

    [HttpGet("Item/{itemId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult GetItemRatings(Guid itemId)
    {
        var ratings = resolver.ResolveRatingsForItem(itemId);
        var stats = repository.GetStatsForItem(itemId);

        return Ok(new ItemRatingsResponse(
            true,
            ratings.Select(r => new UserRatingInfo(
                r.UserId.ToString("N"),
                r.UserName,
                r.Rating,
                r.Note,
                r.Timestamp
            )).ToList(),
            stats.AverageRating,
            stats.TotalRatings
        ));
    }

    [HttpGet("User/{userId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult GetUserRatings(Guid userId)
    {
        var ratings = repository.GetRatingsForUser(userId);

        return Ok(new UserRatingsResponse(
            true,
            ratings.Select(r => new SimpleRatingInfo(
                r.ItemId.ToString("N"),
                r.Rating,
                r.Note,
                r.Timestamp
            )).ToList()
        ));
    }

    [HttpDelete("Rating")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult DeleteRating([FromQuery] Guid itemId, [FromQuery] Guid userId)
    {
        repository.DeleteRating(itemId, userId);
        return Ok(new ApiResponse(true, "Rating deleted successfully"));
    }

    [HttpGet("MyRating/{itemId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult GetMyRating(Guid itemId, [FromQuery] Guid userId)
    {
        var rating = resolver.ResolveRating(itemId, userId);

        if (rating == null)
        {
            return Ok(new MyRatingResponse(true, null, null, null));
        }

        return Ok(new MyRatingResponse(true, rating.Rating, rating.Note, rating.Timestamp));
    }

    [HttpDelete("DeleteAll")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult DeleteAllRatings()
    {
        repository.DeleteAllRatings();
        return Ok(new ApiResponse(true, "All ratings have been deleted successfully"));
    }

    [HttpGet("AllRatedItems")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult GetAllRatedItems()
    {
        var ratedItems = repository.GetAllRatedItems();

        var rawRatings = repository.GetAllRatings();
        var ratingByItemId = rawRatings.Values
            .GroupBy(r => r.ItemId)
            .ToDictionary(g => g.Key, g => g.First());

        var seenCanonical = new HashSet<Guid>();
        var result = new List<RatedItemInfo>();

        foreach (var item in ratedItems)
        {
            var effectiveId = item.ItemId;

            var libItem = libraryManager.GetItemById(item.ItemId);

            if (libItem != null)
            {
                effectiveId = libItem.Id;
            }
            else if (ratingByItemId.TryGetValue(item.ItemId, out var rating)
                && rating.ProviderIds != null && rating.ProviderIds.Count > 0)
            {
                try
                {
                    var providerQuery = new InternalItemsQuery
                    {
                        HasAnyProviderId = rating.ProviderIds
                            .Where(kv => !string.IsNullOrEmpty(kv.Value))
                            .ToDictionary(kv => kv.Key, kv => kv.Value)
                    };

                    if (providerQuery.HasAnyProviderId.Count > 0)
                    {
                        var resolved = libraryManager.GetItemList(providerQuery).FirstOrDefault();
                        if (resolved != null)
                        {
                            effectiveId = resolved.Id;
                        }
                    }
                }
                catch
                {
                }
            }

            if (!seenCanonical.Add(effectiveId))
                continue;

            string? name = null;
            string? type = null;
            string? seriesId = null;

            try
            {
                var resolvedItem = libraryManager.GetItemById(effectiveId);
                if (resolvedItem != null)
                {
                    name = resolvedItem.Name;
                    type = resolvedItem.GetType().Name;
                    if (resolvedItem is Episode ep && ep.Series != null)
                    {
                        seriesId = ep.Series.Id.ToString("N");
                    }
                }
            }
            catch
            {
            }

            result.Add(new RatedItemInfo(
                effectiveId.ToString("N"),
                item.AverageRating,
                item.TotalRatings,
                item.LastRated,
                name,
                type,
                seriesId
            ));
        }

        return Ok(new RatedItemsResponse(true, result));
    }

    [HttpGet("UnratedWatchedItems")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult GetUnratedWatchedItems([FromQuery] Guid userId, [FromQuery] string? itemType = null)
    {
        var watchedUnrated = new List<WatchedItemInfo>();

        var user = userManager.GetUserById(userId);
        if (user == null)
        {
            return NotFound(new ApiResponse(false, "User not found"));
        }

        bool includeMovies = itemType == null || itemType == "Movie";
        bool includeSeries = itemType == null || itemType == "Series";

        if (includeMovies)
        {
            var movieQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                IsPlayed = true,
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                Limit = 50
            };
            var movies = libraryManager.GetItemsResult(movieQuery);

            foreach (var item in movies.Items)
            {
                if (!resolver.HasRating(item.Id, userId))
                {
                    watchedUnrated.Add(new WatchedItemInfo(
                        item.Id.ToString("N"),
                        item.Name,
                        item.GetType().Name,
                        null,
                        item.UserData?.FirstOrDefault()?.LastPlayedDate
                    ));
                }
            }
        }

        if (includeSeries)
        {
            var episodeQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                IsPlayed = true,
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                Limit = 500
            };
            var episodes = libraryManager.GetItemsResult(episodeQuery);

            var seenSeries = new HashSet<Guid>();
            foreach (var ep in episodes.Items)
            {
                if (ep is not Episode episode) continue;
                var seriesId = episode.SeriesId;
                if (seriesId == Guid.Empty) continue;
                if (!seenSeries.Add(seriesId)) continue;

                if (!resolver.HasRating(seriesId, userId))
                {
                    var series = libraryManager.GetItemById(seriesId);
                    if (series != null)
                    {
                        watchedUnrated.Add(new WatchedItemInfo(
                            series.Id.ToString("N"),
                            series.Name,
                            series.GetType().Name,
                            null,
                            ep.UserData?.FirstOrDefault()?.LastPlayedDate
                        ));
                    }
                }

                if (seenSeries.Count >= 50) break;
            }
        }

        return Ok(new UnratedWatchedItemsResponse(true, watchedUnrated));
    }

    [HttpGet("MigrationStatus")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult GetMigrationStatus()
    {
        var metadata = repository.Metadata;
        var ratingsAbove5 = repository.GetRatingsAbove5();

        return Ok(new MigrationStatusResponse(
            true,
            metadata.CurrentVersion,
            metadata.VersionHistory.ToList(),
            metadata.Migrations.Select(m => new MigrationRecordDto(
                m.Name,
                m.Date,
                m.PluginVersion,
                m.ResultMigrated,
                m.ResultSkipped
            )).ToList(),
            repository.RatingCount,
            ratingsAbove5.Count,
            ratingsAbove5.Select(r => {
                var item = libraryManager.GetItemById(r.ItemId);
                return new RatingAbove5Dto(
                    r.ItemId.ToString("N"),
                    r.UserId.ToString("N"),
                    r.Rating,
                    item?.Name ?? null
                );
            }).ToList()
        ));
    }

    [HttpPost("MigrateTo10Star")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult MigrateTo10Star()
    {
        var (backupSuccess, backupPath, _) = backupService.CreateBackup();
        if (!backupSuccess)
        {
            return Ok(new MigrateResponse(false, 0, 0, string.Empty));
        }

        var (migrated, skipped) = repository.MigrateTo10StarScale();
        return Ok(new MigrateResponse(true, migrated, skipped, backupPath));
    }
}
