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
    public void LoginRequest_RequiresEmailAndPassword()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest(string.Empty, string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Password));
    }
}
