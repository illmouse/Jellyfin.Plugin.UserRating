using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Services;

public class HealthCheckService(
RatingRepository repository,
ILibraryManager libraryManager,
ILogger<HealthCheckService> logger)
{

    public HealthReport RunHealthCheck(bool heal = false)
    {
        repository.Reload();
        var report = new HealthReport();
        var allRatings = repository.GetAllRatings();

        foreach (var kvp in allRatings)
        {
            var rating = kvp.Value;

            var item = libraryManager.GetItemById(rating.ItemId);
            if (item != null)
            {
                if (item.ProviderIds != null && item.ProviderIds.Count > 0)
                {
                    bool needsUpdate = false;

                    if (rating.ProviderIds == null || rating.ProviderIds.Count == 0)
                    {
                        needsUpdate = true;
                    }
                    else
                    {
                        foreach (var pkv in item.ProviderIds)
                        {
                            if (!rating.ProviderIds.TryGetValue(pkv.Key, out var existing)
                                || !string.Equals(existing, pkv.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                needsUpdate = true;
                                break;
                            }
                        }
                    }

                    if (needsUpdate)
                    {
                        var updated = rating with { ProviderIds = new Dictionary<string, string>(item.ProviderIds) };
                        repository.SaveRating(updated);
                        report.Updated++;
                    }
                }

                report.Ok++;
                continue;
            }

            if (rating.ProviderIds != null && rating.ProviderIds.Count > 0)
            {
                var matched = TryResolveByProviderIds(rating.ProviderIds);
                if (matched != null)
                {
                    if (heal)
                    {
                        report.Healed++;

                        logger.LogInformation(
                            "Healed rating: {OldItemId} → {NewItemId} for user {UserId}",
                            rating.ItemId, matched.Id, rating.UserId);

                        repository.RepairRatingKey(rating.ItemId, matched.Id, rating.UserId);

                        var healedRating = repository.GetRating(matched.Id, rating.UserId);
                        if (healedRating != null)
                        {
                            healedRating = healedRating with { ProviderIds = new Dictionary<string, string>(matched.ProviderIds) };
                            repository.SaveRating(healedRating);
                        }

                        report.HealedItems.Add(new HealedItem
                        {
                            OldItemId = rating.ItemId,
                            NewItemId = matched.Id,
                            ItemName = matched.Name,
                            UserId = rating.UserId,
                            Rating = rating.Rating
                        });
                    }
                    else
                    {
                        report.Recoverable++;
                        report.RecoverableItems.Add(new RecoverableItem
                        {
                            OldItemId = rating.ItemId,
                            NewItemId = matched.Id,
                            ItemName = matched.Name,
                            UserId = rating.UserId,
                            Rating = rating.Rating,
                            ProviderIds = rating.ProviderIds
                        });
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

        logger.LogInformation(
            "Health check complete: {Ok} ok, {Recoverable} recoverable, {Healed} healed, {Updated} updated, {Stale} stale (heal={Heal})",
            report.Ok, report.Recoverable, report.Healed, report.Updated, report.Stale, heal);

        return report;
    }

    public int ClearStale()
    {
        var report = RunHealthCheck(heal: false);
        var removed = 0;

        foreach (var stale in report.StaleItems)
        {
            repository.DeleteRating(stale.ItemId, stale.UserId);
            removed++;
        }

        logger.LogInformation("Cleared {Count} stale ratings", removed);
        return removed;
    }

    private BaseItem? TryResolveByProviderIds(Dictionary<string, string> providerIds)
    {
        if (providerIds == null || providerIds.Count == 0)
        {
            return null;
        }

        var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            HasAnyProviderId = providerIds
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        if (query.HasAnyProviderId == null || query.HasAnyProviderId.Count == 0)
        {
            return null;
        }

        var results = libraryManager.GetItemList(query);
        return results.FirstOrDefault();
    }
}