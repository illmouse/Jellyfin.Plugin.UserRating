using System;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.UserRatings.Api;

public static class AuthHelper
{
    public static async Task<Guid> GetAuthenticatedUserIdAsync(this HttpContext httpContext, IAuthorizationContext authContext)
    {
        var authInfo = await authContext.GetAuthorizationInfo(httpContext.Request).ConfigureAwait(false);
        return authInfo.UserId;
    }

    public static async Task<string> GetAuthenticatedUserNameAsync(this HttpContext httpContext, IAuthorizationContext authContext)
    {
        var authInfo = await authContext.GetAuthorizationInfo(httpContext.Request).ConfigureAwait(false);
        return authInfo.User?.Name ?? "Unknown";
    }

    public static async Task<bool> IsAdminAsync(this HttpContext httpContext, IAuthorizationContext authContext)
    {
        var authInfo = await authContext.GetAuthorizationInfo(httpContext.Request).ConfigureAwait(false);
        return authInfo.User?.HasPermission(PermissionKind.IsAdministrator) == true;
    }

    public static async Task<bool> IsApiKeyAsync(this HttpContext httpContext, IAuthorizationContext authContext)
    {
        var authInfo = await authContext.GetAuthorizationInfo(httpContext.Request).ConfigureAwait(false);
        return authInfo.IsApiKey;
    }
}