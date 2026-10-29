using Instafuel.Api.Modules.Identity.DTOs;

namespace Instafuel.Api.Modules.Identity;

public interface IOtpService
{
    Task<OtpSendResult> SendAsync(SendOtpRequest request, CancellationToken cancellationToken);
    Task<OtpSendResult> SendPhoneChangeAsync(Guid userId, string phone, CancellationToken cancellationToken);
    Task<VerifyOtpResponse> VerifyAsync(VerifyOtpRequest request, CancellationToken cancellationToken);
    Task<Guid?> ConsumePasswordResetCodeAsync(string phone, string code, CancellationToken cancellationToken);
}
