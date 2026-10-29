namespace Instafuel.Api.Modules.Identity;

public sealed class DevelopmentConsoleSmsSender(ILogger<DevelopmentConsoleSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string bangladeshMsisdn, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development SMS recipient {Phone}: {Message}", bangladeshMsisdn, message);
        return Task.CompletedTask;
    }
}
