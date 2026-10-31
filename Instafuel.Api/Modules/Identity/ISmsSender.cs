namespace Instafuel.Api.Modules.Identity;

public interface ISmsSender
{
    Task SendAsync(string bangladeshMsisdn, string message, CancellationToken cancellationToken);
}
