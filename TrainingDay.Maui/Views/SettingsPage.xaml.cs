using CommunityToolkit.Maui.Alerts;
using System.Globalization;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Views;

public partial class SettingsPage : ContentPage
{
    private readonly Dictionary<string, CultureInfo> _availableLanguages = new Dictionary<string, CultureInfo>();
    private WorkoutService workoutService;
    private readonly IAuthService authService;
    private readonly IUserSettingsService userSettingsService;

    public SettingsPage(WorkoutService workoutService, IAuthService authService, IUserSettingsService userSettingsService)
    {
        InitializeComponent();
        this.workoutService = workoutService;
        this.authService = authService;
        this.userSettingsService = userSettingsService;

        ShowAdvicesOnImplementingSwitch.IsToggled = Settings.IsShowAdvicesOnImplementing;
        ScreenOnImplementedSwitch.IsToggled = Settings.IsDisplayOnImplement;
        NicknameEntry.Text = Settings.Nickname;
        ShareCompletedWorkoutsSwitch.IsToggled = Settings.ShareCompletedWorkouts;

        FillAvailableLanguage();
        FillAvailableMeasureWeight();
        LanguagePicker.SelectedIndexChanged += LanguageSwitch_Changed;
        MeasureWeightPicker.SelectedIndexChanged += MeasureWeightPicker_Changed;
		DonateButton.IsVisible = DeviceInfo.Platform != DevicePlatform.iOS;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        RefreshAuthState();

        if (authService.IsLoggedIn)
        {
            var serverSettings = await userSettingsService.GetAsync();
            if (serverSettings != null)
            {
                NicknameEntry.Text = serverSettings.Nickname;
                ShareCompletedWorkoutsSwitch.IsToggled = serverSettings.ShareCompletedWorkouts;
            }
        }
    }

    private void RefreshAuthState()
    {
        var isLoggedIn = authService.IsLoggedIn;

        LoggedInAsLabel.IsVisible = isLoggedIn;
        LoggedInAsLabel.Text = isLoggedIn ? string.Format(AppResources.LoggedInAsString, Settings.UserEmail) : string.Empty;
        LoggedOutButtons.IsVisible = !isLoggedIn;
        LogoutButton.IsVisible = isLoggedIn;

        NickBorder.IsVisible = isLoggedIn;
        ShareCompletedWorkoutsSwitch.IsEnabled = isLoggedIn;
        LoginRequiredHintLabel.IsVisible = !isLoggedIn;
    }

    private async void LoginButton_Click(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private async void RegisterButton_Click(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async void LogoutButton_Click(object sender, EventArgs e)
    {
        await authService.LogoutAsync();
        RefreshAuthState();
    }

	private void ScreenOnImplementedSwitch_OnToggled(object sender, ToggledEventArgs e)
    {
        Settings.IsDisplayOnImplement = ScreenOnImplementedSwitch.IsToggled;
    }

    private async void Donate_Click(object sender, EventArgs e)
    {
        await Browser.OpenAsync(@"https://www.donationalerts.com/r/trainingday", BrowserLaunchMode.SystemPreferred);
    }

    private async void LanguageSwitch_Changed(object sender, EventArgs e)
    {
        try
        {
            CultureInfo selected = _availableLanguages[LanguagePicker.SelectedItem as string];
            bool updateExercise = selected.Name != Settings.CultureName;
            Settings.CultureName = selected.Name;
            if (updateExercise)
            {
                await workoutService.UpdateExerciseNameAndDescription();
            }

            LocalizationResourceManager.Instance.SetCulture(Settings.GetLanguage());

            await Toast.Make(AppResources.ChangesAcceptAfterReboot).Show();
            LanguagePicker.Unfocus();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            LoggingService.TrackError(exception);
        }
    }

    private void MeasureWeightPicker_Changed(object? sender, EventArgs e)
    {
        Settings.WeightMeasureType = MeasureWeightPicker.SelectedIndex;
    }

    private void FillAvailableMeasureWeight()
    {
        List<Tuple<MeasureWeightTypes, string>> items =
        [
            new Tuple<MeasureWeightTypes, string>(MeasureWeightTypes.Kilograms, AppResources.KilogramsString),
            new Tuple<MeasureWeightTypes, string>(MeasureWeightTypes.Lbs, AppResources.LbsString),
        ];

        foreach (var measureWeightType in items)
        {
            MeasureWeightPicker.Items.Add(measureWeightType.Item2);
        }

        MeasureWeightTypes current = (MeasureWeightTypes)Settings.WeightMeasureType;
        int index = items.FindIndex(0, item => item.Item1 == current);
        MeasureWeightPicker.SelectedIndex = index;
    }

    private void FillAvailableLanguage()
    {
        _availableLanguages.Add("Русский", new CultureInfo("ru"));
        _availableLanguages.Add("English", new CultureInfo("en"));
        _availableLanguages.Add("Deutsch", new CultureInfo("de"));
        _availableLanguages.Add("Português", new CultureInfo("pt"));
        _availableLanguages.Add("Español", new CultureInfo("es"));

        foreach (var language in _availableLanguages)
        {
            LanguagePicker.Items.Add(language.Key);
        }

        var current = Settings.GetLanguage();
        var selected = _availableLanguages.FirstOrDefault(item => item.Value.TwoLetterISOLanguageName == current.TwoLetterISOLanguageName);
        if (selected.Key == null)
        {
            LanguagePicker.SelectedIndex = 1;
        }
        else
        {
            LanguagePicker.SelectedItem = selected.Key; // need to change by find index in dictionary
        }
    }

    private void ShowAdvicesOnImplementingSwitch_OnToggled(object sender, ToggledEventArgs e)
    {
        Settings.IsShowAdvicesOnImplementing = ShowAdvicesOnImplementingSwitch.IsToggled;
    }

    private async void NicknameEntry_OnUnfocused(object sender, FocusEventArgs e)
    {
        if (!authService.IsLoggedIn)
        {
            return;
        }

        var value = NicknameEntry.Text;
        if (string.IsNullOrWhiteSpace(value))
        {
            NicknameEntry.Text = Settings.Nickname;
            return;
        }

        await userSettingsService.UpdateAsync(value.Trim(), ShareCompletedWorkoutsSwitch.IsToggled);
    }

    private async void ShareCompletedWorkoutsSwitch_OnToggled(object sender, ToggledEventArgs e)
    {
        if (!authService.IsLoggedIn)
        {
            return;
        }

        LoggingService.TrackEvent(ShareCompletedWorkoutsSwitch.IsToggled ? "Share Enabled" : "Share Disabled");
        await userSettingsService.UpdateAsync(NicknameEntry.Text, ShareCompletedWorkoutsSwitch.IsToggled);
    }

    private async void OpenStatistics_Click(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StatisticsPage));
    }
}