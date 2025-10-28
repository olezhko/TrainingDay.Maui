using System.Collections.ObjectModel;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.ViewModels;

public sealed class PreparedTrainingViewModel : BaseViewModel
{
    public string Name { get; set; }

    public ImageSource TrainingImageUrl { get; set; }

    public Action CreateTraining { get; set; }

    public List<PreparedSuperSet> SuperSets { get; set; }

    public TrainingViewModel Training { get; set; }
}