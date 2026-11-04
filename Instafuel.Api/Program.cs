using Microsoft.EntityFrameworkCore;
using Instafuel.Api.Models;
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
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        "logs/app-error-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Command",
    LogLevel.Warning
);



builder.Services.AddOpenApi();
// IgnoreCycles: loaded navigation properties point back at each other
// (e.g. Delivery -> Driver -> Deliveries), so skip the loop instead of throwing.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PgDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgresConnection")
    )
);

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();