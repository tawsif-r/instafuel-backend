using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Customer;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customer")]
public sealed class CustomerProfileController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet("profile")]
    public async Task<ActionResult<CustomerProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var profile = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new CustomerProfileResponse(user.Id, user.FullName, user.Email, user.Phone, user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<CustomerProfileResponse>> UpdateProfile(
        UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 150)
            throw new AppValidationException(nameof(request.FullName), "Name is required and must be at most 150 characters.");
        if (request.Phone?.Length > 30)
            throw new AppValidationException(nameof(request.Phone), "Phone must be at most 30 characters.");

        var userId = CustomerControllerSupport.GetUserId(User);
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return NotFound();
        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CustomerProfileResponse(user.Id, user.FullName, user.Email, user.Phone, user.CreatedAt));
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<CustomerAddressResponse>>> GetAddresses(CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var addresses = await dbContext.CustomerAddresses.AsNoTracking()
            .Where(address => address.CustomerId == userId).OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.CreatedAt)
            .Select(address => new CustomerAddressResponse(address.Id, address.Label, address.RecipientName,
                address.RecipientPhone, address.AddressLine, address.City, address.Latitude, address.Longitude, address.IsDefault))
            .ToListAsync(cancellationToken);
        return Ok(addresses);
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<CustomerAddressResponse>> CreateAddress(
        CustomerAddressRequest request, CancellationToken cancellationToken)
    {
        ValidateAddress(request);
        var userId = CustomerControllerSupport.GetUserId(User);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var hasAddresses = await dbContext.CustomerAddresses.AnyAsync(item => item.CustomerId == userId, cancellationToken);
        if (request.IsDefault || !hasAddresses)
        {
            var existing = await dbContext.CustomerAddresses.Where(item => item.CustomerId == userId && item.IsDefault).ToListAsync(cancellationToken);
            foreach (var address in existing) address.IsDefault = false;
        }
        var entity = new CustomerAddress
        {
            CustomerId = userId, Label = request.Label.Trim(), RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(), AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(), Latitude = request.Latitude, Longitude = request.Longitude,
            IsDefault = request.IsDefault || !hasAddresses
        };
        dbContext.CustomerAddresses.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAddresses), null, ToResponse(entity));
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<CustomerAddressResponse>> UpdateAddress(
        Guid addressId, CustomerAddressRequest request, CancellationToken cancellationToken)
    {
        ValidateAddress(request);
        var userId = CustomerControllerSupport.GetUserId(User);
        var entity = await dbContext.CustomerAddresses.SingleOrDefaultAsync(
            item => item.Id == addressId && item.CustomerId == userId, cancellationToken);
        if (entity is null) return NotFound();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsDefault)
        {
            var existing = await dbContext.CustomerAddresses.Where(item => item.CustomerId == userId && item.IsDefault && item.Id != addressId).ToListAsync(cancellationToken);
            foreach (var address in existing) address.IsDefault = false;
        }
        entity.Label = request.Label.Trim(); entity.RecipientName = request.RecipientName.Trim();
        entity.RecipientPhone = request.RecipientPhone.Trim(); entity.AddressLine = request.AddressLine.Trim();
        entity.City = request.City.Trim(); entity.Latitude = request.Latitude; entity.Longitude = request.Longitude;
        entity.IsDefault = request.IsDefault;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(ToResponse(entity));
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var entity = await dbContext.CustomerAddresses.SingleOrDefaultAsync(
            item => item.Id == addressId && item.CustomerId == userId, cancellationToken);
        if (entity is null) return NotFound();
        var wasDefault = entity.IsDefault;
        dbContext.CustomerAddresses.Remove(entity);
        if (wasDefault)
        {
            var next = await dbContext.CustomerAddresses.Where(item => item.CustomerId == userId && item.Id != addressId)
                .OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (next is not null) next.IsDefault = true;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<IReadOnlyList<CustomerVehicleResponse>>> GetVehicles(CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        return Ok(await dbContext.CustomerVehicles.AsNoTracking().Where(vehicle => vehicle.CustomerId == userId)
            .OrderBy(vehicle => vehicle.CreatedAt)
            .Select(vehicle => new CustomerVehicleResponse(vehicle.Id, vehicle.Label, vehicle.VehicleType,
                vehicle.Make, vehicle.Model, vehicle.RegistrationNumber)).ToListAsync(cancellationToken));
    }

    [HttpPost("vehicles")]
    public async Task<ActionResult<CustomerVehicleResponse>> CreateVehicle(
        CustomerVehicleRequest request, CancellationToken cancellationToken)
    {
        ValidateVehicle(request);
        var vehicle = new CustomerVehicle
        {
            CustomerId = CustomerControllerSupport.GetUserId(User), Label = request.Label.Trim(),
            VehicleType = request.VehicleType.Trim(), Make = Clean(request.Make), Model = Clean(request.Model),
            RegistrationNumber = Clean(request.RegistrationNumber)
        };
        dbContext.CustomerVehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetVehicles), null, ToResponse(vehicle));
    }

    [HttpPut("vehicles/{vehicleId:guid}")]
    public async Task<ActionResult<CustomerVehicleResponse>> UpdateVehicle(
        Guid vehicleId, CustomerVehicleRequest request, CancellationToken cancellationToken)
    {
        ValidateVehicle(request);
        var userId = CustomerControllerSupport.GetUserId(User);
        var vehicle = await dbContext.CustomerVehicles.SingleOrDefaultAsync(
            item => item.Id == vehicleId && item.CustomerId == userId, cancellationToken);
        if (vehicle is null) return NotFound();
        vehicle.Label = request.Label.Trim(); vehicle.VehicleType = request.VehicleType.Trim();
        vehicle.Make = Clean(request.Make); vehicle.Model = Clean(request.Model);
        vehicle.RegistrationNumber = Clean(request.RegistrationNumber);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(vehicle));
    }

    [HttpDelete("vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var vehicle = await dbContext.CustomerVehicles.SingleOrDefaultAsync(
            item => item.Id == vehicleId && item.CustomerId == userId, cancellationToken);
        if (vehicle is null) return NotFound();
        dbContext.CustomerVehicles.Remove(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static void ValidateAddress(CustomerAddressRequest request)
    {
        if (new[] { request.Label, request.RecipientName, request.RecipientPhone, request.AddressLine, request.City }.Any(string.IsNullOrWhiteSpace))
            throw new AppValidationException("Address fields must not be empty.");
        if (request.Label.Length > 50 || request.RecipientName.Length > 150 || request.RecipientPhone.Length > 30 || request.AddressLine.Length > 500 || request.City.Length > 100)
            throw new AppValidationException("One or more address fields exceed their maximum length.");
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            throw new AppValidationException("Coordinates are outside valid latitude or longitude ranges.");
    }

    private static void ValidateVehicle(CustomerVehicleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Label) || request.Label.Length > 80 || string.IsNullOrWhiteSpace(request.VehicleType) || request.VehicleType.Length > 50)
            throw new AppValidationException("Vehicle label and type are required and must be within their maximum lengths.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CustomerAddressResponse ToResponse(CustomerAddress a) => new(a.Id, a.Label, a.RecipientName, a.RecipientPhone, a.AddressLine, a.City, a.Latitude, a.Longitude, a.IsDefault);
    private static CustomerVehicleResponse ToResponse(CustomerVehicle v) => new(v.Id, v.Label, v.VehicleType, v.Make, v.Model, v.RegistrationNumber);
}

public sealed record UpdateCustomerProfileRequest(
    [property: Required, StringLength(150, MinimumLength = 1)] string FullName,
    [property: Phone, StringLength(30)] string? Phone);

public sealed record CustomerProfileResponse(Guid Id, string FullName, string? Email, string? Phone, DateTime CreatedAt);
public sealed record CustomerAddressRequest(string Label, string RecipientName, string RecipientPhone, string AddressLine, string City, decimal? Latitude, decimal? Longitude, bool IsDefault = false);
public sealed record CustomerAddressResponse(Guid Id, string Label, string RecipientName, string RecipientPhone, string AddressLine, string City, decimal? Latitude, decimal? Longitude, bool IsDefault);
public sealed record CustomerVehicleRequest(string Label, string VehicleType, string? Make, string? Model, string? RegistrationNumber);
public sealed record CustomerVehicleResponse(Guid Id, string Label, string VehicleType, string? Make, string? Model, string? RegistrationNumber);
