using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(PgDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        try
        {
            var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users. Operation: {Operation}", nameof(GetUsers));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving users." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        try
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            return user is null ? NotFound() : Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId}. Operation: {Operation}", id, nameof(GetUser));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the user." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserRequest request)
    {
        try
        {
            if (request.Email is null && request.Phone is null)
                return BadRequest(new { message = "Either email or phone is required." });

            if (await EmailOrPhoneTaken(request.Email, request.Phone, null))
                return Conflict(new { message = "Email or phone is already in use." });

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user. Operation: {Operation}", nameof(CreateUser));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the user." });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<User>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            if (await EmailOrPhoneTaken(request.Email, request.Phone, id))
                return Conflict(new { message = "Email or phone is already in use." });

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}. Operation: {Operation}", id, nameof(UpdateUser));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the user." });
        }
    }

    // Soft delete: users are referenced by orders, so we only deactivate them.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user {UserId}. Operation: {Operation}", id, nameof(DeactivateUser));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deactivating the user." });
        }
    }

    // excludeUserId lets an update keep its own email/phone.
    private Task<bool> EmailOrPhoneTaken(string? email, string? phone, Guid? excludeUserId) =>
        _context.Users.AnyAsync(u =>
            u.Id != excludeUserId &&
            ((email != null && u.Email == email) || (phone != null && u.Phone == phone)));
}
