namespace DiscriminatedUnionGenerator;

internal static class EnumerableExtensions
{
    internal static IEnumerable<T> Prepend<T>(this IEnumerable<T> current, IEnumerable<T> extra)
    {
        foreach (var item in extra)
        {
            yield return item;
        }

        foreach (var item in current)
        {
            yield return item;
        }
    }
}