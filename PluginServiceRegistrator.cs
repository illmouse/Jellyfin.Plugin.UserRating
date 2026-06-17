using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.UserRatings
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<RatingRepository>();
            serviceCollection.AddSingleton<ProgressTracker>();
            serviceCollection.AddSingleton<PlexImportService>();
        }
    }
}