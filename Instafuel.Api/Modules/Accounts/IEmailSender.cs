namespace Instafuel.Api.Modules.Accounts;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
