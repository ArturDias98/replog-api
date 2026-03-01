using FluentValidation;
using replog_shared.Models.Requests;

namespace replog_application.Validators;

public class PushSyncRequestValidator : AbstractValidator<PushSyncRequest>
{
    public PushSyncRequestValidator()
    {
        RuleFor(x => x.Changes)
            .NotNull().WithMessage("Changes list is required.")
            .NotEmpty().WithMessage("Changes list cannot be empty.")
            .Must(c => c.Count <= 100).WithMessage("Maximum 100 changes per push request.");

        RuleForEach(x => x.Changes)
            .SetValidator(new SyncChangeDtoValidator());
    }
}
