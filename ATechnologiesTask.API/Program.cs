using ATechnologiesTask.Application.Interfaces;
using ATechnologiesTask.Application.Services;
using ATechnologiesTask.Core.Interfaces;
using ATechnologiesTask.Core.Settings;
using ATechnologiesTask.Infrastructure.BackgroundServices;
using ATechnologiesTask.Infrastructure.Data;
using ATechnologiesTask.Infrastructure.Services;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IBlockedCountryService, BlockedCountryService>();
builder.Services.AddSingleton<IBlockedCountryRepository, InMemoryBlockedCountryRepository>();
builder.Services.AddHttpClient<IGeoLocationService, GeoLocationService>();
builder.Services.AddHostedService<TemporalBlockCleanupService>();
builder.Services.Configure<IpGeolocationSettings>(builder.Configuration.GetSection("IpGeolocationSettings")); builder.Services.AddOptions();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blocked Countries API",
        Version = "v1",
        Description = "A .NET Core API for managing blocked countries and IP geolocation. Features include blocking/unblocking countries, checking IP-based country blocks, and logging blocked attempts. Each endpoint is rate-limited to 5 requests per minute. Use the Swagger UI to test endpoints and view responses."
    });
    c.EnableAnnotations();
});
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Path.ToString().ToLowerInvariant(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, 
                Window = TimeSpan.FromMinutes(1), 
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            })
    );

    options.RejectionStatusCode = 429;

    options.OnRejected = async (context, token) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Rate limit exceeded for path {Path} from IP {IP}",
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress);

        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            data = (object?)null,
            error = "Too many requests, please try again later.",
            statusCode = 429
        }, token);
    };
});


builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = string.Empty;
    });
}
app.UseRateLimiter();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

