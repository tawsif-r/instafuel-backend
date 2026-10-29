using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Instafuel.Api.Models;

namespace Instafuel.Api.Authorization;

public sealed class ConcealSupplierAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context,
        AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var failedRequirements = authorizeResult.AuthorizationFailure?.FailedRequirements ?? [];
        var supplierMembershipDenied = failedRequirements
            .OfType<SupplierMemberRequirement>()
            .Any(requirement => requirement.MinimumRoleRank == 0);
        var supplierRoleDenied = failedRequirements
            .OfType<RolesAuthorizationRequirement>()
            .Any(requirement => requirement.AllowedRoles.Contains(RoleNames.SupplierMember,
                StringComparer.Ordinal));

        if (authorizeResult.Forbidden
            && context.User.Identity?.IsAuthenticated == true
            && (supplierMembershipDenied || supplierRoleDenied)
            && context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Any(data => string.Equals(data.Policy, AuthorizationPolicies.SupplierMember,
                    StringComparison.Ordinal)) == true)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                title = "Not Found",
                status = StatusCodes.Status404NotFound
            }, context.RequestAborted);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
