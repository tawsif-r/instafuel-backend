using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Data;
using Instafuel.Api.Handlers;
using Instafuel.Api.Middleware;
using Instafuel.Api.Models;
using Instafuel.Api.Modules.Identity;
using Instafuel.Api.Modules.Accounts;
using Instafuel.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var deliveryFeeBdt = builder.Configuration["Checkout:DeliveryFeeBdt"] ?? builder.Configuration["DELIVERY_FEE_BDT"];
if (!string.IsNullOrWhiteSpace(deliveryFeeBdt))
{
    builder.Configuration["Checkout:DeliveryFeeBdt"] = deliveryFeeBdt;
}

if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(deliveryFeeBdt))
{
    throw new InvalidOperationException(
        "DELIVERY_FEE_BDT or Checkout:DeliveryFeeBdt is required outside Development. Set the business-approved flat fee from deployment configuration.");
}

Directory.CreateDirectory("logs");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [RequestId:{RequestId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/app-error-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [RequestId:{RequestId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) && builder.Environment.IsDevelopment())
{
    // Use a process-local key for local development; configure a stable secret for shared environments.
    jwtOptions.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

var otpOptions = builder.Configuration.GetSection(OtpOptions.SectionName).Get<OtpOptions>() ?? new OtpOptions();
if (string.IsNullOrWhiteSpace(otpOptions.HashingKey) && builder.Environment.IsDevelopment())
    otpOptions.HashingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
if (Encoding.UTF8.GetByteCount(otpOptions.HashingKey) < 32)
    throw new InvalidOperationException("Otp:HashingKey must contain at least 32 bytes.");

var smsOptions = builder.Configuration.GetSection(SmsOptions.SectionName).Get<SmsOptions>() ?? new SmsOptions();
smsOptions.Provider = builder.Configuration["Sms:Provider"]
    ?? (builder.Environment.IsDevelopment() ? "Console" : "SslWireless");
if (builder.Environment.IsDevelopment() && !smsOptions.Provider.Equals("Console", StringComparison.OrdinalIgnoreCase)
    && !smsOptions.Provider.Equals("SslWireless", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Sms:Provider must be Console or SslWireless.");
if (!builder.Environment.IsDevelopment()
    && !smsOptions.Provider.Equals("SslWireless", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Non-Development environments must use the configured SSL Wireless SMS provider.");
if (!builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(smsOptions.ApiToken) || string.IsNullOrWhiteSpace(smsOptions.SenderId)))
    throw new InvalidOperationException("Sms:ApiToken and Sms:SenderId are required outside Development.");
if (!Uri.TryCreate(smsOptions.Endpoint, UriKind.Absolute, out var smsEndpoint) || smsEndpoint.Scheme != Uri.UriSchemeHttps)
    throw new InvalidOperationException("Sms:Endpoint must be an HTTPS URL.");

var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
emailOptions.Provider = builder.Configuration["Email:Provider"]
    ?? (builder.Environment.IsDevelopment() ? "Console" : "Smtp");
if (builder.Environment.IsDevelopment() && !emailOptions.Provider.Equals("Console", StringComparison.OrdinalIgnoreCase)
    && !emailOptions.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Email:Provider must be Console or Smtp.");
if (!builder.Environment.IsDevelopment() && !emailOptions.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Non-Development environments must use SMTP for email delivery.");

if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
}
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || jwtOptions.AccessTokenMinutes is < 1 or > 60 || jwtOptions.RefreshTokenDays is < 1 or > 365)
{
    throw new InvalidOperationException("Jwt issuer/audience and token lifetimes must be configured with valid values (access 1-60 minutes, refresh 1-365 days).");
}

builder.Services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwtOptions));
builder.Services.AddSingleton<IOptions<OtpOptions>>(Options.Create(otpOptions));
builder.Services.AddSingleton<IOptions<SmsOptions>>(Options.Create(smsOptions));
builder.Services.AddSingleton<IOptions<EmailOptions>>(Options.Create(emailOptions));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken)
                    && context.HttpContext.Request.Path.StartsWithSegments("/hubs/delivery-tracking"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue("sub");
                if (!Guid.TryParse(idValue, out var userId))
                {
                    context.Fail("The token does not identify an account.");
                    return;
                }
                var db = context.HttpContext.RequestServices.GetRequiredService<PgDbContext>();
                if (!await db.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.IsActive,
                        context.HttpContext.RequestAborted))
                    context.Fail("The account is inactive.");
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddInstafuelAuthorization();
builder.Services.AddScoped<ISupplierMembershipLookup, SupplierMembershipLookup>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddOpenApi();
builder.Services.AddControllers(options => options.Filters.Add<FluentValidationActionFilter>());
builder.Services.AddSignalR();
builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        throw new AppValidationException(errors);
    };
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountManagementService, AccountManagementService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddHostedService<OtpCodeCleanupService>();
if (builder.Environment.IsDevelopment() && smsOptions.Provider.Equals("Console", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<ISmsSender, DevelopmentConsoleSmsSender>();
}
else
{
    builder.Services.AddHttpClient<SslWirelessSmsSender>();
    builder.Services.AddScoped<ISmsSender>(services => services.GetRequiredService<SslWirelessSmsSender>());
}

if (builder.Environment.IsDevelopment() && emailOptions.Provider.Equals("Console", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IEmailSender, DevelopmentConsoleEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddDbContext<PgDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"))
        .UseSnakeCaseNamingConvention()
        .ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>());

var app = builder.Build();

app.UseRouting();
app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    // Swagger middleware must run before the fallback authorization policy so
    // development documentation remains reachable without a bearer token.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PgDbContext>();
        dbContext.Database.Migrate();
    }

    app.MapOpenApi().AllowAnonymous();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapHub<Instafuel.Api.Modules.Riders.DeliveryTrackingHub>("/hubs/delivery-tracking");
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapGet("/health/ready", async (PgDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.Run();
