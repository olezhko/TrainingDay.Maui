namespace TrainingDay.Maui.Extensions;

public static class CollectionExtensions
{
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        foreach (var item in collection)
        {
            action(item);
        }
    }
}