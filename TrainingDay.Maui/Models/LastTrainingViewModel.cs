using System.Collections.ObjectModel;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models;

public class LastTrainingViewModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime ImplementDateTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public int TrainingId { get; set; }
    public ObservableCollection<TrainingExerciseViewModel> Items { get; set; }

    public LastTrainingViewModel()
    {
        Items = new ObservableCollection<TrainingExerciseViewModel>();
    }
}