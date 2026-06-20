using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.UserRatings.Models
{
    public class UserRating
    {
        public Guid ItemId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; } // 1-5
        public string? Note { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserName { get; set; } // Cached for display
        public Dictionary<string, string> ProviderIds { get; set; } = new(); // Imdb, Tmdb, Tvdb, etc.
    }

    public class RatingStats
    {
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public Dictionary<Guid, UserRating> UserRatings { get; set; } = new();
    }

    public class RatedItemSummary
    {
        public Guid ItemId { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public DateTime LastRated { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public Guid? SeriesId { get; set; }
    }

    public class HealthReport
    {
        public int Ok { get; set; }
        public int Recoverable { get; set; }
        public int Healed { get; set; }
        public int Updated { get; set; }
        public int Stale { get; set; }
        public List<StaleItem> StaleItems { get; set; } = new();
        public List<RecoverableItem> RecoverableItems { get; set; } = new();
        public List<HealedItem> HealedItems { get; set; } = new();
    }

    public class StaleItem
    {
        public Guid ItemId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
        public string? Note { get; set; }
        public Dictionary<string, string> ProviderIds { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class RecoverableItem
    {
        public Guid OldItemId { get; set; }
        public Guid NewItemId { get; set; }
        public string? ItemName { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
        public Dictionary<string, string> ProviderIds { get; set; } = new();
    }

    public class HealedItem
    {
        public Guid OldItemId { get; set; }
        public Guid NewItemId { get; set; }
        public string? ItemName { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
    }

    public class BackupFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
    }
}

