using MediaBrowser.Model.Plugins;
using Jellyfin.Plugin.UserRatings.Services;

namespace Jellyfin.Plugin.UserRatings.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public int RecentlyRatedItemsCount { get; set; } = 10;

    public string PlexServerUrl { get; set; } = string.Empty;

    public string EncryptedPlexToken { get; set; } = string.Empty;

    public string PlexImportConflictMode { get; set; } = "skip";

    public string SyncConflictMode { get; set; } = "skip";

    public string NewPlexToken { get; set; } = string.Empty;

    public bool EnableAutoSync { get; set; }

    public int SyncIntervalHours { get; set; } = 24;

    public string SyncUserId { get; set; } = string.Empty;

    public bool EnableAutoBackup { get; set; } = true;

    public int BackupIntervalHours { get; set; } = 24;

    public int MaxBackups { get; set; } = 7;

    public string BackupPath { get; set; } = string.Empty;

    [System.Xml.Serialization.XmlIgnore]
    public string PlexToken
    {
        get => string.IsNullOrEmpty(EncryptedPlexToken) ? string.Empty : TokenEncryption.Decrypt(EncryptedPlexToken);
    }
}
