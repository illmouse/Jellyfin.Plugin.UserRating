using System;
using System.Collections.Generic;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.UserRatings.Services;

public class RatingResolver(
RatingRepository repository,
ILibraryManager libraryManager)
{
    public UserRating? ResolveRating(Guid itemId, Guid userId)
    {
        return repository.GetRating(itemId, userId);
    }

    public List<UserRating> ResolveRatingsForItem(Guid itemId)
    {
        return repository.GetRatingsForItem(itemId);
    }

    public Dictionary<string, string>? GetProviderIdsForItem(Guid itemId)
    {
        var item = libraryManager.GetItemById(itemId);
        return item?.ProviderIds != null && item.ProviderIds.Count > 0
            ? new Dictionary<string, string>(item.ProviderIds)
            : null;
    }
}