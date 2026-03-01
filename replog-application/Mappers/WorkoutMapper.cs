using replog_shared.Models.Entities;
using replog_shared.Models.Responses;

namespace replog_application.Mappers;

public static class WorkoutMapper
{
    public static WorkoutDto ToDto(WorkoutEntity entity)
    {
        return new WorkoutDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Date = entity.Date,
            OrderIndex = entity.OrderIndex,
            MuscleGroup = entity.MuscleGroup.Select(mg => new MuscleGroupDto
            {
                Id = mg.Id,
                WorkoutId = mg.WorkoutId,
                Title = mg.Title,
                Date = mg.Date,
                OrderIndex = mg.OrderIndex,
                Exercises = mg.Exercises.Select(e => new ExerciseDto
                {
                    Id = e.Id,
                    MuscleGroupId = e.MuscleGroupId,
                    Title = e.Title,
                    OrderIndex = e.OrderIndex,
                    Log = e.Log.Select(l => new LogDto
                    {
                        Id = l.Id,
                        NumberReps = l.NumberReps,
                        MaxWeight = l.MaxWeight,
                        Date = l.Date
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }
}
