using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Instafuel.Api.Modules.Suppliers;

[Authorize]
[Route("api/v1/supplier/{supplierId:guid}/members")]
public sealed class SupplierMembersController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierMemberResponse>>> GetMembers(Guid supplierId, CancellationToken cancellationToken)
    {
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken);
        var members = await dbContext.SupplierMembers.AsNoTracking().Where(member => member.SupplierId == supplierId)
            .OrderBy(member => member.CreatedAt)
            .Select(member => new SupplierMemberResponse(member.UserId, member.User.FullName,
                member.User.Email, member.MemberRole, member.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(members);
    }

    // Adds an existing account directly. Email invitations are not sent because no email provider is configured.
    [HttpPost]
    public async Task<ActionResult<SupplierMemberResponse>> AddMember(
        Guid supplierId, AddSupplierMemberRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 254
            || !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            throw new AppValidationException(nameof(request.Email), "Provide a valid account email address.");
        if (request.Role is not ("manager" or "staff"))
            throw new AppValidationException(nameof(request.Role), "Member role must be manager or staff.");
        var actorId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, actorId, cancellationToken, "owner");
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Email == email && item.IsActive, cancellationToken);
        if (user is null) throw new NotFoundException("An active account with that email was not found.");
        if (await dbContext.SupplierMembers.AnyAsync(item => item.SupplierId == supplierId && item.UserId == user.Id, cancellationToken))
            throw new ConflictException("This account is already a member of the supplier.");
        var supplierRole = await dbContext.Roles.SingleAsync(role => role.Name == RoleNames.SupplierMember, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.SupplierMembers.Add(new SupplierMember { SupplierId = supplierId, UserId = user.Id, MemberRole = request.Role });
        if (!await dbContext.UserRoles.AnyAsync(link => link.UserId == user.Id && link.RoleId == supplierRole.Id, cancellationToken))
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = supplierRole.Id, GrantedAt = DateTime.UtcNow });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("This account is already a member of the supplier.");
        }
        return Created($"/api/v1/supplier/{supplierId}/members/{user.Id}",
            new SupplierMemberResponse(user.Id, user.FullName, user.Email, request.Role, DateTime.UtcNow));
    }

    [HttpPut("{memberUserId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid supplierId, Guid memberUserId,
        UpdateSupplierMemberRoleRequest request, CancellationToken cancellationToken)
    {
        if (request.Role is not ("manager" or "staff"))
            throw new AppValidationException(nameof(request.Role), "Member role must be manager or staff.");
        var actorId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, actorId, cancellationToken, "owner");
        var member = await dbContext.SupplierMembers.SingleOrDefaultAsync(item =>
            item.SupplierId == supplierId && item.UserId == memberUserId, cancellationToken);
        if (member is null) return NotFound();
        if (member.MemberRole == "owner")
            throw new ConflictException("Owner membership cannot be changed with this endpoint.");
        member.MemberRole = request.Role;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{memberUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid supplierId, Guid memberUserId, CancellationToken cancellationToken)
    {
        var actorId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, actorId, cancellationToken, "owner");
        var member = await dbContext.SupplierMembers.SingleOrDefaultAsync(item =>
            item.SupplierId == supplierId && item.UserId == memberUserId, cancellationToken);
        if (member is null) return NotFound();
        if (member.MemberRole == "owner")
            throw new ConflictException("Owner cannot be removed. Owner transfer workflow is not available yet.");
        dbContext.SupplierMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record AddSupplierMemberRequest([property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(30)] string Role);
public sealed record UpdateSupplierMemberRoleRequest(string Role);
public sealed record SupplierMemberResponse(Guid UserId, string FullName, string? Email, string Role, DateTime CreatedAt);
