using System.Collections.ObjectModel;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

public class SuperSetViewModel : ObservableCollection<TrainingExerciseViewModel>
{
    public int Id { get; set; }

    public int TrainingId { get; set; }

    public bool IsSuperSet => SuperSetItems.Count > 1;

    public ObservableCollection<TrainingExerciseViewModel> SuperSetItems
    {
        get { return new ObservableCollection<TrainingExerciseViewModel>(this.Items); }
    }

    public SuperSet Model =>
        new SuperSet()
        {
            Count = Items.Count,
            Id = Id,
            TrainingId = TrainingId
        };
}