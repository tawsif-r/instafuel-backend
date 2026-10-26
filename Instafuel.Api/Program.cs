using Microsoft.EntityFrameworkCore;
using Instafuel.Api.Models;
using Instafuel.Api.Handlers;
using Instafuel.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog: console + error log file
// Directory.CreateDirectory("logs");
// Log.Logger = new LoggerConfiguration()
//     .MinimumLevel.Information()
//     .WriteTo.Console()
//     .WriteTo.File(
//         "logs/app-error-.log",
//         rollingInterval: RollingInterval.Day,
//         restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
//         outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}"
//     )
//     .CreateLogger();

// builder.Host.UseSerilog();


// =======================
// LOGS config
// ======================

Directory.CreateDirectory("logs");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [RequestId:{RequestId}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        "logs/app-error-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [RequestId:{RequestId}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Command",
    LogLevel.Warning
);



builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PgDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection")
    )
);

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<PgDbContext>();
    dbContext.Database.Migrate();

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();