using FluentValidation;
using replog_shared.Enums;
using replog_shared.Models.Requests;

namespace replog_application.Validators;

public class SyncChangeDtoValidator : AbstractValidator<SyncChangeDto>
{
    public SyncChangeDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Change id is required.");

        RuleFor(x => x.EntityType)
            .IsInEnum().WithMessage("Invalid entity type.");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Invalid action.");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is required.");

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Data is required.");
    }
}
