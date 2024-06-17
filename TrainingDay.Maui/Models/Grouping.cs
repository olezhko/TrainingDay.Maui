using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TrainingDay.Maui.Models;

public class Grouping<K, T> : ObservableCollection<T>
{
    public int Id { get; set; }
    public K Key { get; private set; }
    private bool _isExpanded = true;
    public bool Expanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            if (value)
            {
                foreach (var item in itemsMain)
                {
                    Add(item);
                }
                itemsMain.Clear();
            }
            else
            {
                foreach (var item in Items)
                {
                    itemsMain.Add(item);
                }

                foreach (var item in itemsMain)
                {
                    RemoveAt(0);
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Expanded"));
        }
    }

    private ObservableCollection<T> itemsMain = new ObservableCollection<T>();
    public Grouping(K key, IEnumerable<T> items)
    {
        Key = key;
        foreach (var item in items)
            this.Items.Add(item);
    }
}