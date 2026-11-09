using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(PgDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/products?categoryId=...
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] Guid? categoryId)
    {
        try
        {
            var query = _context.Products.AsNoTracking();
            if (categoryId is not null)
                query = query.Where(p => p.CategoryId == categoryId);

            var products = await query.OrderBy(p => p.Name).ToListAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve products. Operation: {Operation}", nameof(GetProducts));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving products." });
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        try
        {
            var product = await _context.Products.AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
            return product is null ? NotFound() : Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve product {ProductId}. Operation: {Operation}", id, nameof(GetProduct));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the product." });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Product>> CreateProduct(SaveProductRequest request)
    {
        try
        {
            var error = await Validate(request);
            if (error is not null) return error;

            var product = new Product();
            Apply(product, request);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product. Operation: {Operation}", nameof(CreateProduct));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the product." });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Product>> UpdateProduct(Guid id, SaveProductRequest request)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null) return NotFound();

            var error = await Validate(request);
            if (error is not null) return error;

            Apply(product, request);
            await _context.SaveChangesAsync();
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product {ProductId}. Operation: {Operation}", id, nameof(UpdateProduct));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the product." });
        }
    }

    // Soft delete: offers and past order items point at products.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeactivateProduct(Guid id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null) return NotFound();

            product.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate product {ProductId}. Operation: {Operation}", id, nameof(DeactivateProduct));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deactivating the product." });
        }
    }

    private async Task<ActionResult?> Validate(SaveProductRequest request)
    {
        if (request.ProductType == "fuel" && string.IsNullOrWhiteSpace(request.FuelGrade))
            return BadRequest(new { message = "FuelGrade is required for fuel products." });

        if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { message = "Category not found." });

        return null;
    }

    private static void Apply(Product product, SaveProductRequest request)
    {
        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.ProductType = request.ProductType;
        product.UnitCode = request.UnitCode;
        product.FuelGrade = request.FuelGrade;
        product.IsActive = request.IsActive;
    }
}
