using FluentValidation;
using replog_shared.Models.Sync.Exercise;

namespace replog_application.Validators.Exercise;

public class DeleteExerciseSyncModelValidator : AbstractValidator<DeleteExerciseSyncModel>
{
    public DeleteExerciseSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Exercise id is required.");

        RuleFor(x => x.WorkoutId)
            .NotEmpty().WithMessage("Workout id is required.");

        RuleFor(x => x.MuscleGroupId)
            .NotEmpty().WithMessage("Muscle group id is required.");
    }
}
