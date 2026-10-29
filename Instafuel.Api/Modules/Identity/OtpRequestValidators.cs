using FluentValidation;
using Instafuel.Api.Modules.Identity.DTOs;

namespace Instafuel.Api.Modules.Identity;

public sealed class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpRequestValidator()
    {
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(30)
            .Matches(@"^\+?[0-9\s().-]{7,30}$");
        RuleFor(request => request.Purpose).Must(IsSupportedPurpose)
            .WithMessage("Purpose must be signup, login, or reset.");
    }

    internal static bool IsSupportedPurpose(string? purpose) => purpose is not null
        && (purpose.Equals("signup", StringComparison.OrdinalIgnoreCase)
            || purpose.Equals("login", StringComparison.OrdinalIgnoreCase)
            || purpose.Equals("reset", StringComparison.OrdinalIgnoreCase));
}

public sealed class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(30)
            .Matches(@"^\+?[0-9\s().-]{7,30}$");
        RuleFor(request => request.Purpose).Must(IsSupportedVerificationPurpose)
            .WithMessage("Purpose must be signup, login, reset, or phone_change.");
        RuleFor(request => request.Code).NotEmpty().Matches("^[0-9]{6}$");
    }

    private static bool IsSupportedVerificationPurpose(string? purpose) =>
        SendOtpRequestValidator.IsSupportedPurpose(purpose)
        || string.Equals(purpose, "phone_change", StringComparison.OrdinalIgnoreCase);
}
