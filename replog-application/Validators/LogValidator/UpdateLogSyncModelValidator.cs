using FluentValidation;
using replog_shared.Models.Sync.LogSync;

namespace replog_application.Validators.LogValidator;

public class UpdateLogSyncModelValidator : AbstractValidator<UpdateLogSyncModel>
{
    public UpdateLogSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Log id is required.");

        RuleFor(x => x.WorkoutId)
            .NotEmpty().WithMessage("Workout id is required.");

        RuleFor(x => x.MuscleGroupId)
            .NotEmpty().WithMessage("Muscle group id is required.");

        RuleFor(x => x.ExerciseId)
            .NotEmpty().WithMessage("Exercise id is required.");

        RuleFor(x => x.NumberReps)
            .GreaterThanOrEqualTo(0).WithMessage("Number of reps must be non-negative.");

        RuleFor(x => x.MaxWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Max weight must be non-negative.");
    }
}
