using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using replog_api.Settings;

namespace replog_api.Auth;

public class GoogleTokenValidator(IOptions<GoogleAuthSettings> settings) : IGoogleTokenValidator
{
    private readonly GoogleAuthSettings _settings = settings.Value;

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_settings.ClientId]
                });

            return new GoogleUserInfo
            {
                Subject = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                Picture = payload.Picture
            };
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
