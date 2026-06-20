using Jellyfin.Plugin.UserRatings.Data;
using Jellyfin.Plugin.UserRatings.Middleware;
using Jellyfin.Plugin.UserRatings.ScheduledTasks;
using Jellyfin.Plugin.UserRatings.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.UserRatings;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<RatingRepository>();
        serviceCollection.AddSingleton<BackupService>();
        serviceCollection.AddSingleton<ProgressTracker>();
        serviceCollection.AddSingleton<PlexImportService>();
        serviceCollection.AddSingleton<RatingResolver>();
        serviceCollection.AddSingleton<HealthCheckService>();
        serviceCollection.AddSingleton<PlexSyncScheduledTask>();
        serviceCollection.AddSingleton<RatingsHealthTask>();
        serviceCollection.AddSingleton<RatingsBackupTask>();
        serviceCollection.AddSingleton<IStartupFilter, ScriptInjectionStartupFilter>();
        }
}
