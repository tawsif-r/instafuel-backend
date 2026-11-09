using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(PgDbContext context, ILogger<SuppliersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
    {
        try
        {
            var suppliers = await _context.Suppliers.AsNoTracking().OrderBy(s => s.DisplayName).ToListAsync();
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve suppliers. Operation: {Operation}", nameof(GetSuppliers));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving suppliers." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Supplier>> GetSupplier(Guid id)
    {
        try
        {
            var supplier = await _context.Suppliers.AsNoTracking()
                .Include(s => s.Locations)
                .FirstOrDefaultAsync(s => s.Id == id);
            return supplier is null ? NotFound() : Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve supplier {SupplierId}. Operation: {Operation}", id, nameof(GetSupplier));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the supplier." });
        }
    }

    // New suppliers start as "pending" until approved via PATCH status.
    [HttpPost]
    public async Task<ActionResult<Supplier>> CreateSupplier(SaveSupplierRequest request)
    {
        try
        {
            var supplier = new Supplier
            {
                LegalName = request.LegalName,
                DisplayName = request.DisplayName
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create supplier. Operation: {Operation}", nameof(CreateSupplier));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the supplier." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Supplier>> UpdateSupplier(Guid id, SaveSupplierRequest request)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier is null) return NotFound();

            supplier.LegalName = request.LegalName;
            supplier.DisplayName = request.DisplayName;
            await _context.SaveChangesAsync();
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update supplier {SupplierId}. Operation: {Operation}", id, nameof(UpdateSupplier));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the supplier." });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Supplier>> UpdateSupplierStatus(Guid id, UpdateSupplierStatusRequest request)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier is null) return NotFound();

            supplier.Status = request.Status;
            await _context.SaveChangesAsync();
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of supplier {SupplierId}. Operation: {Operation}", id, nameof(UpdateSupplierStatus));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the supplier status." });
        }
    }
}
