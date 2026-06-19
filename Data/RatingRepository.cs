using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Data
{
    public class RatingRepository
    {
        private readonly string _dataPath;
        private Dictionary<string, UserRating> _ratings = new();
        private readonly object _lock = new object();
        private readonly ILogger<RatingRepository> _logger;
        private bool _loadFailed;

        public RatingRepository(IApplicationPaths appPaths, ILogger<RatingRepository> logger)
        {
            _dataPath = Path.Combine(appPaths.PluginConfigurationsPath, "UserRatings", "ratings.json");
            _logger = logger;
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
            LoadRatings();
        }

        public void Reload()
        {
            _loadFailed = false;
            LoadRatings();
        }

        private void LoadRatings()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_dataPath))
                    {
                        _ratings = new Dictionary<string, UserRating>();
                        _loadFailed = false;
                        return;
                    }

                    var json = File.ReadAllText(_dataPath);
                    var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (raw == null)
                    {
                        _ratings = new Dictionary<string, UserRating>();
                        _loadFailed = false;
                        return;
                    }

                    var loaded = new Dictionary<string, UserRating>();
                    var skipped = 0;

                    foreach (var kvp in raw)
                    {
                        try
                        {
                            var rating = kvp.Value.Deserialize<UserRating>();
                            if (rating == null)
                            {
                                skipped++;
                                _logger.LogWarning("Skipping null rating entry with key {Key}", kvp.Key);
                                continue;
                            }

                            var expectedKey = $"{rating.ItemId}_{rating.UserId}";
                            if (kvp.Key != expectedKey)
                            {
                                _logger.LogWarning(
                                    "Key mismatch for rating entry: dictionary key {DictKey} does not match expected {ExpectedKey} (ItemId={ItemId}, UserId={UserId}). Re-keying under correct key.",
                                    kvp.Key, expectedKey, rating.ItemId, rating.UserId);
                            }

                            loaded[expectedKey] = rating;
                        }
                        catch (JsonException ex)
                        {
                            skipped++;
                            _logger.LogWarning(ex, "Skipping malformed rating entry with key {Key}", kvp.Key);
                        }
                    }

                    _ratings = loaded;
                    _loadFailed = false;

                    if (skipped > 0)
                    {
                        _logger.LogWarning("Skipped {Skipped} malformed rating entries during load", skipped);
                    }

                    _logger.LogInformation("Loaded {Count} ratings from {Path}", _ratings.Count, _dataPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load ratings from {Path}", _dataPath);

                    if (File.Exists(_dataPath))
                    {
                        try
                        {
                            var backup = _dataPath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                            File.Copy(_dataPath, backup);
                            _logger.LogInformation("Corrupted ratings file backed up to {BackupPath}", backup);
                        }
                        catch (Exception backupEx)
                        {
                            _logger.LogError(backupEx, "Failed to backup corrupted ratings file");
                        }
                    }

                    _ratings = new Dictionary<string, UserRating>();
                    _loadFailed = true;
                }
            }
        }

        private void SaveRatings()
        {
            if (_loadFailed && _ratings.Count == 0)
            {
                _logger.LogWarning("Skipping save: ratings file failed to load and no ratings in memory — would destroy data");
                return;
            }

            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_ratings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_dataPath, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save ratings to {Path}", _dataPath);
                }
            }
        }

        private static string GetKey(Guid itemId, Guid userId) => $"{itemId}_{userId}";

        public string GetDataPath() => _dataPath;

        public void SaveRating(UserRating rating)
        {
            lock (_lock)
            {
                var key = GetKey(rating.ItemId, rating.UserId);
                _ratings[key] = rating;
                SaveRatings();
            }
        }

        public UserRating? GetRating(Guid itemId, Guid userId)
        {
            lock (_lock)
            {
                var key = GetKey(itemId, userId);
                return _ratings.TryGetValue(key, out var rating) ? rating : null;
            }
        }

        public List<UserRating> GetRatingsForItem(Guid itemId)
        {
            lock (_lock)
            {
                return _ratings.Values
                    .Where(r => r.ItemId == itemId)
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();
            }
        }

        public List<UserRating> GetRatingsForUser(Guid userId)
        {
            lock (_lock)
            {
                return _ratings.Values
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();
            }
        }

        public void DeleteRating(Guid itemId, Guid userId)
        {
            lock (_lock)
            {
                var key = GetKey(itemId, userId);
                _ratings.Remove(key);
                SaveRatings();
            }
        }

        public RatingStats GetStatsForItem(Guid itemId)
        {
            lock (_lock)
            {
                var ratings = _ratings.Values.Where(r => r.ItemId == itemId).ToList();
                
                return new RatingStats
                {
                    AverageRating = ratings.Any() ? ratings.Average(r => r.Rating) : 0,
                    TotalRatings = ratings.Count,
                    UserRatings = ratings.ToDictionary(r => r.UserId, r => r)
                };
            }
        }

        public void DeleteAllRatings()
        {
            lock (_lock)
            {
                _ratings.Clear();
                _loadFailed = false;
                SaveRatings();
            }
        }

        public List<RatedItemSummary> GetAllRatedItems()
        {
            lock (_lock)
            {
                return _ratings.Values
                    .GroupBy(r => r.ItemId)
                    .Select(g => new RatedItemSummary
                    {
                        ItemId = g.Key,
                        AverageRating = g.Average(r => r.Rating),
                        TotalRatings = g.Count(),
                        LastRated = g.Max(r => r.Timestamp)
                    })
                    .OrderByDescending(s => s.LastRated)
                    .ToList();
            }
        }

        public (int imported, int skipped, int overwritten) BulkSaveRatings(IEnumerable<UserRating> ratings, string conflictMode)
        {
            var imported = 0;
            var skipped = 0;
            var overwritten = 0;

            lock (_lock)
            {
                foreach (var rating in ratings)
                {
                    var key = GetKey(rating.ItemId, rating.UserId);

                    switch (conflictMode)
                    {
                        case "skip":
                            if (_ratings.ContainsKey(key))
                            {
                                skipped++;
                                continue;
                            }
                            _ratings[key] = rating;
                            imported++;
                            break;

                        case "overwrite":
                            if (_ratings.ContainsKey(key))
                            {
                                overwritten++;
                            }
                            _ratings[key] = rating;
                            imported++;
                            break;

                        case "keepHigher":
                            if (_ratings.TryGetValue(key, out var existing))
                            {
                                if (rating.Rating > existing.Rating)
                                {
                                    _ratings[key] = rating;
                                    overwritten++;
                                    imported++;
                                }
                                else
                                {
                                    skipped++;
                                }
                            }
                            else
                            {
                                _ratings[key] = rating;
                                imported++;
                            }
                            break;

                        default:
                            if (_ratings.ContainsKey(key))
                            {
                                skipped++;
                                continue;
                            }
                            _ratings[key] = rating;
                            imported++;
                            break;
                    }
                }

                SaveRatings();
            }

            return (imported, skipped, overwritten);
        }

        public UserRating? FindByProviderIds(Guid userId, Dictionary<string, string> providerIds)
        {
            lock (_lock)
            {
                foreach (var rating in _ratings.Values)
                {
                    if (rating.UserId != userId) continue;
                    if (rating.ProviderIds == null || rating.ProviderIds.Count == 0) continue;

                    foreach (var kvp in providerIds)
                    {
                        if (rating.ProviderIds.TryGetValue(kvp.Key, out var value)
                            && !string.IsNullOrEmpty(value)
                            && !string.IsNullOrEmpty(kvp.Value)
                            && string.Equals(value, kvp.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            return rating;
                        }
                    }
                }

                return null;
            }
        }

        public void RepairRatingKey(Guid oldItemId, Guid newItemId, Guid userId)
        {
            lock (_lock)
            {
                var oldKey = GetKey(oldItemId, userId);
                if (_ratings.TryGetValue(oldKey, out var rating))
                {
                    _ratings.Remove(oldKey);
                    rating.ItemId = newItemId;
                    var newKey = GetKey(newItemId, userId);
                    _ratings[newKey] = rating;
                    SaveRatings();
                }
            }
        }

        public Dictionary<string, UserRating> GetAllRatings()
        {
            lock (_lock)
            {
                return new Dictionary<string, UserRating>(_ratings);
            }
        }
    }
}