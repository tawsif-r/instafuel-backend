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
[Route("api/v1/supplier/{supplierId:guid}")]
public sealed class SupplierCatalogController(PgDbContext dbContext) : BaseApiController
{
    private const decimal MaxUnitPrice = 999999999999.99m;
    private const decimal MaxStock = 99999999999.999m;

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<SupplierProductResponse>>> GetProducts(Guid supplierId, CancellationToken cancellationToken)
    {
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken);
        var products = await dbContext.Products.AsNoTracking()
            .Where(product => product.IsActive && product.Category.IsActive)
            .OrderBy(product => product.Category.Name).ThenBy(product => product.Name)
            .Select(product => new SupplierProductResponse(product.Id, product.Name, product.Description,
                product.Category.Name, product.ProductType, product.UnitCode, product.FuelGrade))
            .ToListAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("offers")]
    public async Task<ActionResult<IReadOnlyList<SupplierOfferResponse>>> GetOffers(Guid supplierId, CancellationToken cancellationToken)
    {
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken);
        var offers = await dbContext.SupplierOffers.AsNoTracking().Where(offer => offer.SupplierId == supplierId)
            .OrderBy(offer => offer.Product.Name)
            .Select(offer => new SupplierOfferResponse(offer.Id, offer.ProductId, offer.Product.Name,
                offer.Product.UnitCode, offer.SellerSku, offer.UnitPrice, offer.Currency, offer.Status, offer.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Ok(offers);
    }

    [HttpPost("offers")]
    public async Task<ActionResult<SupplierOfferResponse>> CreateOffer(Guid supplierId,
        CreateSupplierOfferRequest request, CancellationToken cancellationToken)
    {
        ValidateOffer(request.UnitPrice, request.SellerSku);
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken, "owner", "manager");
        if (request.ProductId == Guid.Empty) throw new AppValidationException(nameof(request.ProductId), "A valid product is required.");
        var product = await dbContext.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.ProductId
            && item.IsActive && item.Category.IsActive, cancellationToken);
        if (product is null) throw new NotFoundException("Active product was not found.");
        var locations = await dbContext.SupplierLocations.Where(item => item.SupplierId == supplierId && item.IsActive)
            .ToListAsync(cancellationToken);
        if (locations.Count == 0) throw new ConflictException("Create an active supplier location before adding offers.");
        if (await dbContext.SupplierOffers.AnyAsync(item => item.SupplierId == supplierId && item.ProductId == request.ProductId, cancellationToken))
            throw new ConflictException("An offer for this product already exists. Update the existing offer instead.");

        var now = DateTime.UtcNow;
        var offer = new SupplierOffer
        {
            SupplierId = supplierId,
            ProductId = product.Id,
            SellerSku = string.IsNullOrWhiteSpace(request.SellerSku) ? null : request.SellerSku.Trim(),
            UnitPrice = request.UnitPrice,
            Currency = "BDT",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now,
            InventoryBalances = locations.Select(location => new InventoryBalance
            {
                SupplierLocationId = location.Id,
                QuantityOnHand = 0m,
                QuantityReserved = 0m,
                UpdatedAt = now
            }).ToList()
        };
        dbContext.SupplierOffers.Add(offer);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("An offer for this product already exists.");
        }
        return Created($"/api/v1/supplier/{supplierId}/offers/{offer.Id}",
            new SupplierOfferResponse(offer.Id, offer.ProductId, product.Name, product.UnitCode,
                offer.SellerSku, offer.UnitPrice, offer.Currency, offer.Status, offer.UpdatedAt));
    }

    [HttpPut("offers/{offerId:guid}")]
    public async Task<ActionResult<SupplierOfferResponse>> UpdateOffer(Guid supplierId, Guid offerId,
        UpdateSupplierOfferRequest request, CancellationToken cancellationToken)
    {
        ValidateOffer(request.UnitPrice, request.SellerSku);
        if (request.Status is not ("active" or "paused" or "discontinued"))
            throw new AppValidationException(nameof(request.Status), "Status must be active, paused, or discontinued.");
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken, "owner", "manager");
        var offer = await dbContext.SupplierOffers.Include(item => item.Product)
            .SingleOrDefaultAsync(item => item.Id == offerId && item.SupplierId == supplierId, cancellationToken);
        if (offer is null) return NotFound();
        if (request.Status == "active" && (!offer.Product.IsActive || !await dbContext.Categories
                .AnyAsync(category => category.Id == offer.Product.CategoryId && category.IsActive, cancellationToken)))
            throw new ConflictException("An inactive product or category cannot be offered.");
        offer.SellerSku = string.IsNullOrWhiteSpace(request.SellerSku) ? null : request.SellerSku.Trim();
        offer.UnitPrice = request.UnitPrice;
        offer.Status = request.Status;
        offer.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new SupplierOfferResponse(offer.Id, offer.ProductId, offer.Product.Name,
            offer.Product.UnitCode, offer.SellerSku, offer.UnitPrice, offer.Currency, offer.Status, offer.UpdatedAt));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<IReadOnlyList<SupplierInventoryResponse>>> GetInventory(Guid supplierId, CancellationToken cancellationToken)
    {
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken);
        var rows = await dbContext.InventoryBalances.AsNoTracking()
            .Where(balance => balance.SupplierLocation.SupplierId == supplierId)
            .OrderBy(balance => balance.SupplierLocation.Name).ThenBy(balance => balance.SupplierOffer.Product.Name)
            .Select(balance => new SupplierInventoryResponse(balance.SupplierLocationId, balance.SupplierLocation.Name,
                balance.SupplierOfferId, balance.SupplierOffer.Product.Name, balance.SupplierOffer.UnitPrice,
                balance.QuantityOnHand, balance.QuantityReserved, balance.QuantityOnHand - balance.QuantityReserved,
                balance.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("inventory/{locationId:guid}/{offerId:guid}/receipts")]
    public Task<IActionResult> ReceiveStock(Guid supplierId, Guid locationId, Guid offerId,
        StockReceiptRequest request, CancellationToken cancellationToken) =>
        ApplyStockMovement(supplierId, locationId, offerId, request.Quantity, "receipt", request.Notes, cancellationToken);

    [HttpPost("inventory/{locationId:guid}/{offerId:guid}/adjustments")]
    public Task<IActionResult> AdjustStock(Guid supplierId, Guid locationId, Guid offerId,
        StockAdjustmentRequest request, CancellationToken cancellationToken) =>
        ApplyStockMovement(supplierId, locationId, offerId, request.QuantityDelta, "adjustment", request.Notes, cancellationToken);

    private async Task<IActionResult> ApplyStockMovement(Guid supplierId, Guid locationId, Guid offerId,
        decimal delta, string movementType, string? notes, CancellationToken cancellationToken)
    {
        if (delta == 0 || decimal.Abs(delta) > MaxStock || delta != decimal.Round(delta, 3))
            throw new AppValidationException(nameof(delta), "Stock quantity must be non-zero, at most 3 decimal places, and within the supported range.");
        if (movementType == "receipt" && delta < 0)
            throw new AppValidationException(nameof(delta), "A stock receipt must be a positive quantity.");
        if (notes?.Length > 1000) throw new AppValidationException(nameof(notes), "Notes must be at most 1000 characters.");
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken);
        var balance = await dbContext.InventoryBalances.AsNoTracking().Where(item =>
                item.SupplierLocationId == locationId && item.SupplierOfferId == offerId
                && item.SupplierLocation.SupplierId == supplierId && item.SupplierOffer.SupplierId == supplierId)
            .Select(item => new { item.Id, item.SupplierOffer.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (balance is null) return NotFound();
        if (balance.Status == "discontinued") throw new ConflictException("Stock cannot be changed for a discontinued offer.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var changed = await dbContext.InventoryBalances.Where(item => item.Id == balance.Id
                && item.QuantityOnHand + delta >= item.QuantityReserved
                && item.QuantityOnHand + delta <= MaxStock)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.QuantityOnHand, item => item.QuantityOnHand + delta)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);
        if (changed != 1)
            throw new ConflictException("Stock adjustment would make on-hand quantity lower than reserved stock or exceed the supported range.");
        dbContext.InventoryMovements.Add(new InventoryMovement
        {
            InventoryBalanceId = balance.Id,
            MovementType = movementType,
            QuantityDelta = delta,
            ReferenceType = "supplier_stock",
            ReferenceId = supplierId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private static void ValidateOffer(decimal unitPrice, string? sellerSku)
    {
        if (unitPrice <= 0 || unitPrice > MaxUnitPrice || unitPrice != decimal.Round(unitPrice, 2))
            throw new AppValidationException(nameof(unitPrice), "Unit price must be positive, within range, and have at most 2 decimal places.");
        if (sellerSku?.Length > 100) throw new AppValidationException(nameof(sellerSku), "Seller SKU must be at most 100 characters.");
    }
}

public sealed record SupplierProductResponse(Guid Id, string Name, string? Description, string Category,
    string ProductType, string UnitCode, string? FuelGrade);
public sealed record CreateSupplierOfferRequest([property: Required] Guid ProductId,
    [property: Required] decimal UnitPrice, [property: StringLength(100)] string? SellerSku);
public sealed record UpdateSupplierOfferRequest(decimal UnitPrice, string? SellerSku, string Status);
public sealed record SupplierOfferResponse(Guid Id, Guid ProductId, string ProductName, string UnitCode,
    string? SellerSku, decimal UnitPrice, string Currency, string Status, DateTime UpdatedAt);
public sealed record SupplierInventoryResponse(Guid LocationId, string LocationName, Guid OfferId,
    string ProductName, decimal UnitPrice, decimal QuantityOnHand, decimal QuantityReserved,
    decimal QuantityAvailable, DateTime UpdatedAt);
public sealed record StockReceiptRequest(decimal Quantity, [property: StringLength(1000)] string? Notes);
public sealed record StockAdjustmentRequest(decimal QuantityDelta, [property: StringLength(1000)] string? Notes);
