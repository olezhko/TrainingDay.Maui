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

    public static int WeightMeasureType
    {
        get => Preferences.Get(nameof(WeightMeasureType), (int)MeasureWeightTypes.Kilograms);
        set => Preferences.Set(nameof(WeightMeasureType), value);
    }
    public static bool IsUserKnownExerciseFilters
    {
        get => Preferences.Get(nameof(IsUserKnownExerciseFilters), false);
        set => Preferences.Set(nameof(IsUserKnownExerciseFilters), value);
    }

    public static bool IsLightTheme
    {
        get => Preferences.Get(nameof(IsLightTheme), false);
        set => Preferences.Set(nameof(IsLightTheme), value);
    }

    public static string AuthToken
    {
        get => Preferences.Get(nameof(AuthToken), string.Empty);
        set => Preferences.Set(nameof(AuthToken), value);
    }

    public static string RefreshToken
    {
        get => Preferences.Get(nameof(RefreshToken), string.Empty);
        set => Preferences.Set(nameof(RefreshToken), value);
    }

    public static DateTime AccessTokenExpiresAtUtc
    {
        get => new DateTime(Preferences.Get(nameof(AccessTokenExpiresAtUtc), 0L), DateTimeKind.Utc);
        set => Preferences.Set(nameof(AccessTokenExpiresAtUtc), value.ToUniversalTime().Ticks);
    }

    public static DateTime RefreshTokenExpiresAtUtc
    {
        get => new DateTime(Preferences.Get(nameof(RefreshTokenExpiresAtUtc), 0L), DateTimeKind.Utc);
        set => Preferences.Set(nameof(RefreshTokenExpiresAtUtc), value.ToUniversalTime().Ticks);
    }

    public static string UserEmail
    {
        get => Preferences.Get(nameof(UserEmail), string.Empty);
        set => Preferences.Set(nameof(UserEmail), value);
    }

    public static string UserRole
    {
        get => Preferences.Get(nameof(UserRole), string.Empty);
        set => Preferences.Set(nameof(UserRole), value);
    }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(AuthToken);

    public static void ClearSession()
    {
        AuthToken = string.Empty;
        RefreshToken = string.Empty;
        AccessTokenExpiresAtUtc = DateTime.MinValue;
        RefreshTokenExpiresAtUtc = DateTime.MinValue;
        UserEmail = string.Empty;
        UserRole = string.Empty;
    }

    /// <summary>
    /// Local cache of the last known server value (from /api/UserSettings), so code that needs a
    /// synchronous read (e.g. deciding whether to share a just-finished workout) doesn't need an
    /// async round trip. The server is the source of truth; see IUserSettingsService.
    /// </summary>
    public static string Nickname
    {
        get => Preferences.Get(nameof(Nickname), string.Empty);
        set => Preferences.Set(nameof(Nickname), value);
    }

    /// <summary>
    /// Local cache of the last known server value (from /api/UserSettings); see <see cref="Nickname"/>.
    /// </summary>
    public static bool ShareCompletedWorkouts
    {
        get => Preferences.Get(nameof(ShareCompletedWorkouts), true);
        set => Preferences.Set(nameof(ShareCompletedWorkouts), value);
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