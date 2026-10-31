namespace Instafuel.Api.Modules.Accounts;

public interface IAccountManagementService
{
    Task<MyProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<MyProfileResponse> UpdateProfileAsync(Guid userId, PatchMyProfileRequest request,
        CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request, CancellationToken cancellationToken);
    Task RequestEmailPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task ResetPasswordWithEmailAsync(ResetPasswordWithEmailRequest request, CancellationToken cancellationToken);
    Task DeactivateOwnAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken);
    Task SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    Task RequestEmailChangeAsync(Guid userId, string email, CancellationToken cancellationToken);
    Task VerifyEmailChangeAsync(string token, CancellationToken cancellationToken);
}
