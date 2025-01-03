using System.Text;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;
using CommunityToolkit.Maui.Core.Platform;
using Microsoft.Maui.Controls;
using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace TrainingDay.Maui.Views;

public partial class WeightViewAndSetPage : ContentPage
{
    private WeightViewAndSetPageViewModel vm;
    public WeightViewAndSetPage()
    {
        InitializeComponent();
        vm = new WeightViewAndSetPageViewModel();
        BindingContext = vm;
        BodyControlView.ChildAdded += ChilderAdded;
        vm.PropertyChanged += ViewModelPropertyChanged;

        var md = App.Current.Resources.MergedDictionaries.First();
        inactiveColor = App.Current.RequestedTheme == AppTheme.Dark ? (Color)md["ContentPageBackgroundColor"] : (Color)md["ContentPageBackgroundColorLight"];
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        vm?.OnAppearing();
    }

    Color inactiveColor;
    Button lastPeriodButton;
    private void SetPeriod_Click(object sender, EventArgs args)
    {
        if (lastPeriodButton is not null)
        {
            lastPeriodButton.BackgroundColor = inactiveColor;
        }

        lastPeriodButton = sender as Button;

        ChartWeightPeriod period = (ChartWeightPeriod)Enum.Parse(typeof(ChartWeightPeriod), lastPeriodButton.AutomationId);
        vm.WeightPeriodChangedCommand.Execute(period);
        lastPeriodButton.BackgroundColor = Colors.Orange;
    }

    private void ViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(WeightViewAndSetPageViewModel.BodyControlItems))
        {
            foreach (var entry in weightEntryList)
            {
                entry.HideKeyboardAsync(CancellationToken.None);
            }
        }
    }

    List<Entry> weightEntryList = new List<Entry>();
    private void ChilderAdded(object sender, ElementEventArgs args)
    {
        Grid element = args.Element as Grid;
        var entries = TrainingDay.Maui.Extensions.UIHelper.FindVisualChildren<Entry>(element);
        weightEntryList.AddRange(entries);
    }

    private async void ShowInfo_Click(object sender, EventArgs e)
    {
        try
        {
            double coef = -1;
            if (vm.BodyControlItems.Any(item => item.Type == WeightType.Waist) && vm.BodyControlItems.Any(item => item.Type == WeightType.Waist))
            {
                var waist = vm.BodyControlItems.First(item => item.Type == WeightType.Waist).CurrentValue;
                var hips = vm.BodyControlItems.First(item => item.Type == WeightType.Hip).CurrentValue;
                coef = waist / hips;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(AppResources.WaistHipMessage.Replace("_", "\n"), coef));
            var result = await MessageManager.DisplayAlert(AppResources.WaistHipTitle, sb.ToString(), AppResources.YesString, AppResources.CancelString);
            if (result)
            {
                await Browser.OpenAsync("https://tday-app.blogspot.com/2024/12/blog-post_19.html", BrowserLaunchMode.SystemPreferred);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}