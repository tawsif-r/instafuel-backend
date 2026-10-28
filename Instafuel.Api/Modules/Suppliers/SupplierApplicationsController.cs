using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Suppliers;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/supplier/applications")]
public sealed class SupplierApplicationsController(PgDbContext dbContext) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<SupplierApplicationResponse>> Apply(
        SupplierApplicationRequest request, CancellationToken cancellationToken)
    {
        SupplierControllerSupport.ValidateText(request.LegalName, nameof(request.LegalName), 200);
        SupplierControllerSupport.ValidateText(request.DisplayName, nameof(request.DisplayName), 150);
        var userId = SupplierControllerSupport.GetUserId(User);
        var hasPendingApplication = await dbContext.SupplierMembers.AnyAsync(member => member.UserId == userId
            && member.MemberRole == "owner" && member.Supplier.Status == "pending", cancellationToken);
        if (hasPendingApplication)
            throw new ConflictException("You already have a supplier application awaiting review.");

        var supplierRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == RoleNames.SupplierMember, cancellationToken)
            ?? throw new InvalidOperationException("Supplier-member role has not been seeded.");
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);
        if (user is null) throw new UnauthorizedException();

        var supplier = new Supplier
        {
            LegalName = request.LegalName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Status = "pending",
            Members = [new SupplierMember { UserId = userId, MemberRole = "owner" }]
        };
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Suppliers.Add(supplier);
        if (!await dbContext.UserRoles.AnyAsync(link => link.UserId == userId && link.RoleId == supplierRole.Id, cancellationToken))
            dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = supplierRole.Id, GrantedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Created($"/api/v1/supplier/applications/{supplier.Id}",
            new SupplierApplicationResponse(supplier.Id, supplier.LegalName, supplier.DisplayName, supplier.Status, supplier.CreatedAt));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<SupplierApplicationResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = SupplierControllerSupport.GetUserId(User);
        var applications = await dbContext.SupplierMembers.AsNoTracking()
            .Where(member => member.UserId == userId && member.MemberRole == "owner")
            .OrderByDescending(member => member.Supplier.CreatedAt)
            .Select(member => new SupplierApplicationResponse(member.SupplierId, member.Supplier.LegalName,
                member.Supplier.DisplayName, member.Supplier.Status, member.Supplier.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(applications);
    }
}

[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/suppliers")]
public sealed class AdminSuppliersController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<SupplierApplicationAdminRecord>>> GetPending(CancellationToken cancellationToken)
    {
        var pending = await dbContext.Suppliers.AsNoTracking().Where(item => item.Status == "pending")
            .OrderBy(item => item.CreatedAt)
            .Select(item => new SupplierApplicationAdminRecord(item.Id, item.LegalName, item.DisplayName,
                item.Members.Where(member => member.MemberRole == "owner").Select(member => member.User.FullName).FirstOrDefault(),
                item.Members.Where(member => member.MemberRole == "owner").Select(member => member.User.Email).FirstOrDefault(),
                item.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(pending);
    }

    [HttpPut("{supplierId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid supplierId, SupplierStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not ("active" or "rejected" or "suspended"))
            throw new AppValidationException(nameof(request.Status), "Status must be active, rejected, or suspended.");
        var supplier = await dbContext.Suppliers.SingleOrDefaultAsync(item => item.Id == supplierId, cancellationToken);
        if (supplier is null) return NotFound();
        if (supplier.Status is "rejected" or "suspended" && request.Status == "active")
            throw new ConflictException("Rejected or suspended supplier accounts require a new application/review before activation.");
        if (request.Status is "rejected" or "suspended")
        {
            var hasOrdersInSupplierQueue = await dbContext.SupplierOrders.AnyAsync(item => item.SupplierId == supplierId
                && (item.Status == "pending" || item.Status == "accepted" || item.Status == "preparing"), cancellationToken);
            if (hasOrdersInSupplierQueue)
                throw new ConflictException("Supplier cannot be rejected or suspended while orders are awaiting supplier action.");
        }
        supplier.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record SupplierApplicationRequest([property: Required, StringLength(200)] string LegalName,
    [property: Required, StringLength(150)] string DisplayName);
public sealed record SupplierApplicationResponse(Guid SupplierId, string LegalName, string DisplayName, string Status, DateTime CreatedAt);
public sealed record SupplierApplicationAdminRecord(Guid SupplierId, string LegalName, string DisplayName,
    string? OwnerName, string? OwnerEmail, DateTime SubmittedAt);
public sealed record SupplierStatusRequest(string Status);
