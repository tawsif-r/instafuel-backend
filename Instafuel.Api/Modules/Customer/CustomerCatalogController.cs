using Instafuel.Api.Common.Pagination;
using Instafuel.Api.Controllers;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Customer;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customer")]
public sealed class CustomerCatalogController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet("catalog/offers")]
    public async Task<ActionResult<PagedResult<CatalogOfferResponse>>> SearchOffers(
        [FromQuery] PagedRequest page,
        [FromQuery] string? search,
        [FromQuery] string? city,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SupplierOffers.AsNoTracking()
            .Where(offer => offer.Status == "active" && offer.Product.IsActive && offer.Supplier.Status == "active")
            .Where(offer => !categoryId.HasValue || offer.Product.CategoryId == categoryId.Value)
            .Where(offer => string.IsNullOrWhiteSpace(search) || offer.Product.Name.ToLower().Contains(search.Trim().ToLower()))
            .Where(offer => string.IsNullOrWhiteSpace(city) || offer.InventoryBalances.Any(balance => balance.SupplierLocation.IsActive && balance.SupplierLocation.City.ToLower() == city.Trim().ToLower()))
            .SelectMany(offer => offer.InventoryBalances
                .Where(balance => balance.SupplierLocation.IsActive && (string.IsNullOrWhiteSpace(city) || balance.SupplierLocation.City.ToLower() == city.Trim().ToLower()))
                .Select(balance => new CatalogOfferResponse(
                    offer.Id, offer.ProductId, offer.Product.Name, offer.Product.Description,
                    offer.Product.Category.Name, offer.Product.ProductType, offer.Product.UnitCode,
                    offer.Product.FuelGrade, offer.Supplier.DisplayName, balance.SupplierLocation.Id,
                    balance.SupplierLocation.Name, balance.SupplierLocation.City, offer.UnitPrice,
                    offer.Currency, balance.QuantityOnHand - balance.QuantityReserved)))
            .Where(item => item.AvailableQuantity > 0);

        return Ok(await query.OrderBy(item => item.ProductName).ThenBy(item => item.UnitPrice)
            .ToPagedResultAsync(page, cancellationToken));
    }
}

public sealed record CatalogOfferResponse(
    Guid OfferId, Guid ProductId, string ProductName, string? Description, string Category,
    string ProductType, string UnitCode, string? FuelGrade, string SupplierName,
    Guid SupplierLocationId, string LocationName, string City, decimal UnitPrice,
    string Currency, decimal AvailableQuantity);
