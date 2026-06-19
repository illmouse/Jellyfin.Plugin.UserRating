using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.UserRatings.Configuration;
using Jellyfin.Plugin.UserRatings.Services;

namespace Jellyfin.Plugin.UserRatings
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILogger<Plugin> _logger;

        public override string Name => "User Ratings";

        public override Guid Id => Guid.Parse("b8c5d3e7-4f6a-8b9c-1d2e-3f4a5b6c7d8e");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _logger = logger;
            _logger.LogInformation("User Ratings plugin loaded. Script injection handled by middleware.");
        }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var newConfig = (PluginConfiguration)configuration;

            if (!string.IsNullOrEmpty(newConfig.NewPlexToken))
            {
                newConfig.EncryptedPlexToken = TokenEncryption.Encrypt(newConfig.NewPlexToken);
                newConfig.NewPlexToken = string.Empty;
            }

            base.UpdateConfiguration(configuration);
        }

        public static Plugin? Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                },
                new PluginPageInfo
                {
                    Name = "ratings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.ratings.js"
                }
            };
        }
    }
}

