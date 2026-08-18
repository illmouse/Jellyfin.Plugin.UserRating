using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Services;

public record PlexDirectory
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}

public record PlexVideo
{
    public string RatingKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Guid { get; init; } = string.Empty;
    public double UserRating { get; init; }
    public int ViewCount { get; init; }
    public long? LastViewedAt { get; init; }
    public List<PlexGuid> Guids { get; init; } = new();
}

public record PlexGuid
{
    public string Id { get; init; } = string.Empty;

    public string Provider => Id.Contains("://") ? Id.Split("://")[0].ToLowerInvariant() : string.Empty;

    public string ExternalId => Id.Contains("://") ? Id.Substring(Id.IndexOf("://") + 3) : string.Empty;

    public string JellyfinProviderKey => Provider switch
    {
        "imdb" => "Imdb",
        "tmdb" => "Tmdb",
        "tvdb" => "Tvdb",
        _ => string.Empty
    };
}

public record ImportProgress
{
    public string OperationId { get; init; } = string.Empty;
    public string Status { get; init; } = "pending";
    public double PercentComplete { get; init; }
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public int ImportedItems { get; init; }
    public int SkippedItems { get; init; }
    public int WatchedItems { get; init; }
    public int UnmatchedItems { get; init; }
    public string CurrentItem { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public List<UnmatchedItem> Unmatched { get; init; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record ImportResult
{
    public bool Success { get; init; }
    public string OperationId { get; init; } = string.Empty;
    public int Imported { get; init; }
    public int Skipped { get; init; }
    public int Watched { get; init; }
    public int Unmatched { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<UnmatchedItem> UnmatchedItems { get; init; } = new();
}

public record UnmatchedItem
{
    public string Title { get; init; } = string.Empty;
    public double PlexRating { get; init; }
    public string PlexType { get; init; } = string.Empty;
    public List<string> Guids { get; init; } = new();
}
