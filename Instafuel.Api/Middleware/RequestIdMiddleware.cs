using Serilog.Context;

namespace Instafuel.Api.Middleware;

public sealed class RequestIdMiddleware
{
    private const string HeaderName = "X-Request-Id";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incomingId = context.Request.Headers[HeaderName].FirstOrDefault();

        var requestId = IsValidRequestId(incomingId)
            ? incomingId!
            : Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;
        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }

    private static bool IsValidRequestId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= 128
            && value.All(c => c >= 0x21 && c <= 0x7E);
    }
}
