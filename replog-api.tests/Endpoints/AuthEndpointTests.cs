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
    public async Task Login_ShouldReturn200WithTokens_WhenGoogleTokenIsValid()
    {
        factory.GoogleValidator
            .ValidateAsync(Arg.Any<string>())
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
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.AccessToken);
        Assert.NotEmpty(body.RefreshToken);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenGoogleTokenIsInvalid()
    {
        factory.GoogleValidator
            .ValidateAsync(Arg.Any<string>())
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
