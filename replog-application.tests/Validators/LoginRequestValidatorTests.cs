using FluentValidation.TestHelper;
using replog_application.Validators;
using replog_shared.Models.Requests;

namespace replog_application.tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenGoogleIdTokenIsProvided()
    {
        var request = new LoginRequest { GoogleIdToken = "some-valid-token" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenGoogleIdTokenIsEmpty(string? token)
    {
        var request = new LoginRequest { GoogleIdToken = token! };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.GoogleIdToken);
    }
}
