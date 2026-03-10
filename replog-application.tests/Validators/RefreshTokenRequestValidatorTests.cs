using FluentValidation.TestHelper;
using replog_application.Validators;
using replog_shared.Models.Requests;

namespace replog_application.tests.Validators;

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenBothTokensAreProvided()
    {
        var request = new RefreshTokenRequest
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token"
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenAccessTokenIsEmpty()
    {
        var request = new RefreshTokenRequest
        {
            AccessToken = "",
            RefreshToken = "refresh-token"
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRefreshTokenIsEmpty()
    {
        var request = new RefreshTokenRequest
        {
            AccessToken = "access-token",
            RefreshToken = ""
        };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
