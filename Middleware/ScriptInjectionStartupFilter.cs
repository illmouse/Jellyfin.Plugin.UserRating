using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Jellyfin.Plugin.UserRatings.Middleware;

namespace Jellyfin.Plugin.UserRatings.Middleware;

public class ScriptInjectionStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<ScriptInjectionMiddleware>();
            next(app);
        };
    }
}