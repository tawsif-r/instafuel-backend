namespace Instafuel.Api.Modules.Accounts;

public sealed class DevelopmentConsoleEmailSender(ILogger<DevelopmentConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Development email to {Recipient}, subject {Subject}: {Body}", recipient, subject, body);
        return Task.CompletedTask;
    }
}
