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

public class ImportProgress
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public double PercentComplete { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int ImportedItems { get; set; }
    public int SkippedItems { get; set; }
    public int WatchedItems { get; set; }
    public int UnmatchedItems { get; set; }
    public string CurrentItem { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<UnmatchedItem> Unmatched { get; set; } = new();
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
