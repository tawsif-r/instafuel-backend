using System.Text;
using Instafuel.Api.Dtos;
using Instafuel.Api.Extensions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    // PBKDF2 hasher from ASP.NET Core Identity. The hash string includes the salt and algorithm version.
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(PgDbContext context, IConfiguration config, ILogger<AuthController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            // Store emails lowercased so login is case-insensitive.
            var email = request.Email.Trim().ToLowerInvariant();
            var phone = request.Phone.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { message = "An account with this email already exists." });
            if (await _context.Users.AnyAsync(u => u.Phone == phone))
                return Conflict(new { message = "An account with this phone number already exists." });

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = phone,
                Role = "customer",
                AccountType = request.AccountType,
                CompanyName = request.AccountType == "corporate"
                    ? string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim()
                    : null
            };
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(CreateAuthResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user. Operation: {Operation}", nameof(Register));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the account." });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            var identifier = request.Identifier.Trim();
            var email = identifier.ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.IsActive && (u.Email == email || u.Phone == identifier));

            // Same message for "no such user" and "wrong password" so accounts can't be probed.
            var result = user?.PasswordHash is null
                ? PasswordVerificationResult.Failed
                : _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (user is null || result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Incorrect email/phone or password." });

            // Hash was made with older settings: upgrade it now that we know the password.
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, request.Password);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(CreateAuthResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log in. Operation: {Operation}", nameof(Login));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while logging in." });
        }
    }

    // Returns the user the token belongs to. Handy for the app to check its saved session.
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<User>> Me()
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            return user is null ? Unauthorized() : Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current user. Operation: {Operation}", nameof(Me));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while loading the account." });
        }
    }

    // Signed JWT with the user's id (sub), email, name and role. Settings come from the "Jwt" config section.
    private AuthResponse CreateAuthResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddDays(_config.GetValue("Jwt:ExpiryDays", 7));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email ?? string.Empty,
                [JwtRegisteredClaimNames.Name] = user.FullName,
                ["role"] = user.Role
            }
        });

        return new AuthResponse(token, expiresAt, user);
    }
}
