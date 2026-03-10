using FluentValidation;
using replog_shared.Models.Requests;

namespace replog_application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.GoogleIdToken)
            .NotEmpty().WithMessage("Google ID token is required.");
    }
}
