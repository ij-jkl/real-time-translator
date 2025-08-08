using gatewayapi.Data;
using gatewayapi.Middleware;
using gatewayapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Database configuration
var databaseProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=transcriptions.db";

if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<TranscriptionDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<TranscriptionDbContext>(options =>
        options.UseSqlite(connectionString));
}

// Register services
builder.Services.AddScoped<ITranscriptionLogService, TranscriptionLogService>();
builder.Services.AddHostedService<DataRetentionService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlGroup(new Uri("http://localhost:8000/healthz"), name: "transcriptor-service")
    .AddUrlGroup(new Uri("http://localhost:9000/healthz"), name: "audio-streaming-service")
    .AddDbContextCheck<TranscriptionDbContext>("database");
    
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TranscriptionDbContext>();
    try
    {
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialization failed");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TranscriptionLoggingMiddleware>();

// API routes
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");
app.MapGet("/health/status", async (IServiceProvider services) =>
{
    var healthCheckService = services.GetRequiredService<HealthCheckService>();
    var result = await healthCheckService.CheckHealthAsync();
    
    return Results.Ok(new
    {
        status = result.Status.ToString(),
        services = result.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    });
});

// YARP reverse proxy
app.MapReverseProxy();

app.Run();
