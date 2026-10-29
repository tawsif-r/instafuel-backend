using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Instafuel.Api.Authorization;

public static class AuthorizationRegistration
{
    public static IServiceCollection AddInstafuelAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build();
            options.AddPolicy(AuthorizationPolicies.Customer,
                policy => policy.RequireRole(RoleNames.Customer));
            options.AddPolicy(AuthorizationPolicies.Rider,
                policy => policy.RequireRole(RoleNames.Rider));
            options.AddPolicy(AuthorizationPolicies.SupplierMember, policy =>
            {
                policy.RequireRole(RoleNames.SupplierMember);
                policy.AddRequirements(new SupplierMemberRequirement());
            });
            options.AddPolicy(AuthorizationPolicies.SupplierManager, policy =>
            {
                policy.RequireRole(RoleNames.SupplierMember);
                policy.AddRequirements(new SupplierMemberRequirement(minimumRoleRank: 1));
            });
            options.AddPolicy(AuthorizationPolicies.SupplierOwner, policy =>
            {
                policy.RequireRole(RoleNames.SupplierMember);
                policy.AddRequirements(new SupplierMemberRequirement(minimumRoleRank: 2));
            });
            options.AddPolicy(AuthorizationPolicies.SectorSupervisor,
                policy => policy.RequireRole(RoleNames.SectorSupervisor));
            options.AddPolicy(AuthorizationPolicies.Admin,
                policy => policy.RequireRole(RoleNames.Admin, RoleNames.SuperAdmin));
            options.AddPolicy(AuthorizationPolicies.SuperAdmin,
                policy => policy.RequireRole(RoleNames.SuperAdmin));

        });
        services.AddScoped<IAuthorizationHandler, SupplierMemberAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler,
            ConcealSupplierAuthorizationResultHandler>();
        return services;
    }
}
