using System.Security.Claims;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Suppliers;

internal static class SupplierControllerSupport
{
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthorizedException();
    }

    public static async Task<SupplierMember> RequireMemberAsync(PgDbContext db, Guid supplierId, Guid userId,
        CancellationToken cancellationToken, params string[] allowedRoles)
    {
        var member = await db.SupplierMembers.Include(item => item.Supplier)
            .SingleOrDefaultAsync(item => item.SupplierId == supplierId && item.UserId == userId, cancellationToken);
        if (member is null) throw new NotFoundException("Supplier account was not found.");
        if (member.Supplier.Status != "active")
            throw new ConflictException("This supplier account is not active.");
        if (allowedRoles.Length > 0 && !allowedRoles.Contains(member.MemberRole, StringComparer.Ordinal))
            throw new ForbiddenException("Your supplier membership does not allow this operation.");
        return member;
    }

    public static void ValidateText(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new AppValidationException(field, $"{field} is required and must be at most {maxLength} characters.");
    }
}
