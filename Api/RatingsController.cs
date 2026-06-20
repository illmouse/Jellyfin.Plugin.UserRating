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

namespace Jellyfin.Plugin.UserRatings.Api
{
    [ApiController]
    [Route("api/UserRatings")]
    public class RatingsController : ControllerBase
    {
        private readonly RatingRepository _repository;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly RatingResolver _resolver;

        public RatingsController(
            RatingRepository repository,
            ILibraryManager libraryManager,
            IUserManager userManager,
            RatingResolver resolver)
        {
            _repository = repository;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _resolver = resolver;
        }

        [HttpPost("Rate")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult RateItem([FromQuery] Guid itemId, [FromQuery] Guid userId, [FromQuery] int rating, [FromQuery] string? note, [FromQuery] string? userName)
        {
            try
            {
                if (rating < 1 || rating > 5)
                {
                    return BadRequest(new { success = false, message = "Rating must be between 1 and 5" });
                }

                var userRating = new UserRating
                {
                    ItemId = itemId,
                    UserId = userId,
                    Rating = rating,
                    Note = note,
                    Timestamp = DateTime.UtcNow,
                    UserName = userName ?? "Unknown"
                };

                var providerIds = _resolver.GetProviderIdsForItem(itemId);
                if (providerIds != null)
                {
                    userRating.ProviderIds = providerIds;
                }

                _repository.SaveRating(userRating);

                return Ok(new { success = true, message = "Rating saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Item/{itemId}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetItemRatings(Guid itemId)
        {
            try
            {
                var ratings = _resolver.ResolveRatingsForItem(itemId);
                var stats = _repository.GetStatsForItem(itemId);

                return Ok(new
                {
                    success = true,
                    ratings = ratings.Select(r => new
                    {
                        userId = r.UserId.ToString("N"),
                        userName = r.UserName,
                        rating = r.Rating,
                        note = r.Note,
                        timestamp = r.Timestamp
                    }),
                    averageRating = stats.AverageRating,
                    totalRatings = stats.TotalRatings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("User/{userId}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetUserRatings(Guid userId)
        {
            try
            {
                var ratings = _repository.GetRatingsForUser(userId);

                return Ok(new
                {
                    success = true,
                    ratings = ratings.Select(r => new
                    {
                        itemId = r.ItemId.ToString("N"),
                        rating = r.Rating,
                        note = r.Note,
                        timestamp = r.Timestamp
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Rating")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult DeleteRating([FromQuery] Guid itemId, [FromQuery] Guid userId)
        {
            try
            {
                _repository.DeleteRating(itemId, userId);

                return Ok(new { success = true, message = "Rating deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("MyRating/{itemId}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetMyRating(Guid itemId, [FromQuery] Guid userId)
        {
            try
            {
                var rating = _resolver.ResolveRating(itemId, userId);

                if (rating == null)
                {
                    return Ok(new { success = true, rating = (int?)null });
                }

                return Ok(new
                {
                    success = true,
                    rating = rating.Rating,
                    note = rating.Note,
                    timestamp = rating.Timestamp
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("DeleteAll")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult DeleteAllRatings()
        {
            try
            {
                _repository.DeleteAllRatings();

                return Ok(new { success = true, message = "All ratings have been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("AllRatedItems")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetAllRatedItems()
        {
            try
            {
                var ratedItems = _repository.GetAllRatedItems();

                var rawRatings = _repository.GetAllRatings();
                var ratingByItemId = rawRatings.Values
                    .GroupBy(r => r.ItemId)
                    .ToDictionary(g => g.Key, g => g.First());

                var seenCanonical = new HashSet<Guid>();
                var result = new List<object>();

                foreach (var item in ratedItems)
                {
                    var effectiveId = item.ItemId;

                    var libItem = _libraryManager.GetItemById(item.ItemId);

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
                                var resolved = _libraryManager.GetItemList(providerQuery).FirstOrDefault();
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
                        var resolvedItem = _libraryManager.GetItemById(effectiveId);
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

                    result.Add(new
                    {
                        itemId = effectiveId.ToString("N"),
                        averageRating = item.AverageRating,
                        totalRatings = item.TotalRatings,
                        lastRated = item.LastRated,
                        name,
                        type,
                        seriesId
                    });
                }

                return Ok(new { success = true, items = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("UnratedWatchedItems")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetUnratedWatchedItems([FromQuery] Guid userId, [FromQuery] string? itemType = null)
        {
            try
            {
                var watchedUnrated = new List<object>();

                var user = _userManager.GetUserById(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
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
                    var movies = _libraryManager.GetItemsResult(movieQuery);

                    foreach (var item in movies.Items)
                    {
                        if (!_resolver.HasRating(item.Id, userId))
                        {
                            watchedUnrated.Add(new
                            {
                                itemId = item.Id.ToString("N"),
                                name = item.Name,
                                type = item.GetType().Name,
                                seriesId = (string?)null,
                                lastPlayedDate = item.UserData?.FirstOrDefault()?.LastPlayedDate
                            });
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
                    var episodes = _libraryManager.GetItemsResult(episodeQuery);

                    var seenSeries = new HashSet<Guid>();
                    foreach (var ep in episodes.Items)
                    {
                        if (ep is not Episode episode) continue;
                        var seriesId = episode.SeriesId;
                        if (seriesId == Guid.Empty) continue;
                        if (!seenSeries.Add(seriesId)) continue;

                        if (!_resolver.HasRating(seriesId, userId))
                        {
                            var series = _libraryManager.GetItemById(seriesId);
                            if (series != null)
                            {
                                watchedUnrated.Add(new
                                {
                                    itemId = series.Id.ToString("N"),
                                    name = series.Name,
                                    type = series.GetType().Name,
                                    seriesId = (string?)null,
                                    lastPlayedDate = ep.UserData?.FirstOrDefault()?.LastPlayedDate
                                });
                            }
                        }

                        if (seenSeries.Count >= 50) break;
                    }
                }

                return Ok(new { success = true, items = watchedUnrated });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}

