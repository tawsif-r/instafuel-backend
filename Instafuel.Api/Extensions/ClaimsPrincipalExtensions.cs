using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Instafuel.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The logged-in user's id from the JWT "sub" claim. Only call this inside
    // [Authorize] actions, where the token has already been validated.
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new InvalidOperationException("Token has no sub claim."));
}
