using Instafuel.Api.Controllers;
using Instafuel.Api.Modules.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Instafuel.Api.Modules.Accounts;

[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AccountRecoveryController(IAccountManagementService accounts) : BaseApiController
{
    [HttpPost("password/reset/email/request")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AccountActionResponse>> RequestEmailPasswordReset(
        RequestEmailPasswordResetRequest request, CancellationToken cancellationToken)
    {
        await accounts.RequestEmailPasswordResetAsync(request.Email, cancellationToken);
        return Accepted(new AccountActionResponse(true,
            "If the address is registered and verified, a password reset link will be sent."));
    }

    [HttpPost("password/reset/email/confirm")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPasswordWithEmail(ResetPasswordWithEmailRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.ResetPasswordWithEmailAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("password/reset")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordWithOtpRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.ResetPasswordWithOtpAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("email/change/verify")]
    public async Task<IActionResult> VerifyEmailChange(VerifyEmailChangeRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.VerifyEmailChangeAsync(request.Token, cancellationToken);
        return NoContent();
    }
}

public sealed record VerifyEmailChangeRequest(string Token);
