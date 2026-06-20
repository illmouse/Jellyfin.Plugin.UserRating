using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jellyfin.Plugin.UserRatings.Api;

public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException)
        {
            return Task.CompletedTask;
        }

        context.Result = new ObjectResult(new ApiResponse(false, context.Exception.Message))
        {
            StatusCode = 500
        };
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
