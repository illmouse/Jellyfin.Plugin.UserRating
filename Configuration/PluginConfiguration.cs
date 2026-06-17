using MediaBrowser.Model.Plugins;
using Jellyfin.Plugin.UserRatings.Services;

namespace Jellyfin.Plugin.UserRatings.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int RecentlyRatedItemsCount { get; set; } = 10;

        public string PlexServerUrl { get; set; } = string.Empty;

        public string EncryptedPlexToken { get; set; } = string.Empty;

        public string PlexImportConflictMode { get; set; } = "skip";

        public string NewPlexToken { get; set; } = string.Empty;

        public bool EnableAutoSync { get; set; }

        public int SyncIntervalHours { get; set; } = 24;

        public string SyncUserId { get; set; } = string.Empty;

        [System.Xml.Serialization.XmlIgnore]
        public string PlexToken
        {
            get => string.IsNullOrEmpty(EncryptedPlexToken) ? string.Empty : TokenEncryption.Decrypt(EncryptedPlexToken);
        }
    }
}