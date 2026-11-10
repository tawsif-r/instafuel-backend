using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(PgDbContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        try
        {
            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve categories. Operation: {Operation}", nameof(GetCategories));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving categories." });
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Category>> GetCategory(Guid id)
    {
        try
        {
            var category = await _context.Categories.AsNoTracking()
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);
            return category is null ? NotFound() : Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve category {CategoryId}. Operation: {Operation}", id, nameof(GetCategory));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the category." });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Category>> CreateCategory(SaveCategoryRequest request)
    {
        try
        {
            var error = await Validate(request, null);
            if (error is not null) return error;

            var category = new Category
            {
                Name = request.Name,
                Slug = request.Slug,
                ParentId = request.ParentId,
                IsActive = request.IsActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category. Operation: {Operation}", nameof(CreateCategory));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the category." });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Category>> UpdateCategory(Guid id, SaveCategoryRequest request)
    {
        try
        {
            var category = await _context.Categories.FindAsync(id);
            if (category is null) return NotFound();

            if (request.ParentId == id)
                return BadRequest(new { message = "A category cannot be its own parent." });

            var error = await Validate(request, id);
            if (error is not null) return error;

            category.Name = request.Name;
            category.Slug = request.Slug;
            category.ParentId = request.ParentId;
            category.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update category {CategoryId}. Operation: {Operation}", id, nameof(UpdateCategory));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the category." });
        }
    }

    // Soft delete: products point at categories, so we only deactivate.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeactivateCategory(Guid id)
    {
        try
        {
            var category = await _context.Categories.FindAsync(id);
            if (category is null) return NotFound();

            category.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate category {CategoryId}. Operation: {Operation}", id, nameof(DeactivateCategory));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deactivating the category." });
        }
    }

    // Returns an error result, or null when the request is fine.
    private async Task<ActionResult?> Validate(SaveCategoryRequest request, Guid? currentId)
    {
        if (await _context.Categories.AnyAsync(c => c.Slug == request.Slug && c.Id != currentId))
            return Conflict(new { message = "Slug is already in use." });

        if (request.ParentId is not null && !await _context.Categories.AnyAsync(c => c.Id == request.ParentId))
            return BadRequest(new { message = "Parent category not found." });

        return null;
    }
}
