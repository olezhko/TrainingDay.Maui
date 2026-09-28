using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Models;

public sealed record MeasureWeightOption(MeasureWeightTypes Type, string Name)
{
    public static IReadOnlyList<MeasureWeightOption> GetAll() =>
    [
        new(MeasureWeightTypes.Kilograms, AppResources.KilogramsString),
        new(MeasureWeightTypes.Lbs, AppResources.LbsString),
    ];
}
