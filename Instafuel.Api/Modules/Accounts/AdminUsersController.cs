using Instafuel.Api.Authorization;
using Instafuel.Api.Controllers;
using Instafuel.Api.Modules.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Instafuel.Api.Modules.Accounts;

[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(IAccountManagementService accounts) : BaseApiController
{
    [HttpPut("{userId:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid userId, SetAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        await accounts.SetAccountActiveAsync(userId, request.IsActive!.Value, cancellationToken);
        return NoContent();
    }
}

public sealed record SetAccountStatusRequest(bool? IsActive);

public sealed class SetAccountStatusRequestValidator : FluentValidation.AbstractValidator<SetAccountStatusRequest>
{
    public SetAccountStatusRequestValidator() => RuleFor(request => request.IsActive).NotNull();
}
