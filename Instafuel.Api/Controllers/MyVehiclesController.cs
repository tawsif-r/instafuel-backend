using System.Text.RegularExpressions;
using Instafuel.Api.Dtos;
using Instafuel.Api.Extensions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

// The logged-in customer's own vehicles. Every query is filtered by the user id
// from the token, so another customer's vehicle id simply returns 404.
[ApiController]
[Authorize]
[Route("api/me/vehicles")]
public class MyVehiclesController : ControllerBase
{
    // A vehicle needs all of these before it can be verified.
    private static readonly string[] RequiredDocumentKinds =
        ["registration_certificate", "fitness_certificate", "number_plate_photo"];

    private readonly PgDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<MyVehiclesController> _logger;

    public MyVehiclesController(PgDbContext context, IConfiguration config, ILogger<MyVehiclesController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerVehicle>>> GetMyVehicles()
    {
        try
        {
            var userId = User.GetUserId();
            var vehicles = await _context.CustomerVehicles.AsNoTracking()
                .Include(v => v.Documents)
                .Where(v => v.UserId == userId && v.IsActive)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer vehicles. Operation: {Operation}", nameof(GetMyVehicles));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving your vehicles." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerVehicle>> GetMyVehicle(Guid id)
    {
        try
        {
            var vehicle = await MyVehicles().AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            return vehicle is null ? NotFound() : Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(GetMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the vehicle." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerVehicle>> CreateMyVehicle(SaveCustomerVehicleRequest request)
    {
        try
        {
            var plate = NormalizePlate(request.RegistrationNumber);
            if (await PlateTaken(plate, null))
                return Conflict(new { message = "This number plate is already registered." });

            var vehicle = new CustomerVehicle
            {
                UserId = User.GetUserId(),
                Nickname = request.Nickname.Trim(),
                VehicleType = request.VehicleType,
                RegistrationNumber = plate,
                FuelType = request.FuelType
            };
            ApplyDocuments(vehicle, request.Documents);
            ResetVerification(vehicle);

            _context.CustomerVehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMyVehicle), new { id = vehicle.Id }, vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer vehicle. Operation: {Operation}", nameof(CreateMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while saving the vehicle." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerVehicle>> UpdateMyVehicle(Guid id, SaveCustomerVehicleRequest request)
    {
        try
        {
            var vehicle = await MyVehicles().FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle is null) return NotFound();

            var plate = NormalizePlate(request.RegistrationNumber);
            if (await PlateTaken(plate, id))
                return Conflict(new { message = "This number plate is already registered." });

            // A new plate or new documents need a fresh check. Nickname/fuel changes don't.
            var needsReview = vehicle.RegistrationNumber != plate;
            needsReview |= ApplyDocuments(vehicle, request.Documents);

            vehicle.Nickname = request.Nickname.Trim();
            vehicle.VehicleType = request.VehicleType;
            vehicle.RegistrationNumber = plate;
            vehicle.FuelType = request.FuelType;
            vehicle.UpdatedAt = DateTime.UtcNow;
            if (needsReview) ResetVerification(vehicle);

            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(UpdateMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while saving the vehicle." });
        }
    }

    // Soft delete, so past orders can still show the vehicle.
    // TODO: block this while the vehicle is in an active order (once orders link to customer vehicles).
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMyVehicle(Guid id)
    {
        try
        {
            var vehicle = await MyVehicles().FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle is null) return NotFound();

            vehicle.IsActive = false;
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(DeleteMyVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the vehicle." });
        }
    }

    // Active vehicles of the logged-in user, with documents.
    private IQueryable<CustomerVehicle> MyVehicles()
    {
        var userId = User.GetUserId();
        return _context.CustomerVehicles.Include(v => v.Documents).Where(v => v.UserId == userId && v.IsActive);
    }

    // "dhaka metro-ga  12-3456 " -> "DHAKA METRO-GA 12-3456"
    private static string NormalizePlate(string plate) =>
        Regex.Replace(plate.Trim(), @"\s+", " ").ToUpperInvariant();

    // excludeVehicleId lets an update keep its own plate.
    private Task<bool> PlateTaken(string plate, Guid? excludeVehicleId) =>
        _context.CustomerVehicles.AnyAsync(v =>
            v.IsActive && v.RegistrationNumber == plate && v.Id != excludeVehicleId);

    // Makes the vehicle's documents match the request (one per kind).
    // Returns true when anything was added, replaced or removed.
    private bool ApplyDocuments(CustomerVehicle vehicle, List<VehicleDocumentRequest>? documents)
    {
        var wanted = (documents ?? [])
            .GroupBy(d => d.Kind)
            .ToDictionary(g => g.Key, g => g.Last().FileName.Trim());
        var changed = false;

        foreach (var doc in vehicle.Documents.Where(d => !wanted.ContainsKey(d.Kind)).ToList())
        {
            vehicle.Documents.Remove(doc);
            _context.CustomerVehicleDocuments.Remove(doc);
            changed = true;
        }

        foreach (var (kind, fileName) in wanted)
        {
            var existing = vehicle.Documents.FirstOrDefault(d => d.Kind == kind);
            if (existing is null)
            {
                vehicle.Documents.Add(new CustomerVehicleDocument { Kind = kind, FileName = fileName });
                changed = true;
            }
            else if (existing.FileName != fileName)
            {
                existing.FileName = fileName;
                existing.UploadedAt = DateTime.UtcNow;
                changed = true;
            }
        }
        return changed;
    }

    // Back to "pending" for an admin to review. In Development,
    // CustomerVehicles:AutoVerifyWhenComplete verifies complete vehicles straight away
    // so the order flow can be tried without an admin.
    private void ResetVerification(CustomerVehicle vehicle)
    {
        var complete = RequiredDocumentKinds.All(k => vehicle.Documents.Any(d => d.Kind == k));
        var autoVerify = complete && _config.GetValue("CustomerVehicles:AutoVerifyWhenComplete", false);

        vehicle.VerificationStatus = autoVerify ? "verified" : "pending";
        vehicle.VerifiedAt = autoVerify ? DateTime.UtcNow : null;
        vehicle.RejectionReason = null;
    }
}
