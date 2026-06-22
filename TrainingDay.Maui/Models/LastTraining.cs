using System.Collections.ObjectModel;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models;

public class LastTraining
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime ImplementDateTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public int TrainingId { get; set; }
    public ObservableCollection<TrainingExerciseViewModel> Items { get; set; }

    public LastTraining()
    {
        Items = new ObservableCollection<TrainingExerciseViewModel>();
    }
}