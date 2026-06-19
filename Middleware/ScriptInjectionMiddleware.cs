using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UserRatings.Middleware;

public class ScriptInjectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ScriptInjectionMiddleware> _logger;
    private const string ScriptTag = "<script plugin=\"UserRatings\" src=\"/web/ConfigurationPage?name=ratings.js\"></script>";
    private const string BodyClosingTag = "</body>";
    private const string InjectMarker = "plugin=\"UserRatings\"";

    public ScriptInjectionMiddleware(RequestDelegate next, ILogger<ScriptInjectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path == null || !IsIndexHtmlRequest(path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context).ConfigureAwait(false);

        responseBody.Seek(0, SeekOrigin.Begin);

        var contentType = context.Response.ContentType ?? string.Empty;
        if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            context.Response.Body = originalBodyStream;
            return;
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        var originalContent = await ReadStreamAsync(responseBody).ConfigureAwait(false);

        if (string.IsNullOrEmpty(originalContent))
        {
            context.Response.Body = originalBodyStream;
            return;
        }

        if (originalContent.Contains(InjectMarker, StringComparison.Ordinal))
        {
            context.Response.Body = originalBodyStream;
            var bytes = Encoding.UTF8.GetBytes(originalContent);
            context.Response.ContentLength = bytes.Length;
            await originalBodyStream.WriteAsync(bytes).ConfigureAwait(false);
            return;
        }

        var modifiedContent = InjectScript(originalContent);
        context.Response.Body = originalBodyStream;

        var modifiedBytes = Encoding.UTF8.GetBytes(modifiedContent);
        context.Response.ContentLength = modifiedBytes.Length;
        await originalBodyStream.WriteAsync(modifiedBytes).ConfigureAwait(false);

        _logger.LogInformation("Injected User Ratings script into index.html response");
    }

    private static bool IsIndexHtmlRequest(string path)
    {
        if (path.Length > 0 && path[0] == '/')
        {
            path = path.TrimEnd('/');
        }

        return string.Equals(path, "/web/index.html", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/web", StringComparison.OrdinalIgnoreCase);
    }

    private static string InjectScript(string html)
    {
        var insertPoint = html.LastIndexOf(BodyClosingTag, StringComparison.OrdinalIgnoreCase);
        if (insertPoint == -1)
        {
            return html + ScriptTag;
        }

        return html.Insert(insertPoint, ScriptTag);
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }
}