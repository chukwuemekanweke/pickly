using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Unhandled exception occurred after the response started for {Path}",
                httpContext.Request.Path);

            return false;
        }

        logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity.Current?.AddException(exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Request failed",
            Detail = "An unexpected error occurred while processing your request. Please try again later."
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });

        return true;
    }
}
