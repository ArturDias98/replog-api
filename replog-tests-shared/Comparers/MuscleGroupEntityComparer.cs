using replog_domain.Entities;

namespace replog_tests_shared.Comparers;

public class MuscleGroupEntityComparer : IEqualityComparer<MuscleGroupEntity>
{
    public static readonly MuscleGroupEntityComparer Instance = new();

    public bool Equals(MuscleGroupEntity? x, MuscleGroupEntity? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id
            && x.WorkoutId == y.WorkoutId
            && x.Title == y.Title
            && x.Date == y.Date
            && x.OrderIndex == y.OrderIndex
            && DictionaryCompareHelper.DictionariesEqual(x.Exercises, y.Exercises, ExerciseEntityComparer.Instance);
    }

    public int GetHashCode(MuscleGroupEntity obj) =>
        HashCode.Combine(obj.Id, obj.WorkoutId, obj.Title, obj.Date, obj.OrderIndex);
}
