using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.UserRatings.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Data;

public enum HealConflictResult
{
    Replaced,
    Merged,
    Skipped,
    ConflictSkipped
}

public class RatingRepository
{
    private readonly string _dataPath;
    private Dictionary<string, UserRating> _ratings = new();
    private Dictionary<(string provider, string id, Guid userId), string> _providerIndex = new();
    private Dictionary<Guid, (double Sum, int Count)> _averagesByItem = new();
    private readonly object _lock = new object();
    private readonly ILogger<RatingRepository> _logger;
    private bool _loadFailed;
    private PluginMetadata _metadata = new();
    private bool _metadataWasMissing;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public RatingRepository(IApplicationPaths appPaths, ILogger<RatingRepository> logger)
    {
        _dataPath = Path.Combine(appPaths.PluginConfigurationsPath, "UserRatings", "ratings.json");
        _logger = logger;
        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
        LoadRatings();
    }

    public PluginMetadata Metadata => _metadata;
    public bool MetadataWasMissing => _metadataWasMissing;
    public int RatingCount
    {
        get { lock (_lock) { return _ratings.Count; } }
    }

    public int RatingsAbove5Count
    {
        get { lock (_lock) { return _ratings.Values.Count(r => r.Rating > 5); } }
    }

    public List<UserRating> GetRatingsAbove5()
    {
        lock (_lock)
        {
            return _ratings.Values.Where(r => r.Rating > 5).ToList();
        }
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
                var tmpPath = _dataPath + ".tmp";
                if (File.Exists(tmpPath))
                {
                    _logger.LogWarning("Found stale temporary file {TmpPath}, attempting recovery", tmpPath);
                    try
                    {
                        if (!File.Exists(_dataPath))
                        {
                            File.Move(tmpPath, _dataPath);
                            _logger.LogInformation("Recovered ratings from temporary file");
                        }
                        else
                        {
                            File.Delete(tmpPath);
                            _logger.LogInformation("Deleted stale temporary file (data file exists)");
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to clean up temporary file {TmpPath}", tmpPath);
                    }
                }

                if (!File.Exists(_dataPath))
                {
                    _ratings = new Dictionary<string, UserRating>();
                    _averagesByItem = new Dictionary<Guid, (double Sum, int Count)>();
                    _loadFailed = false;
                    return;
                }

                var json = File.ReadAllText(_dataPath);
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (raw == null)
                {
                    _ratings = new Dictionary<string, UserRating>();
                    _averagesByItem = new Dictionary<Guid, (double Sum, int Count)>();
                    _metadata = new PluginMetadata();
                    _loadFailed = false;
                    return;
                }

                _metadataWasMissing = !raw.ContainsKey("_metadata");

                if (!_metadataWasMissing)
                {
                    try
                    {
                        _metadata = raw["_metadata"].Deserialize<PluginMetadata>() ?? new PluginMetadata();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize _metadata, starting fresh");
                        _metadata = new PluginMetadata();
                        _metadataWasMissing = true;
                    }
                }
                else
                {
                    _metadata = new PluginMetadata();
                }

                var loaded = new Dictionary<string, UserRating>();
                var skipped = 0;

                foreach (var kvp in raw)
                {
                    if (kvp.Key == "_metadata")
                    {
                        continue;
                    }

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

                RebuildProviderIndex();
                RebuildAveragesIndex();

                UpdatePluginVersion();
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
                _averagesByItem = new Dictionary<Guid, (double Sum, int Count)>();
                _loadFailed = true;
            }
        }
    }

    private void RebuildProviderIndex()
    {
        _providerIndex = new Dictionary<(string, string, Guid), string>();
        foreach (var kvp in _ratings)
        {
            IndexProviderIds(kvp.Key, kvp.Value);
        }
    }

    private void RebuildAveragesIndex()
    {
        _averagesByItem = new Dictionary<Guid, (double Sum, int Count)>();
        foreach (var r in _ratings.Values)
        {
            if (_averagesByItem.TryGetValue(r.ItemId, out var entry))
                _averagesByItem[r.ItemId] = (entry.Sum + r.Rating, entry.Count + 1);
            else
                _averagesByItem[r.ItemId] = (r.Rating, 1);
        }
    }

    private void IndexAdd(Guid itemId, int rating)
    {
        if (_averagesByItem.TryGetValue(itemId, out var entry))
            _averagesByItem[itemId] = (entry.Sum + rating, entry.Count + 1);
        else
            _averagesByItem[itemId] = (rating, 1);
    }

    private void IndexRemove(Guid itemId, int rating)
    {
        if (_averagesByItem.TryGetValue(itemId, out var entry))
        {
            if (entry.Count <= 1)
                _averagesByItem.Remove(itemId);
            else
                _averagesByItem[itemId] = (entry.Sum - rating, entry.Count - 1);
        }
    }

    private static (string, string, Guid) NormalizeProviderKey(string provider, string id, Guid userId)
        => (provider.ToLowerInvariant(), id.ToLowerInvariant(), userId);

    private void IndexProviderIds(string key, UserRating rating)
    {
        if (rating.ProviderIds == null) return;
        foreach (var pkv in rating.ProviderIds)
        {
            if (!string.IsNullOrEmpty(pkv.Value))
            {
                _providerIndex[NormalizeProviderKey(pkv.Key, pkv.Value, rating.UserId)] = key;
            }
        }
    }

    private void UnindexProviderIds(UserRating rating)
    {
        if (rating.ProviderIds == null) return;
        foreach (var pkv in rating.ProviderIds)
        {
            if (!string.IsNullOrEmpty(pkv.Value))
            {
                _providerIndex.Remove(NormalizeProviderKey(pkv.Key, pkv.Value, rating.UserId));
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
                using var ms = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

                writer.WriteStartObject();

                writer.WritePropertyName("_metadata");
                JsonSerializer.Serialize(writer, _metadata, MetadataJsonOptions);

                foreach (var (key, rating) in _ratings)
                {
                    writer.WritePropertyName(key);
                    JsonSerializer.Serialize(writer, rating, IndentedJsonOptions);
                }

                writer.WriteEndObject();
                writer.Flush();

                var tmpPath = _dataPath + ".tmp";
                File.WriteAllBytes(tmpPath, ms.ToArray());
                File.Move(tmpPath, _dataPath, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save ratings to {Path}", _dataPath);
            }
        }
    }

    private static string GetCurrentPluginVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
    }

    private void UpdatePluginVersion()
    {
        var runningVersion = GetCurrentPluginVersion();
        var now = DateTime.UtcNow;

        if (string.IsNullOrEmpty(_metadata.CurrentVersion))
        {
            _metadata.CurrentVersion = runningVersion;
            _metadata.CurrentVersionInstalledAt = now;
            SaveRatings();
            return;
        }

        if (_metadata.CurrentVersion != runningVersion)
        {
            _metadata.VersionHistory.Add(new VersionEntry
            {
                Version = _metadata.CurrentVersion,
                InstalledAt = _metadata.CurrentVersionInstalledAt
            });
            _metadata.CurrentVersion = runningVersion;
            _metadata.CurrentVersionInstalledAt = now;
            SaveRatings();
        }
    }

    public (int migrated, int skipped) MigrateTo10StarScale()
    {
        lock (_lock)
        {
            var migrated = 0;
            var skipped = 0;
            var updated = new Dictionary<string, UserRating>(_ratings.Count);

            foreach (var (key, rating) in _ratings)
            {
                if (rating.Rating <= 5)
                {
                    updated[key] = rating with { Rating = rating.Rating * 2 };
                    migrated++;
                }
                else
                {
                    updated[key] = rating;
                    skipped++;
                }
            }

            _ratings = updated;
            RebuildProviderIndex();
            RebuildAveragesIndex();

            _metadata.Migrations.Add(new MigrationRecord
            {
                Name = "To10StarScale",
                Date = DateTime.UtcNow,
                PluginVersion = GetCurrentPluginVersion(),
                ResultMigrated = migrated,
                ResultSkipped = skipped
            });

            SaveRatings();

            _logger.LogInformation(
                "Migration complete: {Migrated} ratings converted (×2), {Skipped} ratings >5 preserved",
                migrated, skipped);

            return (migrated, skipped);
        }
    }

    private static string GetKey(Guid itemId, Guid userId) => $"{itemId}_{userId}";

    public string GetDataPath() => _dataPath;

    public void SaveRating(UserRating rating)
    {
        lock (_lock)
        {
            var key = GetKey(rating.ItemId, rating.UserId);

            if (_ratings.TryGetValue(key, out var existingByKey))
            {
                rating = rating with
                {
                    Timestamp = existingByKey.Timestamp,
                    Note = string.IsNullOrEmpty(rating.Note) ? existingByKey.Note : rating.Note
                };
                UnindexProviderIds(existingByKey);
                IndexRemove(existingByKey.ItemId, existingByKey.Rating);
            }
            else
            {
                var existingByProvider = FindByProviderIdsInternal(rating.UserId, rating.ProviderIds);
                if (existingByProvider != null)
                {
                    rating = rating with
                    {
                        Timestamp = existingByProvider.Timestamp,
                        Note = string.IsNullOrEmpty(rating.Note) ? existingByProvider.Note : rating.Note
                    };
                    var oldKey = GetKey(existingByProvider.ItemId, existingByProvider.UserId);
                    UnindexProviderIds(existingByProvider);
                    IndexRemove(existingByProvider.ItemId, existingByProvider.Rating);
                    _ratings.Remove(oldKey);
                    _logger.LogInformation("Re-keyed rating for user {UserId} from {OldItemId} to {NewItemId} (SaveRating)", rating.UserId, existingByProvider.ItemId, rating.ItemId);
                }
            }

            _ratings[key] = rating;
            IndexProviderIds(key, rating);
            IndexAdd(rating.ItemId, rating.Rating);
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
            if (_ratings.TryGetValue(key, out var existing))
            {
                UnindexProviderIds(existing);
                IndexRemove(existing.ItemId, existing.Rating);
            }
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
            _providerIndex.Clear();
            _averagesByItem.Clear();
            _loadFailed = false;
            SaveRatings();
        }
    }

    public List<RatedItemSummary> GetAllRatedItems()
    {
        lock (_lock)
        {
            var lastRatedByItem = new Dictionary<Guid, DateTime>();
            foreach (var r in _ratings.Values)
            {
                if (!lastRatedByItem.TryGetValue(r.ItemId, out var last) || r.Timestamp > last)
                    lastRatedByItem[r.ItemId] = r.Timestamp;
            }

            return _averagesByItem
                .Select(kv => new RatedItemSummary
                {
                    ItemId = kv.Key,
                    AverageRating = kv.Value.Sum / kv.Value.Count,
                    TotalRatings = kv.Value.Count,
                    LastRated = lastRatedByItem.TryGetValue(kv.Key, out var last) ? last : DateTime.MinValue
                })
                .OrderByDescending(s => s.LastRated)
                .ToList();
        }
    }

    public Dictionary<Guid, (double AverageRating, int TotalRatings)> GetBatchAverages(IEnumerable<Guid> itemIds)
    {
        lock (_lock)
        {
            var result = new Dictionary<Guid, (double AverageRating, int TotalRatings)>();
            foreach (var id in itemIds)
            {
                if (_averagesByItem.TryGetValue(id, out var entry) && entry.Count > 0)
                    result[id] = (entry.Sum / entry.Count, entry.Count);
            }
            return result;
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

                var existingByKey = _ratings.TryGetValue(key, out var byKey) ? byKey : null;
                var existingByProvider = existingByKey == null ? FindByProviderIdsInternal(rating.UserId, rating.ProviderIds) : null;

                switch (conflictMode)
                {
                    case "skip":
                        if (existingByKey != null)
                        {
                            skipped++;
                            continue;
                        }
                        if (existingByProvider != null)
                        {
                            var rekeyed = existingByProvider with { ItemId = rating.ItemId, ProviderIds = rating.ProviderIds };
                            var oldKey = GetKey(existingByProvider.ItemId, existingByProvider.UserId);
                            UnindexProviderIds(existingByProvider);
                            IndexRemove(existingByProvider.ItemId, existingByProvider.Rating);
                            _ratings.Remove(oldKey);
                            _ratings[key] = rekeyed;
                            IndexProviderIds(key, rekeyed);
                            IndexAdd(rekeyed.ItemId, rekeyed.Rating);
                            _logger.LogInformation("Re-keyed rating for user {UserId} from {OldItemId} to {NewItemId} (skip mode, provider match)", rating.UserId, existingByProvider.ItemId, rating.ItemId);
                            imported++;
                            break;
                        }
                        _ratings[key] = rating;
                        IndexProviderIds(key, rating);
                        IndexAdd(rating.ItemId, rating.Rating);
                        imported++;
                        break;

                    case "overwrite":
                        if (existingByKey != null)
                        {
                            UnindexProviderIds(existingByKey);
                            IndexRemove(existingByKey.ItemId, existingByKey.Rating);
                            overwritten++;
                        }
                        else if (existingByProvider != null)
                        {
                            var oldKey = GetKey(existingByProvider.ItemId, existingByProvider.UserId);
                            UnindexProviderIds(existingByProvider);
                            IndexRemove(existingByProvider.ItemId, existingByProvider.Rating);
                            _ratings.Remove(oldKey);
                            overwritten++;
                            _logger.LogInformation("Re-keyed rating for user {UserId} from {OldItemId} to {NewItemId} (overwrite mode, provider match)", rating.UserId, existingByProvider.ItemId, rating.ItemId);
                        }
                        _ratings[key] = rating;
                        IndexProviderIds(key, rating);
                        IndexAdd(rating.ItemId, rating.Rating);
                        imported++;
                        break;

                    case "keepHigher":
                        if (existingByKey != null)
                        {
                            if (rating.Rating > existingByKey.Rating)
                            {
                                UnindexProviderIds(existingByKey);
                                IndexRemove(existingByKey.ItemId, existingByKey.Rating);
                                _ratings[key] = rating;
                                IndexProviderIds(key, rating);
                                IndexAdd(rating.ItemId, rating.Rating);
                                overwritten++;
                                imported++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                        else if (existingByProvider != null)
                        {
                            if (rating.Rating > existingByProvider.Rating)
                            {
                                var oldKey = GetKey(existingByProvider.ItemId, existingByProvider.UserId);
                                UnindexProviderIds(existingByProvider);
                                IndexRemove(existingByProvider.ItemId, existingByProvider.Rating);
                                _ratings.Remove(oldKey);
                                _ratings[key] = rating;
                                IndexProviderIds(key, rating);
                                IndexAdd(rating.ItemId, rating.Rating);
                                overwritten++;
                                imported++;
                                _logger.LogInformation("Re-keyed rating for user {UserId} from {OldItemId} to {NewItemId} (keepHigher mode, provider match)", rating.UserId, existingByProvider.ItemId, rating.ItemId);
                            }
                            else
                            {
                                var rekeyed = existingByProvider with { ItemId = rating.ItemId, ProviderIds = rating.ProviderIds };
                                var oldKey = GetKey(existingByProvider.ItemId, existingByProvider.UserId);
                                UnindexProviderIds(existingByProvider);
                                IndexRemove(existingByProvider.ItemId, existingByProvider.Rating);
                                _ratings.Remove(oldKey);
                                _ratings[key] = rekeyed;
                                IndexProviderIds(key, rekeyed);
                                IndexAdd(rekeyed.ItemId, rekeyed.Rating);
                                _logger.LogInformation("Re-keyed rating for user {UserId} from {OldItemId} to {NewItemId} (keepHigher mode, existing rating kept)", rating.UserId, existingByProvider.ItemId, rating.ItemId);
                                skipped++;
                            }
                        }
                        else
                        {
                            _ratings[key] = rating;
                            IndexProviderIds(key, rating);
                            IndexAdd(rating.ItemId, rating.Rating);
                            imported++;
                        }
                        break;

                    default:
                        if (existingByKey != null)
                        {
                            skipped++;
                            continue;
                        }
                        if (existingByProvider != null)
                        {
                            var rekeyed = existingByProvider with { ItemId = rating.ItemId, ProviderIds = rating.ProviderIds };
                            var oldKey = GetKey(existingByProvider.ItemId, existingByProvider.UserId);
                            UnindexProviderIds(existingByProvider);
                            IndexRemove(existingByProvider.ItemId, existingByProvider.Rating);
                            _ratings.Remove(oldKey);
                            _ratings[key] = rekeyed;
                            IndexProviderIds(key, rekeyed);
                            IndexAdd(rekeyed.ItemId, rekeyed.Rating);
                            _logger.LogInformation("Re-keyed rating for user {UserId} from {OldItemId} to {NewItemId} (default mode, provider match)", rating.UserId, existingByProvider.ItemId, rating.ItemId);
                            imported++;
                            break;
                        }
                        _ratings[key] = rating;
                        IndexProviderIds(key, rating);
                        IndexAdd(rating.ItemId, rating.Rating);
                        imported++;
                        break;
                }
            }

            SaveRatings();
        }

        return (imported, skipped, overwritten);
    }

    private UserRating? FindByProviderIdsInternal(Guid userId, Dictionary<string, string>? providerIds)
    {
        if (providerIds == null || providerIds.Count == 0) return null;

        foreach (var kvp in providerIds)
        {
            if (string.IsNullOrEmpty(kvp.Value)) continue;
            if (_providerIndex.TryGetValue(NormalizeProviderKey(kvp.Key, kvp.Value, userId), out var key))
            {
                if (_ratings.TryGetValue(key, out var rating))
                {
                    return rating;
                }
            }
        }

        return null;
    }

    public UserRating? FindByProviderIds(Guid userId, Dictionary<string, string> providerIds)
    {
        lock (_lock)
        {
            return FindByProviderIdsInternal(userId, providerIds);
        }
    }

    public HealConflictResult RepairRatingKey(Guid oldItemId, Guid newItemId, Guid userId, string conflictMode = "skip")
    {
        lock (_lock)
        {
            var oldKey = GetKey(oldItemId, userId);
            if (!_ratings.TryGetValue(oldKey, out var incomingRating))
            {
                return HealConflictResult.Skipped;
            }

            var newKey = GetKey(newItemId, userId);
            var incomingSource = string.IsNullOrEmpty(incomingRating.Source) ? "jellyfin" : incomingRating.Source;

            if (_ratings.TryGetValue(newKey, out var existingRating))
            {
                // Conflict: existing rating at newKey — resolve based on conflictMode
                var existingSource = string.IsNullOrEmpty(existingRating.Source) ? "jellyfin" : existingRating.Source;

                switch (conflictMode)
                {
                    case "overwrite":
                        // Incoming always wins
                        UnindexProviderIds(existingRating);
                        IndexRemove(existingRating.ItemId, existingRating.Rating);
                        break;

                    case "keepHigher":
                        if (incomingRating.Rating > existingRating.Rating)
                        {
                            UnindexProviderIds(existingRating);
                            IndexRemove(existingRating.ItemId, existingRating.Rating);
                        }
                        else if (incomingRating.Rating < existingRating.Rating)
                        {
                            _logger.LogInformation(
                                "Heal conflict at {NewKey}: kept existing rating {ExistingRating} over incoming {IncomingRating} (keepHigher) — incoming preserved at old key",
                                newKey, existingRating.Rating, incomingRating.Rating);
                            return HealConflictResult.ConflictSkipped;
                        }
                        // Equal ratings: fall through to keep newer (by Timestamp)
                        if (incomingRating.Timestamp > existingRating.Timestamp)
                        {
                            UnindexProviderIds(existingRating);
                            IndexRemove(existingRating.ItemId, existingRating.Rating);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Heal conflict at {NewKey}: kept existing rating (keepHigher, equal ratings, existing is newer) — incoming preserved at old key",
                                newKey);
                            return HealConflictResult.ConflictSkipped;
                        }
                        break;

                    case "skip":
                    default:
                        if (existingSource == "jellyfin" && incomingSource == "plex")
                        {
                            _logger.LogInformation(
                                "Heal conflict at {NewKey}: kept existing JF rating {ExistingRating} over Plex rating {IncomingRating} (skip) — incoming preserved at old key",
                                newKey, existingRating.Rating, incomingRating.Rating);
                            return HealConflictResult.ConflictSkipped;
                        }
                        else if (existingSource == "plex" && incomingSource == "jellyfin")
                        {
                            UnindexProviderIds(existingRating);
                            IndexRemove(existingRating.ItemId, existingRating.Rating);
                        }
                        else
                        {
                            if (incomingRating.Timestamp > existingRating.Timestamp)
                            {
                                UnindexProviderIds(existingRating);
                                IndexRemove(existingRating.ItemId, existingRating.Rating);
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "Heal conflict at {NewKey}: kept existing rating (skip, same source, existing is newer) — incoming preserved at old key",
                                    newKey);
                                return HealConflictResult.ConflictSkipped;
                            }
                        }
                        break;
                }

                _logger.LogInformation(
                    "Heal conflict at {NewKey}: resolved via {Mode} (incoming source={IncomingSource}, existing source={ExistingSource})",
                    newKey, conflictMode, incomingSource, existingSource);
            }

            var updated = incomingRating with { ItemId = newItemId };
            UnindexProviderIds(incomingRating);
            IndexRemove(oldItemId, incomingRating.Rating);
            _ratings.Remove(oldKey);
            _ratings[newKey] = updated;
            IndexProviderIds(newKey, updated);
            IndexAdd(newItemId, incomingRating.Rating);
            SaveRatings();
            return HealConflictResult.Replaced;
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