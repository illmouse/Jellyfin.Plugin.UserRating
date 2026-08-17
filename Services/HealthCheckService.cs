using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.UserRatings.Configuration;
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
    private static readonly HashSet<string> SpecificProviderKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Imdb", "Tmdb", "Tvdb", "Tvb", "MusicBrainzAlbum", "MusicBrainzArtist", "MusicBrainzReleaseGroup"
    };

    public HealthReport RunHealthCheck(bool heal = false)
    {
        var report = new HealthReport();
        var allRatings = repository.GetAllRatings();

        var allItemIds = allRatings.Values.Select(r => r.ItemId).Distinct().ToArray();
        var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            ItemIds = allItemIds
        };
        var libItems = libraryManager.GetItemList(query);
        var libItemMap = libItems
            .Where(i => i != null)
            .GroupBy(i => i.Id)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var kvp in allRatings)
        {
            var rating = kvp.Value;

            if (libItemMap.TryGetValue(rating.ItemId, out var item) && item != null)
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
                var (matched, matchType, matchedProviderIds) = TryResolveByProviderIds(rating.ProviderIds);
                if (matched != null)
                {
                    if (heal)
                    {
                        var healConflictMode = (Plugin.Instance?.Configuration as Configuration.PluginConfiguration)?.HealingConflictMode ?? "skip";
                        var result = repository.RepairRatingKey(rating.ItemId, matched.Id, rating.UserId, healConflictMode);

                        if (result == HealConflictResult.ConflictSkipped)
                        {
                            report.Conflicts++;
                            var existingRating = repository.GetRating(matched.Id, rating.UserId);
                            report.ConflictItems.Add(new ConflictItem
                            {
                                OldItemId = rating.ItemId,
                                NewItemId = matched.Id,
                                ItemName = matched.Name,
                                UserId = rating.UserId,
                                IncomingRating = rating.Rating,
                                ExistingRating = existingRating?.Rating ?? 0,
                                IncomingNote = rating.Note,
                                ExistingNote = existingRating?.Note,
                                IncomingTimestamp = rating.Timestamp,
                                ExistingTimestamp = existingRating?.Timestamp ?? DateTime.MinValue,
                                ConflictReason = DetermineConflictReason(healConflictMode, rating, existingRating),
                                ProviderIds = rating.ProviderIds ?? new Dictionary<string, string>()
                            });

                            logger.LogInformation(
                                "Heal conflict at {NewItemId}: kept existing rating, incoming rating preserved at old key {OldItemId}",
                                matched.Id, rating.ItemId);
                        }
                        else
                        {
                            report.Healed++;
                            logger.LogInformation(
                                "Healed rating: {OldItemId} → {NewItemId} for user {UserId}",
                                rating.ItemId, matched.Id, rating.UserId);

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
                                Rating = rating.Rating,
                                Note = rating.Note,
                                Timestamp = rating.Timestamp,
                                ProviderIds = matched.ProviderIds != null ? new Dictionary<string, string>(matched.ProviderIds) : new Dictionary<string, string>()
                            });
                        }
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
                            Note = rating.Note,
                            Timestamp = rating.Timestamp,
                            ProviderIds = rating.ProviderIds,
                            MatchType = matchType,
                            MatchedProviderIds = matchedProviderIds
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
            "Health check complete: {Ok} ok, {Recoverable} recoverable, {Healed} healed, {Updated} updated, {Stale} stale, {Conflicts} conflicts (heal={Heal})",
            report.Ok, report.Recoverable, report.Healed, report.Updated, report.Stale, report.Conflicts, heal);

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

    public HealConflictResult HealSingleItem(Guid oldItemId, Guid newItemId, Guid userId)
    {
        var healConflictMode = (Plugin.Instance?.Configuration as Configuration.PluginConfiguration)?.HealingConflictMode ?? "skip";
        var result = repository.RepairRatingKey(oldItemId, newItemId, userId, healConflictMode);

        if (result == HealConflictResult.Replaced)
        {
            var healedRating = repository.GetRating(newItemId, userId);
            if (healedRating != null)
            {
                var libItem = libraryManager.GetItemById(newItemId);
                if (libItem?.ProviderIds != null)
                {
                    healedRating = healedRating with { ProviderIds = new Dictionary<string, string>(libItem.ProviderIds) };
                    repository.SaveRating(healedRating);
                }
            }
        }

        return result;
    }

    private (BaseItem? item, string matchType, Dictionary<string, string> matchedProviderIds) TryResolveByProviderIds(Dictionary<string, string> providerIds)
    {
        if (providerIds == null || providerIds.Count == 0)
        {
            return (null, "none", new Dictionary<string, string>());
        }

        var specificIds = providerIds
            .Where(kv => SpecificProviderKeys.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        if (specificIds.Count > 0)
        {
            var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                HasAnyProviderId = specificIds
            };
            var results = libraryManager.GetItemList(query);
            if (results.Count > 0)
            {
                var match = results[0];
                var matchedIds = specificIds
                    .Where(kv => match.ProviderIds != null &&
                                 match.ProviderIds.TryGetValue(kv.Key, out var v) &&
                                 string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                return (match, "specific", matchedIds);
            }
        }

        var collectionIds = providerIds
            .Where(kv => !SpecificProviderKeys.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        if (collectionIds.Count > 0)
        {
            var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                HasAnyProviderId = collectionIds
            };
            var results = libraryManager.GetItemList(query);
            if (results.Count > 0)
            {
                var match = results[0];
                var matchedIds = collectionIds
                    .Where(kv => match.ProviderIds != null &&
                                 match.ProviderIds.TryGetValue(kv.Key, out var v) &&
                                 string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                return (match, "collection", matchedIds);
            }
        }

        return (null, "none", new Dictionary<string, string>());
    }

    private string DetermineConflictReason(string conflictMode, UserRating incoming, UserRating? existing)
    {
        var incomingSource = string.IsNullOrEmpty(incoming?.Source) ? "jellyfin" : incoming.Source;
        var existingSource = string.IsNullOrEmpty(existing?.Source) ? "jellyfin" : existing.Source;

        // Note: This method is only called when ConflictSkipped is returned.
        // "overwrite" mode never returns ConflictSkipped (incoming always wins).

        if (conflictMode == "keepHigher")
        {
            if (incoming?.Rating < existing?.Rating)
                return "keepHigher-existing-higher";
            if (incoming?.Rating == existing?.Rating && incoming?.Timestamp <= existing?.Timestamp)
                return "keepHigher-equal-existing-newer";
            return "keepHigher";
        }

        // skip (default)
        if (existingSource == "jellyfin" && incomingSource == "plex")
            return "skip-jellyfin-over-plex";
        if (incoming?.Timestamp <= existing?.Timestamp)
            return "skip-same-source-existing-newer";
        return "skip";
    }
}