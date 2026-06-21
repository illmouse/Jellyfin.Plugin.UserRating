using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Services;

public class PlexImportService(
IHttpClientFactory httpClientFactory,
RatingRepository repository,
ILibraryManager libraryManager,
ILogger<PlexImportService> logger,
ProgressTracker progressTracker,
IUserManager userManager,
IUserDataManager userDataManager)
{

    private PluginConfiguration GetConfig()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    public async Task<ImportResult> ImportFromPlexAsync(Guid jellyfinUserId, string operationId, CancellationToken cancellationToken, string? conflictMode = null)
    {
        var config = GetConfig();
        var plexUrl = config.PlexServerUrl?.TrimEnd('/') ?? string.Empty;
        var plexToken = config.PlexToken;
        var effectiveConflictMode = conflictMode ?? config.PlexImportConflictMode ?? "skip";
        var syncWatchHistory = config.EnablePlexWatchHistorySync;

        if (string.IsNullOrEmpty(plexUrl) || string.IsNullOrEmpty(plexToken))
        {
            return new ImportResult
            {
                Success = false,
                OperationId = operationId,
                Message = "Plex server URL and token must be configured"
            };
        }

        try
        {
            var userName = userManager.GetUserById(jellyfinUserId)?.Username ?? "Unknown";
            var ratedItems = new List<(PlexVideo plexItem, Guid jellyfinItemId, int convertedRating)>();
            var unmatchedItems = new List<UnmatchedItem>();
            var importedCount = 0;
            var skippedCount = 0;
            var watchedCount = 0;

            progressTracker.UpdateProgress(operationId, p => p.Status = "fetching");

            var libraries = await GetPlexLibrariesAsync(plexUrl, plexToken, cancellationToken).ConfigureAwait(false);

            var movieLibraries = libraries.Where(l => l.Type == "movie").ToList();
            var showLibraries = libraries.Where(l => l.Type == "show").ToList();

            logger.LogInformation("Found {MovieLibs} movie libraries and {ShowLibs} show libraries in Plex", movieLibraries.Count, showLibraries.Count);

            var allPlexItems = new List<PlexVideo>();

            foreach (var lib in movieLibraries)
            {
                var items = await GetPlexLibraryItemsAsync(plexUrl, plexToken, lib.Key, cancellationToken).ConfigureAwait(false);
                allPlexItems.AddRange(items);
            }

            foreach (var lib in showLibraries)
            {
                var items = await GetPlexLibraryItemsAsync(plexUrl, plexToken, lib.Key, cancellationToken).ConfigureAwait(false);
                allPlexItems.AddRange(items);
            }

            // === RATINGS PASS ===
            var ratedPlexItems = allPlexItems.Where(i => i.UserRating > 0 && i.Type != "episode").ToList();
            var totalRatedItems = ratedPlexItems.Count;

            progressTracker.UpdateProgress(operationId, p =>
            {
                p.TotalItems = totalRatedItems;
                p.Status = "matching";
            });

            logger.LogInformation("Found {Count} rated items in Plex to import", totalRatedItems);

            for (var i = 0; i < ratedPlexItems.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    progressTracker.UpdateProgress(operationId, p => p.Status = "cancelled");
                    return new ImportResult
                    {
                        Success = false,
                        OperationId = operationId,
                        Message = "Import was cancelled",
                        Imported = importedCount,
                        Skipped = skippedCount,
                        Watched = watchedCount,
                        Unmatched = unmatchedItems.Count
                    };
                }

                var plexItem = ratedPlexItems[i];
                progressTracker.UpdateProgress(operationId, p =>
                {
                    p.ProcessedItems = i + 1;
                    p.CurrentItem = plexItem.Title;
                    p.PercentComplete = (double)(i + 1) / (totalRatedItems + (syncWatchHistory ? 1 : 0)) * 80;
                });

                var jellyfinItem = ResolvePlexItemToJellyfin(plexItem);

                if (jellyfinItem == null)
                {
                    unmatchedItems.Add(new UnmatchedItem
                    {
                        Title = plexItem.Title,
                        PlexRating = plexItem.UserRating,
                        PlexType = plexItem.Type,
                        Guids = plexItem.Guids.Select(g => g.Id).ToList()
                    });
                    continue;
                }

                var convertedRating = ConvertRating(plexItem.UserRating);
                ratedItems.Add((plexItem, jellyfinItem.Value, convertedRating));
            }

            var ratings = ratedItems.Select(r => new UserRating
            {
                ItemId = r.jellyfinItemId,
                UserId = jellyfinUserId,
                Rating = r.convertedRating,
                Note = $"Imported from Plex (original: {r.plexItem.UserRating}/10)",
                Timestamp = DateTime.UtcNow,
                UserName = userName,
                ProviderIds = r.plexItem.Guids
                    .Where(g => !string.IsNullOrEmpty(g.JellyfinProviderKey) && !string.IsNullOrEmpty(g.ExternalId))
                    .GroupBy(g => g.JellyfinProviderKey)
                    .ToDictionary(g => g.Key, g => g.First().ExternalId)
            }).ToList();

            progressTracker.UpdateProgress(operationId, p =>
            {
                p.Status = "saving";
                p.PercentComplete = 95;
                p.CurrentItem = "Saving ratings...";
            });

            var (imported, skipped, _) = repository.BulkSaveRatings(ratings, effectiveConflictMode);

            importedCount = imported;
            skippedCount = skipped;

            // === WATCH HISTORY PASS ===
            if (syncWatchHistory)
            {
                var watchedPlexItems = allPlexItems.Where(i => i.ViewCount > 0).ToList();
                var totalWatchedItems = watchedPlexItems.Count;

                progressTracker.UpdateProgress(operationId, p =>
                {
                    p.Status = "watch_history";
                    p.TotalItems = totalWatchedItems;
                    p.ProcessedItems = 0;
                    p.PercentComplete = 85;
                });

                logger.LogInformation("Syncing watch history for {Count} items from Plex", totalWatchedItems);

                for (var i = 0; i < watchedPlexItems.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        progressTracker.UpdateProgress(operationId, p => p.Status = "cancelled");
                        return new ImportResult
                        {
                            Success = false,
                            OperationId = operationId,
                            Message = "Import was cancelled",
                            Imported = importedCount,
                            Skipped = skippedCount,
                            Watched = watchedCount,
                            Unmatched = unmatchedItems.Count
                        };
                    }

                    var plexItem = watchedPlexItems[i];
                    progressTracker.UpdateProgress(operationId, p =>
                    {
                        p.ProcessedItems = i + 1;
                        p.CurrentItem = plexItem.Title;
                        p.PercentComplete = 88 + (double)(i + 1) / totalWatchedItems * 12;
                    });

                    var jellyfinItemId = ResolvePlexItemToJellyfin(plexItem);
                    if (jellyfinItemId == null)
                    {
                        if (plexItem.UserRating <= 0)
                        {
                            unmatchedItems.Add(new UnmatchedItem
                            {
                                Title = plexItem.Title,
                                PlexRating = 0,
                                PlexType = plexItem.Type,
                                Guids = plexItem.Guids.Select(g => g.Id).ToList()
                            });
                        }
                        continue;
                    }

                    MarkItemAsPlayed(jellyfinItemId.Value, jellyfinUserId, plexItem);
                    watchedCount++;
                }
            }

            var message = $"Imported {importedCount} ratings, skipped {skippedCount}, could not match {unmatchedItems.Count}";
            if (syncWatchHistory)
            {
                message += $", marked {watchedCount} items as played";
            }

            var result = new ImportResult
            {
                Success = true,
                OperationId = operationId,
                Imported = importedCount,
                Skipped = skippedCount,
                Watched = watchedCount,
                Unmatched = unmatchedItems.Count,
                Message = message,
                UnmatchedItems = unmatchedItems
            };

            progressTracker.CompleteOperation(operationId, result);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing ratings from Plex");
            progressTracker.FailOperation(operationId, ex.Message);
            return new ImportResult
            {
                Success = false,
                OperationId = operationId,
                Message = $"Error importing from Plex: {ex.Message}"
            };
        }
    }

    public async Task<List<PlexDirectory>> GetPlexLibrariesAsync(string plexUrl, string plexToken, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var url = $"{plexUrl}/library/sections/?X-Plex-Token={Uri.EscapeDataString(plexToken)}";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var doc = XDocument.Parse(xml);

        var directories = doc.Root?.Elements("Directory") ?? Enumerable.Empty<XElement>();

        return directories.Select(d => new PlexDirectory
        {
            Key = d.Attribute("key")?.Value ?? string.Empty,
            Title = d.Attribute("title")?.Value ?? string.Empty,
            Type = d.Attribute("type")?.Value ?? string.Empty
        }).Where(d => d.Type == "movie" || d.Type == "show").ToList();
    }

    public async Task<List<PlexVideo>> GetPlexLibraryItemsAsync(string plexUrl, string plexToken, string sectionKey, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        var url = $"{plexUrl}/library/sections/{sectionKey}/all?X-Plex-Token={Uri.EscapeDataString(plexToken)}&includeGuids=1";
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParsePlexXml(xml);
    }

    private List<PlexVideo> ParsePlexXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        var result = new List<PlexVideo>();

        foreach (var element in doc.Root?.Elements() ?? Enumerable.Empty<XElement>())
        {
            var type = element.Attribute("type")?.Value ?? string.Empty;
            if (type != "movie" && type != "show" && type != "episode")
            {
                continue;
            }

            var userRatingStr = element.Attribute("userRating")?.Value;
            double.TryParse(userRatingStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var userRating);

            var viewCountStr = element.Attribute("viewCount")?.Value;
            var lastViewedAtStr = element.Attribute("lastViewedAt")?.Value;

            int.TryParse(viewCountStr, out var viewCount);
            if (userRating <= 0 && viewCount <= 0)
            {
                continue;
            }

            var video = new PlexVideo
            {
                RatingKey = element.Attribute("ratingKey")?.Value ?? string.Empty,
                Title = element.Attribute("title")?.Value ?? "Unknown",
                Type = type,
                Guid = element.Attribute("guid")?.Value ?? string.Empty,
                UserRating = userRating > 0 ? userRating : 0,
                ViewCount = viewCount,
                LastViewedAt = long.TryParse(lastViewedAtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lva) ? lva : null
            };

            foreach (var guidElement in element.Elements("Guid"))
            {
                var guidId = guidElement.Attribute("id")?.Value;
                if (!string.IsNullOrEmpty(guidId))
                {
                    video.Guids.Add(new PlexGuid { Id = guidId });
                }
            }

            if (!string.IsNullOrEmpty(video.Guid))
            {
                ParseLegacyGuid(video.Guid, video.Guids);
            }

            result.Add(video);
        }

        return result;
    }

    private void ParseLegacyGuid(string guid, List<PlexGuid> guids)
    {
        if (guid.Contains("imdb://"))
        {
            var start = guid.IndexOf("imdb://");
            var imdbId = guid.Substring(start + 7);
            var questionMark = imdbId.IndexOf('?');
            if (questionMark > 0)
            {
                imdbId = imdbId.Substring(0, questionMark);
            }

            if (imdbId.StartsWith("tt") && !guids.Any(g => g.Provider == "imdb"))
            {
                guids.Add(new PlexGuid { Id = $"imdb://{imdbId}" });
            }
        }
        else if (guid.Contains("tmdb://"))
        {
            var start = guid.IndexOf("tmdb://");
            var tmdbId = guid.Substring(start + 7);
            var questionMark = tmdbId.IndexOf('?');
            if (questionMark > 0)
            {
                tmdbId = tmdbId.Substring(0, questionMark);
            }

            if (!guids.Any(g => g.Provider == "tmdb"))
            {
                guids.Add(new PlexGuid { Id = $"tmdb://{tmdbId}" });
            }
        }
        else if (guid.Contains("tvdb://"))
        {
            var start = guid.IndexOf("tvdb://");
            var tvdbId = guid.Substring(start + 7);
            var questionMark = tvdbId.IndexOf('?');
            if (questionMark > 0)
            {
                tvdbId = tvdbId.Substring(0, questionMark);
            }

            if (!guids.Any(g => g.Provider == "tvdb"))
            {
                guids.Add(new PlexGuid { Id = $"tvdb://{tvdbId}" });
            }
        }
    }

    private Guid? ResolvePlexItemToJellyfin(PlexVideo plexItem)
    {
        var providerIds = plexItem.Guids
            .Where(g => !string.IsNullOrEmpty(g.JellyfinProviderKey) && !string.IsNullOrEmpty(g.ExternalId))
            .GroupBy(g => g.JellyfinProviderKey)
            .ToDictionary(g => g.Key, g => g.First().ExternalId);

        if (providerIds.TryGetValue("Imdb", out var imdbId))
        {
            var item = FindByProviderId("Imdb", imdbId);
            if (item != null)
            {
                logger.LogDebug("Matched '{Title}' via IMDB ID {ImdbId}", plexItem.Title, imdbId);
                return item.Id;
            }
        }

        if (providerIds.TryGetValue("Tmdb", out var tmdbId))
        {
            var item = FindByProviderId("Tmdb", tmdbId);
            if (item != null)
            {
                logger.LogDebug("Matched '{Title}' via TMDB ID {TmdbId}", plexItem.Title, tmdbId);
                return item.Id;
            }
        }

        if (providerIds.TryGetValue("Tvdb", out var tvdbId))
        {
            var item = FindByProviderId("Tvdb", tvdbId);
            if (item != null)
            {
                logger.LogDebug("Matched '{Title}' via TVDB ID {TvdbId}", plexItem.Title, tvdbId);
                return item.Id;
            }
        }

        logger.LogWarning("Could not match Plex item '{Title}' with GUIDs: {Guids}", plexItem.Title, string.Join(", ", plexItem.Guids.Select(g => g.Id)));
        return null;
    }

    private MediaBrowser.Controller.Entities.BaseItem? FindByProviderId(string providerKey, string providerValue)
    {
        try
        {
            var query = new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                HasAnyProviderId = new Dictionary<string, string>
                {
                    { providerKey, providerValue }
                },
                Limit = 1
            };

            var items = libraryManager.GetItemList(query);
            return items.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error looking up provider ID {Key}={Value}", providerKey, providerValue);
            return null;
        }
    }

    private void MarkItemAsPlayed(Guid itemId, Guid userId, PlexVideo plexItem)
    {
        try
        {
            var user = userManager.GetUserById(userId);
            if (user == null)
            {
                logger.LogDebug("User {UserId} not found, skipping watch mark", userId);
                return;
            }

            var item = libraryManager.GetItemById(itemId);
            if (item == null)
            {
                logger.LogDebug("Item {ItemId} not found in library, skipping watch mark", itemId);
                return;
            }

            var userData = userDataManager.GetUserData(user, item);
            if (userData == null)
            {
                logger.LogDebug("UserData not found for item {ItemId}, skipping watch mark", itemId);
                return;
            }

            userData.Played = true;
            userData.LastPlayedDate = plexItem.LastViewedAt.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(plexItem.LastViewedAt.Value).UtcDateTime
                : DateTime.UtcNow;
            userData.PlayCount = Math.Max(userData.PlayCount, plexItem.ViewCount);

            userDataManager.SaveUserData(user, item, userData, UserDataSaveReason.Import, CancellationToken.None);

            logger.LogDebug("Marked '{Title}' ({ItemId}) as played for user {UserId}", plexItem.Title, itemId, userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark item '{Title}' ({ItemId}) as played", plexItem.Title, itemId);
        }
    }

    private static int ConvertRating(double plexRating)
    {
        var rating = Math.Round(plexRating, MidpointRounding.AwayFromZero);
        return Math.Max(1, Math.Min(10, (int)rating));
    }

    public async Task<(bool success, string message, int libraryCount)> ValidatePlexConnectionAsync(string plexUrl, string plexToken, CancellationToken cancellationToken)
    {
        try
        {
            var libraries = await GetPlexLibrariesAsync(plexUrl, plexToken, cancellationToken).ConfigureAwait(false);
            var movieLibs = libraries.Count(l => l.Type == "movie");
            var showLibs = libraries.Count(l => l.Type == "show");
            return (true, $"Connected. Found {movieLibs} movie and {showLibs} TV show libraries.", libraries.Count);
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}", 0);
        }
    }
}