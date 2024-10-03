using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Views;

public partial class SettingsPage : ContentPage
{
    private readonly Dictionary<string, CultureInfo> _availableLanguages = new Dictionary<string, CultureInfo>();

    public SettingsPage()
    {
        InitializeComponent();
        ShowAdvicesOnImplementingSwitch.IsToggled = Settings.IsShowAdvicesOnImplementing;
        ScreenOnImplementedSwitch.IsToggled = Settings.IsDisplayOnImplement;

        FillAvailableLanguage();
        LanguagePicker.SelectedIndexChanged += LanguageSwitch_Changed;
        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            DonateButton.IsVisible = false;
        }

        EmailSpan.Text = Settings.Email;
    }

    private void ScreenOnImplementedSwitch_OnToggled(object sender, ToggledEventArgs e)
    {
        Settings.IsDisplayOnImplement = ScreenOnImplementedSwitch.IsToggled;
    }

    private async void Donate_Click(object sender, EventArgs e)
    {
        await Browser.OpenAsync(@"https://www.donationalerts.com/r/trainingday", BrowserLaunchMode.SystemPreferred);
    }

    private void LanguageSwitch_Changed(object sender, EventArgs e)
    {
        try
        {
            CultureInfo selected = _availableLanguages[LanguagePicker.SelectedItem as string];
            bool updateExercise = selected.Name != Settings.CultureName;
            Settings.CultureName = selected.Name;
            if (updateExercise)
            {
                FixExercisesData();
            }

            LocalizationResourceManager.Instance.SetCulture(Settings.GetLanguage());

            Toast.Make(AppResources.ChangesAcceptAfterReboot).Show();
            LanguagePicker.Unfocus();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Crashes.TrackError(exception);
        }
    }

    private void FillAvailableLanguage()
    {
        var ru = new CultureInfo("ru-RU");
        var en = new CultureInfo("en-US");
        var de = new CultureInfo("de-DE");
        _availableLanguages.Add("Русский", ru);
        _availableLanguages.Add("English", en);
        _availableLanguages.Add("Deutsch", de);

        foreach (var language in _availableLanguages)
        {
            LanguagePicker.Items.Add(language.Key);
        }

        var current = Settings.GetLanguage();
        var selected = _availableLanguages.FirstOrDefault(item => item.Key == current.NativeName.Split(' ')[0].FirstCharToUpper());
        if (selected.Key == null)
        {
            LanguagePicker.SelectedIndex = 1;
        }
        else
        {
            LanguagePicker.SelectedItem = selected.Key; // need to change by find index in dictionary
        }
    }

    private void FixExercisesData()
    {
        var inits = ExerciseTools.InitExercises(Settings.GetLanguage().TwoLetterISOLanguageName);
        var exers = App.Database.GetExerciseItems();
        foreach (var exer in exers)
        {
            if (exer.CodeNum != 0)
            {
                try
                {
                    var init = inits.FirstOrDefault(item => item.CodeNum == exer.CodeNum);
                    if (init != null)
                    {
                        exer.Description = JsonConvert.SerializeObject(init.Description);
                        exer.ExerciseItemName = init.ExerciseItemName;
                        App.Database.SaveExerciseItem(exer);
                    }
                    else
                    {
                        Console.WriteLine($"NO AVAILABLE {exer.ExerciseItemName}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

    private void ShowAdvicesOnImplementingSwitch_OnToggled(object sender, ToggledEventArgs e)
    {
        Settings.IsShowAdvicesOnImplementing = ShowAdvicesOnImplementingSwitch.IsToggled;
    }

    private async void OpenStatistics_Click(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StatisticsPage));
    }

    private void PassShowButton_OnClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PassShowButton.Source = !PasswordEntry.IsPassword ? ImageSource.FromFile("pass_hide.png") : ImageSource.FromFile("pass_show.png");
    }
}