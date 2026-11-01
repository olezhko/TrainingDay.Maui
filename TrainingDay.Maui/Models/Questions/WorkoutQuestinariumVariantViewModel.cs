using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models.Questions;

public class WorkoutQuestinariumVariantViewModel : BaseViewModel
{
    private bool isChecked;
    private string title = string.Empty;
    private bool isMultiple;
    private string option = string.Empty;

    public bool IsChecked { get => isChecked; set => SetProperty(ref isChecked, value); }
    public bool IsMultiple { get => isMultiple; set => SetProperty(ref isMultiple, value); }
    public string Title { get => title; set => SetProperty(ref title, value); }

    public string Option { get => option; set => SetProperty(ref option, value); }
}
