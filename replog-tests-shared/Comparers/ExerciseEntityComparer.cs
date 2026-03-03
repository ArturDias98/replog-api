using replog_domain.Entities;

namespace replog_tests_shared.Comparers;

public class ExerciseEntityComparer : IEqualityComparer<ExerciseEntity>
{
    public static readonly ExerciseEntityComparer Instance = new();

    public bool Equals(ExerciseEntity? x, ExerciseEntity? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id
            && x.MuscleGroupId == y.MuscleGroupId
            && x.Title == y.Title
            && x.OrderIndex == y.OrderIndex
            && DictionaryCompareHelper.DictionariesEqual(x.Log, y.Log, LogEntityComparer.Instance);
    }

    public int GetHashCode(ExerciseEntity obj) =>
        HashCode.Combine(obj.Id, obj.MuscleGroupId, obj.Title, obj.OrderIndex);
}
