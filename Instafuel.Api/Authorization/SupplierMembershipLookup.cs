using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Authorization;

public sealed class SupplierMembershipLookup(PgDbContext dbContext) : ISupplierMembershipLookup
{
    public Task<string?> GetActiveMemberRoleAsync(Guid supplierId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.SupplierMembers.AsNoTracking().Where(member =>
            member.SupplierId == supplierId
            && member.UserId == userId
            && member.User.IsActive
            && member.Supplier.Status == "active")
            .Select(member => member.MemberRole)
            .SingleOrDefaultAsync(cancellationToken);
}
