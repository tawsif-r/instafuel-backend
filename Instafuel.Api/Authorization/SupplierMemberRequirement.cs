using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Instafuel.Api.Authorization;

public sealed class SupplierMemberRequirement(int minimumRoleRank = 0) : IAuthorizationRequirement
{
    public int MinimumRoleRank { get; } = minimumRoleRank;
}

public sealed class SupplierMemberAuthorizationHandler(ISupplierMembershipLookup membershipLookup)
    : AuthorizationHandler<SupplierMemberRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SupplierMemberRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub"), out var userId))
            return;

        var supplierId = context.Resource switch
        {
            Guid id => id,
            HttpContext httpContext when Guid.TryParse(
                httpContext.Request.RouteValues["supplierId"]?.ToString(), out var id) => id,
            _ => Guid.Empty
        };

        if (supplierId == Guid.Empty) return;

        var cancellationToken = context.Resource is HttpContext requestContext
            ? requestContext.RequestAborted
            : CancellationToken.None;
        var memberRole = await membershipLookup.GetActiveMemberRoleAsync(supplierId, userId, cancellationToken);
        var roleRank = memberRole switch
        {
            "staff" => 0,
            "manager" => 1,
            "owner" => 2,
            _ => -1
        };
        if (roleRank >= requirement.MinimumRoleRank)
            context.Succeed(requirement);
    }
}
