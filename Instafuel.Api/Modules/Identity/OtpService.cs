using System.Security.Cryptography;
using System.Text;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Instafuel.Api.Modules.Identity.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Instafuel.Api.Modules.Identity;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SendWindow = TimeSpan.FromMinutes(15);
    private const int MaxAttempts = 5;
    private const int MaxSendsPerWindow = 3;

    private readonly PgDbContext _dbContext;
    private readonly ISmsSender _smsSender;
    private readonly byte[] _hashingKey;
    private readonly IHostEnvironment _environment;

    public OtpService(
        PgDbContext dbContext,
        ISmsSender smsSender,
        IOptions<OtpOptions> otpOptions,
        IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _smsSender = smsSender;
        _environment = environment;
        _hashingKey = Encoding.UTF8.GetBytes(otpOptions.Value.HashingKey);
        if (_hashingKey.Length < 32)
            throw new InvalidOperationException("Otp:HashingKey must contain at least 32 bytes.");
    }

    public async Task<OtpSendResult> SendAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        var phone = OtpPhoneNumber.NormalizeBangladeshPhone(request.Phone);
        var purpose = request.Purpose.ToLowerInvariant();
        var msisdn = OtpPhoneNumber.ToSslWirelessMsisdn(phone);
        var now = DateTime.UtcNow;

        // Serialize requests for the same phone so simultaneous calls cannot exceed
        // the per-phone send limit or leave multiple active codes.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({phone}, 0));", cancellationToken);

        var recentSends = await _dbContext.OtpCodes.CountAsync(
            code => code.Phone == phone && code.CreatedAt >= now - SendWindow,
            cancellationToken);
        if (recentSends >= MaxSendsPerWindow)
            throw new TooManyRequestsException("Too many OTP requests. Try again later.");

        var storedPhoneVariants = OtpPhoneNumber.GetStoredPhoneVariants(phone);
        var matchingUsers = await _dbContext.Users
            .Where(candidate => candidate.Phone != null && storedPhoneVariants.Contains(candidate.Phone))
            .Take(2)
            .ToListAsync(cancellationToken);
        var user = matchingUsers.Count == 1 ? matchingUsers[0] : null;
        var eligible = user is not null && user.IsActive && purpose switch
        {
            "signup" => user.PhoneVerifiedAt is null,
            "login" or "reset" => user.PhoneVerifiedAt is not null,
            _ => false
        };

        var rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otp = new OtpCode
        {
            UserId = eligible ? user!.Id : null,
            Phone = phone,
            Purpose = purpose,
            CodeHash = HashCode(phone, purpose, rawCode),
            CreatedAt = now,
            ExpiresAt = now.Add(OtpLifetime),
            ConsumedAt = eligible ? null : now
        };

        if (eligible)
        {
            await _dbContext.OtpCodes.Where(code => code.UserId == user!.Id
                    && code.Purpose == purpose && code.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(code => code.ConsumedAt, now), cancellationToken);
        }

        _dbContext.OtpCodes.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (!eligible)
            return new OtpSendResult(true, null);

        try
        {
            await _smsSender.SendAsync(msisdn, $"Your InstaFuel verification code is {rawCode}. It expires in 5 minutes.",
                cancellationToken);
        }
        catch
        {
            await _dbContext.OtpCodes.Where(code => code.Id == otp.Id && code.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(code => code.ConsumedAt, DateTime.UtcNow),
                    cancellationToken);
            throw;
        }

        return new OtpSendResult(true, _environment.IsDevelopment() ? rawCode : null);
    }

    public async Task<OtpSendResult> SendPhoneChangeAsync(Guid userId, string phoneInput,
        CancellationToken cancellationToken)
    {
        var phone = OtpPhoneNumber.NormalizeBangladeshPhone(phoneInput);
        var msisdn = OtpPhoneNumber.ToSslWirelessMsisdn(phone);
        var now = DateTime.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({phone}, 0));", cancellationToken);

        var recentSends = await _dbContext.OtpCodes.CountAsync(
            code => code.Phone == phone && code.CreatedAt >= now - SendWindow, cancellationToken);
        if (recentSends >= MaxSendsPerWindow)
            throw new TooManyRequestsException("Too many OTP requests. Try again later.");

        var variants = OtpPhoneNumber.GetStoredPhoneVariants(phone);
        var user = await _dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId
            && candidate.IsActive, cancellationToken);
        var isAvailable = !await _dbContext.Users.AnyAsync(candidate => candidate.Id != userId
            && candidate.Phone != null && variants.Contains(candidate.Phone), cancellationToken);
        var eligible = user is not null && isAvailable;
        var rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var otp = new OtpCode
        {
            UserId = eligible ? userId : null,
            Phone = phone,
            Purpose = "phone_change",
            CodeHash = HashCode(phone, "phone_change", rawCode),
            CreatedAt = now,
            ExpiresAt = now.Add(OtpLifetime),
            ConsumedAt = eligible ? null : now
        };
        if (eligible)
        {
            await _dbContext.OtpCodes.Where(code => code.UserId == userId
                    && code.Purpose == "phone_change" && code.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(code => code.ConsumedAt, now), cancellationToken);
        }
        _dbContext.OtpCodes.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (!eligible) return new OtpSendResult(true, null);
        try
        {
            await _smsSender.SendAsync(msisdn,
                $"Your InstaFuel phone-change verification code is {rawCode}. It expires in 5 minutes.",
                cancellationToken);
        }
        catch
        {
            await _dbContext.OtpCodes.Where(code => code.Id == otp.Id && code.ConsumedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(code => code.ConsumedAt, DateTime.UtcNow),
                    cancellationToken);
            throw;
        }
        return new OtpSendResult(true, _environment.IsDevelopment() ? rawCode : null);
    }

    public async Task<VerifyOtpResponse> VerifyAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        string phone;
        try
        {
            phone = OtpPhoneNumber.NormalizeBangladeshPhone(request.Phone);
        }
        catch (AppValidationException)
        {
            throw new UnauthorizedException("OTP is invalid or expired.");
        }
        var purpose = request.Purpose.ToLowerInvariant();
        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var otp = await _dbContext.OtpCodes
            .Where(code => code.Phone == phone && code.Purpose == purpose
                && code.ConsumedAt == null && code.ExpiresAt > now && code.Attempts < MaxAttempts)
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || !FixedTimeHashMatches(otp.CodeHash, HashCode(phone, purpose, request.Code)))
        {
            if (otp is not null)
            {
                await _dbContext.OtpCodes.Where(code => code.Id == otp.Id
                        && code.ConsumedAt == null && code.Attempts < MaxAttempts)
                    .ExecuteUpdateAsync(update => update.SetProperty(code => code.Attempts, code => code.Attempts + 1),
                        cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("OTP is invalid or expired.");
        }

        // Password reset consumes the challenge only when the password is changed,
        // so clients may validate the code before submitting their new password.
        if (purpose == "reset")
        {
            await transaction.CommitAsync(cancellationToken);
            return new VerifyOtpResponse(true, purpose);
        }

        var consumed = await _dbContext.OtpCodes.Where(code => code.Id == otp.Id
                && code.ConsumedAt == null && code.ExpiresAt > now && code.Attempts < MaxAttempts)
            .ExecuteUpdateAsync(update => update.SetProperty(code => code.ConsumedAt, now), cancellationToken);
        if (consumed != 1 || otp.UserId is null)
        {
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("OTP is invalid or expired.");
        }

        if (purpose == "signup")
        {
            await _dbContext.Users.Where(user => user.Id == otp.UserId && user.Phone == phone && user.IsActive)
                .ExecuteUpdateAsync(update => update.SetProperty(user => user.PhoneVerifiedAt, now), cancellationToken);
        }
        else if (purpose == "phone_change")
        {
            var variants = OtpPhoneNumber.GetStoredPhoneVariants(phone);
            if (await _dbContext.Users.AnyAsync(user => user.Id != otp.UserId
                    && user.Phone != null && variants.Contains(user.Phone), cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                throw new ConflictException("That phone number is already linked to another account.");
            }
            int updated;
            try
            {
                updated = await _dbContext.Users.Where(user => user.Id == otp.UserId && user.IsActive)
                    .ExecuteUpdateAsync(update => update.SetProperty(user => user.Phone, phone)
                        .SetProperty(user => user.PhoneVerifiedAt, now)
                        .SetProperty(user => user.UpdatedAt, now), cancellationToken);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ConflictException("That phone number is already linked to another account.");
            }
            if (updated != 1)
            {
                await transaction.CommitAsync(cancellationToken);
                throw new UnauthorizedException("OTP is invalid or expired.");
            }
        }
        await transaction.CommitAsync(cancellationToken);
        return new VerifyOtpResponse(true, purpose);
    }

    public async Task<Guid?> ConsumePasswordResetCodeAsync(string phoneInput, string code,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Password reset OTP must be consumed inside the password reset transaction.");
        string phone;
        try
        {
            phone = OtpPhoneNumber.NormalizeBangladeshPhone(phoneInput);
        }
        catch (AppValidationException)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var otp = await _dbContext.OtpCodes.Where(item => item.Phone == phone && item.Purpose == "reset"
                && item.ConsumedAt == null && item.ExpiresAt > now && item.Attempts < MaxAttempts)
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (otp is null || otp.UserId is null
            || !FixedTimeHashMatches(otp.CodeHash, HashCode(phone, "reset", code)))
        {
            if (otp is not null)
                await _dbContext.OtpCodes.Where(item => item.Id == otp.Id
                        && item.ConsumedAt == null && item.Attempts < MaxAttempts)
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.Attempts,
                        item => item.Attempts + 1), cancellationToken);
            return null;
        }

        var consumed = await _dbContext.OtpCodes.Where(item => item.Id == otp.Id
                && item.ConsumedAt == null && item.ExpiresAt > now && item.Attempts < MaxAttempts)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.ConsumedAt, now), cancellationToken);
        return consumed == 1 ? otp.UserId : null;
    }

    private string HashCode(string phone, string purpose, string code)
    {
        var payload = Encoding.UTF8.GetBytes($"{phone}\n{purpose}\n{code}");
        return Convert.ToHexString(HMACSHA256.HashData(_hashingKey, payload));
    }

    private static bool FixedTimeHashMatches(string expectedHex, string actualHex)
    {
        var expected = Convert.FromHexString(expectedHex);
        var actual = Convert.FromHexString(actualHex);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

}
