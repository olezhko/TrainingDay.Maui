using System.Collections.ObjectModel;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

public class SuperSetViewModel<T> : ObservableCollection<T>
{
    public int Id { get; set; }

    public int TrainingId { get; set; }

    public bool IsSuperSet => SuperSetItems.Count > 1;

    public ObservableCollection<T> SuperSetItems
    {
        get { return new ObservableCollection<T>(this.Items); }
    }

    public SuperSetEntity Model =>
        new ()
        {
            Count = Items.Count,
            Id = Id,
            TrainingId = TrainingId
        };
}