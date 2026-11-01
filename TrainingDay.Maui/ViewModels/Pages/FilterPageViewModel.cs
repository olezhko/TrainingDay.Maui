using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public enum EquipmentFilterKind
{
    NoEquipment,
    Barbell,
    Dumbbell,
}

public sealed class FilterPageViewModel : BaseViewModel, IQueryAttributable
{
    private FilterModel filter = new();

    public FilterPageViewModel()
    {
        Muscles = new ObservableCollection<MuscleCheckItem>(Enum.GetValues<MusclesEnum>().Select(muscle => new MuscleCheckItem
        {
            Text = ExerciseExtensions.GetEnumDescription(muscle, Settings.GetLanguage()),
            Muscle = muscle,
        }));

        ToggleEquipmentCommand = new RelayCommand<EquipmentFilterKind>(ToggleEquipment);
        ToggleMuscleCommand = new RelayCommand<MuscleCheckItem>(ToggleMuscle);
        ResetDifficultyCommand = new RelayCommand(() => Filter.DifficultyLevel = 0);
        ResetAllCommand = new RelayCommand(ResetAll);
        ApplyCommand = new AsyncRelayCommand(Apply);

        Filter = new FilterModel();
    }

    public FilterModel Filter
    {
        get => filter;
        private set
        {
            filter.PropertyChanged -= Filter_PropertyChanged;

            SetProperty(ref filter, value);
            filter.PropertyChanged += Filter_PropertyChanged;

            foreach (var item in Muscles)
            {
                item.IsChecked = filter.Muscles.Contains(item.Muscle);
            }

            OnPropertyChanged(nameof(SelectedMuscles));
            NotifyStateChanged();
        }
    }

    public ObservableCollection<MuscleCheckItem> Muscles { get; }

    /// <summary>
    /// Muscles highlighted on the body map; change notifications tell the view to redraw it.
    /// </summary>
    public IReadOnlyList<MusclesEnum> SelectedMuscles => Filter.Muscles;

    public string DifficultyText => Filter.DifficultyLevel switch
    {
        1 => AppResources.DifficultyEasy,
        2 => AppResources.DifficultyMedium,
        3 => AppResources.DifficultyHard,
        _ => AppResources.DifficultyAny
    };

    public bool HasDifficulty => Filter.DifficultyLevel != 0;

    public bool HasActiveFilters => Filter.IsNoEquipmentFilter || Filter.IsBarbellExists || Filter.IsDumbbellExists
        || Filter.DifficultyLevel != 0 || Filter.Muscles.Count > 0;

    public ICommand ToggleEquipmentCommand { get; }

    public ICommand ToggleMuscleCommand { get; }

    public ICommand ResetDifficultyCommand { get; }

    public ICommand ResetAllCommand { get; }

    public ICommand ApplyCommand { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Filter", out var filterObj) && filterObj is FilterModel incoming)
        {
            // Edit a copy so leaving with Back doesn't half-apply the changes
            Filter = incoming.Clone();
        }
    }

    private void Filter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(DifficultyText));
        OnPropertyChanged(nameof(HasDifficulty));
        OnPropertyChanged(nameof(HasActiveFilters));
    }

    private void ToggleEquipment(EquipmentFilterKind kind)
    {
        switch (kind)
        {
            case EquipmentFilterKind.NoEquipment:
                Filter.IsNoEquipmentFilter = !Filter.IsNoEquipmentFilter;
                break;
            case EquipmentFilterKind.Barbell:
                Filter.IsBarbellExists = !Filter.IsBarbellExists;
                break;
            case EquipmentFilterKind.Dumbbell:
                Filter.IsDumbbellExists = !Filter.IsDumbbellExists;
                break;
        }
    }

    private void ToggleMuscle(MuscleCheckItem? item)
    {
        if (item == null)
            return;

        SetMuscleChecked(item, !item.IsChecked);
        OnMusclesChanged();
    }

    private void SetMuscleChecked(MuscleCheckItem item, bool isChecked)
    {
        item.IsChecked = isChecked;
        Filter.Muscles.RemoveAll(muscle => muscle == item.Muscle);
        if (isChecked)
            Filter.Muscles.Add(item.Muscle);
    }

    private void OnMusclesChanged()
    {
        OnPropertyChanged(nameof(SelectedMuscles));
        OnPropertyChanged(nameof(HasActiveFilters));
    }

    private void ResetAll()
    {
        Filter.IsNoEquipmentFilter = false;
        Filter.IsBarbellExists = false;
        Filter.IsDumbbellExists = false;
        Filter.DifficultyLevel = 0;
        foreach (var item in Muscles)
        {
            SetMuscleChecked(item, false);
        }

        OnMusclesChanged();
    }

    private async Task Apply()
    {
        // Hides the chips while the page animates out
        IsBusy = true;
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new FilterAcceptedForExercisesMessage(Filter));
    }
}
