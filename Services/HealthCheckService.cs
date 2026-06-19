using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Services
{
    public class HealthCheckService
    {
        private readonly RatingRepository _repository;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(
            RatingRepository repository,
            ILibraryManager libraryManager,
            ILogger<HealthCheckService> logger)
        {
            _repository = repository;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public HealthReport RunHealthCheck(bool heal = false)
        {
            var report = new HealthReport();
            var allRatings = _repository.GetAllRatings();

            foreach (var kvp in allRatings)
            {
                var rating = kvp.Value;

                var item = _libraryManager.GetItemById(rating.ItemId);
                if (item != null)
                {
                    report.Ok++;

                    if ((rating.ProviderIds == null || rating.ProviderIds.Count == 0)
                        && item.ProviderIds != null && item.ProviderIds.Count > 0)
                    {
                        rating.ProviderIds = new Dictionary<string, string>(item.ProviderIds);
                        _repository.SaveRating(rating);
                    }

                    continue;
                }

                if (rating.ProviderIds != null && rating.ProviderIds.Count > 0)
                {
                    var matched = TryResolveByProviderIds(rating.ProviderIds);
                    if (matched != null)
                    {
                        report.Healed++;

                        if (heal)
                        {
                            _logger.LogInformation(
                                "Healed rating: {OldItemId} → {NewItemId} for user {UserId}",
                                rating.ItemId, matched.Id, rating.UserId);

                            _repository.RepairRatingKey(rating.ItemId, matched.Id, rating.UserId);

                            var healedRating = _repository.GetRating(matched.Id, rating.UserId);
                            if (healedRating != null)
                            {
                                healedRating.ProviderIds = new Dictionary<string, string>(matched.ProviderIds);
                                _repository.SaveRating(healedRating);
                            }
                        }

                        continue;
                    }
                }

                report.Stale++;
                report.StaleItems.Add(new StaleItem
                {
                    ItemId = rating.ItemId,
                    UserId = rating.UserId,
                    Rating = rating.Rating,
                    Note = rating.Note,
                    ProviderIds = rating.ProviderIds ?? new Dictionary<string, string>(),
                    Timestamp = rating.Timestamp
                });
            }

            _logger.LogInformation(
                "Health check complete: {Ok} ok, {Healed} healed, {Stale} stale (heal={Heal})",
                report.Ok, report.Healed, report.Stale, heal);

            return report;
        }

        public int ClearStale()
        {
            var report = RunHealthCheck(heal: false);
            var removed = 0;

            foreach (var stale in report.StaleItems)
            {
                _repository.DeleteRating(stale.ItemId, stale.UserId);
                removed++;
            }

            _logger.LogInformation("Cleared {Count} stale ratings", removed);
            return removed;
        }

        private BaseItem? TryResolveByProviderIds(Dictionary<string, string> providerIds)
        {
            if (providerIds == null || providerIds.Count == 0)
            {
                return null;
            }

            var query = new MediaBrowser.Model.Querying.InternalItemsQuery
            {
                HasAnyProviderId = providerIds
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
            };

            if (query.HasAnyProviderId == null || query.HasAnyProviderId.Count == 0)
            {
                return null;
            }

            var results = _libraryManager.GetItemList(query);
            return results.FirstOrDefault();
        }
    }
}