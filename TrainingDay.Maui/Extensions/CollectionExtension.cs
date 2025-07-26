using System.Collections.ObjectModel;

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

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        return new ObservableCollection<T>(collection);
    }

    public static IEnumerable<KeyValuePair<DateTime, T>> TakeEvenlyDistributed<T>(
        this IEnumerable<KeyValuePair<DateTime, T>> source,
        int count = 5)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var orderedList = source.OrderBy(kv => kv.Key).ToList();

        if (orderedList.Count <= count)
        {
            return orderedList;
        }

        var result = new List<KeyValuePair<DateTime, T>>();
        double step = (orderedList.Count - 1) / (double)(count - 1);

        for (int i = 0; i < count; i++)
        {
            int index = (int)Math.Round(i * step);
            result.Add(orderedList[index]);
        }

        return result;
    }
}

public static class UIHelper
{
    public static IEnumerable<T> FindVisualChildren<T>(Element parent) where T : Element
    {
        if (parent == null) yield break;

        foreach (var child in parent.LogicalChildren)
        {
            if (child is T tChild)
            {
                yield return tChild;
            }

            // Recursively look for children of the specific type
            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}