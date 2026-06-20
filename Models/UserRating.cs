using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Models;

public record UserRating
{
    public Guid ItemId { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; } // 1-5
    public string? Note { get; init; }
    public DateTime Timestamp { get; init; }
    public string? UserName { get; init; } // Cached for display
    public Dictionary<string, string> ProviderIds { get; init; } = new(); // Imdb, Tmdb, Tvdb, etc.
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
    public List<StaleItem> StaleItems { get; init; } = new();
    public List<RecoverableItem> RecoverableItems { get; init; } = new();
    public List<HealedItem> HealedItems { get; init; } = new();
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
    public Dictionary<string, string> ProviderIds { get; init; } = new();
}

public record HealedItem
{
    public Guid OldItemId { get; init; }
    public Guid NewItemId { get; init; }
    public string? ItemName { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; }
}

public record BackupFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime LastModified { get; init; }
}
