using FluentValidation;
using replog_shared.Models.Sync.MuscleGroup;

namespace replog_application.Validators.MuscleGroup;

public class AddMuscleGroupSyncModelValidator : AbstractValidator<AddMuscleGroupSyncModel>
{
    public AddMuscleGroupSyncModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Muscle group id is required.");

        RuleFor(x => x.WorkoutId)
            .NotEmpty().WithMessage("Workout id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.OrderIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Order index must be non-negative.");
    }
}
