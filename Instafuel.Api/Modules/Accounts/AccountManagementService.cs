using System.Security.Cryptography;
using System.Text;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Instafuel.Api.Modules.Identity;
using Instafuel.Api.Modules.Identity.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Instafuel.Api.Modules.Accounts;

public sealed class AccountManagementService : IAccountManagementService
{
    private static readonly TimeSpan EmailChangeLifetime = TimeSpan.FromMinutes(30);
    private readonly PgDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly IHostEnvironment _environment;

    public AccountManagementService(PgDbContext db, IPasswordHasher<User> passwordHasher,
        IOtpService otpService, IEmailSender emailSender, IOptions<EmailOptions> emailOptions,
        IHostEnvironment environment)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _otpService = otpService;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _environment = environment;
    }

    public async Task<MyProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken) =>
        await _db.Users.AsNoTracking().Where(user => user.Id == userId && user.IsActive)
            .Select(user => new MyProfileResponse(user.Id, user.FullName, user.Email,
                user.EmailVerifiedAt != null, user.Phone, user.PhoneVerifiedAt != null,
                user.UserRoles.Select(link => link.Role.Name).ToList(), user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<MyProfileResponse> UpdateProfileAsync(Guid userId, PatchMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        if (fullName.Length is 0 or > 150)
            throw new AppValidationException(nameof(request.FullName), "Name is required and must be at most 150 characters.");
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId
            && candidate.IsActive, cancellationToken);
        if (user is null) throw new NotFoundException("Account was not found.");
        user.FullName = fullName;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetProfileAsync(userId, cancellationToken))!;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId
            && candidate.IsActive, cancellationToken);
        if (user?.PasswordHash is null) throw new NotFoundException("Account was not found.");
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword)
            == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Current password is incorrect.");
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await RevokeRefreshTokensAsync(userId, user.UpdatedAt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var userId = await _otpService.ConsumePasswordResetCodeAsync(request.Phone, request.Code, cancellationToken);
        if (userId is null)
        {
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("OTP is invalid or expired.");
        }
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId
            && candidate.IsActive, cancellationToken);
        if (user is null)
        {
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("OTP is invalid or expired.");
        }
        var now = DateTime.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = now;
        await RevokeRefreshTokensAsync(user.Id, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestEmailPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail
            && candidate.EmailVerifiedAt != null && candidate.IsActive, cancellationToken);
        if (user is null) return;

        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var resetUrl = BuildEmailVerificationUrl(token, "password-reset");
        var now = DateTime.UtcNow;
        await _db.PasswordResetRequests.Where(item => item.UserId == user.Id && item.ConsumedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, now), cancellationToken);
        var request = new PasswordResetRequest
        {
            UserId = user.Id,
            TokenHash = HashToken(token),
            CreatedAt = now,
            ExpiresAt = now.Add(EmailChangeLifetime)
        };
        _db.PasswordResetRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            await _emailSender.SendAsync(normalizedEmail, "Reset your InstaFuel password",
                $"Reset your password within 30 minutes by opening:\n{resetUrl}\n\nIf you did not request this, ignore this message.",
                cancellationToken);
        }
        catch
        {
            await _db.PasswordResetRequests.Where(item => item.Id == request.Id && item.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, DateTime.UtcNow),
                    cancellationToken);
            throw;
        }
    }

    public async Task ResetPasswordWithEmailAsync(ResetPasswordWithEmailRequest request,
        CancellationToken cancellationToken)
    {
        var hash = HashToken(request.Token);
        var now = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var reset = await _db.PasswordResetRequests.AsNoTracking().SingleOrDefaultAsync(item =>
            item.TokenHash == hash && item.ConsumedAt == null && item.ExpiresAt > now && item.User.IsActive,
            cancellationToken);
        if (reset is null) throw new UnauthorizedException("Password reset link is invalid or expired.");

        var consumed = await _db.PasswordResetRequests.Where(item => item.Id == reset.Id
                && item.ConsumedAt == null && item.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, now), cancellationToken);
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == reset.UserId
            && candidate.IsActive, cancellationToken);
        if (consumed != 1 || user is null)
        {
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Password reset link is invalid or expired.");
        }
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = now;
        await RevokeRefreshTokensAsync(user.Id, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateOwnAccountAsync(Guid userId, string currentPassword,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId
            && candidate.IsActive, cancellationToken);
        if (user?.PasswordHash is null) throw new NotFoundException("Account was not found.");
        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword)
            == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Current password is incorrect.");
        await SetAccountActiveAsync(userId, false, cancellationToken);
    }

    public async Task SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null) throw new NotFoundException("Account was not found.");
        if (user.IsActive == isActive) return;
        var now = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        user.IsActive = isActive;
        user.UpdatedAt = now;
        if (!isActive) await RevokeRefreshTokensAsync(userId, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestEmailChangeAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId
            && candidate.IsActive, cancellationToken);
        if (user is null) throw new NotFoundException("Account was not found.");
        if (string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal) && user.EmailVerifiedAt is not null) return;
        if (await _db.Users.AnyAsync(candidate => candidate.Id != userId
                && candidate.Email == normalizedEmail, cancellationToken))
            throw new ConflictException("That email address is already linked to another account.");
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var verificationUrl = BuildEmailVerificationUrl(token);
        var now = DateTime.UtcNow;
        var request = new EmailChangeRequest
        {
            UserId = userId,
            NewEmail = normalizedEmail,
            TokenHash = HashToken(token),
            CreatedAt = now,
            ExpiresAt = now.Add(EmailChangeLifetime)
        };
        await _db.EmailChangeRequests.Where(item => item.UserId == userId && item.ConsumedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, now), cancellationToken);
        _db.EmailChangeRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailSender.SendAsync(normalizedEmail, "Verify your InstaFuel email address",
                $"Confirm this email change within 30 minutes by opening:\n{verificationUrl}\n\nIf you did not request this change, ignore this message.",
                cancellationToken);
        }
        catch
        {
            await _db.EmailChangeRequests.Where(item => item.Id == request.Id && item.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, DateTime.UtcNow),
                    cancellationToken);
            throw;
        }
    }

    public async Task VerifyEmailChangeAsync(string token, CancellationToken cancellationToken)
    {
        var hash = HashToken(token);
        var now = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var change = await _db.EmailChangeRequests.AsNoTracking().SingleOrDefaultAsync(item =>
            item.TokenHash == hash && item.ConsumedAt == null && item.ExpiresAt > now
            && item.User.IsActive, cancellationToken);
        if (change is null) throw new UnauthorizedException("Email verification link is invalid or expired.");
        if (await _db.Users.AnyAsync(user => user.Id != change.UserId
                && user.Email == change.NewEmail, cancellationToken))
        {
            await _db.EmailChangeRequests.Where(item => item.Id == change.Id && item.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, now), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new ConflictException("That email address is already linked to another account.");
        }
        var consumed = await _db.EmailChangeRequests.Where(item => item.Id == change.Id
                && item.ConsumedAt == null && item.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, now), cancellationToken);
        if (consumed != 1)
        {
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Email verification link is invalid or expired.");
        }
        int updated;
        try
        {
            updated = await _db.Users.Where(user => user.Id == change.UserId && user.IsActive)
                .ExecuteUpdateAsync(update => update.SetProperty(user => user.Email, change.NewEmail)
                    .SetProperty(user => user.EmailVerifiedAt, now)
                    .SetProperty(user => user.UpdatedAt, now), cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("That email address is already linked to another account.");
        }
        if (updated != 1)
        {
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Email verification link is invalid or expired.");
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private string BuildEmailVerificationUrl(string token, string? purpose = null)
    {
        if (!Uri.TryCreate(_emailOptions.VerificationBaseUrl, UriKind.Absolute, out var uri)
            || (!_environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps))
            throw new ServiceUnavailableException("Email verification is not configured. Contact support or try later.");
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        var purposeQuery = purpose is null ? string.Empty : $"&purpose={Uri.EscapeDataString(purpose)}";
        return $"{uri}{separator}token={Uri.EscapeDataString(token)}{purposeQuery}";
    }

    private Task<int> RevokeRefreshTokensAsync(Guid userId, DateTime now, CancellationToken cancellationToken) =>
        _db.RefreshTokens.Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
