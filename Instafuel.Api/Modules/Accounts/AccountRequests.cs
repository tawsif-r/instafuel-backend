using FluentValidation;

namespace Instafuel.Api.Modules.Accounts;

public sealed record PatchMyProfileRequest(string FullName);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ResetPasswordWithOtpRequest(string Phone, string Code, string NewPassword);
public sealed record RequestEmailPasswordResetRequest(string Email);
public sealed record ResetPasswordWithEmailRequest(string Token, string NewPassword);
public sealed record DeactivateAccountRequest(string CurrentPassword);
public sealed record ChangeEmailRequest(string Email);
public sealed record ChangePhoneRequest(string Phone);
public sealed record MyProfileResponse(Guid Id, string FullName, string? Email, bool EmailVerified,
    string? Phone, bool PhoneVerified, IReadOnlyList<string> Roles, DateTime CreatedAt);
public sealed record AccountActionResponse(bool Accepted, string Message);

public sealed class PatchMyProfileRequestValidator : AbstractValidator<PatchMyProfileRequest>
{
    public PatchMyProfileRequestValidator() => RuleFor(request => request.FullName)
        .NotEmpty().MaximumLength(150);
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(request => request.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128)
            .NotEqual(request => request.CurrentPassword)
            .WithMessage("The new password must differ from the current password.");
    }
}

public sealed class ResetPasswordWithOtpRequestValidator : AbstractValidator<ResetPasswordWithOtpRequest>
{
    public ResetPasswordWithOtpRequestValidator()
    {
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(30)
            .Matches(@"^\+?[0-9\s().-]{7,30}$");
        RuleFor(request => request.Code).NotEmpty().Matches("^[0-9]{6}$");
        RuleFor(request => request.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128);
    }
}

public sealed class RequestEmailPasswordResetRequestValidator : AbstractValidator<RequestEmailPasswordResetRequest>
{
    public RequestEmailPasswordResetRequestValidator() => RuleFor(request => request.Email)
        .NotEmpty().EmailAddress().MaximumLength(255);
}

public sealed class ResetPasswordWithEmailRequestValidator : AbstractValidator<ResetPasswordWithEmailRequest>
{
    public ResetPasswordWithEmailRequestValidator()
    {
        RuleFor(request => request.Token).NotEmpty().MaximumLength(128);
        RuleFor(request => request.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128);
    }
}

public sealed class DeactivateAccountRequestValidator : AbstractValidator<DeactivateAccountRequest>
{
    public DeactivateAccountRequestValidator() => RuleFor(request => request.CurrentPassword)
        .NotEmpty().MaximumLength(128);
}

public sealed class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailRequestValidator() => RuleFor(request => request.Email)
        .NotEmpty().EmailAddress().MaximumLength(255);
}

public sealed class ChangePhoneRequestValidator : AbstractValidator<ChangePhoneRequest>
{
    public ChangePhoneRequestValidator() => RuleFor(request => request.Phone)
        .NotEmpty().MaximumLength(30).Matches(@"^\+?[0-9\s().-]{7,30}$");
}

public sealed class VerifyEmailChangeRequestValidator : AbstractValidator<VerifyEmailChangeRequest>
{
    public VerifyEmailChangeRequestValidator() => RuleFor(request => request.Token)
        .NotEmpty().MaximumLength(128);
}
