using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Authorization;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Suppliers;

[Authorize(Policy = AuthorizationPolicies.SupplierMember)]
[Route("api/v1/supplier/{supplierId:guid}/locations")]
public sealed class SupplierLocationsController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierLocationResponse>>> GetLocations(Guid supplierId, CancellationToken cancellationToken)
    {
        var locations = await dbContext.SupplierLocations.AsNoTracking().Where(item => item.SupplierId == supplierId)
            .OrderBy(item => item.Name)
            .Select(item => new SupplierLocationResponse(item.Id, item.Name, item.AddressLine, item.City,
                item.Latitude, item.Longitude, item.IsActive, item.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(locations);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SupplierManager)]
    public async Task<ActionResult<SupplierLocationResponse>> CreateLocation(Guid supplierId,
        SupplierLocationRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var location = new SupplierLocation
        {
            SupplierId = supplierId,
            Name = request.Name.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true
        };
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.SupplierLocations.Add(location);
        var offers = await dbContext.SupplierOffers.Where(item => item.SupplierId == supplierId && item.Status != "discontinued")
            .ToListAsync(cancellationToken);
        foreach (var offer in offers)
            location.InventoryBalances.Add(new InventoryBalance
            {
                SupplierOfferId = offer.Id,
                QuantityOnHand = 0m,
                QuantityReserved = 0m,
                UpdatedAt = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Created($"/api/v1/supplier/{supplierId}/locations/{location.Id}", ToResponse(location));
    }

    [HttpPut("{locationId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SupplierManager)]
    public async Task<ActionResult<SupplierLocationResponse>> UpdateLocation(Guid supplierId, Guid locationId,
        SupplierLocationRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var location = await dbContext.SupplierLocations.SingleOrDefaultAsync(item =>
            item.Id == locationId && item.SupplierId == supplierId, cancellationToken);
        if (location is null) return NotFound();
        location.Name = request.Name.Trim();
        location.AddressLine = request.AddressLine.Trim();
        location.City = request.City.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(location));
    }

    [HttpDelete("{locationId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SupplierManager)]
    public async Task<IActionResult> DeactivateLocation(Guid supplierId, Guid locationId, CancellationToken cancellationToken)
    {
        var location = await dbContext.SupplierLocations.SingleOrDefaultAsync(item =>
            item.Id == locationId && item.SupplierId == supplierId, cancellationToken);
        if (location is null) return NotFound();
        if (!location.IsActive) return NoContent();
        location.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static void Validate(SupplierLocationRequest request)
    {
        SupplierControllerSupport.ValidateText(request.Name, nameof(request.Name), 150);
        SupplierControllerSupport.ValidateText(request.AddressLine, nameof(request.AddressLine), 500);
        SupplierControllerSupport.ValidateText(request.City, nameof(request.City), 100);
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            throw new AppValidationException("Coordinates are outside valid latitude or longitude ranges.");
        if (request.Latitude.HasValue != request.Longitude.HasValue)
            throw new AppValidationException("Latitude and longitude must either both be provided or both be empty.");
    }

    private static SupplierLocationResponse ToResponse(SupplierLocation location) => new(location.Id, location.Name,
        location.AddressLine, location.City, location.Latitude, location.Longitude, location.IsActive, location.CreatedAt);
}

public sealed record SupplierLocationRequest([property: Required, StringLength(150)] string Name,
    [property: Required, StringLength(500)] string AddressLine, [property: Required, StringLength(100)] string City,
    decimal? Latitude, decimal? Longitude);
public sealed record SupplierLocationResponse(Guid Id, string Name, string AddressLine, string City,
    decimal? Latitude, decimal? Longitude, bool IsActive, DateTime CreatedAt);
