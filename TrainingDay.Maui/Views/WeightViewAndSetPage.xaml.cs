using Microcharts;
using System.Text;
using TrainingDay.Common;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;
using CommunityToolkit.Maui.Core.Platform;

namespace TrainingDay.Maui.Views;

public partial class WeightViewAndSetPage : ContentPage
{
    private WeightViewAndSetPageViewModel vm;
    public WeightViewAndSetPage()
    {
        InitializeComponent();
        PeriodPicker.Items.Add(AppResources.WeekString);
        PeriodPicker.Items.Add(AppResources.TwoWeeksString);
        PeriodPicker.Items.Add(AppResources.OneMounthString);
        PeriodPicker.Items.Add(AppResources.TwoMonthString);
        PeriodPicker.Items.Add(AppResources.ThreeMounthString);
        PeriodPicker.Items.Add(AppResources.HalfYearString);
        PeriodPicker.Items.Add(AppResources.YearString);
        vm = new WeightViewAndSetPageViewModel();
        BindingContext = vm;
        BodyControlView.ChildAdded += ChilderAdded;
        vm.PropertyChanged += ViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PeriodPicker.SelectedIndex = 0;
        vm?.OnAppearing();
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
            var result = await MessageManager.DisplayAlert(AppResources.WaistHipTitle, sb.ToString(), AppResources.OkString, AppResources.CancelString);
            if (result)
            {
                await Browser.OpenAsync(Consts.Site + @"/waist-hip?year=2020&Month=10", BrowserLaunchMode.SystemPreferred);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}