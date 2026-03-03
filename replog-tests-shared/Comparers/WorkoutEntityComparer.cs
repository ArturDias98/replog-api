using replog_domain.Entities;

namespace replog_tests_shared.Comparers;

public class WorkoutEntityComparer : IEqualityComparer<WorkoutEntity>
{
    public static readonly WorkoutEntityComparer Instance = new();

    public bool Equals(WorkoutEntity? x, WorkoutEntity? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id
            && x.UserId == y.UserId
            && x.Title == y.Title
            && x.Date == y.Date
            && x.OrderIndex == y.OrderIndex
            && DictionaryCompareHelper.DictionariesEqual(x.MuscleGroup, y.MuscleGroup, MuscleGroupEntityComparer.Instance);
    }

    public int GetHashCode(WorkoutEntity obj) =>
        HashCode.Combine(obj.Id, obj.UserId, obj.Title, obj.Date, obj.OrderIndex);
}
