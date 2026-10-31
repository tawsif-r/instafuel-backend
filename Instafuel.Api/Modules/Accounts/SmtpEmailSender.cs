using System.Net;
using System.Net.Mail;
using Instafuel.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace Instafuel.Api.Modules.Accounts;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.FromAddress)
            || settings.Port is < 1 or > 65535)
            throw new ServiceUnavailableException("Email delivery is not configured. Contact support or try later.");

        try
        {
            using var message = new MailMessage(settings.FromAddress, recipient, subject, body);
            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = string.IsNullOrWhiteSpace(settings.Username)
                    ? null
                    : new NetworkCredential(settings.Username, settings.Password)
            };
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServiceUnavailableException("Email delivery is temporarily unavailable.");
        }
    }
}
