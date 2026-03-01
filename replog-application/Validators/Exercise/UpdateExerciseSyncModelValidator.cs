using FluentValidation;
using replog_shared.Models.Sync.Exercise;

namespace replog_application.Validators.Exercise;

public class UpdateExerciseSyncModelValidator : AbstractValidator<UpdateExerciseSyncModel>
{
    public UpdateExerciseSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Exercise id is required.");

        RuleFor(x => x.WorkoutId)
            .NotEmpty().WithMessage("Workout id is required.");

        RuleFor(x => x.MuscleGroupId)
            .NotEmpty().WithMessage("Muscle group id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Order index must be non-negative.");
    }
}
