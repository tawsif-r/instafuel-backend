using FluentValidation;
using Instafuel.Api.Modules.Identity.DTOs;

namespace Instafuel.Api.Modules.Identity;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FullName).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(150);
        RuleFor(request => request.Email).EmailAddress().When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Email).MaximumLength(255);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(request => request.Phone).Matches(@"^\+?[0-9\s().-]{7,30}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.Phone).Must(phone => phone!.Count(character => character is >= '0' and <= '9') >= 7)
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.Phone).MaximumLength(30);
        RuleFor(request => request).Must(request => !string.IsNullOrWhiteSpace(request.Email)
                || !string.IsNullOrWhiteSpace(request.Phone))
            .WithName("Contact").WithMessage("Provide an email address, a phone number, or both.");
        RuleFor(request => request.DeviceInfo).MaximumLength(200);
    }
}

public sealed class RegisterRiderRequestValidator : AbstractValidator<RegisterRiderRequest>
{
    public RegisterRiderRequestValidator()
    {
        RuleFor(request => request.FullName).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(150);
        RuleFor(request => request.Email).EmailAddress().When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Email).MaximumLength(255);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(request => request.Phone).Matches(@"^\+?[0-9\s().-]{7,30}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.Phone).Must(phone => phone!.Count(character => character is >= '0' and <= '9') >= 7)
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.Phone).MaximumLength(30);
        RuleFor(request => request).Must(request => !string.IsNullOrWhiteSpace(request.Email)
                || !string.IsNullOrWhiteSpace(request.Phone))
            .WithName("Contact").WithMessage("Provide an email address, a phone number, or both.");
        RuleFor(request => request.VehicleType).NotEmpty().MaximumLength(50);
        RuleFor(request => request.VehicleRegistrationNumber).NotEmpty().MaximumLength(50);
        RuleFor(request => request.DeviceInfo).MaximumLength(200);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).EmailAddress().When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Email).MaximumLength(255);
        RuleFor(request => request.Phone).Matches(@"^\+?[0-9\s().-]{7,30}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.Phone).Must(phone => phone!.Count(character => character is >= '0' and <= '9') >= 7)
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        RuleFor(request => request.Phone).MaximumLength(30);
        RuleFor(request => request).Must(request => string.IsNullOrWhiteSpace(request.Email)
                != string.IsNullOrWhiteSpace(request.Phone))
            .WithName("Contact").WithMessage("Provide exactly one of email or phone to sign in.");
        RuleFor(request => request.Password).NotEmpty();
        RuleFor(request => request.DeviceInfo).MaximumLength(200);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(256);
        RuleFor(request => request.DeviceInfo).MaximumLength(200);
    }
}
