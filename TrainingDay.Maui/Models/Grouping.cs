using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TrainingDay.Maui.Models;

public class Grouping<K, T> : ObservableCollection<T>
{
    public int Id { get; set; }
    private K key;
    public K Key
    {
        get => key; set
        {
            key = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Key)));
        }
    }

    private bool isSelected = false;
    public bool IsSelected
    {
        get => isSelected; set
        {
            isSelected = value;
            OnPropertyChanged(new PropertyChangedEventArgs("IsSelected"));
        }
    }

    public Grouping(K key, IEnumerable<T> items)
    {
        Key = key;
        foreach (var item in items)
            this.Items.Add(item);
    }
}