using System.Security.Claims;
using System.Threading.RateLimiting;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using replog_api.Endpoints;
using replog_api.Middleware;
using replog_api_host;
using replog_api_host.Endpoints;
using replog_api_host.Middleware;
using replog_application;
using replog_infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddJsonConsole();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("sync", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = int.Parse(builder.Configuration["RateLimiter:PermitLimit"] ?? "10"),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddReplogCors(builder.Environment);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();
app.UseCors();
// Authentication happens at the gateway; trust the injected x-user-id header.
app.UseMiddleware<TrustedUserMiddleware>();
app.UseRateLimiter();

app.MapSyncEndpoints();
app.MapHealthEndpoint("/api/sync/health");

app.Run();

namespace replog_api
{
    public partial class Program { }
}
