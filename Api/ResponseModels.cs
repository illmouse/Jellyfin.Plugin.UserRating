using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Api;

public record ApiResponse(bool success, string? message = null);

public record UserRatingInfo(string userId, string? userName, int rating, string? note, DateTime timestamp);

public record SimpleRatingInfo(string itemId, int rating, string? note, DateTime timestamp);

public record MyRatingResponse(bool success, int? rating, string? note, DateTime? timestamp);

public record ItemRatingsResponse(
    bool success,
    IReadOnlyList<UserRatingInfo> ratings,
    double averageRating,
    int totalRatings
);

public record UserRatingsResponse(bool success, IReadOnlyList<SimpleRatingInfo> ratings);

public record RatedItemInfo(string itemId, double averageRating, int totalRatings, DateTime lastRated, string? name, string? type, string? seriesId, string? seriesName = null);

public record RatedItemsResponse(bool success, IReadOnlyList<RatedItemInfo> items);

public record RatedItemsPaginatedResponse(bool success, IReadOnlyList<RatedItemInfo> items, int total);

public record WatchedItemInfo(string itemId, string? name, string? type, string? seriesId, DateTime? lastPlayedDate);

public record UnratedWatchedItemsResponse(bool success, IReadOnlyList<WatchedItemInfo> items);

public record RecoverableItemDto(Guid oldItemId, Guid newItemId, string? itemName, Guid userId, int rating, string? note, DateTime timestamp, Dictionary<string, string> providerIds, string matchType, Dictionary<string, string> matchedProviderIds);

public record StaleItemDto(Guid itemId, Guid userId, int rating, string? note, Dictionary<string, string> providerIds, DateTime timestamp);

public record HealedItemDto(Guid oldItemId, Guid newItemId, string? itemName, Guid userId, int rating, string? note, DateTime timestamp, Dictionary<string, string> providerIds);

public record ConflictItemDto(Guid oldItemId, Guid newItemId, string? itemName, Guid userId, int incomingRating, int existingRating, string? incomingNote, string? existingNote, DateTime incomingTimestamp, DateTime existingTimestamp, string? conflictReason, Dictionary<string, string> providerIds);

public record BackupInfoDto(string fileName, long fileSize, DateTime lastModified, DateTime? parsedTimestamp = null);

public record MarkFavoritesResponse(bool success, int marked, int skipped);

public record HealthReportResponse(
    bool success,
    int ok,
    int recoverable,
    int healed,
    int updated,
    int stale,
    int conflicts,
    IReadOnlyList<RecoverableItemDto> recoverableItems,
    IReadOnlyList<StaleItemDto> staleItems,
    IReadOnlyList<HealedItemDto> healedItems,
    IReadOnlyList<ConflictItemDto> conflictItems
);

public record HealResponse(
    bool success,
    int ok,
    int recoverable,
    int healed,
    int updated,
    int stale,
    int conflicts,
    IReadOnlyList<HealedItemDto> healedItems,
    IReadOnlyList<StaleItemDto> staleItems,
    IReadOnlyList<ConflictItemDto> conflictItems,
    string? message
);

public record ClearStaleResponse(bool success, string message, int removed);

public record HealSingleItemResponse(bool success, string? message, string? result, string? conflictReason = null);

public record CreateBackupResponse(bool success, string? message, string? backupPath, int? totalBackups);

public record BackupListResponse(bool success, IReadOnlyList<BackupInfoDto> backups);

public record StartImportResponse(bool success, string operationId);

public record CheckPlexStatusResponse(bool success, string? message, int libraryCount);

public record MigrationRecordDto(
    string name,
    DateTime date,
    string pluginVersion,
    int resultMigrated,
    int resultSkipped
);

public record RatingAbove5Dto(string itemId, string userId, int rating, string? itemName);

public record VersionEntryDto(string version, DateTime? installedAt);

public record MigrationStatusResponse(
    bool success,
    string currentVersion,
    DateTime? currentVersionInstalledAt,
    IReadOnlyList<VersionEntryDto> versionHistory,
    IReadOnlyList<MigrationRecordDto> migrations,
    int ratingCount,
    int ratingsAbove5Count,
    IReadOnlyList<RatingAbove5Dto> ratingsAbove5
);

public record MigrateResponse(
    bool success,
    int migratedCount,
    int skippedCount,
    string backupPath
);

public record BatchAverageRequest(List<string> itemIds);

public record BatchAverageItem(double averageRating, int totalRatings);

public record BatchAverageResponse(bool success, Dictionary<string, BatchAverageItem> items);
