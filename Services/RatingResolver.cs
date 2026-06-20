using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Services;

public class RatingResolver(
RatingRepository repository,
ILibraryManager libraryManager,
ILogger<RatingResolver> logger)
{
public UserRating? ResolveRating(Guid itemId, Guid userId)
    {
        var rating = repository.GetRating(itemId, userId);
        if (rating != null)
        {
            return rating;
        }

        var item = libraryManager.GetItemById(itemId);
        if (item?.ProviderIds == null || item.ProviderIds.Count == 0)
        {
            return null;
        }

        var found = repository.FindByProviderIds(userId, item.ProviderIds);
        if (found != null)
        {
            var oldItem = libraryManager.GetItemById(found.ItemId);
            if (oldItem == null)
            {
                logger.LogInformation(
                    "Healed rating: re-keyed {OldItemId} → {NewItemId} for user {UserId} via provider ID match",
                    found.ItemId, itemId, userId);

                repository.RepairRatingKey(found.ItemId, itemId, userId);

                found = found with { ItemId = itemId };

                if (found.ProviderIds == null || found.ProviderIds.Count == 0)
                {
                    found = found with { ProviderIds = new Dictionary<string, string>(item.ProviderIds) };
                    repository.SaveRating(found);
                }
            }
            else
            {
                logger.LogDebug(
                    "Skipped rating re-key: both {OldItemId} and {NewItemId} are valid library items",
                    found.ItemId, itemId);
            }

            return found;
        }

        return null;
    }

    public List<UserRating> ResolveRatingsForItem(Guid itemId)
    {
        var ratings = repository.GetRatingsForItem(itemId);
        if (ratings.Any())
        {
            return ratings;
        }

        var item = libraryManager.GetItemById(itemId);
        if (item?.ProviderIds == null || item.ProviderIds.Count == 0)
        {
            return ratings;
        }

        var allRatings = repository.GetRatingsForItem(itemId);
        if (allRatings.Any())
        {
            return allRatings;
        }

        var allUserRatings = repository.GetAllRatings();
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
                var oldItemObj = libraryManager.GetItemById(r.ItemId);
                if (oldItemObj == null)
                {
                    var oldItemId = r.ItemId;
                    repository.RepairRatingKey(r.ItemId, itemId, r.UserId);
                    r = r with { ItemId = itemId };

                    if (r.ProviderIds == null || r.ProviderIds.Count == 0)
                    {
                        r = r with { ProviderIds = new Dictionary<string, string>(item.ProviderIds) };
                    }

                    repository.SaveRating(r);
                    healed.Add(r);
                    logger.LogInformation(
                        "Healed rating for item {OldItemId} → {NewItemId}, user {UserId}",
                        oldItemId, itemId, r.UserId);
                }
                else
                {
                    logger.LogDebug(
                        "Skipped rating re-key in ResolveRatingsForItem: both {OldItemId} and {NewItemId} are valid library items",
                        r.ItemId, itemId);
                    healed.Add(r);
                }
            }
        }

        return healed;
    }

    public Dictionary<string, string>? GetProviderIdsForItem(Guid itemId)
    {
        var item = libraryManager.GetItemById(itemId);
        return item?.ProviderIds != null && item.ProviderIds.Count > 0
            ? new Dictionary<string, string>(item.ProviderIds)
            : null;
    }

    public bool HasRating(Guid itemId, Guid userId)
    {
        if (repository.GetRating(itemId, userId) != null)
            return true;

        var item = libraryManager.GetItemById(itemId);
        if (item?.ProviderIds == null || item.ProviderIds.Count == 0)
            return false;

        return repository.FindByProviderIds(userId, item.ProviderIds) != null;
    }
}