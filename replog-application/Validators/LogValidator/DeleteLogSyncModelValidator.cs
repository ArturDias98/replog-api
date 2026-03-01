using FluentValidation;
using replog_shared.Models.Sync.LogSync;

namespace replog_application.Validators.LogValidator;

public class DeleteLogSyncModelValidator : AbstractValidator<DeleteLogSyncModel>
{
    public DeleteLogSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Log id is required.");

        RuleFor(x => x.WorkoutId)
            .NotEmpty().WithMessage("Workout id is required.");

        RuleFor(x => x.MuscleGroupId)
            .NotEmpty().WithMessage("Muscle group id is required.");

        RuleFor(x => x.ExerciseId)
            .NotEmpty().WithMessage("Exercise id is required.");
    }
}
