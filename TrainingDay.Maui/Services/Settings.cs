using System.Globalization;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Services;

public static class Settings
{
    public static double WeightGoal
    {
        get => Preferences.Get(nameof(WeightGoal), 0.0);
        set => Preferences.Set(nameof(WeightGoal), value);
    }

    public static bool IsDisplayOnImplement
    {
        get => Preferences.Get(nameof(IsDisplayOnImplement), true);
        set => Preferences.Set(nameof(IsDisplayOnImplement), value);
    }

    public static bool IsTrainingNotFinished
    {
        get => Preferences.Get(nameof(IsTrainingNotFinished), false);
        set => Preferences.Set(nameof(IsTrainingNotFinished), value);
    }

    public static string IsTrainingNotFinishedTime
    {
        get => Preferences.Get(nameof(IsTrainingNotFinishedTime), string.Empty);
        set => Preferences.Set(nameof(IsTrainingNotFinishedTime), value);
    }

    public static string Token
    {
        get => Preferences.Get(nameof(Token), string.Empty);
        set => Preferences.Set(nameof(Token), value);
    }

    public static bool IsTokenSavedOnServer
    {
        get => Preferences.Get(nameof(IsTokenSavedOnServer), false);
        set => Preferences.Set(nameof(IsTokenSavedOnServer), value);
    }

    public static double WaistGoal
    {
        get => Preferences.Get(nameof(WaistGoal), 0.0);
        set => Preferences.Set(nameof(WaistGoal), value);
    }

    public static double HipGoal
    {
        get => Preferences.Get(nameof(HipGoal), 0.0);
        set => Preferences.Set(nameof(HipGoal), value);
    }

    public static string GoogleToken
    {
        get => Preferences.Get(nameof(GoogleToken), string.Empty);
        set => Preferences.Set(nameof(GoogleToken), value);
    }

    public static string Email
    {
        get => Preferences.Get(nameof(Email), string.Empty);
        set => Preferences.Set(nameof(Email), value);
    }

    public static string Password
    {
        get => Preferences.Get(nameof(Password), string.Empty);
        set => Preferences.Set(nameof(Password), value);
    }

    public static string LastDatabaseSyncDateTime
    {
        get => Preferences.Get(nameof(LastDatabaseSyncDateTime), string.Empty);
        set => Preferences.Set(nameof(LastDatabaseSyncDateTime), value);
    }

    /// <summary>
    /// like ru-RU.
    /// </summary>
    public static string CultureName
    {
        get => Preferences.Get(nameof(CultureName), string.Empty);
        set => Preferences.Set(nameof(CultureName), value);
    }

    public static int ToolTipsState
    {
        get => Preferences.Get(nameof(ToolTipsState), 0);
        set => Preferences.Set(nameof(ToolTipsState), value);
    }

    public static bool IsShowAdvicesOnImplementing
    {
        get => Preferences.Get(nameof(IsShowAdvicesOnImplementing), true);
        set => Preferences.Set(nameof(IsShowAdvicesOnImplementing), value);
    }

    public static string Nick
    {
        get => Preferences.Get(nameof(Nick), string.Empty);
        set => Preferences.Set(nameof(Nick), value);
    }

    public static int WeightMeasureType
    {
        get => Preferences.Get(nameof(WeightMeasureType), (int)MeasureWeightTypes.Kilograms);
        set => Preferences.Set(nameof(WeightMeasureType), value);
    }

    public static CultureInfo GetLanguage()
    {
        if (string.IsNullOrEmpty(CultureName))
        {
            return CultureInfo.CurrentCulture;
        }
        else
        {
            return new CultureInfo(CultureName);
        }
    }
}