using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using replog_api.Auth;
using replog_api.tests.Fixtures;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_api.tests.Endpoints;

[Collection("Api")]
public class AuthEndpointTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Login_ShouldReturn200WithCookiesAndExpiresAt_WhenGoogleTokenIsValid()
    {
        factory.GoogleValidator
            .ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo
            {
                Subject = "google-sub-123",
                Email = "user@example.com",
                Name = "Test User",
                Picture = "https://example.com/avatar.png"
            });

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { GoogleIdToken = "valid-google-token" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.Contains("access_token=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookies, c => c.Contains("refresh_token=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
        Assert.Equal("google-sub-123", body.UserId);
        Assert.Equal("user@example.com", body.Email);
        Assert.Equal("Test User", body.DisplayName);
        Assert.Equal("https://example.com/avatar.png", body.AvatarUrl);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenGoogleTokenIsInvalid()
    {
        factory.GoogleValidator
            .ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GoogleUserInfo?)null);

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { GoogleIdToken = "invalid-google-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("invalid_google_token", body.Error);
    }
}
