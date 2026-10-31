using System.Security.Claims;
using Instafuel.Api.Exceptions;

namespace Instafuel.Api.Modules.Suppliers;

internal static class SupplierControllerSupport
{
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthorizedException();
    }

    public static void ValidateText(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new AppValidationException(field, $"{field} is required and must be at most {maxLength} characters.");
    }
}
