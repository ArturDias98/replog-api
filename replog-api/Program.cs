using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using replog_api.Auth;
using replog_api.Endpoints;
using replog_api.Middleware;
using replog_api.Settings;
using replog_application;
using replog_infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddJsonConsole();

await SecretsLoader.LoadFromSecretsManagerAsync(builder);

// Authentication — Custom JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"] ?? "replog-api",
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"] ?? "replog-client",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies.TryGetValue("access_token", out var token)
                    ? token
                    : null;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:4200" }
            : new[] { "https://replog.adrvcode.com", "https://api.replog.adrvcode.com", "https://localhost" };

        policy.WithOrigins(origins)
            .WithHeaders("Content-Type")
            .WithMethods("GET", "POST")
            .AllowCredentials();
    });
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("Google"));
builder.Services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// When running inside AWS Lambda, this wires the ASP.NET Core pipeline to
// API Gateway HTTP API (v2) events. Outside Lambda it is a no-op and Kestrel runs.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapAuthEndpoints();
app.MapSyncEndpoints();
app.MapHealthEndpoints();

app.Run();

namespace replog_api
{
    public partial class Program { }
}
