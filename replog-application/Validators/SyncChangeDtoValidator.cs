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
            .Must(x => x is EntityType.Workout or EntityType.MuscleGroup
                    or EntityType.Exercise or EntityType.Log)
            .WithMessage("Entity type must be one of: workout, muscleGroup, exercise, log.");

        RuleFor(x => x.Action)
            .Must(x => x is ChangeAction.Create or ChangeAction.Update or ChangeAction.Delete)
            .WithMessage("Action must be one of: CREATE, UPDATE, DELETE.");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is required.");

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Data is required.");
    }
}
