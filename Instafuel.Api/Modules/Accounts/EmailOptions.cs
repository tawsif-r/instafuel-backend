namespace Instafuel.Api.Modules.Accounts;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "Smtp";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string? VerificationBaseUrl { get; set; }
}
