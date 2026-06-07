namespace replog_api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddReplogCors(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = environment.IsDevelopment()
                    ? new[] { "http://localhost:4200" }
                    : new[] { "https://replog.adrvcode.com", "https://api.replog.adrvcode.com", "https://localhost" };

                policy.WithOrigins(origins)
                    .WithHeaders("Content-Type")
                    .WithMethods("GET", "POST")
                    .AllowCredentials();
            });
        });

        return services;
    }
}
