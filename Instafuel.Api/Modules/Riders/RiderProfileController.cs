using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Riders;

[Authorize(Roles = RoleNames.Rider)]
[Route("api/v1/rider")]
public sealed class RiderProfileController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet("profile")]
    public async Task<ActionResult<RiderProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetRiderId();
        var result = await dbContext.RiderProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new RiderProfileResponse(profile.Id, profile.VerificationStatus,
                profile.IsAvailable, profile.VehicleType, profile.VehicleRegistrationNumber,
                profile.PreferredAreas.Select(area => area.City).OrderBy(city => city).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<RiderProfileResponse>> UpdateProfile(
        RiderProfileRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var userId = GetRiderId();
        var profile = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Rider profile was not found. Complete rider registration first.");
        if (profile.VerificationStatus is "verified" or "suspended")
            throw new ConflictException("A verified or suspended rider profile cannot be changed by the rider.");
        profile.VehicleType = request.VehicleType.Trim();
        profile.VehicleRegistrationNumber = request.VehicleRegistrationNumber.Trim().ToUpperInvariant();
        profile.VerificationStatus = "pending";
        profile.IsAvailable = false;
        profile.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var preferredAreas = await dbContext.RiderPreferredAreas.AsNoTracking()
            .Where(area => area.RiderProfileId == profile.Id).Select(area => area.City)
            .OrderBy(city => city).ToListAsync(cancellationToken);
        return Ok(new RiderProfileResponse(profile.Id, profile.VerificationStatus, profile.IsAvailable,
            profile.VehicleType, profile.VehicleRegistrationNumber, preferredAreas));
    }

    [HttpPut("preferred-areas")]
    public async Task<ActionResult<IReadOnlyList<string>>> SetPreferredAreas(
        RiderPreferredAreasRequest request, CancellationToken cancellationToken)
    {
        if (request.Cities is null || request.Cities.Count is < 1 or > 10
            || request.Cities.Any(city => string.IsNullOrWhiteSpace(city) || city.Trim().Length > 100))
            throw new AppValidationException(nameof(request.Cities), "Provide between 1 and 10 valid city names (maximum 100 characters each).");
        var userId = GetRiderId();
        var profile = await dbContext.RiderProfiles.Include(item => item.PreferredAreas)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Rider profile was not found.");
        if (profile.IsAvailable)
            throw new ConflictException("Go offline before changing preferred areas.");

        var cities = request.Cities.Select(city => city.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.RiderPreferredAreas.RemoveRange(profile.PreferredAreas);
        await dbContext.SaveChangesAsync(cancellationToken);
        profile.PreferredAreas = cities.Select(city => new RiderPreferredArea
        {
            RiderProfileId = profile.Id,
            City = city
        }).ToList();
        profile.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(cities);
    }

    [HttpPut("availability")]
    public async Task<ActionResult<RiderAvailabilityResponse>> SetAvailability(
        SetRiderAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var userId = GetRiderId();
        var profile = await dbContext.RiderProfiles.Include(item => item.PreferredAreas)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Rider profile was not found.");
        if (request.IsAvailable && profile.VerificationStatus != "verified")
            throw new ForbiddenException("Only verified riders can go online.");
        if (request.IsAvailable && profile.PreferredAreas.Count == 0)
            throw new ConflictException("Set at least one preferred area before going online.");
        profile.IsAvailable = request.IsAvailable;
        profile.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new RiderAvailabilityResponse(profile.IsAvailable, profile.VerificationStatus));
    }

    private Guid GetRiderId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedException();
    }

    private static void Validate(RiderProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VehicleType) || request.VehicleType.Trim().Length > 50)
            throw new AppValidationException(nameof(request.VehicleType), "Vehicle type is required and must be at most 50 characters.");
        if (string.IsNullOrWhiteSpace(request.VehicleRegistrationNumber) || request.VehicleRegistrationNumber.Trim().Length > 50)
            throw new AppValidationException(nameof(request.VehicleRegistrationNumber), "Vehicle registration is required and must be at most 50 characters.");
    }
}

public sealed record RiderProfileRequest([property: Required, StringLength(50)] string VehicleType,
    [property: Required, StringLength(50)] string VehicleRegistrationNumber);
public sealed record RiderPreferredAreasRequest(IReadOnlyList<string> Cities);
public sealed record SetRiderAvailabilityRequest(bool IsAvailable);
public sealed record RiderProfileResponse(Guid Id, string VerificationStatus, bool IsAvailable,
    string? VehicleType, string? VehicleRegistrationNumber, IReadOnlyList<string> PreferredAreas);
public sealed record RiderAvailabilityResponse(bool IsAvailable, string VerificationStatus);
