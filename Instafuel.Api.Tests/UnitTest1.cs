using System.Text.Json;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Handlers;
using Instafuel.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Instafuel.Api.Tests;

public class GlobalExceptionHandlerTests
{
    private static readonly GlobalExceptionHandler Handler =
        new(NullLogger<GlobalExceptionHandler>.Instance);

    private static async Task<(int Status, string ContentType, string Body)>
        HandleExceptionAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        context.Items["RequestId"] = "test-request-123";

        var handled = await Handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        Assert.True(handled);

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (
            context.Response.StatusCode,
            context.Response.ContentType ?? "",
            body);
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(ForbiddenException), 403)]
    public async Task KnownExceptions_ReturnExpectedStatus(
        Type exceptionType,
        int expectedStatus)
    {
        Exception exception = exceptionType == typeof(NotFoundException)
            ? new NotFoundException("Resource missing.")
            : exceptionType == typeof(ConflictException)
                ? new ConflictException("Resource conflict.")
                : new ForbiddenException();

        var result = await HandleExceptionAsync(exception);

        Assert.Equal(expectedStatus, result.Status);
        Assert.StartsWith("application/problem+json", result.ContentType);

        using var json = JsonDocument.Parse(result.Body);
        Assert.Equal(expectedStatus, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("test-request-123",
            json.RootElement.GetProperty("requestId").GetString());
    }

    [Fact]
    public async Task ValidationException_Returns400WithErrors()
    {
        var exception = new AppValidationException(
            new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be greater than zero."]
            });

        var result = await HandleExceptionAsync(exception);

        Assert.Equal(400, result.Status);

        using var json = JsonDocument.Parse(result.Body);
        Assert.True(json.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("quantity", out _));
    }

    [Fact]
    public async Task UnknownException_Returns500WithoutInternalDetails()
    {
        var result = await HandleExceptionAsync(
            new InvalidOperationException("Sensitive database password details"));

        Assert.Equal(500, result.Status);

        using var json = JsonDocument.Parse(result.Body);
        var body = json.RootElement.GetRawText();

        Assert.Contains("An unexpected error occurred.", body);
        Assert.DoesNotContain("Sensitive database password details", body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
    }
}

public class RequestIdMiddlewareTests
{
    [Fact]
    public async Task IncomingRequestId_IsReturnedInResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-Id"] = "client-request-456";
        context.Response.Body = new MemoryStream();

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("client-request-456",
            context.Response.Headers["X-Request-Id"].ToString());
        Assert.Equal("client-request-456", context.Items["RequestId"]);
        Assert.Equal("client-request-456", context.TraceIdentifier);
    }

    [Fact]
    public async Task MissingRequestId_GeneratesAndReturnsOne()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var requestId = context.Response.Headers["X-Request-Id"].ToString();

        Assert.False(string.IsNullOrWhiteSpace(requestId));
        Assert.Equal(requestId, context.Items["RequestId"]);
        Assert.Equal(requestId, context.TraceIdentifier);
    }

    [Fact]
    public async Task InvalidRequestId_IsReplaced()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-Id"] = "invalid request id with spaces";
        context.Response.Body = new MemoryStream();

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.NotEqual(
            "invalid request id with spaces",
            context.Response.Headers["X-Request-Id"].ToString());
    }
}
