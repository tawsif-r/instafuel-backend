using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(PgDbContext context, ILogger<VehiclesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/vehicles?available=true -> only trucks that can take a delivery right now
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles([FromQuery] bool? available)
    {
        try
        {
            var query = _context.Vehicles.AsNoTracking();
            if (available == true)
                query = query.Where(v => v.IsAvailable && v.Status == "active");

            var vehicles = await query.OrderBy(v => v.RegistrationNumber).ToListAsync();
            return Ok(vehicles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve vehicles. Operation: {Operation}", nameof(GetVehicles));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving vehicles." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Vehicle>> GetVehicle(Guid id)
    {
        try
        {
            var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            return vehicle is null ? NotFound() : Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve vehicle {VehicleId}. Operation: {Operation}", id, nameof(GetVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the vehicle." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Vehicle>> CreateVehicle(SaveVehicleRequest request)
    {
        try
        {
            if (await _context.Vehicles.AnyAsync(v => v.RegistrationNumber == request.RegistrationNumber))
                return Conflict(new { message = "Registration number is already registered." });

            var vehicle = new Vehicle();
            Apply(vehicle, request);

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vehicle. Operation: {Operation}", nameof(CreateVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the vehicle." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Vehicle>> UpdateVehicle(Guid id, SaveVehicleRequest request)
    {
        try
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle is null) return NotFound();

            if (await _context.Vehicles.AnyAsync(v => v.RegistrationNumber == request.RegistrationNumber && v.Id != id))
                return Conflict(new { message = "Registration number is already registered." });

            Apply(vehicle, request);
            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vehicle {VehicleId}. Operation: {Operation}", id, nameof(UpdateVehicle));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the vehicle." });
        }
    }

    // Moving a truck to maintenance/retired takes it off the road (IsAvailable = false).
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Vehicle>> UpdateVehicleStatus(Guid id, UpdateVehicleStatusRequest request)
    {
        try
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle is null) return NotFound();

            var onDelivery = await _context.Deliveries.AnyAsync(d =>
                d.VehicleId == id && (d.Status == "assigned" || d.Status == "dispatched" || d.Status == "in_transit"));
            if (onDelivery)
                return Conflict(new { message = "Vehicle is on an active delivery." });

            vehicle.Status = request.Status;
            vehicle.IsAvailable = request.Status == "active";
            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of vehicle {VehicleId}. Operation: {Operation}", id, nameof(UpdateVehicleStatus));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the vehicle status." });
        }
    }

    private static void Apply(Vehicle vehicle, SaveVehicleRequest request)
    {
        vehicle.RegistrationNumber = request.RegistrationNumber;
        vehicle.VehicleType = request.VehicleType;
        vehicle.CapacityLitres = request.CapacityLitres;
        vehicle.CompartmentCount = request.CompartmentCount;
        vehicle.FitnessCertificateExpiry = request.FitnessCertificateExpiry;
        vehicle.LastInspectionDate = request.LastInspectionDate;
    }
}
