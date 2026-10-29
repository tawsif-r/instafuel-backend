using Microsoft.EntityFrameworkCore;
using Instafuel.Api.Models;

namespace Instafuel.Api.Modules.Identity;

public sealed class OtpCodeCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OtpCodeCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PgDbContext>();
                var retentionCutoff = DateTime.UtcNow.AddDays(-1);
                await dbContext.OtpCodes.Where(code => code.ExpiresAt < retentionCutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                await dbContext.EmailChangeRequests.Where(request => request.ExpiresAt < retentionCutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                await dbContext.PasswordResetRequests.Where(request => request.ExpiresAt < retentionCutoff)
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Do not log database command details or any OTP-related data.
                logger.LogWarning("OTP cleanup failed ({ExceptionType}).", exception.GetType().Name);
            }
        }
    }
}
