namespace Instafuel.Api.Authorization;

public interface ISupplierMembershipLookup
{
    Task<string?> GetActiveMemberRoleAsync(Guid supplierId, Guid userId, CancellationToken cancellationToken);
}
