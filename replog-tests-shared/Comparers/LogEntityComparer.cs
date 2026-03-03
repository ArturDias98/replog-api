using replog_domain.Entities;

namespace replog_tests_shared.Comparers;

public class LogEntityComparer : IEqualityComparer<LogEntity>
{
    public static readonly LogEntityComparer Instance = new();

    public bool Equals(LogEntity? x, LogEntity? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Id == y.Id
            && x.NumberReps == y.NumberReps
            && x.MaxWeight == y.MaxWeight
            && x.Date == y.Date;
    }

    public int GetHashCode(LogEntity obj) =>
        HashCode.Combine(obj.Id, obj.NumberReps, obj.MaxWeight, obj.Date);
}
