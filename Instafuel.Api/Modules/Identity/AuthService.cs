using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Instafuel.Api.Modules.Identity.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;

namespace Instafuel.Api.Modules.Identity;

public sealed class AuthService : IAuthService
{
    private readonly PgDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        PgDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        await RegisterForRoleAsync(request, RoleNames.Customer, null, cancellationToken);

    public async Task<AuthResponse> RegisterRiderAsync(
        RegisterRiderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new AppValidationException(nameof(request.Phone), "Riders must provide a phone number that can be verified.");

        return await RegisterForRoleAsync(
            new RegisterRequest(request.FullName, request.Email, request.Password, request.Phone, request.DeviceInfo),
            RoleNames.Rider, request, cancellationToken);
    }

    private async Task<AuthResponse> RegisterForRoleAsync(
        RegisterRequest request,
        string roleName,
        RegisterRiderRequest? riderRequest,
        CancellationToken cancellationToken)
    {
        var email = AuthContactNormalizer.NormalizeEmail(request.Email);
        var phone = AuthContactNormalizer.NormalizePhone(request.Phone);
        if (email is null && phone is null)
            throw new AppValidationException("Contact", "Provide an email address, a phone number, or both.");
        ValidateDeviceInfo(request.DeviceInfo);
        if (await _dbContext.Users.AnyAsync(user =>
                (email != null && user.Email == email) || (phone != null && user.Phone == phone), cancellationToken))
            throw new ConflictException("An account with that email or phone already exists.");

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(candidate => candidate.Name == roleName, cancellationToken)
            ?? throw new InvalidOperationException($"The {roleName} role has not been seeded.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = phone,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        _dbContext.UserRoles.Add(new UserRole
        {
            User = user,
            Role = role,
            GrantedAt = DateTime.UtcNow
        });
        if (riderRequest is not null)
        {
            _dbContext.RiderProfiles.Add(new RiderProfile
            {
                User = user,
                VerificationStatus = "pending",
                IsAvailable = false,
                VehicleType = riderRequest.VehicleType.Trim(),
                VehicleRegistrationNumber = riderRequest.VehicleRegistrationNumber.Trim().ToUpperInvariant()
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            var response = await IssueTokensAsync(user, request.DeviceInfo, null, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            throw new ConflictException("An account with that email or phone already exists.");
        }
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        ValidateDeviceInfo(request.DeviceInfo);
        var email = AuthContactNormalizer.NormalizeEmail(request.Email);
        var phone = AuthContactNormalizer.NormalizePhone(request.Phone);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => email != null
                ? candidate.Email == email
                : phone != null && candidate.Phone == phone, cancellationToken);

        if (user is null || !user.IsActive || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new UnauthorizedException("Email or password is incorrect.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Email or password is incorrect.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        return await IssueTokensAsync(user, request.DeviceInfo, null, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        ValidateDeviceInfo(request.DeviceInfo);
        var now = DateTime.UtcNow;
        var tokenHash = HashToken(request.RefreshToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var storedToken = await _dbContext.RefreshTokens.AsNoTracking()
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
            throw new UnauthorizedException("Refresh token is invalid or expired.");

        if (storedToken.RevokedAt is not null)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token reuse was detected. This session family has been revoked.");
        }
        if (storedToken.ExpiresAt <= now)
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        if (!storedToken.User.IsActive)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        // Atomically claim the token. A repeated presentation revokes every token
        // rotated from the same family, including a concurrently issued successor.
        var claimed = await _dbContext.RefreshTokens.Where(token => token.Id == storedToken.Id
                && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
        if (claimed != 1)
        {
            var latest = await _dbContext.RefreshTokens.AsNoTracking()
                .SingleOrDefaultAsync(token => token.Id == storedToken.Id, cancellationToken);
            if (latest?.RevokedAt is not null)
                await RevokeTokenFamilyAsync(storedToken.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token is invalid or has already been used.");
        }

        var response = await IssueTokensAsync(storedToken.User,
            request.DeviceInfo ?? storedToken.DeviceInfo, storedToken.FamilyId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task RevokeRefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await _dbContext.RefreshTokens.Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.FullName,
                candidate.Email,
                candidate.Phone,
                Roles = candidate.UserRoles.Select(userRole => userRole.Role.Name).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? null
            : new CurrentUserResponse(user.Id, user.FullName, user.Email, user.Phone, user.Roles);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string? deviceInfo, Guid? tokenFamilyId,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.UserRoles
            .Where(userRole => userRole.UserId == user.Id)
            .Select(userRole => userRole.Role.Name)
            .ToListAsync(cancellationToken);

        var issuedAt = DateTime.UtcNow;
        var accessExpiresAt = issuedAt.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: accessExpiresAt,
            signingCredentials: credentials);

        var rawRefreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var refreshExpiresAt = issuedAt.AddDays(_jwtOptions.RefreshTokenDays);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            FamilyId = tokenFamilyId ?? Guid.NewGuid(),
            TokenHash = HashToken(rawRefreshToken),
            DeviceInfo = deviceInfo,
            CreatedAt = issuedAt,
            ExpiresAt = refreshExpiresAt
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            new JwtSecurityTokenHandler().WriteToken(jwt),
            accessExpiresAt,
            rawRefreshToken,
            refreshExpiresAt);
    }

    private async Task RevokeTokenFamilyAsync(Guid familyId, DateTime revokedAt, CancellationToken cancellationToken)
    {
        await _dbContext.RefreshTokens.Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken);
    }

    private static void ValidateDeviceInfo(string? deviceInfo)
    {
        if (deviceInfo?.Length > 200)
            throw new AppValidationException(nameof(deviceInfo), "Device info must be at most 200 characters.");
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
