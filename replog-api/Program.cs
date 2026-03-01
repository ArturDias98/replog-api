using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using replog_api.Endpoints;
using replog_api.Middleware;
using replog_application;
using replog_infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Authentication — Google JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://accounts.google.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = ["accounts.google.com", "https://accounts.google.com"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Google:ClientId"],
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// Rate limiting — 10 sync requests per minute per user
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("sync", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ValidationExceptionHandler>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapSyncEndpoints();

app.Run();
