using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    // Which status can move to which. Anything not listed here is rejected.
    //   pending -> assigned happens through POST {id}/assign, not PATCH status.
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        ["pending"]    = ["cancelled"],
        ["assigned"]   = ["dispatched", "cancelled"],
        ["dispatched"] = ["in_transit", "failed", "cancelled"],
        ["in_transit"] = ["delivered", "failed"],
    };

    // Once a delivery reaches one of these, the driver and vehicle are free again.
    private static readonly string[] FinalStatuses = ["delivered", "failed", "cancelled"];

    private readonly PgDbContext _context;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(PgDbContext context, ILogger<DeliveriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/deliveries?status=pending
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Delivery>>> GetDeliveries([FromQuery] string? status)
    {
        try
        {
            var query = _context.Deliveries.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.Status == status);

            var deliveries = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return Ok(deliveries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve deliveries. Operation: {Operation}", nameof(GetDeliveries));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving deliveries." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Delivery>> GetDelivery(Guid id)
    {
        try
        {
            var delivery = await _context.Deliveries.AsNoTracking()
                .Include(d => d.Driver)
                .Include(d => d.Vehicle)
                .Include(d => d.StatusHistory.OrderBy(h => h.CreatedAt))
                .FirstOrDefaultAsync(d => d.Id == id);
            return delivery is null ? NotFound() : Ok(delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve delivery {DeliveryId}. Operation: {Operation}", id, nameof(GetDelivery));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the delivery." });
        }
    }

    // Creates a pending delivery for a supplier order. Assign a driver + truck afterwards.
    [HttpPost]
    public async Task<ActionResult<Delivery>> CreateDelivery(CreateDeliveryRequest request)
    {
        try
        {
            if (!await _context.SupplierOrders.AnyAsync(s => s.Id == request.SupplierOrderId))
                return BadRequest(new { message = "Supplier order not found." });

            // A failed/cancelled delivery can be retried with a new one; otherwise only one at a time.
            var hasOpenDelivery = await _context.Deliveries.AnyAsync(d =>
                d.SupplierOrderId == request.SupplierOrderId && !FinalStatuses.Contains(d.Status));
            if (hasOpenDelivery)
                return Conflict(new { message = "This supplier order already has an open delivery." });

            var delivery = new Delivery { SupplierOrderId = request.SupplierOrderId };
            delivery.StatusHistory.Add(new DeliveryStatusHistory { Status = delivery.Status });

            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDelivery), new { id = delivery.Id }, delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create delivery. Operation: {Operation}", nameof(CreateDelivery));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the delivery." });
        }
    }

    // Assigns a driver and a truck to a pending delivery (plan.md rule: only authorized
    // drivers and vehicles). Both are marked unavailable until the delivery finishes.
    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<Delivery>> AssignDelivery(Guid id, AssignDeliveryRequest request)
    {
        try
        {
            var delivery = await _context.Deliveries.FindAsync(id);
            if (delivery is null) return NotFound();
            if (delivery.Status != "pending")
                return BadRequest(new { message = $"Only pending deliveries can be assigned (current: {delivery.Status})." });

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var driver = await _context.Drivers.FindAsync(request.DriverId);
            if (driver is null) return BadRequest(new { message = "Driver not found." });
            if (driver.VerificationStatus != "verified" || driver.EmploymentStatus != "active")
                return BadRequest(new { message = "Driver is not verified or not active." });
            if (!driver.DangerousGoodsCertified)
                return BadRequest(new { message = "Driver is not certified to carry fuel." });
            if (driver.LicenseExpiry < today)
                return BadRequest(new { message = "Driver's license has expired." });
            if (!driver.IsAvailable)
                return Conflict(new { message = "Driver is not available." });

            var vehicle = await _context.Vehicles.FindAsync(request.VehicleId);
            if (vehicle is null) return BadRequest(new { message = "Vehicle not found." });
            if (vehicle.Status != "active")
                return BadRequest(new { message = $"Vehicle is {vehicle.Status}." });
            if (vehicle.FitnessCertificateExpiry is not null && vehicle.FitnessCertificateExpiry < today)
                return BadRequest(new { message = "Vehicle's fitness certificate has expired." });
            if (!vehicle.IsAvailable)
                return Conflict(new { message = "Vehicle is not available." });

            delivery.DriverId = driver.Id;
            delivery.VehicleId = vehicle.Id;
            delivery.Status = "assigned";
            delivery.AssignedAt = DateTime.UtcNow;
            driver.IsAvailable = false;
            vehicle.IsAvailable = false;

            _context.DeliveryStatusHistories.Add(new DeliveryStatusHistory
            {
                DeliveryId = delivery.Id,
                Status = delivery.Status
            });

            await _context.SaveChangesAsync();
            return Ok(delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign delivery {DeliveryId}. Operation: {Operation}", id, nameof(AssignDelivery));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while assigning the delivery." });
        }
    }

    // Moves a delivery forward (dispatched -> in_transit -> delivered, or failed/cancelled).
    // Every change is written to DeliveryStatusHistory.
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Delivery>> UpdateDeliveryStatus(Guid id, UpdateDeliveryStatusRequest request)
    {
        try
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Driver)
                .Include(d => d.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (delivery is null) return NotFound();

            if (!AllowedTransitions.TryGetValue(delivery.Status, out var next) || !next.Contains(request.Status))
                return BadRequest(new { message = $"Cannot change delivery from {delivery.Status} to {request.Status}." });

            if (request.Status == "delivered" && request.DeliveredQuantity is null)
                return BadRequest(new { message = "DeliveredQuantity is required when status is delivered." });
            if (request.Status == "failed" && string.IsNullOrWhiteSpace(request.FailureReason))
                return BadRequest(new { message = "FailureReason is required when status is failed." });

            delivery.Status = request.Status;
            switch (request.Status)
            {
                case "dispatched":
                    delivery.DispatchedAt = DateTime.UtcNow;
                    break;
                case "delivered":
                    delivery.DeliveredAt = DateTime.UtcNow;
                    delivery.DeliveredQuantity = request.DeliveredQuantity;
                    break;
                case "failed":
                    delivery.FailureReason = request.FailureReason;
                    break;
            }

            // Delivery is over: free up the driver and truck for the next job.
            if (FinalStatuses.Contains(request.Status))
            {
                if (delivery.Driver is not null) delivery.Driver.IsAvailable = true;
                if (delivery.Vehicle is not null) delivery.Vehicle.IsAvailable = true;
            }

            _context.DeliveryStatusHistories.Add(new DeliveryStatusHistory
            {
                DeliveryId = delivery.Id,
                Status = request.Status,
                Notes = request.Notes,
                RecordedByUserId = request.RecordedByUserId
            });

            await _context.SaveChangesAsync();
            return Ok(delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of delivery {DeliveryId}. Operation: {Operation}", id, nameof(UpdateDeliveryStatus));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the delivery status." });
        }
    }
}
