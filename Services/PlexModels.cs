using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Services
{
    public class PlexDirectory
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class PlexVideo
    {
        public string RatingKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
        public double UserRating { get; set; }
        public List<PlexGuid> Guids { get; set; } = new();
    }

    public class PlexGuid
    {
        public string Id { get; set; } = string.Empty;

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
        public int UnmatchedItems { get; set; }
        public string CurrentItem { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<UnmatchedItem> Unmatched { get; set; } = new();
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string OperationId { get; set; } = string.Empty;
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Unmatched { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<UnmatchedItem> UnmatchedItems { get; set; } = new();
    }

    public class UnmatchedItem
    {
        public string Title { get; set; } = string.Empty;
        public double PlexRating { get; set; }
        public string PlexType { get; set; } = string.Empty;
        public List<string> Guids { get; set; } = new();
    }
}