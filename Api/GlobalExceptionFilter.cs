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
            context.Result = new StatusCodeResult(499);
            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }

        context.Result = new ObjectResult(new ApiResponse(false, "An unexpected error occurred."))
        {
            StatusCode = 500
        };
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}