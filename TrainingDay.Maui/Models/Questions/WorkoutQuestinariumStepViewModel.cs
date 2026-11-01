using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models.Questions;

public class WorkoutQuestinariumStepViewModel : BaseViewModel
{
    private string title = string.Empty;
    private string instruction = string.Empty;
    private WorkoutQuestinariumStepViewModel? next;
    private WorkoutQuestinariumStepViewModel? previous;
    private ObservableCollection<WorkoutQuestinariumVariantViewModel> variants = [];

    public WorkoutQuestinariumStepViewModel()
    {
        SelectVariantCommand = new RelayCommand<WorkoutQuestinariumVariantViewModel>(SelectVariant);
    }

    public int Number { get => field; set => field = value; }

    /// <summary>Zero-based position of the step in the wizard.</summary>
    public int Index { get; set; }

    public bool IsMultiple { get; set; }

    public string Title { get => title; set => SetProperty(ref title, value); }
    public string Instruction { get => instruction; set => SetProperty(ref instruction, value); }

    public string Hint => IsMultiple ? AppResources.ChooseOneOrMoreAnswers : AppResources.ChooseOneAnswer;

    public bool HasSelection => Variants.Any(item => item.IsChecked);

    public ICommand SelectVariantCommand { get; }

    public ObservableCollection<WorkoutQuestinariumVariantViewModel> Variants
    {
        get => variants;
        set
        {
            foreach (var item in variants)
            {
                item.PropertyChanged -= OnVariantPropertyChanged;
            }

            variants = value;
            foreach (var item in variants)
            {
                item.PropertyChanged += OnVariantPropertyChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    public WorkoutQuestinariumStepViewModel? Next { get => next; set => SetProperty(ref next, value); }

    public WorkoutQuestinariumStepViewModel? Previous { get => previous; set => SetProperty(ref previous, value); }

    private void SelectVariant(WorkoutQuestinariumVariantViewModel? variant)
    {
        if (variant is null)
        {
            return;
        }

        if (IsMultiple)
        {
            variant.IsChecked = !variant.IsChecked;
            return;
        }

        foreach (var item in Variants)
        {
            item.IsChecked = item == variant;
        }
    }

    private void OnVariantPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkoutQuestinariumVariantViewModel.IsChecked))
        {
            OnPropertyChanged(nameof(HasSelection));
        }
    }
}
