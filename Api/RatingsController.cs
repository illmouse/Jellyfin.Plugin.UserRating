using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
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

        public RatingsController(RatingRepository repository, ILibraryManager libraryManager, IUserManager userManager)
        {
            _repository = repository;
            _libraryManager = libraryManager;
            _userManager = userManager;
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
                var ratings = _repository.GetRatingsForItem(itemId);
                var stats = _repository.GetStatsForItem(itemId);

                return Ok(new
                {
                    success = true,
                    ratings = ratings.Select(r => new
                    {
                        userId = r.UserId,
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
                        itemId = r.ItemId,
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
                var rating = _repository.GetRating(itemId, userId);

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

                var result = ratedItems.Select(item =>
                {
                    string? name = null;
                    string? type = null;
                    string? seriesId = null;

                    try
                    {
                        var libraryItem = _libraryManager.GetItemById(item.ItemId);
                        if (libraryItem != null)
                        {
                            name = libraryItem.Name;
                            type = libraryItem.GetType().Name;
                            if (libraryItem is Episode ep)
                            {
                                seriesId = ep.Series?.Id.ToString();
                            }
                        }
                    }
                    catch
                    {
                    }

                    return new
                    {
                        itemId = item.ItemId,
                        averageRating = item.AverageRating,
                        totalRatings = item.TotalRatings,
                        lastRated = item.LastRated,
                        name,
                        type,
                        seriesId
                    };
                });

                return Ok(new { success = true, items = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("UnratedWatchedItems")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult GetUnratedWatchedItems([FromQuery] Guid userId)
        {
            try
            {
                // Get all items this user has rated
                var ratedItemIds = _repository.GetRatingsForUser(userId)
                    .Select(r => r.ItemId)
                    .ToHashSet();

                // Get all movies and series the user has played/watched
                var watchedUnrated = new List<object>();

                var user = _userManager.GetUserById(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Query library for Movies
                var movieQuery = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    IsPlayed = true,
                    OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                    Limit = 100,
                    Recursive = true
                };
                var movies = _libraryManager.GetItemsResult(movieQuery);

                foreach (var item in movies.Items)
                {
                    if (!ratedItemIds.Contains(item.Id))
                    {
                        watchedUnrated.Add(new
                        {
                            itemId = item.Id,
                            name = item.Name,
                            type = item.GetType().Name,
                            seriesId = (string?)null
                        });
                    }
                }

                // Query library for Series
                var seriesQuery = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = new[] { BaseItemKind.Series },
                    IsPlayed = true,
                    OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                    Limit = 100,
                    Recursive = true
                };
                var series = _libraryManager.GetItemsResult(seriesQuery);

                foreach (var item in series.Items)
                {
                    if (!ratedItemIds.Contains(item.Id))
                    {
                        watchedUnrated.Add(new
                        {
                            itemId = item.Id,
                            name = item.Name,
                            type = item.GetType().Name,
                            seriesId = (string?)null
                        });
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

