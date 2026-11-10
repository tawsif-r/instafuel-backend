using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<DriversController> _logger;

    public DriversController(PgDbContext context, ILogger<DriversController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/drivers?available=true -> only drivers that can take a delivery right now
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers([FromQuery] bool? available)
    {
        try
        {
            var query = _context.Drivers.AsNoTracking().Include(d => d.User).AsQueryable();
            if (available == true)
                query = query.Where(d => d.IsAvailable
                    && d.VerificationStatus == "verified"
                    && d.EmploymentStatus == "active");

            var drivers = await query.OrderBy(d => d.User.FullName).ToListAsync();
            return Ok(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve drivers. Operation: {Operation}", nameof(GetDrivers));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving drivers." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Driver>> GetDriver(Guid id)
    {
        try
        {
            var driver = await _context.Drivers.AsNoTracking()
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);
            return driver is null ? NotFound() : Ok(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve driver {DriverId}. Operation: {Operation}", id, nameof(GetDriver));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the driver." });
        }
    }

    // Turns an existing user into a driver. New drivers start as "pending" verification.
    [HttpPost]
    public async Task<ActionResult<Driver>> CreateDriver(CreateDriverRequest request)
    {
        try
        {
            if (!await _context.Users.AnyAsync(u => u.Id == request.UserId && u.IsActive))
                return BadRequest(new { message = "User not found or inactive." });

            if (await _context.Drivers.AnyAsync(d => d.UserId == request.UserId))
                return Conflict(new { message = "This user is already a driver." });

            if (await _context.Drivers.AnyAsync(d => d.LicenseNumber == request.LicenseNumber))
                return Conflict(new { message = "License number is already registered." });

            var driver = new Driver
            {
                UserId = request.UserId,
                LicenseNumber = request.LicenseNumber,
                LicenseExpiry = request.LicenseExpiry,
                DangerousGoodsCertified = request.DangerousGoodsCertified
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDriver), new { id = driver.Id }, driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create driver. Operation: {Operation}", nameof(CreateDriver));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the driver." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Driver>> UpdateDriver(Guid id, UpdateDriverRequest request)
    {
        try
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver is null) return NotFound();

            if (await _context.Drivers.AnyAsync(d => d.LicenseNumber == request.LicenseNumber && d.Id != id))
                return Conflict(new { message = "License number is already registered." });

            driver.LicenseNumber = request.LicenseNumber;
            driver.LicenseExpiry = request.LicenseExpiry;
            driver.DangerousGoodsCertified = request.DangerousGoodsCertified;
            driver.VerificationStatus = request.VerificationStatus;
            driver.EmploymentStatus = request.EmploymentStatus;

            await _context.SaveChangesAsync();
            return Ok(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update driver {DriverId}. Operation: {Operation}", id, nameof(UpdateDriver));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the driver." });
        }
    }

    // Manual on/off duty toggle. Assigning/finishing a delivery also changes this automatically.
    [HttpPatch("{id:guid}/availability")]
    public async Task<ActionResult<Driver>> UpdateAvailability(Guid id, UpdateAvailabilityRequest request)
    {
        try
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver is null) return NotFound();

            var onDelivery = await _context.Deliveries.AnyAsync(d =>
                d.DriverId == id && (d.Status == "assigned" || d.Status == "dispatched" || d.Status == "in_transit"));
            if (onDelivery)
                return Conflict(new { message = "Driver is on an active delivery." });

            driver.IsAvailable = request.IsAvailable;
            await _context.SaveChangesAsync();
            return Ok(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update availability of driver {DriverId}. Operation: {Operation}", id, nameof(UpdateAvailability));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating driver availability." });
        }
    }
}
