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

    // Display texts, filled by the view model so the model stays free of formatting dependencies
    public string DateText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string ExerciseCountText { get; set; } = string.Empty;
    public ObservableCollection<TrainingExerciseViewModel> Items { get; set; }

    public LastTraining()
    {
        Items = new ObservableCollection<TrainingExerciseViewModel>();
    }
}