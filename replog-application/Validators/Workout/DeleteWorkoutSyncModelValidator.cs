using FluentValidation;
using replog_shared.Models.Sync.Workout;

namespace replog_application.Validators.Workout;

public class DeleteWorkoutSyncModelValidator : AbstractValidator<DeleteWorkoutSyncModel>
{
    public DeleteWorkoutSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Workout id is required.");
    }
}
