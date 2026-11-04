using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

// Admin side of customer vehicles: review documents and verify or reject.
// Customers manage their own vehicles through MyVehiclesController.
[ApiController]
[Authorize(Roles = "admin")]
[Route("api/customer-vehicles")]
public class CustomerVehiclesController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<CustomerVehiclesController> _logger;

    public CustomerVehiclesController(PgDbContext context, ILogger<CustomerVehiclesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/customer-vehicles?status=pending -> the review queue
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerVehicle>>> GetCustomerVehicles([FromQuery] string? status)
    {
        try
        {
            var query = _context.CustomerVehicles.AsNoTracking().Include(v => v.Documents).Where(v => v.IsActive);
            if (status is not null)
                query = query.Where(v => v.VerificationStatus == status);

            var vehicles = await query.OrderBy(v => v.UpdatedAt).ToListAsync();
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer vehicles. Operation: {Operation}", nameof(GetCustomerVehicles));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving customer vehicles." });
        }
    }

    [HttpPatch("{id:guid}/verification")]
    public async Task<ActionResult<CustomerVehicle>> VerifyCustomerVehicle(Guid id, VerifyCustomerVehicleRequest request)
    {
        try
        {
            if (request.Status == "rejected" && string.IsNullOrWhiteSpace(request.RejectionReason))
                return BadRequest(new { message = "A rejection reason is required." });

            var vehicle = await _context.CustomerVehicles.Include(v => v.Documents)
                .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);
            if (vehicle is null) return NotFound();

            vehicle.VerificationStatus = request.Status;
            vehicle.RejectionReason = request.Status == "rejected" ? request.RejectionReason!.Trim() : null;
            vehicle.VerifiedAt = request.Status == "verified" ? DateTime.UtcNow : null;
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify customer vehicle {VehicleId}. Operation: {Operation}", id, nameof(VerifyCustomerVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the vehicle." });
        }
    }
}
