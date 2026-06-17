using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.ScheduledTasks;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.UserRatings
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<RatingRepository>();
            serviceCollection.AddSingleton<ProgressTracker>();
            serviceCollection.AddSingleton<PlexImportService>();
            serviceCollection.AddSingleton<PlexSyncScheduledTask>();
        }
    }
}