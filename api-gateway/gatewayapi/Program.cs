using gatewayapi.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddUrlGroup(new Uri("http://localhost:8000/healthz"), name: "transcriptor-service")
    .AddUrlGroup(new Uri("http://localhost:9000/healthz"), name: "audio-streaming-service");
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add custom logging middleware before MapReverseProxy
app.UseMiddleware<RequestLoggingMiddleware>();

// Health check endpoints
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

// Enable YARP reverse proxy
app.MapReverseProxy();

app.Run();
