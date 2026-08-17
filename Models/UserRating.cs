using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Models;

public record UserRating
{
    public Guid ItemId { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; } // 1-10 (half-star scale)
    public string? Note { get; init; }
    public DateTime Timestamp { get; init; }
    public string? UserName { get; init; } // Cached for display
    public Dictionary<string, string> ProviderIds { get; init; } = new(); // Imdb, Tmdb, Tvdb, etc.
    public string? Source { get; init; }  // "jellyfin" or "plex", null defaults to "jellyfin"
}

public record RatingStats
{
    public double AverageRating { get; init; }
    public int TotalRatings { get; init; }
    public Dictionary<Guid, UserRating> UserRatings { get; init; } = new();
}

public record RatedItemSummary
{
    public Guid ItemId { get; init; }
    public double AverageRating { get; init; }
    public int TotalRatings { get; init; }
    public DateTime LastRated { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public Guid? SeriesId { get; init; }
}

public record HealthReport
{
    public int Ok { get; set; }
    public int Recoverable { get; set; }
    public int Healed { get; set; }
    public int Updated { get; set; }
    public int Stale { get; set; }
    public int Conflicts { get; set; }
    public List<StaleItem> StaleItems { get; init; } = new();
    public List<RecoverableItem> RecoverableItems { get; init; } = new();
    public List<HealedItem> HealedItems { get; init; } = new();
    public List<ConflictItem> ConflictItems { get; init; } = new();
}

public record StaleItem
{
    public Guid ItemId { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; }
    public string? Note { get; init; }
    public Dictionary<string, string> ProviderIds { get; init; } = new();
    public DateTime Timestamp { get; init; }
}

public record RecoverableItem
{
    public Guid OldItemId { get; init; }
    public Guid NewItemId { get; init; }
    public string? ItemName { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; }
    public string? Note { get; init; }
    public DateTime Timestamp { get; init; }
    public Dictionary<string, string> ProviderIds { get; init; } = new();
    public string MatchType { get; init; } = "specific"; // "specific" or "collection"
    public Dictionary<string, string> MatchedProviderIds { get; init; } = new();
}

public record HealedItem
{
    public Guid OldItemId { get; init; }
    public Guid NewItemId { get; init; }
    public string? ItemName { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; }
    public string? Note { get; init; }
    public DateTime Timestamp { get; init; }
    public Dictionary<string, string> ProviderIds { get; init; } = new();
}

public record ConflictItem
{
    public Guid OldItemId { get; init; }
    public Guid NewItemId { get; init; }
    public string? ItemName { get; init; }
    public Guid UserId { get; init; }
    public int IncomingRating { get; init; }
    public int ExistingRating { get; init; }
    public string? IncomingNote { get; init; }
    public string? ExistingNote { get; init; }
    public DateTime IncomingTimestamp { get; init; }
    public DateTime ExistingTimestamp { get; init; }
    public string? ConflictReason { get; init; }
    public Dictionary<string, string> ProviderIds { get; init; } = new();
}

public record BackupFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime LastModified { get; init; }
    public DateTime? ParsedTimestamp { get; init; }
}
