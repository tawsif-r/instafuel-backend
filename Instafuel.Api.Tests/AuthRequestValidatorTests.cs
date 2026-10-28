using Instafuel.Api.Modules.Identity;
using Instafuel.Api.Modules.Identity.DTOs;

namespace Instafuel.Api.Tests;

public class AuthRequestValidatorTests
{
    [Fact]
    public void RegisterRequest_AcceptsValidInput()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest(
            "Alex Fuel",
            "alex@example.com",
            "Long-Enough-Password-123",
            "+8801700000000"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null, "+880 1700-000-000", true)]
    [InlineData("alex@example.com", null, true)]
    [InlineData("alex@example.com", "+8801700000000", true)]
    [InlineData(null, null, false)]
    [InlineData("", "-------", false)]
    public void RegisterRequest_RequiresAtLeastOneValidContact(string? email, string? phone, bool expectedValid)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest(
            "Alex Fuel", email, "Long-Enough-Password-123", phone));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void RegisterRequest_RejectsInvalidEmailAndShortPassword()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest(
            "Alex Fuel",
            "not-an-email",
            "short",
            null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void LoginRequest_RequiresContactAndPassword()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest(string.Empty, string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Contact");
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Theory]
    [InlineData("alex@example.com", null, true)]
    [InlineData(null, "+8801700000000", true)]
    [InlineData("alex@example.com", "+8801700000000", false)]
    [InlineData(null, null, false)]
    public void LoginRequest_RequiresExactlyOneContact(string? email, string? phone, bool expectedValid)
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest(email, "Long-Enough-Password-123", phone));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void AuthContactNormalizer_CanonicalizesEmailAndPhoneFormatting()
    {
        Assert.Equal("alex@example.com", AuthContactNormalizer.NormalizeEmail(" Alex@Example.com "));
        Assert.Equal("+8801700000000", AuthContactNormalizer.NormalizePhone(" +880 (1700) 000-000 "));
        Assert.Null(AuthContactNormalizer.NormalizePhone("---"));
    }

    [Fact]
    public void RiderRegistration_AllowsPhoneAndRequiresVehicleDetails()
    {
        var validator = new RegisterRiderRequestValidator();
        var result = validator.Validate(new RegisterRiderRequest(
            "Rider Name", null, "Long-Enough-Password-123", "+8801700000000", "", ""));

        Assert.True(result.Errors.All(error => error.PropertyName != "Contact"));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRiderRequest.VehicleType));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterRiderRequest.VehicleRegistrationNumber));
    }

    [Fact]
    public void RiderRegistration_RejectsMissingContact()
    {
        var result = new RegisterRiderRequestValidator().Validate(new RegisterRiderRequest(
            "Rider Name", null, "Long-Enough-Password-123", null, "Motorbike", "DHA-1234"));

        Assert.Contains(result.Errors, error => error.PropertyName == "Contact");
    }
}
