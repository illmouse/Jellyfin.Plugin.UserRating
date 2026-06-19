using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Services
{
    public class RatingResolver
    {
        private readonly RatingRepository _repository;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<RatingResolver> _logger;

        public RatingResolver(
            RatingRepository repository,
            ILibraryManager libraryManager,
            ILogger<RatingResolver> logger)
        {
            _repository = repository;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public UserRating? ResolveRating(Guid itemId, Guid userId)
        {
            var rating = _repository.GetRating(itemId, userId);
            if (rating != null)
            {
                return rating;
            }

            var item = _libraryManager.GetItemById(itemId);
            if (item?.ProviderIds == null || item.ProviderIds.Count == 0)
            {
                return null;
            }

            var found = _repository.FindByProviderIds(userId, item.ProviderIds);
            if (found != null)
            {
                _logger.LogInformation(
                    "Healed rating: re-keyed {OldItemId} → {NewItemId} for user {UserId} via provider ID match",
                    found.ItemId, itemId, userId);

                _repository.RepairRatingKey(found.ItemId, itemId, userId);

                found.ItemId = itemId;

                if (found.ProviderIds == null || found.ProviderIds.Count == 0)
                {
                    found.ProviderIds = new Dictionary<string, string>(item.ProviderIds);
                    _repository.SaveRating(found);
                }

                return found;
            }

            return null;
        }

        public List<UserRating> ResolveRatingsForItem(Guid itemId)
        {
            var ratings = _repository.GetRatingsForItem(itemId);
            if (ratings.Any())
            {
                return ratings;
            }

            var item = _libraryManager.GetItemById(itemId);
            if (item?.ProviderIds == null || item.ProviderIds.Count == 0)
            {
                return ratings;
            }

            var allRatings = _repository.GetRatingsForItem(itemId);
            if (allRatings.Any())
            {
                return allRatings;
            }

            var allUserRatings = _repository.GetAllRatings();
            var healed = new List<UserRating>();

            foreach (var kvp in allUserRatings)
            {
                var r = kvp.Value;
                if (r.ProviderIds == null || r.ProviderIds.Count == 0) continue;

                bool match = false;
                foreach (var pkv in item.ProviderIds)
                {
                    if (r.ProviderIds.TryGetValue(pkv.Key, out var val)
                        && !string.IsNullOrEmpty(val)
                        && !string.IsNullOrEmpty(pkv.Value)
                        && string.Equals(val, pkv.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                {
                    var oldItemId = r.ItemId;
                    _repository.RepairRatingKey(r.ItemId, itemId, r.UserId);
                    r.ItemId = itemId;

                    if (r.ProviderIds == null || r.ProviderIds.Count == 0)
                    {
                        r.ProviderIds = new Dictionary<string, string>(item.ProviderIds);
                    }

                    _repository.SaveRating(r);
                    healed.Add(r);
                    _logger.LogInformation(
                        "Healed rating for item {OldItemId} → {NewItemId}, user {UserId}",
                        oldItemId, itemId, r.UserId);
                }
            }

            return healed;
        }

        public Dictionary<string, string>? GetProviderIdsForItem(Guid itemId)
        {
            var item = _libraryManager.GetItemById(itemId);
            return item?.ProviderIds != null && item.ProviderIds.Count > 0
                ? new Dictionary<string, string>(item.ProviderIds)
                : null;
        }
    }
}