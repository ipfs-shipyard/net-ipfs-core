using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ipfs.Server.HttpApi.V0;

/// <summary>
///     Handles exceptions thrown by a controller.
/// </summary>
/// <remarks>
///     Returns a <see cref="ApiError" /> to the caller.
/// </remarks>
public class ApiExceptionFilter : ExceptionFilterAttribute
{
    /// <inheritdoc />
    public override void OnException(ExceptionContext context)
    {
        var statusCode = 500; // Internal Server Error
        var message = context.Exception.Message;
        string[] details = null;

        switch (context.Exception)
        {
            // Map special exceptions to a status code.
            case FormatException:
            // Bad Request
            case KeyNotFoundException:
                statusCode = 400; // Bad Request
                break;
            case TaskCanceledException:
                statusCode = 504; // Gateway Timeout
                message = "The request took too long to process or was cancelled.";
                break;
            case NotImplementedException:
                statusCode = 501; // Not Implemented
                break;
            case TargetInvocationException:
                message = context.Exception.InnerException?.Message;
                break;
        }

        details = statusCode switch
        {
            500 => context.Exception.StackTrace?.Split(Environment.NewLine),
            501 => context.Exception.StackTrace?.Split(Environment.NewLine),
            _ => null
        };

        context.HttpContext.Response.StatusCode = statusCode;
        context.Result = new JsonResult(new ApiError { Message = message, Details = details });

        // Remove any caching headers
        context.HttpContext.Response.Headers.Remove("cache-control");
        context.HttpContext.Response.Headers.Remove("etag");
        context.HttpContext.Response.Headers.Remove("last-modified");

        base.OnException(context);
    }
}