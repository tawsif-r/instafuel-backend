using System.Security.Claims;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Modules.Identity.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Instafuel.Api.Modules.Identity;

public sealed class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly IHostEnvironment _environment;

    public AuthController(IAuthService authService, IOtpService otpService, IHostEnvironment environment)
    {
        _authService = authService;
        _otpService = otpService;
        _environment = environment;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.RegisterAsync(request, cancellationToken));

    [HttpPost("register/rider")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> RegisterRider(
        RegisterRiderRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.RegisterRiderAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.RefreshAsync(request, cancellationToken));

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Logout(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("otp/send")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<SendOtpResponse>> SendOtp(
        SendOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _otpService.SendAsync(request, cancellationToken);
        return Accepted(new SendOtpResponse(result.Accepted,
            _environment.IsDevelopment() ? result.DevelopmentCode : null));
    }

    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<VerifyOtpResponse>> VerifyOtp(
        VerifyOtpRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _otpService.VerifyAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new UnauthorizedException();
        }

        var currentUser = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return currentUser is null ? NotFound() : Ok(currentUser);
    }
}
