namespace replog_tests_shared.Comparers;

internal static class DictionaryCompareHelper
{
    public static bool DictionariesEqual<TValue>(
        Dictionary<string, TValue>? a,
        Dictionary<string, TValue>? b,
        IEqualityComparer<TValue> valueComparer)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var other)) return false;
            if (!valueComparer.Equals(value, other)) return false;
        }

        return true;
    }
}
