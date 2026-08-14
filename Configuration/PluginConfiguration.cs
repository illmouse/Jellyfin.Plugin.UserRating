using MediaBrowser.Model.Plugins;
using Jellyfin.Plugin.UserRatings.Services;

namespace Jellyfin.Plugin.UserRatings.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public int RecentlyRatedItemsCount { get; set; } = 10;

    public bool ShowAverageRatingBadge { get; set; } = true;

    public bool ShowPersonalRatingBadge { get; set; } = true;

    public string AverageBadgePosition { get; set; } = "top-left";

    public string PersonalBadgePosition { get; set; } = "top-left";

    public string PlexServerUrl { get; set; } = string.Empty;

    public string EncryptedPlexToken { get; set; } = string.Empty;

    public string PlexImportConflictMode { get; set; } = "skip";

    public string SyncConflictMode { get; set; } = "skip";

    public string HealingConflictMode { get; set; } = "skip";

    public bool EnablePlexWatchHistorySync { get; set; }

    public bool EnablePlexRatingSync { get; set; } = true;

    public string NewPlexToken { get; set; } = string.Empty;

    public bool EnableAutoSync { get; set; }

    public int SyncIntervalHours { get; set; } = 24;

    public string SyncUserId { get; set; } = string.Empty;

    public bool EnableAutoBackup { get; set; } = true;

    public int BackupIntervalHours { get; set; } = 24;

    public int MaxBackups { get; set; } = 7;

    public string BackupPath { get; set; } = string.Empty;

    public int HealthCheckIntervalMinutes { get; set; } = 30;

    public bool EnableAutoHealthCheck { get; set; } = true;

    public int FavoriteThreshold { get; set; } = 9;

    [System.Xml.Serialization.XmlIgnore]
    public string PlexToken
    {
        get => string.IsNullOrEmpty(EncryptedPlexToken) ? string.Empty : TokenEncryption.Decrypt(EncryptedPlexToken);
    }
}
