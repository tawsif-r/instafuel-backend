using System.Security.Claims;
using Instafuel.Api.Exceptions;

namespace Instafuel.Api.Modules.Customer;

internal static class CustomerControllerSupport
{
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthorizedException();
    }
}
