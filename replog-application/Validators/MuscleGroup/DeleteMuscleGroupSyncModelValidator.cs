using FluentValidation;
using replog_shared.Models.Sync.MuscleGroup;

namespace replog_application.Validators.MuscleGroup;

public class DeleteMuscleGroupSyncModelValidator : AbstractValidator<DeleteMuscleGroupSyncModel>
{
    public DeleteMuscleGroupSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Muscle group id is required.");

        RuleFor(x => x.WorkoutId)
            .NotEmpty().WithMessage("Workout id is required.");
    }
}
