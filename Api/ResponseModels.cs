using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Api;

public record ApiResponse(bool Success, string? Message = null);

public record UserRatingInfo(string UserId, string? UserName, int Rating, string? Note, DateTime Timestamp);

public record SimpleRatingInfo(string ItemId, int Rating, string? Note, DateTime Timestamp);

public record MyRatingResponse(bool Success, int? Rating, string? Note, DateTime? Timestamp);

public record ItemRatingsResponse(
    bool Success,
    IReadOnlyList<UserRatingInfo> Ratings,
    double AverageRating,
    int TotalRatings
);

public record UserRatingsResponse(bool Success, IReadOnlyList<SimpleRatingInfo> Ratings);

public record RatedItemInfo(string ItemId, double AverageRating, int TotalRatings, DateTime LastRated, string? Name, string? Type, string? SeriesId);

public record RatedItemsResponse(bool Success, IReadOnlyList<RatedItemInfo> Items);

public record WatchedItemInfo(string ItemId, string? Name, string? Type, string? SeriesId, DateTime? LastPlayedDate);

public record UnratedWatchedItemsResponse(bool Success, IReadOnlyList<WatchedItemInfo> Items);

public record HealthReportResponse(
    bool Success,
    int Ok,
    int Recoverable,
    int Healed,
    int Updated,
    int Stale,
    IReadOnlyList<Models.RecoverableItem> RecoverableItems,
    IReadOnlyList<Models.StaleItem> StaleItems,
    IReadOnlyList<Models.HealedItem> HealedItems
);

public record HealResponse(
    bool Success,
    int Ok,
    int Recoverable,
    int Healed,
    int Updated,
    int Stale,
    IReadOnlyList<Models.HealedItem> HealedItems,
    IReadOnlyList<Models.StaleItem> StaleItems,
    string? Message
);

public record ClearStaleResponse(bool Success, string Message, int Removed);

public record CreateBackupResponse(bool Success, string? Message, string? BackupPath, int? TotalBackups);

public record BackupListResponse(bool Success, IReadOnlyList<Models.BackupFileInfo> Backups);

public record StartImportResponse(bool Success, string OperationId);

public record CheckPlexStatusResponse(bool Success, string? Message, int LibraryCount);
