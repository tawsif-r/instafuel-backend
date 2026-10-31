using System.Security.Claims;
using Instafuel.Api.Authorization;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Modules.Identity;
using Instafuel.Api.Modules.Identity.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Instafuel.Api.Modules.Accounts;

[Authorize]
[Route("api/v1/me")]
public sealed class MyAccountController(IAccountManagementService accounts, IOtpService otpService)
    : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<MyProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var profile = await accounts.GetProfileAsync(GetUserId(), cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPatch]
    public async Task<ActionResult<MyProfileResponse>> Patch(PatchMyProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await accounts.UpdateProfileAsync(GetUserId(), request, cancellationToken));

    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.ChangePasswordAsync(GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("phone/change/request")]
    public async Task<ActionResult<SendOtpResponse>> RequestPhoneChange(ChangePhoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await otpService.SendPhoneChangeAsync(GetUserId(), request.Phone, cancellationToken);
        return Accepted(new SendOtpResponse(result.Accepted, result.DevelopmentCode));
    }

    [HttpPost("email/change/request")]
    public async Task<ActionResult<AccountActionResponse>> RequestEmailChange(ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.RequestEmailChangeAsync(GetUserId(), request.Email, cancellationToken);
        return Accepted(new AccountActionResponse(true,
            "If the email address can be used, a verification link will be sent."));
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> Deactivate(DeactivateAccountRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.DeactivateOwnAccountAsync(GetUserId(), request.CurrentPassword, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthorizedException();
    }
}

