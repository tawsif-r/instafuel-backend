using Instafuel.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Instafuel.Api.Handlers;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            AppValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                exception.Message),

            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message),

            ConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                exception.Message),

            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. RequestId: {RequestId}",
                context.Items["RequestId"]);
        }
        else
        {
            _logger.LogWarning(
                "Request failed with status {StatusCode}: {ErrorType}. RequestId: {RequestId}",
                statusCode,
                exception.GetType().Name,
                context.Items["RequestId"]);
        }

        ProblemDetails problem;

        if (exception is AppValidationException validationException)
        {
            problem = new ValidationProblemDetails(
                validationException.Errors)
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
        }
        else
        {
            problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
        }

        problem.Extensions["requestId"] = context.Items["RequestId"]?.ToString();

        context.Response.StatusCode = statusCode;

        context.Response.ContentType = "application/problem+json";
        await System.Text.Json.JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem,
            problem.GetType(),
            cancellationToken: cancellationToken);

        return true;
    }
}
