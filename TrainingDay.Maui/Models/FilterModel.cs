using TrainingDay.Common;

namespace TrainingDay.Maui.Models;

public sealed class FilterModel
{
    public string NameFilter { get; set; }
    public List<MusclesEnum> CurrentMuscles { get; set; }

    public bool IsNoEquipmentFilter { get; set; }
    public bool IsBarbell { get; set; }
    public bool IsDumbbell { get; set; }

    public FilterModel()
    {
        CurrentMuscles = new List<MusclesEnum>();
    }
}