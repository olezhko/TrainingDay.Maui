using TrainingDay.Common.Models;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models;

public sealed class FilterModel : BaseViewModel
{
    private string? nameFilter = null;
    private List<MusclesEnum> currentMuscles;
    private bool isNoEquipmentFilter;
    private bool isBarbellExists;
    private bool isDumbbellExists;
    private int difficultyLevel;

    public string? NameFilter { get => nameFilter; set => SetProperty(ref nameFilter, value); }
    public List<MusclesEnum> CurrentMuscles { get => currentMuscles; set => SetProperty(ref currentMuscles, value); }

    public bool IsNoEquipmentFilter { get => isNoEquipmentFilter; set => SetProperty(ref isNoEquipmentFilter, value); }
    public bool IsBarbellExists { get => isBarbellExists; set => SetProperty(ref isBarbellExists, value); }
    public bool IsDumbbellExists { get => isDumbbellExists; set => SetProperty(ref isDumbbellExists, value); }
    public int DifficultyLevel { get => difficultyLevel; set => SetProperty(ref difficultyLevel, value); }

    public FilterModel()
    {
        CurrentMuscles = [];
    }
}