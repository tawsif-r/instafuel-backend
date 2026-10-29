namespace Instafuel.Api.Modules.Identity.DTOs;

public sealed record SendOtpRequest(string Phone, string Purpose = "signup");
public sealed record VerifyOtpRequest(string Phone, string Purpose, string Code);
public sealed record SendOtpResponse(bool Accepted, string? DevelopmentCode = null);
public sealed record VerifyOtpResponse(bool Verified, string Purpose);

public sealed record OtpSendResult(bool Accepted, string? DevelopmentCode);
