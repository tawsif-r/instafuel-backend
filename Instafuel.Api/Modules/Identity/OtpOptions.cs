namespace Instafuel.Api.Modules.Identity;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";
    public string HashingKey { get; set; } = string.Empty;
}

public sealed class SmsOptions
{
    public const string SectionName = "Sms";
    public string Provider { get; set; } = "Console";
    public string ApiToken { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://smsplus.sslwireless.com/api/v3/send-sms";
}
